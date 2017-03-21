using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Serialization;
using Geocentrale.Common;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using Org.BouncyCastle.Asn1.Mozilla;

namespace Geocentrale.Apps.Server
{
    [Serializable]
    public class Config
    {
        public enum LanguageCode
        {
            de,
            fr,
            it,
            rm,
            en,
        }

        [XmlIgnore]
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [XmlIgnore]
        public string RootPath { get; set; }

        public List<Canton> Cantons { get; set; }

        public string PathLogoNational { get; set; }
        public string PathLogoOereb { get; set; }
        public string PathNorthArrow { get; set; }

        [XmlIgnore]
        public Image Empty { get; set; }

        public Config()
        {
            RootPath = PathTasks.GetBinDirectory().Parent.FullName;
            Empty = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        }

        public Image LogoNational()
        {
            var filename = Path.Combine(RootPath, PathLogoNational);

            if (!File.Exists(filename))
            {
                log.Error($"imagefile logo national not found {filename}");
                return Empty;
            }

            return Image.FromFile(filename);
        }

        public Image LogoCanton(string canton)
        {
            var item = Cantons.FirstOrDefault(x => x.Name.ToLower() == canton.ToLower() || x.Shorname.ToLower() == canton.ToLower());

            if (item == null)
            {
                log.Error($"canton not found {canton} in config");
                return Empty;
            }

            var filename = Path.Combine(RootPath, item.PathLogo);

            if (!File.Exists(filename))
            {
                log.Error($"imagefile canton not found {filename}");
                return Empty;
            }

            return Image.FromFile(filename);
        }

        public Image LogoMunicipality(string municipality)
        {
            Municipality item = Cantons.SelectMany(canton => canton.Communities.Where(municipalityItem => municipalityItem.Name.ToLower() == municipality.ToLower())).FirstOrDefault();

            if (item == null)
            {
                log.Error($"municipality not found {municipality} in config");
                return Empty;
            }

            var filename = Path.Combine(RootPath, item.PathLogo);

            if (!File.Exists(filename))
            {
                log.Error($"imagefile municipality not found {filename}");
                return Empty;
            }

            return Image.FromFile(filename);
        }

        public Image LogoOereb()
        {
            var filename = Path.Combine(RootPath, PathLogoOereb);

            if (!File.Exists(filename))
            {
                log.Error($"imagefile logo oereb not found {filename}");
                return Empty;
            }

            return Image.FromFile(filename);
        }

        public Image LogoCadastralAuthority(string canton)
        {
            var item = Cantons.FirstOrDefault(x => x.Name.ToLower() == canton.ToLower() || x.Shorname.ToLower() == canton.ToLower());

            var filename = Path.Combine(RootPath, item.CadastralAuthority.PathLogo);

            if (!File.Exists(filename))
            {
                log.Error($"imagefile logo compagny not found {filename}");
                return Empty;
            }

            return Image.FromFile(filename);
        }

        public Image NorthArrow()
        {
            var filename = Path.Combine(RootPath, PathNorthArrow);

            if (!File.Exists(filename))
            {
                log.Error($"imagefile north arrow not found {filename}");
                return Empty;
            }

            return Image.FromFile(filename);
        }

        public Image QrCode(Uri url, int imageSize)
        {
            MemoryStream ms = new MemoryStream();
            QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.H);
            QrCode qrCode = new QrCode();

            qrEncoder.TryEncode(url.ToString(), out qrCode);

            var renderer = new GraphicsRenderer(new FixedCodeSize(imageSize, QuietZoneModules.Zero), Brushes.Black, Brushes.White);
            renderer.WriteToStream(qrCode.Matrix, ImageFormat.Png, ms);

           return new Bitmap(Image.FromStream(ms), new Size(imageSize,imageSize));
        }

        public Canton GetCantonFromMunicipality(string municipality)
        {
            return Cantons.FirstOrDefault(canton => canton.Communities.Any(municipalityItem => municipalityItem.Name.ToLower() == municipality.ToLower()));
        }

        public class Topic
        {
            public string NameEnum { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string LegendUrl { get; set; }
            public string LegendLabel { get; set; }
            public List<AdditionalLayer> AdditionalLayers { get; set; }

            public Topic()
            {
                AdditionalLayers = new List<AdditionalLayer>();
            }
        }

        public class AdditionalLayer
        {
            public Guid Guid { get; set; }
            public int Seq { get; set; } = 5;
            public double Transparency { get; set; } = 0.5;
            public bool Background { get; set; }
            public string Comment { get; set; }
        }

        public class Canton
        {
            public string Name { get; set; }
            public string Shorname { get; set; }
            public string OfficialNumberPrefix { get; set; }
            public string PathLogo { get; set; }
            public List<Municipality> Communities { get; set; }

            public Office CadastralAuthority { get; set; }

            public List<Glossary> Glossaries { get; set; }

            public List<LocalisedText> GeneralInformation { get; set; }

            public List<LocalisedText> Basedata { get; set; }

            public List<ExclusionOfLiability> ExclusionOfLiabilities { get; set; }

            public List<Resource> Resources { get; set; }

            public RealEstate RealEstate { get; set; }

            public List<Topic> Topics { get; set; }

            [XmlIgnore]
            public Dictionary<string, string> TopicsDictionary
            {
                get
                {
                    return Topics.ToDictionary(topic => topic.Name, topic => topic.NameEnum);
                }
            }

            public Canton()
            {
                Communities = new List<Municipality>();
                Glossaries = new List<Glossary>();
                Resources = new List<Resource>();
            }
        }

        public class RealEstate
        {
            public string LegendUrl { get; set; }
            public string LegendLabel { get; set; }
            public string WmsUrl { get; set; }
            public string FieldnameNumber { get; set; }
            public string FieldnameEgrid { get; set; }
            public string FieldnameBfsNumber { get; set; }
            public string FieldnameArea { get; set; }
            public string FieldnameMunicipality { get; set; }
        }

        public class Municipality
        {
            public string Name { get; set; }
            public string PathLogo { get; set; }
        }

        public class Office
        {
            public List<LocalisedText> Name { get; set; }
            public string OfficeAtWeb { get; set; }
            public string UID { get; set; }
            public string Line1 { get; set; }
            public string Line2 { get; set; }
            public string Street { get; set; }
            public string Number { get; set; }
            public string PostalCode { get; set; }
            public string City { get; set; }
            public string Email { get; set; } //aditional
            public string Phone { get; set; } //additional
            public string PathLogo { get; set; }

            public Office()
            {
                Name = new List<LocalisedText>();
            }
        }

        public class LocalisedText
        {
            public LanguageCode LanguageCode { get; set; }
            public string Text { get; set; }
        }

        public class Glossary
        {
            public List<LocalisedText> Title { get; set; }
            public List<LocalisedText> Content { get; set; }
        }

        public class ExclusionOfLiability
        {
            public List<LocalisedText> Title { get; set; }
            public List<LocalisedText> Content { get; set; }
        }

        public class Resource
        {
            public enum ResourceType
            {
                Layer,
                Selectionlayer,
                Baselayer,
                //Scalarservice,
            }

            public ResourceType Type { get; set; }

            public Guid Guid { get; set; }

            public int Seq { get; set; } = 5;

            public double Transparency { get; set; } = 0.5;

            public string Comment { get; set; }

            public string WmsUrl { get; set; }

            public string MetadataUrl { get; set; }

            public string AttributeLawStatus { get; set; }

            public string AttributeMoreInformation { get; set; } //weitere Informationen und Hinweise
        }
    }
}