//-------------------------------------------------------------------------------------------------
// <copyright file="WixSimpleReferenceRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for the WixSimpleReference table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Xml;

    /// <summary>
    /// Specialization of a row for the WixSimpleReference table.
    /// </summary>
    public sealed class WixSimpleReferenceRow : Row
    {
        /// <summary>
        /// Creates a WixSimpleReferenceRow that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixSimpleReferenceRow(SourceLineNumberCollection sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Creates a WixSimpleReferenceRow that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDefinitions">Table definitions for this row.</param>
        public WixSimpleReferenceRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDefinitions)
            : base(sourceLineNumbers, tableDefinitions)
        {
        }

        /// <summary>
        /// Gets or sets the primary keys of the simple reference.
        /// </summary>
        /// <value>The primary keys of the simple reference.</value>
        public string PrimaryKeys
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets the symbolic name.
        /// </summary>
        /// <value>Symbolic name.</value>
        public string SymbolicName
        {
            get { return String.Concat(this.TableName, ":", this.PrimaryKeys); }
        }

        /// <summary>
        /// Gets or sets the table name of the simple reference.
        /// </summary>
        /// <value>The table name of the simple reference.</value>
        public string TableName
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }
    }
}
