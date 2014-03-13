//-----------------------------------------------------------------------
// <copyright file="UxBase.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>base for different Burn UX</summary>
//-----------------------------------------------------------------------

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
