using Geocentrale.Apps.DataContracts;
namespace Geocentrale.Apps.Server.Catalog
{
    // TODO: document this class and members
    public class ProcessedObject
    {
        public MergerRequest MergerRequest { get; set; }
        public GAReport GaReport { get; set; }
        public Config.Canton Canton { get; set; }
    }
}