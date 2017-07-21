using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using Geocentrale.Apps.DataContracts;
using Oereb.Service.DataContracts;
using Geocentrale.Apps.Server;
using Geocentrale.Apps.Server.Adapters;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.Common;

namespace Oereb.Service
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //public static List<Uri> ApplicationsLoaded = new List<Uri>();

        //public static List<Application> Applications = new List<Application>();

        //public static Dictionary<Guid, IGADataSet> DataSets = new Dictionary<Guid, IGADataSet>();
        public static Dictionary<Guid, IGADataSet> DataSetsSearch = new Dictionary<Guid, IGADataSet>();
        //public static Dictionary<Guid, ScalarClass> ScalarClasses = new Dictionary<Guid, ScalarClass>();

        public static Dictionary<string, ProcessedObject> ProcessedObjects = new Dictionary<string, ProcessedObject>();

        public static Geocentrale.Apps.Server.Config _config { get; set; }

        public static Geocentrale.Apps.Server.Config Config
        {
            get
            {
                if (_config == null)
                {
                    _config = Helper.Xml<Geocentrale.Apps.Server.Config>.DeserializeFromFile(Path.Combine(PathTasks.GetBinDirectory().Parent.FullName, "Config/base.xml"));
                }

                return _config;
            }
        }

        protected void Application_Start()
        {
            log4net.Config.XmlConfigurator.Configure();

            Log.Info("Application pool is starting");

            GlobalConfiguration.Configure(WebApiConfig.Register);
            SettingsReader.ReadFromConfig();

            var statusSearchCatalog = Helper.DataAdaptor.LoadSearchCatalog();

            if (!statusSearchCatalog.Successful)
            {
                Log.Fatal("Initial search catalog fail");
            }
            else
            {
                Log.Info("Initial search catalog");
            }

            var preloadProjectsString = ConfigurationManager.AppSettings["preloadProjects"];

            var preloadProjects = preloadProjectsString.Split(';');

            foreach (var preloadProject in preloadProjects)
            {
                var statusCatalog = Catalog.LoadAppDefinition(preloadProject);

                if (!statusCatalog.Successful)
                {
                    Log.Fatal($"Initial search catalog for project {preloadProject} fail");
                }
                else
                {
                    Log.Info($"Initial search catalog for project {preloadProject}");
                }
            }
        }

        protected void Application_End()
        {
            Log.Info("Application pool is ending");
        }
    }
}
