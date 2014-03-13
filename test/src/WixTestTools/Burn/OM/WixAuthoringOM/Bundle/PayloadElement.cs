//-----------------------------------------------------------------------
// <copyright file="PayloadElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Resource element OM</summary>
//-----------------------------------------------------------------------

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("Payload")]
    public class PayloadElement
    {
        // Xml attributes
        private string m_Compressed;
        [BurnXmlAttribute("Compressed")]
        public string Compressed
        {
            get { return m_Compressed; }
            set { m_Compressed = value; }
        }

        private string m_Name;
        [BurnXmlAttribute("Name")]
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        private string m_SourceFile;
        [BurnXmlAttribute("SourceFile")]
        public string SourceFile
        {
            get { return m_SourceFile; }
            set { m_SourceFile = value; }
        }

        private string m_DownloadUrl;
        [BurnXmlAttribute("DownloadUrl")]
        public string DownloadUrl
        {
            get { return m_DownloadUrl; }
            set { m_DownloadUrl = value; }
        }

        #region Properties that are not part of Wix authoring schema but are used to keep track of things tests use

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