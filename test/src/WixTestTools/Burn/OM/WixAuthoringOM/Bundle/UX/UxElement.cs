//-----------------------------------------------------------------------
// <copyright file="UxElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Ux element OM</summary>
//-----------------------------------------------------------------------

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.UX
{
    using System.Collections.Generic;
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("UX")]
    public class UXElement
    {
        // Xml attributes
        private string m_SourceFile;
        [BurnXmlAttribute("SourceFile")]
        public string SourceFile
        {
            get { return m_SourceFile; }
            set { m_SourceFile = value; }
        }

        private List<PayloadElement> m_Payloads;

        public List<PayloadElement> Payloads
        {
            get
            {
                if (m_Payloads == null) m_Payloads = new List<PayloadElement>();
                return m_Payloads;
            }
            set
            {
                m_Payloads = value;
            }
        }
        [BurnXmlChildElement()]
        public PayloadElement[] PayloadsArray
        {
            get { return Payloads.ToArray(); }
        }
    }
}
