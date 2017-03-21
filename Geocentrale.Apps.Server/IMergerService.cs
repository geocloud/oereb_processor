using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Geocentrale.Apps.Server
{
    [ServiceContract]
    public interface IMergerService
    {
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "ProcessByObject")]
        Stream ProcessByObject(Stream body);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "GetReportByEgrid/{project}/{language}/{format}/{value}")]
        Stream GetReportByEgrid(string project, string language, string format, string value);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "GetReport/{format}/{processHash}")]
        Stream GetReport(string format, string processHash);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "GetFile/{format}/{guid}/{saveFile}")]
        Stream GetFile(string format, string guid, string saveFile);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "CreateReport/{format}/{processHash}")]
        Stream CreateReport(string format, string processHash);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "IsLive")]
        Stream IsLive();
    }
}