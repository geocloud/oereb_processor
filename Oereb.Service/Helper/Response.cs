using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web;
using log4net;

namespace Oereb.Service.Helper
{
    public class Response
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static HttpResponseMessage Create(HttpStatusCode status, string message, bool log = true, log4net.Core.Level level = null)
        {
            if (level == null)
            {
                level = log4net.Core.Level.Error;
            }

            if (log)
            {
                Log.Logger.Log(null,level,message,null);
            }

            return new HttpResponseMessage()
            {
                StatusCode = status,
                Content = new StringContent(message)
            };
        }
    }
}