//-----------------------------------------------------------------------
// <copyright file="Sequencing.SequencingTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     General tests for sequencing
// </summary>
//-----------------------------------------------------------------------

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
