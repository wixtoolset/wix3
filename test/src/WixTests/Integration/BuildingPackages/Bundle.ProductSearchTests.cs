//-----------------------------------------------------------------------
// <copyright file="Bundle.ProductSearchTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Bundle ProductSearch element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// Tests for Bundle ProductSearch element
    /// </summary>
    public class ProductSearchTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\ProductSearchTests");

        [NamedFact]
        [Description("ProductSearch @Variable is required.")]
        [Priority(3)]
        public void ProductSearchVariableMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ProductSearchTests.TestDataDirectory, @"ProductSearchVariableMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:ProductSearch/@Variable attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("ProductSearch @Guid is required.")]
        [Priority(3)]
        public void ProductSearchGuidMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ProductSearchTests.TestDataDirectory, @"ProductSearchGuidMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:ProductSearch/@Guid attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("ProductSearch @Guid is not a valid GUID.")]
        [Priority(3)]
        public void ProductSearchInvalidGuid()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ProductSearchTests.TestDataDirectory, @"ProductSearchInvalidGuid\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(9, "The util:ProductSearch/@Guid attribute's value, 'Not_A_Product_Guid', is not a legal guid value.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 9;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("ProductSearch @Variable should not be a predefined variable.")]
        [Priority(3)]
        [Trait("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=2980329&group_id=105970")]
        public void ProductSearchPredefinedVariable()
        {
            string expectedErrorMessage = @"The util:ProductSearch/@Variable attribute's value, 'ProgramFilesFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFilesFolder', 'CompatibilityMode', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'LocalAppDataFolder', 'MyPicturesFolder', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProgramFilesFolder', 'ProgramMenuFolder', 'SendToFolder', 'StartMenuFolder', 'StartupFolder', 'SystemFolder', 'TempFolder', 'TemplateFolder', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsFolder', or 'WindowsVolume'.";

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ProductSearchTests.TestDataDirectory, @"ProductSearchPredefinedVariable\Product.wxs"));
            candle.OutputFile = "Setup.exe";
            candle.Extensions.Add("WixUtilExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(348, expectedErrorMessage, Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 348;
            candle.Run();
        }

        [NamedFact]
        [Description("ProductSearch @Result contains invalid value (something other than Version, Language, Statem and Assignment)")]
        [Priority(3)]
        public void ProductSearchInvalidResultValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ProductSearchTests.TestDataDirectory, @"ProductSearchInvalidResultValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The util:ProductSearch/@Result attribute's value, 'NotExists', is not one of the legal options: 'Version', 'Language', 'State', or 'Assignment'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 21;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("Cannot have dupplicate ProductSearch with the same id.")]
        [Priority(3)]
        public void DuplicateProductSearch()
        {
            string sourceFile = Path.Combine(ProductSearchTests.TestDataDirectory, @"DuplicateProductSearch\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceFile }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(91, Message.MessageTypeEnum.Error));  //  duplicate symbol error
            light.ExpectedWixMessages.Add(new WixMessage(92, Message.MessageTypeEnum.Error));  //  Location of symbol related to previous error.
            light.ExpectedExitCode = 92;
            light.IgnoreWixMessageOrder = true;
            light.Run();
        }

        [NamedFact]
        [Description("After contains an Id of a missing search.")]
        [Priority(3)]
        public void ProductSearchAfterUndefinedSearch()
        {
            string sourceFile = Path.Combine(ProductSearchTests.TestDataDirectory, @"ProductSearchAfterUndefinedSearch\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceFile }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(94, Message.MessageTypeEnum.Error));  //  Unresolved reference to symbol 'WixSearch:UndefinedSearch'
            light.ExpectedExitCode = 94;
            light.Run();
        }

        [NamedFact]
        [Description("After contains an Id of a search after this search.")]
        [Priority(3)]
        public void ProductSearchRecursiveAfter()
        {
            string sourceDirectory = Path.Combine(ProductSearchTests.TestDataDirectory, @"ProductSearchRecursiveAfter\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceDirectory }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: ProductSearch1 -> ProductSearch2 -> ProductSearch1. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: ProductSearch2 -> ProductSearch1 -> ProductSearch2. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 5060;
            light.Run();
        }

        [NamedFact]
        [Description("Valid ProductSearch.")]
        [Priority(2)]
        public void ValidProductSearch()
        {
            string sourceFile = Path.Combine(ProductSearchTests.TestDataDirectory, @"ValidProductSearch\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile, new string[] { "WixUtilExtension" });

            // verify the ParameterInfo and burnManifest has the correct information 
            ProductSearchTests.VerifyProductSearchInformation(outputDirectory, "ProductSearch1", "{738D02BF-E231-4370-8209-E9FD4E1BE2A1}", "Variable1",@"1 & 2 < 3",null);
            ProductSearchTests.VerifyProductSearchInformation(outputDirectory, "ProductSearch2", "{738D02BF-E231-4370-8209-E9FD4E1BE2A2}", "Variable2", null, "language");
            ProductSearchTests.VerifyProductSearchInformation(outputDirectory, "ProductSearch3", "{738D02BF-E231-4370-8209-E9FD4E1BE2A3}", "Variable # 3", null,"state");
            ProductSearchTests.VerifyProductSearchInformation(outputDirectory, "ProductSearch4", "{738D02BF-E231-4370-8209-E9FD4E1BE2A4}", "Variable4", null, "assignment");
            ProductSearchTests.VerifyProductSearchInformation(outputDirectory, "ProductSearch5", "{738D02BF-E231-4370-8209-E9FD4E1BE2A5}", "Variable5", null, null);

            ProductSearchTests.VerifyProductSearchOrder(outputDirectory, "ProductSearch4", "ProductSearch5", "ProductSearch2");
            ProductSearchTests.VerifyProductSearchOrder(outputDirectory, "ProductSearch4", "ProductSearch3", "ProductSearch1");
        }

        #region Verification Methods

        /// <summary>
        /// Verifies ProductSearch information in Burn_Manifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="expectedId">Expected ProductSearch @Id; this attribute is used to search for the element.</param>
        /// <param name="expectedGuid">Expected ProductSearch @Guid value.</param>
        /// <param name="expectedVariableName">Expected ProductSearch @Variable value.</param>
        /// <param name="expectedCondition">Expected ProductSearch @Condition value.</param>
        /// <param name="expectedResult">Expected ProductSearch @Attribute value.</param>
        public static void VerifyProductSearchInformation(string embededResourcesDirectoryPath, string expectedId, string expectedGuid, string expectedVariableName, string expectedCondition, string expectedResult)
        {
            string burnManifestXPath = string.Format(@"//burn:MsiProductSearch[@Id='{0}']", expectedId);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.True(1 == burnManifestNodes.Count, String.Format("No ProductSearch with the Id: '{0}' was found in Burn_Manifest.xml.", expectedId));
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "ProductCode", expectedGuid);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Variable", expectedVariableName);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Type", expectedResult);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Condition", expectedCondition);
        }

        /// <summary>
        /// Verify ProductSearch elements appear in a specific order
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="productIds">Ids of the ProductSearch elements in order.</param>
        public static void VerifyProductSearchOrder(string embededResourcesDirectoryPath, params string[] productIds)
        {
            BundleTests.VerifyBurnManifestElementOrder(embededResourcesDirectoryPath, "MsiProductSearch", "Id", productIds);
        }

        #endregion
    }
}
