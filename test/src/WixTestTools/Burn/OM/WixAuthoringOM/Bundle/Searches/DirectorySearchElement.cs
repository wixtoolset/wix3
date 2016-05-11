// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Searches
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("DirectorySearch", BundleElement.wixUtilExtNamespace)]
    public class DirectorySearchElement : Searches
    {
        public enum DirectorySearchResultType
        {
            Exists
        }

        # region Private member

        private string m_Path;
        private DirectorySearchResultType m_ResultType;

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
        public DirectorySearchResultType Result
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
