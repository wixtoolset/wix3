//-------------------------------------------------------------------------------------------------
// <copyright file="Symbol.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Symbol representing a single row in a database.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Text;

    /// <summary>
    /// Symbol representing a single row in a database.
    /// </summary>
    public sealed class Symbol
    {
        private Row row;

        /// <summary>
        /// Creates a symbol for a row.
        /// </summary>
        /// <param name="row">Row for the symbol</param>
        public Symbol(Row row)
        {
            this.row = row;
        }

        /// <summary>
        /// Gets the name of the symbol.
        /// </summary>
        /// <value>Name of the symbol.</value>
        public string Name
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(this.row.TableDefinition.Name);
                sb.Append(":");
                sb.Append(this.row.GetPrimaryKey('/'));

                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets the section for the symbol.
        /// </summary>
        /// <value>Section for the symbol.</value>
        public Section Section
        {
            get { return (null == this.row.Table) ? null : this.row.Table.Section; }
        }

        /// <summary>
        /// Gets the row for this symbol.
        /// </summary>
        /// <value>Row for this symbol.</value>
        public Row Row
        {
            get { return this.row; }
        }
    }
}
