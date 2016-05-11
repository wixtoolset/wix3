// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.OM.WixAuthoringOM
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("Wix")]
    public class WixElement
    {
        // Xml attributes
        private string m_Xmlns;

        [BurnXmlAttribute("xmlns")]
        public string Xmlns
        {
            get
            { return m_Xmlns; }
            set
            { m_Xmlns = value; }

        }

        private Bundle.BundleElement m_Bundle;
        
        [BurnXmlChildElement()]
        public Bundle.BundleElement Bundle
        {
            get { return m_Bundle; }
            set { m_Bundle = value; }
        }

    }
}
