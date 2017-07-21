using System.Collections.Generic;
using System;

namespace Geocentrale.Apps.Server.Catalog
{
    // TODO: document this class and members
    public class Application
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Project { get; set; }

        public List<Application> ChildApps { get; set; }
        public List<Resource> Resources { get; set; }
        public List<ApplicationTopic> Topics { get; set; }

        public Application()
        {
            ChildApps = new List<Application>();
            Resources = new List<Resource>();
            Topics = new List<ApplicationTopic>();
        }

        public Application(Guid guid, string name)
            : this()
        {
            Guid = guid;
            Name = name;
        }
    }
}
