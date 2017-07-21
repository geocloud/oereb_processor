using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web.Http;
using Geocentrale.Apps.Server;
using Geocentrale.Common;
using log4net;
using Oereb.Service.DataContracts;
using Oereb.Service.DataContracts.Model;

namespace Oereb.Service.Controllers
{
    public class CapabilityController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        public HttpResponseMessage GetCapabilities()
        {
            var config = Helper.Xml<Geocentrale.Apps.Server.Config>.DeserializeFromFile(Path.Combine(PathTasks.GetBinDirectory().Parent.FullName, "Config/base.xml"));

            var capabilities = new DataContracts.Model.GetCapabilitiesResponseType();
            var themes = new List<Theme>();
            var municipalities = new List<string>();

            foreach (var cantonShortcut in Settings.AvailableCantonsLocal)
            {
                var canton = config.Cantons.FirstOrDefault(x => x.Shorname == cantonShortcut);

                if (canton == null)
                {
                    Log.Fatal($"canton {cantonShortcut} not found in config");
                    continue;
                }

                foreach (var topic in canton.Topics)
                {
                    var theme = new Theme()
                    {
                        Code = topic.Code,
                        Text = new LocalisedText() { Language = LanguageCode.de, Text = topic.Name, LanguageSpecified = true}
                    };

                    if (themes.Any(x=> x.Code == theme.Code))
                    {
                        Log.Warn($"theme code {theme.Code} exists several times");
                    }

                    themes.Add(theme);
                }

                municipalities.AddRange(canton.Communities.Select(x=> x.Code));
            }

            capabilities.topic = themes.ToArray();
            capabilities.municipality = municipalities.ToArray();
            capabilities.flavour = Settings.SupportedFlavours.Select(x=> x.ToString()).ToArray();
            capabilities.language = Settings.SupportedLanguages.Select(x => x.ToString()).ToArray();
            capabilities.crs  = new string[] {"2056"};

            var xmlstring = Xml<GetCapabilitiesResponseType>.SerializeToXmlString(capabilities);

            return new HttpResponseMessage
            {
                Content = new StringContent(xmlstring, Encoding.UTF8, "application/xml")
            };
        }

    }
}
