// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using System.Management;
    using Xunit;

    /// <summary>
    /// Util extension FileShare element tests
    /// </summary>
    public class FileShareTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\FileShareTests");

        protected override void TestInitialize()
        {
            // set the environment variable to store the current user information
            string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            Environment.SetEnvironmentVariable("tempdomain", username.Split('\\')[0]);
            Environment.SetEnvironmentVariable("tempusername", username.Split('\\')[1]);
        }

        [NamedFact]
        [Description("Verify that the (FileShare, FileSharePermissions and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void FileShare_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(FileShareTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureSmbInstall", 1, "ScaSchedule", "ConfigureSmbInstall"),
                new CustomActionTableData("ConfigureSmbUninstall", 1, "ScaSchedule", "ConfigureSmbUninstall"),
                new CustomActionTableData("CreateSmb", 11265, "ScaExecute", "CreateSmb"),
                new CustomActionTableData("CreateSmbRollback", 11585, "ScaExecute", "DropSmb"),
                new CustomActionTableData("DropSmb", 11265, "ScaExecute", "DropSmb"));

            // Verify FileShare table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.FileShare,
                new TableRow(FileShareColumns.FileShare.ToString(), "TestShare"),
                new TableRow(FileShareColumns.ShareName.ToString(), "TestShareName"),
                new TableRow(FileShareColumns.Component_.ToString(), "Component1"),
                new TableRow(FileShareColumns.Description.ToString(), "This is a test share."),
                new TableRow(FileShareColumns.Directory_.ToString(), "WixTestFolder"),
                new TableRow(FileShareColumns.User_.ToString(), string.Empty),
                new TableRow(FileShareColumns.Permissions.ToString(), string.Empty, false));

            // Verify FileSharePermissions table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.FileSharePermissions,
                new TableRow(FileSharePermissionsColumns.FileShare_.ToString(), "TestShare"),
                new TableRow(FileSharePermissionsColumns.User_.ToString(), "User1"),
                new TableRow(FileSharePermissionsColumns.Permissions.ToString(), "268435456", false));

            Verifier.VerifyTableData(msiFile, MSITables.FileSharePermissions,
                new TableRow(FileSharePermissionsColumns.FileShare_.ToString(), "TestShare"),
                new TableRow(FileSharePermissionsColumns.User_.ToString(), "User2"),
                new TableRow(FileSharePermissionsColumns.Permissions.ToString(), "-2146435072", false));
        }

        [NamedFact]
        [Description("Install the MSI and verify that the testShare is created with the right permissions.")]
        [Priority(2)]
        [RuntimeTest]
        public void FileShare_Install()
        {
            string sourceFile = Path.Combine(FileShareTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Fileshare Exists.
            string testFolderPath = Path.Combine(@"\\" + System.Environment.MachineName, "TestShareName");
            Assert.True(Directory.Exists(testFolderPath), String.Format("Share '{0}' was not created on Install.", testFolderPath));

            // Verify Fileshare Permissions.
            PermissionsVerifier.VerifySharePermession(Environment.GetEnvironmentVariable("tempdomain") + "\\ddrelqa1", testFolderPath, PermissionsVerifier.ACCESS_MASK.READ_CONTROL | PermissionsVerifier.ACCESS_MASK.WRITE_DAC | PermissionsVerifier.ACCESS_MASK.WRITE_OWNER | PermissionsVerifier.ACCESS_MASK.DELETE);
            PermissionsVerifier.VerifySharePermession(Environment.GetEnvironmentVariable("tempdomain") + "\\ddrelqa2", testFolderPath, PermissionsVerifier.ACCESS_MASK.READ_CONTROL);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Fileshare is removed.
            Assert.False(Directory.Exists(testFolderPath), String.Format("Share '{0}' was not removed on Uninstall.", testFolderPath));
        }

        [NamedFact]
        [Description("Install the MSI and verify that the testShare is created with the right permissions in a 64-bit specific folder.")]
        [Priority(2)]
        [RuntimeTest]
        [Is64BitSpecificTest]
        public void FileShare_Install_64bit()
        {
            string sourceFile = Path.Combine(FileShareTests.TestDataDirectory, @"product_64.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Directory Exists.
            string physicalTestFolderPath = Path.Combine(Environment.ExpandEnvironmentVariables(@"%ProgramW6432%"), @"WixTestFolder");
            Assert.True(Directory.Exists(physicalTestFolderPath), String.Format("Test folder '{0}' was not created on Install.", physicalTestFolderPath));

            // Verify Fileshare Exists.
            string testFolderPath = Path.Combine(@"\\" + System.Environment.MachineName, "TestShareName");
            Assert.True(Directory.Exists(testFolderPath), String.Format("Share '{0}' was not created on Install.", testFolderPath));

            // Verify Fileshare Permissions.
            PermissionsVerifier.VerifySharePermession(Environment.GetEnvironmentVariable("tempdomain") + "\\ddrelqa1", testFolderPath, PermissionsVerifier.ACCESS_MASK.READ_CONTROL | PermissionsVerifier.ACCESS_MASK.WRITE_DAC | PermissionsVerifier.ACCESS_MASK.WRITE_OWNER | PermissionsVerifier.ACCESS_MASK.DELETE);
            PermissionsVerifier.VerifySharePermession(Environment.GetEnvironmentVariable("tempdomain") + "\\ddrelqa2", testFolderPath, PermissionsVerifier.ACCESS_MASK.READ_CONTROL);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Fileshare is removed.
            Assert.False(Directory.Exists(testFolderPath), String.Format("Share '{0}' was not removed on Uninstall.", testFolderPath));
        }

        [NamedFact]
        [Description("Install the MSI and verify that the testShare is removed when the setup is cancled.")]
        [Priority(2)]
        [RuntimeTest]
        public void FileShare_InstallFailure()
        {
            string sourceFile = Path.Combine(FileShareTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify Fileshare is removed.
            string testFolderPath = Path.Combine(@"\\" + System.Environment.MachineName, "TestShareName");
            Assert.False(Directory.Exists(testFolderPath), String.Format("Share '{0}' was not removed on Rollback.", testFolderPath));
        }

        [NamedFact]
        [Description("Verify that if the fileshare already exists the the custom action does not fail.")]
        [Priority(2)]
        [RuntimeTest]
        // bug: https://sourceforge.net/tracker/?func=detail&aid=2824407&group_id=105970&atid=642714
        public void FileShare_ExistingShare()
        {
            string sourceFile = Path.Combine(FileShareTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            // create the share
            string testFolderPath = Path.Combine(@"\\" + System.Environment.MachineName, "TestShareName");
            string testForderPhysicalPath = Utilities.FileUtilities.GetUniqueFileName();
            Directory.CreateDirectory(testForderPhysicalPath);
            FileUtilities.CreateShare(testForderPhysicalPath, "TestShareName");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Fileshare still Exists.
            Assert.True(Directory.Exists(testFolderPath), String.Format("Share '{0}' was not created on Install.", testFolderPath));

            // Verify Fileshare Permissions.
            PermissionsVerifier.VerifySharePermession(Environment.GetEnvironmentVariable("tempdomain") + "\\ddrelqa1", testFolderPath, PermissionsVerifier.ACCESS_MASK.READ_CONTROL | PermissionsVerifier.ACCESS_MASK.WRITE_DAC | PermissionsVerifier.ACCESS_MASK.WRITE_OWNER | PermissionsVerifier.ACCESS_MASK.DELETE);
            PermissionsVerifier.VerifySharePermession(Environment.GetEnvironmentVariable("tempdomain") + "\\ddrelqa2", testFolderPath, PermissionsVerifier.ACCESS_MASK.READ_CONTROL);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Fileshare is removed.
            Assert.False(Directory.Exists(testFolderPath), String.Format("Share '{0}' was not removed on Uninstall.", testFolderPath));
        }
    }
}
