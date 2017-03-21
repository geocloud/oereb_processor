using System;

namespace Geocentrale.Apps.Server.Catalog
{
    // TODO: document this class and members
    public class Resource
    {
        public Guid Guid { get; set; }
        public string Type { get; set; }

        public Resource(Guid guid, string type)
        {
            Guid = guid;
            Type = type;
        }
    }
}