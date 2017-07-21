using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.Apps.Server.Modules;
using Geocentrale.DataAdaptors.Resolver;

namespace Oereb.Service.Helper
{
    public class Report
    {
        public static GAReport Process(MergerRequest mergerRequest, Guid resourceSelectionGuid)
        {
            var oerebModule = new OerebModule();
            GAReport gAReport;

            if (mergerRequest.Selections.First().gaObject != null)
            {
                mergerRequest.QueryWithPseudoObject = true; //important
            }

            if (!WebApiApplication.ProcessedObjects.ContainsKey(mergerRequest.ProcessHash) || !mergerRequest.Cache)
            {
                var selection = mergerRequest.Selections.First();
                var inputDataSet = Catalog.GetDataSet(resourceSelectionGuid);

                if (selection.gaFilter != null)
                {
                    var inputObject = Resolver.ResolveGAObjects(new List<ResolverSkeleton> { new ResolverSkeleton(inputDataSet, selection.gaFilter) }).FirstOrDefault();
                    mergerRequest.InputObject = inputObject;
                }
                else if (selection.gaObject != null)
                {
                    var geoClass = inputDataSet.GaClass as GAGeoClass;
                    var inputObject = new GAObject(geoClass);

                    var geometryAttribute = selection.gaObject.Attributes.FirstOrDefault(x => x.AttributeSpec.Name == geoClass.GeometryFieldName);

                    if (string.IsNullOrEmpty(geometryAttribute?.Value.ToString()))
                    {
                        return new GAReport(null, "")
                        {
                            HasError = true,
                            Error = $"geometry object is not valid"
                        };
                    }

                    inputObject.DsGuid = inputDataSet.Guid;
                    inputObject[geoClass.GeometryFieldName] = (string)geometryAttribute.Value;

                    if (geoClass.AttributeSpecs.First(x => x.Name == geoClass.ObjectIdFieldName).Type == typeof(Int32))
                    {
                        inputObject[geoClass.ObjectIdFieldName] = (Int32)0;
                    }
                    else if (geoClass.AttributeSpecs.First(x => x.Name == geoClass.ObjectIdFieldName).Type == typeof(Int64))
                    {
                        inputObject[geoClass.ObjectIdFieldName] = (Int64)0;
                    }

                    mergerRequest.InputObject = inputObject;
                }
                else
                {
                    return new GAReport(null, "")
                    {
                        HasError = true,
                        Error = $"geometry object is not valid"
                    };
                }

                gAReport = oerebModule.Process(mergerRequest);

                if (mergerRequest.Cache)
                {
                    WebApiApplication.ProcessedObjects.Add(mergerRequest.ProcessHash, new ProcessedObject()
                    {
                        GaReport = gAReport
                    });
                }
            }
            else
            {
                gAReport = WebApiApplication.ProcessedObjects[mergerRequest.ProcessHash].GaReport;
            }

            return gAReport;
        }
    }
}