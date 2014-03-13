//-----------------------------------------------------------------------
// <copyright file="MsiPropertyElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>MsiProperty element OM</summary>
//-----------------------------------------------------------------------

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("MsiProperty")]
    public class MsiPropertyElement
    {
        private string m_Name;
        private string m_Value;

        [BurnXmlAttribute("Name")]
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        [BurnXmlAttribute("Value")]
        public string Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                m_Value = value;
            }
        }
    }
}
