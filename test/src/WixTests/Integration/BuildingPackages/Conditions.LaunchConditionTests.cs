// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Conditions
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for defining launch conditions
    /// </summary>
    public class LaunchConditionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Conditions\LaunchConditionTests");

        [NamedFact]
        [Description("Verify that a launch condition can be specified")]
        [Priority(1)]
        public void SimpleCondition()
        {
            string sourceFile = Path.Combine(LaunchConditionTests.TestDataDirectory, @"SimpleCondition\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Sequence` FROM `InstallUISequence` WHERE `Action` = 'AppSearch'";
            Verifier.VerifyQuery(msi, query1, "50");
        }
    }
}
