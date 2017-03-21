using Geocentrale.Apps.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Geocentrale.Apps.Server
{
    public class Setting
    {
        public static readonly Guid GuidOereb = Guid.Parse("0dec20e8-82cb-47a5-98c2-ac515fad10b9");
        public static readonly Guid GuidRechtsnorm = Guid.Parse("e2826482-300b-4437-b525-4055b7304e56");
        public static readonly Guid GuidArtikel = Guid.Parse("1c715771-0f6f-4a5a-961a-92c93cb374ad");
        public static readonly Guid GuidOerebkThema = Guid.Parse("8c7bcb11-3791-479f-b8a7-f227f1751ff9");
        public static readonly Guid GuidZustStelle = Guid.Parse(" fd939ac3-94e5-4410-a789-223bf4972d78");

        public const string EmptyPng = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsIAAA7CARUoSoAAAAAadEVYdFNvZnR3YXJlAFBhaW50Lk5FVCB2My41LjExR/NCNwAAAA1JREFUGFdj+P//PwMACPwC/ohfBuAAAAAASUVORK5CYII=";

        public const string Legislation = "Rechtsvorschrift";
        public const string LegalBasis = "Gesetzliche Grundlage";

        public const string DefaultMimeType = "image/png";

        public static double Inch = 25.40005080010160020; //mm

        public static readonly string GeometryPartAttributeName = "GAGeometryPart";
        public static readonly string GeometryPartValueAttributeName = "GAGeometryPartValue";
        public static readonly string GeometryPartPercentageAttributeName = "GAGeometryPartPercentage";
        public static readonly string GeometryPartGeometryTypeAttributeName = "GAGeometryPartGeometryType";
        public static readonly string GeometryBufferAttributeName = "GAGeometryBuffer";
        public static readonly string RuleExpressionAttributeName = "GARuleExpression";
        public static readonly string NiceRuleExpressionAttributeName = "GANiceRuleExpression";

        public static readonly string DistanceAbstractionRule = "distanceAbstractionRule";
        public static readonly string StatisticalBufferInputobject = "statisticalBufferInputobject";
        public static readonly string SliverTolerance = "sliverTolerance";

        public static Dictionary<string, GAAttributeSpec> ConstAttributeSpecs = new Dictionary<string, GAAttributeSpec>()
        {
            {"GAGeometryPart", new GAAttributeSpec(998,"GAGeometryPart","GAGeometryPart",typeof(string))},
            {"GAGeometryPartValue", new GAAttributeSpec(997,"GAGeometryPartValue","GAGeometryPartValue",typeof(double))},
            {"GAGeometryPartPercentage", new GAAttributeSpec(996,"GAGeometryPartPercentage","GAGeometryPartPercentage",typeof(double))},
            {"GAGeometryPartGeometryType", new GAAttributeSpec(992,"GAGeometryPartGeometryType","GAGeometryPartGeometryType",typeof(string))},
            {"GAGeometryBuffer", new GAAttributeSpec(995,"GAGeometryBuffer","GAGeometryBuffer",typeof(string))},
            {"GARuleExpression", new GAAttributeSpec(994,"GARuleExpression","GARuleExpression",typeof(string))},
            {"GANiceRuleExpression", new GAAttributeSpec(993,"GANiceRuleExpression","GANiceRuleExpression",typeof(string))},
        };
    }
}