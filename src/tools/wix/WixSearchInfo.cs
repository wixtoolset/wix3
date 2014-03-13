//-------------------------------------------------------------------------------------------------
// <copyright file="WixSearchInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utility base class for all WixSearches.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Xml;

    /// <summary>
    /// Utility base class for all WixSearches.
    /// </summary>
    internal abstract class WixSearchInfo
    {
        public WixSearchInfo(string id)
        {
            this.Id = id;
        }

        public void AddWixSearchRowInfo(Row row)
        {
            Debug.Assert((string)row[0] == Id);
            Variable = (string)row[1];
            Condition = (string)row[2];
        }

        public string Id { get; private set; }
        public string Variable { get; private set; }
        public string Condition { get; private set; }

        /// <summary>
        /// Generates Burn manifest and ParameterInfo-style markup a search.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlTextWriter writer)
        {
        }

        /// <summary>
        /// Writes attributes common to all WixSearch elements.
        /// </summary>
        /// <param name="writer"></param>
        protected void WriteWixSearchAttributes(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Id", this.Id);
            writer.WriteAttributeString("Variable", this.Variable);
            if (!String.IsNullOrEmpty(this.Condition))
            {
                writer.WriteAttributeString("Condition", this.Condition);
            }
        }
    }
}
