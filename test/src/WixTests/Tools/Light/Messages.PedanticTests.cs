//-----------------------------------------------------------------------
// <copyright file="Messages.PedanticTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for pedantic output
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Messages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for pedantic output
    /// </summary>
    public class PedanticTests : WixTests
    {
        [NamedFact]
        [Description("Verify that Light prints pedantic output")]
        [Priority(1)]
        public void SimplePedantic()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(WixTests.SharedAuthoringDirectory, "BasicProduct.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.Pedantic = true;
            light.Run();
        }
    }
}