using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web.Http;
using Geocentrale.Apps.Server;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.Common;
using log4net;
using Newtonsoft.Json;
using Oereb.Service.DataContracts;
using Oereb.Service.Helper;

namespace Oereb.Service.Controllers
{
    public class QueryController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [HttpPost]
        public HttpResponseMessage GetExtractByFilter([FromBody] object postData)
        {
            var config = WebApiApplication.Config;

            if (config == null || config.Cantons == null)
            {
                return Helper.Response.Create(HttpStatusCode.InternalServerError, "base config is not valid");
            }

            List<string> serializationErrors = new List<string>();

            var mergerRequest = JsonConvert.DeserializeObject<MergerRequest>(postData.ToString(), new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    serializationErrors.Add(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            });

            if (serializationErrors.Any())
            {
                StringBuilder stringBuilder = new StringBuilder();

                foreach (var error in serializationErrors)
                {
                    stringBuilder.AppendLine(error);
                }

                var message = $"query controller, deserialize post fail: {stringBuilder}";

                return Helper.Response.Create(HttpStatusCode.BadRequest, message);
            }

            var canton = config.Cantons.FirstOrDefault(x => x.Shorname == mergerRequest.Canton);

            if (canton == null)
            {
                return Helper.Response.Create(HttpStatusCode.BadRequest, $"canton {mergerRequest.Canton} not found");
            }

            var status = Catalog.LoadAppDefinition(canton.Process.Project);

            if (!status.Successful)
            {
                return Helper.Response.Create(HttpStatusCode.InternalServerError, "initial catalog fail");
            }

            mergerRequest.AppId = canton.Process.App;

            var resourceSelection = canton.Resources.FirstOrDefault(x=>x.Type == Geocentrale.Apps.Server.Config.Resource.ResourceType.Selectionlayer);

            if (resourceSelection == null)
            {
                return Helper.Response.Create(HttpStatusCode.BadRequest, $"no resource for selection found, canton {canton.Shorname}");
            }

            if (mergerRequest.Selections == null || mergerRequest.Selections.Count != 1)
            {
                return Helper.Response.Create(HttpStatusCode.BadRequest, $"no selection exists, canton {canton.Shorname}");
            }

            mergerRequest.ProcessHash = CryptTasks.GetSha1HashHex($"{canton.Shorname};{resourceSelection.Guid};{JsonConvert.SerializeObject(mergerRequest.Selections.First())};");

            var gAReport = Helper.Report.Process(mergerRequest, resourceSelection.Guid);

            if (gAReport.HasError)
            {
                return Helper.Response.Create(HttpStatusCode.InternalServerError, $"no selection object found, canton {canton.Shorname}");
            }

            //todo merger mergerRequest and options or helper to convert
            var options = new Options(mergerRequest.Format, mergerRequest.ReportComplete ? "Full" : "Reduced", "De", "", true, mergerRequest.Canton, "Mimetype",mergerRequest.ReportAppendixesAttached, mergerRequest.IncludeDetail);

            return Export.Process(mergerRequest, gAReport, options, canton);
        }
    }
}
