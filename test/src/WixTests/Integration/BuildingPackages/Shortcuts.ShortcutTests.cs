// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Shortcuts
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for shortcuts
    /// </summary>
    public class ShortcutTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Shortcuts\ShortcutTests");

        [NamedFact]
        [Description("Verify that a simple shortcut can be created")]
        [Priority(1)]
        public void SimpleShortcut()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ShortcutTests.TestDataDirectory, @"SimpleShortcut\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.SuppressedICEs.Add("ICE66");
            light.Run();

            Verifier.VerifyResults(Path.Combine(ShortcutTests.TestDataDirectory, @"SimpleShortcut\expected.msi"), light.OutputFile, "Shortcut");
        }
    }
}
