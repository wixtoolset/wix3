//-----------------------------------------------------------------------
// <copyright file="Shortcuts.ShortcutTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for shortcuts
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Shortcuts
{
    using System;
    using System.IO;
    using System.Text;

    using WixTest;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for shortcuts
    /// </summary>
    [TestClass]
    public class ShortcutTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Shortcuts\ShortcutTests");

        [TestMethod]
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
