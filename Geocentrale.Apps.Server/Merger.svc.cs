using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using Aspose.Words;
using Aspose.Words.Saving;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.Apps.Server.Export;
using Geocentrale.Apps.Server.Modules;
using Geocentrale.Common;
using Geocentrale.DataAdaptors.Contracts;
using Geocentrale.DataAdaptors.Resolver;
using Geocentrale.Filter;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using System.Configuration;
using Geocentrale.Apps.DataContracts.Filter;
using Geocentrale.Apps.Server.Adapters;

[assembly: XmlConfigurator(Watch = true)]

namespace Geocentrale.Apps.Server
{
	[ServiceContract]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
	public class Merger //: IMergerService //if interface is active the normal webservice is not working
	{
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[OperationContract]
		[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "ProcessByObject")]
		public Stream ProcessByObject(Stream body)
		{
		    log.Debug("****** Begin with process");

			// get the request json into a string

            string mergerRequestAsString;

			using (var reader = new StreamReader(body, Encoding.UTF8))
			{
				mergerRequestAsString = reader.ReadToEnd();
			}

            log.DebugFormat("mergerRequest: {0}", mergerRequestAsString);

			string processHash = CryptTasks.GetSha1HashHex(mergerRequestAsString);
			MergerRequest mergerRequest;
			GAReport gAReport;

			if (!Global.ProcessedObjects.ContainsKey(processHash))
			{
                log.DebugFormat("Catalog has to be build, processHash {0}", processHash);

				List<string> serializationErrors = new List<string>();

				mergerRequest = JsonConvert.DeserializeObject<MergerRequest>(mergerRequestAsString, new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        serializationErrors.Add(args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                });

                if (serializationErrors.Any())
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var error in serializationErrors)
                    {
                        sb.AppendLine(error);
                    }
                    var msg = "Serialization error(s): " + sb;
                    log.Error(msg);
                    return
                        GetStreamFromString(
                            JsonConvert.SerializeObject(
                                new { HasError = true, Error = msg }), "application/json");
                }

				mergerRequest.ProcessHash = processHash;

				dynamic project;
				mergerRequest.ModuleParameters.TryGetValue("project", out project);
				project = project as string ?? string.Empty;

			    mergerRequest.DistanceAbstractionRule = ConfigAccessTask.GetAppSettingsDouble(Setting.DistanceAbstractionRule);//TODO: is a default to set?

                log.Debug(string.Format("****** begin load catalog, {0}", project));

				var status = Catalog.Catalog.LoadAppDefinition(project);
                
                if (!status.Successful)
			    {
			        var msg = "initial catalog fail";
			        log.Error(msg);
                    return GetStreamFromString(JsonConvert.SerializeObject(new { HasError = true, Error = msg }), "application/json");
			    }

                log.Debug(string.Format("****** end load catalog, {0}", project));

				var module = GetMergerModule(mergerRequest.ModuleId);

				if (module == null)
				{
				    var msg = "Module not found";
                    log.Error(msg);
                    return GetStreamFromString(
						JsonConvert.SerializeObject(new {HasError = true, Error = msg}), "application/json");
				}

                log.Debug("****** process is starting");

				gAReport = module.Process(mergerRequest);

                log.Debug("****** process is ending");

				if (gAReport.HasError)
				{
                    log.ErrorFormat("report fail, {0}", gAReport.Error);
					return GetStreamFromString(JsonConvert.SerializeObject(gAReport), "application/json");
				}

				Global.ProcessedObjects.Add(processHash, new ProcessedObject {mergerRequest = mergerRequest, gAReport = gAReport});
			}
			else
			{
                log.DebugFormat("take Catalog from cache, processHash {0}", processHash);

				mergerRequest = Global.ProcessedObjects[processHash].mergerRequest;
				gAReport = Global.ProcessedObjects[processHash].gAReport;
			}

            log.Debug("****** export is starting");

            var export = Export(mergerRequest, gAReport);

            log.Debug("****** export is ending");

            return export;
        }

		[OperationContract]
		[WebInvoke(Method = "GET",
			ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "CreateReport/{format}/{processHash}")]
		public Stream CreateReport(string format, string processHash)
		{
			if (!Global.ProcessedObjects.ContainsKey(processHash))
			{
                return Error("there is no processed object");
            }

			if (!new List<string> { "pdf", "json", "xml", "pdfCompl", "pdfComplInteg" }.Contains(format))
			{
                return Error($"invalid output format {format}");
            }

			MergerRequest mergerRequest = CloneObject(Global.ProcessedObjects[processHash].mergerRequest) ;
			GAReport gAReport = Global.ProcessedObjects[processHash].gAReport;

            if (format == "pdfCompl")
            {
                format = "pdf";
                mergerRequest.Format = "pdf";
                mergerRequest.ReportComplete = true;
                mergerRequest.ReportAppendixesAttached = false;
            }
            else if (format == "pdfComplInteg")
            {
                format = "pdf";
                mergerRequest.Format = "pdf";
                mergerRequest.ReportComplete = true;
                mergerRequest.ReportAppendixesAttached = true;
            }

            GATreeNode<GAObject> parcel = gAReport.Result.FirstOrDefault();

			string niceName = string.Empty;

			if (parcel != null)
			{
				niceName = string.Format("Oereb_{0:yyyyMMdd}_{1}_{2}", 
                    DateTime.Now, 
                    parcel.Value.Attributes.First(x=>x.AttributeSpec.Name.ToLower()=="gemeinde").Value, 
                    parcel.Value.Attributes.First(x => x.AttributeSpec.Name.ToLower() == "nummer").Value); // TODO subject to configure
			}

			Stream export = Export(mergerRequest, gAReport, format);

		    if (export.Length == 0)
		    {
                return Error($"error create report {format}");
            }

			string requestGuid = Guid.NewGuid().ToString().Replace("-",string.Empty);

			using (FileStream fileStream = File.Create(Path.Combine(Path.GetTempPath(), string.Format("{0}-{1}.{2}", requestGuid,niceName, format)), (int)export.Length))
			{
				byte[] bytesInStream = new byte[export.Length];
				export.Read(bytesInStream, 0, bytesInStream.Length);
				fileStream.Write(bytesInStream, 0, bytesInStream.Length);
			}

			return GetStreamFromString(string.Format("{0}-{1}", requestGuid, niceName), "text/plain");
		}

		[OperationContract]
		[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "GetFile/{format}/{guid}/{saveFile}")]
		public Stream GetFile(string format, string guid,string saveFile)
		{
			if (!new List<string> { "pdf", "json", "xml", "pdfCompl", "pdfComplInteg" }.Contains(format))
			{
                return Error($"invalid output format {format}");
            }

            if (format == "pdfCompl")
            {
                format = "pdf";
            }
            else if (format == "pdfComplInteg")
            {
                format = "pdf";
            }

            bool savFileFlag = saveFile.ToLower() == "true";

			string filePath = Path.Combine(Path.GetTempPath(), $"{guid}.{format}");

			if (!File.Exists(filePath))
			{
                return Error($"file {guid} with format {format} not found");
            }

            var memStream = new MemoryStream();

			using (FileStream fileStream = File.OpenRead(filePath))
			{
					memStream.SetLength(fileStream.Length);
					fileStream.Read(memStream.GetBuffer(), 0, (int) fileStream.Length);
			}

			WebOperationContext currentWebContext = WebOperationContext.Current;
			currentWebContext.OutgoingResponse.ContentLength = memStream.Length;
			currentWebContext.OutgoingResponse.Headers.Add("Content-Length", memStream.Length.ToString());

			string niceName = guid;
			string[] parts = guid.Split('-');

			if (parts.Length == 2)
			{
				niceName = parts[1];
			}

            currentWebContext.OutgoingResponse.Headers.Add("Content-Disposition", $"attachment; filename=\"{niceName}.{format}\"");

            if (savFileFlag)
			{
				currentWebContext.OutgoingResponse.ContentType = "application/octet-stream";
			}
			else
			{
			    if (format == "pdf")
			    {
                    currentWebContext.OutgoingResponse.ContentType = "application/pdf";
                }
                else if (format == "xml")
                {
                    currentWebContext.OutgoingResponse.ContentType = "text/xml";
                }
                else if (format == "json")
                {
                    currentWebContext.OutgoingResponse.ContentType = "application/json";
                }

            }

			return memStream;
		}

		[OperationContract]
		[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, UriTemplate = "GetReportByEgrid/{project}/{language}/{format}/{value}")]
		public Stream GetReportByEgrid(string project, string language, string format, string value)
		{
			if (!new List<string> { "pdf", "xml", "json" }.Contains(format))
			{
				return Error("this format is not supported");
			}

			if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(language) || string.IsNullOrEmpty(value))
			{
				return Error("the project, language or the value is not valid");
			}

            if (language.ToLower() != "de")
		    {
                return Error("only language de is supported");
		    }

			var regex = new Regex(@"^CH(\d){12}");

			if (value.Length!=14 || !regex.IsMatch(value))
			{
				return Error("no valid egrid value");
			}

			project = project.Replace("_", "/");

			var status = Catalog.Catalog.LoadAppDefinition(project);

		    if (!status.Successful)
		    {
                return Error("intial catalog fail");
		    }

		    var application = Global.Applications.FirstOrDefault(x => x.Project == project);

		    if (application == null)
		    {
                return Error(string.Format("application from project {0} not found", project));		        
		    }

		    var function = application.Resources.FirstOrDefault(x => x.Type == "Function_OerebAnalysis");

		    if (function == null)
		    {
                return Error(string.Format("oereb analysis is not configured in project {0}", project));
            }

            var selectionLayer = application.Resources.FirstOrDefault(x => x.Type == "Selectionlayer");

            if (selectionLayer == null)
		    {
                return Error(string.Format("selectionlayer not found in project {0}", project));		        
		    }

		    var selectionDataset = Catalog.Catalog.GetDataSet(selectionLayer.Guid);

		    if (selectionDataset == null)
		    {
		        return Error(string.Format("selectiondataset not found in project {0}", project));
		    }

		    var selectionAdaptor = selectionDataset.GetDataUtilities().LastOrDefault(x=> (x as IGADataAdaptor) != null);

            if (selectionAdaptor == null)
		    {
		        return Error(string.Format("selectionAdaptor not found in project {0}", project));
		    }

		    var dataAdaptor = selectionAdaptor as IGADataAdaptor;

		    var attributeSpec = dataAdaptor.GaClass.AttributeSpecs.FirstOrDefault(x => x.Name.ToUpper() == "EGRIS_EGRID");

		    if (attributeSpec == null)
		    {
		        return Error(string.Format("attribute EGRIS_EGRID not found in project {0}", project));
		    }

            var filter = new GAFilter();
            filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, attributeSpec.Name, AttributeOperator.Like, value, typeof(string)));

            var inputObject = Resolver.ResolveGAObjects(new List<ResolverSkeleton> { new ResolverSkeleton(selectionDataset, filter) }).FirstOrDefault();

            if (inputObject == null)
            {
                return Error("no object found");
            }

            string[] baseLayers = application.Resources.Where(x => x.Type == "Baselayer").Select(x => x.Guid.ToString()).ToArray();

            MergerRequest mergerRequest = new MergerRequest(application.Guid.ToString(), function.Guid.ToString(), false, baseLayers, format, project);

		    var idField = selectionAdaptor.GaClass.ObjectIdFieldName;

            var filterObjectId = new GAFilter();
            filterObjectId.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, idField, AttributeOperator.In, (int)inputObject[inputObject.GAClass.ObjectIdFieldName], typeof(int)));

            mergerRequest.Selections.Add(new MergerRequest.JsonSelection()
            {
                classId = selectionLayer.Guid, 
                gaFilter = filterObjectId
            });

		    //var ser = Newtonsoft.Json.JsonConvert.SerializeObject(mergerRequest); //for debugging DES

            var module = GetMergerModule(mergerRequest.ModuleId);

			if (module == null)
			{
				return Error("module not found");
			}

			var gAReport = module.Process(mergerRequest);

			if (gAReport.HasError)
			{
				return Error("the reporting has an error");
			}

            return Export(mergerRequest, gAReport, format == "pdf" ? "pdfA1a" : "");
		}

		#region private

        #region Merger Modules Directory

        private static Dictionary<Guid, Type> _mergerModules = new Dictionary<Guid, Type>();
        private static Dictionary<Guid, Type> _exportModules = new Dictionary<Guid, Type>();

        private void RefreshModuleDirectory()
        {
            // refresh if not populated
            if (!_mergerModules.Any())
            {
                var mergerModuleTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .ToList()
                    .Where(x => x.FullName.StartsWith("Geocentrale.Apps.Server")) //filter !!
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(IMergerModule).IsAssignableFrom(p) && p.IsClass);

                foreach (Type mergerModuleType in mergerModuleTypes)
                {
                    var moduleGuidAttr = Attribute.GetCustomAttributes(mergerModuleType).Where(x => x is ModuleGuid).Cast<ModuleGuid>().SingleOrDefault();
                    if (moduleGuidAttr != null)
                    {
                        _mergerModules[moduleGuidAttr.Value] = mergerModuleType;
                    }
                }
            }
            if (!_exportModules.Any())
            {
                var exportModuleTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .ToList()
                    .Where(x => x.FullName.StartsWith("Geocentrale.Apps.Server")) //filter !!
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(IExportModule).IsAssignableFrom(p) && p.IsClass);

                foreach (Type exportModuleType in exportModuleTypes)
                {
                    var exportGuidAttr = Attribute.GetCustomAttributes(exportModuleType).Where(x => x is ModuleGuid).Cast<ModuleGuid>().SingleOrDefault();
                    if (exportGuidAttr != null)
                    {
                        _exportModules[exportGuidAttr.Value] = exportModuleType;
                    }
                }
            }
        }

        public IMergerModule GetMergerModule(Guid guid)
        {
            RefreshModuleDirectory();
            if (_mergerModules.ContainsKey(guid))
            {
                return Activator.CreateInstance(_mergerModules[guid]) as IMergerModule;
            }
            else
            {
                return null;
            }
        }

        public IExportModule GetExportModule(Guid guid)
        {
            RefreshModuleDirectory();
            if (_exportModules.ContainsKey(guid))
            {
                return Activator.CreateInstance(_exportModules[guid]) as IExportModule;
            }
            else
            {
                return null;
            }
        }
        #endregion

		private Stream GetStreamFromString(string response,string contentType)
		{
			WebOperationContext CurrentWebContext = WebOperationContext.Current;
			CurrentWebContext.OutgoingResponse.ContentType = contentType;
			return new MemoryStream(Encoding.UTF8.GetBytes(response));
		}

        private Stream Error(string description)
        {
            OutgoingWebResponseContext response = WebOperationContext.Current.OutgoingResponse;
            response.StatusCode = HttpStatusCode.NotFound;
            response.StatusDescription = description;
            return new MemoryStream();
        }

        private Stream Export(MergerRequest mergerRequest, GAReport gAReport, string format = "")
        {
            var exportModule = GetExportModule(mergerRequest.ModuleId);

            if (exportModule == null)
            {
                return GetStreamFromString(JsonConvert.SerializeObject(new { HasError = true, Error = "Export module not found" }), "application/json");
            }

            if (string.IsNullOrEmpty(format))
            {
                format = mergerRequest.Format.ToLower();
            }

            // Exportmodule: html, xml, pdf, pdfA1, json

            WebOperationContext currentWebContext = WebOperationContext.Current;
            
            if (currentWebContext == null)
            {
                return Error("no current web context exists");
            }

            try
            {
                byte[] content = null;

                switch (format)
                {
                    case "html":

                        var htmlContent = exportModule.ToHtml(mergerRequest, gAReport);

                        currentWebContext.OutgoingResponse.ContentType = "text/html";
                        content = Encoding.UTF8.GetBytes((string)htmlContent.Value);
                        currentWebContext.OutgoingResponse.ContentLength = content.Length;
                        currentWebContext.OutgoingResponse.Headers.Add("Content-Length", content.Length.ToString());
                        return new MemoryStream(content);

                    case "xml":

                        var resultXml = exportModule.ToXml(mergerRequest, gAReport);

                        currentWebContext.OutgoingResponse.ContentType = "text/xml";
                        content = Encoding.Unicode.GetBytes(resultXml); //UTF16
                        currentWebContext.OutgoingResponse.ContentLength = content.Length;
                        currentWebContext.OutgoingResponse.Headers.Add("Content-Length", content.Length.ToString());
                        return new MemoryStream(content);

                    case "pdf":

                        content = exportModule.ToPdf(mergerRequest, gAReport);

                        currentWebContext.OutgoingResponse.ContentType = "application/pdf";
                        currentWebContext.OutgoingResponse.ContentLength = content.Length;
                        currentWebContext.OutgoingResponse.Headers.Add("Content-Length", content.Length.ToString());
                        currentWebContext.OutgoingResponse.Headers.Add("Content-Disposition", "inline; filename=export.pdf");
                        return new MemoryStream(content);

                    case "pdfA1a":

                        content = exportModule.ToPdfA1(mergerRequest, gAReport);

                        currentWebContext.OutgoingResponse.ContentType = "application/pdf";
                        currentWebContext.OutgoingResponse.ContentLength = content.Length;
                        currentWebContext.OutgoingResponse.Headers.Add("Content-Length", content.Length.ToString());
                        currentWebContext.OutgoingResponse.Headers.Add("Content-Disposition", "inline; filename=export.pdf");
                        return new MemoryStream(content);

                    case "json":
                    default:

                        var jsonContent = exportModule.ToJson(mergerRequest, gAReport);

                        currentWebContext.OutgoingResponse.ContentType = "application/json";
                        byte[] contentJson = Encoding.UTF8.GetBytes(jsonContent); //UTF8
                        currentWebContext.OutgoingResponse.ContentLength = contentJson.Length;
                        currentWebContext.OutgoingResponse.Headers.Add("Content-Length", contentJson.Length.ToString());
                        return new MemoryStream(contentJson);
                }
            }
            catch (Exception ex)
            {
                var msg = "error creating pdfa1a " + ex.Message;
                log.Error(msg, ex);
                return Error(msg);
            }

        }

        /// <summary>
        /// Perform a deep Copy of the object, using Json as a serialisation method. NOTE: Private members are not cloned using this method.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T CloneObject<T>(T source)
        {
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        }

        #endregion

    }

}