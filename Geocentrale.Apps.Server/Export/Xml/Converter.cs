using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.DataContracts.Filter;
using Geocentrale.Apps.Server.Helper;
using Geocentrale.Common;
using Geocentrale.DataAdaptors.ArcgisServerRestAdaptor;
using Geocentrale.DataAdaptors.Contracts;
using Geocentrale.DataAdaptors.Resolver;
using Geocentrale.Filter;
using Oereb.Service.DataContracts.Model.v04;

namespace Geocentrale.Apps.Server.Export.Xml
{
    /// <summary>
    /// converts the native reportObject from the App to the xml-class (converted with xsd.exe)
    /// </summary>

    public class Converter
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private MergerRequest _mergerRequest { get; set; }
        private GAReport _gAReport { get; set; }
        private OerebResult _oerebResult { get; set; }
        private Config _config { get; set; }

        private int _dpi { get; set; } = 300;

        /// <summary>
        /// include the images in the realEstate and restrictionOnLandownership
        /// </summary>
        private bool _includeMap { get; set; } = true;

        /// <summary>
        /// include additional raster-information in the extension section of restrictionOnLandownership
        /// </summary>
        private bool _includeMapAdditionalTheme { get; set; } = true;

        /// <summary>
        /// include additional raster-information in the extension section of realEstate
        /// </summary>
        private bool _includeSelectionGeometry { get; set; } = true;

        /// <summary>
        /// include additional xml-data in the extension section of realEstate and restrictionOnLandownership
        /// </summary>
        private bool _includeDetail { get; set; } = true;

        private bool _includeLogo { get; set; } = true;

        private Image _imageEmptyPng { get; set; }

        private byte[] _imageEmptyPngByteArray { get; set; }

        private Dictionary<string,Image> _imageCache{ get; set; }

        public Converter(MergerRequest mergerRequest, GAReport gAReport)
        {
            _mergerRequest = mergerRequest;
            _gAReport = gAReport;
            _oerebResult = new OerebResult(gAReport);
            _config = Global.Config;

            _includeMap = mergerRequest.IncludeMap;
            _includeLogo = mergerRequest.IncludeLogo;
            _includeDetail = mergerRequest.IncludeDetail;

            _imageEmptyPng = ImageTasks.GetImageFromBase64String(OerebExportModule.EmptyPngBase64);
            _imageEmptyPngByteArray = ImageTasks.ImageToByteArray(_imageEmptyPng, ImageFormat.Png);

            _imageCache = new Dictionary<string, Image>();
        }

        public string Process()
        {
            string result = string.Empty;

            foreach (var parcel in _oerebResult.Parcels)
            {
                if (_oerebResult.Parcels.Count > 1)
                {
                    throw new NotSupportedException("at this time we does not support more than one parcel in a reportobject");
                }

                var cantonname = _config.GetCantonFromMunicipality(parcel.Municipality).Shorname;

                var canton = _config.Cantons.FirstOrDefault(x => x.Shorname == cantonname);

                if (canton == null)
                {
                    throw new Exception($"canton {cantonname} not found in config");
                }

                var extractData = new Extract();

                //main-section

                extractData.CreationDate = DateTime.Now;
                extractData.ExtractIdentifier = Guid.NewGuid().ToString();

                //extract-topics

                var sectionCt = parcel.Sections.FirstOrDefault(x => x.Key == OerebResult.SectionType.ConcernedTheme).Value;
                extractData.ConcernedTheme = sectionCt.Topics.OrderBy(x=>x.ConfigTopic.Seq).Select(x => 
                    new Theme()
                    {
                        Code = x.ConfigTopic.NameEnum,
                        Text = new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = x.ConfigTopic.Name }
                    } 
                ).ToArray();

                var sectionNot = parcel.Sections.FirstOrDefault(x => x.Key == OerebResult.SectionType.NotConcernedTheme).Value;

                //todo filter topics which not exist in country
                var test = sectionNot.Topics.Select(x => !canton.TopicsDictionary.ContainsKey(x.Name)).ToList();

                extractData.NotConcernedTheme = sectionNot.Topics.OrderBy(x => x.ConfigTopic.Seq).Select(x => new Theme() { Code = x.ConfigTopic.NameEnum, Text = new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = x.ConfigTopic.Name } }).ToArray();

                var sectionWod = parcel.Sections.FirstOrDefault(x => x.Key == OerebResult.SectionType.ThemeWithoutData).Value;
                extractData.ThemeWithoutData = sectionWod.Topics.OrderBy(x => x.ConfigTopic.Seq).Select(x => new Theme() { Code = x.ConfigTopic.NameEnum, Text = new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = x.ConfigTopic.Name } }).ToArray();

                //only true is possible per definition

                extractData.isReduced = true;

                //extract-Images

                extractData.Item1 = _includeLogo ? ImageTasks.ImageToByteArray(_config.LogoNational(), ImageFormat.Png) : _imageEmptyPngByteArray;
                extractData.Item = _includeLogo ? ImageTasks.ImageToByteArray(_config.LogoOereb(), ImageFormat.Png) : _imageEmptyPngByteArray;
                extractData.Item2 = _includeLogo ? ImageTasks.ImageToByteArray(_config.LogoCanton(cantonname), ImageFormat.Png) : _imageEmptyPngByteArray;
                extractData.Item3 = _includeLogo ? ImageTasks.ImageToByteArray(_config.LogoMunicipality(parcel.Municipality), ImageFormat.Png) : _imageEmptyPngByteArray;

                var urlTemplate = ConfigurationManager.AppSettings["rootUrlXmlExport"] ?? "http://127.0.0.1/this_should_not_happen";

                urlTemplate = urlTemplate.Replace("{language}", "de");
                urlTemplate = urlTemplate.Replace("{format}", "pdf");
                urlTemplate = urlTemplate.Replace("{egrid}", parcel.Egrid);

                Uri urlToExtract = new Uri(urlTemplate, UriKind.Absolute); //todo take the right url
                extractData.Item4 = _includeLogo ? ImageTasks.ImageToByteArray(_config.QrCode(urlToExtract, 200), ImageFormat.Png) : _imageEmptyPngByteArray;

                extractData.GeneralInformation = canton.GeneralInformation.Select(x => ConvertLocalisedMText(x)).ToArray();
                extractData.BaseData = canton.Basedata.Select(x => ConvertLocalisedMText(x)).ToArray();

                foreach (var data in extractData.BaseData)
                {
                    data.Text = data.Text.Replace("#nfdata#", $"{DateTime.Now.AddDays(-1):dd.MM.yyyy}"); //automatic data now minus 1 day
                }

                extractData.Glossary = GetGlossary(canton.Glossaries);

                extractData.ExclusionOfLiability = GetExclusionOfLiabilities(canton.ExclusionOfLiabilities);

                var cadastralAuthority = canton.CadastralAuthority;
                extractData.PLRCadastreAuthority = GetOffice(cadastralAuthority);

                //parcel

                extractData.RealEstate = GetRealEstate(parcel, _config, canton);

                result = Xml<Extract>.SerializeToXmlString(extractData);
            }

            return FixSerializationProblems(result);
        }

        #region private

        private string FixSerializationProblems(string result)
        {
            return result.Replace("&gt;", ">").Replace("&lt;", "<");

            //TODO optimize serialization
            //return result.Replace("<ItemGeometryLOLS>", "").Replace("</ItemGeometryLOLS>", "");
        }

        private RealEstate_DPR GetRealEstate(OerebResult.Parcel parcel, Config config, Config.Canton canton)
        {
            var realEstate = new RealEstate_DPR();
            var selectionlayer = canton.Resources.FirstOrDefault(x => x.Type == Config.Resource.ResourceType.Selectionlayer);

            if (selectionlayer == null)
            {
                throw new Exception("each config needs a selectionlayer");
            }

            realEstate.Number = parcel.Nummer;
            realEstate.EGRID = parcel.Egrid;
            realEstate.FosNr = parcel.BfsNr.ToString();
            realEstate.Canton = EnumResolve.ParseEnum<CantonCode>(canton.Shorname);
            realEstate.IdentDN = $"{realEstate.Canton}02{parcel.BfsNr:00000000}";
            realEstate.Type = RealEstateType.RealEstate; //TODO introduce RealEstateType.Distinct_and_permanent_rightsBuildingRight in GaObject

            realEstate.LandRegistryArea = parcel.LandRegistryArea.ToString();
            realEstate.Municipality = parcel.Municipality;

            realEstate.SubunitOfLandRegister = "";                                     //TODO use we that ?
            realEstate.MetadataOfGeographicalBaseData = selectionlayer.MetadataUrl;

            realEstate.Limit = GetLimit(parcel.WktGeometry, parcel.GeoDataAdaptor.GaGeoClass.SpatialReferenceEpsg).ToString();

            realEstate.RestrictionOnLandownership = GetRestrictionsOnLandownership(parcel.Sections, canton);

            var baselayers = canton.Resources.Where(x => x.Type == Config.Resource.ResourceType.Baselayer);

            if (baselayers.Count() != 2)
            {
                throw new Exception("each config needs two baselayers, for the title and for the body");
            }

            var imageSets = new List<ImageSet>()
            {
                new ImageSet()
                {
                    Extent = parcel.FrameExtent,
                    Size = parcel.FrameSize,
                    GeoDataset = (IGAGeoDataSet) Catalog.Catalog.GetDataSet(baselayers.First().Guid),
                    Type = GeoDatasetType.Baselayer
                },
                new ImageSet()
                {
                    Extent = parcel.FrameExtent,
                    Size = parcel.FrameSize,
                    GeoDataset = (IGAGeoDataSet) Catalog.Catalog.GetDataSet(baselayers.Last().Guid),
                    Type = GeoDatasetType.Baselayer
                }
            };

            RenderImages(imageSets);

            if (imageSets.First().Image == null)
            {
                throw new Exception("image of title is empty");
            }

            var imageTitle = ImageTasks.ImageToByteArray(imageSets.First().Image, ImageFormat.Png);

            realEstate.PlanForLandRegister = new Map()
            {
                Image = imageTitle,
                ReferenceWMS = canton.RealEstate.WmsUrl,
                LegendAtWeb = new WebReference() {Value = canton.RealEstate.LegendUrl },
                extensions = new extensions()
                {
                    Any = new XmlElement[]
                    {
                        GetElement($"<MapExtension><Scale>{parcel.Scale}</Scale><Blowfactor>{parcel.FrameScaleFactor}</Blowfactor><Dpi>{parcel.FrameDpi}</Dpi><Extent><Xmin>{parcel.FrameExtent.Xmin}</Xmin><Xmax>{parcel.FrameExtent.Xmax}</Xmax><Ymin>{parcel.FrameExtent.Ymin}</Ymin><Ymax>{parcel.FrameExtent.Ymax}</Ymax></Extent><Seq>{baselayers.First().Seq}</Seq><Transparency>{baselayers.First().Transparency}</Transparency></MapExtension>"),
                    }
                }
                //OtherLegend = new LegendEntry[], //TODO no legend entry for the parcel
            };

            var concernedTheme = parcel.Sections[OerebResult.SectionType.ConcernedTheme];
            var extensionAdditionalLayers = String.Empty;
             
            foreach (var topic in concernedTheme.Topics)
            {
                foreach (var additionalLayer in topic.ConfigTopic.AdditionalLayers)
                {
                    var geoDataset =  (IGAGeoDataSet)Catalog.Catalog.GetDataSet(additionalLayer.Guid);

                    var imageSet = new ImageSet()
                    {
                        Extent = topic.Parcel.FrameExtent,
                        Size = topic.Parcel.FrameSize,
                        MimeType = Setting.DefaultMimeType,
                        GeoDataset = geoDataset
                    };

                    RenderImages(new List<ImageSet>() { imageSet });

                    extensionAdditionalLayers += $"<AdditionalLayer><Guid>{additionalLayer.Guid}</Guid><Topicname>{topic.Name}</Topicname><Image>{GetImageBase64Encoded(imageSet.Image)}</Image><Scale>{parcel.Scale}</Scale><Blowfactor>{parcel.FrameScaleFactor}</Blowfactor><Dpi>{parcel.FrameDpi}</Dpi><Extent><Xmin>{parcel.FrameExtent.Xmin}</Xmin><Xmax>{parcel.FrameExtent.Xmax}</Xmax><Ymin>{parcel.FrameExtent.Ymin}</Ymin><Ymax>{parcel.FrameExtent.Ymax}</Ymax></Extent><Seq>{additionalLayer.Seq}</Seq><Transparency>{additionalLayer.Transparency}</Transparency></AdditionalLayer>";
                }
            }

            realEstate.extensions = new extensions() {Any = new XmlElement[]
            {
                GetElement($"<RealEstateExtension><PlanForROL><Image>{GetImageBase64Encoded(imageSets.Last().Image)}</Image><Scale>{parcel.Scale}</Scale><Blowfactor>{parcel.FrameScaleFactor}</Blowfactor><Dpi>{parcel.FrameDpi}</Dpi><Extent><Xmin>{parcel.FrameExtent.Xmin}</Xmin><Xmax>{parcel.FrameExtent.Xmax}</Xmax><Ymin>{parcel.FrameExtent.Ymin}</Ymin><Ymax>{parcel.FrameExtent.Ymax}</Ymax></Extent><Seq>{baselayers.Last().Seq}</Seq><Transparency>{baselayers.Last().Transparency}</Transparency></PlanForROL>{extensionAdditionalLayers}</RealEstateExtension>"),
            }
            };

            return realEstate;
        }

        private static string GetImageBase64Encoded(Image imageIn)
        {
            using (Image image = imageIn)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, ImageFormat.Png);
                    byte[] imageBytes = memoryStream.ToArray();
                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }

        private static XmlElement GetElement(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        private XElement GetLimit(string geometry, int epsgCode)
        {
            string convertToMultiPolygon = geometry;

            if (geometry.ToLower().Trim().StartsWith("polygon"))
            {
                convertToMultiPolygon = $"{geometry.ToLower().Replace("polygon", "MULTIPOLYGON(")})";
            }

            return GetGeometry(convertToMultiPolygon, epsgCode);
        }

        private RestrictionOnLandownership[] GetRestrictionsOnLandownership(Dictionary<OerebResult.SectionType, OerebResult.Section> sections, Config.Canton canton)
        {
            var restrictionsOnLandownership = new List<RestrictionOnLandownership>();
            var concernedTheme = sections[OerebResult.SectionType.ConcernedTheme];

            foreach (var topic in concernedTheme.Topics.OrderBy(x=>x.ConfigTopic.Seq))
            {
                foreach (var oerebDef in topic.OerebDefs)
                {
                    if (oerebDef.Features.Count > 1)
                    {
                        log.Warn($"oerebdef has more {oerebDef.Features.Count} features, is that possible");
                    }

                    //ToDo question group the oerebDef geometric childs (feature) by gaObject.DsGuid ???
                    //special case: only layer with relations has more than one object 

                    foreach (var feature in oerebDef.Features)
                    {                    
                        var restrictionOnLandownership = new RestrictionOnLandownership();

                        var themeConfig = canton.Topics.First(x => x.Name == topic.Name);

                        restrictionOnLandownership.Theme = new Theme() { Code = themeConfig.NameEnum, Text = new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = themeConfig.Name } };
                        restrictionOnLandownership.SubTheme = topic.Name;
                        restrictionOnLandownership.Information = new LocalisedMText[] { new LocalisedMText() { Language = LanguageCode.de, LanguageSpecified = true, Text = oerebDef.Information } }; //Aussage

                        var lawStatusCode = EnumResolve.ParseEnum<LawstatusCode>(oerebDef.LawStatus.ToString());
                        restrictionOnLandownership.Lawstatus = new Lawstatus() { Code = lawStatusCode, Text = new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = oerebDef.LawStatus.ToString() } };

                        restrictionOnLandownership.ResponsibleOffice = new Office()
                        {
                            Name = new LocalisedText[] {new LocalisedText() {Language = LanguageCode.de, LanguageSpecified = true, Text = oerebDef.OfficeName} },
                            OfficeAtWeb = new WebReference() { Value = oerebDef.OfficeAtWeb}
                        };

                        var legendItem = GetLegendFromFeature(feature.Item.Value);  //Common.GetLegendFromFeature(feature.Item.Value);

                        restrictionOnLandownership.Item = ImageTasks.ImageToByteArray(legendItem.Image ?? _imageEmptyPng, ImageFormat.Png);

                        var symbolLabel = Common.GetLabel((IGAGeoDataAdaptor)feature.DataSet.GetDataUtilities().Last(), feature.Item.Value); //TODO limitation OerebAdaptor

                        restrictionOnLandownership.TypeCode = legendItem.Key;
                        //restrictionOnLandownership.TypeCodelist = ""; //todo not used at this time

                        //------------------------------------------------------------------------------------------------
                        //Geometry

                        restrictionOnLandownership.Geometry = new Geometry[] {new Geometry()
                        {
                           ItemElementName = GetGeometryType(feature.GaGeoClass),
                           ItemGeometryLOLS =  GetGeometry(feature.WktGeometry,feature.GaGeoClass.SpatialReferenceEpsg).ToString(),
                           Lawstatus = new Lawstatus() {Code = EnumResolve.ParseEnum<LawstatusCode>(feature.LawStatus.ToString()), Text = new LocalisedText() {Language = LanguageCode.de, LanguageSpecified = true, Text = feature.LawStatus.ToString()} },
                           MetadataOfGeographicalBaseData = feature.MetadataUrl,
                           ResponsibleOffice = new Office()
                           {
                               Name = new LocalisedText[] {new LocalisedText() {Language = LanguageCode.de, LanguageSpecified = true, Text = "-"}}, //TODO content
                               OfficeAtWeb = new WebReference() { Value = "http://127.0.0.1"} //TODO content
                           },
                           extensions = new extensions() {Any = new XmlElement[]
                                {
                                    GetElement($"<GeometryExtension><Type>{feature.GaGeoClass.GeometryType}</Type></GeometryExtension>"),
                                }
                            }
                        }};

                        //------------------------------------------------------------------------------------------------
                        //Map

                        //todo make GetMap Async or use a cache
                        restrictionOnLandownership.Map = GetMap(feature, topic);

                        restrictionOnLandownership.Area = Math.Round(feature.Area,0).ToString(CultureInfo.InvariantCulture);
                        restrictionOnLandownership.PartInPercent = Math.Round(feature.PartinPercent*100).ToString(CultureInfo.InvariantCulture);

                        var relevantGeodatasetsFromExtent = new List<IGAGeoDataSet>() {feature.GeoDataset};

                        var legendItems = GetLegendFromExtent(relevantGeodatasetsFromExtent, topic.Parcel.FrameExtent);
                        var legendItemsDistinct = legendItems.GroupBy(x => x.Key).Select(x => x.First()).ToList();

                        restrictionOnLandownership.Map.OtherLegend = legendItemsDistinct.Select(x => new LegendEntry()
                        {
                            Item = ImageTasks.ImageToByteArray(x.Image, Setting.DefaultMimeType),
                            LegendText = new LocalisedText[] {new LocalisedText() {Language = LanguageCode.de, LanguageSpecified = true, Text = x.Label} },
                            TypeCode = x.Key,
                            TypeCodelist = "", //TODO what is the content
                            Theme = restrictionOnLandownership.Theme,
                            SubTheme = restrictionOnLandownership.SubTheme,
                        }).ToArray();

                        //------------------------------------------------------------------------------------------------
                        //Documents

                        restrictionOnLandownership.LegalProvisions = GetDocuments(oerebDef,canton);

                        //------------------------------------------------------------------------------------------------
                        //Extension

                        if (_includeDetail)
                        {
                            var rolExtension = "<RestrictionOnLandownershipExtension><Attributes>{0}</Attributes></RestrictionOnLandownershipExtension>";
                            var attributes = string.Empty;

                            foreach (var attribute in feature.Item.Value.Attributes)
                            {
                                if (attribute.AttributeSpec.Name.StartsWith("GAGeometry") || attribute.AttributeSpec.Name.StartsWith("GARuleExpression") || attribute.AttributeSpec.Name.StartsWith("GANiceRuleExpression"))
                                {
                                    continue;
                                }

                                attributes += $"<Attribute><Name>{attribute.AttributeSpec.Name}</Name><Type>{attribute.AttributeSpec.TypeName}</Type><Value>{attribute.Value.ToString()}</Value></Attribute>";
                            }

                            restrictionOnLandownership.extensions = new extensions()
                            {
                                Any = new XmlElement[]
                                {
                                    GetElement(String.Format(rolExtension, attributes)),
                                }
                            };

                        }

                        restrictionsOnLandownership.Add(restrictionOnLandownership);
                    }
                }
            }

            return restrictionsOnLandownership.ToArray();
        }

        private ItemChoiceType GetGeometryType(GAGeoClass gaGeoClass)
        {
            switch (gaGeoClass.GeometryType)
            {
                case GAGeoClass.GeometryTypes.Point:
                    return ItemChoiceType.Point;
                case GAGeoClass.GeometryTypes.Polyline:
                    return ItemChoiceType.Line;
                case GAGeoClass.GeometryTypes.Polygon:
                    return ItemChoiceType.Surface;
                default:
                    throw new Exception("GetGeometryType: type of geometry not supported"); 
            }
        }

        private DocumentBase[] GetDocuments(OerebResult.OerebDef oerebDef, Config.Canton canton)
        {
            var documents = new List<DocumentBase>{};

            foreach (var dokument in oerebDef.Documents)
            {
                if (dokument.Item.Value["RechtsnormTyp"] == Setting.Legislation)
                {
                    documents.Add(GetDocument(new LegalProvisions(), dokument, canton));
                }
                else
                {
                    documents.Add(GetDocument(new Document(), dokument, canton));
                }
            }

            foreach (var article in oerebDef.Articles)
            {
                if (article.Type == Setting.Legislation)
                {
                    documents.Add(GetDocumentFromArticle(new LegalProvisions(), article, canton));
                }
                else
                {
                    documents.Add(GetDocumentFromArticle(new Document(), article, canton));
                }
            }

            return documents.ToArray();
        }

        private Document GetDocumentFromArticle(Document document, OerebResult.Article resultArticle, Config.Canton canton)
        {
            document.TextAtWeb = new LocalisedUri[] { new LocalisedUri() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultArticle.Url } };

            var articleDescription = string.IsNullOrEmpty(resultArticle.Number) ? "" : " ("+ resultArticle.Number + ")";

            document.Title = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = $"{resultArticle.Title }{articleDescription}" } };
            document.OfficialTitle = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = $"{resultArticle.OfficialTitle }{articleDescription}" } };
            document.Abbrevation = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultArticle.Abbrevation } };
            document.ResponsibleOffice = new Office()
            {
                Name = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultArticle.ResponsibleOffice } },
                OfficeAtWeb = new WebReference() { Value = resultArticle.ResponsibleOfficeUrl }
            };

            //federal

            if (resultArticle.SortIndex == 0)
            {
                document.OfficialNumber = string.IsNullOrEmpty(resultArticle.OfficialNumber) ? "" : "SR " + resultArticle.OfficialNumber;
            }

            //canton

            if (resultArticle.SortIndex == 1)
            {
                document.OfficialNumber = string.IsNullOrEmpty(resultArticle.OfficialNumber) ? "" : $"{canton.OfficialNumberPrefix ?? ""} {resultArticle.OfficialNumber}";
                document.Canton = EnumResolve.ParseEnum<CantonCode>(String.IsNullOrEmpty(resultArticle.Canton) ? canton.Shorname : resultArticle.Canton);
                document.CantonSpecified = true;
            }

            // community

            if (resultArticle.SortIndex == 2)
            {
                document.OfficialNumber = resultArticle.OfficialNumber;
                document.Municipality = resultArticle.Municipality;
            }

            document.Lawstatus = new Lawstatus() { Code = EnumResolve.ParseEnum<LawstatusCode>(resultArticle.LawStatus.ToString()), Text = new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultArticle.LawStatus.ToString() } };

            document.extensions = SetExtensions(new List<XmlElement>()
            {
                GetElement($"<Icon>{resultArticle.Icon}</Icon>")
            });

            return document;
        }

        private Document GetDocument(Document document, OerebResult.Document resultDocument, Config.Canton canton)
        {
            document.TextAtWeb = new LocalisedUri[] { new LocalisedUri() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultDocument.Url } };
            document.Title = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultDocument.Title } };
            document.OfficialTitle = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultDocument.OfficialTitle } };
            document.Abbrevation = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultDocument.Abbrevation } };
            document.ResponsibleOffice = new Office()
            {
                Name = new LocalisedText[] { new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultDocument.ResponsibleOffice } },
                OfficeAtWeb = new WebReference() { Value = resultDocument.ResponsibleOfficeUrl }
            };

            //federal

            if (resultDocument.SortIndex == 0)
            {
                document.OfficialNumber = string.IsNullOrEmpty(resultDocument.OfficialNumber) ? "" : "SR "  + resultDocument.OfficialNumber;
            }

            //canton

            if (resultDocument.SortIndex == 1)
            {
                document.OfficialNumber = string.IsNullOrEmpty(resultDocument.OfficialNumber) ? "" : $"{canton.OfficialNumberPrefix??""} {resultDocument.OfficialNumber}";
                document.Canton = EnumResolve.ParseEnum<CantonCode>(String.IsNullOrEmpty(resultDocument.Canton) ? canton.Shorname : resultDocument.Canton);
                document.CantonSpecified = true;
            }

            // community

            if (resultDocument.SortIndex == 2)
            {
                document.OfficialNumber = resultDocument.OfficialNumber;
                document.Municipality = resultDocument.Municipality;
            }

            document.Lawstatus = new Lawstatus() { Code = EnumResolve.ParseEnum<LawstatusCode>(resultDocument.LawStatus.ToString()), Text = new LocalisedText() { Language = LanguageCode.de, LanguageSpecified = true, Text = resultDocument.LawStatus.ToString() } };

            document.extensions = SetExtensions(new List<XmlElement>()
            {
                GetElement($"<Icon>{resultDocument.Icon}</Icon>")
            });

            return document;
        }

        private extensions SetExtensions(List<XmlElement> extensionElements)
        {
            var extensions = new extensions();
            extensions.Any = new XmlElement[] {};

            if (extensionElements != null)
            {
                extensions.Any = extensionElements.ToArray();
            }

            return extensions;
        }

        private Map GetMap(OerebResult.Feature feature,OerebResult.Topic topic)
        {
            var map = new Map();

            map.ReferenceWMS = feature.ConfigResource.MetadataUrl;
            map.LegendAtWeb = new WebReference() { Value = topic.ConfigTopic.LegendUrl };

            var imageSet = new ImageSet()
            {
                Extent = topic.Parcel.FrameExtent,
                Size = topic.Parcel.FrameSize,
                MimeType = Setting.DefaultMimeType,
                GeoDataset = (IGAGeoDataSet)feature.DataSet
            };

            RenderImages(new List<ImageSet>() {imageSet});

            if (imageSet.Image == null || !(imageSet.Image is Image))
            {
                throw new Exception($"returned image is not valid {feature.Guid}");
            }

            map.Image = ImageTasks.ImageToByteArray(imageSet.Image, Setting.DefaultMimeType);

            map.extensions = new extensions()
            {
                Any = new XmlElement[]
                {
                    GetElement($"<MapExtension><Seq>{feature.ConfigResource.Seq}</Seq><Transparency>{feature.ConfigResource.Transparency}</Transparency></MapExtension>"),
                }
            };

            return map;
        }

        private XElement GetGeometry(string geometry, int epsgCode)
        {
            var geometryGml = Server.Common.Geometry.GetGeometry(geometry, "gml", epsgCode);

            var rootElement = XElement.Parse($"<container xmlns:gml=\"http://www.opengis.net/gml/3.2\" >{geometryGml}</container>");
            var geometryElement = rootElement.Descendants().First();

            //TODO add id's to GML manually ?
            //geometryElement .Add(new XAttribute("id", $"{Guid.NewGuid()}.1"));
            AddId(geometryElement, _globalFeatureId);

            return geometryElement;
        }

        private int _globalFeatureId = 0;

        private void AddId(XElement element, int id)
        {
            XNamespace ns = "http://www.opengis.net/gml/3.2";

            var geometriesWithId = new List<string>() {"multisurface", "polygon", "linestring", "point",  "multipolygon", "multilinestring", "multipoint" }; //TODO add point and lines

            if (geometriesWithId.Contains(element.Name.LocalName.ToLower()) && !element.Attributes().Any(x=>x.Name.LocalName.ToLower() == "id"))
            {
                element.Add(new XAttribute(ns+"id", $"shape.{id}"));
            }

            foreach (var elementChild in element.Descendants())
            {
                _globalFeatureId++;
                AddId(elementChild, _globalFeatureId);
            }
        }

        private LocalisedText ConvertLocalisedText(Config.LocalisedText localisedText)
        {
            return new LocalisedText()
            {
                Language = EnumResolve.ParseEnum<LanguageCode>(localisedText.LanguageCode.ToString()),
                LanguageSpecified = true,
                Text = localisedText.Text
            };
        }

        private LocalisedMText ConvertLocalisedMText(Config.LocalisedText localisedText)
        {
            return new LocalisedMText()
            {
                Language = EnumResolve.ParseEnum<LanguageCode>(localisedText.LanguageCode.ToString()),
                LanguageSpecified = true,
                Text = localisedText.Text
            };
        }

        private ExclusionOfLiability[] GetExclusionOfLiabilities(List<Config.ExclusionOfLiability> exclusionOfLiabilities)
        {
            var results = new List<ExclusionOfLiability>();

            foreach (var exclusionOfLiability in exclusionOfLiabilities)
            {
                results.Add(new ExclusionOfLiability()
                {
                    Title = exclusionOfLiability.Title.Select(x => ConvertLocalisedText(x)).ToArray(),
                    Content = exclusionOfLiability.Content.Select(x => ConvertLocalisedMText(x)).ToArray()
                });
            }

            return results.ToArray();
        }

        private Glossary[] GetGlossary(List<Config.Glossary> glossaries)
        {
            var glossary = new List<Glossary>();

            foreach (var glossaryItem in glossaries)
            {
                glossary.Add(new Glossary()
                {
                    Title = glossaryItem.Title.Select(x => ConvertLocalisedText(x)).ToArray(),
                    Content = glossaryItem.Content.Select(x => ConvertLocalisedMText(x)).ToArray()
                });
            }

            return glossary.ToArray();
        }

        private Office GetOffice(Config.Office office)
        {
            var names = office.Name.Select(x => ConvertLocalisedText(x)).ToArray();

            var cadastralAuthority = new Office()
            {
                Name = names,
                City = office.City,
                Line1 = office.Line1,
                Line2 = office.Line2,
                Number = office.Number,
                OfficeAtWeb = new WebReference() { Value = office.OfficeAtWeb },
                PostalCode = office.PostalCode,
                Street = office.Street,
                UID = office.UID
            };

            return cadastralAuthority;
        }

        private void RenderImages(List<ImageSet> imageSets)
        {
            var tasks = new List<Task<ImageSet>>();

            foreach (var imageset in imageSets)
            {
                tasks.Add(Task.Factory.StartNew<ImageSet>(parameter =>
                {
                    var imageItem = (ImageSet)parameter;

                    if (_imageCache.ContainsKey(imageItem.ImageKey))
                    {
                        log.Info($"*** image {imageItem.ImageKey} comes from cache");
                        imageItem.Image = _imageCache[imageItem.ImageKey];
                    }
                    else
                    {
                        Image image;

                        if (_includeMap)
                        {
                            var gaGeoDataAdaptor = imageItem.GeoDataset.GetDataUtilities().Last() as IGAGeoDataAdaptor;

                            if (gaGeoDataAdaptor is ArcgisServerRestAdaptor || gaGeoDataAdaptor is OerebAdaptor || gaGeoDataAdaptor is ArcgisServerRestMapserviceAdaptor)
                            {
                                PropertyInfo property = gaGeoDataAdaptor.GetType().GetProperty("_dpi", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                property.SetValue(gaGeoDataAdaptor, _dpi, null);
                                log.Debug($"set image dpi to {_dpi}");
                            }

                            var response = gaGeoDataAdaptor.ExportImage(imageItem.MimeType, imageItem.Extent, imageItem.Size);

                            if (!response.Successful)
                            {
                                log.ErrorFormat("Export image for layer {0} was not successful", imageItem.GeoDataset.Guid);
                                throw new Exception($"error create image {imageItem.GeoDataset.Guid}");
                            }

                            image = response.Value as Image;

                            lock (_imageCache)
                            {
                                if (!_imageCache.ContainsKey(imageItem.ImageKey))
                                {
                                    _imageCache.Add(imageItem.ImageKey, image);
                                }
                            }
                        }
                        else
                        {
                            image = _imageEmptyPng;
                        }                     

                        imageItem.Image = image;
                    }

                    return imageItem;
                }, imageset, TaskCreationOptions.LongRunning));
            }

            Task.WaitAll(tasks.ToArray<Task>());
        }

        private List<LegendItem> GetLegendFromExtent(IEnumerable<IGADataSet> datasets, GAExtent extent)
        {
            var legendItems = new List<LegendItem>();
            var skeletons = new List<ResolverSkeleton>();

            foreach (var dataset in datasets)
            {
                var geoDataAdaptor = dataset.GetDataUtilities().Last() as IGAGeoDataAdaptor;

                if (geoDataAdaptor == null)
                {
                    continue;
                }

                var gaGeoClass = geoDataAdaptor.GaGeoClass;

                var filter = new GAFilter();
                filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, gaGeoClass.GeometryFieldName, AttributeOperator.Intersect, extent.GeometryWkt, typeof(string)));
                skeletons.Add(new ResolverSkeleton(dataset, filter));
            }

            var gaObjects = Resolver.ResolveGAObjects(skeletons);

            foreach (var gaObject in gaObjects)
            {
                var legend = GetLegendFromFeature(gaObject);
                
                if (string.IsNullOrEmpty(legend.Key))
                {
                    continue;
                }

                var dataset = Catalog.Catalog.GetDataSetFromClass(gaObject.GAClass.Guid);

                //todo more generic version please

                IGAGeoDataAdaptor oerebDataAdaptor = dataset.GetDataUtilities().Last() as OerebAdaptor;

                if (oerebDataAdaptor == null)
                {
                    oerebDataAdaptor = dataset.GetDataUtilities().Last() as Geocentrale.DataAdaptors.PostgreSqlAdaptor.OerebAdaptor;

                    if (oerebDataAdaptor == null)
                    {
                        continue;
                    }
                }

                legendItems.Add(new LegendItem()
                {
                    Label = Common.GetLabel(oerebDataAdaptor, gaObject), //TODO resolve murks
                    Image = legend.Image,
                    Key = legend.Key,
                    ObjectKey =  legend.ObjectKey,
                });
            }

            return legendItems;
        }

        public LegendItem GetLegendFromFeature(GAObject gaObject)
        {
            var legend = new LegendItem();

            if (gaObject == null)
            {
                log.Error("GetLegendFromFeature: gaObject is null");
                return legend;
            }

            var dataset = Catalog.Catalog.GetDataSetFromClass(gaObject.GAClass.Guid);

            if (dataset == null || !dataset.GetDataUtilities().Any())
            {
                log.Error("GetLegendFromFeature: dataset from gaObject is null");
                return legend;
            }

            var geoDataAdaptor = dataset.GetDataUtilities().Last() as IGAGeoDataAdaptor;

            if (geoDataAdaptor == null)
            {
                log.Error("GetLegendFromFeature: only geoDataAdaptor allowed");
                return legend;
            }

            var legendItem = geoDataAdaptor.GaGeoClass.Renderer.GetLegendItem(gaObject);

            if (legendItem.Value == null)
            {
                log.Warn("GetLegendFromFeature: legendItem is null");
                return legend;
            }

            var legendKey = $"{gaObject.DsGuid.ToString("N").Substring(0,8)}:L:{legendItem.Key}"; //todo remove murks

            if (!_LegendKeys.ContainsKey(legendKey))
            {
                _LegendKeys.Add(legendKey, $"{gaObject.DsGuid.ToString("N").Substring(0, 8)}:L:{_LegendKeys.Count}");
            }

            legend.Key = _LegendKeys[legendKey];
            legend.Label = legendItem.Value.Label;
            legend.Image = legendItem.Value.Image;
            legend.ObjectKey = $"{dataset.Guid}:F:{gaObject[gaObject.GAClass.ObjectIdFieldName]}";
            return legend;
        }

        private Dictionary<string,string> _LegendKeys { get; set; } = new Dictionary<string, string>();

        public class LegendItem
        {
            public string Key { get; set; }
            public string ObjectKey { get; set; }
            public string Label { get; set; }
            public Image Image { get; set; }
        }

        #endregion
    }
}