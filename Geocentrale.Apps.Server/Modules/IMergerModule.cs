using Geocentrale.Apps.DataContracts;

namespace Geocentrale.Apps.Server.Modules
{
    public interface IMergerModule
    {
        GAReport Process(MergerRequest mergerRequest);
    }
}
