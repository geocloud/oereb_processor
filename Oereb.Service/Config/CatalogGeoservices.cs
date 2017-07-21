using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server;
using Geocentrale.Apps.Server.Adapters;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.ConfigProcessor.GeoDataSet;

namespace Oereb.Service.Config
{
    public class CatalogGeoservices
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static GAStatus Load(string project, Geocentrale.Apps.Server.Config.Canton canton)
        {
            if (string.IsNullOrEmpty(project))
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, $"project is empty, {project}");
            }

            Uri configUri;

            var url = string.Format("{0}{1}/getappsconfig?token={2}", 
                ConfigurationManager.AppSettings["configServerUrl"], 
                project, 
                ConfigurationManager.AppSettings["AdminToken"]
            );

            Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out configUri);

            if (configUri == null)
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, $"url is invalid, {url}");
            }

            if (Global.ApplicationsLoaded.Contains(configUri))
            {
                return new GAStatus(true, null, GAStatus.LogLevel.Debug, $"cache is used, {url}");
            }

            var request = new GAWebRequest();
            var gaWebRequestParameter = new GAWebRequestParameter(configUri, "", "", 80000);
            gaWebRequestParameter.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

            var gaWebResponse = request.GetWebRequestAsStream(gaWebRequestParameter);

            if (!gaWebResponse.Successfully)
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, $"request from config fail, {gaWebResponse.Description}, url {gaWebResponse.Exception}" );
            }

            var response = Encoding.UTF8.GetString(gaWebResponse.Content);

            XDocument xDocument;

            try
            {
                xDocument = XDocument.Parse(response);
            }
            catch (Exception ex)
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, $"error parsing app definition, {ex.Message}", ex);
            }

            if (xDocument.Root == null)
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, "no xml root exists");
            }

            var geoservices = xDocument.Root.XPathSelectElement("/Config/Geoservices");
            
            IniResource(canton.Resources.Select(x=>x.Guid).ToList(), geoservices);
            IniScalarClass();

            //---------------------------------------------------------
            //temprorary until config is fully switched

            var app = new Application();

            app.Guid = canton.Process.App;

            foreach (var resource in canton.Resources)
            {
                app.Resources.Add(new Resource(resource.Guid, resource.Type.ToString()));
            }

            foreach (var topic in canton.TopicsExcluded)
            {
                app.Topics.Add(new ApplicationTopic() {Id = topic.Code, Exclude = true});
            }

            Global.Applications.Add(app);

            //---------------------------------------------------------

            Global.ApplicationsLoaded.Add(configUri);

            return new GAStatus(true, "", GAStatus.LogLevel.Debug, $"catalog succesfully loaded for url {configUri}");
        }

        public static void IniResource(List<Guid> resources, XElement geoservices)
        {
            var status = new GAStatus(true);

            foreach (var resource in resources)
            {
                if (Global.DataSets.ContainsKey(resource))
                {
                    continue;
                }

                XElement resourceElement = geoservices.XPathSelectElement(string.Format("/descendant::*[@Guid='{0}']", resource.ToString().ToLower()));

                if (resourceElement == null)
                {
                    status.Add( new GAStatus(false, $"dataset with guid {resource} does not exist"));
                    continue;
                }

                if (resourceElement.Name.LocalName == "GeoDataSet")
                {
                    var geoDataSet = ConfigGeoDataSet.GetGeoDataSet(resourceElement, null);

                    if (geoDataSet == null)
                    {
                        status.Add(new GAStatus(false, $"DES of dataset fail, {resource}"));
                        continue;
                    }

                    foreach (var dataAdaptor in geoDataSet.GetDataUtilities())
                    {
                        var oerebAdaptor = dataAdaptor as Geocentrale.DataAdaptors.ArcgisServerRestAdaptor.OerebAdaptor;

                        if (oerebAdaptor == null)
                        {
                            continue;
                        }

                        oerebAdaptor.UseNativeReading = false; // switch reading to sql server
                    }

                    Log.Debug($"add dataset to pool, guid {resource}");

                    Global.DataSets.Add(resource, geoDataSet);
                }
            }
        }

        public static GAStatus IniScalarClass()
        {
            var scalarServices = new List<ScalarClass>();
            IEnumerable<Type> adapterTypes = new List<Type>(); ;           

            try
            {
                adapterTypes =
                    AppDomain.CurrentDomain.GetAssemblies()
                        .ToList()
                        .SelectMany(s => s.GetTypes())
                        .Where(p => typeof(Geocentrale.Apps.Server.Adapters.IGAAdapter).IsAssignableFrom(p) && p.IsClass);

            }
            catch (Exception ex)
            {
                return new GAStatus(false,"error ini ScalarClass", ex);
            }

            foreach (var adapterType in adapterTypes)
            {
                var adapter = (Geocentrale.Apps.Server.Adapters.IGAAdapter)Activator.CreateInstance(adapterType);

                try
                {
                    scalarServices.AddRange(adapter.Identify());
                }
                catch (Exception ex)
                {
                    Log.Warn($"exception in resource {ex}");
                }
            }

            foreach (var scalarService in scalarServices)
            {
                if (Global.ScalarClasses.ContainsKey(scalarService.Guid))
                {
                    continue;
                }

                Global.ScalarClasses.Add(scalarService.Guid, scalarService);
            }

            if (!Global.ScalarClasses.Any())
            {
                return new GAStatus(false, "ini catalog no scalar services found");
            }

            return new GAStatus(true);
        }
    }
}