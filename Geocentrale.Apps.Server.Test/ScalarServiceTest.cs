using System;
using System.Collections.Generic;
using System.Linq;
using Geocentrale.Apps.Db.Law;
using Geocentrale.Apps.Db.LexfindCache;
using Geocentrale.Apps.Db.RuleEngine;
using Geocentrale.Apps.Server.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Geocentrale.TestUtils;

namespace Geocentrale.Apps.Server.Test
{
    [TestClass]
    public class ScalarServiceTest
    {
        private static ScalarServiceAccess _scalarServiceAccess = new ScalarServiceAccess();
        private static readonly Guid _oerebGuid = Guid.Parse("0dec20e8-82cb-47a5-98c2-ac515fad10b9");
        private static readonly Guid _rechtsnormGuid = Guid.Parse("e2826482-300b-4437-b525-4055b7304e56");
        private static readonly Guid _artikelGuid = Guid.Parse("1c715771-0f6f-4a5a-961a-92c93cb374ad");
        private static readonly Guid _oerebkThemaGuid = Guid.Parse("8c7bcb11-3791-479f-b8a7-f227f1751ff9");
        private static readonly Guid _zustStelleGuid = Guid.Parse(" fd939ac3-94e5-4410-a789-223bf4972d78");

        [TestInitialize]
        public void Initialize()
        {
            GATestLogConfiguration.ConfigureLog4NetLoggingInTest(GATestLogLevel.Error); //GATestLogLevel.Info
        }

        [TestMethod, TestCategory("ScalarService"), TestCategory(GATestCategory.MUSTRUNTEST)]
        public void QueryById()
        {
            var status = Catalog.Catalog.LoadAppDefinition("gis-daten-ag/nw/oereb");

            Assert.IsTrue(status.Successful);

            //this is necessary otherwise the db connection fails in some parts

            LexFindCacheContainer dbLF = new LexFindCacheContainer();
            var dbRule = new RuleEngineContainer();
            var dbLaw = new OerebLaw3Container();

            var resultArtikel = _scalarServiceAccess.GetById(Global.ScalarClasses, _artikelGuid, new List<dynamic>() { (dynamic)60 });

            Assert.AreEqual(1, resultArtikel.Count);

            Assert.IsNotNull(Global.ScalarClasses.ContainsKey(_oerebGuid));

            var resultOereb = _scalarServiceAccess.GetById(Global.ScalarClasses, _oerebGuid, new List<dynamic>() { (dynamic)62 });

            Assert.AreEqual(1, resultOereb.Count);
            Assert.AreEqual("Nutzungsplanung Buochs", resultOereb.First()["Aussage"]);

            var resultOereb2 = _scalarServiceAccess.GetById(Global.ScalarClasses, _oerebGuid, new List<dynamic>() { (dynamic)"62" });

            Assert.AreEqual(1, resultOereb2.Count);
            Assert.AreEqual("Nutzungsplanung Buochs", resultOereb2.First()["Aussage"]);

            Assert.IsNotNull(Global.ScalarClasses.ContainsKey(_rechtsnormGuid));

            var resultRechtsnorm = _scalarServiceAccess.GetById(Global.ScalarClasses, _rechtsnormGuid, new List<dynamic>() { (dynamic)110 });

            Assert.AreEqual(1, resultRechtsnorm.Count);
            Assert.AreEqual("Bau- und Zonenreglement der Gemeinde Buochs", resultRechtsnorm.First()["Titel"]);

            Assert.IsNotNull(Global.ScalarClasses.ContainsKey(_oerebkThemaGuid));

            var resultOerebThema = _scalarServiceAccess.GetById(Global.ScalarClasses, _oerebkThemaGuid, new List<dynamic>() { (dynamic)1 });

            Assert.AreEqual(1, resultOerebThema.Count);
            Assert.AreEqual("73", resultOerebThema.First()["Beschreibung"]);

            Assert.IsNotNull(Global.ScalarClasses.ContainsKey(_artikelGuid));
        }
    }
}
