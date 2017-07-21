﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oereb.Service.DataContracts
{
    public class Settings
    {
        public enum Flavour
        {
            Reduced,
            Full,
            Signed,
            Embeddable
        }

        public enum Format
        {
            Pdf,
            Xml,
            Json,
            Html,  //project specific
            PdfA1a //project specific
        }

        public enum Language
        {
            De,
            Fr,
            It,
            Rm
        }

        //TODO move setting in the web.config

        public static List<Flavour> SupportedFlavours { get; set; } = new List<Flavour> (){ Flavour.Reduced, Flavour.Full, Flavour.Embeddable};
        public static List<Format> SupportedFormats { get; set; } = new List<Format>() { Format.Pdf, Format.Xml };
        public static List<Language> SupportedLanguages { get; set; } = new List<Language>() { Language.De };

        public static List<string> AvailableCantonsLocal { get; set; }

        public static Dictionary<string, Uri>  AvailableCantonsRedirected { get; set; }

        public static Dictionary<string, List<string>>AvailableCantonsAndTopics { get; set; }

        public const string TopicAll = "ALL";
        public const string TopicAllFederal = "ALL_FEDERAL";
        public const char TopicSeparator = ',';
    }
}
