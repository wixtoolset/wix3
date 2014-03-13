//-----------------------------------------------------------------------
// <copyright file="IISExtension.IISWebPropertyTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>IIS Extension WebProperty tests</summary>
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
    /// IIS extension WebProperty element tests
    /// </summary>
    [TestClass]
    public class IISWebPropertyTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebPropertyTests");

        [TestMethod]
        [Description("Verify that the (IIsProperty,CustomAction) Tables are created in the MSI and have defined data.")]
        [Priority(1)]
        public void IISWebPropertyEtag_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_etag.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsProperty,
                new TableRow(IIsPropertyColumns.Property.ToString(), "ETagChangeNumber"),
                new TableRow(IIsPropertyColumns.Component_.ToString(), "TestWebPropertyComponent"),
                new TableRow(IIsPropertyColumns.Attributes.ToString(), "0", false),
                new TableRow(IIsPropertyColumns.Value.ToString(), "1234"));
        }

        [TestMethod]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        [Ignore] // ETag property is not exposed by IIS6; the test case will fail to query the value of the property. Should be enabled when moving to IIS7
        public void IISWebPropertyEtag_Install()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_etag.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // get original value for the property
            int originalEtagPropertyValue;
            if (!int.TryParse(IISVerifier.GetMetaBasePropertyValue("MD_ETAG_CHANGENUMBER").ToString(), out originalEtagPropertyValue))
            {
                originalEtagPropertyValue = -1;  // default is null
            }

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Etag property was set correctelly on install
            int acctualEtagPropertyValue = (int)IISVerifier.GetMetaBasePropertyValue("MD_ETAG_CHANGENUMBER");
            int expectedEtagPropertyValue = 1234;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "Etag Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            if (!int.TryParse(IISVerifier.GetMetaBasePropertyValue("MD_ETAG_CHANGENUMBER").ToString(), out acctualEtagPropertyValue))
            {
                acctualEtagPropertyValue = -1;  // default
            }
            expectedEtagPropertyValue = originalEtagPropertyValue;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "Etag Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);
        }

        [TestMethod]
        [Description("Verify that the (IIsProperty,CustomAction) Tables are created in the MSI and have defined data.")]
        [Priority(1)]
        public void IISWebPropertyIIS5IsoationMode_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_IIS5IsolationMode.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsProperty,
                new TableRow(IIsPropertyColumns.Property.ToString(), "IIs5IsolationMode"),
                new TableRow(IIsPropertyColumns.Component_.ToString(), "TestWebPropertyComponent"),
                new TableRow(IIsPropertyColumns.Attributes.ToString(), "0", false),
                new TableRow(IIsPropertyColumns.Value.ToString(), string.Empty));
        }

        [TestMethod]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISWebPropertyIIS5IsoationMode_Install()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_IIS5IsolationMode.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // get original value for the property
            bool originalEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("IIs5IsolationModeEnabled");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Etag property was set correctelly on install
            bool acctualEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("IIs5IsolationModeEnabled");
            bool expectedEtagPropertyValue = true;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "IIs5IsolationModeEnabled Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            acctualEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("IIs5IsolationModeEnabled");
            expectedEtagPropertyValue = originalEtagPropertyValue;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "IIs5IsolationModeEnabled Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);
        }

        [TestMethod]
        [Description("Verify that the (IIsProperty,CustomAction) Tables are created in the MSI and have defined data.")]
        [Priority(1)]
        public void IISWebPropertyMaxGlobalBandwidth_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_MaxGlobalBandwidth.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsProperty,
                new TableRow(IIsPropertyColumns.Property.ToString(), "MaxGlobalBandwidth"),
                new TableRow(IIsPropertyColumns.Component_.ToString(), "TestWebPropertyComponent"),
                new TableRow(IIsPropertyColumns.Attributes.ToString(), "0", false),
                new TableRow(IIsPropertyColumns.Value.ToString(), "7340032"));
        }

        [TestMethod]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISWebPropertyMaxGlobalBandwidth_Install()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_MaxGlobalBandwidth.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // get original value for the property
            int originalEtagPropertyValue;
            if (! int.TryParse(IISVerifier.GetMetaBasePropertyValue("MaxGlobalBandwidth").ToString(),out originalEtagPropertyValue))
            {
                originalEtagPropertyValue = -1;  // default
            }

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Etag property was set correctelly on install
            int acctualEtagPropertyValue = (int)IISVerifier.GetMetaBasePropertyValue("MaxGlobalBandwidth");
            int expectedEtagPropertyValue = -1073741824;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "MaxGlobalBandwidth Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            if (! int.TryParse(IISVerifier.GetMetaBasePropertyValue("MaxGlobalBandwidth").ToString(),out acctualEtagPropertyValue))
            {
                acctualEtagPropertyValue = -1;  // default
            }
            expectedEtagPropertyValue = (int)originalEtagPropertyValue;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "MaxGlobalBandwidth Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);
        }

        [TestMethod]
        [Description("Verify that the (IIsProperty,CustomAction) Tables are created in the MSI and have defined data.")]
        [Priority(1)]
        public void IISWebPropertyLogInUTF8_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_LogInUTF8.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsProperty,
                new TableRow(IIsPropertyColumns.Property.ToString(), "LogInUTF8"),
                new TableRow(IIsPropertyColumns.Component_.ToString(), "TestWebPropertyComponent"),
                new TableRow(IIsPropertyColumns.Attributes.ToString(), "0", false),
                new TableRow(IIsPropertyColumns.Value.ToString(), string.Empty));
        }

        [TestMethod]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISWebPropertyLogInUTF8_Install()
        {
            string sourceFile = Path.Combine(IISWebPropertyTests.TestDataDirectory, @"product_LogInUTF8.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // get original value for the property
            bool originalEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("LogInUTF8");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Etag property was set correctelly on install
            bool acctualEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("LogInUTF8");
            bool expectedEtagPropertyValue = true;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "LogInUTF8 Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            acctualEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("LogInUTF8");
            expectedEtagPropertyValue = originalEtagPropertyValue;
            Assert.IsTrue(acctualEtagPropertyValue == expectedEtagPropertyValue, "LogInUTF8 Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue);
        }
   }
}
