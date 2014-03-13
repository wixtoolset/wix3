//-----------------------------------------------------------------------
// <copyright file="Files.RemoveFileTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the RemoveFile element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Files
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for the RemoveFile element
    /// </summary>
    [TestClass]
    public class RemoveFilesTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Files\RemoveFilesTests");

        [TestMethod]
        [Description("Verify that a file that is installed can removed on install, uninstall or both")]
        [Priority(1)]
        public void SimpleRemoveFile()
        {
            string sourceFile = Path.Combine(RemoveFilesTests.TestDataDirectory, @"SimpleRemoveFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `InstallMode` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile1'";
            string query1 = "SELECT `InstallMode` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile2'";
            string query2 = "SELECT `InstallMode` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile3'";
            Verifier.VerifyQuery(msi, query, "1");
            Verifier.VerifyQuery(msi, query1, "2");
            Verifier.VerifyQuery(msi, query2, "3");
        }

        [TestMethod]
        [Description("Verify that multiple files can be removed with wildcard characters")]
        [Priority(1)]
        public void WildcardRemoveFile()
        {
            string sourceFile = Path.Combine(RemoveFilesTests.TestDataDirectory, @"WildcardRemoveFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `FileName` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile1'";
            Verifier.VerifyQuery(msi, query, "Test?.*");
        }

        [TestMethod]
        [Description("Verify that the file to be removed can be specified in a Property")]
        [Priority(1)]
        public void RemoveFileWithProperty()
        {
            string sourceFile = Path.Combine(RemoveFilesTests.TestDataDirectory, @"RemoveFileWithProperty\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `DirProperty` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile1'";
            Verifier.VerifyQuery(msi, query, "TESTDIR");
        }

        [TestMethod]
        [Description("Verify that the file to be removed can be specified in a Directory reference")]
        [Priority(1)]
        public void RemoveFileWithDirectory()
        {
            string sourceFile = Path.Combine(RemoveFilesTests.TestDataDirectory, @"RemoveFileWithDirectory\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `DirProperty` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile1'";
            Verifier.VerifyQuery(msi, query, "TARGETDIR");
        }
    }
}
