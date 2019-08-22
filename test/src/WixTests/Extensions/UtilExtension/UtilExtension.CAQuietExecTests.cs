// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Util extension CAQuietExec element tests
    /// </summary>
    public class CAQuietExecTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\CAQuietExecTests");
     
        [NamedFact]
        [Description("Verify that CAQuietExec executes the expected command.")]
        [Priority(2)]
        [RuntimeTest]
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

            Assert.True(File.Exists(immediateOutputFileName), String.Format("Immediate Command was not executed. File '{0}' does not exist.", immediateOutputFileName));
            Assert.True(File.Exists(deferredOutputFileName), String.Format("Deferred Command was not executed. File '{0}' does not exist.", deferredOutputFileName));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
