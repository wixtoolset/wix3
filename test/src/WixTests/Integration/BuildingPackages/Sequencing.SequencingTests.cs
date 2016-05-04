// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Sequencing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// General tests for sequencing
    /// </summary>
    public class SequencingTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Sequencing\SequencingTests");

        [NamedFact]
        [Description("Verify that a custom action can be sequenced")]
        [Priority(1)]
        public void SimpleSequencing()
        {
            string msi = Builder.BuildPackage(Path.Combine(SequencingTests.TestDataDirectory, @"SimpleSequencing\product.wxs"));
            string expectedMsi = Path.Combine(SequencingTests.TestDataDirectory, @"SimpleSequencing\expected.msi");
            Verifier.VerifyResults(expectedMsi, msi, "AdminExecuteSequence", "InstallExecuteSequence");
        }
    }
}
