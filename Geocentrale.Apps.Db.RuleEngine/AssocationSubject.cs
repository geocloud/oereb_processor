//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Geocentrale.Apps.Db.RuleEngine
{
    using System;
    using System.Collections.Generic;
    
    public partial class AssocationSubject
    {
        public AssocationSubject()
        {
            this.Rules = new HashSet<Rule>();
        }
    
        public int Id { get; set; }
        public System.Guid GAClassGuid { get; set; }
        public string ObjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    
        public virtual ICollection<Rule> Rules { get; set; }
    }
}