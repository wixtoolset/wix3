//-------------------------------------------------------------------------------------------------
// <copyright file="WixUpdateRegistrationRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Update registration information.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Update registration information for Binding.
    /// </summary>
    public class WixUpdateRegistrationRow : Row
    {
        /// <summary>
        /// Creates a WixUpdateRegistrationRow row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this WixUpdateRegistrationRow row belongs to and should get its column definitions from.</param>
        public WixUpdateRegistrationRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixUpdateRegistrationRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this WixUpdateRegistrationRow row belongs to and should get its column definitions from.</param>
        public WixUpdateRegistrationRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        public string Manufacturer
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        public string Department
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        public string ProductFamily
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        public string Name
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        public string Classification
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }
    }
}
