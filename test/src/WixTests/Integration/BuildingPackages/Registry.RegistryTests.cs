//-----------------------------------------------------------------------
// <copyright file="Registry.RegistryTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for editing registry keys
// </summary>
//-----------------------------------------------------------------------

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
