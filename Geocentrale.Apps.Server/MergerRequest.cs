using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Common;
using Geocentrale.Filter;

namespace Geocentrale.Apps.Server
{
    // TODO: document class andm members
    [Serializable]
    public class MergerRequest
    {
        #region example request

        //deserialization example:
        //{
        //    "appId": "84a0d662-f319-42a0-ab9f-566c3a010b91",
        //    "moduleId": "78833cf2-2d5e-42f7-b38e-2099c266e4de",
        //    "moduleParameters": {
        //        "adminMode": true,
        //        "baseLayers": [
        //            "afd517f7-9b9b-4f10-838f-6d3873088191",
        //            "85e4f572-7bed-4bce-8837-ef628532410f"
        //        ]
        //    },
        //    "selections": [
        //        {
        //            "classId": "4ca34e64-47d3-4aad-bcf1-dcbe78db5924",
        //            "gaFilter": <gaFilter>,
        //            "buffer": 0
        //        },
        //        {
        //            "classId": "4ca34e64-47d3-4aad-bcf1-dcbe78db5924",
        //            "gaFilter": <gaFilter>,
        //            "buffer": 0
        //        }
        //    ],
        //    "format": "html"
        //}

        #endregion

        public Guid AppId { get; set; }                 //deprecated
        public Guid ModuleId { get; set; }              //deprecated
        public Dictionary<string, dynamic> ModuleParameters { get; set; }   //deprecated
        public List<JsonSelection> Selections { get; set; }

        public string Format { get; set; }
        public string ProcessHash { get; set; }
        public double DistanceAbstractionRule { get; set; }

        public bool QueryWithPseudoObject { get; set; }

        public bool IncludeMap { get; set; } = true;
        public bool IncludeDetail { get; set; } = true;
        public bool IncludeLogo { get; set; } = true;

        public bool ReportComplete { get; set; } = false;
        public bool ReportAppendixesAttached { get; set; } = false;

        public GAObject InputObject { get; set; }
        public List<GAObject> InvolvedObjects { get; set; }

        public string Canton { get; set; } = "";

        public bool Cache { get; set; } = true;

        public MergerRequest()
        {
            ModuleParameters = new Dictionary<string, dynamic>();
            QueryWithPseudoObject = false;
            InvolvedObjects = new List<GAObject>();
        }

        public MergerRequest(string appId, string moduleId,bool adminMode, string[] baseLayers, string format, string project)
        {
            AppId = GetGuid(appId);
            ModuleId = GetGuid(moduleId);
            Format = format;

            ModuleParameters = new Dictionary<string, dynamic>();
            ModuleParameters.Add("adminMode", adminMode);
            ModuleParameters.Add("project", project);

            Selections = new List<JsonSelection>();

            QueryWithPseudoObject = false;
        }

        [Serializable]
        public class JsonSelection
        {
            public Guid classId { get; set; } //deprecated
            public GAFilter gaFilter { get; set; }
            public GaObjectRaw gaObject { get; set; }
            public double buffer { get; set; }
        }

        public class GaObjectRaw
        {
            public List<GAAttribute> Attributes { get; set; }
        }
        
        #region private 

        private static Guid GetGuid(string value)
        {
            Guid guid;
            Guid.TryParse(value, out guid);
            return guid;
        }

        #endregion
    }
}