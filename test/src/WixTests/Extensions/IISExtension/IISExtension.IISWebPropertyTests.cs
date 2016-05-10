// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers.Extensions;
    using Xunit;

    /// <summary>
    /// IIS extension WebProperty element tests
    /// </summary>
    public class IISWebPropertyTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebPropertyTests");

        [NamedFact]
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

        [NamedFact(Skip="Ignore")]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [RuntimeTest]
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
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("Etag Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            if (!int.TryParse(IISVerifier.GetMetaBasePropertyValue("MD_ETAG_CHANGENUMBER").ToString(), out acctualEtagPropertyValue))
            {
                acctualEtagPropertyValue = -1;  // default
            }
            expectedEtagPropertyValue = originalEtagPropertyValue;
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("Etag Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));
        }

        [NamedFact]
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

        [NamedFact]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [RuntimeTest]
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
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("IIs5IsolationModeEnabled Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            acctualEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("IIs5IsolationModeEnabled");
            expectedEtagPropertyValue = originalEtagPropertyValue;
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("IIs5IsolationModeEnabled Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));
        }

        [NamedFact]
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

        [NamedFact]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [RuntimeTest]
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
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("MaxGlobalBandwidth Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            if (! int.TryParse(IISVerifier.GetMetaBasePropertyValue("MaxGlobalBandwidth").ToString(),out acctualEtagPropertyValue))
            {
                acctualEtagPropertyValue = -1;  // default
            }
            expectedEtagPropertyValue = (int)originalEtagPropertyValue;
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("MaxGlobalBandwidth Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));
        }

        [NamedFact]
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

        [NamedFact]
        [Description("Install the MSI. Verify that the webProperty was set.Uninstall MSI . Verify that the webProperty was set back correctelly.")]
        [Priority(2)]
        [RuntimeTest]
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
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("LogInUTF8 Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            acctualEtagPropertyValue = (bool)IISVerifier.GetMetaBasePropertyValue("LogInUTF8");
            expectedEtagPropertyValue = originalEtagPropertyValue;
            Assert.True(acctualEtagPropertyValue == expectedEtagPropertyValue, String.Format("LogInUTF8 Property value does not meat expected. Acctual: '{0}'. Expected: '{1}'.", acctualEtagPropertyValue, expectedEtagPropertyValue));
        }
   }
}
