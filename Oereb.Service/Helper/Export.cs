using Geocentrale.Apps.DataContracts;
using Oereb.Service.DataContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Xml;
using Geocentrale.Apps.Server;
using Geocentrale.Apps.Server.Export;
using Oereb.Service.DataContracts.Model;

namespace Oereb.Service.Helper
{
    public class Export
    {
        public static HttpResponseMessage Process(MergerRequest mergerRequest, GAReport gAReport, Options options, Geocentrale.Apps.Server.Config.Canton canton)
        {
            var exportModule = new OerebExportModule();

            //mergerrequest is deprecated

            mergerRequest.ReportComplete = options.Flavour == Settings.Flavour.Full;
            mergerRequest.ReportAppendixesAttached = options.AppendixAttached;
            mergerRequest.IncludeDetail = options.Details;
            mergerRequest.IncludeMap = options.WithImages;
            mergerRequest.IncludeLogo = options.WithImages;

            if (options.Format == Settings.Format.Html && options.Response == Options.ResponseType.Mimetype)
            {
                var htmlExport = exportModule.ToHtml(mergerRequest, gAReport);

                if (!htmlExport.Successful)
                {
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent("error export html")
                    };
                }

                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent((string)htmlExport.Value, Encoding.UTF8, "text/html")
                };
            }
            else if (options.Format == Settings.Format.Xml && options.Response == Options.ResponseType.Mimetype)
            {
                var exportXml = exportModule.ToXml(mergerRequest, gAReport);

                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(exportXml, Encoding.UTF8, "text/xml")
                };
            }
            else if (options.Format == Settings.Format.Pdf && (options.Response == Options.ResponseType.Mimetype || options.Response == Options.ResponseType.Specification) )
            {
                var exportPdf = exportModule.ToPdf(mergerRequest, gAReport);

                var pdfResponse = new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(exportPdf)
                };

                pdfResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                //pdfResponse.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline;filename=export.pdf");

                return pdfResponse;
            }
            else if (options.Format == Settings.Format.PdfA1a && options.Response == Options.ResponseType.Mimetype)
            {
                var exportPdfA1 = exportModule.ToPdfA1(mergerRequest, gAReport);

                var pdfResponse = new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(exportPdfA1)
                };

                pdfResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                return pdfResponse;
            }
            else if (options.Format == Settings.Format.Json && options.Response == Options.ResponseType.Mimetype)
            {
                var exportJson = exportModule.ToJson(mergerRequest, gAReport);

                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(exportJson, Encoding.UTF8, "application/json")
                };
            }
            else if (options.Format == Settings.Format.Xml && options.Response == Options.ResponseType.Specification && options.Flavour == Settings.Flavour.Reduced)
            {
                var exportXml = exportModule.ToXml(mergerRequest, gAReport);

                var prefix = "<?xml version=\"1.0\" encoding=\"utf-8\"?><GetExtractByIdResponse xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:data=\"http://schemas.geo.admin.ch/swisstopo/OeREBK/15/ExtractData\" xmlns=\"http://schemas.geo.admin.ch/swisstopo/OeREBK/15/Extract\"><data:Extract>";
                var postfix = "</data:Extract></GetExtractByIdResponse>";

                var document = new XmlDocument();
                document.LoadXml(exportXml);
                
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($"{prefix}{document.DocumentElement.InnerXml}{postfix}", Encoding.UTF8, "text/xml")
                };
            }
            else if (options.Format == Settings.Format.Xml && options.Response == Options.ResponseType.Specification && options.Flavour == Settings.Flavour.Embeddable)
            {
                var exportPdf = exportModule.ToPdf(mergerRequest, gAReport);

                var now = DateTime.Now;
                var datetime = new DateTime(now.Year, now.Month, now.Day, 5, 0, 0, DateTimeKind.Local);

                var embeddable = new GetExtractByIdResponseTypeEmbeddable
                {
                    pdf = exportPdf,
                    cadasterOrganisationName = canton.CadastralAuthority.Name.First().Text,
                    cadasterState = datetime,
                    dataownerNameCadastralSurveying = canton.CadastralAuthority.Name.First().Text,
                    transferFromSourceCadastralSurveying = datetime
                };

                var getExtractByIdResponseType = new GetExtractByIdResponseType() {Item = embeddable};

                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(Xml<GetExtractByIdResponseType>.SerializeToXmlString(getExtractByIdResponseType), Encoding.UTF8, "text/xml")
                };
            }
            else if (options.Format == Settings.Format.Pdf && options.Response == Options.ResponseType.Url)
            {
                var exportPdf = exportModule.ToPdf(mergerRequest, gAReport);

                GATreeNode<GAObject> parcel = gAReport.Result.FirstOrDefault();

                string niceName = string.Empty;

                if (parcel != null)
                {
                    niceName = string.Format("{0}-Oereb_{1:yyyyMMdd}_{2}_{3}",
                        Guid.NewGuid().ToString().Replace("-", string.Empty),
                        DateTime.Now,
                        parcel.Value.Attributes.First(x => x.AttributeSpec.Name.ToLower() == "gemeinde").Value,
                        parcel.Value.Attributes.First(x => x.AttributeSpec.Name.ToLower() == "nummer").Value); // TODO subject to configure
                }
                else
                {
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent("export pdf with response type url, parcel object not found")
                    };
                }

                using (FileStream fileStream = File.Create(Path.Combine(Path.GetTempPath(), $"{niceName}.pdf"), (int)exportPdf.Length))
                {
                    fileStream.Write(exportPdf, 0, exportPdf.Length);
                }

                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(niceName, Encoding.UTF8, "text/plain")
                };
            }

            /*

                 var httpResponseMessage = new HttpResponseMessage();

                var memoryStream = new MemoryStream();

                image.Save(memoryStream, ImageFormat.Png);

                image.Dispose();

                httpResponseMessage.Content = new ByteArrayContent(memoryStream.ToArray());

                httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                httpResponseMessage.StatusCode = HttpStatusCode.OK;

                 */

            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("unknown export")
            };
        }
    }
}