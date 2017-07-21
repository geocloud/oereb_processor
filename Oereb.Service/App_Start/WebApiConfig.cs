using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Oereb.Service
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            //*************************************************************************************************************
            // GetCapabilities

            config.Routes.MapHttpRoute(
                name: "GetCapabilities_2",
                routeTemplate: "oereb/capabilities",
                defaults: new { controller = "capability", action = "getcapabilities" }
            );

            //*************************************************************************************************************
            // GetExtractbyId

            config.Routes.MapHttpRoute(
                name: "GetExtractById_Variante_B_2",
                routeTemplate: "oereb/extract/{flavour}/{format}/geometry/{identdn}/{number}",
                defaults: new { controller = "extract", action = "extractByIdWithGeometry" }
            );

            config.Routes.MapHttpRoute(
                name: "GetExtractById_Variante_A_2",
                routeTemplate: "oereb/extract/{flavour}/{format}/geometry/{egrid}",
                defaults: new { controller = "extract", action = "extractByEgridWithGeometry" }
            );

            config.Routes.MapHttpRoute(
                name: "GetExtractById_Variante_B_1",
                routeTemplate: "oereb/extract/{flavour}/{format}/{identdn}/{number}",
                defaults: new { controller = "extract", action = "extractByIdWithoutGeometry" }
            );

            config.Routes.MapHttpRoute(
                name: "GetExtractById_Variante_A_1",
                routeTemplate: "oereb/extract/{flavour}/{format}/{egrid}",
                defaults: new { controller = "extract", action = "extractByEgridWithoutGeometry"}
            );

            //*************************************************************************************************************
            // Report (no federal specification)

            config.Routes.MapHttpRoute(
               name: "GetPdfFromXml",
               routeTemplate: "oereb/report/{flavour}",
               defaults: new { controller = "report", action = "getpdffromxml" }
           );

            //*************************************************************************************************************
            // GetEgrid

            config.Routes.MapHttpRoute(
               name: "GetEgridByCustom",
               routeTemplate: "oereb/getegrid/{value1}/{value2}",
               defaults: new { controller = "getegrid", action = "getegridbycustom" }
            );

            config.Routes.MapHttpRoute(
               name: "GetEgridByAdress",
               routeTemplate: "oereb/getegrid/{postalcode}/{localisation}/{number}",
               defaults: new { controller = "getegrid", action = "getegridbyadress" }
            );

            config.Routes.MapHttpRoute(
               name: "GetEgridByPos",
               routeTemplate: "oereb/getegrid",
               defaults: new { controller = "getegrid", action = "getegridbypos" }
           );

            //*************************************************************************************************************
            // Version

            config.Routes.MapHttpRoute(
                name: "GetVersions",
                routeTemplate: "oereb/versions",
                defaults: new { controller = "version", action = "getversions" }
            );

            //*************************************************************************************************************
            // Check

            config.Routes.MapHttpRoute(
                name: "CheckConnection",
                routeTemplate: "oereb/check/connection/{project}",
                defaults: new { controller = "check", action = "connection" }
            );

            config.Routes.MapHttpRoute(
                name: "CheckProcessor",
                routeTemplate: "oereb/check/processor/{project}",
                defaults: new { controller = "check", action = "processor" }
            );

            //*************************************************************************************************************
            // GetFile (no federal specification)

            config.Routes.MapHttpRoute(
                name: "GetFile",
                routeTemplate: "oereb/getfile/{format}/{file}/{saveFile}",
                defaults: new { controller = "file", action = "getfile" }
            );

            config.Routes.MapHttpRoute(
                name: "GetEFile",
                routeTemplate: "oereb/getefile",
                defaults: new { controller = "file", action = "getefile" }
            );

            //*************************************************************************************************************
            // Terravis Interface, Legacy

            config.Routes.MapHttpRoute(
                name: "TerravisGetByEgrid",
                routeTemplate: "terravis/GetReportByEgrid/{project}/{language}/{format}/{egrid}",
                defaults: new { controller = "terravis", action = "getreportbyegrid" }
            );

            //*************************************************************************************************************
            // GCG interface

            config.Routes.MapHttpRoute(
                name: "GetExtractByFilter",
                routeTemplate: "oereb/getbyfilter",
                defaults: new { controller = "query", action = "getextractbyfilter" }
            );

            //*************************************************************************************************************
            // Root (no federal specification)

            config.Routes.MapHttpRoute(
                name: "GetCapabilities_1",
                routeTemplate: "oereb",
                defaults: new { controller = "capability", action = "getcapabilities" }
            );
        }
    }
}
