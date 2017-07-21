using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Geocentrale.Apps.Server.Helper
{
    public static class TasksExpandoObject
    {
        public static Dictionary<string,string>Keywords = new Dictionary<string, string>()
        {
            { "loadCatalog","True"},
            { "statusMapserver","True"},
            { "statusDb","True"},
            { "error",""},
            { "successful", "True"}
        };

        public static string ConvertToHtml(ExpandoObject value, bool status, bool includeTitle = true, bool includeBody = true)
        {
            var title = string.Empty;

            if (includeTitle)
            {
                title = status ? "<h1 style='color:55ff55;'>Global status is OK</h1>" : "<h1 style='color:ff5555;'>Global status is NOT OK</h1>";
            }

            var result = string.Empty;

            if (includeBody)
            {
                result = $"<html><body>{title}#taba#{ConvertToHtmlInternal(value, "", "", 0)}#tabe#</body></html>";
            }
            else
            {
                result = $"#taba#{ConvertToHtmlInternal(value, "", "", 0)}#tabe#";
            }

            result = result.Replace("#thar#", "<th style='vertical-align:top;text-align:left;background-color:#ffcccc;'>");
            result = result.Replace("#tdar#", "<td style='vertical-align:top;text-align:left;background-color:#ffcccc;'>");
            result = result.Replace("#thag#", "<th style='vertical-align:top;text-align:left;background-color:#ccffcc;'>");
            result = result.Replace("#tdag#", "<td style='vertical-align:top;text-align:left;background-color:#ccffcc;'>");

            result = result.Replace("#taba#", "<table border='1'>");
            result = result.Replace("#tabe#", "</table>");
            result = result.Replace("#tha#", "<th style='vertical-align:top;text-align: left;'>");
            result = result.Replace("#the#", "</th>");
            result = result.Replace("#tda#", "<td style='vertical-align:top;text-align: left;'>");
            result = result.Replace("#tde#", "</td>");
            result = result.Replace("#tra#", "<tr>");
            result = result.Replace("#tre#", "</tr>");

            return result;
        }

        private static string Highlight(object value)
        {
            var lineKey = ((KeyValuePair<string, object>)value).Key;
            var lineValue = ((KeyValuePair<string, object>)value).Value.ToString();

            foreach (var keyword in Keywords)
            {
                if (lineKey.Contains(keyword.Key))
                {
                    if (lineValue == keyword.Value)
                    {
                        return $"#tra##thag#{lineKey}#the##tdag#{lineValue}#tde##tre#";
                    }
                    else
                    {
                        return $"#tra##thar#{lineKey}#the##tdar#{lineValue}#tde##tre#";
                    }
                }
            }

            return $"#tra##tha#{lineKey}#the##tda#{lineValue}#tde##tre#";
        }

        private static string ConvertToHtmlInternal(object value, string path, string level, int index)
        {
            var response = string.Empty;

            if (value is KeyValuePair<string, object> && ((KeyValuePair<string, object>)value).Value is string)
            {
                response += Highlight(value);
                //response += $"#tra#tha{((KeyValuePair<string, object>)value).Key}#the#tda{((KeyValuePair<string, object>)value).Value.ToString()}#tde#tre";
            }
            else if (value is string)
            {
                response += $"#tra##tda#{value}#tde##tre#";
            }
            else if (value is ExpandoObject)
            {
                foreach (var item in (ExpandoObject)value)
                {
                    var newlevel = string.IsNullOrEmpty(level) ? item.Key : level + ";" + item.Key;
                    response += ConvertToHtmlInternal(item, path, newlevel, index + 1);
                }
            }
            else if (value is IEnumerable)
            {
                foreach (var item in (IEnumerable)value)
                {
                    response += ConvertToHtmlInternal(item, path, level, index + 1);
                }
            }
            else if (value is KeyValuePair<string, object> && ((KeyValuePair<string, object>)value).Value is IEnumerable)
            {
                response += $"#tra##tha#{((KeyValuePair<string, object>) value).Key}#the##tha##taba#";

                var newlevel = string.IsNullOrEmpty(level) ? ((KeyValuePair<string, object>)value).Key : level + ";" + ((KeyValuePair<string, object>)value).Key;

                foreach (var item in (IEnumerable)((KeyValuePair<string, object>)value).Value)
                {
                    response += ConvertToHtmlInternal(item, path, newlevel, index + 1);
                }

                response += $"#tabe##the##tre#";
            }
            else if (value is KeyValuePair<string, object> && ((KeyValuePair<string, object>)value).Value is ExpandoObject)
            {
                response += $"#tra##tha#{((KeyValuePair<string, object>)value).Key}#the##tha##taba#";

                var newlevel = string.IsNullOrEmpty(level) ? level : level + ";" + ((KeyValuePair<string, object>)value).Key;
                response += ConvertToHtmlInternal(((KeyValuePair<string, object>)value).Value, path, newlevel, index + 1);

                response += $"#tabe##the##tre#";
            }
            else if (value is KeyValuePair<string, object>)
            {
                response += Highlight(value);
                //response += $"#tra#tha{((KeyValuePair<string, object>)value).Key}#the#tda{((KeyValuePair<string, object>)value).Value.ToString()}#tde#tre";
            }
            else
            {
                response += $"#tra##tda#{value}#tde##tre#";
            }

            return response;
        }

        public static string ConvertToJson(ExpandoObject expandoObject)
        {
            return JsonConvert.SerializeObject(
                 expandoObject,
                 Formatting.Indented,
                 new JsonConverter[] { new StringEnumConverter() }
             );
        }
    }
}