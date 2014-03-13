//-----------------------------------------------------------------------
// <copyright file="Bundle.VariableTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Bundle Variable element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for Bundle Variable element
    /// </summary>
    [TestClass]
    public class VariableTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\VariableTests");

        [TestMethod]
        [Description("Variable Name is required.")]
        [Priority(3)]
        public void VariableNameMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"VariableNameMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Variable/@Name attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [TestMethod]
        [Description("Variable cannot be redefined.")]
        [Priority(3)]
        public void DuplicateVariableName()
        {
            string candleOutput = Candle.Compile(Path.Combine(VariableTests.TestDataDirectory, @"DuplicateVariableName\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, "Duplicate symbol 'Variable:Variable1' found.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(92, "Location of symbol related to previous error.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [TestMethod]
        [Description("Variable cannot be redefined.")]
        [Priority(3)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2980330&group_id=105970&atid=642714")]
        public void BuiltInVariableName()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"BuiltInVariableName\Product.wxs"));
            candle.OutputFile = "Setup.exe";
            candle.ExpectedWixMessages.Add(new WixMessage(348, "The Variable/@Name attribute's value, 'AdminToolsFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFilesFolder', 'CompatibilityMode', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'LocalAppDataFolder', 'MyPicturesFolder', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProgramFilesFolder', 'ProgramMenuFolder', 'SendToFolder', 'StartMenuFolder', 'StartupFolder', 'SystemFolder', 'TempFolder', 'TemplateFolder', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsFolder', or 'WindowsVolume'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(348, "The Variable/@Name attribute's value, 'FontsFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFilesFolder', 'CompatibilityMode', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'LocalAppDataFolder', 'MyPicturesFolder', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProgramFilesFolder', 'ProgramMenuFolder', 'SendToFolder', 'StartMenuFolder', 'StartupFolder', 'SystemFolder', 'TempFolder', 'TemplateFolder', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsFolder', or 'WindowsVolume'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 348;
            candle.Run();
        }

        [TestMethod]
        [Description("Variable Value is required.")]
        [Priority(3)]
        public void VariableValueMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"VariableValueMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Variable/@Value attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [TestMethod]
        [Description("Variable Value cannot be empty.")]
        [Priority(3)]
        public void VariableValueEmpty()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"VariableValueEmpty\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(6, "The Variable/@Value attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 6;
            candle.Run();
        }

        [TestMethod]
        [Description("Variable Name containing spaces, special characters, numbers.")]
        [Priority(2)]
        public void ValidVariableName()
        {
            string sourceFile = Path.Combine(VariableTests.TestDataDirectory, @"ValidVariableName\Product.wxs");
            string testFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Bootstrapper.exe");
            string outputDirectory = this.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            VariableTests.VerifyVariableInformation(outputDirectory, @"Variable Name has  spaces", @"Value1", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VARIABLENAMEISALLCAPS", @"Value1", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableNamehas_.(){}[]$@#!~.:,;?+=-*|", @"Value2", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"1234567890", @"Value3", VariableType.String);
        }

        [TestMethod]
        [Description("Variable value containing spaces, special characters, numbers, expressions, .")]
        [Priority(2)]
        // bug# https://sourceforge.net/tracker/?func=detail&aid=2980315&group_id=105970&atid=642714
        public void ValidVariableValue()
        {
            string sourceFile = Path.Combine(VariableTests.TestDataDirectory, @"ValidVariableValue\Product.wxs");
            string testFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Bootstrapper.exe");
            string outputDirectory = this.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 

            // Valid string values
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsString", "stringValue", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueHasSpecialCharacters", @"_.(){}[]$@#!~.:,;?+=-*|", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsFilePath", @"%windir%\System32\initsrv\iis.dll", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsHash", "7D7E1F2D7BE56300B51FFD1CDEB41FBFBEC6E7AB", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsAVariableName", "VariableValueIsAVariableName", VariableType.String);

            // Valid version values
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsVersion", "V10.3.0216.00", VariableType.Version);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsAnotherVersion", "V1", VariableType.Version);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsYetAnotherVersion", "V0.0", VariableType.Version);

            // Valid numeric values
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsInteger", "2677598087974754", VariableType.Numeric);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsAnotherInteger", "0", VariableType.Numeric);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsANegativeInteger", "-10", VariableType.Numeric);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsALongMaxValue", "9223372036854775807", VariableType.Numeric); //Long.MaxValue
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsALongMinValue", "-9223372036854775808", VariableType.Numeric); //Long.MinValue


            // More String Values
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsNotAVersion", "V10.foo", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueAgainIsNotAVersion", "V1bar", VariableType.String);
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsStillNotAVersion", "1.0.0.0", VariableType.String);

            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsNotAnInteger", "0.0", VariableType.String);

            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsLongMaxValuePlus1", "9223372036854775808", VariableType.String); // Long.MaxValue + 1
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsLongMinValueMinus1", "-9223372036854775809", VariableType.String); // Long.MinValue - 1
            VariableTests.VerifyVariableInformation(outputDirectory, @"VariableValueIsTooLongForAnInteger", "012345678901234567890123456789012345678901234567890123456789", VariableType.String);
        }
     
        #region Verification Methods

        /// <summary>
        /// Legal variable types for Burn variables
        /// </summary>
        public enum VariableType
        {
            /// <summary>
            /// Number
            /// </summary>
            Numeric = 0,

            /// <summary>
            /// String literal
            /// </summary>
            String,

            /// <summary>
            /// Version
            /// </summary>
            Version,
        };

        /// <summary>
        /// Get the string value for a corresponding VariableType value.
        /// </summary>
        /// <param name="value">The variable type to convert</param>
        /// <returns>String representation</returns>
        public static string GetVariableTypeName(VariableType value)
        {
            switch (value)
            {
                case VariableType.Numeric:
                    return "numeric";
                case VariableType.String:
                    return "string";
                case VariableType.Version:
                    return "version";
                default:
                    throw new ArgumentOutOfRangeException("value", string.Format("Variable Type: '{0}' is not defined.", value.ToString()));
            };
        }

        /// <summary>
        /// Verifies Variable information in Burn_Manifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="variableName">Expected name of the variable.</param>
        /// <param name="variableValue">Expected value of the variable.</param>
        /// <param name="variableType">Expected Type of the variable.</param>
        public static void VerifyVariableInformation(string embededResourcesDirectoryPath, string variableName, string variableValue, VariableType variableType)
        {
            string expectedVariableTypeName = GetVariableTypeName(variableType);

            string burnManifestXPath = string.Format(@"//burn:Variable[@Id='{0}']", variableName);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.AreEqual(1, burnManifestNodes.Count, "No Variable with the name: '{0}' was found in Burn_Manifest.xml.", variableName);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Value", variableValue);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Type", expectedVariableTypeName);
        }


        #endregion
    }
}
