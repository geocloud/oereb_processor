using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;
using Geocentrale.Apps.DataContracts;
using Geocentrale.DataAdaptors.Contracts;
using System.Reflection;
using Geocentrale.DataAdaptors.ArcgisServerRestAdaptor;

namespace Geocentrale.Apps.Server.Export
{
    public static class Common
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // TODO this is not oereb specific move to Geocentrale.DataAdaptors.Geometry in GAObjectExtensions class document it and test it.
        
        public static KeyValuePair<string, string> GetLegendFromFeature(GAObject gaObject)
        {
            if (gaObject == null)
            {
                log.Error("GetLegendFromFeature: gaObject is null");
                return new KeyValuePair<string, string>("", "");
            }

            var dataset = Catalog.Catalog.GetDataSetFromClass(gaObject.GAClass.Guid);

            if (dataset == null || !dataset.GetDataUtilities().Any())
            {
                log.Error("GetLegendFromFeature: dataset from gaObject is null");
                return new KeyValuePair<string, string>("", "");
            }

            var geoDataAdaptor = dataset.GetDataUtilities().Last() as IGAGeoDataAdaptor;

            if (geoDataAdaptor == null)
            {
                log.Error("GetLegendFromFeature: only geoDataAdaptor allowed");
                return new KeyValuePair<string, string>("", "");
            }

            var legendItem = geoDataAdaptor.GaGeoClass.Renderer.GetLegendItem(gaObject);

            if (legendItem.Value == null)
            {
                log.Warn("GetLegendFromFeature: legendItem is null");
                return new KeyValuePair<string, string>("", "");
            }

            return new KeyValuePair<string, string>(legendItem.Value.Key, Geocentrale.Common.ImageTasks.GetBase64StringFromImage(legendItem.Value.Image, ImageFormat.Png));
        }

        //TODO is this oerebAdaptor specific? move tho OerebAdaptor or to GeoDataAdaptor document it and test it.
        
        public static string GetLabel(IGAGeoDataAdaptor oerebDataAdaptor, GAObject gaObject)
        {
            if (!(oerebDataAdaptor is OerebAdaptor || oerebDataAdaptor is Geocentrale.DataAdaptors.PostgreSqlAdaptor.OerebAdaptor))
            {
                return $"{oerebDataAdaptor.GaGeoClass.Guid}.{gaObject[oerebDataAdaptor.GaGeoClass.ObjectIdFieldName]}";
            }

            PropertyInfo propertyFieldLabel = oerebDataAdaptor.GetType().GetProperty("_dynamicLegendFieldLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            //TODO GaClass from Adaptor and GaObject is inconsistent
            gaObject.GAClass.Legend = oerebDataAdaptor.GaClass.Legend;
            gaObject.GAClass.Renderer = oerebDataAdaptor.GaClass.Renderer;

            if (propertyFieldLabel != null && propertyFieldLabel.GetValue(oerebDataAdaptor) != null && !gaObject.GAClass.Labels.Any())
            {
                var fieldLabel = propertyFieldLabel.GetValue(oerebDataAdaptor) as string;
                gaObject.GAClass.Labels.Add("0", new GALabel(String.Format("#{0}#", fieldLabel), "text/plain", "Oereb"));
            }

            if (!gaObject.GAClass.Labels.Any())
            {
                if (gaObject.GAClass.Legend.LegendItems.Count > 0)
                {
                    var legendItem = oerebDataAdaptor.GaGeoClass.Renderer.GetLegendItem(gaObject);

                    //todo if the value in the object does not exist in the metadata (inconsistent):  legendItem.Value is null => exception

                    if (!String.IsNullOrEmpty(legendItem.Key) && !String.IsNullOrEmpty(legendItem.Value.Label))
                    {
                        var index = legendItem.Value.Label.IndexOf("#"); //legend separator in oereb projects, bad hack from the prototyp
                        if (index == -1)
                        {
                            //prio 2, take label from legend
                            return legendItem.Value.Label;                            
                        }

                        //prio 3, cut the bad part away
                        return legendItem.Value.Label.Substring(index + 1);
                    }
                }

                //prio 4, take label from gaClass
                var gaGeoClass = gaObject.GAClass as GAGeoClass;
                return gaGeoClass.Layername;
            }

           //prio 1, take label from labelengine
           return gaObject.GetLabel("Oereb").Value as string;
        }
    }
}