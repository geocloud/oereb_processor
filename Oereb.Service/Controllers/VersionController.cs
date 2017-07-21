using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.UI.WebControls;
using Oereb.Service.Helper;

namespace Oereb.Service.Controllers
{
    public class VersionController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage GetVersions()
        {
            var response = new Oereb.Service.DataContracts.Model.VersionType();

            response.serviceEndpointBase = ConfigurationManager.AppSettings["serviceendpointbase"];
            response.version = ConfigurationManager.AppSettings["version"];

            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(Xml<Oereb.Service.DataContracts.Model.VersionType>.SerializeToXmlString(response), Encoding.UTF8, "application/xml")
            };
        }
    }
}
