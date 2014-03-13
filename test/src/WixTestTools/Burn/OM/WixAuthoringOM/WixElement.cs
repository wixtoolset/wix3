//-----------------------------------------------------------------------
// <copyright file="WixElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Wix element OM</summary>
//-----------------------------------------------------------------------

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
