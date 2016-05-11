// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Light.BindPaths
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Test for setting bind paths
    /// </summary>
    public class BindPathTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\BindPaths\BindPathTests");

        [NamedFact]
        [Description("Verify that Light can use a bind path")]
        [Priority(1)]
        public void SimpleBindPath()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(BindPathTests.TestDataDirectory, @"SimpleBindPath\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.BindPath = WixTests.SharedFilesDirectory;
            light.Run();

            Verifier.VerifyResults(Path.Combine(BindPathTests.TestDataDirectory, @"SimpleBindPath\expected.msi"), light.OutputFile);
        }

        [NamedFact]
        [Description("Verify that Light can use named bind paths")]
        [Priority(1)]
        public void NamedBindPath()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(BindPathTests.TestDataDirectory, @"NamedBindPath\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.BindPath = String.Concat("Test=", WixTests.SharedFilesDirectory);
            light.Run();

            Verifier.VerifyResults(Path.Combine(BindPathTests.TestDataDirectory, @"NamedBindPath\expected.msi"), light.OutputFile);
        }
    }
}
