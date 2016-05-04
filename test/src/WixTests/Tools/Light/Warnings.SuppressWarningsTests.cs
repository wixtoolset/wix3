// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Light.Warnings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for suppressing warnings
    /// </summary>
    public class SuppressWarningsTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Warnings");

        [NamedFact]
        [Description("Verify that specific warnings can be suppressed")]
        [Priority(1)]
        public void SimpleSuppressWarnings()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Environment.ExpandEnvironmentVariables(Path.Combine(SuppressWarningsTests.TestDataDirectory, @"Shared\Warning1079.wxs")));
            candle.Run();

            Light light = new Light(candle);
            light.SuppressWarnings.Add("1079");
            light.Run();
        }
    }
}
