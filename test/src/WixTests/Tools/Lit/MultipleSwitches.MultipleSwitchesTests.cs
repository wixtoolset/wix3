// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Lit.MultipleInputSwitches
{
    using System;
    using WixTest;
    
    /// <summary>
    /// Tests how Lit handles multiple input switches.
    /// </summary>
    public class MultipleSwitchesTests : WixTests
    {
        [NamedFact(Skip="Ignore")]
        [Description("Verify that Lit handles the case when multiple switches like 'Suppress All Warnings' and 'Treat Warnings as Errors' are given. In this scenario, Lit honors the sw switch and suppresses all warnings")]
        [Priority(3)]
        public void WxAndSw()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.OtherArguments = " -abc";
            lit.TreatAllWarningsAsErrors = true;
            lit.SuppressAllWarnings = true;
            lit.Run();
        }
    }
}
