// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Regression tests for Candle/Light Integration
    /// </summary>
    public class RegressionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\RegressionTests");

        [NamedFact]
        [Description("Verify that a Directory inherits its parent directory's Name, not the ShortName")]
        [Trait("Bug Link", "https://sourceforge.net/tracker/index.php?func=detail&aid=1824809&group_id=105970&atid=642714")]
        [Priority(3)]
        public void SF1824809()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory, @"SF1824809\product.wxs"));
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.Run();
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Builds an MSI with a file search. This is not currently a test because there is an open spec issue.")]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1656236&group_id=105970&atid=642714")]
        [Priority(3)]
        public void SF1656236()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory, @"SF1656236\product.wxs"));
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.Run();

            // Verify that the DrLocator table was generated correctly
        }

        [NamedFact]
        [Description("Verify that a Duplicate authoring of IgnoreModularization element do not cause a build failure.")]
        [Priority(3)]
        public void DuplicateIgnoreModularization()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory, @"DuplicateIgnoreModularization\product.wxs"));
            // supress deprecated element warning
            candle.SuppressWarnings.Add(1085);
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.Run();
        }
    }
}
