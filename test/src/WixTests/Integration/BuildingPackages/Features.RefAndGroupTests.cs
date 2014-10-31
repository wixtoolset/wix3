//-----------------------------------------------------------------------
// <copyright file="Features.RefAndGroupTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for FeatureGroups and FeatureRefs
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Features
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for FeatureGroups and FeatureRefs
    /// </summary>
    public class RefAndGroupTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Features\RefAndGroupTests");

        [NamedFact]
        [Description("Verify that features can be referenced")]
        [Priority(1)]
        public void FeatureRefs()
        {
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"FeatureRefs\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature1'";
            Verifier.VerifyQuery(msi, query, "Feature1");
        }

        [NamedFact]
        [Description("Verify that feature group can be created in Fragments/FeatureRefs and referenced")]
        [Priority(1)]
        public void FeatureGroups()
        {
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"FeatureGroups\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature2'";
            string query3 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature3'";
            Verifier.VerifyQuery(msi, query1, "Feature1");
            Verifier.VerifyQuery(msi, query2, "Feature2");
            Verifier.VerifyQuery(msi, query3, "Feature3");
        }

        [NamedFact]
        [Description("Verify that features can be nested")]
        [Priority(1)]
        public void NestedFeatures()
        {
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"NestedFeatures\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Feature_Parent` FROM `Feature` WHERE `Feature` = 'Feature2'";
            Verifier.VerifyQuery(msi, query1, "Feature1");
            Verifier.VerifyQuery(msi, query2, "Feature1");
        }

        [NamedFact]
        [Description("Verify that feature groups can be nested and referenced")]
        [Priority(1)]
        public void NestedFeatureGroups()
        {
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"NestedFeatureGroups\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature2'";
            Verifier.VerifyQuery(msi, query1, "Feature1");
            Verifier.VerifyQuery(msi, query2, "Feature2");
        }

        [NamedFact]
        [Description("Verify that the Product element can contain Features, FeatureGroups, FeatureRefs and FeatureGroupRefs")]
        [Priority(1)]
        public void ComplexFeatureUsage()
        {
            //product can't contain FeatureGroup,removed it;
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"ComplexFeatureUsage\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature1'";
            string query2 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature2'";
            string query3 = "SELECT `Feature` FROM `Feature` WHERE `Feature` = 'Feature3'";
            Verifier.VerifyQuery(msi, query1, "Feature1");
            Verifier.VerifyQuery(msi, query2, "Feature2");
            Verifier.VerifyQuery(msi, query3, "Feature3");
       }

        [NamedFact]
        [Description("Verify that Merge Module References are handled correctly within FeatureGroups")]
        [Priority(2)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/download.php?group_id=105970&atid=642714&file_id=238466&aid=1760155")]
        public void FeatureGroupContainingMergeRef()
        {
            string msi = Builder.BuildPackage(Path.Combine(RefAndGroupTests.TestDataDirectory, @"FeatureGroupContainingMergeRef\Product.wxs"));

            // verify only one row is added for the merge module and it has the correct value
            string query = "SELECT `Component_` FROM `FeatureComponents`";
            Verifier.VerifyQuery(msi, query, "ModuleComponent.D75D42C7_6B72_46FE_8EB1_83D02B9341D2");
        }
    }
}
