//-------------------------------------------------------------------------------------------------
// <copyright file="ChainMsiPackageRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Specialization of a row for the ChainMsiPackage table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Specialization of a row for the ChainMsiPackage table.
    /// </summary>
    public sealed class ChainMsiPackageRow : Row
    {
        /// <summary>
        /// Creates a ChainMsiPackage row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public ChainMsiPackageRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a ChainMsiPackageRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public ChainMsiPackageRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the primary key of the ChainPackage row.
        /// </summary>
        /// <value>Primary key of the ChainPackage row.</value>
        public string ChainPackage
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the MSI package's product code.
        /// </summary>
        /// <value>MSI package's product code.</value>
        public string ProductCode
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the language of the MSI package.
        /// </summary>
        /// <value>Language id of the MSI package.</value>
        public int ProductLanguage
        {
            get { return Convert.ToInt32(this.Fields[4].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the product name of the MSI package.
        /// </summary>
        /// <value>Product name of the MSI package.</value>
        public string ProductName
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the product version of the MSI package.
        /// </summary>
        /// <value>Product version of the MSI package.</value>
        public string ProductVersion
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the MSI package's upgrade code.
        /// </summary>
        /// <value>MSI package's upgrade code.</value>
        public string UpgradeCode
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }
    }
}
