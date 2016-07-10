// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.LayoutManager.UX
{
    using WixTest.Burn.OM.WixAuthoringOM;

    public abstract class UxBase
    {
        private string m_UxBinaryFilename;
        public string UxBinaryFilename
        {
            get
            {
                return m_UxBinaryFilename;
            }
            set
            {
                m_UxBinaryFilename = value;
            }
        }

        public abstract void CopyAndConfigureUx(string LayoutLocation, WixElement Wix);

        public abstract WixTest.Burn.OM.WixAuthoringOM.Bundle.UX.UXElement GetWixBundleUXElement();
    }
}
