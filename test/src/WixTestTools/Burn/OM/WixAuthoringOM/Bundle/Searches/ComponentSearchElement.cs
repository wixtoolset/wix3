// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Searches
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("ComponentSearch", BundleElement.wixUtilExtNamespace)]
    public class ComponentSearchElement : Searches
    {
        public enum ComponentSearchResultType
        {
            KeyPath,
            State,
            Directory
        }

        # region Private member

        private string m_Guid;
        private ComponentSearchResultType m_ResultType;
        private string m_ProductCode;

        # endregion

        # region Public property

        [BurnXmlAttribute("Guid")]
        public string Guid
        {
            get
            {
                return m_Guid;
            }
            set
            {
                m_Guid = value;
            }
        }

        [BurnXmlAttribute("Result")]
        public ComponentSearchResultType Result
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

        [BurnXmlAttribute("ProductCode")]
        public string ProductCode
        {
            get
            {
                return m_ProductCode;
            }
            set
            {
                m_ProductCode = value;
            }
        }


        # endregion
    }
}
