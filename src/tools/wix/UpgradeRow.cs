//-------------------------------------------------------------------------------------------------
// <copyright file="UpgradeRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for the upgrade table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Specialization of a row for the upgrade table.
    /// </summary>
    public sealed class UpgradeRow : Row
    {
        /// <summary>
        /// Creates an Upgrade row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Upgrade row belongs to and should get its column definitions from.</param>
        public UpgradeRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets and sets the upgrade code for the row.
        /// </summary>
        /// <value>Upgrade code for the row.</value>
        public string UpgradeCode
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets and sets the version minimum for the row.
        /// </summary>
        /// <value>Version minimum for the row.</value>
        public string VersionMin
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets and sets the version maximum for the row.
        /// </summary>
        /// <value>Version maximum for the row.</value>
        public string VersionMax
        {        
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets and sets the language for the row.
        /// </summary>
        /// <value>Language for the row.</value>
        public string Language
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets and sets the attributes for the row.
        /// </summary>
        /// <value>Attributes for the row.</value>
        public int Attributes
        {
            get { return (int)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets and sets the remove code for the row.
        /// </summary>
        /// <value>Remove code for the row.</value>
        public string Remove
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets and sets the action property for the row.
        /// </summary>
        /// <value>Action property for the row.</value>
        public string ActionProperty
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }
    }
}
