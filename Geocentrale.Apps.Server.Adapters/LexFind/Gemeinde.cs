using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geocentrale.Apps.Server.Adapters.LexFind
{
    public class Gemeinde
    {
        public string GDEKT { get; set; }
        public int GDEBZNR { get; set; }
        public int GDENR { get; set; }
        public string GDENAME { get; set; }
        public string GDENAMK { get; set; }
        public string GDEBZNA { get; set; }
        public string GDEKTNA { get; set; }
        public DateTime GDEMUTDAT { get; set; }

        public string KtNameNummer
        {
            get
            {
                return String.Format("{0}: {1} ({2})", this.GDEKT, this.GDENAME, this.GDENR);
            }
        }

    }
}
