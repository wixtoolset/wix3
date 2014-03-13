//-----------------------------------------------------------------------
// <copyright file="MsiPackageElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>MsiPackage element OM</summary>
//-----------------------------------------------------------------------

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("MsiPackage")]
    public class MsiPackageElement : Package
    {
        private MsiPropertyElement m_MsiPropertylement;

        [BurnXmlChildElement()]
        public MsiPropertyElement MsiProperty
        {
            get
            {
                return m_MsiPropertylement;
            }
            set
            {
                m_MsiPropertylement = value;
            }
        }
    }
}
