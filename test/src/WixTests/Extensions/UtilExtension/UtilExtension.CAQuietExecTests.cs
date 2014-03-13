//-----------------------------------------------------------------------
// <copyright file="UtilExtension.CAQuietExecTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension CAQuietExec tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
   
    /// <summary>
    /// Util extension CAQuietExec element tests
    /// </summary>
    [TestClass]
    public class CAQuietExecTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\CAQuietExecTests");
     
        [TestMethod]
        [Description("Verify that CAQuietExec executes the expected command.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void CAQuietExec_Install()
        {
            string sourceFile = Path.Combine(CAQuietExecTests.TestDataDirectory, @"product.wxs");
           
            string immediateFileName = Path.Combine(Path.GetTempPath(), "ImmediateCommand.cmd");
            string immediateOutputFileName = Path.Combine(Path.GetTempPath(), "immediate.txt");
          
            string deferredFileName = Path.Combine(Path.GetTempPath(), "DeferredCommand.cmd");
            string deferredOutputFileName = Path.Combine(Path.GetTempPath(), "deferred.txt");

            File.Copy(Path.Combine(CAQuietExecTests.TestDataDirectory, @"ImmediateCommand.cmd"), immediateFileName, true);
            File.Copy(Path.Combine(CAQuietExecTests.TestDataDirectory, @"DeferredCommand.cmd"), deferredFileName, true);
            
            if (File.Exists(immediateOutputFileName))
            {
                File.Delete(immediateOutputFileName);
            }
            if (File.Exists(deferredOutputFileName))
            {
                File.Delete(deferredOutputFileName);
            }
            
            string msiFile = Builder.BuildPackage(Environment.CurrentDirectory, sourceFile, "test.msi", string.Format("-dimmediate={0} -ddeferred={1} -ext WixUtilExtension",immediateFileName, deferredFileName), "-ext WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.IsTrue(File.Exists(immediateOutputFileName), "Immediate Command was not executed. File '{0}' does not exist.", immediateOutputFileName);
            Assert.IsTrue(File.Exists(deferredOutputFileName), "Deferred Command was not executed. File '{0}' does not exist.", deferredOutputFileName);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
