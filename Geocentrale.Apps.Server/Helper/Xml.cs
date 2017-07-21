using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Geocentrale.Apps.DataContracts;
using Oereb.Service.DataContracts;

namespace Geocentrale.Apps.Server.Helper
{
    public static class Xml<T>
    {

        public static string SerializeToXmlString(T value)
        {
            if (value == null)
            {
                throw new Exception("serialize value is null");
            }

            var serializer = new XmlSerializer(typeof(T));
            using (var memStm = new MemoryStream())
            using (var xw = XmlWriter.Create(memStm))
            {
                serializer.Serialize(xw, value);
                var buffer = memStm.ToArray();
                var xmlContent = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                if (xmlContent[0] == 65279)
                {
                    xmlContent = xmlContent.Substring(1);
                }

                return xmlContent;
            }

            //XmlSerializer serializer = new XmlSerializer(typeof(T));

            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Encoding = new UTF8Encoding(); //new UnicodeEncoding(false, false); // no BOM in a .NET string
            //settings.Indent = false;
            //settings.OmitXmlDeclaration = false;

            //using (StringWriter textWriter = new StringWriter())
            //{
            //    using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
            //    {
            //        serializer.Serialize(xmlWriter, value);
            //    }
            //    return textWriter.ToString();
            //}
        }

        public static T DeserializeFromXmlString(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlReaderSettings settings = new XmlReaderSettings();

            using (StringReader textReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(textReader, settings))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }

        public static void SerializeToFile(T value, string filename)
        {
            var xml = SerializeToXmlString(value);

            try
            {
                File.WriteAllText(filename, xml);
            }
            catch (Exception ex)
            {
                throw new Exception($"could not serialize to file {filename}", ex);
            }

        }

        public static T DeserializeFromFile(string filename)
        {
            try
            {  
                using (StreamReader streamReader = new StreamReader(filename))
                {
                    String value = streamReader.ReadToEnd();
                    return DeserializeFromXmlString(value);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"could not deserialize from file {filename}", ex);
            }
        }

        public static GAStatus Validate(string xmlPath, string pathSchema)
        {
            XDocument document = XDocument.Load(xmlPath);

            if (!string.IsNullOrEmpty(pathSchema))
            {
                SetAbsoluteSchemaPath(document, pathSchema);
            }

            document.Save(xmlPath.Replace(".xml", "_schemapath.xml"));

            document = XDocument.Load(xmlPath.Replace(".xml", "_schemapath.xml"));

            var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            //settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            //settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            //settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            //settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;

            settings.DtdProcessing = DtdProcessing.Parse;

            var status = new GAStatus(true);

            settings.ValidationEventHandler += (o, args) => status.Add(ValidationCallBack(o, args));

            try
            {
                var reader = XmlReader.Create(document.CreateReader(), settings);
                while (reader.Read())
                {
                    // nothing to do here
                }
            }
            catch (Exception ex)
            {
                var msg = "error during parse " + ex.Message;
                status.Add(new GAStatus(false, msg, ex));
            }

            return status;
        }

        private static GAStatus ValidationCallBack(object sender, ValidationEventArgs args)
        {
            var exception = (args.Exception as XmlSchemaValidationException);
            var msg = args.Message + "SourceUri: " + args.Exception.SourceUri + " Line:" + args.Exception.LineNumber + " Position:" + args.Exception.LinePosition;
            var element = exception?.SourceObject as XElement;

            if (element != null)
            {
                var si = element.GetSchemaInfo();
                msg += " Schema: " + si;
            }

            return new GAStatus(false, msg, args.Exception);
        }

        private static void SetAbsoluteSchemaPath(XDocument document, string schemaPath)
        {
            if (document.Root == null)
            {
                return;
            }

            XNamespace ns = "http://www.w3.org/2001/XMLSchema-instance";
            var attributeXName = ns + "schemaLocation";
           
            var attribute = document.Root.Attribute(attributeXName);

            if (attribute == null)
            {
                var xAttribute = new XAttribute(attributeXName, "http://schemas.geo.admin.ch/swisstopo/OeREBK/15/ExtractData " + schemaPath);
                document.Root.Add(xAttribute);
            }
            else
            {
                attribute.Value = "http://schemas.geo.admin.ch/swisstopo/OeREBK/15/ExtractData " + schemaPath;
            }
        }
    }
}