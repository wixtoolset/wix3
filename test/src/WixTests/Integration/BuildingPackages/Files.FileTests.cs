// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Files
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for authoring files into a package
    /// </summary>
    public class FileTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Files\FileTests");

        [NamedFact]
        [Description("Verify the simple use of the File element")]
        [Priority(1)]
        public void SimpleFile()
        {
            string sourceFile = Path.Combine(FileTests.TestDataDirectory, @"SimpleFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            Verifier.VerifyResults(Path.Combine(FileTests.TestDataDirectory, @"SimpleFile\expected.msi"), msi, "BindImage", "File", "MsiAssembly", "MsiAssemblyName");
        }

        [NamedFact]
        [Description("Verify an MSI can be built for an assembly targetting 4.0 Client")]
        [Priority(1)]
        public void AssemblyTarget40Client()
        {
            string sourceFile = Path.Combine(FileTests.TestDataDirectory, @"AssemblyTarget40Client\product.wxs");
            QuickTest.BuildMsiTest(sourceFile, Path.Combine(FileTests.TestDataDirectory, @"AssemblyTarget40Client\expected.msi"));
        }

        [NamedFact]
        [Description("Verify a companion file can be specified")]
        [Priority(2)]
        public void CompanionFile()
        {
            string sourceFile = Path.Combine(FileTests.TestDataDirectory, @"CompanionFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Version` FROM `File` WHERE `File` = 'Assembly2.exe'";
            Verifier.VerifyQuery(msi, query, "Assembly1.dll");
        }

        [NamedFact]
        [Description("Verify that the publicKeyToken in the MsiAssemblyName table is not 'NEUTRAL'")]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1801826&group_id=105970&atid=642714")]
        [Priority(3)]
        public void AssemblyNullPublicKey()
        {
            // Run Candle
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(FileTests.TestDataDirectory, @"AssemblyNullPublicKey\product.wxs"));
            candle.Run();

            // Run Light
            Light light = new Light(candle);

            string assembly1 = Path.Combine(WixTests.SharedFilesDirectory, @"TestBinaries\bin\Assembly1.dll");
            light.ExpectedWixMessages.Add(new WixMessage(282, String.Format("Assembly {0} in component Component1 has no strong name and has been marked to be placed in the GAC. All assemblies installed to the GAC must have a valid strong name.", assembly1), WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 282;

            light.Run();
        }

        [NamedFact]
        [Description("Verify that the specifing @DefaultVersion for a non versioned file does not cause its addition to the msifilehash table")]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1666461&group_id=105970&atid=642714")]
        [Priority(3)]
        public void DefaultVersion()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(FileTests.TestDataDirectory, @"DefaultVersion\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(1103, "The DefaultVersion '3.0.0.0' was used for file 'TextFile1.txt' which has no version. No entry for this file will be placed in the MsiFileHash table. For unversioned files, specifying a version that is different from the actual file may result in unexpected versioning behavior during a repair or while patching. Version the resource to eliminate this warning.", WixMessage.MessageTypeEnum.Warning));
            light.Run();

            // verify unversioned file is not added to the msifilehash table
            Verifier.VerifyNotTableExists(light.OutputFile, "MsiFileHash");
        }
    }
}
