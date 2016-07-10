// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Light.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Test MSI localization with Light
    /// </summary>
    public class LocalizationTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Localization\LocalizationTests");

        [NamedFact]
        [Description("Verify that an MSI can be localized")]
        [Priority(2)]
        public void Hebrew()
        {
            string wixobj = Candle.Compile(Path.Combine(LocalizationTests.TestDataDirectory, @"Shared\product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(wixobj);
            light.LocFiles.Add(Path.Combine(LocalizationTests.TestDataDirectory, @"he-il\he-il.wxl"));
            light.OutputFile = Path.Combine(Path.Combine(this.TestContext.TestDirectory, Path.GetRandomFileName()), "he-il.msi");
            light.Run();

            Verifier.CompareResults(Path.Combine(LocalizationTests.TestDataDirectory, @"he-il\he-il.msi"), light.OutputFile);
        }

        [NamedFact]
        [Description("Verify that variable Ids containing dots are accepted.")]
        [Priority(2)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1711440&group_id=105970&atid=642714")]
        public void ValidIdentifier()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(LocalizationTests.TestDataDirectory, @"ValidIdentifier\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.LocFiles.Add(Path.Combine(LocalizationTests.TestDataDirectory, @"ValidIdentifier\en-us.wxl"));
            light.Cultures = "en-us";
            light.Run();
        }
    }
}
