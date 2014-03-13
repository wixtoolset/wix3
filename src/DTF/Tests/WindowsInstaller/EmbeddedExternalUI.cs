//---------------------------------------------------------------------
// <copyright file="EmbeddedExternalUI.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation tests.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.Test
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Globalization;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Deployment.WindowsInstaller;
    using View = Microsoft.Deployment.WindowsInstaller.View;

    [TestClass]
    public class EmbeddedExternalUI
    {
        const InstallLogModes TestLogModes =
                InstallLogModes.FatalExit |
                InstallLogModes.Error |
                InstallLogModes.Warning |
                InstallLogModes.User |
                InstallLogModes.Info |
                InstallLogModes.ResolveSource |
                InstallLogModes.OutOfDiskSpace |
                InstallLogModes.ActionStart |
                InstallLogModes.ActionData |
                InstallLogModes.CommonData;

#if DEBUG
        const string EmbeddedUISampleBinDir = @"..\..\..\..\..\build\debug\x86\";
#else
        const string EmbeddedUISampleBinDir = @"..\..\..\..\..\build\ship\x86\";
#endif

        [TestMethod]
        public void EmbeddedUISingleInstall()
        {
            string dbFile = "EmbeddedUISingleInstall.msi";
            string productCode;

            string uiDir = Path.GetFullPath(EmbeddedExternalUI.EmbeddedUISampleBinDir);
            string uiFile = "Microsoft.Deployment.Samples.EmbeddedUI.dll";

            using (Database db = new Database(dbFile, DatabaseOpenMode.CreateDirect))
            {
                WindowsInstallerUtils.InitializeProductDatabase(db);
                WindowsInstallerUtils.CreateTestProduct(db);

                productCode = db.ExecuteStringQuery("SELECT `Value` FROM `Property` WHERE `Property` = 'ProductCode'")[0];

                using (Record uiRec = new Record(5))
                {
                    uiRec[1] = "TestEmbeddedUI";
                    uiRec[2] = Path.GetFileNameWithoutExtension(uiFile) + ".Wrapper.dll";
                    uiRec[3] = 1;
                    uiRec[4] = (int) (
                        EmbeddedExternalUI.TestLogModes |
                        InstallLogModes.Progress |
                        InstallLogModes.Initialize |
                        InstallLogModes.Terminate |
                        InstallLogModes.ShowDialog);
                    uiRec.SetStream(5, Path.Combine(uiDir, uiFile));
                    db.Execute(db.Tables["MsiEmbeddedUI"].SqlInsertString, uiRec);
                }

                db.Commit();
            }

            Installer.SetInternalUI(InstallUIOptions.Full);

            ProductInstallation installation = new ProductInstallation(productCode);
            Assert.IsFalse(installation.IsInstalled, "Checking that product is not installed before starting.");

            Exception caughtEx = null;
            try
            {
                Installer.EnableLog(EmbeddedExternalUI.TestLogModes, "install.log");
                Installer.InstallProduct(dbFile, String.Empty);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Exception thrown while installing product: " + caughtEx);

            Assert.IsTrue(installation.IsInstalled, "Checking that product is installed.");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("===================================================================");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            try
            {
                Installer.EnableLog(EmbeddedExternalUI.TestLogModes, "uninstall.log");
                Installer.InstallProduct(dbFile, "REMOVE=All");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Exception thrown while uninstalling product: " + caughtEx);
        }

        // This test does not pass if run normally.
        // It only passes when a failure is injected into the EmbeddedUI launcher.
        ////[TestMethod]
        public void EmbeddedUIInitializeFails()
        {
            string dbFile = "EmbeddedUIInitializeFails.msi";
            string productCode;

            string uiDir = Path.GetFullPath(EmbeddedExternalUI.EmbeddedUISampleBinDir);
            string uiFile = "Microsoft.Deployment.Samples.EmbeddedUI.dll";

            // A number that will be used to check whether a type 19 CA runs.
            const string magicNumber = "3.14159265358979323846264338327950";

            using (Database db = new Database(dbFile, DatabaseOpenMode.CreateDirect))
            {
                WindowsInstallerUtils.InitializeProductDatabase(db);
                WindowsInstallerUtils.CreateTestProduct(db);

                const string failureActionName = "EmbeddedUIInitializeFails";
                db.Execute("INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) " +
                    "VALUES ('{0}', 19, '', 'Logging magic number: {1}')", failureActionName, magicNumber);

                // This type 19 CA (launch condition) is given a condition of 'UILevel = 3' so that it only runs if the
                // installation is running in BASIC UI mode, which is what we expect if the EmbeddedUI fails to initialize.
                db.Execute("INSERT INTO `InstallExecuteSequence` (`Action`, `Condition`, `Sequence`) " +
                    "VALUES ('{0}', 'UILevel = 3', 1)", failureActionName);

                productCode = db.ExecuteStringQuery("SELECT `Value` FROM `Property` WHERE `Property` = 'ProductCode'")[0];

                using (Record uiRec = new Record(5))
                {
                    uiRec[1] = "TestEmbeddedUI";
                    uiRec[2] = Path.GetFileNameWithoutExtension(uiFile) + ".Wrapper.dll";
                    uiRec[3] = 1;
                    uiRec[4] = (int)(
                        EmbeddedExternalUI.TestLogModes |
                        InstallLogModes.Progress |
                        InstallLogModes.Initialize |
                        InstallLogModes.Terminate |
                        InstallLogModes.ShowDialog);
                    uiRec.SetStream(5, Path.Combine(uiDir, uiFile));
                    db.Execute(db.Tables["MsiEmbeddedUI"].SqlInsertString, uiRec);
                }

                db.Commit();
            }

            Installer.SetInternalUI(InstallUIOptions.Full);

            ProductInstallation installation = new ProductInstallation(productCode);
            Assert.IsFalse(installation.IsInstalled, "Checking that product is not installed before starting.");

            string logFile = "install.log";
            Exception caughtEx = null;
            try
            {
                Installer.EnableLog(EmbeddedExternalUI.TestLogModes, logFile);
                Installer.InstallProduct(dbFile, String.Empty);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(InstallerException),
                "Excpected InstallerException installing product; caught: " + caughtEx);

            Assert.IsFalse(installation.IsInstalled, "Checking that product is not installed.");

            string logText = File.ReadAllText(logFile);
            Assert.IsTrue(logText.Contains(magicNumber), "Checking that the type 19 custom action ran.");
        }
    }
}
