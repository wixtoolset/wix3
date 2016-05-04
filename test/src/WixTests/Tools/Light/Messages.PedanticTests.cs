// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
