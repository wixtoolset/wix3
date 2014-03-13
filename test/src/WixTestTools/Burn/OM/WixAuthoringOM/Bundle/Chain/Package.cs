//-----------------------------------------------------------------------
// <copyright file="Package.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Package element OM</summary>
//-----------------------------------------------------------------------

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain
{
    using System.Collections.Generic;
    using WixTest.Burn.OM.ElementAttribute;

    public abstract class Package
    {
        // Xml attributes
        private string m_After;
        [BurnXmlAttribute("After")]
        public string After
        {
            get { return m_After; }
            set { m_After = value; }
        }

        private string m_Cache;
        [BurnXmlAttribute("Cache")]
        public string Cache
        {
            get { return m_Cache; }
            set { m_Cache = value; }
        }

        private string m_CacheId;
        [BurnXmlAttribute("CacheId")]
        public string CacheId
        {
            get { return m_CacheId; }
            set { m_CacheId = value; }
        }

        private string m_Compressed;
        [BurnXmlAttribute("Compressed")]
        public string Compressed
        {
            get { return m_Compressed; }
            set { m_Compressed = value; }
        }

        private string m_DownloadUrl;
        [BurnXmlAttribute("DownloadUrl")]
        public string DownloadUrl
        {
            get { return m_DownloadUrl; }
            set { m_DownloadUrl = value; }
        }

        private string m_Id;
        [BurnXmlAttribute("Id")]
        public string Id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        private string m_InstallCondition;
        [BurnXmlAttribute("InstallCondition")]
        public string InstallCondition
        {
            get { return m_InstallCondition; }
            set { m_InstallCondition = value; }
        }

        private string m_LogPathVariable;
        [BurnXmlAttribute("LogPathVariable")]
        public string LogPathVariable
        {
            get { return m_LogPathVariable; }
            set { m_LogPathVariable = value; }
        }

        private string m_Name;
        [BurnXmlAttribute("Name")]
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        private string m_Permanent;
        [BurnXmlAttribute("Permanent")]
        public string Permanent
        {
            get { return m_Permanent; }
            set { m_Permanent = value; }
        }

        private string m_RollbackLogPathVariable;
        [BurnXmlAttribute("RollbackLogPathVariable")]
        public string RollbackLogPathVariable
        {
            get { return m_RollbackLogPathVariable; }
            set { m_RollbackLogPathVariable = value; }
        }

        private string m_SourceFile;
        [BurnXmlAttribute("SourceFile")]
        public string SourceFile
        {
            get { return m_SourceFile; }
            set { m_SourceFile = value; }
        }

        private string m_Vital;
        [BurnXmlAttribute("Vital")]
        public string Vital
        {
            get { return m_Vital; }
            set { m_Vital = value; }
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

        #region Properties that are not part of Wix authoring schema but are used to keep track of things tests use

        /// <summary>
        /// True if the item is to be installed per-machine, false if it is to be installed per-user.
        /// This is what the tests expect.  When the bundle is built, this is calculated.  This tracks the expected vs what actually ends up authored in the built bundle.
        /// </summary>
        private bool m_PerMachineT = true;
        public bool PerMachineT
        {
            // TODO automatically calculate this for MSIs & MSPs...
            get { return m_PerMachineT; }
            set { m_PerMachineT = value; }
        }

        /// <summary>
        /// Full path to the package.  Tests use this to verify if packages are installed or not.
        /// </summary>
        private string m_SourceFilePathT;
        public string SourceFilePathT
        {
            get { return m_SourceFilePathT; }
            set { m_SourceFilePathT = value; }
        }

        #endregion
    }
}
