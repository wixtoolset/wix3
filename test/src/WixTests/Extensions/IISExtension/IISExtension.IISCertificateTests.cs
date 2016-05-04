// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using System.Security.Cryptography.X509Certificates;
    using Xunit;

    /// <summary>
    /// IIS extension Certificate element tests
    /// </summary>
    public class IISCertificateTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISCertificateTests");

        [NamedFact]
        [Description("Verify that the (Certificate,CustomAction) Tables are created in the MSI and have defined data.")]
        [Priority(1)]
        public void IISCertificate_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("AddMachineCertificate", 11265, "IIsExecute", "AddMachineCertificate"),
                new CustomActionTableData("DeleteMachineCertificate", 11265, "IIsExecute", "DeleteMachineCertificate"),
                new CustomActionTableData("InstallCertificates", 1, "IIsSchedule", "InstallCertificates"),
                new CustomActionTableData("RollbackAddMachineCertificate", 11521, "IIsExecute", "DeleteMachineCertificate"),
                new CustomActionTableData("RollbackDeleteMachineCertificate", 11521, "IIsExecute", "AddMachineCertificate"),
                new CustomActionTableData("UninstallCertificates", 1, "IIsSchedule", "UninstallCertificates"));

            Verifier.VerifyTableData(msiFile, MSITables.Certificate,
                new TableRow(CertificateColumns.Certificate.ToString(), "MachineTestCertificate1"),
                new TableRow(CertificateColumns.Component_.ToString(), "MachineCertificateComponent"),
                new TableRow(CertificateColumns.Name.ToString(), "machinecert"),
                new TableRow(CertificateColumns.StoreLocation.ToString(), "2", false),
                new TableRow(CertificateColumns.StoreName.ToString(), "MY"),
                new TableRow(CertificateColumns.Attributes.ToString(), "0", false),
                new TableRow(CertificateColumns.Binary_.ToString(), string.Empty),
                new TableRow(CertificateColumns.CertificatePath.ToString(), Path.Combine(IISCertificateTests.TestDataDirectory, "Testcertificate1.cer")),
                new TableRow(CertificateColumns.PFXPassword.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.Certificate,
                new TableRow(CertificateColumns.Certificate.ToString(), "MachineTestCertificate2"),
                new TableRow(CertificateColumns.Component_.ToString(), "MachineCertificateComponent"),
                new TableRow(CertificateColumns.Name.ToString(), "User"),
                new TableRow(CertificateColumns.StoreLocation.ToString(), "1", false),
                new TableRow(CertificateColumns.StoreName.ToString(), "MY"),
                new TableRow(CertificateColumns.Attributes.ToString(), "0", false),
                new TableRow(CertificateColumns.Binary_.ToString(), string.Empty),
                new TableRow(CertificateColumns.CertificatePath.ToString(), Path.Combine(IISCertificateTests.TestDataDirectory, "Testcertificate2.cer")),
                new TableRow(CertificateColumns.PFXPassword.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.Certificate,
                new TableRow(CertificateColumns.Certificate.ToString(), "MachineTestCertificate3"),
                new TableRow(CertificateColumns.Component_.ToString(), "MachineCertificateComponent"),
                new TableRow(CertificateColumns.Name.ToString(), "machineCertFromBinary"),
                new TableRow(CertificateColumns.StoreLocation.ToString(), "2", false),
                new TableRow(CertificateColumns.StoreName.ToString(), "MY"),
                new TableRow(CertificateColumns.Attributes.ToString(), "6", false),
                new TableRow(CertificateColumns.Binary_.ToString(), "Testcertificate3"),
                new TableRow(CertificateColumns.CertificatePath.ToString(), string.Empty),
                new TableRow(CertificateColumns.PFXPassword.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.Certificate,
               new TableRow(CertificateColumns.Certificate.ToString(), "MachineTestCertificate4"),
               new TableRow(CertificateColumns.Component_.ToString(), "MachineCertificateComponent"),
               new TableRow(CertificateColumns.Name.ToString(), "TestCertPrivateKey"),
               new TableRow(CertificateColumns.StoreLocation.ToString(), "2", false),
               new TableRow(CertificateColumns.StoreName.ToString(), "MY"),
               new TableRow(CertificateColumns.Attributes.ToString(), "0", false),
               new TableRow(CertificateColumns.Binary_.ToString(), string.Empty),
               new TableRow(CertificateColumns.CertificatePath.ToString(), Path.Combine(IISCertificateTests.TestDataDirectory, "Testcertificate4.pfx")),
               new TableRow(CertificateColumns.PFXPassword.ToString(), "test"));

            // Verify Table CertificateHash exists in the MSI
            Verifier.VerifyTableExists(msiFile, "CertificateHash");
        }

        [NamedFact]
        [Description("Install the MSI. Verify that �TestCertifiate� was installed. Uninstall the MSi Verify that the certificate is removed")]
        [Priority(2)]
        [RuntimeTest]
        public void IISCertificate_Install()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");
        
            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was installed
            Assert.True(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not created in the LocalMachine store on Install", "machinecert"));
            Assert.True(IISVerifier.CertificateExists("User", StoreLocation.CurrentUser), String.Format("Certificate '{0}' was not created in the CurrentUser store on Install", "machinecert"));
            Assert.True(IISVerifier.CertificateExists("machineCertFromBinary", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not created in the LocalMachine store on Install", "machineCertFromBinary"));
            Assert.True(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not created in the LocalMachine store on Install", "TestCertPrivateKey"));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was removed
            Assert.False(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "machinecert"));
            Assert.False(IISVerifier.CertificateExists("User", StoreLocation.CurrentUser), String.Format("Certificate '{0}' was not removed from the CurrentUser store on Uninstall", "machinecert"));
            Assert.False(IISVerifier.CertificateExists("machineCertFromBinary", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "machineCertFromBinary"));
            Assert.False(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "TestCertPrivateKey"));
        }

        [NamedFact]
        [Description("Install the MSI. Verify that �TestCertifiate� was installed. Uninstall the MSi Verify that the certificate is removed")]
        [Priority(2)]
        [RuntimeTest]
        public void IISCertificate_CertificateRef_Install()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"CertificateRef.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was installed
            Assert.True(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not created in the LocalMachine store on Install", "machinecert"));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was removed
            Assert.False(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "machinecert"));
        }

        [NamedFact]
        [Description("Install the MSI. Verify installtion fails")]
        [Priority(2)]
        [RuntimeTest]
        public void IISCertificate_WrongPassword_InstallFailure()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"WrongPassword.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            string logFile = MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify the log file for failure reason
            Assert.True(LogVerifier.MessageInLogFile(logFile, "Failed to open PFX file"), String.Format("Could not find fail message in log file: '{0}'.", logFile));

            // Verify Machine certificate was not created
            Assert.False(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not removed from the LocalMachine store on Rollback", "TestCertPrivateKey"));
        }

        [NamedFact]
        [Description("Install the MSI. Verify installtion fails")]
        [Priority(2)]
        [RuntimeTest]
        public void IISCertificate_InvalidCertificateFile_InstallFailure()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"InvalidCertificateFile.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            string logFile = MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify the log file for failure reason
            Assert.True(LogVerifier.MessageInLogFile(logFile, "Failed to read certificate from file path"), String.Format("Could not find fail message in log file: '{0}'.", logFile));

            // Verify Machine certificate was not created
            Assert.False(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), String.Format("Certificate '{0}' was not removed from the LocalMachine store on Rollback", "TestCertPrivateKey"));
        }
    }
}
