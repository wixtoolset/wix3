//-----------------------------------------------------------------------
// <copyright file="UI.HyperlinkTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the Hyperlinks in UI
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.UI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using DTF = Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Tests for the Hyperlinks in UI
    /// </summary>
    /// <remarks>
    /// Hyperlinks is new in Windows Installer 5.0
    /// </remarks>
    public class HyperlinkTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\UI\HyperlinkTests");

        [NamedFact]
        [Description("Verify that a hyperlink can be created")]
        [Priority(2)]
        public void SimpleHyperlink()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(HyperlinkTests.TestDataDirectory, @"SimpleHyperlink\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // Only run validation if the current version of Windows Installer is 5.0 or above
            if (DTF.Installer.Version < MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
            {
                light.SuppressMSIAndMSMValidation = true;
            }

            light.Run();

            Verifier.VerifyQuery(light.OutputFile, "SELECT `Type` from `Control` WHERE `Control`='Control1'", "Hyperlink");
        }

        [NamedFact]
        [Description("Verify that multiple hyperlinks can be created")]
        [Priority(2)]
        public void MultipleHyperlinks()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(HyperlinkTests.TestDataDirectory, @"MultipleHyperlinks\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // Only run validation if the current version of Windows Installer is 5.0 or above
            if (DTF.Installer.Version < MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
            {
                light.SuppressMSIAndMSMValidation = true;
            }

            light.Run();

            Verifier.VerifyQuery(light.OutputFile, "SELECT `Type` from `Control` WHERE `Control`='Control1'", "Hyperlink");
        }
    }
}
