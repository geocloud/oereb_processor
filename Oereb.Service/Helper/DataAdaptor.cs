using Oereb.Service.DataContracts.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.DataContracts.Filter;
using Geocentrale.Common;
using Geocentrale.ConfigProcessor.GeoDataSet;
using Geocentrale.DataAdaptors.Contracts;
using Geocentrale.Filter;
using System.Net.Http;
using Oereb.Service.DataContracts;

namespace Oereb.Service.Helper
{
    public class DataAdaptor
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public enum FieldMapping
        {
            Egrid,
            Nbident,
            Number,
            Street,
            Housenumber,
            Postalcode,
            Shape,
            Type,
            KindOf
        }

        public static GAStatus<GetEGRIDResponseType> GetEgridFromDa(Dictionary<Guid, QueryObject> queryObjects, string extended)
        {
            var result = new GetEGRIDResponseType()
            {
                egrid = new string[] {},
                number = new string[] {},
                identDN = new string[] {}
            };

            var response = new GAStatus<GetEGRIDResponseType>(true,String.Empty,result);
            var values = new Dictionary<FieldMapping,List<string>>()
            {
                { FieldMapping.Egrid, new List<string>() },
                { FieldMapping.Nbident, new List<string>() },
                { FieldMapping.Number, new List<string>() },
                { FieldMapping.Shape, new List<string>() },
                { FieldMapping.KindOf, new List<string>() }
            };

            var status = new GAStatus(true);

            if (!queryObjects.Any())
            {
                response.Descriptions.Add($"no queryObjects available");
                response.Successful = true;
                return response;
            }

            if (!WebApiApplication.DataSetsSearch.Any())
            {
                status = LoadSearchCatalog();
            }

            if (!status.Successful)
            {
                response.Successful = false;
                response.Exceptions.AddRange(status.Exceptions);
                response.Descriptions.AddRange(status.Descriptions);
                return response;
            }

            foreach (var query in queryObjects)
            {
                if (!WebApiApplication.DataSetsSearch.ContainsKey(query.Key))
                {
                    response.Descriptions.Add($"Dataset not found {query.Key}");
                    response.Successful = false;
                    continue;
                }

                var dataadaptor = WebApiApplication.DataSetsSearch[query.Key].GetDataUtilities().Last() as IGAGeoDataAdaptor;

                if (dataadaptor == null)
                {
                    response.Descriptions.Add($"Dataadaptor from Dataset {query.Key} is empty");
                    response.Successful = false;
                    continue;
                }

                dataadaptor.GaFilter = query.Value.Filter;

                var resultQuery = dataadaptor.ReadData();

                if (!resultQuery.Successful)
                {
                    response.Descriptions.Add($"Query from Dataset {query.Key} was not successful");
                    response.Successful = false;
                    continue;
                }

                foreach (var gaobject in dataadaptor.GaObjects)
                {
                    values[FieldMapping.Egrid].Add(gaobject[query.Value.Mapping[FieldMapping.Egrid]] is GANull ? "" : gaobject[query.Value.Mapping[FieldMapping.Egrid]]);
                    values[FieldMapping.Nbident].Add(gaobject[query.Value.Mapping[FieldMapping.Nbident]] is GANull ? "" : gaobject[query.Value.Mapping[FieldMapping.Nbident]]);
                    values[FieldMapping.Number].Add(gaobject[query.Value.Mapping[FieldMapping.Number]] is GANull ? "" : gaobject[query.Value.Mapping[FieldMapping.Number]]);
                    values[FieldMapping.Shape].Add(gaobject[query.Value.Mapping[FieldMapping.Shape]] is GANull ? "" : gaobject[query.Value.Mapping[FieldMapping.Shape]]);
                    values[FieldMapping.KindOf].Add(gaobject[query.Value.Mapping[FieldMapping.KindOf]] is GANull ? "" : gaobject[query.Value.Mapping[FieldMapping.KindOf]]);
                }

                if (dataadaptor.GaObjects.Any())
                {
                    break;
                }
            }

            response.Value.egrid = values[FieldMapping.Egrid].ToArray();
            response.Value.identDN = values[FieldMapping.Nbident].ToArray();
            response.Value.number = values[FieldMapping.Number].ToArray();

            if (extended.ToLower() == "true")
            {
                var getEgridResponseTypeExtended = new GetEGRIDResponseTypeExtended()
                {
                    egrid = response.Value.egrid,
                    number = response.Value.number,
                    identDN = response.Value.identDN,
                    shape = values[FieldMapping.Shape].ToArray(),
                    kindOf = values[FieldMapping.KindOf].ToArray(),
                };

                response.Value = getEgridResponseTypeExtended;
            }

            return response;
        }

        public static GAStatus LoadSearchCatalog()
        {
            try
            {
                var file = Path.Combine(PathTasks.GetBinDirectory().Parent.FullName, "Config/geoservices.xml");
                var geoservies = XElement.Load(file);

                foreach (var geoservice in geoservies.XPathSelectElements("GeoDataSet"))
                {
                    var geoDataSet = ConfigGeoDataSet.GetGeoDataSet(geoservice, null);

                    if (WebApiApplication.DataSetsSearch.ContainsKey(geoDataSet.Guid))
                    {
                        continue;
                    }

                    WebApiApplication.DataSetsSearch.Add(geoDataSet.Guid, geoDataSet);
                }
            }
            catch (Exception ex)
            {
                return new GAStatus(false,String.Empty,ex);
            }

            return new GAStatus(true);
        }

        public static Dictionary<Guid, DataAdaptor.QueryObject> CreateFilter(int type, string value1, string value2, string value3, Point point, string filterCanton)
        {
            var config = WebApiApplication.Config;

            var queryObjects = new Dictionary<Guid, DataAdaptor.QueryObject>();

            foreach (var canton in config.Cantons)
            {
                if (!string.IsNullOrEmpty(filterCanton) && canton.Shorname != filterCanton)
                {
                    continue; //ignore
                }

                if (!Settings.AvailableCantonsLocal.Contains(canton.Shorname))
                {
                    continue;
                }

                if (canton.Search.DsGuid == Guid.Empty)
                {
                    //no search defined
                    continue;
                }

                var filter = new GAFilter();
                int postalode;

                if (type == 1 && int.TryParse(value1, out postalode))
                {
                    //search postalcode, street
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Postalcode, AttributeOperator.Equal, value1, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Street, AttributeOperator.Like, value2, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Type, AttributeOperator.Equal, 1, typeof(int)));
                }
                else if (type == 1)
                {
                    //search NBIdent and number
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Nbindent, AttributeOperator.Equal, value1, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Number, AttributeOperator.Equal, value2, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Type, AttributeOperator.Equal, 0, typeof(int)));
                }
                else if (type == 2)
                {
                    //search postalcode, street, housenumber
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Postalcode, AttributeOperator.Equal, value1, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Street, AttributeOperator.Equal, value2, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Housenumber, AttributeOperator.Equal, value3, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Type, AttributeOperator.Equal, 1, typeof(int)));
                }
                else if (type == 3)
                {
                    //search with point
                    var pointTransformed = Geocentrale.Common.Spatial.Transform.TransformPointFromEpsgToEpsg(point.X, point.Y, 0, point.Crs, canton.Search.Crs);

                    var pointTransformedForLength = Geocentrale.Common.Spatial.Transform.TransformPointFromEpsgToEpsg(point.X + point.Radius, point.Y, 0, point.Crs, canton.Search.Crs);

                    var radiusTransFormed = Math.Abs(pointTransformed[0] - pointTransformedForLength[0]);

                    var bufferGeometry = Geocentrale.Common.Spatial.Operation.Buffer($"POINT({pointTransformed[0]} {pointTransformed[1]})", radiusTransFormed, 20, 2);

                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Shape, AttributeOperator.Intersect, bufferGeometry, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Type, AttributeOperator.Equal, 0, typeof(int)));
                }
                else if (type == 4)
                {
                    //search egrid
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Egrid, AttributeOperator.Equal, value1, typeof(string)));
                    filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, canton.Search.Type, AttributeOperator.Equal, 0, typeof(int)));
                }

                var fieldMappings = new Dictionary<DataAdaptor.FieldMapping, string>()
                {
                    { DataAdaptor.FieldMapping.Egrid, canton.Search.Egrid} ,
                    { DataAdaptor.FieldMapping.Nbident, canton.Search.Nbindent},
                    { DataAdaptor.FieldMapping.Number, canton.Search.Number},
                    { DataAdaptor.FieldMapping.Shape, canton.Search.Shape},
                    { DataAdaptor.FieldMapping.KindOf, canton.Search.KindOf},
                };

                queryObjects.Add(canton.Search.DsGuid, new DataAdaptor.QueryObject()
                {
                    Filter = filter,
                    Mapping = fieldMappings
                });
            }

            return queryObjects;
        }

        public class QueryObject
        {
            public GAFilter Filter { get; set; }
            public Dictionary<FieldMapping, string> Mapping { get; set; }
        }

        public class Point
        {
            public HttpResponseMessage Response { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Radius { get; set; }
            public int Crs { get; set; }
        }

    }
}