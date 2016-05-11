// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using Xunit;

    /// <summary>
    /// Util extension ServiceConfig element tests
    /// </summary>
    public class ServiceConfigTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\ServiceConfigTests");

        [NamedFact]
        [Description("Verify that the (ServiceConfig and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void ServiceConfig_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(ServiceConfigTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("SchedServiceConfig", 1, "WixCA", "SchedServiceConfig"),
                new CustomActionTableData("ExecServiceConfig", 3073, "WixCA", "ExecServiceConfig"),
                new CustomActionTableData("RollbackServiceConfig", 3329, "WixCA", "RollbackServiceConfig"));

            // Verify ServiceConfig table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.ServiceConfig,
                new TableRow(ServiceConfigColumns.ServiceName.ToString(), "W32Time"),
                new TableRow(ServiceConfigColumns.Component_.ToString(), "Component1"),
                new TableRow(ServiceConfigColumns.NewService.ToString(), "0", false),
                new TableRow(ServiceConfigColumns.FirstFailureActionType.ToString(), "restart"),
                new TableRow(ServiceConfigColumns.SecondFailureActionType.ToString(), "reboot"),
                new TableRow(ServiceConfigColumns.ThirdFailureActionType.ToString(), "none"),
                new TableRow(ServiceConfigColumns.ResetPeriodInDays.ToString(), "1", false),
                new TableRow(ServiceConfigColumns.RestartServiceDelayInSeconds.ToString(), string.Empty, false),
                new TableRow(ServiceConfigColumns.ProgramCommandLine.ToString(), string.Empty),
                new TableRow(ServiceConfigColumns.RebootMessage.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.ServiceConfig,
                new TableRow(ServiceConfigColumns.ServiceName.ToString(), "MynewService"),
                new TableRow(ServiceConfigColumns.Component_.ToString(), "Component2"),
                new TableRow(ServiceConfigColumns.NewService.ToString(), "1", false),
                new TableRow(ServiceConfigColumns.FirstFailureActionType.ToString(), "reboot"),
                new TableRow(ServiceConfigColumns.SecondFailureActionType.ToString(), "restart"),
                new TableRow(ServiceConfigColumns.ThirdFailureActionType.ToString(), "none"),
                new TableRow(ServiceConfigColumns.ResetPeriodInDays.ToString(), "3", false),
                new TableRow(ServiceConfigColumns.RestartServiceDelayInSeconds.ToString(), string.Empty, false),
                new TableRow(ServiceConfigColumns.ProgramCommandLine.ToString(), string.Empty),
                new TableRow(ServiceConfigColumns.RebootMessage.ToString(), string.Empty));
        }

        [NamedFact]
        [Description("Verify that the Services are being installed and configured as expected.")]
        [Priority(2)]
        [RuntimeTest]
        public void ServiceConfig_Install()
        {
            string sourceFile = Path.Combine(ServiceConfigTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate Existing Service Information.
            ServiceFailureActionType[] expectedFailureActions = new ServiceFailureActionType[] { ServiceFailureActionType.RestartService, ServiceFailureActionType.RebootComputer, ServiceFailureActionType.None };
            ServiceVerifier.VerifyServiceInformation("W32Time", 1, expectedFailureActions);

            // Validate New Service Information.
            expectedFailureActions = new ServiceFailureActionType[] { ServiceFailureActionType.RebootComputer, ServiceFailureActionType.RestartService, ServiceFailureActionType.None };
            ServiceVerifier.VerifyServiceInformation("MynewService", 3, expectedFailureActions);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New Service Does NOT exist any more.
            Assert.False(ServiceVerifier.ServiceExists("MynewService"), String.Format("Service '{0}' was NOT removed on Uninstall.", "MynewService"));
        }

        [NamedFact]
        [Description("Verify that the Services are is repaired as expected.")]
        [Priority(2)]
        [RuntimeTest]
        public void ServiceConfig_Repair()
        {
            string sourceFile = Path.Combine(ServiceConfigTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Change the service details
            ServiceFailureActionType[] expectedFailureActions = new ServiceFailureActionType[] { ServiceFailureActionType.RestartService, ServiceFailureActionType.RestartService, ServiceFailureActionType.RestartService };
            ServiceVerifier.SetServiceInformation("MynewService", 4, expectedFailureActions);

            MSIExec.RepairProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate Existing Service Information.
            expectedFailureActions = new ServiceFailureActionType[] { ServiceFailureActionType.RestartService, ServiceFailureActionType.RebootComputer, ServiceFailureActionType.None };
            ServiceVerifier.VerifyServiceInformation("W32Time", 1, expectedFailureActions);

            // Validate New Service Information.
            expectedFailureActions = new ServiceFailureActionType[] { ServiceFailureActionType.RebootComputer, ServiceFailureActionType.RestartService, ServiceFailureActionType.None };
            ServiceVerifier.VerifyServiceInformation("MynewService", 3, expectedFailureActions);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New Service Does NOT exist any more.
            Assert.False(ServiceVerifier.ServiceExists("MynewService"), String.Format("Service '{0}' was NOT removed on Uninstall.", "MynewService"));
        }

        [NamedFact]
        [Description("Verify that the Installation fails if ServiceConfig references a non-existing service.")]
        [Priority(2)]
        [RuntimeTest]
        public void ServiceConfig_NonExistingService()
        {
            string sourceFile = Path.Combine(ServiceConfigTests.TestDataDirectory, @"NonExistingService.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            Assert.False(ServiceVerifier.ServiceExists("NonExistingService"), String.Format("Service '{0}' was created on Rollback.", "NonExistingService"));
        }
    }
}
