// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
