//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Geocentrale.Apps.Db.Law
{
    using System;
    using System.Collections.Generic;
    
    public partial class OerebKThema
    {
        public OerebKThema()
        {
            this.OerebDefinition = new HashSet<OerebDefinition>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string Beschreibung { get; set; }
    
        public virtual ICollection<OerebDefinition> OerebDefinition { get; set; }
    }
}
