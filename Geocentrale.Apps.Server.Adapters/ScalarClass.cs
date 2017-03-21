using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.Linq;
using Geocentrale.Apps.DataContracts;

namespace Geocentrale.Apps.Server.Adapters
{
    //TODO: document it. what is this class exactly for?
    public class ScalarClass : GAClass
    {
        public string TypeName { get; set; } // used on the server side only

        public ScalarClass(string niceName, List<GAAttributeSpec> attributeSpecs, Guid guid, string objectIdFieldName, string displayField, string typeName) : base(niceName,attributeSpecs,guid,objectIdFieldName,displayField)
        {
            TypeName = typeName;
        }

        public ScalarClass()
        {
        }
    }
}