using System.Collections.Generic;

namespace Geocentrale.Apps.Server.Catalog
{
    // TODO: document this class and members
    public class ApplicationTopic
    {
        public string Id { get; set; }
        public bool Exclude { get; set; }
        public List<ApplicationAdditionalLayer> AdditionalLayers { get; set; }

        public string LegendUrl { get; set; }
        public string LegendName { get; set; }

        public ApplicationTopic()
        {
            AdditionalLayers = new List<ApplicationAdditionalLayer>();
        }
    }
}