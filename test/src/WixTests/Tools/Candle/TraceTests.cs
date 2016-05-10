// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.Trace
{
    using System;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles the Trace switch.
    /// </summary>
    public class TraceTests : WixTests
    {
        private static string testFile = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\TraceTests\Product.wxs");

        [NamedFact]
        [Description("Verify that Candle honors the trace switch when specified.")]
        [Priority(2)]
        public void TraceSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.Trace = true;
            //WixMessage verification will not work on multiple line outputs, we have to check standard output.
            string output = String.Format("warning CNDL1075 : The Product/@UpgradeCode attribute was not found; it is strongly recommended to ensure that this product can be upgraded.Source trace:{0}at {1}: line 6", Environment.NewLine, testFile);
            candle.ExpectedOutputStrings.Add(output);
            candle.ExpectedWixMessages.Add(new WixMessage(1075, WixMessage.MessageTypeEnum.Warning));
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle does not print trace messages when trace switch is not specified.")]
        [Priority(2)]
        public void NoTraceSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.Trace = false;
            candle.ExpectedWixMessages.Add(new WixMessage(1075, "The Product/@UpgradeCode attribute was not found; it is strongly recommended to ensure that this product can be upgraded.", WixMessage.MessageTypeEnum.Warning));
            candle.Run();
        }
    }
}
