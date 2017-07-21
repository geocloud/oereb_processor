using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web.Http;
using log4net;
using System.IO;
using System.Net.Http.Headers;
using Geocentrale.Common;

namespace Oereb.Service.Controllers
{
    public class FileController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        public HttpResponseMessage GetFile(string format, string file, bool saveFile)
        {
            if (format.ToLower() != "pdf")
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("getfile, unknown format {format}",Encoding.UTF8,"text/plain")
                };
            }

            string filename = $"{file}.{format}";
            string filePath = Path.Combine(Path.GetTempPath(), filename);
            string[] parts = filename.Split('-');

            if (!File.Exists(filePath) || parts.Length!= 2)
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent($"file {file} with format {format} not found or wrong name convention", Encoding.UTF8, "text/plain")
                };
            }

            var memoryStream = new MemoryStream();

            using (FileStream fileStream = File.OpenRead(filePath))
            {
                memoryStream.SetLength(fileStream.Length);
                fileStream.Read(memoryStream.GetBuffer(), 0, (int)fileStream.Length);
            }


            var streamContent = new StreamContent(memoryStream);

            if (saveFile)
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = parts[1]
                };
            }
            else
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            }

            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = streamContent
            };
        }

        [HttpGet]
        public HttpResponseMessage GetEFile([FromUri] string file = "")
        {
            if (string.IsNullOrEmpty(file))
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("getEFile, file is missing", Encoding.UTF8, "text/plain")
                };
            }

            var filepath = CryptTasks.DecryptString(Encoding.UTF8.GetString(Convert.FromBase64String(file)), ConfigurationManager.AppSettings["AESFilePath"]);
            var filefullpath = Path.Combine(Path.GetTempPath(), filepath);

            if (!File.Exists(filefullpath))
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent($"file {file} not found", Encoding.UTF8, "text/plain")
                };
            }

            var memoryStream = new MemoryStream();

            using (FileStream fileStream = File.OpenRead(filefullpath))
            {
                memoryStream.SetLength(fileStream.Length);
                fileStream.Read(memoryStream.GetBuffer(), 0, (int)fileStream.Length);
            }

            var streamContent = new StreamContent(memoryStream);
            var fileinfo = new FileInfo(filefullpath);

            switch (fileinfo.Extension.ToLower())
            {
                case ".pdf":
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                    break;

                case ".json":
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    break;

                case ".xml":
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                    break;

                case ".html":
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    break;

                default:
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = fileinfo.Name
                    };
                    break;
            }

            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = streamContent
            };
        }
    }
}
