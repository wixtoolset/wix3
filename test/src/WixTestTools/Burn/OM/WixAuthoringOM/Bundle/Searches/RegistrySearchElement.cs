//-----------------------------------------------------------------------
// <copyright file="RegistrySearchElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>RegistrySearch element OM</summary>
//-----------------------------------------------------------------------

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Searches
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("RegistrySearch", BundleElement.wixUtilExtNamespace)]
    public class RegistrySearchElement : Searches
    {
        public enum RegistrySearchResultType
        {
            Exists,
            Value
        }

        public enum RegRoot
        {
            HKCU,
            HKLM,
            HKCR,
            HKU
        }

        public enum YesNoType
        {
            yes,
            no
        }

        public enum ResultFormat
        {
            Raw,
            Compatible
        }

        # region Private member

        private YesNoType m_ExpandEnvironmentVariables;
        private ResultFormat m_Format;
        private string m_Key;
        private RegistrySearchResultType m_ResultType;
        private RegRoot m_Root;
        private string m_Value;

        # endregion

        # region Public property

        [BurnXmlAttribute("ExpandEnvironmentVariables")]
        public YesNoType ExpandEnvironmentVariables
        {
            get
            {
                return m_ExpandEnvironmentVariables;
            }
            set
            {
                m_ExpandEnvironmentVariables = value;
            }
        }

        [BurnXmlAttribute("Format")]
        public ResultFormat Format
        {
            get
            {
                return m_Format;
            }
            set
            {
                m_Format = value;
            }
        }

        [BurnXmlAttribute("Key")]
        public string Key
        {
            get
            {
                return m_Key;
            }
            set
            {
                m_Key = value;
            }
        }

        [BurnXmlAttribute("Result")]
        public RegistrySearchResultType Result
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

        [BurnXmlAttribute("Root")]
        public RegRoot Root
        {
            get
            {
                return m_Root;
            }
            set
            {
                m_Root = value;
            }
        }

        [BurnXmlAttribute("Value")]
        public string Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                m_Value = value;
            }
        }

        # endregion

    }
}
