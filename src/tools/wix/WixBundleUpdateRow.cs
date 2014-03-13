//-------------------------------------------------------------------------------------------------
// <copyright file="WixBundleUpdateRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Bundle update info for binding Bundles.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Bundle update info for binding Bundles.
    /// </summary>
    public class WixBundleUpdateRow : Row
    {
        /// <summary>
        /// Creates a WixBundleUpdateRow row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this WixBundleUpdateRow row belongs to and should get its column definitions from.</param>
        public WixBundleUpdateRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixBundleUpdateRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this WixBundleUpdateRow row belongs to and should get its column definitions from.</param>
        public WixBundleUpdateRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        public string Location
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }
    }
}
