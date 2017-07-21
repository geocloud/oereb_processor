using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Http;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.Apps.Server.Helper;
using Geocentrale.DataAdaptors.Contracts;
using log4net;
using Npgsql;
using Oereb.Service.Config;

namespace Oereb.Service.Controllers
{
    public class CheckController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        public HttpResponseMessage Connection(string project, [FromUri] string token = "", [FromUri] string format = "html")
        {
            var tokenConfig = ConfigurationManager.AppSettings["AdminToken"]??"";

            if (string.IsNullOrEmpty(tokenConfig) || token != tokenConfig)
            {                
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("token is invalid for this function", Encoding.UTF8, "text/plain")
                };
            }

            if (!(new List<string>() {"html", "json"}).Contains(format.ToLower()))
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent($"format {format} not supported", Encoding.UTF8, "text/plain")
                };
            }

            var config = WebApiApplication.Config;

            var cantons = config.Cantons.Where(x => x.Shorname.ToUpper() == project.ToUpper());

            if (!cantons.Any())
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("project not found", Encoding.UTF8, "text/plain")
                };
            }

            dynamic summary = new ExpandoObject();
            var cantonsExo = new List<ExpandoObject>();
            var globalError= false;

            foreach (var canton in cantons)
            {
                var status = CatalogGeoservices.Load(canton.Process.Project, canton);
                dynamic cantonExo = new ExpandoObject();
                dynamic resourcesExo = new List<ExpandoObject>();

                cantonExo.name = canton.Shorname;
                cantonExo.loadCatalog = status.Successful;

                if (!status.Successful)
                {
                    globalError = true;
                    continue;
                }

                foreach (var resource in canton.Resources)
                {
                    dynamic resourceExo = new ExpandoObject();

                    resourceExo.guid = resource.Guid;
                    resourceExo.comment = resource.Comment;
                    resourceExo.seq = resource.Seq;
                    resourceExo.transparency = resource.Transparency;
                    resourceExo.type = resource.Type.ToString();

                    var dataset = Catalog.GetDataSet(resource.Guid);

                    resourceExo.datasetIsValid = (dataset != null);

                    if (dataset == null)
                    {
                        globalError = true;
                        continue;
                    }

                    var dataAdaptor = dataset.GetDataUtilities().Last() as IGAGeoDataAdaptor;

                    resourceExo.dataadaptorIsValid = (dataAdaptor != null);

                    if (dataAdaptor == null)
                    {
                        globalError = true;
                        continue;
                    }

                    resourceExo.dataAdaptor = CheckDataAdaptor(dataAdaptor, ref globalError);

                    resourcesExo.Add(resourceExo);
                }

                cantonExo.resources = resourcesExo;

                cantonsExo.Add(cantonExo);
            }

            summary.cantons = cantonsExo;
            summary.successful = !globalError;

            if (format.ToLower() == "html")
            {
                return new HttpResponseMessage()
                {
                    StatusCode = globalError ? HttpStatusCode.InternalServerError : HttpStatusCode.OK,
                    Content = new StringContent(
                        TasksExpandoObject.ConvertToHtml(summary, !globalError),
                        Encoding.UTF8,
                        "text/html"
                   )
                };
            }
            else
            {
                return new HttpResponseMessage()
                {
                    StatusCode = globalError ? HttpStatusCode.InternalServerError : HttpStatusCode.OK,
                    Content = new StringContent(
                        TasksExpandoObject.ConvertToJson(summary),
                        Encoding.UTF8,
                        "application/json"
                   )
                };
            }
        }

        [HttpGet]
        public HttpResponseMessage Processor(string project, [FromUri] string token = "", [FromUri] string exports = "minimum")
        {
            var tokenConfig = ConfigurationManager.AppSettings["AdminToken"] ?? "";

            if (string.IsNullOrEmpty(tokenConfig) || token != tokenConfig)
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("token is invalid for this function", Encoding.UTF8, "text/plain")
                };
            }

            if (!(new List<string>() { "minimum", "none", "all" }).Contains(exports.ToLower()))
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent($"exports {exports} not supported", Encoding.UTF8, "text/plain")
                };
            }

            var config = WebApiApplication.Config;

            var cantons = config.Cantons.Where(x => x.Shorname.ToUpper() == project.ToUpper());

            if (!cantons.Any())
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("project not found", Encoding.UTF8, "text/plain")
                };
            }

            var canton = cantons.First();

            if (!canton.CheckProcessor.Any())
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($"project {project} has noch configurated checks", Encoding.UTF8, "text/plain")
                };
            }

            Catalog.LoadAppDefinition(canton.Process.Project);

            var mergerRequest = new MergerRequest
            {
                AppId = canton.Process.App,
                ModuleId = canton.Process.Function,
                Format = "html",
                ReportComplete = false,
                ReportAppendixesAttached = false
            };

            mergerRequest.ModuleParameters.Add("adminMode", false);

            var parcelsToCheck = canton.CheckProcessor.ToDictionary(x => x.Egrid, x => x.Comment);

            var formats = new List<string>();

            if (exports == "minimum")
            {
                formats = new List<string>() { "pdf", "xml" };
            }
            else if (exports == "all")
            {
                formats = new List<string>() { "pdf", "pdfa1", "xml", "json", "pdfAttach", "pdfEmbedd", "html" };
            }

            var selectionlayer = canton.Resources.FirstOrDefault(x => x.Type == Geocentrale.Apps.Server.Config.Resource.ResourceType.Selectionlayer);

            mergerRequest.Selections = new List<MergerRequest.JsonSelection>()
            {
                new MergerRequest.JsonSelection()
                {
                    classId = selectionlayer.Guid,
                    buffer = 0
                }
            };

            var appDomainAppPath = HttpRuntime.AppDomainAppVirtualPath ?? "";

            var status = CheckProcessing.Evaluate(parcelsToCheck, mergerRequest, formats, canton.Process.Egrid, Request.RequestUri.GetLeftPart(UriPartial.Authority)+ appDomainAppPath);

            return new HttpResponseMessage()
            {
                StatusCode = !status.Successful ? HttpStatusCode.InternalServerError : HttpStatusCode.OK,
                Content = new StringContent(
                        status.Value.ToString(),
                        Encoding.UTF8,
                        "text/html"
                   )
            };
        }

        #region private section

        private ExpandoObject CheckDataAdaptor(IGAGeoDataAdaptor geodataAdaptor, ref bool globalError)
        {
            dynamic dataAdaptor = new ExpandoObject();

            dataAdaptor.crs = geodataAdaptor.GaGeoClass.SpatialReferenceEpsg;
            dataAdaptor.geometrytype = geodataAdaptor.GaGeoClass.GeometryType.ToString();
            dataAdaptor.type = geodataAdaptor.GetType().ToString();

            var urlCheckRequest = String.Empty;
            bool containsLayerCheck = false;

            if (geodataAdaptor is Geocentrale.DataAdaptors.ArcgisServerRestAdaptor.OerebAdaptor)
            {
                dataAdaptor.urlMapserver = GetValueFromProperty<string>("_url", geodataAdaptor);
                dataAdaptor.layername = GetValueFromProperty<Int32>("_layerId", geodataAdaptor).ToString();
                dataAdaptor.connection = GetValueFromProperty<string>("_connectionString", geodataAdaptor);
                dataAdaptor.table = GetValueFromProperty<string>("_table", geodataAdaptor);

                urlCheckRequest = $"{dataAdaptor.urlMapserver}/{dataAdaptor.layername}";

                GAStatus statusDb = CheckSqlServer(dataAdaptor.connection, dataAdaptor.table);

                dataAdaptor.statusDb = statusDb.Successful;

                if (statusDb.Exceptions.Any())
                {
                    globalError = true;
                    dataAdaptor.errorDb = statusDb.Exceptions.Select(x => x.Message).Aggregate((x, y) => x + "," + y);
                }
            }
            else if (geodataAdaptor is Geocentrale.DataAdaptors.PostgreSqlAdaptor.OerebAdaptor)
            {
                dataAdaptor.urlMapserver = GetValueFromProperty<string>("_url", geodataAdaptor);
                dataAdaptor.layername = GetValueFromProperty<string>("_layername", geodataAdaptor);
                dataAdaptor.connection = $"Server={GetValueFromProperty<string>("_dbHost", geodataAdaptor)};Database={GetValueFromProperty<string>("_dbDatabase", geodataAdaptor)};User Id={GetValueFromProperty<string>("_dbUser", geodataAdaptor)};Password={GetValueFromProperty<string>("_dbPassword", geodataAdaptor)};";
                dataAdaptor.table = GetValueFromProperty<string>("_dbTable", geodataAdaptor);

                urlCheckRequest = $"{dataAdaptor.urlMapserver}?service=wms&version=1.3.0&request=getcapabilities";
                containsLayerCheck = true;

                GAStatus statusDb = CheckPostgresServer(dataAdaptor.connection, dataAdaptor.table);

                dataAdaptor.statusDb = statusDb.Successful;

                if (statusDb.Exceptions.Any())
                {
                    globalError = true;
                    dataAdaptor.errorDb = statusDb.Exceptions.Select(x => x.Message).Aggregate((x, y) => x + "," + y);
                }
            }
            else if (geodataAdaptor is Geocentrale.DataAdaptors.OgcAdaptor.WmsAdaptor)
            {
                dataAdaptor.urlMapserver = GetValueFromProperty<string>("_url", geodataAdaptor);
                dataAdaptor.layername = GetValueFromProperty<string>("_layername", geodataAdaptor);

                urlCheckRequest = $"{dataAdaptor.urlMapserver}?service=wms&version=1.3.0&request=getcapabilities";
                containsLayerCheck = true;
            }
            else if (geodataAdaptor is Geocentrale.DataAdaptors.ArcgisServerRestAdaptor.ArcgisServerRestAdaptor)
            {
                dataAdaptor.urlMapserver = GetValueFromProperty<string>("_url", geodataAdaptor);
                dataAdaptor.layername = GetValueFromProperty<Int32>("_layerId", geodataAdaptor).ToString();

                urlCheckRequest = $"{dataAdaptor.urlMapserver}/{dataAdaptor.layername}";
            }
            else if (geodataAdaptor is Geocentrale.DataAdaptors.ArcgisServerRestAdaptor.ArcgisServerRestMapserviceAdaptor)
            {
                dataAdaptor.urlMapserver = GetValueFromProperty<string>("_url", geodataAdaptor);
                dataAdaptor.layername = GetValueFromProperty<string>("_layerIds", geodataAdaptor);

                urlCheckRequest = $"{dataAdaptor.urlMapserver}";
            }

            GAWebResponse gaWebResponse = null;

            if (!string.IsNullOrEmpty(urlCheckRequest))
            {
                var request = new GAWebRequest();
                var gaWebRequestParameter = new GAWebRequestParameter(urlCheckRequest, "", "", 10000);
                gaWebRequestParameter.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

                gaWebResponse = request.GetWebRequestAsStream(gaWebRequestParameter);

                dataAdaptor.statusMapserver = (gaWebResponse.StatusHttp == 200);
                dataAdaptor.statusCodeMapserver = gaWebResponse.StatusHttp;

                if (gaWebResponse.Exception != null)
                {
                    globalError = true;
                    dataAdaptor.error = gaWebResponse.Exception.Message;
                }
            }

            if (gaWebResponse != null && gaWebResponse.Content != null && containsLayerCheck)
            {
                var layerExist = Encoding.UTF8.GetString(gaWebResponse.Content).Contains(dataAdaptor.layername);
                dataAdaptor.statusMapserver = (gaWebResponse.StatusHttp == 200) && layerExist;

                if (!layerExist)
                {
                    globalError = true;
                    dataAdaptor.error = $"layer {dataAdaptor.layername} does not exist in getcapabilities file";
                }
            }

            //var embeddedDa = GetValueFromProperty< IGAGeoDataAdaptor>("_gaGeoDataAdaptor", geodataAdaptor);

            PropertyInfo embeddedDaProperty = geodataAdaptor.GetType().GetProperty("_gaGeoDataAdaptor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (embeddedDaProperty == null)
            {
                return dataAdaptor;
            }

            var embeddedDa = embeddedDaProperty.GetValue(geodataAdaptor) as IGAGeoDataAdaptor;

            if (embeddedDa == null)
            {
                return dataAdaptor;
            }

            dataAdaptor.embeddedtype = embeddedDa.GetType().ToString();

            return dataAdaptor;
        }

        private T GetValueFromProperty<T>(string name, IGAGeoDataAdaptor geodataAdaptor)
        {

            var property = geodataAdaptor.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var obj = property.GetValue(geodataAdaptor);

            if (typeof(T) == obj.GetType())
            {
                return (T)obj;
            }

            return default(T);
        }

        private GAStatus CheckSqlServer(string connectionstring, string table)
        {
            var status = new GAStatus(true);

            SqlConnection connection = new SqlConnection(connectionstring);

            try
            {
                connection.Open();

                var command = new SqlCommand($"SELECT TOP 1 * from {table}", connection);
                var dataReader = command.ExecuteReader();

                //todo check for attributes: rule <> db

                while (dataReader.Read())
                {
                    //nothing to to
                }

                dataReader.Close();
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                return new GAStatus(false,"",ex);
            }

            return status;
        }

        private GAStatus CheckPostgresServer(string connectionstring, string table)
        {
            var status = new GAStatus(true);

            NpgsqlConnection connection = new NpgsqlConnection(connectionstring);

            try
            {
                connection.Open();

                var command = new NpgsqlCommand($"SELECT * from {table} LIMIT 1", connection);
                var dataReader = command.ExecuteReader();

                //todo check for attributes: rule <> db

                while (dataReader.Read())
                {
                    //nothing to to
                }

                dataReader.Close();
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                return new GAStatus(false, "", ex);
            }

            return status;
        }

        #endregion
    }
}
