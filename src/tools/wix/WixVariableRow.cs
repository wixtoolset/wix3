// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Specialization of a row for the WixVariable table.
    /// </summary>
    public sealed class WixVariableRow : Row
    {
        /// <summary>
        /// Creates a WixVariable row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this WixVariable row belongs to and should get its column definitions from.</param>
        public WixVariableRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixVariable row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this WixVariable row belongs to and should get its column definitions from.</param>
        public WixVariableRow(SourceLineNumberCollection sourceLineNumbers, Table table) : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the variable identifier.
        /// </summary>
        /// <value>The variable identifier.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the variable's value.
        /// </summary>
        /// <value>The variable's value.</value>
        public string Value
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets whether this variable is overridable.
        /// </summary>
        /// <value>Whether this variable is overridable.</value>
        public bool Overridable
        {
            get
            {
                return (0x1 == (Convert.ToInt32(this.Fields[2].Data, CultureInfo.InvariantCulture) & 0x1));
            }

            set
            {
                if (null == this.Fields[2].Data)
                {
                    this.Fields[2].Data = 0;
                }

                if (value)
                {
                    this.Fields[2].Data = (int)this.Fields[2].Data | 0x1;
                }
                else
                {
                    this.Fields[2].Data = (int)this.Fields[2].Data & ~0x1;
                }
            }
        }
    }
}
