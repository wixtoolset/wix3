// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Specialization of a row for the Control table.
    /// </summary>
    public sealed class BBControlRow : Row
    {
        private string sourceFile;

        /// <summary>
        /// Creates a Control row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Control row belongs to and should get its column definitions from.</param>
        public BBControlRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the dialog of the Control row.
        /// </summary>
        /// <value>Primary key of the Control row.</value>
        public string Billboard
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the identifier for this Control row.
        /// </summary>
        /// <value>Identifier for this Control row.</value>
        public string BBControl
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the type of the BBControl.
        /// </summary>
        /// <value>Name of the BBControl.</value>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public string Type
        {
            get { return Convert.ToString(this.Fields[2].Data); }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the X location of the BBControl.
        /// </summary>
        /// <value>X location of the BBControl.</value>
        public string X
        {
            get { return Convert.ToString(this.Fields[3].Data); }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the Y location of the BBControl.
        /// </summary>
        /// <value>Y location of the BBControl.</value>
        public string Y
        {
            get { return Convert.ToString(this.Fields[4].Data); }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the width of the BBControl.
        /// </summary>
        /// <value>Width of the BBControl.</value>
        public string Width
        {
            get { return Convert.ToString(this.Fields[5].Data); }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the height of the BBControl.
        /// </summary>
        /// <value>Height of the BBControl.</value>
        public string Height
        {
            get { return Convert.ToString(this.Fields[6].Data); }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets or sets the attributes for the BBControl.
        /// </summary>
        /// <value>Attributes for the BBControl.</value>
        public int Attributes
        {
            get { return (int)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        /// <summary>
        /// Gets or sets the text of the BBControl.
        /// </summary>
        /// <value>Text of the BBControl.</value>
        public string Text
        {
            get { return (string)this.Fields[8].Data; }
            set { this.Fields[8].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file to fill in the Text of the BBControl.
        /// </summary>
        /// <value>Source location to the file to fill in the Text of the BBControl.</value>
        public string SourceFile
        {
            get { return this.sourceFile; }
            set { this.sourceFile = value; }
        }
    }
}
