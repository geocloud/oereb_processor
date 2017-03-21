using System.Collections.Generic;
using Geocentrale.Apps.DataContracts;

namespace Geocentrale.Apps.Server.RuleEngine
{
    // TODO: document class andm members
    public class RuleEvaluatorResult
    {
        public List<GAObject> InvolvedObjects { get; set; }
        public List<GAObject> AssociatedObjects { get; set; }
        public string RuleExpression { get; set; }
        public string NiceRuleExpression { get; set; }
    }
}