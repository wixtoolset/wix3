//-----------------------------------------------------------------------
// <copyright file="SuppressSchemaValidationTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Test how Candle handles the SS switch.</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Candle.SuppressSchemaValidation
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles the SS switch.
    /// </summary>
    [TestClass]
    public class SuppressSchemaValidationTests : WixTests
    {
        private static string testFile = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\SuppressSchemaValidationTests\Product.wxs");

        [TestMethod]
        [Description("Verify that Candle can handle the SS switch and not validate the schema.")]
        [Priority(2)]
        public void SuppressSchemaValidation()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);

            // The authoring does not contain a package element which is normally a schema validation error
            candle.SuppressSchemaValidation = true;
            candle.Run();
       }

        [TestMethod]
        [Description("Verify that Candle does schema validation when the SS switch is not specified.")]
        [Priority(2)]
        public void DoNotSuppressSchemaValidation()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.SuppressSchemaValidation = false;
            candle.ExpectedWixMessages.Add(new WixMessage(107, "Schema validation failed with the following error at line 1, column 542: The element 'Product' in namespace 'http://schemas.microsoft.com/wix/2006/wi' has invalid child element 'Media' in namespace 'http://schemas.microsoft.com/wix/2006/wi'. List of possible elements expected: 'Package'.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 107;
            candle.Run();
        }
    }
}