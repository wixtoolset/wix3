//-----------------------------------------------------------------------
// <copyright file="IISExtension.IISWebServiceExtensionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>IIS Extension IISWebServiceExtension tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers.Extensions;

    /// <summary>
    /// IIS extension IISWebServiceExtension element tests
    /// </summary>
    [TestClass]
    public class IISWebServiceExtensionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebServiceExtensionTests");

        [TestMethod]
        [Description("Verify that the (IIsWebServiceExtension,CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void IISWebServiceExtension_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebServiceExtensionTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsWebServiceExtension,
                new TableRow(IIsWebServiceExtensionColumns.WebServiceExtension.ToString(), "extension1"),
                new TableRow(IIsWebServiceExtensionColumns.Component_.ToString(), "TestWebSvcExtProductComponent1"),
                new TableRow(IIsWebServiceExtensionColumns.File.ToString(), "[!TestFile1]"),
                new TableRow(IIsWebServiceExtensionColumns.Description.ToString(), "WiX Test Extension1"),
                new TableRow(IIsWebServiceExtensionColumns.Group.ToString(), "WiXTest"),
                new TableRow(IIsWebServiceExtensionColumns.Attributes.ToString(), "3", false));

            Verifier.VerifyTableData(msiFile, MSITables.IIsWebServiceExtension,
               new TableRow(IIsWebServiceExtensionColumns.WebServiceExtension.ToString(), "extension2"),
               new TableRow(IIsWebServiceExtensionColumns.Component_.ToString(), "TestWebSvcExtProductComponent2"),
               new TableRow(IIsWebServiceExtensionColumns.File.ToString(), "[!TestFile2]"),
               new TableRow(IIsWebServiceExtensionColumns.Description.ToString(), "WiX Test Extension2"),
               new TableRow(IIsWebServiceExtensionColumns.Group.ToString(), "WiXTest"),
               new TableRow(IIsWebServiceExtensionColumns.Attributes.ToString(), "2", false));

        }

        [TestMethod]
        [Description("Install the MSI.Verify that “TestFilter” Was added for website 'Test'.Verify that 'Global Filter' was added as a global filter.Uninstall the MSi Verify that filters are removed")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISWebServiceExtension_Install()
        {
            string sourceFile = Path.Combine(IISWebServiceExtensionTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that Extension “extension1” is present in the WebSvcExtRestrictionList and is enabled
            Assert.IsTrue(IISVerifier.WebServiceExtensionExists("WiX Test Extension1"), "WebServiceExtension '{0}' was not created on Install", "WiX Test Extension1");
            Assert.IsTrue(IISVerifier.WebServiceExtensionEnabled("WiX Test Extension1") == true, "WebServiceExtension '{0}' was not Enabled on Install", "WiX Test Extension1");
            
            // Verify that Extension “extension2” is present in the WebSvcExtRestrictionList and is disabled
            Assert.IsTrue(IISVerifier.WebServiceExtensionExists("WiX Test Extension2"), "WebServiceExtension '{0}' was not created on Install", "WiX Test Extension2");
            Assert.IsTrue(IISVerifier.WebServiceExtensionEnabled("WiX Test Extension2") == false, "WebServiceExtension '{0}' was not Disabled on Install", "WiX Test Extension2");

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that “WiX Test Extension1” and “WiX Test Extension2” were removed 
            Assert.IsFalse(IISVerifier.WebServiceExtensionExists("WiX Test Extension1"), "WebServiceExtension '{0}' was not removed on Uninstall", "WiX Test Extension1");
            Assert.IsFalse(IISVerifier.WebServiceExtensionExists("WiX Test Extension2"), "WebServiceExtension '{0}' was not removed on Uninstall", "WiX Test Extension2");
        }
    }
}
