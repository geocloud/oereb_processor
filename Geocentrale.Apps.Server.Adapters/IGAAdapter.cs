using Geocentrale.Apps.DataContracts;
using System;
using System.Collections.Generic;

namespace Geocentrale.Apps.Server.Adapters
{
    // TODO: document class andm members
    public interface IGAAdapter
    {
        List<ScalarClass> Identify();
        List<ScalarClass> Identify(List<Guid> classGuids);
        List<GAObject> GetAll(Dictionary<Guid, ScalarClass> scalarClasses, Guid classGuid);
        List<GAObject> GetById(Dictionary<Guid, ScalarClass> scalarClasses, Guid classGuid, List<dynamic> objectIds);
    }
}
