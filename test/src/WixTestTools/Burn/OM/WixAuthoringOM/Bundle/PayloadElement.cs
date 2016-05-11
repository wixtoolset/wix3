// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
