// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.LayoutManager.UX
{
    using System.IO;
    using WixTest.Burn.OM.WixAuthoringOM;
    using WixTest.Burn.OM.WixAuthoringOM.Bundle;
    using WixTest.Burn.OM.WixAuthoringOM.Bundle.UX;

    public class TestUX : UxBase
    {
        private static string[] TestUxBinaries = new string[] { "BootstrapperCore.dll", "BootstrapperCore.config;TestUX.BootstrapperCore.config", "TestUX.dll" };

        public TestUX()
        {
            base.UxBinaryFilename = "mbahost.dll";
        }

        public override void CopyAndConfigureUx(string LayoutLocation, WixElement Wix)
        {
            string srcBinDir = WixTest.Settings.WixToolsDirectory;

            // Copy the TestUX binaries
            LayoutManager.CopyFile(Path.Combine(srcBinDir, base.UxBinaryFilename), Path.Combine(LayoutLocation, base.UxBinaryFilename));
            foreach (string uxFile in TestUX.TestUxBinaries)
            {
                string[] paths = uxFile.Split(new char[] { ';' });
                string srcUxFile = Path.Combine(srcBinDir, paths.Length == 2 ? paths[1] : paths[0]);
                string destUxFile = Path.Combine(LayoutLocation, paths[0]);
                LayoutManager.CopyFile(srcUxFile, destUxFile);
            }
        }

        public override UXElement GetWixBundleUXElement()
        {
            UXElement myUX = new UXElement();
            myUX.SourceFile = base.UxBinaryFilename;

            foreach (string resFile in TestUX.TestUxBinaries)
            {
                string[] paths = resFile.Split(new char[] { ';' });
                PayloadElement payloadElement = new PayloadElement();
                payloadElement.SourceFile = paths[0];
                myUX.Payloads.Add(payloadElement);
            }

            return myUX;
        }
    }
}
