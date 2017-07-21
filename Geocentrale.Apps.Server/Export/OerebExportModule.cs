using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Aspose.Words;
using Aspose.Words.Saving;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server.Helper;
using Geocentrale.Common;
using Geocentrale.DataAdaptors;
using Geocentrale.DataAdaptors.ArcgisServerRestAdaptor;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oereb.Report;
using Oereb.Service.DataContracts.Model;

namespace Geocentrale.Apps.Server.Export
{
    [ModuleGuid("78833cf2-2d5e-42f7-b38e-2099c266e4de")]
    public class OerebExportModule : IExportModule
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Guid _oerebGuid = Guid.Parse("0dec20e8-82cb-47a5-98c2-ac515fad10b9");
        private static readonly Guid _rechtsnormGuid = Guid.Parse("e2826482-300b-4437-b525-4055b7304e56");
        private static readonly Guid _artikelGuid = Guid.Parse("1c715771-0f6f-4a5a-961a-92c93cb374ad");
        private static readonly Guid _oerebkThemaGuid = Guid.Parse("8c7bcb11-3791-479f-b8a7-f227f1751ff9");
        private static readonly Guid _zustStelleGuid = Guid.Parse(" fd939ac3-94e5-4410-a789-223bf4972d78");

        public const string EmptyPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAZdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuMTCtCgrAAAAADUlEQVQYV2P4//8/AwAI/AL+iF8G4AAAAABJRU5ErkJggg==";
        public const string EmptyPng = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsIAAA7CARUoSoAAAAAadEVYdFNvZnR3YXJlAFBhaW50Lk5FVCB2My41LjExR/NCNwAAAA1JREFUGFdj+P//PwMACPwC/ohfBuAAAAAASUVORK5CYII=";

        public bool LogEnabled
        {
            get
            {
                var value = ConfigurationManager.AppSettings["LogEnabled"];

                if (!string.IsNullOrEmpty(value) && value.ToLower() == "true")
                {
                    return true;
                }

                return false;
            }
        }

        public string ToJson(MergerRequest mergerRequest, GAReport gAReport)
        {
            var xmlContent = (new Xml.Converter(mergerRequest, gAReport)).Process();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlContent);

            var jsonContent = JsonConvert.SerializeXmlNode(xmlDocument);

            if (LogEnabled)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), $"Extract_{Guid.NewGuid()}.json"), jsonContent);
            }

            return jsonContent;
        }

        public string ToXml(MergerRequest mergerRequest, GAReport gAReport)
        {
            return (new Xml.Converter(mergerRequest, gAReport)).Process();
        }

        public byte[] ToPdf(MergerRequest mergerRequest, GAReport gAReport)
        {
            dynamic adminMode;
            mergerRequest.ModuleParameters.TryGetValue("adminMode", out adminMode);

            AssignVisibility(gAReport, adminMode);

            var xmlContent = (new Xml.Converter(mergerRequest, gAReport)).Process();

            if (LogEnabled)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), String.Format("Extract_{0}.xml", Guid.NewGuid())), xmlContent, Encoding.UTF8);
            }

            return ReportBuilder.GeneratePdf(xmlContent.TrimStart(), mergerRequest.ReportComplete, mergerRequest.ReportAppendixesAttached);
        }

        public byte[] ToPdfA1(MergerRequest mergerRequest, GAReport gAReport)
        {
            dynamic adminMode;
            mergerRequest.ModuleParameters.TryGetValue("adminMode", out adminMode);

            AssignVisibility(gAReport, adminMode);

            var xmlContent = (new Xml.Converter(mergerRequest, gAReport)).Process();

            if (LogEnabled)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), $"Extract_{Guid.NewGuid()}.xml" ), xmlContent, Encoding.UTF8);
            }

            var docContent = ReportBuilder.Generate(xmlContent.TrimStart(), "docx", mergerRequest.ReportComplete, false); //attached = true is not possible because we use the word format to generate pdfA1a

            if (LogEnabled)
            {
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx"), docContent);
            }

            Stream streamDoc = new MemoryStream(docContent);

            var license = new License();
            license.SetLicense("License/Aspose.Words.lic");

            var doc = new Document(streamDoc);

            var options = new PdfSaveOptions();

            options.SaveFormat = SaveFormat.Pdf;
            options.TextCompression = PdfTextCompression.Flate;
            options.JpegQuality = 60;
            options.Compliance = PdfCompliance.PdfA1a;

            if (LogEnabled)
            {
                doc.Save(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_A1.pdf"), options);
            }

            var streamPdf = new MemoryStream();
            doc.Save(streamPdf, options);
            streamPdf.Position = 0;

            return StreamTasks.ByteArrayFromStream(streamPdf);
        }

        public GAStatus ToHtml(MergerRequest mergerRequest, GAReport gAReport)
        {
            dynamic adminMode;

            mergerRequest.ModuleParameters.TryGetValue("adminMode", out adminMode);
            adminMode = adminMode as bool? ?? false;

            AssignVisibility(gAReport, adminMode);

            GATreeNode<GAObject> parcel = gAReport.Result.FirstOrDefault();

            if (parcel == null)
            {
                return new GAStatus(false,null,GAStatus.LogLevel.Error,"no parcel found for html export");
            }

            string pathImage = PathTasks.GetBinDirectory().Parent.FullName + "/Image/oereb";

            if (!Directory.Exists(pathImage))
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, "directory oereb not found, html export");                
            }
            //TODO: configuration candidate
            var sections = new Dictionary<int, string> { { 2, "Betroffene ÖREB Themen" }, { 1, "Nicht betroffene ÖREB Themen" }, { 0, "Nicht vorhandene ÖREB Themen" } };
            var oerebTopics = parcel.Children;

            if (!oerebTopics.Any())
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, "no oereb topics found");
            }

            var status = new GAStatus(true);
            // TODO: handle with template file and replacements to make it configurables
            string gAReportHtml = "<div class='oerebResult'>";

            var selection = mergerRequest.Selections.FirstOrDefault();

            gAReportHtml += "<div id=\'oerebResultHeader\'>";

            if (selection != null && !mergerRequest.QueryWithPseudoObject)
            {
                gAReportHtml += string.Format("<input id=\"OerebPdfButton\" type=\"button\" onclick=\"GetOerebReport('pdf','{0}','{1}');this.disabled=true;\" value=\"PDF anzeigen\" style=\"margin: 0px;margin-top:5px;\"></input>", mergerRequest.ProcessHash, gAReport.CreationId);

                gAReportHtml += string.Format("<p style='margin:0px;margin-bottom:2px;margin-top:5px;'>Ausgewählte Parzelle: {0}, {1} ({2} m2)</p>", 
                    parcel.Value.Attributes.First(x=>x.AttributeSpec.Name.ToLower() == "nummer").Value, 
                    parcel.Value.Attributes.First(x => x.AttributeSpec.Name.ToLower() == "gemeinde").Value, 
                    parcel.Value.Attributes.First(x => x.AttributeSpec.Name.ToLower() == "flaechenmass").Value);
            }
            else
            {
                gAReportHtml += string.Format("<p>Freier Perimeter</p>");
            }

            gAReportHtml += String.Format("<div id='oerebResultHeaderButtons'></div>"); //new

            gAReportHtml += string.Format("</div>");

            gAReportHtml += string.Format("<div id='oerebResultArea'>");
            
            foreach (var section in sections)
            {
                int currentSection = section.Key;
                gAReportHtml += string.Format("<p class='oerebResultSection'>{0}</p>",section.Value);

                foreach (var oerebThema in oerebTopics.Where(x => x.Value["Status"] == currentSection).OrderBy(x => x.Value["Name"]))
                {
                    //topic
                    gAReportHtml += string.Format("<p class='oerebResultThema{0}'><img src='{1}'/>{2}</p>",
                        currentSection,
                        ImageTasks.GetBase64StringFromImage(Path.Combine(pathImage, string.Format("{0}.png", oerebThema.Value["Name"]))),
                        oerebThema.Value["Name"]
                        );

                    if (oerebThema.Children.Count == 0)
                    {
                        continue;
                    }

                    var oerebDefs = oerebThema.Children.Distinct(); //Where(x => GetVisibility(x.Value, adminMode) == true)

                    foreach (var oerebDef in oerebDefs)
                    {
                        if (!oerebDef.Children.Any())
                        {
                            continue;
                        }

                        foreach (var feature in oerebDef.Children.Select(x=> x.Value))
                        {
                            if (feature == null)
                            {
                                continue;
                            }

                            if (!Catalog.Catalog.IsDataSet(feature.GAClass.Guid))
                            {
                                continue;
                            }

                            var dataset = Catalog.Catalog.GetDataSetFromClass(feature.GAClass.Guid);

                            if (dataset == null)
                            {
                                continue;
                            }

                            GAGeoDataAdaptor oerebDataAdaptor = dataset.GetDataUtilities().Last() as OerebAdaptor;

                            if (oerebDataAdaptor == null)
                            {
                                oerebDataAdaptor = dataset.GetDataUtilities().Last() as Geocentrale.DataAdaptors.PostgreSqlAdaptor.OerebAdaptor;

                                if (oerebDataAdaptor == null)
                                {
                                    continue;
                                }
                            }

                            var gaGeoClass = oerebDataAdaptor.GaGeoClass;
                            var geometry = string.Empty;

                            if (feature[gaGeoClass.GeometryFieldName] != null && !string.IsNullOrEmpty(feature[gaGeoClass.GeometryFieldName]))
                            {
                                geometry = Server.Common.Geometry.GetGeometry(feature[gaGeoClass.GeometryFieldName].ToString(), "geojson");
                            }

                            var legend = Common.GetLegendFromFeature(feature);

                            if (string.IsNullOrEmpty(legend.Value))
                            {
                                legend = new KeyValuePair<string, string>(string.Empty, EmptyPng);
                            }

                            string legendLabel = Common.GetLabel(oerebDataAdaptor, feature);
                            
                            string title = string.Empty;

                            if (feature.GAClass.AttributeSpecs.Any(x => x.Name == Server.Common.Geometry.GeometryPartGeometryTypeAttributeName) && feature[Server.Common.Geometry.GeometryPartGeometryTypeAttributeName] == "area")
                            {
                                title = string.Format("title='Überlappungsfläche {0} m2, Anteil {1} %' ", Math.Round(feature[Server.Common.Geometry.GeometryPartValueAttributeName], 0), Math.Round(feature[Server.Common.Geometry.GeometryPartPercentageAttributeName] * 100, 1));
                            }
                            if (feature.GAClass.AttributeSpecs.Any(x => x.Name == Server.Common.Geometry.GeometryPartGeometryTypeAttributeName) && feature[Server.Common.Geometry.GeometryPartGeometryTypeAttributeName] == "perimeter")
                            {
                                title = string.Format("title='Länge innerhalb Auswertungsperimeter {0} m' ", Math.Round(feature[Server.Common.Geometry.GeometryPartValueAttributeName], 0));
                            }

                            //feature
                            gAReportHtml += string.Format("<p class='oerebResultFeature' {0}><img src='{5}' height='25' style='margin-right:5px;' />{4} <img src=\"{1}\" onmousedown=\"HoverFeature('{2}',false)\" onmouseup=\"ClearHover();\" title=\"Geometrie hervorheben\"/></p>", 
                                title,
                                ImageTasks.GetBase64StringFromImage(Path.Combine(pathImage, "highlight.png")), 
                                ExceptBlanks(System.Web.HttpUtility.HtmlEncode(geometry)), 
                                feature.GAClass.NiceName,
                                legendLabel,
                                legend.Value
                                );

                            if (feature.GAClass.AttributeSpecs.Any(x => x.Name == Server.Common.Geometry.NiceRuleExpressionAttributeName) && adminMode)
                            {
                                //rule
                                gAReportHtml += string.Format("<p class='oerebResultRule'>Regel: {0}</p>", feature[Server.Common.Geometry.NiceRuleExpressionAttributeName]); 
                            }

                        }

                        var rechtsNorms = oerebDef.Children.Where(x => x.Value.GAClass.Guid == _rechtsnormGuid).Distinct();
                        var artikels = oerebDef.Children.Where(x => x.Value.GAClass.Guid == _artikelGuid).Distinct();

                        var vorschriftRechtsDokument = rechtsNorms.Where(x => x.Value["RechtsnormTyp"] == "Rechtsvorschrift");
                        var vorschriftArtikel = artikels.Where(x => x.Value["Rechtsnorm.RechtsnormTyp"] == "Rechtsvorschrift");

                        if (vorschriftRechtsDokument.Any() || vorschriftArtikel.Any())
                        {
                            gAReportHtml += "<div class='oerebResultRechtsvorschrift' style='display:inline'>";
                            gAReportHtml += "<p class='oerebResultPartTitle'>Rechtsvorschriften</p>";
                            gAReportHtml = AddLaws(gAReportHtml, vorschriftRechtsDokument, vorschriftArtikel);
                            gAReportHtml += "</div>";
                        }

                        var gesetzRechtsDokument = rechtsNorms.Where(x => x.Value["RechtsnormTyp"] == "Gesetzliche Grundlage");
                        var gesetzArtikel = artikels.Where(x => x.Value["Rechtsnorm.RechtsnormTyp"] == "Gesetzliche Grundlage");

                        if (gesetzRechtsDokument.Any() || gesetzArtikel.Any())
                        {
                            gAReportHtml += "<div class='oerebResultGesetz' style='display:none'>";
                            gAReportHtml += "<p class='oerebResultPartTitle'>Gesetzliche Grundlagen</p>";
                            gAReportHtml = AddLaws(gAReportHtml, gesetzRechtsDokument, gesetzArtikel);
                            gAReportHtml += "</div>";
                        }

                        gAReportHtml += "<div class='oerebResultFeatureDetail' style='display:none'>";
                        gAReportHtml += "<p class='oerebResultPartTitle'>Details</p>";

                        foreach (var feature in oerebDef.Children.Where(x => Catalog.Catalog.IsDataSet(x.Value.GAClass.Guid)))
                        {
                            if (feature == null)
                            {
                                continue;
                            }

                            var dataset = Catalog.Catalog.GetDataSetFromClass(feature.Value.GAClass.Guid);

                            gAReportHtml += "<p class='oerebResultRechtsNorm'><table class='oerebResultDetailTable' width='240px'>";

                            if (dataset.Fields.Any())
                            {
                                foreach (var attributeSpec in feature.Value.GAClass.AttributeSpecs.Where(x=> dataset.Fields.Select(y=> y.NameRef).Contains(x.Name)))
                                {
                                    if (feature.Value[attributeSpec.Name] == null)
                                    {
                                        continue;
                                    }

                                    var field = dataset.Fields.First(x => x.NameRef == attributeSpec.Name);
                                    var value = feature.Value[attributeSpec.Name];

                                    //TODO this mapping should run in the fields class and be configurable

                                    if (value is DateTime)
                                    {
                                        value = String.Format("{0:dd.MM.yyyy}", value);
                                    }
                                    else if (value is String && value.ToLower() == "true")
                                    {
                                        value = "Ja";
                                    }
                                    else if (value is String && value.ToLower() == "false")
                                    {
                                        value = "Nein";
                                    }

                                    gAReportHtml += String.Format("<tr><td>{0}</td><td>{1}</td></tr>", field.Label.First().Value, value);
                                }
                            }
                            else
                            {
                                //default, if there is no fields definition

                                var ignoredAttributes = new List<string>() { "objectid", "id", "fid", "shape", "typ_darstellungscode" };
                                var ignoredAttributeParts = new List<string>() { "gageometry", "garule", "ganicerule" };

                                foreach (var attributeSpec in feature.Value.GAClass.AttributeSpecs)
                                {
                                    if (ignoredAttributes.Contains(attributeSpec.Name.ToLower()))
                                    {
                                        continue;
                                    }

                                    if (ignoredAttributeParts.Select(x => attributeSpec.Name.ToLower().StartsWith(x)).Any(x => x == true))
                                    {
                                        continue;
                                    }

                                    gAReportHtml += String.Format("<tr><td>{0}</td><td>{1}</td></tr>", attributeSpec.Name, feature.Value[attributeSpec.Name]);
                                }
                            }

                            gAReportHtml += "</table></p>";
                        }

                        gAReportHtml += "</div>";
                    }

                }
            }

            gAReportHtml += "</div></div>";

            status.Value = gAReportHtml;
            status.Add(new GAStatus(true, null, GAStatus.LogLevel.Debug, "html export successfully created"));
            return status;
        }

        #region private

        private string AddLaws(string gAReportHtml, IEnumerable<GATreeNode<GAObject>> rechtsNorms, IEnumerable<GATreeNode<GAObject>> artikels)
        {
            string pathImage = PathTasks.GetBinDirectory().Parent.FullName + "/Image/oereb"; //HttpContext.Current.Server.MapPath("~/Image/oereb");

            var allRechtsNorms = rechtsNorms.Select(x => new { Id = x.Value["Id"], SortIndex = x.Value["SortIndex"], Number = x.Value["OffizielleNummer"] }).ToList();
            allRechtsNorms.AddRange(artikels.ToList().Select(x => new { Id = x.Value["Rechtsnorm.Id"], SortIndex = x.Value["SortIndex"], Number = x.Value["Rechtsnorm.OffizielleNummer"] }));

            foreach (var rechtsNormId in allRechtsNorms.Distinct().OrderBy(x => x.SortIndex).ThenBy(x => x.Number).Select(x => x.Id))
            {
                var currentId = rechtsNormId;

                string rechtsNormHtml = "<p class='oerebResultRechtsNorm'><img src='{0}' style='margin-right:5px;'/>{1}{2} {3}{4}</p>";

                var rechtsNormFromArtikel = artikels.FirstOrDefault(x => x.Value["Rechtsnorm.Id"] == currentId);

                if (rechtsNormFromArtikel != null)
                {
                    //rechtsnorm
                    gAReportHtml += String.Format(rechtsNormHtml,
                       ImageTasks.GetHtmlImageFromBase64(rechtsNormFromArtikel.Value["Icon"], ImageFormat.Png),
                       rechtsNormFromArtikel.Value["Rechtsnorm.Url"] != null ? String.Format("<a href='{0}' target='_new' >", rechtsNormFromArtikel.Value["Rechtsnorm.Url"]) : String.Empty,
                       rechtsNormFromArtikel.Value["Rechtsnorm.OffizielleNummer"],
                       rechtsNormFromArtikel.Value["Rechtsnorm.Titel"],
                       rechtsNormFromArtikel.Value["Rechtsnorm.Url"] != null ? "</a>" : String.Empty);


                    foreach (var artikel in artikels.Where(x => x.Value["Rechtsnorm.Id"] == currentId))
                    {
                        //artikel
                        gAReportHtml += String.Format("<p class='oerebResultArtikel'><img src='{0}'/>{1} {2}</p>",
                            ImageTasks.GetBase64StringFromImage(Path.Combine(pathImage, "artikel.png")),
                            artikel.Value["Nummer"],
                            artikel.Value["Text"]);
                    }

                    continue;
                }

                var rechtsNorm = rechtsNorms.FirstOrDefault(x => x.Value["Id"] == rechtsNormId);

                if (rechtsNorm != null)
                {
                    //rechtsnorm
                    gAReportHtml += String.Format(rechtsNormHtml,
                       ImageTasks.GetHtmlImageFromBase64(rechtsNorm.Value["Icon"], ImageFormat.Png),
                       rechtsNorm.Value["Url"] != null ? String.Format("<a href='{0}' target='_new' >", rechtsNorm.Value["Url"]) : String.Empty,
                       rechtsNorm.Value["OffizielleNummer"],
                       rechtsNorm.Value["Titel"],
                       rechtsNorm.Value["Url"] != null ? "</a>" : String.Empty);
                }

            }

            return gAReportHtml;
        }

        private class NoRefsContractResolver : DefaultContractResolver
        {
            public override JsonContract ResolveContract(Type type)
            {
                var contract = base.ResolveContract(type);
                contract.IsReference = false;
                return contract;
            }
        }

        private bool GetVisibility(GAObject gAObject, bool adminMode = false) // check for artikel, rechtsnorm und oerebdef
        {
            if (adminMode)
            {
                return true;
            }
            DateTime visibilityDate = gAObject["VisibilityDate"];
            bool isLive = gAObject["IsLive"]; //true;
            return isLive && (visibilityDate <= DateTime.Now);
        }

        private string ExceptBlanks(string str)
        {
            // TODO: not sure if this is fastest algorithm?
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\r':
                    case '\n':
                    case '\t':
                        //case ' ': //Spaces are ok
                        continue;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private void AssignVisibility(GAReport gAReport,bool adminMode)
        {
            var parcel = gAReport.Result.FirstOrDefault();
            var topics = parcel.Children;

            foreach (var topic in topics)
            {
                topic.Children.RemoveAll(x => !GetVisibility(x.Value, adminMode));

                var oerebDefs = topic.Children;

                foreach (var oerebDef in oerebDefs)
                {
                    var laws = oerebDef.Children.Where(x => x.Value != null && x.Value.GAClass != null && (x.Value.GAClass.Guid == _rechtsnormGuid || x.Value.GAClass.Guid == _artikelGuid)).ToList();
                    laws.RemoveAll(x => !GetVisibility(x.Value, adminMode));
                }
            }
            
        }

        #endregion
    }
}