using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Geocentrale.Apps.Db.Law;

namespace Geocentrale.Apps.Server.Adapters.LexFind
{
    public class RechtsnormViewModel : Rechtsnorm
    {
        public RechtsnormViewModel()
            : base()
        {

        }

        public RechtsnormViewModel(Rechtsnorm rechtsnorm)
            : base()
        {
            foreach (PropertyInfo pi in typeof(Rechtsnorm).GetProperties())
            {
                GetType().GetProperty(pi.Name).SetValue(this, pi.GetValue(rechtsnorm, null), null);
            }
            //LimitDepth(this);
        }
        public string TitelClass { get; set; }
        public string OffiziellerTitelClass { get; set; }
        public string AbkuerzungClass { get; set; }
        public string OffizielleNrClass { get; set; }
        public string UrlClass { get; set; }
        public string UrlSource { get; set; }
        public string PubliziertAbClass { get; set; }
        public string KantonClass { get; set; }
        public string GemeindeText { get; set; }

        private void LimitDepth(dynamic obj)
        {
            if (obj == null)
            {
                return;
            }
            Type type = obj.GetType();
            foreach (PropertyInfo pi in type.GetProperties())
            {
                dynamic propertyValue = pi.GetValue(obj, null);
                if (propertyValue is Rechtsnorm)
                {
                    GetType().GetProperty(pi.Name).SetValue(obj, null, null);
                }
                else
                {
                    LimitDepth(propertyValue);
                }
            }
        }

    }
}
