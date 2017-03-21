using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Geocentrale.Apps.Db.Law;
using Geocentrale.Apps.Server.Adapters.LexFind;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Db.LexfindCache;

namespace Geocentrale.Apps.Server.Adapters.Law3
{
    /// <summary>
    /// Geocentrale Apps Adapter für Law (v3), OEREBK Rechtsdaten
    /// </summary>
    public class Law3Adapter : IGAAdapter
    {
        public static readonly Guid oerebGuid = Guid.Parse("0dec20e8-82cb-47a5-98c2-ac515fad10b9");
        public static readonly Guid rechtsnormGuid = Guid.Parse("e2826482-300b-4437-b525-4055b7304e56");
        public static readonly Guid artikelGuid = Guid.Parse("1c715771-0f6f-4a5a-961a-92c93cb374ad");
        public static readonly Guid oerebkThemaGuid = Guid.Parse("8c7bcb11-3791-479f-b8a7-f227f1751ff9");
        public static readonly Guid zustStelleGuid = Guid.Parse(" fd939ac3-94e5-4410-a789-223bf4972d78");
        public static readonly string _refTemplate = "#REF"; //Shows a LexRef

        private static List<ScalarClass> _myClasses = new List<ScalarClass>();
        private static List<ScalarClass> myClasses {
            get
            {
                if (_myClasses.Count() == 0)
                {
                    PopulateMyClasses();
                }
                return _myClasses;
            }
        }
        private static ScalarClass oerebClass
        {
            get
            {
                return myClasses.Single(x => x.Guid == oerebGuid);
            }
        }
        private static ScalarClass rechtsnormClass
        {
            get
            {
                return myClasses.Single(x => x.Guid == rechtsnormGuid);
            }
        }
        private static ScalarClass artikelClass
        {
            get
            {
                return myClasses.Single(x => x.Guid == artikelGuid);
            }
        }
        private static ScalarClass oerebkThemaClass
        {
            get
            {
                return myClasses.Single(x => x.Guid == oerebkThemaGuid);
            }
        }
        private static ScalarClass zustStelleClass
        {
            get
            {
                return myClasses.Single(x => x.Guid == zustStelleGuid);
            }
        }
        private OerebLaw3Container _db;

        private static void PopulateMyClasses()
        {
            int i = 0;
            var oerebDefAttributeSpecs = new List<GAAttributeSpec>
            {
                new GAAttributeSpec(i++, "Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Aussage", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "UmschreibungRaumbezug", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "OerebKThema", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "OerebKThema.Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Rechtsstatus", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "ZustStelle", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "ZustStelle.Url", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "ZustStelle.Kanton", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "ZustStelle.Gemeinde", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "RechtsnormIds", String.Empty, typeof (List<int>)),
                new GAAttributeSpec(i++, "ArtikelIds", String.Empty, typeof (List<int>)),
                new GAAttributeSpec(i++, "IsLive", String.Empty, typeof (bool)),
                new GAAttributeSpec(i++, "VisibilityDate", String.Empty, typeof (DateTime))
            };
            _myClasses.Add(new ScalarClass("ÖREB", oerebDefAttributeSpecs, oerebGuid, "Id", "Aussage", typeof(Law3Adapter).AssemblyQualifiedName));

            i = 0;
            var rechtsnormAttributeSpecs = new List<GAAttributeSpec>
            {
                new GAAttributeSpec(i++, "Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Titel", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "OffiziellerTitel", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Abkuerzung", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "OffizielleNummer", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Kanton", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Gemeinde", String.Empty, typeof (int?)),
                new GAAttributeSpec(i++, "Dokument", String.Empty, typeof (byte[])),
                new GAAttributeSpec(i++, "Url", String.Empty, typeof (Uri)),
                new GAAttributeSpec(i++, "PubliziertAb", String.Empty, typeof (DateTime)),
                new GAAttributeSpec(i++, "RechtsnormTyp", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsstatus", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "IsLive", String.Empty, typeof (bool)),
                new GAAttributeSpec(i++, "VisibilityDate", String.Empty, typeof (DateTime)),
                new GAAttributeSpec(i++, "ZustStelle", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "ZustStelle.Url", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "ZustStelle.Kanton", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "ZustStelle.Gemeinde", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Parent.Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Level", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "SortIndex", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Icon", String.Empty, typeof (string))
            };
            _myClasses.Add(new ScalarClass("Rechtsnorm", rechtsnormAttributeSpecs, rechtsnormGuid, "Id", "Titel", typeof(Law3Adapter).AssemblyQualifiedName));

            i = 0;
            var artikelAttributeSpecs = new List<GAAttributeSpec>
            {
                new GAAttributeSpec(i++, "Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Nummer", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Text", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Rechtsnorm.Titel", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.OffiziellerTitel", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.Abkuerzung", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.OffizielleNummer", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.Kanton", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.Gemeinde", String.Empty, typeof (int?)),
                new GAAttributeSpec(i++, "Rechtsnorm.Dokument", String.Empty, typeof (byte[])),
                new GAAttributeSpec(i++, "Rechtsnorm.Url", String.Empty, typeof (Uri)),
                new GAAttributeSpec(i++, "Rechtsnorm.PubliziertAb", String.Empty, typeof (DateTime)),
                new GAAttributeSpec(i++, "Rechtsnorm.Rechtsstatus", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "IsLive", String.Empty, typeof (bool)),
                new GAAttributeSpec(i++, "VisibilityDate", String.Empty, typeof (DateTime)),
                new GAAttributeSpec(i++, "Rechtsnorm.ZustStelle", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.ZustStelle.Url", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.ZustStelle.Kanton", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.ZustStelle.Gemeinde", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.RechtsnormTyp", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Rechtsnorm.Parent.Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Level", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "SortIndex", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Icon", String.Empty, typeof (string))
            };
            _myClasses.Add(new ScalarClass("Artikel", artikelAttributeSpecs, artikelGuid, "Id", "Nummer", typeof(Law3Adapter).AssemblyQualifiedName));

            i = 0;
            var oerebkThemaAttributeSpecs = new List<GAAttributeSpec>
            {
                new GAAttributeSpec(i++, "Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Name", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Beschreibung", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Status", String.Empty, typeof (Int32)) //helper, not in db
            };
            _myClasses.Add(new ScalarClass("ÖREBK Thema", oerebkThemaAttributeSpecs, oerebkThemaGuid, "Id", "Name", typeof(Law3Adapter).AssemblyQualifiedName));

            i = 0;
            var zustStelleAttributeSpecs = new List<GAAttributeSpec>
            {
                new GAAttributeSpec(i++, "Id", String.Empty, typeof (int)),
                new GAAttributeSpec(i++, "Name", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Url", String.Empty, typeof (string)),
                new GAAttributeSpec(i++, "Abkuerzung", String.Empty, typeof (string)), 
                new GAAttributeSpec(i++, "Kanton", String.Empty, typeof (string)), 
                new GAAttributeSpec(i++, "Gemeinde", String.Empty, typeof (int?)) 
            };
            _myClasses.Add(new ScalarClass("Zuständige Stelle", zustStelleAttributeSpecs, zustStelleGuid, "Id", "Name", typeof(Law3Adapter).AssemblyQualifiedName));
        }

        public Law3Adapter()
        {
            _db = new OerebLaw3Container();
        }

        public List<ScalarClass> Identify()
        {
            return myClasses;
        }

        public List<ScalarClass> Identify(List<Guid> classGuids)
        {
            return myClasses.Where(x => classGuids.Contains(x.Guid)).ToList();
        }

        public List<GAObject> GetAll(Dictionary<Guid, ScalarClass> scalarClasses, Guid classGuid)
        {
            var result = new List<GAObject>();
            IEnumerable<int> classObjectIds = new List<int>();
            if (classGuid == oerebkThemaGuid)
            {
                classObjectIds = _db.OerebKThemaSet.Select(x => x.Id);
            }
            else if (classGuid == zustStelleGuid)
            {
                classObjectIds = _db.ZustStelleSet.Select(x => x.Id);
            }
            else if (classGuid == oerebGuid)
            {
                classObjectIds = _db.OerebDefinitionSet.Select(x => x.Id);
            }
            else if (classGuid == rechtsnormGuid)
            {
                classObjectIds = _db.RechtsnormSet.Select(x => x.Id);
            }
            else if (classGuid == artikelGuid)
            {
                classObjectIds = _db.ArtikelSet.Select(x => x.Id);
            }
            result.AddRange(GetById(scalarClasses, classGuid, classObjectIds.ToList().Cast<dynamic>().ToList()));
            return result;
        }

        public List<GAObject> GetById(Dictionary<Guid, ScalarClass> scalarClasses, Guid classGuid, List<dynamic> objectIds)
        {
            List<GAObject> result = new List<GAObject>();
            var lexFindResolve = new LexFindResolve();
            var objectIdsNumeric = new List<int>();

            foreach (var objectIdItem in objectIds)
            {
                Int32 objectId;

                if (!Int32.TryParse(objectIdItem.ToString(), out objectId))
                {
                    continue;
                }

                objectIdsNumeric.Add(objectId);
            }

            foreach (var objectId in objectIdsNumeric)
            {
                int i_objectId;

                if (!int.TryParse(objectId.ToString(), out i_objectId))
                {
                    continue;
                }
                if (classGuid == zustStelleGuid)
                {
                    var dbZustStelle = _db.ZustStelleSet.SingleOrDefault(x => x.Id == i_objectId);
                    if (dbZustStelle == null)
                    {
                        continue;
                    }
                    var obj = new GAObject(zustStelleClass);
                    obj["Id"] = dbZustStelle.Id;
                    obj["Name"] = dbZustStelle.Name;
                    obj["Url"] = dbZustStelle.Url;
                    obj["Abkuerzung"] = dbZustStelle.Abkuerzung;
                    obj["Kanton"] = dbZustStelle.Kanton;
                    obj["Gemeinde"] = dbZustStelle.Gemeinde;
                    result.Add(obj);
                }
                else if (classGuid == oerebkThemaGuid)
                {
                    var dbOerebThema = _db.OerebKThemaSet.SingleOrDefault(x => x.Id == i_objectId);
                    if (dbOerebThema == null)
                    {
                        continue;
                    }
                    var obj = new GAObject(oerebkThemaClass);
                    obj["Id"] = dbOerebThema.Id;
                    obj["Name"] = dbOerebThema.Name;
                    obj["Beschreibung"] = dbOerebThema.Beschreibung;
                    obj["Status"] = dbOerebThema.OerebDefinition.Count() > 0 ? 1 : 0; //helper, not in db
                    result.Add(obj);
                }
                else if (classGuid == oerebGuid)
                {
                    var dbOerebDef = _db.OerebDefinitionSet.SingleOrDefault(x => x.Id == i_objectId);
                    if (dbOerebDef == null)
                    {
                        continue;
                    }
                    var obj = new GAObject(oerebClass);
                    obj["Id"] = dbOerebDef.Id;
                    obj["Aussage"] = dbOerebDef.Aussage;
                    obj["UmschreibungRaumbezug"] = dbOerebDef.UmschreibungRaumbezug;
                    obj["OerebKThema"] = dbOerebDef.OerebKThema.Name;
                    obj["OerebKThema.Id"] = dbOerebDef.OerebKThema.Id;
                    obj["Rechtsstatus"] = dbOerebDef.Rechtsstatus.Bezeichnung;
                    obj["ZustStelle"] = dbOerebDef.ZustStelle.Name;
                    obj["ZustStelle.Url"] = dbOerebDef.ZustStelle.Url;
                    obj["ZustStelle.Kanton"] = dbOerebDef.ZustStelle.Kanton;
                    obj["ZustStelle.Gemeinde"] = dbOerebDef.ZustStelle.Gemeinde;
                    obj["RechtsnormIds"] = dbOerebDef.Rechtsnorm.Select(x => x.Id).ToList();
                    obj["ArtikelIds"] = dbOerebDef.Artikel.Select(x => x.Id).ToList();
                    obj["IsLive"] = dbOerebDef.IsLive ?? true;
                    obj["VisibilityDate"] = dbOerebDef.VisibilityDate ?? DateTime.MinValue;
                    result.Add(obj);
                }
                else if (classGuid == rechtsnormGuid)
                {
                    var dbRechtsnorm = _db.RechtsnormSet.SingleOrDefault(x => x.Id == i_objectId);
                    if (dbRechtsnorm == null)
                    {
                        continue;
                    }

                    var rechtsnormViewModels = lexFindResolve.GetRechtsnormViewModels(dbRechtsnorm.Id).FirstOrDefault();
                    //lexFindResolved = rechtsnormViewModels != null;

                    var obj = new GAObject(rechtsnormClass);
                    obj["Id"] = dbRechtsnorm.Id;
                    obj["Titel"] = dbRechtsnorm.Titel == null ? String.Empty : (dbRechtsnorm.Titel.StartsWith(_refTemplate) ? rechtsnormViewModels.Titel : dbRechtsnorm.Titel);
                    obj["OffiziellerTitel"] = dbRechtsnorm.OffiziellerTitel == null ? String.Empty : (dbRechtsnorm.OffiziellerTitel.StartsWith(_refTemplate) ? rechtsnormViewModels.OffiziellerTitel : dbRechtsnorm.OffiziellerTitel);
                    obj["Abkuerzung"] =  dbRechtsnorm.Abkuerzung == null ? String.Empty : (dbRechtsnorm.Abkuerzung.StartsWith(_refTemplate) ? rechtsnormViewModels.Abkuerzung : dbRechtsnorm.Abkuerzung);
                    obj["OffizielleNummer"] =  dbRechtsnorm.OffizielleNr == null ? String.Empty : (dbRechtsnorm.OffizielleNr.StartsWith(_refTemplate) ? rechtsnormViewModels.OffizielleNr : dbRechtsnorm.OffizielleNr);
                    obj["Kanton"] =  dbRechtsnorm.Kanton == null ? String.Empty : (dbRechtsnorm.Kanton.StartsWith(_refTemplate) ? rechtsnormViewModels.Kanton : dbRechtsnorm.Kanton);
                    //TODO if lexfind is not available it breaks here
                    obj["Url"] = dbRechtsnorm.Url == null ? null : (dbRechtsnorm.Url.StartsWith(_refTemplate) ?  new Uri(rechtsnormViewModels.Url,UriKind.Absolute) : new Uri(dbRechtsnorm.Url,UriKind.Absolute));
                    obj["RechtsnormTyp"] = dbRechtsnorm.RechtsnormTyp.Bezeichnung;
                    obj["Rechtsstatus"] = dbRechtsnorm.Rechtsstatus == null ? "" : dbRechtsnorm.Rechtsstatus.Bezeichnung;

                    //var attributeSpec = obj.GAClass.AttributeSpecs.Where(x => x.Name == "Gemeinde").First();
                    //var attribute = new GAAttribute(attributeSpec,ChangeType<int?>(dbRechtsnorm.Gemeinde));
                    //obj.Attributes.Add(attribute);
                    obj["Gemeinde"] = dbRechtsnorm.Gemeinde;

                    obj["Dokument"] = dbRechtsnorm.DokumentBinary;
                    obj["PubliziertAb"] = dbRechtsnorm.PubliziertAb;
                    obj["IsLive"] = dbRechtsnorm.IsLive ?? true;
                    obj["VisibilityDate"] = dbRechtsnorm.VisibilityDate ?? DateTime.MinValue;
                    obj["ZustStelle"] = dbRechtsnorm.ZustStelle.Name;
                    obj["ZustStelle.Url"] = dbRechtsnorm.ZustStelle.Url;
                    obj["ZustStelle.Kanton"] = dbRechtsnorm.ZustStelle.Kanton;
                    obj["ZustStelle.Gemeinde"] = dbRechtsnorm.ZustStelle.Gemeinde;
                    obj["Parent.Id"] = dbRechtsnorm.Parent == null ? 0 : dbRechtsnorm.Parent.Id;

                    obj["Level"] = GetLevel(obj["Kanton"], obj["Gemeinde"]);
                    obj["SortIndex"] = GetSortIndex(obj["Kanton"], obj["Gemeinde"]);

                    var filename = String.IsNullOrEmpty(obj["Level"]) ? "empty" : obj["Level"];
                    var filepath = String.Format(Common.PathTasks.GetBinDirectory().Parent.FullName + @"\Image\Icon\{0}.png", filename);
                    obj["Icon"] = String.Empty;

                    if (File.Exists(filepath))
                    {
                        var image = System.Drawing.Image.FromFile(filepath);
                        obj["Icon"] = Geocentrale.Common.ImageTasks.GetBase64StringFromImage(image, ImageFormat.Png, "raw");
                    }

                    result.Add(obj);
                }
                else if (classGuid == artikelGuid)
                {
                    var dbArtikel = _db.ArtikelSet.SingleOrDefault(x => x.Id == i_objectId);
                    if (dbArtikel == null)
                    {
                        continue;
                    }

                    var rechtsnormViewModels = lexFindResolve.GetRechtsnormViewModels(dbArtikel.Rechtsnorm.Id).FirstOrDefault();
                    //lexFindResolved = rechtsnormViewModels != null;

                    var obj = new GAObject(artikelClass);
                    obj["Id"] = dbArtikel.Id;
                    obj["Nummer"] = String.Format("{0} {1}", dbArtikel.Rechtsnorm.ArtikelBezeichner ?? "Art.", dbArtikel.Nr);
                    obj["Text"] = dbArtikel.Text;
                    obj["Rechtsnorm.Id"] = dbArtikel.Rechtsnorm.Id;

                    obj["Rechtsnorm.Titel"] = dbArtikel.Rechtsnorm.Titel == null ? String.Empty : (dbArtikel.Rechtsnorm.Titel.StartsWith(_refTemplate) ? rechtsnormViewModels.Titel : dbArtikel.Rechtsnorm.Titel);
                    obj["Rechtsnorm.OffiziellerTitel"] = dbArtikel.Rechtsnorm.OffiziellerTitel == null ? String.Empty : (dbArtikel.Rechtsnorm.OffiziellerTitel.StartsWith(_refTemplate) ? rechtsnormViewModels.OffiziellerTitel : dbArtikel.Rechtsnorm.OffiziellerTitel);
                    obj["Rechtsnorm.Abkuerzung"] = dbArtikel.Rechtsnorm.Abkuerzung == null ? String.Empty : (dbArtikel.Rechtsnorm.Abkuerzung.StartsWith(_refTemplate) ? rechtsnormViewModels.Abkuerzung : dbArtikel.Rechtsnorm.Abkuerzung);
                    obj["Rechtsnorm.OffizielleNummer"] = dbArtikel.Rechtsnorm.OffizielleNr == null ? String.Empty : (dbArtikel.Rechtsnorm.OffizielleNr.StartsWith(_refTemplate) ? rechtsnormViewModels.OffizielleNr : dbArtikel.Rechtsnorm.OffizielleNr);
                    obj["Rechtsnorm.Kanton"] = dbArtikel.Rechtsnorm.Kanton == null ? String.Empty : (dbArtikel.Rechtsnorm.Kanton.StartsWith(_refTemplate) ? rechtsnormViewModels.Kanton : dbArtikel.Rechtsnorm.Kanton);
                    //obj["Rechtsnorm.Url"] = dbArtikel.Rechtsnorm.Url == null ? String.Empty : (dbArtikel.Rechtsnorm.Url.StartsWith(_refTemplate) ? rechtsnormViewModels.Url : dbArtikel.Rechtsnorm.Url);
                    obj["Rechtsnorm.Rechtsstatus"] = dbArtikel.Rechtsnorm.Rechtsstatus == null ? "" : dbArtikel.Rechtsnorm.Rechtsstatus.Bezeichnung;
                    obj["Rechtsnorm.Url"] = dbArtikel.Rechtsnorm.Url == null ? null : (dbArtikel.Rechtsnorm.Url.StartsWith(_refTemplate) ? new Uri(rechtsnormViewModels.Url, UriKind.Absolute) : new Uri(dbArtikel.Rechtsnorm.Url, UriKind.Absolute));
                    obj["Rechtsnorm.Parent.Id"] = dbArtikel.Rechtsnorm.Parent == null ? 0 : dbArtikel.Rechtsnorm.Parent.Id;
                    obj["Rechtsnorm.ZustStelle"] = dbArtikel.Rechtsnorm.ZustStelle.Name;
                    obj["Rechtsnorm.ZustStelle.Url"] = dbArtikel.Rechtsnorm.ZustStelle.Url;
                    obj["Rechtsnorm.ZustStelle.Kanton"] = dbArtikel.Rechtsnorm.ZustStelle.Kanton;
                    obj["Rechtsnorm.ZustStelle.Gemeinde"] = dbArtikel.Rechtsnorm.ZustStelle.Gemeinde;
                    obj["Rechtsnorm.RechtsnormTyp"] = dbArtikel.Rechtsnorm.RechtsnormTyp.Bezeichnung;
                    
                    obj["Rechtsnorm.Gemeinde"] = dbArtikel.Rechtsnorm.Gemeinde;
                    obj["Rechtsnorm.Dokument"] = dbArtikel.Rechtsnorm.DokumentBinary;
                    obj["Rechtsnorm.PubliziertAb"] = dbArtikel.Rechtsnorm.PubliziertAb;
                    obj["IsLive"] = (dbArtikel.IsLive ?? true) && (dbArtikel.Rechtsnorm.IsLive ?? true);
                    obj["VisibilityDate"] = (dbArtikel.VisibilityDate ?? DateTime.MinValue) > (dbArtikel.Rechtsnorm.VisibilityDate ?? DateTime.MinValue) ? (dbArtikel.VisibilityDate ?? DateTime.MinValue) : (dbArtikel.Rechtsnorm.VisibilityDate ?? DateTime.MinValue);

                    obj["Level"] = GetLevel(obj["Rechtsnorm.Kanton"], obj["Rechtsnorm.Gemeinde"]);
                    obj["SortIndex"] = GetSortIndex(obj["Rechtsnorm.Kanton"], obj["Rechtsnorm.Gemeinde"]);

                    var filename = String.IsNullOrEmpty(obj["Level"]) ? "empty" : obj["Level"];
                    var filepath = String.Format(Common.PathTasks.GetBinDirectory().Parent.FullName + @"\Image\Icon\{0}.png", filename);
                    obj["Icon"] = String.Empty;

                    if (File.Exists(filepath))
                    {
                        var image = System.Drawing.Image.FromFile(filepath);
                        obj["Icon"] = Geocentrale.Common.ImageTasks.GetBase64StringFromImage(image, ImageFormat.Png, "raw");
                    }

                    result.Add(obj);
                }
            }
            return result;
        }

        private string GetLevel(object kanton, object gemeinde)
        {
            if (kanton == null && gemeinde == null)
            {
                return "CH";
            }

            if (gemeinde != null)
            {
                var _gemeindeResolver = new GemeindeResolver();
                var _gemeinde = _gemeindeResolver.Gemeinden.FirstOrDefault(x => x.GDENR == (Int32)gemeinde);

                return _gemeinde != null ? _gemeinde.GDENAME : String.Empty;
            }

            return kanton as string;
        }

        private int GetSortIndex(object kanton, object gemeinde)
        {
            if (kanton == null && gemeinde == null)
            {
                return 0;
            }

            if (gemeinde != null)
            {
                return 2;
            }

            return 1;
        }

        private static T ChangeType<T>(object value)
        {
            var t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return default(T);
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return (T)Convert.ChangeType(value, t);
        }
    }
}
