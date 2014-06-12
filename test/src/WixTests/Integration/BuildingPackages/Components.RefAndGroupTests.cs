//-----------------------------------------------------------------------
// <copyright file="Components.RefAndGroupTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for ComponentRefs and ComponentGroups
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for ComponentRefs and ComponentGroups
    /// </summary>
    public class RefAndGroupTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\RefAndGroupTests");

        [NamedFact]
        [Description("Verify that Components can be referenced")]
        [Priority(1)]
        public void ComponentRef()
        {
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"ComponentRef\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Component` FROM `Component` WHERE `Component` = 'Component1'";
            Verifier.VerifyQuery(msi, query, "Component1");
        }

        [NamedFact]
        [Description("Verify that a ComponentGroups with child Components and ComponentRefs can be created and referenced")]
        [Priority(1)]
        public void ComponentGroups()
        {
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"ComponentGroups\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Component` FROM `Component` WHERE `Component` = 'Component1'";
            string query1 = "SELECT `Component` FROM `Component` WHERE `Component` = 'Component2'";
            Verifier.VerifyQuery(msi, query, "Component1");
            Verifier.VerifyQuery(msi, query1, "Component2");
        }

        [NamedFact]
        [Description("Verify that a ComponentGroups can be nested using the child element ComponentGroupRef")]
        [Priority(1)]
        public void NestedComponentGroups()
        {
            string sourceFile = Path.Combine(RefAndGroupTests.TestDataDirectory, @"NestedComponentGroups\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Component` FROM `Component` WHERE `Component` = 'Component1'";
            string query1 = "SELECT `Component` FROM `Component` WHERE `Component` = 'Component2'";
            Verifier.VerifyQuery(msi, query, "Component1");
            Verifier.VerifyQuery(msi, query1, "Component2");
        }
    }
}