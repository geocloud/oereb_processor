using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server.Adapters;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.Common;

namespace Geocentrale.Apps.Server
{
    public class Global : System.Web.HttpApplication
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Hashtable PageCount = new Hashtable();

        public static List<Uri> ApplicationsLoaded = new List<Uri>();

        public static List<Application> Applications = new List<Application>();
        public static Dictionary<Guid, IGADataSet> DataSets = new Dictionary<Guid, IGADataSet>();
        public static Dictionary<Guid, ScalarClass> ScalarClasses = new Dictionary<Guid, ScalarClass>();
        public static Dictionary<string, ProcessedObject> ProcessedObjects = new Dictionary<string, ProcessedObject>();

        public static Config _config { get; set; }
        public static Config Config {
            get
            {
                if (_config == null)
                {
                    _config = Helper.Xml<Config>.DeserializeFromFile(Path.Combine(PathTasks.GetBinDirectory().Parent.FullName, "Config/base.xml"));
                }

                return _config;
            }
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            log.Debug("Geocentrale Apps starts");

            //SqlServerTypes.Utilities.LoadNativeAssemblies(Server.MapPath("~/bin"));

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["preloadProjects"]))
            {                
                if (HttpRuntime.Cache["ApplicationsLoaded"] != null)
                {
                    log.InfoFormat("IIS-Cache load existing objects, {0}", ApplicationsLoaded.Count);
                    LoadFromIISCache();
                }
                else
                {
                    log.InfoFormat("IIS-Cache is not used (there are no objects)");

                    if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["preloadProjects"]))
                    {
                        log.InfoFormat("Preload projects data {0} starts", ConfigurationManager.AppSettings["preloadProjects"]);

                        var projects = ConfigurationManager.AppSettings["preloadProjects"].Split(';');

                        foreach (var project in projects)
                        {
                            Catalog.Catalog.LoadAppDefinition(project);
                        }

                        SaveToIISCache();

                        log.InfoFormat("Preload projects data {0} ends", ConfigurationManager.AppSettings["preloadProjects"]);
                    }
                }
            }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Uri requestUrl = Context.Request.Url;
            String thePath = requestUrl.AbsolutePath;

            int theCount = 1;

            if (PageCount.ContainsKey(thePath))
            {
                theCount = (int)PageCount[thePath];
                PageCount[thePath] = ++theCount;
            }
            else
            {
                PageCount.Add(thePath, theCount);
            }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
        }

        protected void Application_Error(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
            //SaveToIISCache();

            log.Debug("Geocentrale Apps ends");
        }

        public static void SaveToIISCache()
        {
            log.InfoFormat("IIS-Cache save existing objects, {0}", ApplicationsLoaded.Count);

            HttpRuntime.Cache["ApplicationsLoaded"] = ApplicationsLoaded;
            HttpRuntime.Cache["Applications"] = Applications;
            HttpRuntime.Cache["DataSets"] = DataSets;
            HttpRuntime.Cache["ScalarClasses"] = ScalarClasses;

            //Caching is not welcome
            //HttpRuntime.Cache["ProcessedObjects"] = ProcessedObjects;
        }

        public static void LoadFromIISCache()
        {
            ApplicationsLoaded = (List<Uri>)HttpRuntime.Cache["ApplicationsLoaded"];
            Applications = (List<Application>)HttpRuntime.Cache["Applications"];
            DataSets = (Dictionary<Guid, IGADataSet>)HttpRuntime.Cache["DataSets"];
            ScalarClasses = (Dictionary<Guid, ScalarClass>)HttpRuntime.Cache["ScalarClasses"];

            //Caching is not welcome
            //ProcessedObjects = (Dictionary<string, ProcessedObject>)HttpRuntime.Cache["ProcessedObjects"];

            log.InfoFormat("IIS-Cache load existing objects, {0}", ApplicationsLoaded.Count);
        }
    }
}