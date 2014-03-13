//-----------------------------------------------------------------------
// <copyright file="IISExtension.IISCertificateTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>IIS Extension Certificate tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// IIS extension Certificate element tests
    /// </summary>
    [TestClass]
    public class IISCertificateTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISCertificateTests");

        [TestMethod]
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

        [TestMethod]
        [Description("Install the MSI. Verify that “TestCertifiate” was installed. Uninstall the MSi Verify that the certificate is removed")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISCertificate_Install()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");
        
            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was installed
            Assert.IsTrue(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), "Certificate '{0}' was not created in the LocalMachine store on Install", "machinecert");
            Assert.IsTrue(IISVerifier.CertificateExists("User", StoreLocation.CurrentUser), "Certificate '{0}' was not created in the CurrentUser store on Install", "machinecert");
            Assert.IsTrue(IISVerifier.CertificateExists("machineCertFromBinary", StoreLocation.LocalMachine), "Certificate '{0}' was not created in the LocalMachine store on Install", "machineCertFromBinary");
            Assert.IsTrue(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), "Certificate '{0}' was not created in the LocalMachine store on Install", "TestCertPrivateKey");

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was removed
            Assert.IsFalse(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), "Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "machinecert");
            Assert.IsFalse(IISVerifier.CertificateExists("User", StoreLocation.CurrentUser), "Certificate '{0}' was not removed from the CurrentUser store on Uninstall", "machinecert");
            Assert.IsFalse(IISVerifier.CertificateExists("machineCertFromBinary", StoreLocation.LocalMachine), "Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "machineCertFromBinary");
            Assert.IsFalse(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), "Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "TestCertPrivateKey");
        }

        [TestMethod]
        [Description("Install the MSI. Verify that “TestCertifiate” was installed. Uninstall the MSi Verify that the certificate is removed")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISCertificate_CertificateRef_Install()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"CertificateRef.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was installed
            Assert.IsTrue(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), "Certificate '{0}' was not created in the LocalMachine store on Install", "machinecert");

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Machine certificate was removed
            Assert.IsFalse(IISVerifier.CertificateExists("machinecert", StoreLocation.LocalMachine), "Certificate '{0}' was not removed from the LocalMachine store on Uninstall", "machinecert");
        }

        [TestMethod]
        [Description("Install the MSI. Verify installtion fails")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISCertificate_WrongPassword_InstallFailure()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"WrongPassword.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            string logFile = MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify the log file for failure reason
            Assert.IsTrue(LogVerifier.MessageInLogFile(logFile, "Failed to open PFX file"), "Could not find fail message in log file: '{0}'.", logFile);

            // Verify Machine certificate was not created
            Assert.IsFalse(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), "Certificate '{0}' was not removed from the LocalMachine store on Rollback", "TestCertPrivateKey");
        }

        [TestMethod]
        [Description("Install the MSI. Verify installtion fails")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISCertificate_InvalidCertificateFile_InstallFailure()
        {
            string sourceFile = Path.Combine(IISCertificateTests.TestDataDirectory, @"InvalidCertificateFile.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            string logFile = MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify the log file for failure reason
            Assert.IsTrue(LogVerifier.MessageInLogFile(logFile, "Failed to read certificate from file path"), "Could not find fail message in log file: '{0}'.", logFile);

            // Verify Machine certificate was not created
            Assert.IsFalse(IISVerifier.CertificateExists("TestCertPrivateKey", StoreLocation.LocalMachine), "Certificate '{0}' was not removed from the LocalMachine store on Rollback", "TestCertPrivateKey");
        }
    }
}
