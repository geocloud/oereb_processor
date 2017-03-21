using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Db.RuleEngine;
using Geocentrale.Apps.Server.Adapters;
using Geocentrale.Common;
using Geocentrale.DataAdaptors.RuleEngine;
using Geocentrale.DataAdaptors.RuleEngine.EvaluatorSpatial;
using log4net;

namespace Geocentrale.Apps.Server.RuleEngine
{
    // TODO: document class andm members
    public class RuleEvaluator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static ScalarServiceAccess _scalarServiceAccess = new ScalarServiceAccess();

        public List<RuleEvaluatorResult> TestAllRules(List<GAObject> gAObjects, double ruleOffset)
        {
            var ruleEvaluatorResults = new List<RuleEvaluatorResult>();

            using (var db = new RuleEngineContainer())
            {
                // eager load everything needed within the async task. otherwise we might have race condition when the
                // garbage collector cleans up the dbcontext before the task has finished and retrieves the associationsubject
                var rules = db.Rules.Include("AssociationSubjects").ToList();

                if (rules.Count == 0)
                {
                    log.Warn("no rules defined in db");
                    return ruleEvaluatorResults;
                }
                
                var datasetsClassGuids = gAObjects.Select(x => x.GAClass.Guid).Distinct().ToList();
                var dataSets = Catalog.Catalog.GetDataSetFromClasses(datasetsClassGuids);

                double sliverTolerance = ConfigAccessTask.GetAppSettingsDouble(Setting.SliverTolerance);

                var oerebEvaluatorEngine = new EvaluatorSpatial(ruleOffset, sliverTolerance, 6);
                var ruleExpressions = rules.Select(rule => new RuleExpression(rule.Expression, rule.Id, oerebEvaluatorEngine)).Where(x => x.ParameterIds.Count > 0).ToList(); //TODO take all rules ?? this is not scalable

                var ruleEngine = new DataAdaptors.RuleEngine.RuleEngine(gAObjects, ruleExpressions, oerebEvaluatorEngine);

                //var tests = ruleExpressions.Select(x => x.Expression).ToList();

                var evaluatedObjects = ruleEngine.Evaluate();

                if (!evaluatedObjects.Any())
                {
                    log.Debug("no objects evaluated");
                    return ruleEvaluatorResults;
                }

                foreach (var evaluatedObject in evaluatedObjects.Where(x=>x.ResultsAreTrue.Count > 0))
                {                   
                    var ruleRecordset = rules.FirstOrDefault(x => x.Id == evaluatedObject.RuleExpression.RuleId);

                    if (ruleRecordset == null)
                    {
                        log.Error("rule not found!"); //should not happen at any time
                        continue;
                    }

                    foreach (var ruleEvaluationResult in evaluatedObject.ResultsAreTrue)
                    {
                        var ruleEvaluatorResult = new RuleEvaluatorResult();

                        ruleEvaluatorResult.RuleExpression = evaluatedObject.RuleExpression.Expression;
                        ruleEvaluatorResult.NiceRuleExpression = evaluatedObject.RuleExpressionNice;

                        ruleEvaluatorResult.InvolvedObjects = ruleEvaluationResult.RuleObjects.Select(x => x.GaObject).ToList();

                        //*************************************************************************************************************
                        //append law's
                        //in a first step the relationship to the law's is working with the old Geocentrale.Apps.ServerAdaptors

                        var associatedObjects = new List<GAObject>();

                        foreach (var law in ruleRecordset.AssociationSubjects)
                        {
                            var lawGaObject = _scalarServiceAccess.GetById(Global.ScalarClasses, law.GAClassGuid, new List<dynamic> { (dynamic)law.ObjectId });
                            associatedObjects.AddRange(lawGaObject);
                        }

                        ruleEvaluatorResult.AssociatedObjects = associatedObjects;

                        ruleEvaluatorResults.Add(ruleEvaluatorResult);
                    }
                }

            }

            return ruleEvaluatorResults;
        }
    }
}
