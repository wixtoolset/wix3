//-----------------------------------------------------------------------
// <copyright file="UtilExtension.WixShellExecBinaryTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension WixShellExecBinary tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
   
    /// <summary>
    /// Util extension WixShellExecBinary element tests
    /// </summary>
    [TestClass]
    public class WixShellExecBinaryTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\WixShellExecBinaryTests");
     
        [TestMethod]
        [Description("Verify that WixShellExecBinary executes the expected command.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void WixShellExecBinary_Install()
        {
            string sourceFile = Path.Combine(WixShellExecBinaryTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            string fileName = Environment.ExpandEnvironmentVariables(@"%TEMP%\DummyFile.txt");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.IsTrue(File.Exists(fileName) , "Command was not executed. File '{0}' does not exist.", fileName);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
