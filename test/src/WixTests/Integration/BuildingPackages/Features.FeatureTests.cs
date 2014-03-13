//-----------------------------------------------------------------------
// <copyright file="Features.FeatureTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for authoring Features
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Features
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Tests for authoring Features
    /// </summary>
    [TestClass]
    public class FeatureTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Features\FeatureTests");

        [TestMethod]
        [Description("Verify that a simple Feature can be defined and that the expected default values are set")]
        [Priority(1)]
        public void SimpleFeature()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"SimpleFeature\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Title` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Description` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query3 = "SELECT `Display` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query4 = "SELECT `Level` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query5 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature1'";
            Verifier.VerifyQuery(msi, query1, "Feature 1");
            Verifier.VerifyQuery(msi, query2, "Test Feature 1");
            Verifier.VerifyQuery(msi, query3, "2");
            Verifier.VerifyQuery(msi, query4, "1");
            Verifier.VerifyQuery(msi, query5, "24");
      }

        [TestMethod]
        [Description("Verify that a feature Id is required")]
        [Priority(2)]
        public void MissingId()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"MissingId\product.wxs");
            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 10;
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Feature/@Id attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that a feature can be advertised")]
        [Priority(2)]
        public void AllowAdvertise()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"AllowAdvertise\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query1 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature2'";
            Verifier.VerifyQuery(msi, query, "0");
            Verifier.VerifyQuery(msi, query1, "8");
        }

        [TestMethod]
        [Description("Verify that the feature level can be set to any valid value")]
        [Priority(2)]
        public void FeatureLevel()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"FeatureLevel\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Level` FROM `Feature` WHERE `Feature` = 'Feature1'";
            Verifier.VerifyQuery(msi, query, "24698");
            string query1 = "SELECT `Level` FROM `Feature` WHERE `Feature` = 'Feature2'";
            Verifier.VerifyQuery(msi, query1, "101");
        }

        [TestMethod]
        [Description("Verify that a configurable directory can be set")]
        [Priority(2)]
        public void ConfigurableDirectory()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"ConfigurableDirectory\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Directory_` FROM `Feature` WHERE `Feature` = 'Feature1'";
            Verifier.VerifyQuery(msi, query, "TARGETDIR");
        }

        [TestMethod]
        [Description("Verify that the feature level is required")]
        [Priority(3)]
        public void MissingFeatureLevel()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"MissingFeatureLevel\product.wxs");
            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 10;
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Feature/@Level attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the initial display of a feature can be set")]
        [Priority(3)]
        public void FeatureDisplay()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"FeatureDisplay\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Display` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Display` FROM `Feature` WHERE `Feature` = 'Feature2'";
            string query3 = "SELECT `Display` FROM `Feature` WHERE `Feature` = 'Feature3'";
            string query4 = "SELECT `Display` FROM `Feature` WHERE `Feature` = 'Feature4'";
            Verifier.VerifyQuery(msi, query1, "2");
            Verifier.VerifyQuery(msi, query2, "3");
            Verifier.VerifyQuery(msi, query3, "0");
            Verifier.VerifyQuery(msi, query4, "7");
        }

        [TestMethod]
        [Description("Verify that a feature can be specified to allow or disallow it to be Absent")]
        [Priority(3)]
        public void Absent()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"Absent\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature2'";
            Verifier.VerifyQuery(msi, query1, "16");
            Verifier.VerifyQuery(msi, query2, "0");
        }

        [TestMethod]
        [Description("Verify that a the default install location of a feature can be set")]
        [Priority(3)]
        public void InstallDefault()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"InstallDefault\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature2'";
            string query3 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature3'";
            string query4 = "SELECT `Feature_Parent` FROM `Feature` WHERE `Feature` = 'Feature2'";
            Verifier.VerifyQuery(msi, query1, "0");
            Verifier.VerifyQuery(msi, query2, "2");
            Verifier.VerifyQuery(msi, query3, "1");
            Verifier.VerifyQuery(msi, query4, "Feature3");
        }

        [TestMethod]
        [Description("Verify that a the default advertise state of a feature can be set")]
        [Priority(3)]
        public void TypicalDefault()
        {
            string sourceFile = Path.Combine(FeatureTests.TestDataDirectory, @"TypicalDefault\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Attributes` FROM `Feature` WHERE `Feature` = 'Feature2'";
            Verifier.VerifyQuery(msi, query1, "4");
            Verifier.VerifyQuery(msi, query2, "0");
        }

    }
}
