//-----------------------------------------------------------------------
// <copyright file="Files.CopyFileTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the CopyFile element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Files
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for the CopyFile element
    /// </summary>
    public class CopyFileTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Files\CopyFileTests");

        [NamedFact]
        [Description("Verify that a file that is installed can be copied")]
        [Priority(1)]
        public void CopyInstalledFile()
        {
            string sourceFile = Path.Combine(CopyFileTests.TestDataDirectory, @"CopyInstalledFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `DestName` FROM `DuplicateFile` WHERE `FileKey` = 'copytest'";
            Verifier.VerifyQuery(msi, query, "copytest.txt");
        }

        [NamedFact]
        [Description("Verify that a file that is installed can be moved")]
        [Priority(1)]
        public void MoveInstalledFile()
        {
            string sourceFile = Path.Combine(CopyFileTests.TestDataDirectory, @"MoveInstalledFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `SourceName` FROM `MoveFile` WHERE `FileKey` = 'copytest'";
            string query1 = "SELECT `Options` FROM `MoveFile` WHERE `FileKey` = 'copytest'";
            Verifier.VerifyQuery(msi, query, "TextFile1.txt");
            Verifier.VerifyQuery(msi, query1, "1");
        }

        [NamedFact]
        [Description("Verify that a file that is already on the machine can be copied")]
        [Priority(1)]
        public void CopyExistingFile()
        {
            string sourceFile = Path.Combine(CopyFileTests.TestDataDirectory, @"CopyExistingFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `SourceName` FROM `MoveFile` WHERE `FileKey` = 'copytest'";
            string query1 = "SELECT `Options` FROM `MoveFile` WHERE `FileKey` = 'copytest'";
            Verifier.VerifyQuery(msi, query, "TextFile1.txt");
            Verifier.VerifyQuery(msi, query1, "0");
        }

        [NamedFact]
        [Description("Verify that a file that is already on the machine can be moved")]
        [Priority(1)]
        public void MoveExistingFile()
        {
            string sourceFile = Path.Combine(CopyFileTests.TestDataDirectory, @"MoveExistingFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `SourceName` FROM `MoveFile` WHERE `FileKey` = 'copytest'";
            string query1 = "SELECT `Options` FROM `MoveFile` WHERE `FileKey` = 'copytest'";
            Verifier.VerifyQuery(msi, query, "TextFile1.txt");
            Verifier.VerifyQuery(msi, query1, "1");
        }


        [NamedFact]
        [Description("Verify that there is an error if FileId is not a defined file")]
        [Priority(3)]
        public void CopyNonExistingFile()
        {
            string sourceFile = Path.Combine(CopyFileTests.TestDataDirectory, @"CopyNonExistingFile\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedExitCode = 94;
            light.ExpectedWixMessages.Add(new WixMessage(94, "Unresolved reference to symbol 'File:test' in section 'Product:*'.", WixMessage.MessageTypeEnum.Error));
            light.Run();
        }
    }
}
