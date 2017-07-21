using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using Geocentrale.Apps.DataContracts;
using Geocentrale.Apps.Server.Export;
using Geocentrale.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Geocentrale.Apps.Server.Helper
{
    public class Metadata
    {
        public static ExpandoObject CreateSummaryFromReport(GAReport gAReport, bool addGeometry = false, bool addAttributes = false, bool includeHash = false)
        {
            var resultObject = new OerebResult(gAReport);

            dynamic summary = new ExpandoObject();

            summary.ParcelCount = resultObject.Parcels.Count;

            var parcelsExo = new List<ExpandoObject>();

            foreach (var parcel in resultObject.Parcels)
            {
                dynamic parcelExo = new ExpandoObject();

                parcelExo.Number = parcel.Nummer;
                parcelExo.Egrid = parcel.Egrid;

                var parts = gAReport.Header.ToString().Split(';');

                if (parts.Length == 3)
                {
                    parcelExo.CountInvolvedObjects = parts[1];
                    parcelExo.CountRuleResults = parts[2];
                }

                parcelExo.NotConcernedThemeCount = parcel.Sections[OerebResult.SectionType.NotConcernedTheme].Topics.Count;
                parcelExo.NotConcernedThemeString = parcel.Sections[OerebResult.SectionType.NotConcernedTheme].Topics.Select(x => x.Name).Distinct().OrderBy(x => x);

                parcelExo.ThemeWithoutDataCount = parcel.Sections[OerebResult.SectionType.ThemeWithoutData].Topics.Count;
                parcelExo.ThemeWithoutDataString = parcel.Sections[OerebResult.SectionType.ThemeWithoutData].Topics.Select(x => x.Name).Distinct().OrderBy(x => x);

                var topicsExo = new List<ExpandoObject>();

                foreach (var topic in parcel.Sections[OerebResult.SectionType.ConcernedTheme].Topics.OrderBy(x => x.Name))
                {
                    dynamic topicExo = new ExpandoObject();

                    topicExo.OerebDefsCount = topic.OerebDefs.Count;

                    var oerebDefsExo = new List<ExpandoObject>();

                    foreach (var oerebDef in topic.OerebDefs.OrderBy(x => x.Information).OrderBy(x => x.Features.Select(y => y.Guid.ToString()).Aggregate((i, j) => i + ";" + j)))
                    {
                        dynamic oerebDefExo = new ExpandoObject();

                        oerebDefExo.Information = oerebDef.Information;
                        oerebDefExo.FeatureCount = oerebDef.Features.Count();

                        var featuresExo = new List<ExpandoObject>();

                        foreach (var feature in oerebDef.Features)
                        {
                            dynamic featureExo = new ExpandoObject();

                            featureExo.Guid = feature.Guid.ToString();
                            featureExo.ShapeType = feature.GeometryTypeFeature.ToString();

                            if (includeHash)
                            {
                                featureExo.ShapeHash = CryptTasks.GetSha1Hash(feature.WktGeometry);
                            }

                            if (addGeometry)
                            {
                                featureExo.Shape = feature.WktGeometry;
                            }
                            
                            var values = new List<string>();

                            var attributesExo = new ExpandoObject() as IDictionary<string, Object>;
                         
                            foreach (var attribute in feature.Item.Value.Attributes.OrderBy(x => x.AttributeSpec.Name))
                            {
                                if (attribute.AttributeSpec.Name == feature.GaGeoClass.GeometryFieldName || attribute.AttributeSpec.Name == feature.GaGeoClass.ObjectIdFieldName)
                                {
                                    //ignore
                                    continue;
                                }

                                values.Add($"{attribute.AttributeSpec.Name} = {attribute.Value}");

                                var name = attribute.AttributeSpec.Name;

                                if (attributesExo.ContainsKey(name))
                                {
                                    name = $"name_{Guid.NewGuid()}";
                                }

                                attributesExo.Add(name,attribute.Value);
                            }

                            if (includeHash)
                            {
                                featureExo.AttributeHash = CryptTasks.GetSha1Hash(values.Aggregate((i, j) => i + ";" + j));
                            }

                            if (addAttributes)
                            {
                                featureExo.Attributes = attributesExo;
                            }                           

                            featuresExo.Add(featureExo);
                        }

                        oerebDefExo.features = featuresExo;

                        oerebDefExo.VorschriftArtikel = oerebDef.VorschriftArtikel.Count();
                        oerebDefExo.VorschriftRechtsDokument = oerebDef.VorschriftRechtsDokument.Count();

                        oerebDefExo.GesetzArtikel = oerebDef.GesetzArtikel.Count();
                        oerebDefExo.GesetzArtikel = oerebDef.GesetzRechtsDokument.Count();

                        oerebDefsExo.Add(oerebDefExo);
                    }

                    topicExo.OerebDefs = oerebDefsExo;
                    topicsExo.Add(topicExo);
                }

                parcelExo.topicsConcerned = topicsExo;
                parcelsExo.Add(parcelExo);
            }

            summary.Parcels = parcelsExo;

            return summary;
        }        
    }
}