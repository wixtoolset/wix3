// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.TreatWarningsAsErrors
{
    using System;
    using System.IO;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles the wx switch.
    /// </summary>
    public class TreatWarningsAsErrorsTests : WixTests
    {
        private static string testFile = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\TreatWarningsAsErrorsTests\Product.wxs");

        [NamedFact]
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

        [NamedFact]
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

        [NamedFact]
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

        [NamedFact(Skip = "Ignored because of a bug")]
        [Description("Verify that Candle handles the wxall switch and displays a message that the switch is deprecated.")]
        [Priority(2)]
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
