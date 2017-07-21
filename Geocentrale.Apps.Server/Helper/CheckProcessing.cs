using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using Geocentrale.Apps.DataContracts.Filter;
using Geocentrale.Apps.Db.RuleEngine;
using Geocentrale.Apps.Server.Modules;
using Geocentrale.Common;
using Geocentrale.Filter;
using System.IO;
using DocumentFormat.OpenXml.Bibliography;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server.Export;
using Oereb.Service.DataContracts.Model.v04;
using System.Configuration;

namespace Geocentrale.Apps.Server.Helper
{
    public class CheckProcessing
    {
        public static GAStatus Evaluate(Dictionary<string, string> parcelsToCheck, MergerRequest mergerRequest, List<string> formats, string field, string baseUrl = "")
        {
            var htmlContent = string.Empty;
            var htmlContentConclusion = $"<a id='top'><h1>Total of check {parcelsToCheck.Count}</h1></a>";
            var oerebmodule = new OerebModule();

            var stopwatch = new Stopwatch();
            var calculations = new List<Calculation>();
            var linkDocument = $"{baseUrl}/oereb/getEFile/?file=";

            foreach (var parcel in parcelsToCheck)
            {
                var guid = Guid.NewGuid();
                var now = DateTime.Now.ToString("yyyyMMddHHmmss");
                var directory = $"Oereb_Check_Processing_{parcel.Key}_{now}_{guid}";
                var outputpath = Path.Combine(Path.GetTempPath(), directory);
                Directory.CreateDirectory(outputpath);

                var calculation = new Calculation
                {
                    Key = parcel.Key,
                    Comment = parcel.Value,
                    Guid = guid
                };

                var filter = new GAFilter();
                filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, field, AttributeOperator.Like, parcel.Key, typeof(string)));

                mergerRequest.InputObject = null;
                mergerRequest.InvolvedObjects = new List<GAObject>();
                mergerRequest.Selections.First().gaFilter = filter;
                mergerRequest.Selections.First().gaObject = null;

                stopwatch.Reset();
                stopwatch.Start();

                var resultProcessing = oerebmodule.Process(mergerRequest);

                calculation.RenderingFormats.Add("Processing", new RenderingFormat(linkDocument) {TimeSpan = stopwatch.Elapsed });
                stopwatch.Stop();

                var metadata = Metadata.CreateSummaryFromReport(resultProcessing, false, false, false);

                var metadataString = Helper.TasksExpandoObject.ConvertToJson(metadata);

                if (!string.IsNullOrEmpty(metadataString))
                {
                    FileTasks.WriteString(Path.Combine(outputpath, $"{parcel.Key}_metadata.json"), metadataString.Trim(), true);
                }

                var metadataHtmlString = Helper.TasksExpandoObject.ConvertToHtml(metadata, true, false, false);

                if (!string.IsNullOrEmpty(metadataHtmlString))
                {
                    FileTasks.WriteString(Path.Combine(outputpath, $"{parcel.Key}_metadata.html"), $"<html><body>{metadataHtmlString}</body></html>", true);
                }

                var binPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                if (binPath.StartsWith(@"file:\"))
                {
                    binPath = binPath.Replace(@"file:\", "");
                }

                var pathToCompare = Path.Combine(new DirectoryInfo(binPath).Parent.FullName, "Checkfiles");
                var filenameToCompare = $"{parcel.Key}_metadata.json";
                var fileToCompare = Path.Combine(pathToCompare, filenameToCompare);
                var resultCompare = string.Empty;
                var contentValid = File.Exists(fileToCompare) ? File.ReadAllText(fileToCompare, Encoding.UTF8) : "";

                if (String.IsNullOrEmpty(contentValid))
                {
                    resultCompare = "<p style='background-color:#ffff55'>there is no resultfile to compare</p>";
                }
                else
                {
                    var contentCurrent = Helper.TasksExpandoObject.ConvertToJson(metadata);

                    var statusContentCompare = contentValid.Trim() == contentCurrent.Trim();

                    calculation.CompareProcessingSuccessful = statusContentCompare;

                    if (statusContentCompare)
                    {
                        resultCompare = "<p style='background-color:#ccffcc'>the result is VALID</p>";
                    }
                    else
                    {
                        resultCompare = "<p style='background-color:#ffcccc'>the result is INVALID</p>";
                        var filepath = Path.Combine(outputpath, filenameToCompare);
                        FileTasks.WriteString(filepath.Replace("_metadata.json", "_metadata_valid.json"), contentValid, true);
                        FileTasks.WriteString(filepath.Replace("_metadata.json", "_metadata_invalid.json"), contentCurrent, true);
                    }
                }

                //copy flatten schema to directory

                var schemaPath = new DirectoryInfo(Path.Combine(pathToCompare, "schema_0.4"));
                var resultXmlValidation = string.Empty;

                foreach (var file in schemaPath.GetFiles())
                {
                    file.CopyTo(Path.Combine(outputpath,file.Name));
                }

                //Export

                var exportModule = new OerebExportModule();

                foreach (var format in formats)
                {
                    var filepath = string.Empty;

                    try
                    {
                        stopwatch.Reset();
                        stopwatch.Start();

                        mergerRequest.ReportComplete = false;
                        mergerRequest.ReportAppendixesAttached = false;

                        switch (format)
                        {
                            case "xml":

                                var resultExportXml = exportModule.ToXml(mergerRequest, resultProcessing);
                                filepath = Path.Combine(directory, $"{parcel.Key}_.xml");
                                var xmlPath = Path.Combine(Path.GetTempPath(), filepath);

                                File.WriteAllText(xmlPath, resultExportXml, Encoding.UTF8);

                                var status = Xml<Extract>.Validate(xmlPath, "ExtractData.xsd");

                                calculation.XmlValidation = status.Successful;

                                if (status.Successful)
                                {
                                    resultXmlValidation =
                                        "<p style='background-color:#ccffcc'>Xml is VALID, schema version 0.4</p>";
                                }
                                else
                                {
                                    resultXmlValidation =
                                        "<p style='background-color:#ffcccc'>Xml is NOT VALID, schema version 0.4</p>";
                                }

                                break;

                            case "json":

                                filepath = Path.Combine(directory, $"{parcel.Key}_.json");
                                var jsonPath = Path.Combine(Path.GetTempPath(), filepath);

                                var resultExportJson = exportModule.ToJson(mergerRequest, resultProcessing);
                                File.WriteAllText(jsonPath, resultExportJson);

                                break;

                            case "html":

                                filepath = Path.Combine(directory, $"{parcel.Key}_.html");
                                var htmlPath = Path.Combine(Path.GetTempPath(), filepath);

                                var resultExportHtml =
                                    (string) exportModule.ToHtml(mergerRequest, resultProcessing).Value;
                                File.WriteAllText(htmlPath, resultExportHtml);

                                break;

                            case "pdf":

                                filepath = Path.Combine(directory, $"{parcel.Key}_red.pdf");
                                var pdfRedPath = Path.Combine(Path.GetTempPath(), filepath);

                                var resultExportPdf = exportModule.ToPdf(mergerRequest, resultProcessing);
                                File.WriteAllBytes(pdfRedPath, resultExportPdf);

                                break;

                            case "pdfAttach":

                                filepath = Path.Combine(directory, $"{parcel.Key}_comA.pdf");
                                var pdfAttPath = Path.Combine(Path.GetTempPath(), filepath);

                                mergerRequest.ReportComplete = true;
                                mergerRequest.ReportAppendixesAttached = true;

                                var resultExportPdfA = exportModule.ToPdf(mergerRequest, resultProcessing);
                                File.WriteAllBytes(pdfAttPath, resultExportPdfA);

                                break;

                            case "pdfEmbedd":

                                filepath = Path.Combine(directory, $"{parcel.Key}_comE.pdf");
                                var pdfEmPath = Path.Combine(Path.GetTempPath(), filepath);

                                mergerRequest.ReportComplete = true;
                                mergerRequest.ReportAppendixesAttached = false;

                                var resultExportPdfE = exportModule.ToPdf(mergerRequest, resultProcessing);
                                File.WriteAllBytes(pdfEmPath, resultExportPdfE);

                                break;

                            case "pdfa1":

                                filepath = Path.Combine(directory, $"{parcel.Key}_A1.pdf");
                                var pdfA1Path = Path.Combine(Path.GetTempPath(), filepath);

                                var resultExportPdfA1 = exportModule.ToPdfA1(mergerRequest, resultProcessing);
                                File.WriteAllBytes(pdfA1Path, resultExportPdfA1);

                                break;

                            default:

                                break;
                        }

                        calculation.RenderingFormats.Add(format,new RenderingFormat(linkDocument) {TimeSpan = stopwatch.Elapsed, FilePath = filepath});                        
                    }
                    catch (Exception ex)
                    {
                        calculation.RenderingFormats.Add(format, new RenderingFormat(linkDocument) {TimeSpan = stopwatch.Elapsed, FilePath = "", Error = ex.Message});
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                }

                //output html

                htmlContent += $"<a id='{guid}'><h2>Processing parcel {parcel.Key}, {parcel.Value}</h2></a>";
                htmlContent += "<a href='#top'><span>&lt;</span></a>";

                htmlContent += resultProcessing.HasError ? $"<p style='background-color:#ffcccc'>Processing has an Error</p>" : $"<p style='background-color:#ccffcc'>Processing was successful</p>";

                htmlContent += "<table><tr><th>Process</th><th>Timespan</th><th>Document</th><th>Error</th></tr>" + calculation.RenderingFormats.Select(x=> $"<tr><td>{x.Key}</td><td>{x.Value.TimeSpan.ToString()}</td><td>{x.Value.Link}</td><td>{x.Value.Error}</td></tr>").Aggregate((i,j)=> i+ "" + j) + "</table>";

                htmlContent += resultXmlValidation;

                htmlContent += resultCompare;

                htmlContent += metadataHtmlString;

                htmlContent += $"<hr>";

                calculations.Add(calculation);
            }

            htmlContentConclusion += "<table><tr><th>Key</th><th>Comment</th><th>XmlValidation</th><th>CompareProcessing</th></tr>" + calculations
                .Select(
                    x => $"<tr><td><a href='#{x.Guid}'>{x.Key}</a></td><td>{x.Comment}</td><td>{x.XmlValidation}</td><td>{x.CompareProcessingSuccessful}</td></tr>"
                ).Aggregate(
                    (i, j) => i + "" + j
                ) + "</table>";

            return new GAStatus(true, "" , htmlContentConclusion + htmlContent);
        }

        public class Calculation
        {
            public Guid Guid { get; set; }
            public string Key { get; set; }
            public string Comment { get; set; }
            public Dictionary<string, RenderingFormat> RenderingFormats { get; set; }
            public bool XmlValidation { get; set; }
            public bool CompareProcessingSuccessful { get; set; }

            public Calculation()
            {
                RenderingFormats = new Dictionary<string, RenderingFormat>();
            }
        }

        public class RenderingFormat
        {
            private string BaseUrl { get; set; }

            public TimeSpan TimeSpan { get; set; }
            public string FilePath { get; set; }
            public string Error { get; set; } = "";
            public string EncryptedFilePath => string.IsNullOrEmpty(FilePath) ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(CryptTasks.EncryptString(FilePath, ConfigurationManager.AppSettings["AESFilePath"])));
            public string Link => string.IsNullOrEmpty(EncryptedFilePath) ? "" : $"<a href='{BaseUrl}{EncryptedFilePath}' target='_new'>Document</a>";

            public RenderingFormat(string url)
            {
                BaseUrl = url;
            }
        }
    }
}