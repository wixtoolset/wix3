// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using IWshRuntimeLibrary;
    using Xunit;

    /// <summary>
    /// Util extension WixInternetShortcut element tests
    /// </summary>
    public class WixInternetShortcutTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\WixInternetShortcutTests");

        [NamedFact]
        [Description("Verify that the (WixInternetShortcut and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void WixInternetShortcut_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(WixInternetShortcutTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("WixSchedInternetShortcuts", 1, "WixCA", "WixSchedInternetShortcuts"),
                new CustomActionTableData("WixRollbackInternetShortcuts", 3329, "WixCA", "WixRollbackInternetShortcuts"),
                new CustomActionTableData("WixCreateInternetShortcuts", 3073, "WixCA", "WixCreateInternetShortcuts"));

            // Verify WixInternetShortcut table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.WixInternetShortcut,
                new TableRow(WixInternetShortcutColumns.WixInternetShortcut.ToString(), "A"),
                new TableRow(WixInternetShortcutColumns.Component_.ToString(), "SampleComponent"),
                new TableRow(WixInternetShortcutColumns.Directory_.ToString(), "DesktopFolder"),
                new TableRow(WixInternetShortcutColumns.Name.ToString(), "Joy of Setup.lnk"),
                new TableRow(WixInternetShortcutColumns.Target.ToString(), "http://joyofsetup.com"),
                new TableRow(WixInternetShortcutColumns.Attributes.ToString(), "0", false)
                );

            // Verify WixInternetShortcut table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.WixInternetShortcut,
                new TableRow(WixInternetShortcutColumns.WixInternetShortcut.ToString(), "A1"),
                new TableRow(WixInternetShortcutColumns.Component_.ToString(), "SampleComponent"),
                new TableRow(WixInternetShortcutColumns.Directory_.ToString(), "SecretLinksDirectory"),
                new TableRow(WixInternetShortcutColumns.Name.ToString(), "InternetShortcuts announcement.lnk"),
                new TableRow(WixInternetShortcutColumns.Target.ToString(), "http://www.joyofsetup.com/2008/03/18/new-wix-feature-internet-shortcuts/"),
                new TableRow(WixInternetShortcutColumns.Attributes.ToString(), "0", false)
                );

            // Verify WixInternetShortcut table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.WixInternetShortcut,
                new TableRow(WixInternetShortcutColumns.WixInternetShortcut.ToString(), "B"),
                new TableRow(WixInternetShortcutColumns.Component_.ToString(), "SampleComponent"),
                new TableRow(WixInternetShortcutColumns.Directory_.ToString(), "ProgramMenuFolder"),
                new TableRow(WixInternetShortcutColumns.Name.ToString(), "Aaron Stebner WebLog.lnk"),
                new TableRow(WixInternetShortcutColumns.Target.ToString(), "http://blogs.msdn.com/astebner/default.aspx"),
                new TableRow(WixInternetShortcutColumns.Attributes.ToString(), "0", false)
                );
        }

        [NamedFact]
        [Description("Verify that the msi installs and the shortcuts were created.")]
        [Priority(2)]
        [RuntimeTest]
        public void WixInternetShortcut_Install()
        {
            string sourceFile = Path.Combine(WixInternetShortcutTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            string desktopPath = string.Format(@"{0}\Users\Public\Desktop", Environment.ExpandEnvironmentVariables(@"%SystemDrive%"));
            string secretLinksPath = string.Format(@"{0}\WiX Sample\Secret Links", Environment.ExpandEnvironmentVariables(@"%ProgramFiles%"));
            string programMenuPath = string.Format(@"{0}\ProgramData\Microsoft\Windows\Start Menu\Programs", Environment.ExpandEnvironmentVariables(@"%SystemDrive%"));

            string link1Location = Path.Combine(desktopPath, "Joy of Setup.lnk");
            string link2Location = Path.Combine(secretLinksPath, "InternetShortcuts announcement.lnk");
            string link3Location = Path.Combine(programMenuPath, "Aaron Stebner WebLog.lnk");
            string link4Location = Path.Combine(programMenuPath, "Heath Stewart's Blog.lnk");
            string link5Location = Path.Combine(programMenuPath, "Peter Marcu's Blog.lnk");
            string link6Location = Path.Combine(programMenuPath, "Rob Mensching Openly Uninstalled.lnk");
            string link7Location = Path.Combine(programMenuPath, "Setup Sense and Sensibility.lnk");
            string link8Location = Path.Combine(programMenuPath, "votive, wix, vsip, and all things microsoft.lnk");
            string link9Location = Path.Combine(programMenuPath, "Windows Installer Team Blog.url");
            string link10Location = Path.Combine(programMenuPath, "ARP.url");

            // Verify that the Internet short cuts were created
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link1Location), String.Format("Shortcut '{0}' was not created on Install.", link1Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link2Location), String.Format("Shortcut '{0}' was not created on Install.", link2Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link3Location), String.Format("Shortcut '{0}' was not created on Install.", link3Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link4Location), String.Format("Shortcut '{0}' was not created on Install.", link4Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link5Location), String.Format("Shortcut '{0}' was not created on Install.", link5Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link6Location), String.Format("Shortcut '{0}' was not created on Install.", link6Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link7Location), String.Format("Shortcut '{0}' was not created on Install.", link7Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link8Location), String.Format("Shortcut '{0}' was not created on Install.", link8Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link9Location), String.Format("Shortcut '{0}' was not created on Install.", link9Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link10Location), String.Format("Shortcut '{0}' was not created on Install.", link10Location));

            // verify the shortcuts have the right targets
            WixInternetShortcutTests.VerifyInterNetShortCutTarget(link9Location, "http://blogs.msdn.com/windows_installer_team/default.aspx", LinkType.URL);
            WixInternetShortcutTests.VerifyInterNetShortCutTarget(link10Location, Environment.ExpandEnvironmentVariables(@"file:///%SystemDrive%/Windows/Help/addremov.chm"), LinkType.URL);
            
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the Internet short cuts were removed
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link1Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link1Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link2Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link2Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link3Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link3Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link4Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link4Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link5Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link5Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link6Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link6Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link7Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link7Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link8Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link8Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link9Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link9Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link10Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link10Location));
        }

        [NamedFact]
        [Description("Verify that the msi installs to a 64-bit specific folder and the shortcuts were created.")]
        [Priority(2)]
        [RuntimeTest]
        [Is64BitSpecificTest]
        public void WixInternetShortcut_Install_64bit()
        {
            string sourceFile = Path.Combine(WixInternetShortcutTests.TestDataDirectory, @"product_64.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            string link1Location = Path.Combine(Environment.ExpandEnvironmentVariables(@"%ProgramW6432%\WiX Sample"), "notepad.lnk");
            string link1Target = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\System32\notepad.exe");
            string link2Location = Path.Combine(Environment.ExpandEnvironmentVariables(@"%ProgramW6432%\WiX Sample"), "notepad2.url");
            string link2Target = Environment.ExpandEnvironmentVariables("file:///%SystemDrive%/Windows/system32/notepad.exe");

            // Verify that the Internet short cuts were created
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link1Location), String.Format("Shortcut '{0}' was not created on Install.", link1Location));
            Assert.True(WixInternetShortcutTests.InterNetShortCutExists(link2Location), String.Format("Shortcut '{0}' was not created on Install.", link2Location));
            
            // verify the shortcuts have the right targets
            WixInternetShortcutTests.VerifyInterNetShortCutTarget(link1Location, link1Target, LinkType.Link);
            WixInternetShortcutTests.VerifyInterNetShortCutTarget(link2Location, link2Target, LinkType.URL);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the Internet short cuts were removed
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link1Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link1Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link2Location), String.Format("Shortcut '{0}' was not removed on Uninstall.", link2Location));
        }

        [NamedFact]
        [Description("Verify that the Internet shortcuts are removed after rollback.")]
        [Priority(2)]
        [RuntimeTest]
        public void WixInternetShortcut_InstallFailure()
        {
            string sourceFile = Path.Combine(WixInternetShortcutTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            string desktopPath = string.Format(@"{0}\Users\Public\Desktop", Environment.ExpandEnvironmentVariables(@"%SystemDrive%"));
            string secretLinksPath = string.Format(@"{0}\WiX Sample\Secret Links", Environment.ExpandEnvironmentVariables(@"%ProgramFiles%"));
            string programMenuPath = string.Format(@"{0}\ProgramData\Microsoft\Windows\Start Menu\Programs", Environment.ExpandEnvironmentVariables(@"%SystemDrive%"));

            string link1Location = Path.Combine(desktopPath, "Joy of Setup.lnk");
            string link2Location = Path.Combine(secretLinksPath, "InternetShortcuts announcement.lnk");
            string link3Location = Path.Combine(programMenuPath, "Aaron Stebner WebLog.lnk");
            string link4Location = Path.Combine(programMenuPath, "Heath Stewart's Blog.lnk");
            string link5Location = Path.Combine(programMenuPath, "Peter Marcu's Blog.lnk");
            string link6Location = Path.Combine(programMenuPath, "Rob Mensching Openly Uninstalled.lnk");
            string link7Location = Path.Combine(programMenuPath, "Setup Sense and Sensibility.lnk");
            string link8Location = Path.Combine(programMenuPath, "votive, wix, vsip, and all things microsoft.lnk");
            string link9Location = Path.Combine(programMenuPath, "Windows Installer Team Blog.url");
            string link10Location = Path.Combine(programMenuPath, "ARP.url");

            // Verify that the Internet short cuts were removed
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link1Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link1Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link2Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link2Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link3Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link3Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link4Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link4Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link5Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link5Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link6Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link6Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link7Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link7Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link8Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link8Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link9Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link9Location));
            Assert.False(WixInternetShortcutTests.InterNetShortCutExists(link10Location), String.Format("Shortcut '{0}' was not removed on Rollback.", link10Location));
        }

        #region Helper Methods

        /// <summary>
        /// Type of a shortcut
        /// </summary>
        public enum LinkType
        {
            Link,
            URL
        };

        /// <summary>
        /// Verify that the Internet Shortcuts were created 
        /// </summary>
        /// <param name="path">Path were the Internet short cuts should be created</param>
        public static bool InterNetShortCutExists(string path)
        {
            return System.IO.File.Exists(path);
        }

        /// <summary>
        /// Verify that the target of a Shortcut
        /// </summary>
        /// <param name="path">Path were the Internet short cuts should be created</param>
        /// <param name="target">expected target</param>
        /// <param name="linkType">is it a .lnk or a .url</param>
        public static void VerifyInterNetShortCutTarget(string path, string target, LinkType linkType)
        {
            if (System.IO.File.Exists(path))
            {
                if (linkType == LinkType.Link)
                {
                    WshShell shell = new WshShell();
                    IWshShortcut link = (IWshShortcut)shell.CreateShortcut(path);
                    Assert.True(link.TargetPath.Equals(target), String.Format("Shourtcut '{0}' target does not match expected. Actual: '{1}'. Expected: '{2}'.", path, link.TargetPath, target));
                }
                else if (linkType == LinkType.URL)
                {
                    WshShell shell = new WshShell();
                    WshURLShortcut link = (WshURLShortcut)shell.CreateShortcut(path);
                    Assert.True(link.TargetPath.Equals(target), String.Format("URL Shourtcut '{0}' target does not match expected. Actual: '{1}'. Expected: '{2}'.", path, link.TargetPath, target));
                }
            }
            else
            {
                Assert.True(false, String.Format("File '{0}' was not found; it was expected to.", path));
            }
        }
         #endregion
    }
}
