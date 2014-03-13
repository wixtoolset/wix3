//-----------------------------------------------------------------------
// <copyright file="Bundle.BundleElementTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Bundle Element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for Bundle
    /// </summary>
    [TestClass]
    public class BundleElementTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\BundleElementTests");

        [TestMethod]
        [Description("Verify that build fails if UX element defined more than once under Bundle.")]
        [Priority(3)]
        public void BundleDoubleUX()
        {
            string sourceFile = Path.Combine(BundleElementTests.TestDataDirectory, @"BundleDoubleUX\BundleDoubleUX.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedWixMessages.Add(new WixMessage(41, @"The Bundle element contains multiple UX child elements.  There can only be one UX child element per Bundle element.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 41;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that build fails if UX element is not defined under Bundle.")]
        [Priority(3)]
        public void BundleMissingUX()
        {
            string sourceFile = Path.Combine(BundleElementTests.TestDataDirectory, @"BundleMissingUX\BundleMissingUX.wxs");
            
            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedWixMessages.Add(new WixMessage(63, @"A Bundle element must have at least one child element of type UX.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 63;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that build fails if Chain element is not defined under Bundle.")]
        [Priority(3)]
        public void BundleMissingChain()
        {
            string sourceFile = Path.Combine(BundleElementTests.TestDataDirectory, @"BundleMissingChain\BundleMissingChain.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedWixMessages.Add(new WixMessage(63, @"A Bundle element must have at least one child element of type Chain.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 63;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that build fails if a Payload element is a child of Bundle")]
        [Priority(3)]
        public void BundleWithPayloadChild()
        {
            string sourceFile = Path.Combine(BundleElementTests.TestDataDirectory, @"BundleWithPayloadChild\BundleWithPayloadChild.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            string message = @"The Bundle element contains an unexpected child element 'Payload'.";
            candle.ExpectedWixMessages.Add(new WixMessage(5, message, WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that build fails if a PayloadGroup element is a child of Bundle")]
        [Priority(3)]
        public void BundleWithPayloadGroupChild()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(BundleElementTests.TestDataDirectory, @"BundleWithPayloadGroupChild\BundleWithPayloadGroupChild.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(5, "The Bundle element contains an unexpected child element 'PayloadGroup'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that build fails if there are multiple Bundle elements defined.")]
        [Priority(3)]
        public void MultipleBundleElements()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(BundleElementTests.TestDataDirectory, @"MultipleBundleElements\Product.wxs"));
            candle.SourceFiles.Add(Path.Combine(BundleElementTests.TestDataDirectory, @"MultipleBundleElements\SecondBundleElement.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(89,Message.MessageTypeEnum.Error)); // Multiple entry sections error
            light.ExpectedWixMessages.Add(new WixMessage(90,Message.MessageTypeEnum.Error)); // Location of entry section related to previous error.
            light.ExpectedExitCode = 90;
            light.Run();
        }

        [TestMethod]
        [Description("Verify that Bundle element properties are transalated into correct registration information for the engin.")]
        [Priority(2)]
        public void ValidBundleElement()
        {
            string sourceFile = Path.Combine(BundleElementTests.TestDataDirectory, @"ValidBundleElement\Product.wxs");
            string outputDirectory = this.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            BundleElementTests.VerifyRegistrationInformation(outputDirectory, "Product.exe", "Wix Test Bundle", "1.0.0.0", "Microsoft Corporation", "{d4cd70bc-7abd-4fcd-8e10-c8db53c73415}", 
                                                            "http://wix.sourceforge.net/about","http://wix.sourceforge.net/help","http://wix.sourceforge.net/update","555-345-2556", true, false, true);
        }

        #region Verification Methods

        /// <summary>
        /// Verifies Registration information in Burn_Manifest.xml
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>/// <param name="expectedExecutableName"></param>
        /// <param name="expectedDisplayName">Expected value of @Name</param>
        /// <param name="expectedVersion">Expected value of @Version</param>
        /// <param name="expectedManufacturer">Expected value of @Manufacturer</param>
        /// <param name="expectedUpgradeCode">Expected value of @UpgradeCode</param>
        /// <param name="expectedAboutUrl">Expected value of @AboutUrl</param>
        /// <param name="expectedHelpUrl">Expected value of @HelpUrl</param>
        /// <param name="expectedUpdateUrl">Expected value of @UpdateUrl</param>
        /// <param name="expectedHelpTelephone">Expected value of @HelpTelephone</param>
        /// <param name="expectedDisableModify">Expected value of @DisableModify</param>
        /// <param name="expectedDisableRemove">Expected value of @DisableRemove</param>
        /// <param name="expectedDisableRepair">Expected value of @DiableRepair</param>
        public static void VerifyRegistrationInformation(string embededResourcesDirectoryPath, string expectedExecutableName, string expectedDisplayName, string expectedVersion, string expectedManufacturer, 
                                                         string expectedUpgradeCode, string expectedAboutUrl, string expectedHelpUrl, string expectedUpdateUrl,  string expectedHelpTelephone,
                                                         bool expectedDisableModify, bool expectedDisableRemove, bool expectedDisableRepair)
        {
            string burnManifestXRegistrationPath = @"//burn:Registration";
            XmlNodeList burnManifestRegistrationNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXRegistrationPath);
            Assert.AreEqual(1, burnManifestRegistrationNodes.Count, "No Registration node was found in Burn_Manifest.xml.");
            BundleTests.VerifyAttributeValue(burnManifestRegistrationNodes[0], "ExecutableName", expectedExecutableName);
            BundleTests.VerifyAttributeValue(burnManifestRegistrationNodes[0], "Version", expectedVersion);
            BundleTests.VerifyAttributeValue(burnManifestRegistrationNodes[0], "UpgradeCode", expectedUpgradeCode);

            string burnManifestArpXPath = @"//burn:Registration/burn:Arp";
            XmlNodeList burnManifestArpNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestArpXPath);
            Assert.AreEqual(1, burnManifestArpNodes.Count, "No Registration/Arp node was found in Burn_Manifest.xml.");
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "DisplayName", expectedDisplayName);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "Publisher", expectedManufacturer);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "HelpLink", expectedHelpUrl);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "HelpTelephone", expectedHelpTelephone);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "AboutUrl", expectedAboutUrl);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "UpdateUrl", expectedUpdateUrl);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "DisableModify", expectedDisableModify ? "yes" : null);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "DisableRepair", expectedDisableRepair ? "yes" : null);
            BundleTests.VerifyAttributeValue(burnManifestArpNodes[0], "DisableRemove", expectedDisableRemove ? "yes" : null);
        }

        #endregion

    }
}
