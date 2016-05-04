// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Lit.Localization
{
    using System;
    using System.IO;
    using WixTest;
    
     /// <summary>
    /// Test the different ways for giving input files to Candle.
    /// </summary>
    public class LocalizationTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Lit\Localization\LocalizationTests");

        [NamedFact]
        [Description("Verify that Lit accepts a single Windows Installer XML include (wxs) file as input")]
        [Priority(1)]
        public void SingleWxlFile()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.LocFiles.Add(Path.Combine(LocalizationTests.TestDataDirectory, @"Shared\en-US.wxl"));
            lit.Run();

            Verifier.VerifyWixLibLocString(lit.OutputFile,"en-us","String1","String1(en-us)");
        }

        [NamedFact]
        [Description("Verify that Lit accepts multiple Windows Installer XML object (wxs) files as input")]
        [Priority(2)]
        public void MultipleWxlFiles()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.LocFiles.Add(Path.Combine(LocalizationTests.TestDataDirectory, @"Shared\en-US.wxl"));
            lit.LocFiles.Add(Path.Combine(LocalizationTests.TestDataDirectory, @"Shared\ja-JP.wxl"));
            lit.Run();

            Verifier.VerifyWixLibLocString(lit.OutputFile, "en-us", "String1", "String1(en-us)");
            Verifier.VerifyWixLibLocString(lit.OutputFile, "ja-jp", "String1", "String1(ja-jp)");
        }
   }
}
