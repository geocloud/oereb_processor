﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oereb.Report.Helper
{
    public static class LocalisedUri
    {
        public static string GetStringFromArray(Service.DataContracts.Model.v04.LocalisedUri[] localisedUri, string language, string defaultValue = "-")
        {
            if (localisedUri.Any(x => x.Language.ToString() == language))
            {
                return localisedUri.First().Text;
            }

            return defaultValue;
        }
    }
}
