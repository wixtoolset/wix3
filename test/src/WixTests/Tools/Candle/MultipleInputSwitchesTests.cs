// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.MultipleInputSwitches
{
    using System;
    using WixTest;
    
    /// <summary>
    /// Tests how Candle handles multiple input switches.
    /// </summary>
    public class MultipleInputSwitchesTests : WixTests
    {
        [NamedFact]
        [Description("Verify that Candle handles the case when multiple switches like 'Suppress All Warnings' and 'Treat Warnings as Errors' are given. In this scenario, Candle honors the sw switch and suppresses all warnings")]
        [Priority(3)]
        public void WxAndSw()
        {
            string testFile = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\MultipleInputSwitchesTests\WxAndSw\Product.wxs");
            Candle candle = new Candle();
            candle.TreatAllWarningsAsErrors = true;
            candle.SuppressAllWarnings = true;
            candle.SourceFiles.Add(testFile);
            candle.Run();
        }
    }
}
