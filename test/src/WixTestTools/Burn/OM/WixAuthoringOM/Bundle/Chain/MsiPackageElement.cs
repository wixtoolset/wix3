// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
