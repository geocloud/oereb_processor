using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Common;
using Geocentrale.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace Geocentrale.Apps.Server.Common
{
    
    // TODO: there are interessting methods for other projects, move them to a separate library e.g. Geocentrale.DataAdaptors.Geometry! Document them! UnitTest them. Add Logging on Errors!
    public static class Geometry
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //TODO take constants from setting

        public static readonly string GeometryPartAttributeName = "GAGeometryPart";
        public static readonly string GeometryPartValueAttributeName = "GAGeometryPartValue";
        public static readonly string GeometryPartPercentageAttributeName = "GAGeometryPartPercentage";
        public static readonly string GeometryPartGeometryTypeAttributeName = "GAGeometryPartGeometryType";
        public static readonly string GeometryBufferAttributeName = "GAGeometryBuffer";
        public static readonly string RuleExpressionAttributeName = "GARuleExpression";
        public static readonly string NiceRuleExpressionAttributeName = "GANiceRuleExpression";

        public static Dictionary<string, GAAttributeSpec> ConstAttributeSpecs = new Dictionary<string, GAAttributeSpec>()
        {
            {"GAGeometryPart", new GAAttributeSpec(998,"GAGeometryPart","GAGeometryPart",typeof(string))},
            {"GAGeometryPartValue", new GAAttributeSpec(997,"GAGeometryPartValue","GAGeometryPartValue",typeof(double))}, 
            {"GAGeometryPartPercentage", new GAAttributeSpec(996,"GAGeometryPartPercentage","GAGeometryPartPercentage",typeof(double))}, 
            {"GAGeometryPartGeometryType", new GAAttributeSpec(992,"GAGeometryPartGeometryType","GAGeometryPartGeometryType",typeof(string))}, 
            {"GAGeometryBuffer", new GAAttributeSpec(995,"GAGeometryBuffer","GAGeometryBuffer",typeof(string))},
            {"GARuleExpression", new GAAttributeSpec(994,"GARuleExpression","GARuleExpression",typeof(string))},
            {"GANiceRuleExpression", new GAAttributeSpec(993,"GANiceRuleExpression","GANiceRuleExpression",typeof(string))},
        };

        static bool _initialized;

        public static void InitializeOgr()
        {
            if (!_initialized)
            {
                GdalConfiguration.ConfigureOgr();
                GdalConfiguration.ConfigureGdal();
                _initialized = true;
            }
        }

        [Obsolete]
        public static OSGeo.OGR.Geometry GetOGRGeometryFromString(string inGeometry)
        {
            InitializeOgr();
            OSGeo.OGR.Geometry geometry = null;

            try
            {
                geometry = Ogr.CreateGeometryFromWkt(ref inGeometry, new SpatialReference(""));
                return geometry;
            }
            catch (Exception ex)
            {
                log.Error(string.Format("*** conversion of Wkt fail, {0}", inGeometry),ex);
            }

            log.Error(string.Format("oereb should work only with wkt geometry, {0}", inGeometry));
            return geometry;
        }

        public static string Buffer(string featureGeometry, double offset)
        {
            InitializeOgr();
            OSGeo.OGR.Geometry featureGeometryOGR = GetOGRGeometryFromString(featureGeometry);
            OSGeo.OGR.Geometry bufferGeometry = featureGeometryOGR.Buffer(offset, 10);

            return bufferGeometry.ExportToGML();
        }

        public static string BufferWkt(string featureGeometry, double offset)
        {
            InitializeOgr();
            OSGeo.OGR.Geometry featureGeometryOGR = GetOGRGeometryFromString(featureGeometry);
            OSGeo.OGR.Geometry bufferGeometry = featureGeometryOGR.Buffer(offset, 10);

            string wkt;
            bufferGeometry.ExportToWkt(out wkt);        
            return wkt;
        }

        //[Obsolete]
        public static string GetGeometry(string featureGeometry, string format, int epsgCode = 0)
        {
            OSGeo.OGR.Geometry geometry = GetOGRGeometryFromString(featureGeometry);

            if (geometry == null)
            {
                return string.Empty;
            }

            if (epsgCode != 0)
            {
                var srs = new OSGeo.OSR.SpatialReference("");
                srs.ImportFromEPSG(epsgCode);
                geometry.AssignSpatialReference(srs);
            }

            switch (format)
            {
                case "geojson":

                    string[] options = null;
                    return geometry.ExportToJson(options);

                case "gml":

                    return geometry.ExportToGML(new string[]{ "FORMAT=GML3", "GML3_LONGSRS=YES/NO" });

                case "wkt":

                    string strGeometry;
                    geometry.ExportToWkt(out strGeometry);

                    return strGeometry;

                case "kml":

                    string altitude_mode = string.Empty;
                    return geometry.ExportToKML(altitude_mode);

                default:

                    break;
            }

            return string.Empty;
        }

        [Obsolete]
        public static bool Intersect(string featureGeometry1, string featureGeometry2)
        {
            OSGeo.OGR.Geometry featureGeometryOGR1 = GetOGRGeometryFromString(featureGeometry1);
            OSGeo.OGR.Geometry featureGeometryOGR2 = GetOGRGeometryFromString(featureGeometry2);

            if (featureGeometryOGR1 == null || featureGeometryOGR2 == null)
            {
                return false;
            }

            return featureGeometryOGR1.Intersect(featureGeometryOGR2);
        }

        [Obsolete]
        public static double[] GetGeometryExtent(string featureGeometry)
        {
            var envelope = new double[4];

            OSGeo.OGR.Geometry geometry = GetOGRGeometryFromString(featureGeometry);

            if (geometry == null)
            {
                return envelope;
            }

            var envelopeOgr = new Envelope();

            geometry.GetEnvelope(envelopeOgr);

            envelope[0] = envelopeOgr.MinX;
            envelope[1] = envelopeOgr.MinY;
            envelope[2] = envelopeOgr.MaxX;
            envelope[3] = envelopeOgr.MaxY;

            return envelope;
        }

        /// <summary>
        /// intersect selection- with involved objects => get area or length (problems are the geometrycollections from the intersection)
        /// </summary>
        /// <param name="inputFeature">normally parcel</param>
        /// <param name="involvedFeature">features from oereb definitions</param>
        /// <returns>add values to GaObject</returns>
        
        public static GAStatus FeatureIntersectCalculation(GAObject inputFeature, GAObject involvedFeature)
        {
            if (inputFeature == null || !(inputFeature.GAClass is GAGeoClass) || involvedFeature == null || !(involvedFeature.GAClass is GAGeoClass))
            {
                return new GAStatus(false, involvedFeature, GAStatus.LogLevel.Error, "FeatureIntersectCalculation, input- /involvedObject are empty");
            }

            var geometryFieldInput = (inputFeature.GAClass as GAGeoClass).GeometryFieldName;
            var geometryFieldInvolvedFeature = (involvedFeature.GAClass as GAGeoClass).GeometryFieldName;

            if (string.IsNullOrEmpty(inputFeature[geometryFieldInput]) || string.IsNullOrEmpty(involvedFeature[geometryFieldInvolvedFeature]))
            {
                return new GAStatus(false, involvedFeature, GAStatus.LogLevel.Error, "FeatureIntersectCalculation, geometry field is empty");
            }

            OSGeo.OGR.Geometry inputGeometry = GetOGRGeometryFromString(inputFeature[geometryFieldInput]);
            OSGeo.OGR.Geometry involvedGeometry = GetOGRGeometryFromString(involvedFeature[geometryFieldInvolvedFeature]);

            if (inputGeometry == null || involvedGeometry == null)
            {
                return new GAStatus(false, involvedFeature, GAStatus.LogLevel.Error, "FeatureIntersectCalculation, wkt geometry was not valid");
            }

            if (!(GeometryType(inputGeometry) == "area" && (GeometryType(involvedGeometry) == "area" || GeometryType(involvedGeometry) == "perimeter")))
            {
                return new GAStatus(true, involvedFeature, GAStatus.LogLevel.Info, "FeatureIntersectCalculation, no statistic necessary");
            }

            if (!inputGeometry.IsValid())
            {
                inputGeometry = inputGeometry.Simplify(0.001);
            }

            if (!involvedGeometry.IsValid())
            {
                involvedGeometry = involvedGeometry.Simplify(0.001);
            }

            if (!inputGeometry.IsValid() || !involvedGeometry.IsValid())
            {
                return new GAStatus(false, involvedFeature, GAStatus.LogLevel.Error, "input- or involved feature not valid");
            }

            double bufferDistanceInputobject = ConfigAccessTask.GetAppSettingsDouble(Setting.StatisticalBufferInputobject);

            OSGeo.OGR.Geometry intersectionGeometry = inputGeometry.Buffer(bufferDistanceInputobject, 12).Intersection(involvedGeometry); //check multipart features

            string wktGeometry = string.Empty;
            intersectionGeometry.ExportToWkt(out wktGeometry);

            if (!involvedFeature.GAClass.AttributeSpecs.Any(x => x.Name == GeometryPartAttributeName))
            {
                involvedFeature.GAClass.AttributeSpecs.Add(ConstAttributeSpecs[GeometryPartAttributeName]);
            }

            involvedFeature[GeometryPartAttributeName] = wktGeometry;

            if (!involvedFeature.GAClass.AttributeSpecs.Any(x => x.Name == GeometryPartGeometryTypeAttributeName))
            {
                involvedFeature.GAClass.AttributeSpecs.Add(ConstAttributeSpecs[GeometryPartGeometryTypeAttributeName]);
            }

            var typeOrigin = GeometryType(involvedGeometry); //the origin type gets a proper type definition (intersected geometry has in some cases a geometrycollection)

            var type = GeometryType(intersectionGeometry); //type from intersection is very often a geometrycollection (mix of points, lines and areas)

            involvedFeature[GeometryPartGeometryTypeAttributeName] = typeOrigin; //type;

            if (typeOrigin == "area")
            {
                double area = intersectionGeometry.Area();

                if (((GAGeoClass)involvedFeature.GAClass).SpatialReferenceEpsg == 3857)
                {
                    area = TransformGeometry(intersectionGeometry, 3857, 2056).Area();
                }

                if (!involvedFeature.GAClass.AttributeSpecs.Any(x => x.Name == GeometryPartValueAttributeName))
                {
                    involvedFeature.GAClass.AttributeSpecs.Add(ConstAttributeSpecs[GeometryPartValueAttributeName]);
                }

                involvedFeature[GeometryPartValueAttributeName] = area;

                double inputFeatureArea = inputGeometry.Area();

                if (((GAGeoClass)inputFeature.GAClass).SpatialReferenceEpsg == 3857)
                {
                    inputFeatureArea = TransformGeometry(inputGeometry, 3857, 2056).Area();
                }

                if (inputFeatureArea != 0)
                {
                    if (!involvedFeature.GAClass.AttributeSpecs.Any(x => x.Name == GeometryPartPercentageAttributeName))
                    {
                        involvedFeature.GAClass.AttributeSpecs.Add(ConstAttributeSpecs[GeometryPartPercentageAttributeName]);
                    }

                    involvedFeature[GeometryPartPercentageAttributeName] = area / inputFeatureArea;
                }
            }
            else if (typeOrigin == "perimeter")
            {
                if (!involvedFeature.GAClass.AttributeSpecs.Any(x => x.Name == GeometryPartValueAttributeName))
                {
                    involvedFeature.GAClass.AttributeSpecs.Add(ConstAttributeSpecs[GeometryPartValueAttributeName]);
                }

                involvedFeature[GeometryPartValueAttributeName] = intersectionGeometry.Length();

                if (((GAGeoClass)involvedFeature.GAClass).SpatialReferenceEpsg == 3857)
                {
                    involvedFeature[GeometryPartValueAttributeName] = TransformGeometry(intersectionGeometry, 3857, 2056).Length();
                }
            }
            else
            {
                //TODO handle mix Collections
                return new GAStatus(false, involvedFeature, GAStatus.LogLevel.Error, string.Format("intersected geometry is not supported, {0}, {1}", intersectionGeometry.GetGeometryType(), wktGeometry));
            }

            return new GAStatus(true, involvedFeature);
        }

        private static string GeometryType(OSGeo.OGR.Geometry geometry)
        {
            switch (geometry.GetGeometryType())
            {
                case wkbGeometryType.wkbMultiPoint:
                    return "point";
                case wkbGeometryType.wkbPolygon:
                case wkbGeometryType.wkbMultiPolygon:
                case wkbGeometryType.wkbLinearRing:
                    return "area";
                case wkbGeometryType.wkbLineString:
                case wkbGeometryType.wkbMultiLineString:
                    return "perimeter";
                case wkbGeometryType.wkbGeometryCollection:

                    var i = geometry.GetGeometryCount();
                    var type = string.Empty;

                    for (int j = 0; j < i; j++)
                    {
                        var typeInside = GeometryType(geometry.GetGeometryRef(j));

                        if (j > 0 && typeInside != type)
                        {
                            return "unknown";
                        }

                        type = typeInside;
                    }
                    return type;
                default:
                    return "unknown";
            }
        }

        public static Image RasterizeGeometry(string geometryWkt, double[] extent, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            Graphics graphic = Graphics.FromImage(bitmap);

            graphic.Clear(Color.Transparent);

            if (geometryWkt.Trim().ToLower().StartsWith("polygon"))
            {
                geometryWkt = $"MULTIPOLYGON({geometryWkt.ToLower().Replace("polygon","")})";
            }

            OSGeo.OGR.Geometry geometry = GetOGRGeometryFromString(geometryWkt);
            wkbGeometryType geometryType = geometry.GetGeometryType();

            if (geometry == null || !(geometryType == wkbGeometryType.wkbPolygon || geometryType == wkbGeometryType.wkbMultiPolygon) || width == 0 || height == 0)
            {
                return bitmap;
            }

            double conversionFactor = (extent[2] - extent[0]) / width; //scale is the same in both directions

            //draws first the transparent color and then the redline color (better results with multipart features)

            for (int colorIndex = 0; colorIndex < 2; colorIndex++)
            {
                for (int polygon = 0; polygon < geometry.GetGeometryCount(); polygon++)
                {
                    var polygonRef = geometry.GetGeometryRef(polygon);

                    //handles multipart features
                    for (int i = 0; i < polygonRef.GetGeometryCount(); i++)
                    {
                        var ring = polygonRef.GetGeometryRef(i);

                        var points = new List<PointF>();

                        for (int index = 0; index < ring.GetPointCount(); index++)
                        {
                            var point = new double[2];
                            ring.GetPoint(index, point);

                            var pX = (float)((point[0] - extent[0]) / conversionFactor);
                            var pY = (float)((extent[3] - point[1]) / conversionFactor);

                            points.Add(new PointF(pX, pY));
                        }

                        if (colorIndex == 0)
                        {
                            Color colorBg = ColorTranslator.FromHtml("#bbffffff"); // TODO: another config value candidate
                            var penBg = new Pen(colorBg, 20);
                            graphic.DrawLines(penBg, points.ToArray());
                        }
                        else
                        {
                            Color color = ColorTranslator.FromHtml("#77ff0000"); // TODO: another config value candidate
                            var pen = new Pen(color, 10);
                            graphic.DrawLines(pen, points.ToArray());
                        }

                        graphic.Flush();
                    }
                }
            }

            return bitmap;
        }

        public static List<GAObject> FilterObjects(List<GAObject> inputObjects, GAObject filterGeometry, string operation)
        {
            return inputObjects.Where(inputObject => TopologicalOperation(inputObject, filterGeometry, operation)).ToList();
        }

        private static bool IsGeoObject(GAObject gaObject)
        {
            if (gaObject == null || gaObject.GAClass == null)
            {
                return false;
            }

            return gaObject.GAClass is GAGeoClass;
        }

        public static bool TopologicalOperation(GAObject object1, GAObject object2, string operation)
        {
            if (!IsGeoObject(object1) || !IsGeoObject(object2))
            {
                return false;
            }

            string geometryString1 = object1[(object1.GAClass as GAGeoClass).GeometryFieldName];
            string geometryString2 = object2[(object2.GAClass as GAGeoClass).GeometryFieldName];

            if (string.IsNullOrEmpty(geometryString1) || string.IsNullOrEmpty(geometryString2))
            {
                return false;
            }

            OSGeo.OGR.Geometry geometry1 = GetOGRGeometryFromString(geometryString1);
            OSGeo.OGR.Geometry geometry2 = GetOGRGeometryFromString(geometryString2);

            if (geometry1 == null || geometry2 == null)
            {
                return false;
            }

            return TopologicalOperation(geometry1, geometry2, operation);
        }

        public static bool TopologicalOperation(OSGeo.OGR.Geometry object1, OSGeo.OGR.Geometry object2, string operation)
        {
            switch (operation)
            {
                case "intersect":
                    return object1.Intersect(object2);
            }

            return false;
        }

        public static OSGeo.OGR.Geometry TransformGeometry(OSGeo.OGR.Geometry srcGeometry, Int32 srcCrs, Int32 targetCrs)
        {
            OSGeo.OGR.Geometry transformedGeometry = srcGeometry.Clone();

            var srcSpatialRef = new SpatialReference(String.Empty);
            srcSpatialRef.ImportFromEPSG(srcCrs);

            var targetSpatialRef = new SpatialReference(String.Empty);
            targetSpatialRef.ImportFromEPSG(targetCrs);

            transformedGeometry.AssignSpatialReference(srcSpatialRef);
            transformedGeometry.TransformTo(targetSpatialRef);

            return transformedGeometry;
        }
    }
}