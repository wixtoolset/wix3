//-----------------------------------------------------------------------
// <copyright file="ProductSearchElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>ProductSearch element OM</summary>
//-----------------------------------------------------------------------

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
