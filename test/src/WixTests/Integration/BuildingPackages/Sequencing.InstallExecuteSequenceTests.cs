// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Sequencing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixTest;

    /// <summary>
    /// Tests for the InstallExecuteSequence table
    /// </summary>
    public class InstallExecuteSequenceTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Sequencing\InstallExecuteSequenceTests");

        [NamedFact]
        [Description("Verify that all of the standard actions can be added to the InstallExecuteSequence table")]
        [Priority(1)]
        public void AllStandardActions()
        {
            string msi = Builder.BuildPackage(Path.Combine(InstallExecuteSequenceTests.TestDataDirectory, @"AllStandardActions\product.wxs"));

            string expectedMsi = Path.Combine(InstallExecuteSequenceTests.TestDataDirectory, @"AllStandardActions\expected.msi");
            Verifier.VerifyResults(expectedMsi, msi, "InstallExecuteSequence");
        }
    }
}
