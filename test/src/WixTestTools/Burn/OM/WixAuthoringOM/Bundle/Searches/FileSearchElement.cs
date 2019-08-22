// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Searches
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("FileSearch", BundleElement.wixUtilExtNamespace)]
    public class FileSearchElement : Searches
    {
        public enum FileSearchResultType
        {
            Exists,
            Version
        }

        # region Private member

        private string m_Path;
        private FileSearchResultType m_ResultType;

        # endregion

        # region Public property

        [BurnXmlAttribute("Path")]
        public string Path
        {
            get
            {
                return m_Path;
            }
            set
            {
                m_Path = value;
            }
        }

        [BurnXmlAttribute("Result")]
        public FileSearchResultType Result
        {
            get
            {
                return m_ResultType;
            }
            set
            {
                m_ResultType = value;
            }
        }

        # endregion

    }
}
