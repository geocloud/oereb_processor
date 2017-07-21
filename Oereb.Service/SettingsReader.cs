﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Oereb.Service.DataContracts;

namespace Oereb.Service
{
    public class SettingsReader
    {
        public static void ReadFromConfig()
        {
            Settings.AvailableCantonsAndTopics = new Dictionary<string, List<string>>();

            var sectionValidCantonsAndTopics = ConfigurationManager.GetSection("OerebSettings/AvailableCantonsAndTopics") as System.Collections.Hashtable;

            if (sectionValidCantonsAndTopics != null)
            {
                var validCantonsAndTopics = sectionValidCantonsAndTopics.Cast<System.Collections.DictionaryEntry>().ToDictionary(n => n.Key.ToString(), n => n.Value.ToString());

                foreach (var canton in validCantonsAndTopics)
                {
                    Settings.AvailableCantonsAndTopics.Add(canton.Key, canton.Value.Split(',').ToList());
                }
            }

            Settings.AvailableCantonsRedirected = new Dictionary<string, Uri>();

            var sectionAvailableCantonsRedirected = ConfigurationManager.GetSection("OerebSettings/AvailableCantonsRedirected") as System.Collections.Hashtable;

            if (sectionAvailableCantonsRedirected != null)
            {
                var validAvailableCantonsRedirected = sectionAvailableCantonsRedirected.Cast<System.Collections.DictionaryEntry>().ToDictionary(n => n.Key.ToString(), n => n.Value.ToString());

                foreach (var canton in validAvailableCantonsRedirected)
                {
                    Settings.AvailableCantonsRedirected.Add(canton.Key, new Uri(canton.Value, UriKind.Absolute));
                }
            }

            Settings.AvailableCantonsLocal = Settings.AvailableCantonsAndTopics.Where(x => !Settings.AvailableCantonsRedirected.ContainsKey(x.Key)).Select(x => x.Key).ToList();
        }
    }
}