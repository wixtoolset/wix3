// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for configuring the SFPCatalog table
    /// </summary>
    public class SFPCatalogTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\SFPCatalogTests");

        [NamedFact]
        [Description("Verify that the SFP Catalog table can be configured")]
        [Priority(1)]
        public void SFPCatalog()
        {
            string sourceFile = Path.Combine(SFPCatalogTests.TestDataDirectory, @"SFPCatalog\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `SFPCatalog` FROM `SFPCatalog` WHERE `SFPCatalog` = 'Test1'";
            Verifier.VerifyQuery(msi, query, "Test1");
        }

        [NamedFact]
        [Description("Verify that an SFP File can be specified")]
        [Priority(1)]
        public void SFPFile()
        {
            string sourceFile = Path.Combine(SFPCatalogTests.TestDataDirectory, @"SFPFile\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `File_` FROM `FileSFPCatalog` WHERE `File_` = 'SFPFile'";
            Verifier.VerifyQuery(msi, query, "SFPFile");
        }
    }
}
