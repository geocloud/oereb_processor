using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.DataContracts.Filter;
using Geocentrale.Apps.Server.Adapters;
using Geocentrale.Apps.Server.Catalog;
using Geocentrale.Apps.Server.Common;
using Geocentrale.Apps.Server.RuleEngine;
using Geocentrale.DataAdaptors.ArcgisServerRestAdaptor;
using Geocentrale.DataAdaptors.Contracts;
using Geocentrale.DataAdaptors.Resolver;
using Geocentrale.Filter;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Geocentrale.Apps.Server.Modules
{
    // TODO: document methdos
    [ModuleGuid("78833cf2-2d5e-42f7-b38e-2099c266e4de")]
    public class OerebModule : IMergerModule
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region properties, variables

        private MergerRequest _mergerRequest;

        private static ScalarServiceAccess _scalarServiceAccess = new ScalarServiceAccess();
        private static readonly Guid _oerebGuid = Guid.Parse("0dec20e8-82cb-47a5-98c2-ac515fad10b9");
        private static readonly Guid _rechtsnormGuid = Guid.Parse("e2826482-300b-4437-b525-4055b7304e56");
        private static readonly Guid _artikelGuid = Guid.Parse("1c715771-0f6f-4a5a-961a-92c93cb374ad");
        private static readonly Guid _oerebkThemaGuid = Guid.Parse("8c7bcb11-3791-479f-b8a7-f227f1751ff9");
        private static readonly Guid _zustStelleGuid = Guid.Parse(" fd939ac3-94e5-4410-a789-223bf4972d78");

        private const int _inputTypeFilter = 0;

        private GAReportDefinition _reportDefinition
        {
            get
            {
                List<Guid> classGuids = new List<Guid>();
                if (_mergerRequest != null)
                {
                    classGuids = Catalog.Catalog.GetAllDataSets(_mergerRequest.AppId).Select(x => x.Guid).ToList();
                }

                return new GAReportDefinition("OEREBK Auswertung", Guid.Parse("792a4304-6938-438a-8205-3bbdffee6d1b"), classGuids, 0, new List<string> { "adminMode", "baseLayers" });
            }
        }

        #endregion

        public GAReport Process(MergerRequest mergerRequest)
        {
            log.Debug("****** Process oerebModule is starting");

            //this part is deprecated, the inputObject has to be created outside

            _mergerRequest = mergerRequest;

            var inputObject = mergerRequest.InputObject;
            var involvedObjectsBypassed = mergerRequest.InvolvedObjects;

            if (inputObject == null)
            {                
                if (mergerRequest.Selections.Count != 1)
                {
                    return GetErrorReport($"no or more than one selections, {DumpObject(mergerRequest)}");
                }
                var selection = mergerRequest.Selections.First();

                Guid selectionGuid = selection.classId; //todo rename classId in mergerRequest
                double selectionBuffer = selection.buffer;

                var inputDataSet = Catalog.Catalog.GetDataSet(selectionGuid);

                if (inputDataSet == null)
                {
                    return GetErrorReport($"input selection class not found or no geoDataSet, {DumpObject(mergerRequest)}");
                }

                if (selection.gaFilter == null || selection.gaFilter.Expression.ValueExpressions.Count != 1 || selection.gaFilter.Expression.ValueExpressions.First().Values.Count == 0)
                {
                    return GetErrorReport($"input selection, filter is not valid, {DumpObject(mergerRequest)}");
                }

                if (selection.gaFilter.Expression.ValueExpressions.First().AttributeOperator ==  AttributeOperator.Intersect)
                {
                    var valueExpression = selection.gaFilter.Expression.ValueExpressions.First();
                    var geometry = valueExpression.Values.First().Value.ToString().ToLower();

                    //geometry-selection, create a virtual object, which is not in the datasource

                    if (!geometry.Contains("polygon")) //TODO && !IsValid(selectionGeometry)
                    {
                        return GetErrorReport($"input selection, geometry is not a polygon or invalid, {DumpObject(mergerRequest)}");
                    }

                    var geoClass = inputDataSet.GaClass as GAGeoClass;
                    inputObject = new GAObject(geoClass);
                    inputObject.DsGuid = inputDataSet.Guid;
                    inputObject[geoClass.GeometryFieldName] = (string)geometry;
                    inputObject[geoClass.ObjectIdFieldName] = (Int64)0;

                    mergerRequest.QueryWithPseudoObject = true; //important
                }
                else
                {
                    //filterselection, TODO has to be more generic

                    //TODO are this two rows necessary for the client
                    //var value = int.Parse(selection.gaFilter.Expression.ValueExpressions.First().Values.First().ToString());
                    //selection.gaFilter.Expression.ValueExpressions.First().Values = new List<object> {value};

                    inputObject = Resolver.ResolveGAObjects(new List<ResolverSkeleton> { new ResolverSkeleton(inputDataSet, selection.gaFilter) }).FirstOrDefault();
                }

                if (inputObject == null)
                {
                    return GetErrorReport("no valid input object");
                }

                log.Debug("****** inputobject is ready");
            }

            //******************************************************************************************************************
            //get involved objects (preselection of objects), layerpuncher

            var allDataSets = Catalog.Catalog.GetAllDataSets(_mergerRequest.AppId);
            var ruleEvaluatorResults = new List<RuleEvaluatorResult>();
            var inputObjectGuidDs = Catalog.Catalog.GetDataSetFromClass(inputObject.GAClass.Guid);

            var filteredDatasets = allDataSets.Where(x => !x.Guid.Equals(inputObjectGuidDs) && Catalog.Catalog.IsLayerDataSet(x.Guid)); //exclude input dataset (intersection to itself is not wise) and baselayers
            var objectSkeletons = new List<ResolverSkeleton>();

            log.Debug("*** layerpuncher start");

            foreach (var dataset in filteredDatasets)
            {
                var gaGeoClass = dataset.GaClass as GAGeoClass;

                if (gaGeoClass == null)
                {
                    log.Warn($"layerpuncher, only geodatasets allowed, {dataset.Guid}");
                    continue;
                }

                var geometryFieldInput = (inputObject.GAClass as GAGeoClass).GeometryFieldName;
                //var geometryValueInput = inputObject[geometryFieldInput];
                var geometryValueInput = Common.Geometry.BufferWkt(inputObject[geometryFieldInput], Convert.ToDouble(ConfigurationManager.AppSettings["distanceAbstractionRule"]));

                //TODO geometryValueInput = Geometry.Common.GetExtent(geometryValueInput); //simply for faster queries
                //TODO geometryValueInput = Geometry.Common.GetBuffer(geometryValueInput, selectionBuffer);

                var filter = new GAFilter();
                filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, gaGeoClass.GeometryFieldName, AttributeOperator.Intersect, geometryValueInput, typeof(string)));

                objectSkeletons.Add(new ResolverSkeleton(dataset, filter));
            }

            log.Debug("****** resolve objects start");

            var involvedObjects = involvedObjectsBypassed ?? new List<GAObject>();

            //log.Debug(JsonConvert.SerializeObject(objectSkeletons));

            try
            {
                involvedObjects.AddRange(Resolver.ResolveGAObjects(objectSkeletons));
            }
            catch (Exception ex)
            {
                log.Error("error resolving: {0}", ex);
            }

            log.Debug("****** resolve objects end");

            if (!involvedObjects.Any())
            {
                return GetErrorReport(string.Format("no involdved objects found, {0}", DumpObject(mergerRequest)));
            }

            involvedObjects.Add(inputObject); //add inputObject again for the rule engine

            //var typeofObjects = involvedObjects.Select(x => $"{x.GAClass.Guid}: {(x.GAClass as GAGeoClass).Layername}").Distinct().ToList();

            //var involvedObjectsSER = JsonConvert.SerializeObject(involvedObjects.First(), Formatting.Indented, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All });
            //log.Debug(involvedObjectsSER);

            //******************************************************************************************************************
            //rule evaluation

            log.Debug(string.Format("****** rule elevation start with <{0}> with involved objects", involvedObjects.Count));

            var ruleEvaluator = new RuleEvaluator();
            var ruleEvaluatorResult = ruleEvaluator.TestAllRules(involvedObjects, mergerRequest.Selections.First().buffer);

            ruleEvaluatorResults.AddRange(ruleEvaluatorResult);

            log.Debug(string.Format("count of elevated objects {0}", ruleEvaluatorResult.Count));
            log.Debug("****** rule elevation end");

            log.Debug("****** create report object start");

            //todo implement good readable log entry for the report (helps for debugging a wrong result)

            /*
             
            if (log.IsDebugEnabled)
            {
                log.Debug(DumpObject(ruleEvaluatorResults));

                var dump1 = "Neue Oereb Auswertung\n\n";
                var dump2 = "\nID;Index;AttributeValues;WKT\n";
                long i = 0;

                dump1 += string.Format("\nruleEvaluatorResults.Count: {0}\n", ruleEvaluatorResults.Count);

                foreach (var ruleEvaluatorResult in ruleEvaluatorResults)
                {
                    i++;

                    dump1 += string.Format("Count(Ids) Involv. {1}({3}), Count(Ids) Assoz. {2}({4}), Expression {0}\n",
                        ruleEvaluatorResult.RuleExpression,
                        ruleEvaluatorResult.InvolvedObjects.Count,
                        ruleEvaluatorResult.AssociatedObjects.Count,
                        string.Join(",", ruleEvaluatorResult.InvolvedObjects.Select(x => x[x.GAClass.ObjectIdFieldName])),
                        string.Join(",", ruleEvaluatorResult.AssociatedObjects.Select(x => x[x.GAClass.ObjectIdFieldName])));

                    foreach (var involvedObject in ruleEvaluatorResult.InvolvedObjects)
                    {
                        dump2 += DumpToCsv(involvedObject, i);
                    }
                }

                log.Debug(dump1);
                log.Debug(dump2);                
            }
             
            */

            var app = Global.Applications.FirstOrDefault(x => x.Guid == mergerRequest.AppId);
            var topicsExcluded = new List<ApplicationTopic>();

            if (app != null)
            {
                topicsExcluded = app.Topics.Where(x => x.Exclude).ToList();
            }

            // Build result Nodetree
            // GAReport.Result
            //      Grundstück (Input Object)
            //          OerebKThema (via GetAll())
            //              OerebDef (Associated Object)
            //                  Schnittgeometrien (Involved Objects)
            //                  Rechtsnormen (inkl. Parents)
            //                  Artikel (inkl. Parents)

            List<GATreeNode<GAObject>> res = new List<GATreeNode<GAObject>>();

            GATreeNode<GAObject> inObjectNode = new GATreeNode<GAObject>(inputObject); // = grundstück
            Guid inObjectClassId = inputObject.GAClass.Guid;
            var evalResultsForThisInObject = ruleEvaluatorResults.Where(x => x.InvolvedObjects.Any(y => y.GAClass.Guid == inObjectClassId && inputObject[inputObject.GAClass.ObjectIdFieldName] == y[y.GAClass.ObjectIdFieldName])).ToList();
            foreach (GAObject oerebKThema in GetOerebKThemen())
            {
                //if (!app.Topics.Select(x => x.Id).ToList().Contains(oerebKThema["Beschreibung"].ToString()))
                //{
                //    continue;
                //}

                if (topicsExcluded.Any(x => x.Id == oerebKThema["Beschreibung"].ToString()))
                {
                    continue;
                }

                GATreeNode<GAObject> oerebKThemaNode = new GATreeNode<GAObject>(oerebKThema); // = oerebk thema
                int oerebKThemaId = oerebKThema["Id"];
                var evalResultsForThisThema = evalResultsForThisInObject.Where(x => x.AssociatedObjects.Select(y => y["OerebKThema.Id"]).Contains(oerebKThemaId));
                foreach (var evalResult in evalResultsForThisThema)
                {
                    foreach (var associatedObject in evalResult.AssociatedObjects)
                    {
                        GATreeNode<GAObject> associatedObjectNode = new GATreeNode<GAObject>(associatedObject); // = oereb def
                        foreach (var involvedObject in evalResult.InvolvedObjects.Where(x => x.GAClass.Guid != inObjectClassId))
                        {
                            var involvedProcessedObject = Geometry.FeatureIntersectCalculation(inputObject, involvedObject).Value as GAObject;
                            InsertRuleExpressionAttributes(evalResult, involvedProcessedObject); // add rule expression attribute for admin mode
                            GATreeNode<GAObject> involvedObjectNode = new GATreeNode<GAObject>(involvedProcessedObject); // = schnittgeometrien
                            associatedObjectNode.Children.Add(involvedObjectNode);
                            var relatedObjects = GetRelatedObjects(involvedObject);
                            if (relatedObjects.Any())
                            {
                                associatedObjectNode.Children.AddRange(relatedObjects.Select(relatedObject => new GATreeNode<GAObject>(relatedObject)));
                            }
                        }
                        List<GAObject> rechtsnormen = GetRechtsnormenForOerebDef(associatedObject);
                        rechtsnormen.ForEach(x => associatedObjectNode.AddChild(x)); // = rechtsnormen / artikel
                        UpdateOerebKThemaStatus(oerebKThemaNode.Value);
                        oerebKThemaNode.Children.Add(associatedObjectNode);
                    }
                }
                inObjectNode.Children.Add(oerebKThemaNode);
            }
            res.Add(inObjectNode);

            var gAReport = new GAReport($"OEREBK Auswertung;{involvedObjects.Count};{ruleEvaluatorResult.Count}", _reportDefinition, res);

            //log.Debug(DumpObject(gAReport));
 
            log.Debug("****** create report object end");

            return gAReport;
        }

        #region private methods

        private GAReport GetErrorReport(string errorMessage)
        {
            var logMsg = errorMessage;
            if (logMsg.Length > 500)
                logMsg = logMsg.Substring(0,500) + "...";

            log.Error(logMsg.Replace("\\n","").Replace("\\r",""));
            return new GAReport(_reportDefinition, errorMessage);
        }

        private string DumpObject(object value)
        {
            // TODO: enable by config, this fills logfile
            return JsonConvert.SerializeObject(value, Formatting.Indented, new StringEnumConverter());
        }

        private string DumpToCsv(GAObject gaObject, long i)
        {
            var gaGeoClass = gaObject.GAClass as GAGeoClass;
            var geometry = string.Empty;
            var geometryField = string.Empty;

            if (gaGeoClass != null)
            {
                geometryField = gaGeoClass.GeometryFieldName;
                geometry = gaObject[geometryField];
            }

            var idField = gaObject.GAClass.ObjectIdFieldName;

            string id = gaObject[idField].ToString();

            var attributeValues = string.Join(" | ", gaObject.Attributes.Where(x => x.AttributeSpec.Name != geometryField && x.AttributeSpec.Name != idField).Select(x => string.Format("{0}:{1}", x.AttributeSpec.Name, x.Value)));

            return string.Format("{0};{3};\"{1}\";\"{2}\"\n", id, attributeValues, geometry,i);
        }

        private void InsertRuleExpressionAttributes(RuleEvaluatorResult evalResult, GAObject involvedObject)
        {
            if (!involvedObject.GAClass.AttributeSpecs.Any(x => x.Name == Geometry.RuleExpressionAttributeName))
            {
                involvedObject.Attributes.Add(new GAAttribute(Geometry.ConstAttributeSpecs[Geometry.RuleExpressionAttributeName], evalResult.RuleExpression));
            }
            if (!involvedObject.GAClass.AttributeSpecs.Any(x => x.Name == Geometry.NiceRuleExpressionAttributeName))
            {
                involvedObject.Attributes.Add(new GAAttribute(Geometry.ConstAttributeSpecs[Geometry.NiceRuleExpressionAttributeName], evalResult.NiceRuleExpression));
            }
        }

        private void UpdateOerebKThemaStatus(GAObject gAObject)
        {
            int? currentStatus = gAObject["Status"];
            if (currentStatus == null)
            {
                return;
            }
            gAObject["Status"] = 2;
        }

        private GAObject GetParentRechtsnorm(GAObject child)
        {
            int? parentId = null;

            if (child.Attributes.Any(x => x.AttributeSpec.Name == "Parent.Id"))
            {
                parentId = child["Parent.Id"] as int?; // Rechtsnorm
            }
            else if (child.Attributes.Any(x => x.AttributeSpec.Name == "Rechtsnorm.Parent.Id"))
            {
                parentId = child["Rechtsnorm.Parent.Id"] as int?; // Artikel
            }

            //int? parentId = child["Parent.Id"] as int?; // Rechtsnorm
            //if (parentId == null)
            //{
            //    parentId = child["Rechtsnorm.Parent.Id"] as int?; // Artikel
            //}

            if (parentId == null || parentId == 0)
            {
                return null;
            }

            var parentRechtsnorm = _scalarServiceAccess.GetById(Global.ScalarClasses, _rechtsnormGuid, new List<dynamic> { parentId }).FirstOrDefault();
            return parentRechtsnorm;
        }

        private List<GAObject> GetRechtsnormenForOerebDef(GAObject oerebDef)
        {
            var res = new List<GAObject>();
            // Rechtsnormen
            var rechtsnormIds = oerebDef["RechtsnormIds"] as List<int>;
            if (rechtsnormIds != null && rechtsnormIds.Any())
            {
                var rechtsnormen = _scalarServiceAccess.GetById(Global.ScalarClasses, _rechtsnormGuid, rechtsnormIds.Select(x => (dynamic)x).ToList());
                foreach (var rechtsnorm in rechtsnormen)
                {
                    GAObject child = rechtsnorm;
                    while (child != null)
                    {
                        res.Insert(0, child);
                        child = GetParentRechtsnorm(child);
                    }
                }
            }
            // Artikel
            var artikelIds = oerebDef["ArtikelIds"] as List<int>;
            if (artikelIds != null && artikelIds.Any())
            {
                var artikel = _scalarServiceAccess.GetById(Global.ScalarClasses, _artikelGuid, artikelIds.Select(x => (dynamic)x).ToList());
                foreach (var artikelItem in artikel)
                {
                    GAObject child = artikelItem;
                    while (child != null)
                    {
                        res.Insert(0, child);
                        child = GetParentRechtsnorm(child);
                    }
                }
            }
            return res;
        }

        private List<GAObject> GetOerebKThemen()
        {
            return _scalarServiceAccess.GetAll(Global.ScalarClasses, _oerebkThemaGuid);
        }

        private List<GAObject> GetRelatedObjects(GAObject gAObject)
        {
            var relatedObjects = new List<GAObject>();
            var gaGeoClass = gAObject.GAClass as GAGeoClass;

            if (gaGeoClass == null)
            {
                log.Error("GetRelatedObjects: gaClass is null");
                return relatedObjects;
            }

            var dataset = Catalog.Catalog.GetDataSetFromClass(gaGeoClass.Guid);
            var oerebDataAdaptor = dataset.GetDataUtilities().Last() as OerebAdaptor; // TODO: confusing with nullcheck on next line?

            if (dataset == null || oerebDataAdaptor == null)
            {
                log.Error("GetRelatedObjects: dataset is null or not a oereb adaptor");
                return relatedObjects;                
            }

            PropertyInfo propertyRelationType = oerebDataAdaptor.GetType().GetProperty("_relationType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo propertyRelationLayers = oerebDataAdaptor.GetType().GetProperty("_relationLayers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (propertyRelationType == null || propertyRelationLayers == null)
            {
                log.Error("GetRelatedObjects: property relationType or relationLayers does not exist");
                return relatedObjects;                                
            }

            var relationType = propertyRelationType.GetValue(oerebDataAdaptor) as string;
            var relationLayersStringArray = (propertyRelationLayers.GetValue(oerebDataAdaptor) as string ?? string.Empty).Split(';');
            var relationLayers = new List<Guid>();

            foreach (var relationLayer in relationLayersStringArray)
            {
                Guid layerGuid;
                bool success = Guid.TryParse(relationLayer, out layerGuid);

                if (success)
                {
                    relationLayers.Add(layerGuid);
                }
            }

            if (relationLayers.Count == 0 || relationType != "geometry")
            {
                log.Debug("GetRelatedObjects: no relationlayer exist or the relationtype is not geometry");
                return relatedObjects;
            }

            var geometry = gAObject[gaGeoClass.GeometryFieldName];

            if (string.IsNullOrEmpty(geometry))
            {
                log.Error("GetRelatedObjects: geometry of object is empty");
                return relatedObjects;
            }

            var relatedDatasets = Catalog.Catalog.GetDataSets(relationLayers);
            var objectSkeletons = new List<ResolverSkeleton>();

            foreach (var relatedDataset in relatedDatasets)
            {
                var getDataAdaptor = relatedDataset.GetDataUtilities().Last() as IGAGeoDataAdaptor; // TODO: why last?

                if (getDataAdaptor == null)
                {
                    log.Error("GetRelatedObjects: relatedDataset is empty or no geoDataAdaptor");
                    continue;
                }

                var filter = new GAFilter();
                filter.Expression.ValueExpressions.Add(new GAValueExpression(BooleanOperator.And, getDataAdaptor.GaGeoClass.GeometryFieldName, AttributeOperator.Intersect, geometry, typeof(string)));

                objectSkeletons.Add(new ResolverSkeleton(relatedDataset, filter));
            }

            relatedObjects = Resolver.ResolveGAObjects(objectSkeletons);

            if (!relatedObjects.Any())
            {
                log.Debug(string.Format("GetRelatedObjects: no related objects found for geometry <{0}>", geometry)); 
            }

            return relatedObjects;
        }

        #endregion
    }
}