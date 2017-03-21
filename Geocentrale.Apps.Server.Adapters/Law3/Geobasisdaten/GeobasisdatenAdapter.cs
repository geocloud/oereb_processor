using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Geocentrale.Apps.Db.Geobasisdaten;
using HtmlAgilityPack;


namespace Geocentrale.Apps.Server.Adapters.Law3.Geobasisdaten
{
    public class GeobasisdatenAdapter
    {
        //private GeobasisdatenCacheContainer _db = new GeobasisdatenCacheContainer();

        private static readonly List<string> _kantone = new List<string> { "ZH", "BE", "LU", "UR", "SZ", "OW", "NW", "GL", "ZG", "FR", "SO", "BS", "BL", "SH", "AR", "AI", "SG", "GR", "AG", "TG", "TI", "VD", "VS", "NE", "GE", "JU" };
        private static readonly List<string> _syncKantone = new List<string> { "NW", "OW" }; //"ZH",
        private static int concurrency = 0;
        private static bool dblock = false;

        public void ClearDb()
        {
            GeobasisdatenCacheContainer db = new GeobasisdatenCacheContainer();
            var objCtx = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)db).ObjectContext;
            objCtx.ExecuteStoreCommand("DELETE FROM ThemaZustaendigeStelle");
            objCtx.ExecuteStoreCommand("DELETE FROM ThemaRechtsgrundlage");
            objCtx.ExecuteStoreCommand("DELETE FROM GemeindeThema");
            objCtx.ExecuteStoreCommand("DELETE FROM ZustaendigeStelleSet");
            objCtx.ExecuteStoreCommand("DELETE FROM RechtsgrundlageSet");
            objCtx.ExecuteStoreCommand("DELETE FROM ThemaSet");
            objCtx.ExecuteStoreCommand("DELETE FROM GemeindeSet");
            objCtx.ExecuteStoreCommand("DELETE FROM KantonSet");
        }

        public void DownloadAll(string path)
        {
            ClearDb();
            var queryLocs = new List<string>();
            //queryLocs.Add("CH");
            //queryLocs.AddRange(_kantone);
            using (var fileReader = File.OpenText(Path.Combine(path, "gemeindeverzeichnis.csv")))
            using (var csvReader = new CsvHelper.CsvReader(fileReader))
            {
                csvReader.Configuration.Delimiter = ";";
                while (csvReader.Read())
                {
                    if (_syncKantone.Any(x => x == csvReader.GetField<string>("GDEKT")))
                    {
                        queryLocs.Add(csvReader.GetField<string>("GDENR"));
                    }
                }
            }
            // Gemeindeliste: http://www.bfs.admin.ch/bfs/portal/de/index/infothek/nomenklaturen/blank/blank/gem_liste/03.html
            //http://www.geobasisdaten.ch/exportN.php?lang=de&loc=CH&s=cataloga&m=6
            //http://www.geobasisdaten.ch/exportN.php?lang=de&loc=ZH&s=cataloga&m=6
            //http://www.geobasisdaten.ch/exportN.php?lang=de&loc=1701&s=cataloga&m=6
            
            foreach (var queryLoc in queryLocs)
            {
                while (concurrency > 7)
                {
                    RandomSleep();
                }
                concurrency++;
                Console.WriteLine("Getting " + queryLoc);
                string url;
                //url = String.Format("http://www.geobasisdaten.ch/exportN.php?lang=de&loc={0}&s=cataloga&m=6", queryLoc);
                url = String.Format("http://www.geobasisdaten.ch/index.php?lang=de&loc={0}&s=catalog&m=6", queryLoc);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = "GET";
                //HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                webRequest.BeginGetResponse(new AsyncCallback(FinishWebRequest), Tuple.Create(webRequest, queryLoc));
            }

            while (concurrency > 0)
            {
                RandomSleep();
            }
        }

        private void FinishWebRequest(IAsyncResult result)
        {
            concurrency--;
            Tuple<HttpWebRequest, string> parameters = (Tuple<HttpWebRequest, string>)result.AsyncState;
            HttpWebResponse response = (parameters.Item1 as HttpWebRequest).EndGetResponse(result) as HttpWebResponse;
            var res = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("ISO-8859-1")).ReadToEnd();
            Console.WriteLine("Processing {0}", parameters.Item2);
            ParseResults(res);
        }

        public void ParseFromFiles()
        {
            ClearDb();
            string[] fileEntries = Directory.GetFiles(@"C:\lexFindSync\");
            foreach (string fileName in fileEntries)
            {
                if(fileName.EndsWith(".html"))
                {
                    String html;
                    using (StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("ISO-8859-1")))
                    {
                        html = sr.ReadToEnd();
                    }
                    Console.Write("Verarbeite {0}", fileName);
                    ParseResults(html);
                    Console.WriteLine();
                }
            }

        }

        public void ParseResults(string html)
        {
            while (dblock)
            {
                RandomSleep();
            }
            dblock = true;
            GeobasisdatenCacheContainer db = new GeobasisdatenCacheContainer();
            //_db.Configuration.AutoDetectChangesEnabled = false;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            //                                                /html/body[1]/div[2]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[7]/table[1]/tbody[1]/tr[4]
            //var metadata = doc.DocumentNode.SelectSingleNode("/html/body[1]/div[2]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[6]/table[1]/tbody[1]/tr[4]/th[2]").InnerText.Split(new string[] {"&nbsp"}, StringSplitOptions.None);
            //var kantonAbk = metadata[1].Trim(';');
            var kantonAbk = doc.DocumentNode.Descendants("a").FirstOrDefault(x => x.GetAttributeValue("onclick", string.Empty).StartsWith("chLoc") && !x.GetAttributeValue("onclick", string.Empty).Contains("CH")).GetAttributeValue("onclick", string.Empty).Substring(7, 2);
            //var kantonAbk = doc.DocumentNode.SelectSingleNode("/html/body[1]/div[2]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[6]/table[1]/tbody[1]/tr[2]/th[4]/a[1]").GetAttributeValue("onclick", string.Empty).Substring(7, 2);
            
            var kanton = db.KantonSet.FirstOrDefault(x => x.Abk == kantonAbk);
            if (kanton == null)
            {
                kanton = new Kanton();
                kanton.Abk = kantonAbk;
                //kanton.Name = doc.DocumentNode.SelectSingleNode("/html/body[1]/div[2]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[6]/table[1]/tbody[1]/tr[3]/th[2]").InnerText.Replace("Kanton ","");
                kanton.Name = doc.DocumentNode.Descendants("span").Where(x => x.GetAttributeValue("class", string.Empty) == "titel" && x.GetAttributeValue("style", string.Empty) == "text-indent:10px;font-size:10pt;").ToList()[1].InnerText.Replace("Kanton ", "");
                db.KantonSet.Add(kanton);
            }
            int bfsNr;
            //Int32.TryParse(metadata[2].Trim('(',')'), out bfsNr);
            var bfsString = doc.DocumentNode.Descendants("span").FirstOrDefault(x => x.GetAttributeValue("style", string.Empty) == "color:#aaaaaa;font-size:10pt").InnerText.Trim('(', ')');
            Int32.TryParse(bfsString, out bfsNr);
            var gemeinde = kanton.Gemeinde.FirstOrDefault(x => x.BfsNr == bfsNr);
            if (gemeinde == null)
            {
                gemeinde = new Gemeinde();
                gemeinde.BfsNr = bfsNr;
                gemeinde.Name = doc.DocumentNode.Descendants("span").FirstOrDefault(x => x.GetAttributeValue("class", string.Empty) == "titel" && x.GetAttributeValue("style",string.Empty) == "text-indent:10px;font-size:14pt;").InnerText.Split(new string[] {"&nbsp"},StringSplitOptions.None)[0].Replace("Gemeinde ", "");
                kanton.Gemeinde.Add(gemeinde);
            }
            db.SaveChanges(); 
            
            
            //Gemeinde Alpnach&nbsp;OW&nbsp(1401)
            //var t = metadataNode.InnerText;
            var laws = doc.DocumentNode.Descendants("tr").Where(x => x.Descendants("td").Any(y => y.GetAttributeValue("class", string.Empty) == "catalogL"));
            foreach (var law in laws)
            {
                var lawElements = law.Elements("td").ToList();
                Console.Write(".");
                var geobasisdatenId = GetCleanCellContents(lawElements[0]);
                var thema = db.ThemaSet.FirstOrDefault(x => x.GeobasisdatenId == geobasisdatenId);
                if (thema != null)
                {
                    gemeinde.Thema.Add(thema);
                    db.SaveChanges();
                    continue;
                }
                thema = new Thema();
                gemeinde.Thema.Add(thema);
                Console.Write("+");
                thema.GeobasisdatenId = geobasisdatenId;
                thema.Bezeichnung = GetCleanCellContents(lawElements[1]);
                // rechtsgrundlagen
                var rgElements = lawElements[2].Descendants("td");
                foreach (var rgElement in rgElements)
                {
                    var fedLevel = GetFederalismLevel(rgElement);
                    var rgLinks = rgElement.Descendants("a");
                    foreach (var lawLink in rgLinks)
                    {
                        var systNr = lawLink.Element("span").InnerText;
                        var artikelNr = lawLink.NextSibling.InnerText.Trim();
                        Rechtsgrundlage rechtsgrundlage = db.RechtsgrundlageSet.FirstOrDefault(x => x.FedLevel == fedLevel && x.SystNr == systNr && x.ArtikelNr == artikelNr);
                        if (rechtsgrundlage == null)
                        {
                            rechtsgrundlage = new Rechtsgrundlage();
                            rechtsgrundlage.SystNr = systNr;
                            rechtsgrundlage.FedLevel = fedLevel;
                            rechtsgrundlage.ArtikelNr = artikelNr;
                            rechtsgrundlage.Url = lawLink.GetAttributeValue("href", string.Empty);
                            rechtsgrundlage.Name = lawLink.GetAttributeValue("title", string.Empty).Substring(5);
                        }
                        thema.Rechtsgrundlage.Add(rechtsgrundlage);
                    }
                }
                // zuständige stellen
                var zuststElements = lawElements[3].Descendants("td");
                foreach (var zuststElement in zuststElements)
                {
                    var fedLevel = GetFederalismLevel(zuststElement);
                    var zuststLinks = zuststElement.Descendants("a");
                    foreach (var respLink in zuststLinks)
                    {
                        var abk = respLink.InnerText;
                        ZustaendigeStelle zustaendigeStelle = db.ZustaendigeStelleSet.FirstOrDefault(x => x.FedLevel == fedLevel && x.Abk == abk);
                        if (zustaendigeStelle == null)
                        {
                            zustaendigeStelle = new ZustaendigeStelle();
                            zustaendigeStelle.FedLevel = fedLevel;
                            zustaendigeStelle.Abk = abk;
                            zustaendigeStelle.Url = respLink.GetAttributeValue("href", string.Empty);
                            zustaendigeStelle.Name = respLink.GetAttributeValue("title", string.Empty);
                        }
                        thema.ZustaendigeStelle.Add(zustaendigeStelle);
                    }
                }

                thema.Klasse = GetKlasseFromKlasseString(GetCleanCellContents(lawElements[4]));
                thema.Georeferenzdaten = (!String.IsNullOrEmpty(GetCleanCellContents(lawElements[5]))).ToString();
                thema.OerebRelevant = GetOerebFromOerebElement(lawElements[6]);
                thema.Zugangsberechtigung = GetZugangsberechtigungFromZugangsberechtigungString(GetCleanCellContents(lawElements[7]));
                thema.Downloaddienst = (!String.IsNullOrEmpty(GetCleanCellContents(lawElements[8]))).ToString();

                db.SaveChanges();  
            }
            //_db.ChangeTracker.DetectChanges();
            // _db.SaveChanges();  
            db.SaveChanges();  
            dblock = false;
        }

        private static short GetFederalismLevel(HtmlNode lawRefElement)
        {
            var style = lawRefElement.GetAttributeValue("style", string.Empty);
            var color = style.Substring(style.IndexOf("color") + 7, 6);
            switch (color)
            {
                case "ffeeee": //bund
                    return 1;
                case "ddeeff": //kanton
                    return 2;
                case "ffffaa": //gemeinde
                    return 3;
                default:
                    return -1;
            }
        }

        private static string GetCleanCellContents(HtmlNode htmlNode)
        {
            return htmlNode.InnerText.Replace("\\n", "").Replace("&nbsp;","").Trim();
        }
        private static short GetKlasseFromKlasseString(string klasse)
        {
            switch (klasse)
            {
                case "I":
                    return 1;
                case "II":
                    return 2;
                case "III":
                    return 3;
                case "IV":
                    return 4;
                case "V":
                    return 5;
                case "VI":
                    return 6;
                default:
                    return -1;
            }
        }
        private static short GetZugangsberechtigungFromZugangsberechtigungString(string zugangsberechtigung)
        {
            switch (zugangsberechtigung)
            {
                case "A":
                    return 1;
                case "B":
                    return 2;
                case "C":
                    return 3;
                default:
                    return -1;
            }
        }
        private static short GetOerebFromOerebElement(HtmlNode oerebElement)
        {
            var span = oerebElement.Element("span");
            if (span == null)
            {
                return -1;
            }
            var style = span.GetAttributeValue("style", string.Empty);
            var color = style.Substring(style.IndexOf("color") + 7, 6);
            switch (color)
            {
                case "ff0000": //bund
                    return 1;
                case "0000ff": //kanton
                    return 2;
                case "00ff00": //gemeinde
                    return 3;
                default:
                    return -1;
            }
        }
        private void RandomSleep()
        {
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(200, 500));
        }
    }
}

