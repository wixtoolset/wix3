//-----------------------------------------------------------------------
// <copyright file="Localization.LocalizationTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Test MSI localization with Light
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Test MSI localization with Light
    /// </summary>
    [TestClass]
    public class LocalizationTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Localization\LocalizationTests");

        [TestMethod]
        [Description("Verify that an MSI can be localized")]
        [Priority(2)]
        public void Hebrew()
        {
            string wixobj = Candle.Compile(Path.Combine(LocalizationTests.TestDataDirectory, @"Shared\product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(wixobj);
            light.LocFiles.Add(Path.Combine(LocalizationTests.TestDataDirectory, @"he-il\he-il.wxl"));
            light.OutputFile = Path.Combine(Path.Combine(this.TestContext.TestDir, Path.GetRandomFileName()), "he-il.msi");
            light.Run();

            Verifier.CompareResults(Path.Combine(LocalizationTests.TestDataDirectory, @"he-il\he-il.msi"), light.OutputFile);
        }

        [TestMethod]
        [Description("Verify that variable Ids containing dots are accepted.")]
        [Priority(2)]
        [TestProperty("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1711440&group_id=105970&atid=642714")]
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