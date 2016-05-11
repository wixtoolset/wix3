// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
