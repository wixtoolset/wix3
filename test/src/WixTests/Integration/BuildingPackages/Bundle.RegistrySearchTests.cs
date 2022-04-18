// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// Tests for Bundle RegistrySearch element
    /// </summary>
    public class RegistrySearchTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\RegistrySearchTests");

        [NamedFact]
        [Description("RegistrySearch @Variable is required.")]
        [Priority(3)]
        public void RegistrySearchVariableMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchVariableMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:RegistrySearch/@Variable attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("RegistrySearch @Root is required.")]
        [Priority(3)]
        public void RegistrySearchRootMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchRootMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:RegistrySearch/@Root attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("RegistrySearch @Key is required.")]
        [Priority(3)]
        public void RegistrySearchKeyMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchKeyMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:RegistrySearch/@Key attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("RegistrySearch @Variable should not be a predefined variable.")]
        [Priority(3)]
        [Trait("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=2980329&group_id=105970")]
        public void RegistrySearchPredefinedVariable()
        {
            string expectedErrorMessage = @"The util:RegistrySearch/@Variable attribute's value, 'AdminToolsFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFilesFolder', 'CompatibilityMode', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'LocalAppDataFolder', 'MyPicturesFolder', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProgramFilesFolder', 'ProgramMenuFolder', 'SendToFolder', 'StartMenuFolder', 'StartupFolder', 'SystemFolder', 'TempFolder', 'TemplateFolder', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsFolder', or 'WindowsVolume'.";

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchPredefinedVariable\Product.wxs"));
            candle.OutputFile = "Setup.exe";
            candle.Extensions.Add("WixUtilExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(348, expectedErrorMessage, Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 348;
            candle.Run();
        }

        [NamedFact]
        [Description("RegistrySearch @Result contains invalid value (something other than Exists)")]
        [Priority(3)]
        public void RegistrySearchInvalidResultValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchInvalidResultValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The util:RegistrySearch/@Result attribute's value, 'NotExists', is not one of the legal options: 'Exists'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 21;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("RegistrySearch @ExpandEnvironmentVariables contains invalid value (something other than yes and no)")]
        [Priority(3)]
        public void RegistrySearchInvalidExpandEnvironmentVariablesValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchInvalidExpandEnvironmentVariablesValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The util:RegistrySearch/@ExpandEnvironmentVariables attribute's value, 'true', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 15;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("RegistrySearch @Format contains invalid value (something other than Row and Compatible)")]
        [Priority(3)]
        public void RegistrySearchInvalidFormatValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchInvalidFormatValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The util:RegistrySearch/@Format attribute's value, 'Column', is not one of the legal options: 'Raw', or 'Compatible'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 21;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("RegistrySearch @Root contains invalid value (something other than the predifined roots)")]
        [Priority(3)]
        public void RegistrySearchInvalidRootValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchInvalidRootValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The util:RegistrySearch/@Root attribute's value, 'HKEY_LOCAL_MACHINE', is not one of the legal options: 'HKCR', 'HKCU', 'HKLM', or 'HKU'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:RegistrySearch/@Root attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("Cannot have dupplicate RegistrySearch with the same id.")]
        [Priority(3)]
        public void DuplicateRegistrySearch()
        {
            string sourceFile = Path.Combine(RegistrySearchTests.TestDataDirectory, @"DuplicateRegistrySearch\Product.wxs");
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
        public void RegistrySearchAfterUndefinedSearch()
        {
            string sourceFile = Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchAfterUndefinedSearch\Product.wxs");
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
        public void RegistrySearchRecursiveAfter()
        {
            string sourceDirectory = Path.Combine(RegistrySearchTests.TestDataDirectory, @"RegistrySearchRecursiveAfter\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceDirectory }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: RegistrySearch1 -> RegistrySearch2 -> RegistrySearch1. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: RegistrySearch2 -> RegistrySearch1 -> RegistrySearch2. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 5060;
            light.Run();
        }

        [NamedFact]
        [Description("Valid RegistrySearch.")]
        [Priority(2)]
        public void ValidRegistrySearch()
        {
            string sourceFile = Path.Combine(RegistrySearchTests.TestDataDirectory, @"ValidRegistrySearch\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile, new string[] { "WixUtilExtension" });

            // verify the ParameterInfo and burnManifest has the correct information 
            RegistrySearchTests.VerifyRegistrySearchInformation(outputDirectory, "RegistrySearch1","HKLM", @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client", "Variable1",null,"InstallPath", "exists", null, null);
            RegistrySearchTests.VerifyRegistrySearchInformation(outputDirectory, "RegistrySearch2", "HKLM", @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client", "Variable2", @"1 & 2 < 3", null, "value", null, null);
            RegistrySearchTests.VerifyRegistrySearchInformation(outputDirectory, "RegistrySearch3", "HKU", @"Software\Microsoft\Microsoft SDKs\Windows\v6.1\WinSDKInterop", "Variable # 3", null, null, "value", "yes", null);
            RegistrySearchTests.VerifyRegistrySearchInformation(outputDirectory, "RegistrySearch4", "HKCR", @"TypeLib\{859D8CF5-7ADE-4DAB-8F7D-AF171643B934}\1.0\0", "Variable4", null, null, "value", null, "Compatible");

            RegistrySearchTests.VerifyRegistrySearchOrder(outputDirectory, "RegistrySearch4", "RegistrySearch3", "RegistrySearch1");
            RegistrySearchTests.VerifyRegistrySearchOrder(outputDirectory, "RegistrySearch4", "RegistrySearch2");
        }

        #region Verification Methods

        /// <summary>
        /// Verifies RegistrySearch information in Burn_Manifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="expectedId">Expected RegistrySearch @Id; this attribute is used to search for the element.</param>
        /// <param name="expectedRoot">Expected RegistrySearch @Root value.</param>
        /// <param name="expectedKey">Expected RegistrySearch @Key value.</param>
        /// <param name="expectedVariableName">Expected RegistrySearch @Variable value.</param>
        /// <param name="expectedCondition">Expected RegistrySearch @Condition value.</param>
        /// <param name="expectedValue">Expected RegistrySearch @Value value.</param>
        /// <param name="expectedResult">Expected RegistrySearch @Attribute value.</param>
        /// <param name="expectedExpandEnvironmentVariables">Expected RegistrySearch @ExpandEnvironmentVariables value.</param>
        /// <param name="expectedFormat">Expected RegistrySearch @Format value.</param>
        /// TODO: Check for @Format value
        public static void VerifyRegistrySearchInformation( string embededResourcesDirectoryPath, string expectedId, string expectedRoot, string expectedKey, 
                                                            string expectedVariableName, string expectedCondition, string expectedValue, string expectedResult, 
                                                            string expectedExpandEnvironmentVariables, string expectedFormat)
        {
            // verify the Burn_Manifest has the correct information 
            string burnManifestXPath = string.Format(@"//burn:RegistrySearch[@Id='{0}']", expectedId);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.True(1 == burnManifestNodes.Count, String.Format("No RegistrySearch with the Id: '{0}' was found in Burn_Manifest.xml.", expectedId));
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Root", expectedRoot);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Key", expectedKey);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Variable", expectedVariableName);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Type", expectedResult);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Condition", expectedCondition);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Value", expectedValue);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "ExpandEnvironment", expectedExpandEnvironmentVariables);
            //BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Format", expectedFormat);
        }

        /// <summary>
        /// Verify RegistrySearch elements appear in a specific order
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="searchIds">Ids of the RegistrySearch elements in order.</param>
        public static void VerifyRegistrySearchOrder(string embededResourcesDirectoryPath, params string[] searchIds)
        {
            BundleTests.VerifyBurnManifestElementOrder(embededResourcesDirectoryPath, "RegistrySearch", "Id", searchIds);
        }

        #endregion
    }
}
