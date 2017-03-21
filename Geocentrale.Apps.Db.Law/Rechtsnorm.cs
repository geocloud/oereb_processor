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
    
    public partial class Rechtsnorm
    {
        public Rechtsnorm()
        {
            this.OerebDefinition = new HashSet<OerebDefinition>();
            this.Children = new HashSet<Rechtsnorm>();
            this.Artikel = new HashSet<Artikel>();
        }
    
        public string LinkLexFindId { get; set; }
        public string LinkLexFindKtSysNr { get; set; }
        public string Titel { get; set; }
        public string OffiziellerTitel { get; set; }
        public string Abkuerzung { get; set; }
        public string OffizielleNr { get; set; }
        public string Kanton { get; set; }
        public Nullable<int> Gemeinde { get; set; }
        public byte[] DokumentBinary { get; set; }
        public int Id { get; set; }
        public string Url { get; set; }
        public System.DateTime PubliziertAb { get; set; }
        public Nullable<bool> IsLive { get; set; }
        public Nullable<System.DateTime> VisibilityDate { get; set; }
        public string ArtikelBezeichner { get; set; }
        public string Source { get; set; }
        public string OriginalId { get; set; }
    
        public virtual ICollection<OerebDefinition> OerebDefinition { get; set; }
        public virtual Rechtsstatus Rechtsstatus { get; set; }
        public virtual RechtsnormTyp RechtsnormTyp { get; set; }
        public virtual ZustStelle ZustStelle { get; set; }
        public virtual Rechtsnorm Parent { get; set; }
        public virtual ICollection<Rechtsnorm> Children { get; set; }
        public virtual ICollection<Artikel> Artikel { get; set; }
    }
}