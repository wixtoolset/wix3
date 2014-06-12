//-----------------------------------------------------------------------
// <copyright file="Permissions.PermissionExTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for PermissionEx (setting ACLs on File, Registry, CreateFolder
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Permissions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using DTF = Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Tests for PermissionEx (setting ACLs on File, Registry, CreateFolder
    /// </summary>
    /// <remarks>
    /// PermissionEx is new in Windows Installer 5.0
    /// </remarks>
    public class PermissionExTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Permissions\PermissionExTests");

        [NamedFact]
        [Description("Verify PermissionEx can be used on Files")]
        [Priority(2)]
        public void FilePermissionEx()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PermissionExTests.TestDataDirectory, @"FilePermissionEx\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // Only run validation if the current version of Windows Installer is 5.0 or above
            if (DTF.Installer.Version < MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
            {
                light.SuppressMSIAndMSMValidation = true;
            }

            light.Run();

            Verifier.VerifyResults(Path.Combine(PermissionExTests.TestDataDirectory, @"FilePermissionEx\expected.msi"), light.OutputFile, "MsiLockPermissionsEx");
        }

        [NamedFact]
        [Description("Verify PermissionEx can be used on Registry")]
        [Priority(2)]
        public void RegistryPermissionEx()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PermissionExTests.TestDataDirectory, @"RegistryPermissionEx\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // Only run validation if the current version of Windows Installer is 5.0 or above
            if (DTF.Installer.Version < MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
            {
                light.SuppressMSIAndMSMValidation = true;
            }

            light.Run();

            Verifier.VerifyResults(Path.Combine(PermissionExTests.TestDataDirectory, @"RegistryPermissionEx\expected.msi"), light.OutputFile, "MsiLockPermissionsEx");
        }

        [NamedFact]
        [Description("Verify PermissionEx can be used on CreateFolder")]
        [Priority(2)]
        public void CreateFolderPermissionEx()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PermissionExTests.TestDataDirectory, @"CreateFolderPermissionEx\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // Only run validation if the current version of Windows Installer is 5.0 or above
            if (DTF.Installer.Version < MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
            {
                light.SuppressMSIAndMSMValidation = true;
            }

            light.Run();

            Verifier.VerifyResults(Path.Combine(PermissionExTests.TestDataDirectory, @"CreateFolderPermissionEx\expected.msi"), light.OutputFile, "MsiLockPermissionsEx");
        }

        [NamedFact]
        [Description("Verify PermissionEx can be used twice on one File")]
        [Priority(2)]
        public void PermissionExTwiceOnOneFile()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PermissionExTests.TestDataDirectory, @"PermissionExTwiceOnOneFile\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // Only run validation if the current version of Windows Installer is 5.0 or above
            if (DTF.Installer.Version < MSIVersions.GetVersion(MSIVersions.Versions.MSI50))
            {
                light.SuppressMSIAndMSMValidation = true;
            }

            light.Run();

            Verifier.VerifyResults(Path.Combine(PermissionExTests.TestDataDirectory, @"PermissionExTwiceOnOneFile\expected.msi"), light.OutputFile, "MsiLockPermissionsEx");
        }
    }
}