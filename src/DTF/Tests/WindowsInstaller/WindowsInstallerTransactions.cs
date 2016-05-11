// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Deployment.Test
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using System.Globalization;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Deployment.WindowsInstaller;
    using View = Microsoft.Deployment.WindowsInstaller.View;

    [TestClass]
    public class WindowsInstallerTransactions
    {
        [TestInitialize()]
        public void Initialize()
        {
        }

        [TestCleanup()]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void InstallerTransactTwoProducts()
        {
            string dbFile1 = "InstallerTransactProduct1.msi";
            string dbFile2 = "InstallerTransactProduct2.msi";
            string productCode1;
            string productCode2;

            using (Database db1 = new Database(dbFile1, DatabaseOpenMode.CreateDirect))
            {
                WindowsInstallerUtils.InitializeProductDatabase(db1);
                WindowsInstallerUtils.CreateTestProduct(db1);

                productCode1 = db1.ExecuteStringQuery("SELECT `Value` FROM `Property` WHERE `Property` = 'ProductCode'")[0];

                db1.Commit();
            }

            using (Database db2 = new Database(dbFile2, DatabaseOpenMode.CreateDirect))
            {
                WindowsInstallerUtils.InitializeProductDatabase(db2);
                WindowsInstallerUtils.CreateTestProduct(db2);

                productCode2 = db2.ExecuteStringQuery("SELECT `Value` FROM `Property` WHERE `Property` = 'ProductCode'")[0];

                db2.Commit();
            }

            ProductInstallation installation1 = new ProductInstallation(productCode1);
            ProductInstallation installation2 = new ProductInstallation(productCode2);
            Assert.IsFalse(installation1.IsInstalled, "Checking that product 1 is not installed before starting.");
            Assert.IsFalse(installation2.IsInstalled, "Checking that product 2 is not installed before starting.");

            Installer.SetInternalUI(InstallUIOptions.Silent);
            ExternalUIHandler prevHandler = Installer.SetExternalUI(WindowsInstallerTest.ExternalUILogger,
                InstallLogModes.FatalExit |
                InstallLogModes.Error |
                InstallLogModes.Warning |
                InstallLogModes.User |
                InstallLogModes.Info |
                InstallLogModes.ResolveSource |
                InstallLogModes.OutOfDiskSpace |
                InstallLogModes.ActionStart |
                InstallLogModes.ActionData |
                InstallLogModes.CommonData |
                InstallLogModes.Progress |
                InstallLogModes.Initialize |
                InstallLogModes.Terminate |
                InstallLogModes.ShowDialog);
            Assert.IsNull(prevHandler, "Checking that returned previous UI handler is null.");

            Transaction transaction = new Transaction("TestInstallTransaction", TransactionAttributes.None);

            Exception caughtEx = null;
            try
            {
                Installer.InstallProduct(dbFile1, String.Empty);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Exception thrown while installing product 1: " + caughtEx);

            Console.WriteLine();
            Console.WriteLine("===================================================================");
            Console.WriteLine();

            try
            {
                Installer.InstallProduct(dbFile2, String.Empty);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Exception thrown while installing product 2: " + caughtEx);

            transaction.Commit();
            transaction.Close();

            prevHandler = Installer.SetExternalUI(prevHandler, InstallLogModes.None);
            Assert.AreEqual<ExternalUIHandler>(WindowsInstallerTest.ExternalUILogger, prevHandler, "Checking that previously-set UI handler is returned.");

            Assert.IsTrue(installation1.IsInstalled, "Checking that product 1 is installed.");
            Assert.IsTrue(installation2.IsInstalled, "Checking that product 2 is installed.");

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("===================================================================");
            Console.WriteLine("===================================================================");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            ExternalUIRecordHandler prevRecHandler = Installer.SetExternalUI(WindowsInstallerTest.ExternalUIRecordLogger,
                InstallLogModes.FatalExit |
                InstallLogModes.Error |
                InstallLogModes.Warning |
                InstallLogModes.User |
                InstallLogModes.Info |
                InstallLogModes.ResolveSource |
                InstallLogModes.OutOfDiskSpace |
                InstallLogModes.ActionStart |
                InstallLogModes.ActionData |
                InstallLogModes.CommonData |
                InstallLogModes.Progress |
                InstallLogModes.Initialize |
                InstallLogModes.Terminate |
                InstallLogModes.ShowDialog);
            Assert.IsNull(prevRecHandler, "Checking that returned previous UI record handler is null.");

            transaction = new Transaction("TestUninstallTransaction", TransactionAttributes.None);

            try
            {
                Installer.InstallProduct(dbFile1, "REMOVE=All");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Exception thrown while removing product 1: " + caughtEx);

            try
            {
                Installer.InstallProduct(dbFile2, "REMOVE=All");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Exception thrown while removing product 2: " + caughtEx);

            transaction.Commit();
            transaction.Close();

            Assert.IsFalse(installation1.IsInstalled, "Checking that product 1 is not installed after removing.");
            Assert.IsFalse(installation2.IsInstalled, "Checking that product 2 is not installed after removing.");

            prevRecHandler = Installer.SetExternalUI(prevRecHandler, InstallLogModes.None);
            Assert.AreEqual<ExternalUIRecordHandler>(WindowsInstallerTest.ExternalUIRecordLogger, prevRecHandler, "Checking that previously-set UI record handler is returned.");
        }
    }
}
