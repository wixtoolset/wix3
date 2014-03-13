//-----------------------------------------------------------------------
// <copyright file="ChainElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Chain element OM</summary>
//-----------------------------------------------------------------------

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain
{
    using System.Collections.Generic;
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("Chain")]
    public class ChainElement
    {
        private string m_DisableRollback;
        [BurnXmlAttribute("DisableRollback")]
        public string DisableRollback
        {
            get { return m_DisableRollback; }
            set { m_DisableRollback = value; }
        }

        private List<Package> m_Packages;

        public List<Package> Packages
        {
            get
            {
                if (m_Packages == null) m_Packages = new List<Package>();
                return m_Packages;
            }
            set
            {
                m_Packages = value;
            }
        }
        [BurnXmlChildElement()]
        public Package[] PackagesArray
        {
            get { return Packages.ToArray(); }
        }
    }
}
