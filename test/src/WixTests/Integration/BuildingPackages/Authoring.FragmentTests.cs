// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Authoring
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for a Fragment
    /// </summary>
    public class FragmentTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Authoring\FragmentTests");

        [NamedFact]
        [Description("Verify that multiple fragments can be defined and referenced indirectly and that multiple levels of referencing is supported")]
        [Priority(1)]
        public void MultipleFragments()
        {
            string sourceFile = Path.Combine(FragmentTests.TestDataDirectory, @"MultipleFragments\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            Verifier.VerifyResults(Path.Combine(FragmentTests.TestDataDirectory, @"MultipleFragments\expected.msi"), msi, "Feature", "Component", "FeatureComponents");
        }

        [NamedFact]
        [Description("Verify that there is an error if FragmentRef is used")]
        [Priority(3)]
        public void FragmentRef()
        {
            string sourceFile = Path.Combine(FragmentTests.TestDataDirectory, @"FragmentRef\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 5;
            candle.ExpectedWixMessages.Add(new WixMessage(5, "The Product element contains an unexpected child element 'FragmentRef'.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }
    }
}
