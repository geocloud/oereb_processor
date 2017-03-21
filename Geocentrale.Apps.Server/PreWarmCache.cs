using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Geocentrale.Apps.Server
{
    public class PreWarmCache : System.Web.Hosting.IProcessHostPreloadClient
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Preload(string[] parameters)
        {
            log.Info("Application warms up");
        }
    }
}