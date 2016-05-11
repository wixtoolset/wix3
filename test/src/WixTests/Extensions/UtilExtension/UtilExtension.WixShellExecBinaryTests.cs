// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Util extension WixShellExecBinary element tests
    /// </summary>
    public class WixShellExecBinaryTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\WixShellExecBinaryTests");
     
        [NamedFact]
        [Description("Verify that WixShellExecBinary executes the expected command.")]
        [Priority(2)]
        [RuntimeTest]
        public void WixShellExecBinary_Install()
        {
            string sourceFile = Path.Combine(WixShellExecBinaryTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            string fileName = Environment.ExpandEnvironmentVariables(@"%TEMP%\DummyFile.txt");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(File.Exists(fileName) , String.Format("Command was not executed. File '{0}' does not exist.", fileName));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
