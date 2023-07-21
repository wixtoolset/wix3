// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// Tests for Bundle DirectorySearch element
    /// </summary>
    public class DirectorySearchTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\DirectorySearchTests");

        [NamedFact]
        [Description("DirectorySearch Variable is required.")]
        [Priority(3)]
        public void DirectorySearchVariableMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(DirectorySearchTests.TestDataDirectory, @"DirectorySearchVariableMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:DirectorySearch/@Variable attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("DirectorySearch Path is required.")]
        [Priority(3)]
        public void DirectorySearchPathMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(DirectorySearchTests.TestDataDirectory, @"DirectorySearchPathMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:DirectorySearch/@Path attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("DirectorySearch @Path contains an invalid  Path.")]
        [Priority(3)]
        [Trait("Bug Link", @"https://sourceforge.net/tracker/?func=detail&aid=2980327&group_id=105970&atid=642714")]
        public void DirectorySearchInvalidPath()
        {
            string sourceFile = Path.Combine(DirectorySearchTests.TestDataDirectory, @"DirectorySearchInvalidPath\Product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Extensions.Add("WixUtilExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The util:DirectorySearch/@Path attribute's value, '%windir%\\System|*32', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 346;
            candle.Run();
        }
       
        [NamedFact]
        [Description("DirectorySearch @Variable should not be a predefined variable.")]
        [Priority(3)]
        [Trait("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=2980329&group_id=105970")]
        public void DirectorySearchPredefinedVariable()
        {
            string expectedErrorMessage = @"The util:DirectorySearch/@Variable attribute's value, 'AdminToolsFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFilesFolder', 'CompatibilityMode', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'LocalAppDataFolder', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProgramFilesFolder', 'ProgramMenuFolder', 'SendToFolder', 'StartMenuFolder', 'StartupFolder', 'SystemFolder', 'TempFolder', 'TemplateFolder', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsFolder', or 'WindowsVolume'.";

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(DirectorySearchTests.TestDataDirectory, @"DirectorySearchPredefinedVariable\Product.wxs"));
            candle.OutputFile = "Setup.exe";
            candle.Extensions.Add("WixUtilExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(348, expectedErrorMessage, Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 348;
            candle.Run();
        }

        [NamedFact]
        [Description("DirectorySearch @Result contains invalid value (something other than Exists)")]
        [Priority(3)]
        public void DirectorySearchInvalidResultValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(DirectorySearchTests.TestDataDirectory, @"DirectorySearchInvalidResultValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The util:DirectorySearch/@Result attribute's value, 'NotExists', is not one of the legal options: 'Exists'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 21;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("Cannot have dupplicate DirectorySearch with the same id.")]
        [Priority(3)]
        public void DuplicateDirectorySearch()
        {
            string sourceFile = Path.Combine(DirectorySearchTests.TestDataDirectory, @"DuplicateDirectorySearch\Product.wxs");
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
        public void DirectorySearchAfterUndefinedSearch()
        {
            string sourceFile = Path.Combine(DirectorySearchTests.TestDataDirectory, @"DirectorySearchAfterUndefinedSearch\Product.wxs");
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
        public void DirectorySearchRecursiveAfter()
        {
            string sourceDirectory = Path.Combine(DirectorySearchTests.TestDataDirectory, @"DirectorySearchRecursiveAfter\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceDirectory }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: DirectorySearch1 -> DirectorySearch2 -> DirectorySearch1. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: DirectorySearch2 -> DirectorySearch1 -> DirectorySearch2. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 5060;
            light.Run();
        }

        [NamedFact]
        [Description("Valid DirectorySearch.")]
        [Priority(2)]
        public void ValidDirectorySearch()
        {
            string sourceFile = Path.Combine(DirectorySearchTests.TestDataDirectory, @"ValidDirectorySearch\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile, new string[] { "WixUtilExtension" });

            // verify the ParameterInfo and burnManifest has the correct information 
            DirectorySearchTests.VerifyDirectorySearchInformation(outputDirectory, "DirectorySearch1", @"%windir%\System", "variable1", "1 & 2 < 3", "exists");
            DirectorySearchTests.VerifyDirectorySearchInformation(outputDirectory, "DirectorySearch2", @"%windir%\System32", "variable2", null, "exists");
            DirectorySearchTests.VerifyDirectorySearchInformation(outputDirectory, "DirectorySearch3", @"%windir%\System32", "variable # 3", "true", "exists");

            DirectorySearchTests.VerifyDirectorySearchOrder(outputDirectory, "DirectorySearch1", "DirectorySearch3", "DirectorySearch2");
        }

        #region Verification Methods

        /// <summary>
        /// Verifies DirectorySearch information in Burn_Manifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="expectedId">Expected DirectorySearch @Id; this attribute is used to search for the element.</param>
        /// <param name="expectedPath">Expected DirectorySearch @Path value.</param>
        /// <param name="expectedVariableName">Expected DirectorySearch @Variable value.</param>
        /// <param name="expectedCondition">Expected DirectorySearch @Condition value.</param>
        /// <param name="expectedResult">Expected DirectorySearch @Attribute value.</param>
        public static void VerifyDirectorySearchInformation(string embededResourcesDirectoryPath, string expectedId, string expectedPath, string expectedVariableName, string expectedCondition, string expectedResult)
        {
            string burnManifestXPath = string.Format(@"//burn:DirectorySearch[@Id='{0}']", expectedId);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.True(1 == burnManifestNodes.Count, String.Format("No DirectorySearch with the Id: '{0}' was found in Burn_Manifest.xml.", expectedId));
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Path", expectedPath);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Variable", expectedVariableName);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Type", expectedResult);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Condition", expectedCondition);
        }

        /// <summary>
        /// Verify DirectorySearch elements appear in a specific order
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="directorySearchIds">Ids of the DirectorySearch elements in order.</param>
        public static void VerifyDirectorySearchOrder(string embededResourcesDirectoryPath, params string[] directorySearchIds)
        {
            BundleTests.VerifyBurnManifestElementOrder(embededResourcesDirectoryPath, "DirectorySearch", "Id", directorySearchIds);
        }

        #endregion
    }
}
