//-----------------------------------------------------------------------
// <copyright file="Authoring.FragmentTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for a Fragment
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Authoring
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for a Fragment
    /// </summary>
    [TestClass]
    public class FragmentTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Authoring\FragmentTests");

        [TestMethod]
        [Description("Verify that multiple fragments can be defined and referenced indirectly and that multiple levels of referencing is supported")]
        [Priority(1)]
        public void MultipleFragments()
        {
            string sourceFile = Path.Combine(FragmentTests.TestDataDirectory, @"MultipleFragments\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            Verifier.VerifyResults(Path.Combine(FragmentTests.TestDataDirectory, @"MultipleFragments\expected.msi"), msi, "Feature", "Component", "FeatureComponents");
        }

        [TestMethod]
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
