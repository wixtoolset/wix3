// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.CustomActions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for editing registry keys
    /// </summary>
    public class RegistryTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Registry\RegistryTests");

        [NamedFact]
        [Description("Verify that registry keys can be added")]
        [Priority(1)]
        public void SimpleRegistry()
        {
            string msi = Builder.BuildPackage(Path.Combine(RegistryTests.TestDataDirectory, @"SimpleRegistry\product.wxs"));
            Verifier.VerifyResults(Path.Combine(RegistryTests.TestDataDirectory, @"SimpleRegistry\expected.msi"), msi, "Registry");
        }
    }
}
