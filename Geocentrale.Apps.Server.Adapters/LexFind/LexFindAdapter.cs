using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Geocentrale.Apps.Db.LexfindCache;

namespace Geocentrale.Apps.Server.Adapters.LexFind
{
    public class LexFindAdapter
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private LexFindCacheContainer _db = new LexFindCacheContainer();

        public void DownloadAllElements()
        {
            ResetSeenInNewestSyncFlag();
            CookieContainer cookieContainer = new CookieContainer();
            HttpWebRequest initialWebRequest = (HttpWebRequest)WebRequest.Create("http://www.lexfind.ch/index.php?&cid=2");
            initialWebRequest.CookieContainer = cookieContainer;
            ASCIIEncoding encoding = new ASCIIEncoding();

            //all countries and nation data
            //string postData = "frmCanton%5B27%5D=1&frmCanton%5B1%5D=1&frmCanton%5B2%5D=1&frmCanton%5B3%5D=1&frmCanton%5B4%5D=1&frmCanton%5B5%5D=1&frmCanton%5B6%5D=1&frmCanton%5B7%5D=1&frmCanton%5B8%5D=1&frmCanton%5B9%5D=1&frmCanton%5B10%5D=1&frmCanton%5B11%5D=1&frmCanton%5B12%5D=1&frmCanton%5B13%5D=1&frmCanton%5B14%5D=1&frmCanton%5B15%5D=1&frmCanton%5B16%5D=1&frmCanton%5B17%5D=1&frmCanton%5B18%5D=1&frmCanton%5B19%5D=1&frmCanton%5B20%5D=1&frmCanton%5B21%5D=1&frmCanton%5B22%5D=1&frmCanton%5B23%5D=1&frmCanton%5B24%5D=1&frmCanton%5B25%5D=1&frmCanton%5B26%5D=1&frmHirarchy%5B1%5D=1&frmHirarchy%5B2%5D=1&frmHirarchy%5B3%5D=1&frmHirarchy%5B4%5D=1&frmHirarchy%5B8%5D=1&frmHirarchy%5B6%5D=1&frmHirarchy%5B7%5D=1&frmHirarchy%5B9%5D=1&frmHirarchy%5B5%5D=1&frmOptMode=1&_qf__indexsearch=&frmIffCat=";

            //get value from app.config
            //string postData = "frmCanton%5B27%5D=1&frmCanton%5B14%5D=1&frmCanton%5B15%5D=1&frmHirarchy%5B1%5D=1&frmHirarchy%5B2%5D=1&frmHirarchy%5B3%5D=1&frmHirarchy%5B4%5D=1&frmHirarchy%5B8%5D=1&frmHirarchy%5B6%5D=1&frmHirarchy%5B7%5D=1&frmHirarchy%5B9%5D=1&frmHirarchy%5B5%5D=1&frmOptMode=1&_qf__indexsearch=&frmIffCat=";
            //todo the current app does not work, import all countries
            //Bund and Kanton UR => 432 items in Db
            string postData = "frmCanton%5B27%5D=1&frmCanton%5B22%5D=1&frmHirarchy%5B1%5D=1&frmHirarchy%5B2%5D=1&frmHirarchy%5B3%5D=1&frmHirarchy%5B4%5D=1&frmHirarchy%5B8%5D=1&frmHirarchy%5B6%5D=1&frmHirarchy%5B7%5D=1&frmHirarchy%5B9%5D=1&frmHirarchy%5B5%5D=1&frmOptMode=1&_qf__indexsearch=&frmIffCat="; //System.Configuration.ConfigurationManager.AppSettings["postData"];

            byte[] data = encoding.GetBytes(postData);

            initialWebRequest.Method = "POST";
            initialWebRequest.ContentType = "application/x-www-form-urlencoded";
            initialWebRequest.ContentLength = data.Length;

            using (Stream stream = initialWebRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            HttpWebResponse initialResponse = (HttpWebResponse)initialWebRequest.GetResponse();

            int pageNr = 1;
            string currentPage = new StreamReader(initialResponse.GetResponseStream()).ReadToEnd();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int errorCount = 0;
            bool abortOperation = false;

            while (errorCount> 0 || NextPageExists(currentPage))
            {
                var msg = String.Format("begin with page {0}, errorCount {1}, time {2}", pageNr, errorCount, sw.Elapsed.TotalSeconds);
                Console.WriteLine(msg);
                log.Info(msg);

                try
                {
                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://www.lexfind.ch/index.php?cid=10&p=" + pageNr.ToString());
                    webRequest.CookieContainer = cookieContainer;
                    webRequest.Timeout = 30000;
                    webRequest.Method = "GET";

                    HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        currentPage = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("ISO-8859-1")).ReadToEnd();

                        log.Info(String.Format("start parse page {0}, time {1}", pageNr, sw.Elapsed.TotalSeconds));
                        ParseResults(currentPage);

                        pageNr++;
                        errorCount = 0;

                        continue;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(String.Format("exception, {0}", ex));
                }

                errorCount += 1;

                if (errorCount > 4)
                {
                    log.Fatal(String.Format("abort parsing, max errorcount {0}", errorCount));
                    abortOperation = true;
                    break;
 
                }

                var warn = String.Format("try again page {0}, loop {1}", pageNr, errorCount);
                Console.WriteLine(warn);
                log.Warn(warn);
            }

            var result = String.Empty;

            if (!abortOperation)
            {
                result = String.Format("*****Successfully,{0},{1}*****", pageNr, sw.Elapsed.TotalSeconds);                
            }
            else
            {
                result = String.Format("*****Failed,{0},{1}*****", pageNr, sw.Elapsed.TotalSeconds);                                
            }

            Console.WriteLine(result);
            log.Info(result);
        }

        public static bool NextPageExists(string html)
        {
            return html.Contains("Weiter &gt;");
        }

        public void ClearDb()
        {
            var objCtx = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)_db).ObjectContext;
            objCtx.ExecuteStoreCommand("DELETE FROM RechtsnormAusserkraftSet");
            objCtx.ExecuteStoreCommand("DELETE FROM RechtsnormSet");
        }

        public void ResetSeenInNewestSyncFlag()
        {
            var objCtx = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)_db).ObjectContext;
            objCtx.ExecuteStoreCommand("UPDATE RechtsnormSet SET SeenInNewestSync = 0");
            objCtx.ExecuteStoreCommand("UPDATE RechtsnormAusserKraftSet SET SeenInNewestSync = 0");
        }

        public void ParseResults(string html)
        {
            //Console.WriteLine("Parsen der Daten...");
            //var rechtsnormen = new List<Rechtsnorm>();
            //var rechtsnormenAusserKraft = new List<RechtsnormAusserKraft>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            var resultTable = doc.GetElementbyId("result_table");
            var resultItems = resultTable.Descendants("tr").Where(x => {
                var classAttribute = x.GetAttributeValue("class",String.Empty);
                return classAttribute.StartsWith("tr_result_item");
            });

            _db = new LexFindCacheContainer();
            _db.Configuration.AutoDetectChangesEnabled = false;

            foreach (var resultItem in resultItems)
            {
                var newRechtsnorm = GetRechtsnormFromResultItem(resultItem);
                var newRechtsnormenAusserKraft = GetRechtsnormenAusserKraftFromFormerVersions(newRechtsnorm, resultItem);

                // process rechtsnorm
                // update values (changes in title etc.), if already present, else add new
                var existingRechtsnorm = _db.RechtsnormLFSet.FirstOrDefault(x => x.LexFindId == newRechtsnorm.LexFindId);
                if (existingRechtsnorm != null)
                {
                    newRechtsnorm.Id = existingRechtsnorm.Id; // preserve the db-id
                    newRechtsnorm.IsUpdated = false;
                    newRechtsnorm.IsNew = false;
                    _db.Entry(existingRechtsnorm).CurrentValues.SetValues(newRechtsnorm);
                    //_db.Entry(existingRechtsnorm).Property(x => x.Id).IsModified = false; // preserve the db-id
                }
                else
                {
                    // check if any of the now ausser kraft rechtsnormen is present in the rechtsnormen table
                    newRechtsnorm.IsUpdated = false;
                    foreach (var newRechtsnormAusserKraft in newRechtsnormenAusserKraft)
                    {
                        var deprecatedRechtsnorm = _db.RechtsnormLFSet.FirstOrDefault(x => x.LexFindId == newRechtsnormAusserKraft.LexFindId);
                        if (deprecatedRechtsnorm != null)
                        {
                            // if deprecated, update values, preserve db-id
                            newRechtsnorm.IsUpdated = true;
                            newRechtsnorm.IsNew = false;
                            newRechtsnorm.Id = deprecatedRechtsnorm.Id; // preserve the db-id
                            _db.Entry(deprecatedRechtsnorm).CurrentValues.SetValues(newRechtsnorm);
                            //_db.Entry(deprecatedRechtsnorm).Property(x => x.Id).IsModified = false; // preserve the db-id
                        }
                    }
                    // rechtsnorm is entirely new
                    if (!(bool)newRechtsnorm.IsUpdated)
                    {
                        newRechtsnorm.IsUpdated = false;
                        newRechtsnorm.IsNew = true;
                        _db.RechtsnormLFSet.Add(newRechtsnorm);
                    }
                }
                //_db.SaveChanges();  
                // process rechtsnormenausserkraft
                foreach (var newRechtsnormAusserKraft in newRechtsnormenAusserKraft)
                {
                    var existingRechtsnormAusserKraft = _db.RechtsnormAusserKraftSet.FirstOrDefault(x => x.LexFindId == newRechtsnormAusserKraft.LexFindId);
                    // update values (changes in title etc.), if already present, else add new
                    if (existingRechtsnormAusserKraft != null)
                    {
                        newRechtsnormAusserKraft.Id = existingRechtsnormAusserKraft.Id; // preserve the db-id
                        newRechtsnormAusserKraft.RechtsnormId = existingRechtsnormAusserKraft.RechtsnormId; // preserve relation
                        newRechtsnormAusserKraft.IsNew = false;
                        _db.Entry(existingRechtsnormAusserKraft).CurrentValues.SetValues(newRechtsnormAusserKraft);
                        //_db.Entry(existingRechtsnormAusserKraft).Property(x => x.Id).IsModified = false; // preserve the db-id
                        //_db.Entry(existingRechtsnormAusserKraft).Property(x => x.RechtsnormId).IsModified = false; // preserve the db-id
                    }
                    else
                    {
                        newRechtsnormAusserKraft.IsNew = true;
                        _db.RechtsnormAusserKraftSet.Add(newRechtsnormAusserKraft);
                    }
                    //_db.SaveChanges();  
                }
                //rechtsnormen.Add(newRechtsnorm);
                //newRechtsnormenAusserKraft.AddRange(newRechtsnormenAusserKraft);
            }
            
            //Console.WriteLine("Übertrage Daten in Datenbank...");
            //rechtsnormen.ForEach(x => _db.RechtsnormSet.Add(x));
            //rechtsnormenAusserKraft.ForEach(x => _db.RechtsnormAusserKraftSet.Add(x));
            _db.ChangeTracker.DetectChanges();
            _db.SaveChanges();  
        }

        private List<RechtsnormAusserKraft> GetRechtsnormenAusserKraftFromFormerVersions(Db.LexfindCache.RechtsnormLF rechtsnorm, HtmlNode resultItem)
        {
            var itemElements = resultItem.Descendants();
            var formerVersions = (itemElements.FirstOrDefault(x => x.Name == "table" && x.GetAttributeValue("id", String.Empty) == "former_versions") ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0)).Descendants().Where(x => x.Name == "a" && x.GetAttributeValue("href", String.Empty).StartsWith("http://www.lexfind.ch/dtah"));
            var rechtsnormenAusserKraft = new List<RechtsnormAusserKraft>();
            foreach (var formerVersion in formerVersions)
            {
                RechtsnormAusserKraft rechtsnormAusserKraft = new RechtsnormAusserKraft();
                rechtsnormAusserKraft.SeenInNewestSync = true;
                rechtsnormAusserKraft.Rechtsnorm = rechtsnorm;

                rechtsnormAusserKraft.LexFindUrl = formerVersion.GetAttributeValue("href",String.Empty);
                if (String.IsNullOrEmpty(rechtsnormAusserKraft.LexFindUrl))
                    rechtsnormAusserKraft.LexFindUrl = null;
                int fvLexFindId = -1;
                if (!String.IsNullOrEmpty(rechtsnormAusserKraft.LexFindUrl) && Int32.TryParse(rechtsnormAusserKraft.LexFindUrl.Split('/').ElementAtOrDefault(4), out fvLexFindId))
                    rechtsnormAusserKraft.LexFindId = fvLexFindId;

                var fvText = formerVersion.InnerText.Replace(":","").Replace("(","").Replace(")","").Split(' ').ToList();

                DateTime fvVon;
                if (DateTime.TryParse(fvText.ElementAtOrDefault(fvText.IndexOf("vom")+1) ?? String.Empty, out fvVon))
                    rechtsnormAusserKraft.InKraftVon = fvVon;
                DateTime fvBis;
                if (DateTime.TryParse(fvText.ElementAtOrDefault(fvText.IndexOf("bis")+1) ?? String.Empty, out fvBis))
                    rechtsnormAusserKraft.InKraftBis = fvBis;
                DateTime fvBerichtigt;
                if (DateTime.TryParse(fvText.ElementAtOrDefault(fvText.IndexOf("berichtigt")+2) ?? String.Empty, out fvBerichtigt))
                    rechtsnormAusserKraft.FormlosBerichtigtAm = fvBerichtigt;

                rechtsnormenAusserKraft.Add(rechtsnormAusserKraft);
            }
            return rechtsnormenAusserKraft;
        }

        private Db.LexfindCache.RechtsnormLF GetRechtsnormFromResultItem(HtmlNode resultItem)
        {
            var itemElements = resultItem.Descendants();
            Db.LexfindCache.RechtsnormLF rechtsnorm = new Db.LexfindCache.RechtsnormLF();
            rechtsnorm.SeenInNewestSync = true;

            rechtsnorm.Kanton = (itemElements.FirstOrDefault(x => x.Name == "img") ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0)).GetAttributeValue("alt", String.Empty);
            if (String.IsNullOrEmpty(rechtsnorm.Kanton))
                rechtsnorm.Kanton = null;
            rechtsnorm.SysNr = (itemElements.Where(x => x.Name == "td").ElementAtOrDefault(1) ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0)).InnerHtml.Split('<').FirstOrDefault();
            if (String.IsNullOrEmpty(rechtsnorm.SysNr))
                rechtsnorm.SysNr = null;
            rechtsnorm.Titel = (itemElements.Where(x => x.Name == "strong").ElementAtOrDefault(0) ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0)).InnerText;
            if (String.IsNullOrEmpty(rechtsnorm.Titel))
                rechtsnorm.Titel = null;
            rechtsnorm.Abk = (itemElements.Where(x => x.Name == "strong").ElementAtOrDefault(1) ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0)).InnerText.Replace("(", "").Replace(")", "");
            rechtsnorm.AusserKraft = rechtsnorm.Abk == "Ausser Kraft!";
            if ((bool)rechtsnorm.AusserKraft || String.IsNullOrEmpty(rechtsnorm.Abk))
                rechtsnorm.Abk = null;
            var urlelement = (itemElements.FirstOrDefault(x => x.Name == "a" && x.GetAttributeValue("href", String.Empty).StartsWith("http://www.lexfind.ch/dtah/")) ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0));
            rechtsnorm.LexFindUrl = urlelement.GetAttributeValue("href", String.Empty);
            if (String.IsNullOrEmpty(rechtsnorm.LexFindUrl))
                rechtsnorm.LexFindUrl = null;
            int lexFindId = -1;
            if (!String.IsNullOrEmpty(rechtsnorm.LexFindUrl) && Int32.TryParse(rechtsnorm.LexFindUrl.Split('/').ElementAtOrDefault(4), out lexFindId))
                rechtsnorm.LexFindId = lexFindId;
            rechtsnorm.OrigUrl = (itemElements.FirstOrDefault(x => x.Name == "a" && x.InnerText == "Original Link") ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0)).GetAttributeValue("href", String.Empty);
            if (String.IsNullOrEmpty(rechtsnorm.OrigUrl))
                rechtsnorm.OrigUrl = null;
            DateTime inKraftSeit;
            if (DateTime.TryParse(urlelement.InnerText.Split(' ').LastOrDefault(), out inKraftSeit))
                rechtsnorm.InKraftSeit = inKraftSeit;
            var inkrafttretenField = (itemElements.FirstOrDefault(x => x.Name == "td" && x.InnerText.Contains("Inkrafttreten:")) ?? new HtmlNode(HtmlNodeType.Element, new HtmlDocument(), 0)).InnerHtml.Replace("Inkrafttreten:", String.Empty).Split('<').FirstOrDefault() ?? String.Empty;
            DateTime infkrafttreten;
            if (DateTime.TryParse(inkrafttretenField, out infkrafttreten))
                rechtsnorm.Infkrafttreten = infkrafttreten;
            
            return rechtsnorm;
        }

    }
}
