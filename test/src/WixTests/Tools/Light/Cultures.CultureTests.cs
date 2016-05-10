// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Light.Cultures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for cultures
    /// </summary>
    public class CulturesTests : WixTests
    {
        [NamedFact]
        [Description("Verify that passing an invalid culture to light does not cause an error.")]
        [Priority(2)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1942991&group_id=105970&atid=642714")]
        public void InvalidCultures()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.Run();

            Light light = new Light(candle);
            light.Cultures = "en-US;in-VA;lid";
            light.Run();
        }
    }
}
