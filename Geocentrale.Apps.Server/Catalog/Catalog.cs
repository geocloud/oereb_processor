using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server.Adapters;
using Geocentrale.ConfigProcessor.GeoDataSet;

namespace Geocentrale.Apps.Server.Catalog
{
    /// <summary>
    /// request and parse config from Gis-Client for apps, static methods for loading existing resources (datasets and scalarclasses) to Global context (global.asax) => fast access to dataadaptors
    /// </summary>
	
    public static class Catalog
	{
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static GAStatus LoadAppDefinition(string project)
		{
			if (string.IsNullOrEmpty(project))
			{
                return new GAStatus(false, null, GAStatus.LogLevel.Error, string.Format("project is empty, {0}", project));
			}

			Uri configUri;

            var url = string.Format("{0}{1}/getappsconfig?token={2}", ConfigurationManager.AppSettings["configServerUrl"], project, ConfigurationManager.AppSettings["AdminToken"]); // TODO: move to AppServerAppSettingsConstants and document it

			Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out configUri);
			
            if (configUri == null)
			{
                return new GAStatus(false, null, GAStatus.LogLevel.Error, string.Format("url is invalid, {0}", url));
			}

			if (Global.ApplicationsLoaded.Contains(configUri))
			{
                return new GAStatus(true, null, GAStatus.LogLevel.Debug, string.Format("cache is used, {0}", url));
			}			

            var request = new GAWebRequest();
            var gaWebRequestParameter = new GAWebRequestParameter(configUri, "", "", 80000);
            gaWebRequestParameter.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

            var gaWebResponse = request.GetWebRequestAsStream(gaWebRequestParameter);

            if (!gaWebResponse.Successfully)
            {
                return new GAStatus(false, null, GAStatus.LogLevel.Error, string.Format("request from config fail, {0}, url {1}", gaWebResponse.Description, configUri), gaWebResponse.Exception);
            }

            var response = Encoding.UTF8.GetString(gaWebResponse.Content);

			XDocument xDocument;

			try
			{
				xDocument = XDocument.Parse(response);
			}
			catch (Exception ex)
			{
                return new GAStatus(false, null, GAStatus.LogLevel.Error, string.Format("error parsing app definition, {0}", ex.Message),ex);
			}

			if (xDocument.Root == null)
			{
                return new GAStatus(false, null, GAStatus.LogLevel.Error, string.Format("no xml root exists"));
			}

			var apps = xDocument.Root.XPathSelectElements("/Config/Apps/Root/App");
			var geoservices = xDocument.Root.XPathSelectElement("/Config/Geoservices");

			foreach (var app in apps)
			{
				Application parsedApp = ParseApp(app, geoservices);

				if (parsedApp == null)
				{
					continue;
				}

			    parsedApp.Project = project;

                Global.Applications.Add(parsedApp);
			}

            Global.ApplicationsLoaded.Add(configUri);

            //save to cache
            //Global.SaveToIISCache();

            return new GAStatus(true,"",GAStatus.LogLevel.Debug,string.Format("catalog succesfully loaded for url {0}",configUri));
		}

	    #region parse config

        public static Application ParseApp(XElement xElement, XElement geoservices)
		{
			if (xElement.Attribute("Guid") == null || xElement.Attribute("Name") == null)
			{
                log.Error(string.Format("ParseApp Guid or Name is null"));
				return null;
			}

			Guid guidApp;

			if (!Guid.TryParse(xElement.Attribute("Guid").Value.ToString(), out guidApp))
			{
                log.Error(string.Format("ParseApp Guid is not valid, {0}", guidApp));
                return null;
			}

            if (Global.Applications.Any(x => x.Guid == guidApp))
			{
                log.Debug(string.Format("ParseApp Guid already exist, {0}", guidApp));
				return null;
			}

			var application = new Application(guidApp, xElement.Attribute("Name").Value.ToString());

            log.Debug("parse resources");

			var resources = xElement.XPathSelectElements("/descendant::Resource");

			foreach (var resource in resources)
			{
                Guid guidResource;

				if (resource.Attribute("Guid") == null || resource.Attribute("Type") == null)
				{
                    log.Error("Resource Guid or Type is null");
					continue;
				}

				if (!Guid.TryParse(resource.Attribute("Guid").Value.ToString(), out guidResource))
				{
                    log.Error(string.Format("Resource Guid is not valid, {0}", guidResource));
                    continue;
				}

				application.Resources.Add(new Resource(guidResource, resource.Attribute("Type").Value));

                ParseResource(guidResource, geoservices, resource.Attribute("Type").Value);

                log.Debug(string.Format("parse resource end {0}", resource.Attribute("Guid").Value));
            }

            log.Debug("parse topics");

            var topics = xElement.XPathSelectElements("/descendant::AppTopic");

		    foreach (var topic in topics)
		    {
		        var applicationTopic = new ApplicationTopic();

		        applicationTopic.Id = topic.Attribute("Id").Value;
		        applicationTopic.Exclude = topic.Attribute("Exclude").Value.ToLower() == "true";

                if (topic.XPathSelectElement("Legend") != null)
                {
                    applicationTopic.LegendUrl = topic.XPathSelectElement("Legend").Attribute("Url").Value;
                    applicationTopic.LegendName = topic.XPathSelectElement("Legend").Attribute("Name").Value;
                }

                foreach (var additionalLayer in topic.XPathSelectElements("AdditionalLayer"))
                {
                    var appAddLayer = new ApplicationAdditionalLayer();

                    appAddLayer.Guid = additionalLayer.Attribute("Guid").Value;
                    appAddLayer.Background = additionalLayer.Attribute("Background").Value.ToLower() == "true";
                    applicationTopic.AdditionalLayers.Add(appAddLayer);
                }

                application.Topics.Add(applicationTopic);
		    }

			var apps = xElement.XPathSelectElements("/App");

			foreach (var app in apps)
			{
				Application childApp = ParseApp(app, geoservices);

				if (childApp == null)
				{
                    log.Error("child application is empty");
					continue;
				}

				application.ChildApps.Add(childApp);
			}

			return application;
		}

		public static void ParseResource(Guid guid, XElement geoservices, string typeResource)
		{
            if (Global.DataSets.ContainsKey(guid))
			{
                log.Debug(string.Format("dataset with guid {0} already exist", guid));
				return;
			}

            switch (typeResource)
		    {
		        case "Layer":
                case "Baselayer":
                case "Selectionlayer":

                    XElement resource = geoservices.XPathSelectElement(string.Format("/descendant::*[@Guid='{0}']", guid.ToString().ToLower()));

                    if (resource == null)
                    {
                        log.Fatal(string.Format("dataset with guid {0} does not exist", guid));
                        return;
                    }

		            if (resource.Name.LocalName == "GeoDataSet")
		            {
                        var geoDataSet = ConfigGeoDataSet.GetGeoDataSet(resource, null);

                        if (geoDataSet == null)
		                {
                            log.Fatal(string.Format("DES of dataset fail, {0}", resource));
		                    return;
		                }

                        foreach (var dataAdaptor in geoDataSet.GetDataUtilities())
                        {
                            var oerebAdaptor = dataAdaptor as DataAdaptors.ArcgisServerRestAdaptor.OerebAdaptor;

                            if (oerebAdaptor == null)
		                    {
		                        continue;
		                    }

                            oerebAdaptor.UseNativeReading = false; // switch reading to sql server
                        }

                        log.Debug(string.Format("add dataset to pool, guid {0}, type {1}", guid, typeResource));
                        Global.DataSets.Add(guid, geoDataSet);
		            }
		            break;

                case "Scalarservice":

                    var scalarServices = new List<ScalarClass>();
				    IEnumerable<Type> adapterTypes = new List<Type>(); ;

			        try
			        {
			            adapterTypes =
			                AppDomain.CurrentDomain.GetAssemblies()
			                    .ToList()
			                    .SelectMany(s => s.GetTypes())
			                    .Where(p => typeof (Geocentrale.Apps.Server.Adapters.IGAAdapter).IsAssignableFrom(p) && p.IsClass);

			        }
                    catch (Exception ex) //ReflectionTypeLoadException ex
                    {
                        log.Error(string.Format("exception in resource {0}", ex));
                        break;
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
                            log.Warn(string.Format("exception in resource {0}", ex));
					    }
				    }

				    ScalarClass gAScalarClass = scalarServices.FirstOrDefault(x => x.Guid.ToString().ToLower() == guid.ToString().ToLower());

                    if (gAScalarClass == null)
                    {
                        log.Debug(string.Format("Sclarclass with guid <{0}> not found", guid));
                        break;
                    }

                    if (Global.ScalarClasses.ContainsKey(guid))
		            {
                        log.InfoFormat("Sclarclass with guid <{0}> already exist in cache", guid);
                        break;
		            }

                    Global.ScalarClasses.Add(guid, gAScalarClass);

                    //TODO check implementation von DataAdaptors

                    //possible resolution with the nee DA concept

                    //if (gAScalarClass == null)
                    //{
                    //    log.Warn(String.Format("gAScalarClass not available {0}", guid.ToString()));
                    //    return;
                    //}

                    //var gaScalarDataSet = new GADataSet();
                    //var gaScalarDataAdaptor = new SimpleDataAdaptor(new List<GAObject>(),gAScalarClass);
                    //IGADataAdaptor igaDataAdaptor = (IGADataAdaptor) gaScalarDataAdaptor;

                    //gaScalarDataAdaptor.Guid = gAScalarClass.Guid; // necessary !!!
                    //gaScalarDataSet.AddWrappedDataUtility(igaDataAdaptor);
                    //DataSets.Add(guid, gaScalarDataSet);

		            break;

                default:

                    log.Warn(string.Format("catalog, type of resource <{0}> not found", typeResource));
		            break;
		    }
		}

        #endregion

        #region helpers

        public static List<IGADataSet> GetAllDataSets(Guid appId)
        {
            var dataSets = new List<IGADataSet>();

            var app = Global.Applications.FirstOrDefault(x => x.Guid == appId);

            if (app == null)
            {
                return dataSets;
            }

            dataSets.AddRange(GetDataSets(app.Resources.Select(x => x.Guid).ToList()));

            return dataSets;
        }

        public static List<IGADataSet> GetDataSets(List<Guid> guids)
        {
            var gADataSets = new List<IGADataSet>();

            foreach (var guid in guids)
            {
                if (!Global.DataSets.ContainsKey(guid))
                {
                    continue;
                }

                gADataSets.Add(GetDataSet(guid));
            }

            return gADataSets;
        }

        public static IGADataSet GetDataSet(Guid guid)
        {
            if (!Global.DataSets.ContainsKey(guid))
            {
                log.Error(string.Format("DataSet with guid <{0}> not found", guid));
                return null;
            }

            return Global.DataSets[guid];
        }

	    public static List<IGADataSet> GetDataSetFromClasses(List<Guid> guidClasses)
	    {
	        return guidClasses.Select(guidClass => GetDataSetFromClass(guidClass)).Where(dataset => dataset != null).ToList();
	    }

	    public static IGADataSet GetDataSetFromClass(Guid guidClass)
	    {
            foreach (var datasetPairValue in Global.DataSets)
            {
                var dataset = datasetPairValue.Value;

                if (dataset.GaClass.Guid.Equals(guidClass))
                {
                   return dataset; 
                }

                var dataAdaptors = dataset.GetDataUtilities();

                if (dataAdaptors.Any(dataAdaptor => dataAdaptor.GaClass.Guid.Equals(guidClass)))
                {
                    return dataset;
                }
            }

            log.Error(string.Format("GetDataSetFromClass, no dataset found for guid {0}", guidClass));
	        return null;
	    }

        public static bool IsDataSet(Guid guid)
	    {
            foreach (var datasetPairValue in Global.DataSets)
            {
                var dataset = datasetPairValue.Value;

                if (dataset.Guid.Equals(guid))
                {
                    return true;
                }

                var dataAdaptors = dataset.GetDataUtilities();

                if (dataAdaptors.Any(dataAdaptor => dataAdaptor.GaClass.Guid.Equals(guid)))
                {
                    return true;
                }
            }

            return false;
	    }

        public static bool IsLayerDataSet(Guid guid)
        {
            if (!IsDataSet(guid))
            {
                return false;
            }

            //todo check is not working for classId's;

            return Global.Applications.Any(application => application.Resources.Any(x => x.Guid.Equals(guid) && x.Type == "Layer"));
        }

        public static T GetAdaptorFromClass<T>(Guid guidClass)
        {
            foreach (var datasetPairValue in Global.DataSets)
            {
                var dataset = datasetPairValue.Value;

                var dataAdaptors = dataset.GetDataUtilities();

                if (dataAdaptors.Any(dataAdaptor => dataAdaptor.GaClass.Guid.Equals(guidClass) && dataAdaptor.GetType() == typeof(T) ))
                {
                    return (T)dataAdaptors.First();
                }
            }

            return default(T);
        }
	    #endregion
    }
}