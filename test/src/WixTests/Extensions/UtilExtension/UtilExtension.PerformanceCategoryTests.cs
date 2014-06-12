//-----------------------------------------------------------------------
// <copyright file="UtilExtension.PerformanceCategoryTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension PerformanceCategory tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using Microsoft.Win32;
    using Xunit;

    /// <summary>
    /// Util extension PerformanceCategory element tests
    /// </summary>
    public class PerformanceCategoryTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\PerformanceCategoryTests");

        [NamedFact]
        [Description("Verify that the (PerformanceCategory and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void PerformanceCategory_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(PerformanceCategoryTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("InstallPerfCounterData", 1, "ScaSchedule", "InstallPerfCounterData"),
                new CustomActionTableData("UninstallPerfCounterData", 1, "ScaSchedule", "UninstallPerfCounterData"),
                new CustomActionTableData("RegisterPerfCounterData", 11265, "ScaExecute", "RegisterPerfCounterData"),
                new CustomActionTableData("UnregisterPerfCounterData", 11265, "ScaExecute", "UnregisterPerfCounterData"),
                new CustomActionTableData("RollbackRegisterPerfCounterData", 11521, "ScaExecute", "UnregisterPerfCounterData"),
                new CustomActionTableData("RollbackUnregisterPerfCounterData", 11521, "ScaExecute", "RegisterPerfCounterData")
                );

            string initData = "[info]\r\ndrivername=InstrumentationDemo\r\nsymbolfile=wixperf.h\r\n\r\n[objects]\r\nOBJECT_1_009_NAME=\r\n\r\n[languages]\r\n009=LANG009\r\n\r\n[text]\r\nOBJECT_1_009_NAME=InstrumentationDemo\r\nOBJECT_1_009_HELP=Instrumentation Demo Counters\r\nDEVICE_COUNTER_1_009_NAME=DemoCounter\r\nDEVICE_COUNTER_1_009_HELP=Just a simple numerical count.\r\nDEVICE_COUNTER_2_054_NAME=DemoCounter1\r\nDEVICE_COUNTER_2_054_HELP=Just a simple numerical count.\r\nDEVICE_COUNTER_3_028_NAME=DemoCounter2\r\nDEVICE_COUNTER_3_028_HELP=Just a simple numerical count.\r\nDEVICE_COUNTER_4_006_NAME=DemoCounter3\r\nDEVICE_COUNTER_4_006_HELP=Just a simple numerical count.\r\n";
            string constantData = "#define OBJECT_1    0\r\n#define DEVICE_COUNTER_1    2\r\n#define DEVICE_COUNTER_2    4\r\n#define DEVICE_COUNTER_3    6\r\n#define DEVICE_COUNTER_4    8\r\n#define LAST_OBJECT_1_COUNTER_OFFSET    8\r\n";
            Verifier.VerifyTableData(msiFile, MSITables.PerformanceCategory,
                new TableRow(PerformanceCategoryColumns.PerformanceCategory.ToString(), "InstrumentationDemo"),
                new TableRow(PerformanceCategoryColumns.Component_.ToString(), "Component1"),
                new TableRow(PerformanceCategoryColumns.Name.ToString(), "InstrumentationDemo"),
                new TableRow(PerformanceCategoryColumns.IniData.ToString(), initData),
                new TableRow(PerformanceCategoryColumns.ConstantData.ToString(), constantData));
        }

        [NamedFact]
        [Description("Verify that the Performance counter is created upon install.")]
        [Priority(2)]
        [RuntimeTest]
        public void PerformanceCategory_Install()
        {
            string sourceFile = Path.Combine(PerformanceCategoryTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            string registryKey = @"system\currentcontrolset\services\InstrumentationDemo";
            Assert.True(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not created on install.", registryKey));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.False(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not removed on uninstall.", registryKey));
        }

        [NamedFact]
        [Description("Verify that the Performance counter is created upon install to a 64bit-specific folder.")]
        [Priority(2)]
        [RuntimeTest]
        [Is64BitSpecificTest]
        public void PerformanceCategory_Install_64bit()
        {
            string sourceFile = Path.Combine(PerformanceCategoryTests.TestDataDirectory, @"product_64.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            string registryKey = @"system\currentcontrolset\services\InstrumentationDemo";
            Assert.True(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not created on install.", registryKey));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.False(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not removed on uninstall.", registryKey));
        }

        [NamedFact]
        [Description("Verify that the Performance counter was removed upon rollback.")]
        [Priority(2)]
        [RuntimeTest]
        public void PerformanceCategory_InstallFailure()
        {
            string sourceFile = Path.Combine(PerformanceCategoryTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            string registryKey = @"system\currentcontrolset\services\InstrumentationDemo";
            
            // Verify that the file was not created
            Assert.False(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not removed on Rollback.", registryKey));
        }
    }
}
