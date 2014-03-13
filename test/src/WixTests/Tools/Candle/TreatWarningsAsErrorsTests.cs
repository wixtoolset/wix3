//-----------------------------------------------------------------------
// <copyright file="TreatWarningsAsErrorsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Tests how Candle handles the wx switch.</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Candle.TreatWarningsAsErrors
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles the wx switch.
    /// </summary>
    [TestClass]
    public class TreatWarningsAsErrorsTests : WixTests
    {
        private static string testFile = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\TreatWarningsAsErrorsTests\Product.wxs");

        [TestMethod]
        [Description("Verify that Candle honors the wx switch when specified.")]
        [Priority(2)]
        public void TreatAllWarningsAsErrorSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.TreatAllWarningsAsErrors = true;
            candle.ExpectedWixMessages.Add(new WixMessage(1096, "Preprocessor Warning", WixMessage.MessageTypeEnum.Error));

            // The authoring causes another warning 1075 but Candle should exit before this warning is encountered
            candle.ExpectedExitCode = 1096;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that Candle honors the wx[N] switch when specified.")]
        [Priority(2)]
        public void TreatSpecificWarningsAsErrorSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.TreatWarningsAsErrors.Add(1075);
            candle.ExpectedWixMessages.Add(new WixMessage(1096, "Preprocessor Warning", WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedWixMessages.Add(new WixMessage(1075, "The Product/@UpgradeCode attribute was not found; it is strongly recommended to ensure that this product can be upgraded.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 1075;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that Candle does not treat warnings as errors when wx switch is not specified.")]
        [Priority(2)]
        public void NoTreatWarningsAsErrorSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.ExpectedWixMessages.Add(new WixMessage(1096, WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedWixMessages.Add(new WixMessage(1075, WixMessage.MessageTypeEnum.Warning));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that Candle handles the wxall switch and displays a message that the switch is deprecated.")]
        [Priority(2)]
        [Ignore] // Bug
        public void VerifyDeprecatedSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.OtherArguments = "-wxall";
            candle.ExpectedWixMessages.Add(new WixMessage(1108, "The command line switch 'wxall' is deprecated. Please use 'wx' instead.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 1108;
            candle.Run();
        }
    }
}