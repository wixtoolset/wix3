// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
