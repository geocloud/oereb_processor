using Geocentrale.Apps.DataContracts;

namespace Geocentrale.Apps.Server.Export
{
    public interface IExportModule
    {
        GAStatus ToHtml(MergerRequest mergerRequest, GAReport gAReport);
        string ToJson(MergerRequest mergerRequest, GAReport gAReport);
        string ToXml(MergerRequest mergerRequest, GAReport gAReport);
        byte[] ToPdf(MergerRequest mergerRequest, GAReport gAReport);
        byte[] ToPdfA1(MergerRequest mergerRequest, GAReport gAReport);
    }
}
