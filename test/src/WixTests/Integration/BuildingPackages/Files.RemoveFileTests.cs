// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Files
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for the RemoveFile element
    /// </summary>
    public class RemoveFilesTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Files\RemoveFilesTests");

        [NamedFact]
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

        [NamedFact]
        [Description("Verify that multiple files can be removed with wildcard characters")]
        [Priority(1)]
        public void WildcardRemoveFile()
        {
            string sourceFile = Path.Combine(RemoveFilesTests.TestDataDirectory, @"WildcardRemoveFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `FileName` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile1'";
            Verifier.VerifyQuery(msi, query, "Test?.*");
        }

        [NamedFact]
        [Description("Verify that the file to be removed can be specified in a Property")]
        [Priority(1)]
        public void RemoveFileWithProperty()
        {
            string sourceFile = Path.Combine(RemoveFilesTests.TestDataDirectory, @"RemoveFileWithProperty\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `DirProperty` FROM `RemoveFile` WHERE `FileKey` = 'RemoveFile1'";
            Verifier.VerifyQuery(msi, query, "TESTDIR");
        }

        [NamedFact]
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
