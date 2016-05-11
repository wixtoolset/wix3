// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Searches
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("ProductSearch", BundleElement.wixUtilExtNamespace)]
    public class ProductSearchElement : Searches
    {
        public enum ProductSearchResultType
        {
            Version,
            Language,
            State,
            Assignment
        }

        # region Private member

        private string m_Guid;
        private ProductSearchResultType m_ResultType;

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
        public ProductSearchResultType Result
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
