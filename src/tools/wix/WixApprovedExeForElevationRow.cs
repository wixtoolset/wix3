//-------------------------------------------------------------------------------------------------
// <copyright file="WixApprovedExeForElevationRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for the WixApprovedExeForElevation table.
// </summary>
//-------------------------------------------------------------------------------------------------

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
        /// <value>The catalog identifier.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source file (path).
        /// </summary>
        /// <value>The source file (path).</value>
        public string SourceFile
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }
    }
}
