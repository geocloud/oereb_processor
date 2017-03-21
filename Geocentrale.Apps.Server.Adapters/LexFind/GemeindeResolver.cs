using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geocentrale.Apps.Server.Adapters.LexFind
{
    public class GemeindeResolver
    {
        private List<Gemeinde> _gemeinden { get; set; }
        public List<Gemeinde> Gemeinden
        {
            get
            {
                if (_gemeinden == null)
                {
                    using (var fileReader = System.IO.File.OpenText(Common.PathTasks.GetBinDirectory().Parent.FullName + @"\Content\gemeinden.csv"))
                    {
                        using (var csvReader = new CsvHelper.CsvReader(fileReader))
                        {
                            csvReader.Configuration.Delimiter = ";";
                            this._gemeinden = csvReader.GetRecords<Gemeinde>().ToList();
                        }
                    }
                }
                return _gemeinden;
            }
        }
    }
}
