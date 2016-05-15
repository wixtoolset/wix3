// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.BurnIntegrationTests
{
    using System.Collections.Generic;
    using System.IO;
    using Xunit;

    /// <summary>
    /// Restart Burn tests.
    /// </summary>
    public class RestartTests : BurnTestBase
    {
        private PackageBuilder packageA;
        private BundleBuilder bundleA;

        [NamedFact]
        [RuntimeTest]
        public void Burn_RetryThenCancel()
        {
            this.SetPackageRetryExecuteFilesInUse("PackageA", 1);

            string bundleAPath = this.GetBundleA().Output;
            BundleInstaller installA = new BundleInstaller(this, bundleAPath);

            // Lock the file that will be installed.
            string targetInstallFile = this.GetTestInstallFolder("A\\A.wxs");
            Directory.CreateDirectory(Path.GetDirectoryName(targetInstallFile));
            using (FileStream lockTargetFile = new FileStream(targetInstallFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose))
            {
                installA.Install(expectedExitCode:(int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            }
            
            this.Complete();
        }

        private PackageBuilder GetPackageA()
        {
            if (null == this.packageA)
            {
                this.packageA = this.CreatePackage("A");
            }

            return this.packageA;
        }

        private BundleBuilder GetBundleA(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageA().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            if (null == this.bundleA)
            {
                this.bundleA = this.CreateBundle("BundleA", bindPaths);
            }

            return this.bundleA;
        }
    }
}
