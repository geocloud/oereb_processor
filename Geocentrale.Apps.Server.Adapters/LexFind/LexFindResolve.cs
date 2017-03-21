using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure;
using Geocentrale.Apps.Db.Law;

namespace Geocentrale.Apps.Server.Adapters.LexFind
{
    public class LexFindResolve
    {
        private OerebLaw3Container dbLaw3 = new OerebLaw3Container();
        private GemeindeResolver _gemeindeResolver = new GemeindeResolver();

        public List<RechtsnormViewModel> GetRechtsnormViewModels(int? rechtsnormId = null)
        {
            List<Rechtsnorm> rechtsnormen;
            List<RechtsnormViewModel> rechtsnormviewmodels = new List<RechtsnormViewModel>();

            if (rechtsnormId != null)
            {
                rechtsnormen = new List<Rechtsnorm>();
                Rechtsnorm selectedRechtsnorm = dbLaw3.RechtsnormSet.Find(rechtsnormId);
                if (selectedRechtsnorm == null)
                {
                    return rechtsnormviewmodels;
                }
                rechtsnormen.Add(selectedRechtsnorm);
            }
            else
            {
                rechtsnormen = dbLaw3.RechtsnormSet.ToList();
            }
            rechtsnormen.ForEach(x => rechtsnormviewmodels.Add(new RechtsnormViewModel(x)));

            var lexfind = new LexFindQuery();
            foreach (var rechtsnorm in rechtsnormviewmodels)
            {
                if (rechtsnorm.Gemeinde != null)
                {
                    var gemeinde = _gemeindeResolver.Gemeinden.FirstOrDefault(x => x.GDENR == rechtsnorm.Gemeinde);
                    if (gemeinde != null)
                    {
                        rechtsnorm.GemeindeText = gemeinde.GDENAME ?? "?";
                        rechtsnorm.Kanton = gemeinde.GDEKT ?? "?";
                    }
                }
                if (rechtsnorm.OffiziellerTitel == "#REF-TITEL")
                {
                    rechtsnorm.OffiziellerTitel = rechtsnorm.Titel;
                    rechtsnorm.OffiziellerTitelClass = "linkedtolexfind";
                }
                //only process if lexfind linked
                if (rechtsnorm.LinkLexFindId == null && rechtsnorm.LinkLexFindKtSysNr == null)
                {
                    continue;
                }
                IQueryable<Db.LexfindCache.RechtsnormLF> lexfindresultlist = null;
                if (rechtsnorm.LinkLexFindId != null)
                {
                    lexfindresultlist = lexfind.QueryDatabase(null, null, null, null, rechtsnorm.LinkLexFindId);
                }
                else if (rechtsnorm.LinkLexFindKtSysNr != null)
                {
                    var ktsysnr = rechtsnorm.LinkLexFindKtSysNr.Split('/');
                    if (ktsysnr.Length != 2)
                    {
                        continue;
                    }
                    lexfindresultlist = lexfind.QueryDatabase(new string[] { ktsysnr[0] }, null, null, ktsysnr[1], null);
                }
                if (lexfindresultlist == null || lexfindresultlist.Count() < 1)
                {
                    continue;
                }
                var lexfindresult = lexfindresultlist.First();

                if (rechtsnorm.Titel == "#REF-LEXFIND")
                {
                    rechtsnorm.Titel = lexfindresult.Titel;
                    rechtsnorm.TitelClass = "linkedtolexfind";
                }
                if (rechtsnorm.Abkuerzung == "#REF-LEXFIND")
                {
                    rechtsnorm.Abkuerzung = lexfindresult.Abk;
                    rechtsnorm.AbkuerzungClass = "linkedtolexfind";
                }
                if (rechtsnorm.OffizielleNr == "#REF-LEXFIND")
                {
                    rechtsnorm.OffizielleNr = lexfindresult.SysNr;
                    rechtsnorm.OffizielleNrClass = "linkedtolexfind";
                }
                if (rechtsnorm.Url == "#REF-LEXFIND-URL")
                {
                    rechtsnorm.Url = lexfindresult.LexFindUrl;
                    rechtsnorm.UrlClass = "linkedtolexfind";
                    rechtsnorm.UrlSource = "lexfindurl";
                }
                if (rechtsnorm.Url == "#REF-LEXFIND-ORIGURL")
                {
                    rechtsnorm.Url = lexfindresult.OrigUrl;
                    rechtsnorm.UrlClass = "linkedtolexfind";
                    rechtsnorm.UrlSource = "lexfindorigurl";
                }
                if (rechtsnorm.PubliziertAb == DateTime.MaxValue || rechtsnorm.PubliziertAb == System.Data.SqlTypes.SqlDateTime.MaxValue)
                {
                    if (lexfindresult.InKraftSeit != null)
                    {
                        rechtsnorm.PubliziertAb = (DateTime)lexfindresult.InKraftSeit;
                        rechtsnorm.PubliziertAbClass = "linkedtolexfind";
                    }
                    else if (lexfindresult.Infkrafttreten != null)
                    {
                        rechtsnorm.PubliziertAb = (DateTime)lexfindresult.Infkrafttreten;
                        rechtsnorm.PubliziertAbClass = "linkedtolexfind";
                    }
                }
                if (rechtsnorm.Kanton == "#REF-LEXFIND")
                {
                    rechtsnorm.Kanton = lexfindresult.Kanton;
                    if (rechtsnorm.Kanton == "Bund")
                    {
                        rechtsnorm.Kanton = null;
                        rechtsnorm.Gemeinde = null;
                    }
                    rechtsnorm.KantonClass = "linkedtolexfind";
                }
                if (rechtsnorm.OffiziellerTitel == "#REF-LEXFIND")
                {
                    rechtsnorm.OffiziellerTitel = rechtsnorm.Titel;
                    rechtsnorm.OffiziellerTitelClass = "linkedtolexfind";
                }
            }
            return rechtsnormviewmodels;
        }

    }
}
