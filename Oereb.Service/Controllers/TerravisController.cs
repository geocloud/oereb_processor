using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Oereb.Service.Controllers
{
    /// <summary>
    /// old terravis interface (deprecated)
    /// </summary>

    public class TerravisController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage GetReportByEgrid(string project, string language, string format, string egrid, [FromUri] bool cache = true)
        {
            var config = WebApiApplication.Config;

            if (config == null || config.Cantons == null)
            {
                return new HttpResponseMessage() {StatusCode = HttpStatusCode.InternalServerError, Content = new StringContent("base config is not valid")};
            }

            project = project.Replace("_", "/");

            var canton = config.Cantons.FirstOrDefault(x => x.Process != null && x.Process.Project == project);

            if (canton == null)
            {
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent($"canton for project {project} not found") };
            }

            if (format == "pdf")
            {
                format = "PdfA1a";
            }

            var options = new Oereb.Service.DataContracts.Options(format, "reduced", language, "", false, canton.Shorname, "mimetype", false, false, cache);

            return ExtractController.ExtractByEgrid(options, egrid);
        }
    }
}
