using System;
using System.Collections.Generic;
using System.Linq;
using Geocentrale.Apps.DataContracts;

namespace Geocentrale.Apps.Server.Adapters
{
    public class ScalarServiceAccess
    {
        public List<GAObject> GetAll(Dictionary<Guid, ScalarClass> scalarClasses, Guid classGuid)
        {
            IGAAdapter adapter = GetAdapter(scalarClasses, classGuid);
            if (adapter != null)
            {
                return adapter.GetAll(scalarClasses, classGuid);
            }
            return null;
        }

        public List<GAObject> GetById(Dictionary<Guid, ScalarClass> scalarClasses, Guid classGuid, List<dynamic> objectIds)
        {
            IGAAdapter adapter = GetAdapter(scalarClasses, classGuid);
            if (adapter != null)
            {
                return adapter.GetById(scalarClasses, classGuid, objectIds);
            }
            return null;
        }

        private IGAAdapter GetAdapter(Dictionary<Guid, ScalarClass> scalarClasses, Guid classGuid)
        {
            var gAScalarClass = scalarClasses.FirstOrDefault(x => x.Key.Equals(classGuid)).Value;

            if (gAScalarClass != null && !String.IsNullOrEmpty(gAScalarClass.TypeName))
            {
                var type = Type.GetType(gAScalarClass.TypeName);
                if (type != null)
                {
                    return Activator.CreateInstance(type) as IGAAdapter;
                }
                return null;
            }
            return null;
        }
    }
}