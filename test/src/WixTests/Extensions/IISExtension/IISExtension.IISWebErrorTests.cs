// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers.Extensions;


    /// <summary>
    /// IIS extension WebError element tests
    /// </summary>
    public class IISWebErrorTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebErrorTests");

        [NamedFact]
        [Description("Verify the msi table data in CustomAction table and WebError table.")]
        [Priority(1)]
        public void IISWebError_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebErrorTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsWebError,
                new TableRow(IIsWebErrorColumns.ErrorCode.ToString(), "400", false),
                new TableRow(IIsWebErrorColumns.SubCode.ToString(), "0", false),
                new TableRow(IIsWebErrorColumns.ParentType.ToString(), "2", false),
                new TableRow(IIsWebErrorColumns.ParentValue.ToString(), "Test"),
                new TableRow(IIsWebErrorColumns.File.ToString(), "[#Error404]"),
                new TableRow(IIsWebErrorColumns.URL.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.IIsWebError,
               new TableRow(IIsWebErrorColumns.ErrorCode.ToString(), "401", false),
               new TableRow(IIsWebErrorColumns.SubCode.ToString(), "1", false),
               new TableRow(IIsWebErrorColumns.ParentType.ToString(), "2", false),
               new TableRow(IIsWebErrorColumns.ParentValue.ToString(), "Test"),
               new TableRow(IIsWebErrorColumns.File.ToString(), "[#Error404]"),
               new TableRow(IIsWebErrorColumns.URL.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.IIsWebError,
               new TableRow(IIsWebErrorColumns.ErrorCode.ToString(), "401", false),
               new TableRow(IIsWebErrorColumns.SubCode.ToString(), "7", false),
               new TableRow(IIsWebErrorColumns.ParentType.ToString(), "2", false),
               new TableRow(IIsWebErrorColumns.ParentValue.ToString(), "Test"),
               new TableRow(IIsWebErrorColumns.File.ToString(), "[#Error404]"),
               new TableRow(IIsWebErrorColumns.URL.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.IIsWebError,
               new TableRow(IIsWebErrorColumns.ErrorCode.ToString(), "401", false),
               new TableRow(IIsWebErrorColumns.SubCode.ToString(), "61", false),
               new TableRow(IIsWebErrorColumns.ParentType.ToString(), "2", false),
               new TableRow(IIsWebErrorColumns.ParentValue.ToString(), "Test"),
               new TableRow(IIsWebErrorColumns.File.ToString(), "[#Error404]"),
               new TableRow(IIsWebErrorColumns.URL.ToString(), string.Empty));

        }
    }
}
