// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Specialization of a row for the WixApprovedExeForElevation table.
    /// </summary>
    public class WixApprovedExeForElevationRow : Row
    {
        /// <summary>
        /// Creates an ApprovedExeForElevation row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this ApprovedExeForElevation row belongs to and should get its column definitions from.</param>
        public WixApprovedExeForElevationRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates an ApprovedExeForElevation row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this ApprovedExeForElevation row belongs to and should get its column definitions from.</param>
        public WixApprovedExeForElevationRow(SourceLineNumberCollection sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the ApprovedExeForElevation identifier.
        /// </summary>
        /// <value>The ApprovedExeForElevation identifier.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the Key path.
        /// </summary>
        /// <value>The Key path.</value>
        public string Key
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the Value name.
        /// </summary>
        /// <value>The Value name.</value>
        public string ValueName
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the attibutes.
        /// </summary>
        /// <value>The BundleApprovedExeForElevationAttributes.</value>
        public BundleApprovedExeForElevationAttributes Attributes
        {
            get { return (BundleApprovedExeForElevationAttributes)this.Fields[3].Data; }
            set { this.Fields[3].Data = (int)value; }
        }
    }
}
