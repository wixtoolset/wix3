//-----------------------------------------------------------------------
// <copyright file="Components.SFPCatalogTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for configuring the SFPCatalog table
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Tests for configuring the SFPCatalog table
    /// </summary>
    [TestClass]
    public class SFPCatalogTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\SFPCatalogTests");

        [TestMethod]
        [Description("Verify that the SFP Catalog table can be configured")]
        [Priority(1)]
        public void SFPCatalog()
        {
            string sourceFile = Path.Combine(SFPCatalogTests.TestDataDirectory, @"SFPCatalog\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `SFPCatalog` FROM `SFPCatalog` WHERE `SFPCatalog` = 'Test1'";
            Verifier.VerifyQuery(msi, query, "Test1");
        }

        [TestMethod]
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
