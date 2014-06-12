//-----------------------------------------------------------------------
// <copyright file="MultipleInputSwitchesTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Tests how Candle handles multiple input switches.</summary>
//-----------------------------------------------------------------------

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