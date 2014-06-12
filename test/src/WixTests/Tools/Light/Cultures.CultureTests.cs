//-----------------------------------------------------------------------
// <copyright file="Cultures.CultureTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for cultures
// </summary>
//-----------------------------------------------------------------------

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