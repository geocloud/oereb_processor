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
    
    public partial class ZustStelle
    {
        public ZustStelle()
        {
            this.OerebDefinition = new HashSet<OerebDefinition>();
            this.Rechtsnorm = new HashSet<Rechtsnorm>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Kanton { get; set; }
        public Nullable<int> Gemeinde { get; set; }
        public string Abkuerzung { get; set; }
        public string Source { get; set; }
        public string OriginalId { get; set; }
    
        public virtual ICollection<OerebDefinition> OerebDefinition { get; set; }
        public virtual ICollection<Rechtsnorm> Rechtsnorm { get; set; }
    }
}