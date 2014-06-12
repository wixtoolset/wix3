//-----------------------------------------------------------------------
// <copyright file="SymbolPaths.SymbolPathTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for SymbolPath elements.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.SymbolPaths
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for SymbolPath elements.
    /// </summary>
    public class SymbolPathTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\SymbolPaths\SymbolPathTests");

        [NamedFact]
        [Description("Verify that a SymbolPath element can exist under a component")]
        [Priority(2)]
        public void ComponentSymbolPath()
        {
            string sourceFile = Path.Combine(SymbolPathTests.TestDataDirectory, @"ComponentSymbolPath\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            Verifier.VerifyResults(Path.Combine(SymbolPathTests.TestDataDirectory, @"ComponentSymbolPath\expected.msi"), msi);
        }
    }
}