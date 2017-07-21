using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Http;
using Geocentrale.Apps.DataContracts.Filter;
using Geocentrale.Apps.Server;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.DataAdaptors.Contracts;
using Geocentrale.DataAdaptors.Resolver;
using Geocentrale.Filter;
using log4net;
using Oereb.Service.Config;
using Oereb.Service.DataContracts;
using Oereb.Service.Helper;

namespace Oereb.Service.Controllers
{
    public class ExtractController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// get extract by id
        /// </summary>
        /// <param name="flavour">reduced | full | embedded</param>
        /// <param name="format">pdf | xml | (json | html) </param>
        /// <param name="egrid"></param>
        /// <param name="lang">De</param>
        /// <param name="topics"></param>
        /// <param name="canton"></param>
        /// <param name="response">Mimetype | Specification | Url</param>
        /// <param name="appendixAttached"></param>
        /// <param name="details"></param>
        /// <param name="cache"></param>
        /// <returns>see specification, it depends on input parameters</returns>

        [HttpGet]
        public HttpResponseMessage ExtractByEgridWithoutGeometry(string flavour,string format, string egrid, [FromUri] string lang = "De", [FromUri] string topics = "ALL", [FromUri] string canton = "", [FromUri] string response = "Specification", [FromUri] bool appendixAttached = false, [FromUri] bool details = false, [FromUri] bool cache = true)
        {
            return ExtractByEgrid(new Options(format, flavour, lang, topics, false, canton, response, appendixAttached, details, cache), egrid);
        }

        [HttpGet]
        public HttpResponseMessage ExtractByEgridWithGeometry(string flavour, string format, string egrid, [FromUri] string lang = "De", [FromUri] string topics = "ALL", [FromUri] string canton = "", [FromUri] string response = "Specification", [FromUri] bool appendixAttached = false, [FromUri] bool details = false, [FromUri] bool cache = true)
        {
            return ExtractByEgrid(new Options(format, flavour, lang, topics, true, canton, response, appendixAttached, details, cache), egrid);
        }

        [HttpGet]
        public HttpResponseMessage ExtractByIdWithoutGeometry(string flavour, string format, string identDn, string number, [FromUri] string lang = "De", [FromUri] string topics = "ALL", [FromUri] string response = "Specification", [FromUri] bool appendixAttached = false, [FromUri] bool details = false, [FromUri] bool cache = true)
        {
            return ExtractById(new Options(format, flavour, lang, topics, false, "", response, appendixAttached, details, cache), identDn, number);
        }

        [HttpGet]
        public HttpResponseMessage ExtractByIdWithGeometry(string flavour, string format, string identDn, string number, [FromUri] string lang = "De", [FromUri] string topics = "ALL", [FromUri] string response = "Specification", [FromUri] bool appendixAttached = false, [FromUri] bool details = false, [FromUri] bool cache = true)
        {
            return ExtractById(new Options(format, flavour, lang, topics, true, "", response, appendixAttached, details, cache), identDn, number);
        }

        #region private 

        public static HttpResponseMessage ExtractByEgrid(Options options, string egrid)
        {
            var regex = new Regex(@"^CH(\d){12}");

            if (egrid.Length != 14 || !regex.IsMatch(egrid))
            {
                return ErrorMessage(HttpStatusCode.InternalServerError,  "no valid egrid value");
            }

            if (!Settings.SupportedLanguages.Contains(options.Language))
            {
                return ErrorMessage(HttpStatusCode.InternalServerError, "language not supported");
            }

            if (!Settings.SupportedFlavours.Contains(options.Flavour))
            {
                return ErrorMessage(HttpStatusCode.InternalServerError, "flavour not supported");
            }

            Geocentrale.Apps.DataContracts.GAReport gAReport;
            Geocentrale.Apps.Server.MergerRequest mergerRequest;
            Geocentrale.Apps.Server.Config.Canton canton;

            if (!WebApiApplication.ProcessedObjects.ContainsKey(egrid) || !options.Cache)
            {
                Log.Info($"processing for egrid {egrid}: {options.ToString()}");
                                
                if (string.IsNullOrEmpty(options.Canton))
                {
                    var queryObjects = DataAdaptor.CreateFilter(4, egrid, "", "", null, "");
                    var statusSearch = Helper.DataAdaptor.GetEgridFromDa(queryObjects, "");

                    if (!statusSearch.Successful || statusSearch.Value.egrid == null ||
                        statusSearch.Value.egrid.Length != 1)
                    {
                        return ErrorMessage(HttpStatusCode.InternalServerError,
                            $"extract: error query the ndident from egrid {egrid}");
                    }

                    options.Canton = new String(statusSearch.Value.identDN.First().ToUpper().Take(2).ToArray());
                }

                var config = WebApiApplication.Config;

                canton = config.Cantons.FirstOrDefault(x => x.Shorname.ToUpper() == options.Canton.ToUpper());

                if (canton == null)
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError,
                        $"extract: config from canton not found {options.Canton}");
                }

                if (canton.Process == null || string.IsNullOrEmpty(canton.Process.Project))
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError,
                        $"extract: process element not defined in base config");
                }

                var status = CatalogGeoservices.Load(canton.Process.Project, canton);

                if (!status.Successful)
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError, $"extract: intial catalog fail");
                }

                //start with processing

                var selectionLayer =
                    canton.Resources.FirstOrDefault(
                        x => x.Type == Geocentrale.Apps.Server.Config.Resource.ResourceType.Selectionlayer);

                if (selectionLayer == null)
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError,
                        $"extract: selectionlayer not found in canton {options.Canton}");
                }

                var selectionDataset = Catalog.GetDataSet(selectionLayer.Guid);

                if (selectionDataset == null)
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError,
                        $"extract: selectionlayer dataset not found in canton {options.Canton}");
                }

                var selectionAdaptor =
                    selectionDataset.GetDataUtilities().LastOrDefault(x => (x as IGADataAdaptor) != null);

                if (selectionAdaptor == null)
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError,
                        $"extract: selectionlayer adaptor not found in canton {options.Canton}");
                }

                var dataAdaptor = selectionAdaptor as IGADataAdaptor;

                var attributeSpec =
                    dataAdaptor.GaClass.AttributeSpecs.FirstOrDefault(x => x.Name == canton.Process.Egrid);

                if (attributeSpec == null)
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError,
                        $"extract: attribute {canton.Process.Egrid} not found in canton {options.Canton}");
                }

                var filter = new GAFilter();
                filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, attributeSpec.Name,
                    AttributeOperator.Equal, egrid, typeof(string)));

                var inputObject =
                    Resolver.ResolveGAObjects(new List<ResolverSkeleton>
                    {
                        new ResolverSkeleton(selectionDataset, filter)
                    }).FirstOrDefault();

                if (inputObject == null)
                {
                    return ErrorMessage(HttpStatusCode.NoContent,
                        $"extract: no input object found in canton {options.Canton}");
                }

                mergerRequest = new MergerRequest(canton.Process.App.ToString(), canton.Process.Function.ToString(),
                    false, null , options.Format.ToString().ToLower(), canton.Process.Project);

                var idField = selectionAdaptor.GaClass.ObjectIdFieldName;

                var filterObjectId = new GAFilter();
                filterObjectId.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, idField,
                    AttributeOperator.In, (int) inputObject[inputObject.GAClass.ObjectIdFieldName], typeof(int)));

                mergerRequest.Selections.Add(new MergerRequest.JsonSelection()
                {
                    classId = selectionLayer.Guid,
                    gaFilter = filterObjectId
                });

                //var ser = Newtonsoft.Json.JsonConvert.SerializeObject(mergerRequest); //for debugging DES

                var module = new Geocentrale.Apps.Server.Modules.OerebModule();

                gAReport = module.Process(mergerRequest);

                if (gAReport.HasError)
                {
                    return ErrorMessage(HttpStatusCode.InternalServerError, $"extract: error processing {canton.Process.Egrid} not found in canton {options.Canton}, error: {gAReport.Guid}");
                }

                if (options.Cache)
                {
                    WebApiApplication.ProcessedObjects.Add(egrid, new ProcessedObject()
                    {
                        GaReport = gAReport,
                        MergerRequest = mergerRequest,
                        Canton = canton
                    });
                }
            }
            else
            {
                Log.Info($"use cache for egrid {egrid}: {options.ToString()}");

                mergerRequest = WebApiApplication.ProcessedObjects[egrid].MergerRequest;
                gAReport = WebApiApplication.ProcessedObjects[egrid].GaReport;
                canton = WebApiApplication.ProcessedObjects[egrid].Canton;
            }

            return Export.Process(mergerRequest, gAReport, options, canton);
        }

        private HttpResponseMessage ExtractById(Options options, string nbident, string number)
        {            
            var egrid = GetEgridFromNbIdent(nbident,number);

            if (string.IsNullOrEmpty(egrid))
            {
                return ErrorMessage(HttpStatusCode.InternalServerError, $"extraxt: query with nbident {nbident} and number {number} not successful");
            }

            options.Canton = new String(nbident.ToUpper().Take(2).ToArray());
            return ExtractByEgrid(options, egrid);
        }

        private string GetEgridFromNbIdent(string nbident, string number)
        {
            var queryObjects = DataAdaptor.CreateFilter(1, nbident, number, "", null, new String(nbident.ToUpper().Take(2).ToArray()));

            var status = Helper.DataAdaptor.GetEgridFromDa(queryObjects, "");

            if (!status.Successful || status.Value.egrid == null || status.Value.egrid.Length != 1)
            {
                return string.Empty;
            }

            return status.Value.egrid.First();
        }

        public static HttpResponseMessage ErrorMessage(HttpStatusCode code, string description)
        {
            return new HttpResponseMessage()
            {
                StatusCode = code,
                Content = new StringContent(description, Encoding.UTF8, "text/xml")
            };
        }

        #endregion
    }
}
