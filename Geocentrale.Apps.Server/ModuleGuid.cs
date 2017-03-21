using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Geocentrale.Apps.Server
{
    // TODO: document class andm members
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class ModuleGuid : System.Attribute
    {
        public Guid Value { get; set; }

        public ModuleGuid(string moduleGuid)
        {
            this.Value = Guid.Parse(moduleGuid);
        }
    }
}