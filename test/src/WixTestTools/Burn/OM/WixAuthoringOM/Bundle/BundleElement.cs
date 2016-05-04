// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle
{
    using WixTest.Burn.OM.ElementAttribute;
    using System.Collections.Generic;

    [BurnXmlElement("Bundle")]
    public class BundleElement
    {
        public const string wixUtilExtNamespace = "http://schemas.microsoft.com/wix/UtilExtension";

        private string m_AboutUrl;
        [BurnXmlAttribute("AboutUrl")]
        public string AboutUrl
        {
            get { return m_AboutUrl; }
            set { m_AboutUrl = value; }
        }

        private string m_Compressed;
        [BurnXmlAttribute("Compressed")]
        public string Compressed
        {
            get { return m_Compressed; }
            set { m_Compressed = value; }
        }

        private string m_DisableModify;
        [BurnXmlAttribute("DisableModify")]
        public string DisableModify
        {
            get { return m_DisableModify; }
            set { m_DisableModify = value; }
        }

        private string m_DisableRemove;
        [BurnXmlAttribute("DisableRemove")]
        public string DisableRemove
        {
            get { return m_DisableRemove; }
            set { m_DisableRemove = value; }
        }

        private string m_DisableRepair;
        [BurnXmlAttribute("DisableRepair")]
        public string DisableRepair
        {
            get { return m_DisableRepair; }
            set { m_DisableRepair = value; }
        }

        private string m_HelpTelephone;
        [BurnXmlAttribute("HelpTelephone")]
        public string HelpTelephone
        {
            get { return m_HelpTelephone; }
            set { m_HelpTelephone = value; }
        }

        private string m_HelpUrl;
        [BurnXmlAttribute("HelpUrl")]
        public string HelpUrl
        {
            get { return m_HelpUrl; }
            set { m_HelpUrl = value; }
        }

        private string m_Manufacturer;
        [BurnXmlAttribute("Manufacturer")]
        public string Manufacturer
        {
            get { return m_Manufacturer; }
            set { m_Manufacturer = value; }
        }

        private string m_Name;
        [BurnXmlAttribute("Name")]
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        private string m_UpdateUrl;
        [BurnXmlAttribute("UpdateUrl")]
        public string UpdateUrl
        {
            get { return m_UpdateUrl; }
            set { m_UpdateUrl = value; }
        }

        private string m_UpgradeCode;
        [BurnXmlAttribute("UpgradeCode")]
        public string UpgradeCode
        {
            get { return m_UpgradeCode; }
            set { m_UpgradeCode = value; }
        }

        private string m_Version;
        [BurnXmlAttribute("Version")]
        public string Version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }


        private UX.UXElement m_UX;
        [BurnXmlChildElement()]
        public UX.UXElement UX
        {
            get { return m_UX; }
            set { m_UX = value; }
        }

        private Chain.ChainElement m_Chain;
        [BurnXmlChildElement()]
        public Chain.ChainElement Chain
        {
            get { return m_Chain; }
            set { m_Chain = value; }
        }

        private List<Variable.VariableElement> m_Variables;
        public List<Variable.VariableElement> Variables
        {
            get
            {
                if (m_Variables == null) m_Variables = new List<Variable.VariableElement>();
                return m_Variables;
            }
            set
            {
                m_Variables = value;
            }
        }
        [BurnXmlChildElement()]
        public Variable.VariableElement[] VariablesArray
        {
            get { return Variables.ToArray(); }
        }

        // properties that are not part of Wix authoring schema but are used to keep track of things tests use

        private bool m_PerMachineT = true;
        public bool PerMachineT
        {
            get { return m_PerMachineT; }
            set { m_PerMachineT = value; }
        }

        // Searches
        private List<Searches.ComponentSearchElement> m_ComponentSearches;
        private List<Searches.DirectorySearchElement> m_DirectorySearches;
        private List<Searches.FileSearchElement> m_FileSearches;
        private List<Searches.ProductSearchElement> m_ProductSearches;
        private List<Searches.RegistrySearchElement> m_RegistrySearches;

        /// <summary>
        /// Property to store list of ComponentSearch elements
        /// </summary>
        public List<Searches.ComponentSearchElement> ComponentSearches
        {
            get
            {
                if (m_ComponentSearches == null)
                    m_ComponentSearches = new List<Searches.ComponentSearchElement>();

                return m_ComponentSearches;
            }
            set
            {
                m_ComponentSearches = value;
            }
        }

        [BurnXmlChildElement()]
        public Searches.ComponentSearchElement[] ComponentSearchArray
        {
            get { return ComponentSearches.ToArray(); }
        }

        /// <summary>
        /// Property to store list of DirectorySearch elements
        /// </summary>
        public List<Searches.DirectorySearchElement> DirectorySearches
        {
            get
            {
                if (m_DirectorySearches == null)
                    m_DirectorySearches = new List<Searches.DirectorySearchElement>();

                return m_DirectorySearches;
            }
            set
            {
                m_DirectorySearches = value;
            }
        }

        [BurnXmlChildElement()]
        public Searches.DirectorySearchElement[] DirectorySearchArray
        {
            get { return DirectorySearches.ToArray(); }
        }

        /// <summary>
        /// Property to store list of FileSearch elements
        /// </summary>
        public List<Searches.FileSearchElement> FileSearches
        {
            get
            {
                if (m_FileSearches == null)
                    m_FileSearches = new List<Searches.FileSearchElement>();

                return m_FileSearches;
            }
            set
            {
                m_FileSearches = value;
            }
        }

        [BurnXmlChildElement()]
        public Searches.FileSearchElement[] FileSearchArray
        {
            get { return FileSearches.ToArray(); }
        }

        /// <summary>
        /// Property to store list of ProductSearch elements
        /// </summary>
        public List<Searches.ProductSearchElement> ProductSearches
        {
            get
            {
                if (m_ProductSearches == null)
                    m_ProductSearches = new List<Searches.ProductSearchElement>();

                return m_ProductSearches;
            }
            set
            {
                m_ProductSearches = value;
            }
        }

        [BurnXmlChildElement()]
        public Searches.ProductSearchElement[] ProductSearchArray
        {
            get { return ProductSearches.ToArray(); }
        }

        /// <summary>
        /// Property to store list of RegistrySearch elements
        /// </summary>
        public List<Searches.RegistrySearchElement> RegistrySearches
        {
            get
            {
                if (m_RegistrySearches == null)
                    m_RegistrySearches = new List<Searches.RegistrySearchElement>();

                return m_RegistrySearches;
            }
            set
            {
                m_RegistrySearches = value;
            }
        }

        [BurnXmlChildElement()]
        public Searches.RegistrySearchElement[] RegistrySearchArray
        {
            get { return RegistrySearches.ToArray(); }
        }

    }
}
