using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.DataContracts.Filter;
using Geocentrale.Common;
using Geocentrale.ConfigProcessor.GeoDataSet;
using Geocentrale.DataAdaptors;
using Geocentrale.DataAdaptors.Contracts;
using Geocentrale.Filter;
using Newtonsoft.Json;
using Oereb.Service.DataContracts;
using Oereb.Service.DataContracts.Model;
using Oereb.Service.Helper;

namespace Oereb.Service.Controllers
{
    public class GetEgridController : ApiController
    {
        /// <summary>
        /// search egrid by point
        /// </summary>
        /// <param name="XY">LV03 or LV95 point</param>
        /// <param name="GNSS">WGS84 point</param>
        /// <param name="radius">Default value is 1.0</param>
        /// <param name="canton">Optimization filtering NW | OW | UR</param>
        /// <param name="format">xml | json, empty or unknown value is a xml</param>
        /// <param name="extended">true add shape and kindof values</param>
        /// <returns></returns>

        [HttpGet]
        public HttpResponseMessage GetEgridByPos([FromUri] string XY = "", [FromUri] string GNSS = "", [FromUri] string radius = "1.0", [FromUri] string canton = "", [FromUri] string format = "", [FromUri] string extended = "", [FromUri] int crs = 0)
        {
            var point = CheckValues(XY, GNSS, radius, crs);

            if (point.Response != null)
            {
                return point.Response;
            }

            var queryObjects = DataAdaptor.CreateFilter(3, "", "", "", point, canton);

            var status = DataAdaptor.GetEgridFromDa(queryObjects, extended);

            return HandleResponse(status, format, extended);
        }

        /// <summary>
        /// the specification is not ver restful getEgridByNbIdent and GetEgridByAdresse without house number has to be at the same controller
        /// </summary>
        /// <param name="value1">nbident or postalcode</param>
        /// <param name="value2">number or localisation</param>
        /// <param name="canton">Optimization filtering NW | OW | UR</param>
        /// <param name="format">xml | json, empty or unknown value is a xml</param>
        /// <param name="extended">true add shape and kindof values</param>
        /// <returns></returns>

        [HttpGet]
        public HttpResponseMessage GetEgridByCustom(string value1, string value2, [FromUri] string canton = "", [FromUri] string format = "", [FromUri] string extended = "")
        {
            var queryObjects = DataAdaptor.CreateFilter(1, value1, value2, "", null, canton);

            var status = Helper.DataAdaptor.GetEgridFromDa(queryObjects, extended);

            return HandleResponse(status, format, extended);
        }

        /// <summary>
        /// search egrid by adress
        /// </summary>
        /// <param name="postalcode"></param>
        /// <param name="localisation"></param>
        /// <param name="number"></param>
        /// <param name="canton">Optimization filtering NW | OW | UR</param>
        /// <param name="format">xml | json, empty or unknown value is a xml</param>
        /// <param name="extended">true add shape and kindof values</param>
        /// <returns></returns>

        [HttpGet]
        public HttpResponseMessage GetEgridByAdress(string postalcode, string localisation, string number, [FromUri] string canton = "", [FromUri] string format = "", [FromUri] string extended = "")
        {
            var queryObjects = DataAdaptor.CreateFilter(2, postalcode, localisation, number, null, canton);

            var status = Helper.DataAdaptor.GetEgridFromDa(queryObjects, extended);

            return HandleResponse(status, format, extended);
        }

        #region private

        private HttpResponseMessage HandleResponse(GAStatus<GetEGRIDResponseType> status, string format, string extended)
        {
            if (!status.Successful)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError }; //error
            }

            if (status.Successful && status.Value.egrid.Length == 0)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent }; //nothing found
            }

            string xmlContent = string.Empty;

            if (extended.ToLower() == "true")
            {
                xmlContent = Helper.Xml<GetEGRIDResponseTypeExtended>.SerializeToXmlString(status.Value as GetEGRIDResponseTypeExtended);
            }
            else
            {
                xmlContent = Helper.Xml<GetEGRIDResponseType>.SerializeToXmlString(status.Value);
            }

            if (format == "json")
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlContent);
              
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeXmlNode(xmlDocument), Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(xmlContent, Encoding.UTF8, "application/xml")
            };
        }

        private DataAdaptor.Point CheckValues(string XY, string GNSS, string Radius, int crs)
        {
            double x = 0;
            double y = 0;
            double radius = 1;

            var point = new DataAdaptor.Point()
            {
                Crs = crs,
                X = x,
                Y = y,
                Radius = radius
            };

            if (string.IsNullOrEmpty(XY) && string.IsNullOrEmpty(GNSS))
            {
                point.Response = new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Parameter XY or GNSS is missing", Encoding.UTF8, "text/plain")
                };

                return point;
            }

            if (string.IsNullOrEmpty(Radius) || !Regex.IsMatch(Radius, @"^[0-9]{1,2}([.]{0,1}[0-9]{0,10})?$"))
            {
                point.Response = new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent($"Radius is invalid", Encoding.UTF8, "text/plain")
                };

                return point;
            }

            var parts = new string[] { };

            if (!string.IsNullOrEmpty(XY))
            {
                parts = XY.Split(',');
            }
            else
            {
                parts = GNSS.Split(',');
                crs = 4326;
            }

            if (parts.Length != 2 || !Regex.IsMatch(parts[0], @"^[0-9]{1,12}([.]{0,1}[0-9]{0,10})?$") || !Regex.IsMatch(parts[0], @"^[0-9]{1,12}([.]{0,1}[0-9]{0,10})?$"))
            {
                point.Response = new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent($"{(crs == 4326 ? "GNSS" : "XY")} values are a invalid expression", Encoding.UTF8, "text/plain")
                };

                return point;
            }

            radius = Convert.ToDouble(Radius);

            if (crs == 4326)
            {
                x = Convert.ToDouble(parts[1]);
                y = Convert.ToDouble(parts[0]);
            }
            else
            {
                if (crs == 0)
                {
                    x = Convert.ToDouble(parts[0]);
                    y = Convert.ToDouble(parts[1]);
                    crs = x > 2000000 ? 2056 : 21781;
                }
                else
                {
                    x = Convert.ToDouble(parts[0]);
                    y = Convert.ToDouble(parts[1]);
                }
            }

            point.X = x;
            point.Y = y;
            point.Crs = crs;
            point.Radius = radius;
            return point;
        }

        #endregion
    }
}
