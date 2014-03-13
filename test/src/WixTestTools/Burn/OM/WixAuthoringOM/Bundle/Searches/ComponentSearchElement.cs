//-----------------------------------------------------------------------
// <copyright file="ComponentSearchElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>ComponentSearch element OM</summary>
//-----------------------------------------------------------------------

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
