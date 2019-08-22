// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
