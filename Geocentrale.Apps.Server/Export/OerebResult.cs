using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Geocentrale.Apps.DataContracts;
using Geocentrale.DataAdaptors;
using Geocentrale.DataAdaptors.Contracts;

namespace Geocentrale.Apps.Server.Export
{
    /// <summary>
    /// helper to access the important nodes from the tree structure
    /// </summary>
    public class OerebResult
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Config _config { get; set; }

        public enum SectionType
        {
            ThemeWithoutData,
            NotConcernedTheme,
            ConcernedTheme
        }

        public enum Lawstatus
        {
            inForce,
            runningModifications
        }

        public enum GeometryType
        {
            Line,
            Point,
            Surface,
        }

        public static Dictionary<string, Enum> LawStatusMapper {get; set;} = new Dictionary<string, Enum>() { {"in kraft", Lawstatus.inForce}, { "ausser kraft", Lawstatus.runningModifications }, { "inkraft", Lawstatus.inForce }, { "ausserkraft", Lawstatus.runningModifications } };

        public List<Parcel> Parcels { get; set; }

        public static List<SectionType> SectionList { get; set; } = new List<SectionType>() { SectionType.ThemeWithoutData, SectionType.NotConcernedTheme, SectionType.ConcernedTheme };

        //public static double[] Scales { get; set; } = new double[16] { 500, 750, 1000, 1250, 1500, 2000, 2500, 3000, 5000, 10000, 20000, 30000, 40000, 50000, 100000, 1000000 };

        public static int Dpi { get; set; } = 300;

        public static double SheetWidth { get; set; } = 0.174; //unit meter, fixtive width on report

        public static double SheetHeight { get; set; } = 0.099; //unit meter, fixtive height on report

        public List<string> MunicipalityNames
        {
            get
            {
                return _config.Cantons.Select(x => x.RealEstate.FieldnameMunicipality).Distinct().ToList();
            }
        }

        public OerebResult(GAReport gAReport)
        {
            _config = Global.Config;

            Parcels = new List<Parcel>();

            if (gAReport == null || !gAReport.Result.Any())
            {
                log.Error("oereb reportobject has no parcels");
                return;
            }

            foreach (var parcel in gAReport.Result)
            {
                Parcels.Add(new Parcel(parcel, MunicipalityNames));
            }
        }

        public class Parcel
        {
            public double FrameScaleFactor { get; set; }
            public int FrameDpi => Dpi;

            public double MinScale { get; set; }
            public double ScaleStep { get; set; }

            public GAObject Value { get; set; }

            public Dictionary<SectionType,Section> Sections { get; set; } = SectionList.ToDictionary(x=> x, x=> new Section());

            public string Nummer => Value[_config.GetCantonFromMunicipality(Municipality).RealEstate.FieldnameNumber];
            public string Egrid => Value[_config.GetCantonFromMunicipality(Municipality).RealEstate.FieldnameEgrid];
            public int BfsNr => System.Convert.ToInt32(Value[_config.GetCantonFromMunicipality(Municipality).RealEstate.FieldnameBfsNumber].ToString()); //todo murks
            public int LandRegistryArea => System.Convert.ToInt32(Value[_config.GetCantonFromMunicipality(Municipality).RealEstate.FieldnameArea].ToString()); //todo murks

            private List<string> _municipalityNames { get; set; }

            public string Municipality
            {
                get
                {
                    foreach (var name in _municipalityNames)
                    {
                        try
                        {
                            return Value[name].ToString();
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    throw new Exception("no municipalityName matches");
                }
            }
           

            public Config.Canton ConfigProject => _config.GetCantonFromMunicipality(Municipality);

            public string Canton => _config.GetCantonFromMunicipality(Municipality).Shorname;

            public GAExtent Extent {
                get
                {
                    var extent = Server.Common.Geometry.GetGeometryExtent(WktGeometry);

                    var gaExtent = new GAExtent(extent[0], extent[1], extent[2], extent[3]);
                    gaExtent.Blow((gaExtent.Width * FrameScaleFactor) - gaExtent.Width, (gaExtent.Width * FrameScaleFactor) - gaExtent.Width);

                    return gaExtent;
                }
            }

            public double Scale
            {
                get
                {
                    double size = Math.Max(Extent.Width, Extent.Height);
                    double sheetSize = Math.Min(SheetWidth, SheetHeight);

                    var scale = Math.Ceiling(size/sheetSize/ScaleStep)*ScaleStep;

                    if (scale < MinScale)
                    {
                        scale = MinScale;
                    }

                    return scale;

                    //return Scales.First(scale => size < sheetSize * scale);
                }
            }

            public GAExtent FrameExtent
            {
                get
                {
                    var cx = Extent.Center.X;
                    var cy = Extent.Center.Y;
                    var widthOffset = SheetWidth / 2 * Scale;
                    var heighthOffset = SheetHeight / 2 * Scale;
                    return new GAExtent(cx- widthOffset, cy - heighthOffset, cx + widthOffset, cy + heighthOffset);
                }
            }

            public GASize FrameSize
            {
                get
                {
                    int width = Convert.ToInt32(SheetWidth * 1000 / Setting.Inch * Dpi);
                    int height = Convert.ToInt32(SheetHeight * 1000 / Setting.Inch * Dpi);
                    return new GASize(width, height);
                }
            }

            public IGAGeoDataAdaptor GeoDataAdaptor { get; set; }

            public string WktGeometry => Value[GeoDataAdaptor.GaGeoClass.GeometryFieldName];

            public Parcel(GATreeNode<GAObject> parcel, List<string> municipalityNames)
            {
                FrameScaleFactor = Convert.ToDouble(ConfigurationManager.AppSettings["scaleFactorFrame"] ?? "1.05");
                MinScale = Convert.ToDouble(ConfigurationManager.AppSettings["minScale"] ?? "250");
                ScaleStep = Convert.ToDouble(ConfigurationManager.AppSettings["scaleStep"] ?? "50");

                Value = parcel.Value;
                _municipalityNames = municipalityNames;

                GeoDataAdaptor = (IGAGeoDataAdaptor)Catalog.Catalog.GetDataSets(new List<Guid>() { Value.DsGuid}).First().GetDataUtilities().Last();

                var topics = parcel.Children;

                if (!topics.Any())
                {
                    log.Error($"oereb reportobject, parcel {parcel.Value[parcel.Value.GAClass.ObjectIdFieldName]} has no topics");
                    return;
                }

                foreach (var topic in topics.OrderBy(x => x.Value["Name"]))
                {
                    var section = topic.Value["Status"];
                    var sectionType = (SectionType)(int)section;

                    Sections[sectionType].Topics.Add(new Topic(topic, ConfigProject, this));
                }
            }
        }

        public class Section
        {
            public List<Topic> Topics { get; set; }

            public Section()
            {
                Topics = new List<Topic>();
            }
        }

        public class Topic
        {
            public GAObject Value { get; set; }

            public List<OerebDef> OerebDefs { get; set; } = new List<OerebDef>();

            public string Name => Value["Name"];

            private Config.Canton _configProject { get; set; }
            public Config.Canton ConfigProject => _configProject;

            private Parcel _parcel { get; set; }
            public Parcel Parcel => _parcel;

            public Config.Topic ConfigTopic
            {
                get
                {
                    return ConfigProject.Topics.FirstOrDefault(x => Value["Beschreibung"] == x.Code);
                }
            }

            public Topic(GATreeNode<GAObject> topic, Config.Canton configProject, Parcel parcel)
            {
                Value = topic.Value;
                _configProject = configProject;
                _parcel = parcel;

                var oerebDefs = topic.Children.Distinct().ToList(); //todo necessary ???

                if (!oerebDefs.Any())
                {
                    log.Info($"oereb reportobject, topic {topic.Value["Name"]} has no oerebdefs");
                    return;
                }

                OerebDefs.AddRange(oerebDefs.Select(x=> new OerebDef(x, ConfigProject, parcel)));
            }

            public T ParseEnum<T>(string value) where T : struct, IConvertible
            {
                if (!typeof(T).IsEnum)
                {
                    throw new ArgumentException("T must be an enumerated type");
                }

                T result;

                if (!Enum.TryParse<T>(value, out result))
                {
                    throw new Exception("value1 is not valid member of enumeration MyEnum");
                }

                return result;
            }
        }

        public class OerebDef
        {
            private Parcel _parcel { get; set; }
            public Parcel Parcel => _parcel;

            private Config.Canton _configProject { get; set; }
            public Config.Canton ConfigProject => _configProject;

            public GAObject Value { get; set; }

            public string Information => Value["Aussage"];

            public string OfficeName => Value["ZustStelle"];
            public string OfficeAtWeb => Value["ZustStelle.Url"];

            public Lawstatus LawStatus {
                get
                {
                    
                    if (LawStatusMapper.ContainsKey(Value["Rechtsstatus"].ToLower()))
                    {
                        return LawStatusMapper[Value["Rechtsstatus"].ToLower()];
                    }

                    return Lawstatus.runningModifications;
                }
            }

            public List<Feature> Features { get; set; } = new List<Feature>();
            public List<Document> VorschriftRechtsDokument { get; set; } = new List<Document>();
            public List<Article> VorschriftArtikel { get; set; } = new List<Article>();
            public List<Document> GesetzRechtsDokument { get; set; } = new List<Document>();
            public List<Article> GesetzArtikel { get; set; } = new List<Article>();

            public List<Document> Documents {
                get
                {
                    var documents = new List<Document>();
                    documents.AddRange(VorschriftRechtsDokument);
                    documents.AddRange(GesetzRechtsDokument);
                    return documents;
                }
            }

            public List<Article> Articles
            {
                get
                {
                    var articles = new List<Article>();
                    articles.AddRange(VorschriftArtikel);
                    articles.AddRange(GesetzArtikel);
                    return articles;
                }
            }

            public OerebDef(GATreeNode<GAObject> oerebDef, Config.Canton configProject, Parcel parcel)
            {
                Value = oerebDef.Value;
                _configProject = configProject;
                _parcel = parcel;

                var features = oerebDef.Children.Where(x => Catalog.Catalog.IsDataSet(x.Value.GAClass.Guid)).ToList();

                if (features.Any())
                {                    
                    Features.AddRange(features.Select(x => new Feature() {ConfigProject = ConfigProject,Item = x, DataSet = Catalog.Catalog.GetDataSets(new List<Guid>() { x.Value.DsGuid }).First() }));
                }

                var rechtsNorms = oerebDef.Children.Where(x => x.Value.GAClass.Guid == Setting.GuidRechtsnorm).Distinct().ToList();

                if (rechtsNorms.Any())
                {
                    VorschriftRechtsDokument.AddRange(rechtsNorms.Where(x => x.Value["RechtsnormTyp"] == Setting.Legislation).Select(x=> new Document()
                    {
                        Item = x
                    }));
                    GesetzRechtsDokument.AddRange(rechtsNorms.Where(x => x.Value["RechtsnormTyp"] == Setting.LegalBasis).Select(x => new Document()
                    {
                        Item = x
                    }));
                }

                var artikels = oerebDef.Children.Where(x => x.Value.GAClass.Guid == Setting.GuidArtikel).Distinct().ToList();

                if (artikels.Any())
                {
                    VorschriftArtikel.AddRange(artikels.Where(x => x.Value["Rechtsnorm.RechtsnormTyp"] == Setting.Legislation).Select(x => new Article()
                    {
                        Item = x
                    }));
                    GesetzArtikel.AddRange(artikels.Where(x => x.Value["Rechtsnorm.RechtsnormTyp"] == Setting.LegalBasis).Select(x => new Article()
                    {
                        Item = x
                    }));
                }
            }
        }

        public class Document
        {
            public GATreeNode<GAObject> Item { get; set; }

            public int Id => Item.Value["Id"];

            public int ParentId => Item.Value["Parent.Id"];

            public string Url => Item.Value["Url"] == null ? "" : Item.Value["Url"].ToString();

            public string Title => Item.Value["Titel"];

            public string OfficialTitle => Item.Value["OffiziellerTitel"];

            public string Abbrevation => Item.Value["Abkuerzung"];

            public string OfficialNumber => Item.Value["OffizielleNummer"];

            public string Canton => Item.Value["Kanton"];

            public string Municipality => Item.Value["Gemeinde"] == null ? "" : Item.Value["Gemeinde"].ToString();

            public string ResponsibleOffice => Item.Value["ZustStelle"];

            public string ResponsibleOfficeUrl => Item.Value["ZustStelle.Url"];

            public string Type => Item.Value["RechtsnormTyp"];

            public string Icon => Item.Value["Icon"];

            public string Level => Item.Value["Level"];

            public bool IsLive => Item.Value["IsLive"];

            public DateTime VisibilityDate => Item.Value["VisibilityDate"];

            public int SortIndex => Item.Value["SortIndex"];

            public Lawstatus LawStatus
            {
                get
                {

                    if (LawStatusMapper.ContainsKey(Item.Value["Rechtsstatus"].ToLower()))
                    {
                        return LawStatusMapper[Item.Value["Rechtsstatus"].ToLower()];
                    }

                    return Lawstatus.runningModifications;
                }
            }
        }

        public class Article : Document
        {
            //new properties

            public string Number => Item.Value["Nummer"];

            public string Text => Item.Value["Text"];

            public string RechtsnormId => Item.Value["Rechtsnorm.Id"]; //todo replace lexfind url with original url

            public string RechtsnormParentId => Item.Value["Rechtsnorm.Parent.Id"];

            //override (new)

            public new string Url => Item.Value["Rechtsnorm.Url"].ToString(); //todo replace lexfind url with original url

            public new string Title => Item.Value["Rechtsnorm.Titel"];

            public new string OfficialTitle => Item.Value["Rechtsnorm.OffiziellerTitel"];

            public new string Abbrevation => Item.Value["Rechtsnorm.Abkuerzung"];

            public new string OfficialNumber => Item.Value["Rechtsnorm.OffizielleNummer"];

            public new string Canton => Item.Value["Rechtsnorm.Kanton"];

            public new string Municipality => Item.Value["Rechtsnorm.Gemeinde"] == null ? "" : Item.Value["Rechtsnorm.Gemeinde"].ToString();

            public new string ResponsibleOffice => Item.Value["Rechtsnorm.ZustStelle"];

            public new string ResponsibleOfficeUrl => Item.Value["Rechtsnorm.ZustStelle.Url"];

            public new string Type => Item.Value["Rechtsnorm.RechtsnormTyp"];

            public new Lawstatus LawStatus
            {
                get
                {

                    if (LawStatusMapper.ContainsKey(Item.Value["Rechtsnorm.Rechtsstatus"].ToLower()))
                    {
                        return LawStatusMapper[Item.Value["Rechtsnorm.Rechtsstatus"].ToLower()];
                    }

                    return Lawstatus.runningModifications;
                }
            }

        }

        public class Feature
        {
            public Config.Canton ConfigProject { get; set; }

            public GATreeNode<GAObject> Item { get; set; }
            public IGADataSet DataSet { get; set; }

            public IGAGeoDataSet GeoDataset => DataSet as IGAGeoDataSet;

            public Guid Guid => DataSet.Guid;

            public GAGeoClass GaGeoClass => (GAGeoClass) DataSet.GetDataUtilities().Last().GaClass;

            public Config.Resource ConfigResource => ConfigProject.Resources.FirstOrDefault(x => x.Guid == Guid);

            public string Fid => $"{Guid}:F:{Item.Value[GaGeoClass.ObjectIdFieldName]}";

            public double Area {
                get
                {
                    if ( GeometryTypeFeature.ToString() == OerebResult.GeometryType.Surface.ToString() && Item.Value.Attributes.Any(x => x.AttributeSpec.Name == Setting.GeometryPartValueAttributeName))
                    {
                        return (double)Item.Value[Setting.GeometryPartValueAttributeName];
                    }

                    return 0;
                }
            }

            public double PartinPercent
            {
                get
                {
                    if (GeometryTypeFeature.ToString() == OerebResult.GeometryType.Surface.ToString() && Item.Value.Attributes.Any(x => x.AttributeSpec.Name == Setting.GeometryPartPercentageAttributeName))
                    {
                        return (double)Item.Value[Setting.GeometryPartPercentageAttributeName];
                    }

                    return 0;
                }
            }

            public Lawstatus LawStatus
            {
                get
                {
                    var resource = ConfigProject.Resources.FirstOrDefault(x => x.Guid == Guid);

                    if (resource != null && 
                        !String.IsNullOrEmpty(resource.AttributeLawStatus) && 
                        Item.Value.Attributes.Any(x=>x.AttributeSpec.Name == resource.AttributeLawStatus) && 
                        LawStatusMapper.ContainsKey(Item.Value[resource.AttributeLawStatus].ToLower()))
                    {
                        return LawStatusMapper[Item.Value[resource.AttributeLawStatus].ToLower()];
                    }

                    return Lawstatus.runningModifications;
                }
            }

            public string MetadataUrl
            {
                get
                {
                    var resource = ConfigProject.Resources.FirstOrDefault(x => x.Guid == Guid);

                    if (resource != null && !String.IsNullOrEmpty(resource.MetadataUrl))
                    {
                        return resource.MetadataUrl;
                    }

                    return "";
                }
            }

            public Enum GeometryTypeFeature
            {
                get
                {
                    var dataAdaptor = (GAGeoDataAdaptor)DataSet.GetDataUtilities().Last();

                    switch (dataAdaptor.GaGeoClass.GeometryType)
                    {
                        case GAGeoClass.GeometryTypes.Point:
                            return OerebResult.GeometryType.Point;
                        case GAGeoClass.GeometryTypes.Polyline:
                            return OerebResult.GeometryType.Line;
                        case GAGeoClass.GeometryTypes.Polygon:
                            return OerebResult.GeometryType.Surface;
                        default:
                            throw new Exception("unknown geometry");
                    }
                }
            }

            public string WktGeometry {
                get
                {
                    var dataAdaptor = (GAGeoDataAdaptor) DataSet.GetDataUtilities().Last();
                    var gaGeoClass = dataAdaptor.GaGeoClass;
                    return Item.Value[gaGeoClass.GeometryFieldName];
                }
            }
        }
    }
}