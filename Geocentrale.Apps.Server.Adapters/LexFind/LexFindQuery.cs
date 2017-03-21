using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geocentrale.Apps.Db.LexfindCache;

namespace Geocentrale.Apps.Server.Adapters.LexFind
{
    public class LexFindQuery
    {
        private LexFindCacheContainer dbLF = new LexFindCacheContainer();
        private List<string> _kantoneCache;
        private List<string> _kantone
        {
            get
            {
                if (_kantoneCache == null)
                {
                    _kantoneCache = dbLF.RechtsnormLFSet.Where(x => x.Kanton.Length > 2).Select(x => x.Kanton).Distinct().OrderBy(x => x).ToList();
                    _kantoneCache.AddRange(dbLF.RechtsnormLFSet.Where(x => x.Kanton.Length <= 2).Select(x => x.Kanton).Distinct().OrderBy(x => x));
                }
                return _kantoneCache;
            }
        }

        public LexFindQuery()
        {
        }

        public IQueryable<Db.LexfindCache.RechtsnormLF> QueryDatabase(string[] kantone, string titel, string abk, string sysnr, string lexfindid)
        {
            if (kantone == null || kantone.Length == 0)
            {
                kantone = _kantone.ToArray();
            }
            else if (kantone.Length == 1)
            {
                string kantoneString = kantone[0].ToString();
                kantone = kantoneString.Split(',');
            }
            for (int i = 0; i < kantone.Length; i++)
            {
                kantone[i] = kantone[i].ToLower();
            }

            IQueryable<Db.LexfindCache.RechtsnormLF> queryResult = dbLF.RechtsnormLFSet.Where(x => kantone.Contains(x.Kanton.ToLower()));
            if (!String.IsNullOrEmpty(sysnr))
            {
                queryResult = queryResult.Where(x => x.SysNr.ToLower() == sysnr.ToLower());
            }
            if (!String.IsNullOrEmpty(abk))
            {
                queryResult = queryResult.Where(x => x.Abk.ToLower() == abk.ToLower());
            }
            if (!String.IsNullOrEmpty(titel))
            {
                queryResult = queryResult.Where(x => x.Titel.ToLower().Contains(titel.ToLower()));
            }
            if (!String.IsNullOrEmpty(lexfindid))
            {
                int int_lexfindid;
                if (Int32.TryParse(lexfindid, out int_lexfindid))
                {
                    queryResult = queryResult.Where(x => x.LexFindId == int_lexfindid);
                }
                else
                {
                    return null;
                }
            }

            return queryResult;
        }
    }
}
