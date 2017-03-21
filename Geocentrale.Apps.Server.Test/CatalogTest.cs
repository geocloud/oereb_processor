using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Geocentrale.Apps.Server;
using Geocentrale.TestUtils;

namespace Geocentrale.Apps.Server.Test
{
    [TestClass]
    public class CatalogTest
    {

        [TestInitialize]
        public void Initialize()
        {
            GATestLogConfiguration.ConfigureLog4NetLoggingInTest(GATestLogLevel.Error); //GATestLogLevel.Info
        }

        [TestMethod]
        public void CatalogIni()
        {
            var status = Catalog.Catalog.LoadAppDefinition("gis-daten-ag/nw/oereb");

            Assert.IsTrue(status.Successful);
            Assert.AreEqual(1, Global.Applications.Count);
            Assert.IsTrue(Global.DataSets.Count > 20);
            Assert.IsTrue(Global.Applications.First().Resources.Count > 20);
            Assert.IsTrue(Global.Applications.First().Settings.Count >= 1);
            Assert.IsTrue(Global.Applications.First().Topics.Count > 1);
            Assert.AreEqual(4, Global.ScalarClasses.Count);
        }
    }
}
