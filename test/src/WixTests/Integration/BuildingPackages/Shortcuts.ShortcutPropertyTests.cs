// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Shortcuts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using DTF = Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Tests for shortcut property
    /// </summary>
    /// <remarks>
    /// ShortcutProperty is new in Windows Installer 5.0
    /// </remarks>
    public class ShortcutPropertyTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Shortcuts\ShortcutPropertyTests");

        [NamedFact]
        [Description("Verify that a shortcut property can be set")]
        [Priority(2)]
        public void SimpleShortcutProperty()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ShortcutPropertyTests.TestDataDirectory, @"SimpleShortcutProperty\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // Only run validation if the current version of Windows Installer is 5.0 or above
            if (DTF.Installer.Version < MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
            {
                light.SuppressMSIAndMSMValidation = true;
            }
            
            light.Run();

            Verifier.VerifyResults(Path.Combine(ShortcutPropertyTests.TestDataDirectory, @"SimpleShortcutProperty\expected.msi"), light.OutputFile, "MsiShortcutProperty");
        }

        [NamedFact]
        [Description("Verify that ShortcutProperty doesn't cause ICEs to fail on MSI versions less than 5.0")]
        [Priority(3)]
        public void PreMsi50ShortcutProperty()
        {
            // Loop through a list of pre-MSI 5.0 Windows Installer version
            foreach (MSIVersions.Versions version in Enum.GetValues(typeof(MSIVersions.Versions)))
            {
                // Skip MSI 5.0 and later
                if (MSIVersions.GetVersion(version) >= MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
                {
                    continue;
                }

                Candle candle = new Candle();
                candle.SourceFiles.Add(Path.Combine(ShortcutPropertyTests.TestDataDirectory, @"PreMsi50ShortcutProperty\product.wxs"));
                candle.PreProcessorParams.Add("InstallerVersion", MSIVersions.GetVersionInMSIFormat(version));
                candle.Run();

                Light light = new Light(candle);

                // Only run validation on MSIs built with the current or earlier than current versions of Windows Installer
                if (DTF.Installer.Version < MSIVersions.GetVersion(version))
                {
                    light.SuppressMSIAndMSMValidation = true;
                }

                light.Run();
            }
        }

        [NamedFact]
        [Description("Verify that there is an error for duplicate ShortcutProperty Ids")]
        [Priority(3)]
        public void DuplicateShortcutPropertyIds()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ShortcutPropertyTests.TestDataDirectory, @"DuplicateShortcutPropertyIds\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedExitCode = 130;
            light.ExpectedWixMessages.Add(new WixMessage(130, "The primary key 'ShortcutProperty1' is duplicated in table 'MsiShortcutProperty'.  Please remove one of the entries or rename a part of the primary key to avoid the collision.", WixMessage.MessageTypeEnum.Error));
            light.Run();
        }
    }
}
