using System;
using System.Net.Http;
using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oereb.Service.Controllers;
using Oereb.Service.Tests.Helper;

namespace Oereb.Service.Tests
{
    [TestClass]
    public class RoutesTest
    {
        HttpConfiguration _config;

        public RoutesTest()
        {
            _config = new HttpConfiguration();

            //add existing routes
            WebApiConfig.Register(_config);

            _config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
           _config.EnsureInitialized();
        }

        [TestMethod]
        public void CheckRouteExtractByEgrid()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/extract/reduced/pdf/CH710574347858?lang=de&topics=ALL");

            var routeTester = new RouteTester(_config, request);
            var controller = new ExtractController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((ExtractController p) => p.ExtractByEgridWithoutGeometry("pdf","reduced", "CH710574347858","De","ALL","","", false, false, true));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteExtractByEgridWithGeometry()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/extract/reduced/pdf/geometry/CH710574347858?lang=de&topics=ALL");

            var routeTester = new RouteTester(_config, request);
            var controller = new ExtractController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((ExtractController p) => p.ExtractByEgridWithGeometry("pdf", "reduced", "CH710574347858", "De", "ALL","","", false, false, true));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteExtractByNbIdent()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/extract/reduced/pdf/NW0200001502/684?lang=de&topics=ALL");

            var routeTester = new RouteTester(_config, request);
            var controller = new ExtractController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((ExtractController p) => p.ExtractByIdWithoutGeometry("pdf", "reduced", "NW0200001502", "684", "De", "ALL","", false, false, true));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteExtractByNbIdentWithGeometry()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/extract/reduced/pdf/geometry/NW0200001502/684?lang=de&topics=ALL");

            var routeTester = new RouteTester(_config, request);
            var controller = new ExtractController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((ExtractController p) => p.ExtractByIdWithGeometry("pdf", "reduced", "NW0200001502", "684", "De", "ALL","", false, false, true));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteGetEgridByXy()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/getegrid/?XY=1,2");

            var routeTester = new RouteTester(_config, request);
            var controller = new GetEgridController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((GetEgridController p) => p.GetEgridByPos("","","1.0","","","",0));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteGetEgridByGnss()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/getegrid/?GNSS=1,2");

            var routeTester = new RouteTester(_config, request);
            var controller = new GetEgridController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((GetEgridController p) => p.GetEgridByPos("", "", "1.0", "", "","",0));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteGetEgridByAdress()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/getegrid/1/2/3");

            var routeTester = new RouteTester(_config, request);
            var controller = new GetEgridController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((GetEgridController p) => p.GetEgridByAdress("", "", "", "", "",""));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteGetEgridByCustom()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/getegrid/1/2");

            var routeTester = new RouteTester(_config, request);
            var controller = new GetEgridController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((GetEgridController p) => p.GetEgridByCustom("", "", "", "",""));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteGetCapability1()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb");

            var routeTester = new RouteTester(_config, request);
            var controller = new CapabilityController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((CapabilityController p) => p.GetCapabilities());
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteGetCapability2()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/capabilities");

            var routeTester = new RouteTester(_config, request);
            var controller = new CapabilityController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((CapabilityController p) => p.GetCapabilities());
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckRouteReport()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://www.dummy.ch/oereb/report/falvour");

            var routeTester = new RouteTester(_config, request);
            var controller = new ReportController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((ReportController p) => p.GetPdfFromXml(""));
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }

        [TestMethod]
        public void CheckVersion()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://www.dummy.ch/oereb/versions");

            var routeTester = new RouteTester(_config, request);
            var controller = new VersionController();

            Assert.AreEqual(controller.GetType(), routeTester.GetControllerType());

            var actionName = ReflectionHelpers.GetMethodName((VersionController p) => p.GetVersions());
            Assert.AreEqual(actionName, routeTester.GetActionName());
        }
    }
}
