//-----------------------------------------------------------------------
// <copyright file="Bundle.ComponentSearchTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Bundle ComponentSearch element
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
    /// Tests for Bundle ComponentSearch element
    /// </summary>
    public class ComponentSearchTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\ComponentSearchTests");

        [NamedFact]
        [Description("ComponentSearch @Variable is required.")]
        [Priority(3)]
        public void ComponentSearchVariableMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchVariableMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:ComponentSearch/@Variable attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("ComponentSearch @Guid is required.")]
        [Priority(3)]
        public void ComponentSearchGuidMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchGuidMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:ComponentSearch/@Guid attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("ComponentSearch @Guid is not a valid GUID.")]
        [Priority(3)]
        public void ComponentSearchInvalidGuid()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchInvalidGuid\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(9, "The util:ComponentSearch/@Guid attribute's value, 'Not_A_Product_Guid', is not a legal guid value.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 9;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("ComponentSearch @ProductCode is not a valid GUID.")]
        [Priority(3)]
        public void ComponentSearchInvalidProductCode()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchInvalidProductCode\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(9, "The util:ComponentSearch/@ProductCode attribute's value, 'Not_A_Product_Guid', is not a legal guid value.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 9;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("ComponentSearch @Variable should not be a predefined variable.")]
        [Priority(3)]
        [Trait("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=2980329&group_id=105970")]
        public void ComponentSearchPredefinedVariable()
        {
            string expectedErrorMessage = @"The util:ComponentSearch/@Variable attribute's value, 'ProgramFilesFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFilesFolder', 'CompatibilityMode', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'LocalAppDataFolder', 'MyPicturesFolder', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProgramFilesFolder', 'ProgramMenuFolder', 'SendToFolder', 'StartMenuFolder', 'StartupFolder', 'SystemFolder', 'TempFolder', 'TemplateFolder', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsFolder', or 'WindowsVolume'.";

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchPredefinedVariable\Product.wxs"));
            candle.OutputFile = "Setup.exe";
            candle.Extensions.Add("WixUtilExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(348, expectedErrorMessage, Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 348;
            candle.Run();
        }

        [NamedFact]
        [Description("ComponentSearch @Result contains invalid value (something other than State, Directory and KeyPath)")]
        [Priority(3)]
        public void ComponentSearchInvalidResultValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchInvalidResultValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The util:ComponentSearch/@Result attribute's value, 'NotState', is not one of the legal options: 'Directory', 'State', or 'KeyPath'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 21;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("Cannot have dupplicate ComponentSearch with the same id.")]
        [Priority(3)]
        public void DuplicateComponentSearch()
        {
            string sourceFile = Path.Combine(ComponentSearchTests.TestDataDirectory, @"DuplicateComponentSearch\Product.wxs");
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
        public void ComponentSearchAfterUndefinedSearch()
        {
            string sourceFile = Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchAfterUndefinedSearch\Product.wxs");
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
        public void ComponentSearchRecursiveAfter()
        {
            string sourceDirectory = Path.Combine(ComponentSearchTests.TestDataDirectory, @"ComponentSearchRecursiveAfter\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceDirectory }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: ComponentSearch1 -> ComponentSearch2 -> ComponentSearch1. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: ComponentSearch2 -> ComponentSearch1 -> ComponentSearch2. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 5060;
            light.Run();
        }

        [NamedFact]
        [Description("Valid ComponentSearch.")]
        [Priority(2)]
        public void ValidComponentSearch()
        {
            string sourceFile = Path.Combine(ComponentSearchTests.TestDataDirectory, @"ValidComponentSearch\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile, new string[] { "WixUtilExtension" });

            // verify the ParameterInfo and burnManifest has the correct information 
            ComponentSearchTests.VerifyComponentSearchInformation(outputDirectory, "ComponentSearch1", "{738D02BF-E231-4370-8209-E9FD4E1BE2A1}", null, "Variable1", @"1 & 2 < 3", "directory");
            ComponentSearchTests.VerifyComponentSearchInformation(outputDirectory, "ComponentSearch2", "{738D02BF-E231-4370-8209-E9FD4E1BE2A2}", null, "Variable2", null, "state");
            ComponentSearchTests.VerifyComponentSearchInformation(outputDirectory, "ComponentSearch3", "{738D02BF-E231-4370-8209-E9FD4E1BE2A3}", "{738D02BF-E231-4370-8209-E9FD4E1BE2A5}", "Variable # 3", null, null);
            ComponentSearchTests.VerifyComponentSearchInformation(outputDirectory, "ComponentSearch4", "{738D02BF-E231-4370-8209-E9FD4E1BE2A4}", null , "Variable4", null, null);

            ComponentSearchTests.VerifyComponentSearchOrder(outputDirectory, "ComponentSearch4", "ComponentSearch2");
            ComponentSearchTests.VerifyComponentSearchOrder(outputDirectory, "ComponentSearch4", "ComponentSearch3", "ComponentSearch1");
        }

        #region Verification Methods

        /// <summary>
        /// Verifies ComponentSearch information in Burn_Manifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="expectedId">Expected ComponentSearch @Id; this attribute is used to search for the element.</param>
        /// <param name="expectedProdcutCode">Expected ComponentSearch @ProductCode value.</param>
        /// <param name="expectedGuid">Expected ComponentSearch @Guid value.</param>
        /// <param name="expectedVariableName">Expected ComponentSearch @Variable value.</param>
        /// <param name="expectedCondition">Expected ComponentSearch @Condition value.</param>
        /// <param name="expectedResult">Expected ComponentSearch @Attribute value.</param>
        public static void VerifyComponentSearchInformation(string embededResourcesDirectoryPath, string expectedId, string expectedGuid, string expectedProdcutCode, string expectedVariableName, string expectedCondition, string expectedResult)
        {
            string burnManifestXPath = string.Format(@"//burn:MsiComponentSearch[@Id='{0}']", expectedId);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.True(1 == burnManifestNodes.Count, String.Format("No ComponentSearch with the Id: '{0}' was found in Burn_Manifest.xml.", expectedId));
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "ComponentId", expectedGuid);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "ProductCode", expectedProdcutCode);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Variable", expectedVariableName);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Type", expectedResult);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Condition", expectedCondition);
        }

        /// <summary>
        /// Verify ComponentSearch elements appear in a specific order
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="productIds">Ids of the ComponentSearch elements in order.</param>
        public static void VerifyComponentSearchOrder(string embededResourcesDirectoryPath, params string[] productIds)
        {
            BundleTests.VerifyBurnManifestElementOrder(embededResourcesDirectoryPath, "MsiComponentSearch", "Id", productIds);
        }

        #endregion
    }
}
