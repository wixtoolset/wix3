// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Definition of a table in a database.
    /// </summary>
    public sealed class TableDefinition : IComparable<TableDefinition>
    {
        /// <summary>
        /// Tracks the maximum number of columns supported in a real table.
        /// This is a Windows Installer limitation.
        /// </summary>
        public const int MaxColumnsInRealTable = 32;

        private bool createSymbols;
        private string name;
        private bool unreal;
        private bool bootstrapperApplicationData;
        private bool localizable;
        private ColumnDefinitionCollection columns;

        /// <summary>
        /// Creates a table definition.
        /// </summary>
        /// <param name="name">Name of table to create.</param>
        /// <param name="createSymbols">Flag if rows in this table create symbols.</param>
        /// <param name="unreal">Flag if table is unreal.</param>
        public TableDefinition(string name, bool createSymbols, bool unreal)
            : this(name, createSymbols, unreal, false)
        {
        }

        /// <summary>
        /// Creates a table definition.
        /// </summary>
        /// <param name="name">Name of table to create.</param>
        /// <param name="createSymbols">Flag if rows in this table create symbols.</param>
        /// <param name="unreal">Flag if table is unreal.</param>
        /// <param name="bootstrapperApplicationData">Flag if table is part of UX Manifest.</param>
        public TableDefinition(string name, bool createSymbols, bool unreal, bool bootstrapperApplicationData)
        {
            this.name = name;
            this.createSymbols = createSymbols;
            this.unreal = unreal;
            this.bootstrapperApplicationData = bootstrapperApplicationData;
            this.columns = new ColumnDefinitionCollection();
        }

        /// <summary>
        /// Gets if rows in this table create symbols.
        /// </summary>
        /// <value>Flag if rows in this table create symbols.</value>
        public bool CreateSymbols
        {
            get { return this.createSymbols; }
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>Name of the table.</value>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets if the table is unreal.
        /// </summary>
        /// <value>Flag if table is unreal.</value>
        public bool IsUnreal
        {
            get { return this.unreal; }
        }

        /// <summary>
        /// Gets if the table is a part of the bootstrapper application data manifest.
        /// </summary>
        /// <value>Flag if table is a part of the bootstrapper application data manifest.</value>
        public bool IsBootstrapperApplicationData
        {
            get { return this.bootstrapperApplicationData; }
        }

        /// <summary>
        /// Gets if the table is localizable (i.e. has any columns that might have localized data in them).
        /// </summary>
        /// <value>Flag if table is localizable.</value>
        public bool IsLocalizable
        {
            get { return this.localizable; }
        }

        /// <summary>
        /// Gets the collection of column definitions for this table.
        /// </summary>
        /// <value>Collection of column definitions for this table.</value>
        public ColumnDefinitionCollection Columns
        {
            get { return this.columns; }
        }

        /// <summary>
        /// Gets the column definition in the table by index.
        /// </summary>
        /// <param name="columnIndex">Index of column to locate.</param>
        /// <value>Column definition in the table by index.</value>
        public ColumnDefinition this[int columnIndex]
        {
            get { return (ColumnDefinition)this.columns[columnIndex]; }
        }

        /// <summary>
        /// Gets the table definition in IDT format.
        /// </summary>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <returns>Table definition in IDT format.</returns>
        public string ToIdtDefinition(bool keepAddedColumns)
        {
            bool first = true;
            StringBuilder columnString = new StringBuilder();
            StringBuilder dataString = new StringBuilder();
            StringBuilder tableString = new StringBuilder();

            tableString.Append(this.name);
            foreach (ColumnDefinition column in this.columns)
            {
                // conditionally keep columns added in a transform; otherwise,
                // break because columns can only be added at the end
                if (column.Added && !keepAddedColumns)
                {
                    break;
                }

                if (!first)
                {
                    columnString.Append('\t');
                    dataString.Append('\t');
                }

                columnString.Append(column.Name);
                dataString.Append(column.IdtType);

                if (column.IsPrimaryKey)
                {
                    tableString.AppendFormat("\t{0}", column.Name);
                }

                first = false;
            }
            columnString.Append("\r\n");
            columnString.Append(dataString);
            columnString.Append("\r\n");
            columnString.Append(tableString);
            columnString.Append("\r\n");

            return columnString.ToString();
        }

        /// <summary>
        /// Parses table definition from xml reader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>The TableDefintion represented by the Xml.</returns>
        internal static TableDefinition Parse(XmlReader reader)
        {
            Debug.Assert("tableDefinition" == reader.LocalName);

            bool empty = reader.IsEmptyElement;
            bool createSymbols = false;
            bool hasPrimaryKeyColumn = false;
            string name = null;
            bool unreal = false;
            bool bootstrapperApplicationData = false;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "createSymbols":
                        createSymbols = Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "createSymbols", reader.Name, reader.Value);
                        break;
                    case "name":
                        name = reader.Value;
                        break;
                    case "unreal":
                        unreal = Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinition", reader.Name, reader.Value);
                        break;
                    case "bootstrapperApplicationData":
                        bootstrapperApplicationData = Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinition", reader.Name, reader.Value);
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinition", reader.Name));
                        }
                        break;
                }
            }

            if (null == name)
            {
                throw new WixException(WixErrors.ExpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinition", "name"));
            }

            TableDefinition tableDefinition = new TableDefinition(name, createSymbols, unreal, bootstrapperApplicationData);

            // parse the child elements
            if (!empty)
            {
                bool done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "columnDefinition":
                                ColumnDefinition columnDefinition = ColumnDefinition.Parse(reader);
                                tableDefinition.columns.Add(columnDefinition);

                                if (columnDefinition.IsLocalizable)
                                {
                                    tableDefinition.localizable = true;
                                }

                                if (columnDefinition.IsPrimaryKey)
                                {
                                    hasPrimaryKeyColumn = true;
                                }
                                break;
                            default:
                                throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinition", reader.Name));
                        }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!unreal && !bootstrapperApplicationData && !hasPrimaryKeyColumn)
                {
                    throw new WixException(WixErrors.RealTableMissingPrimaryKeyColumn(SourceLineNumberCollection.FromUri(reader.BaseURI), name));
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinition"));
                }
            }

            return tableDefinition;
        }

        /// <summary>
        /// Persists an output in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Output should persist itself as XML.</param>
        internal void Persist(XmlWriter writer)
        {
            writer.WriteStartElement("tableDefinition", TableDefinitionCollection.XmlNamespaceUri);

            writer.WriteAttributeString("name", this.name);

            if (this.createSymbols)
            {
                writer.WriteAttributeString("createSymbols", "yes");
            }

            if (this.unreal)
            {
                writer.WriteAttributeString("unreal", "yes");
            }

            if (this.bootstrapperApplicationData)
            {
                writer.WriteAttributeString("bootstrapperApplicationData", "yes");
            }

            foreach (ColumnDefinition columnDefinition in this.columns)
            {
                columnDefinition.Persist(writer);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the validation rows to the _Validation table.
        /// </summary>
        /// <param name="validationTable">The _Validation table.</param>
        internal void AddValidationRows(Table validationTable)
        {
            foreach (ColumnDefinition columnDef in this.columns)
            {
                Row row = validationTable.CreateRow(null);

                row[0] = this.name;

                row[1] = columnDef.Name;

                if (columnDef.IsNullable)
                {
                    row[2] = "Y";
                }
                else
                {
                    row[2] = "N";
                }

                if (columnDef.IsMinValueSet)
                {
                    row[3] = columnDef.MinValue;
                }

                if (columnDef.IsMaxValueSet)
                {
                    row[4] = columnDef.MaxValue;
                }

                row[5] = columnDef.KeyTable;

                if (columnDef.IsKeyColumnSet)
                {
                    row[6] = columnDef.KeyColumn;
                }

                if (ColumnCategory.Unknown != columnDef.Category)
                {
                    row[7] = columnDef.Category.ToString();
                }

                row[8] = columnDef.Possibilities;

                row[9] = columnDef.Description;
            }
        }

        /// <summary>
        /// Compares this table definition to another table definition.
        /// </summary>
        /// <remarks>
        /// Only Windows Installer traits are compared, allowing for updates to WiX-specific table definitions.
        /// </remarks>
        /// <param name="updated">The updated <see cref="TableDefinition"/> to compare with this target definition.</param>
        /// <returns>0 if the tables' core properties are the same; otherwise, non-0.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison")]
        public int CompareTo(TableDefinition updated)
        {
            // by definition, this object is greater than null
            if (null == updated)
            {
                return 1;
            }

            // compare the table names
            int ret = String.Compare(this.Name, updated.Name, StringComparison.InvariantCulture);

            // compare the column count
            if (0 == ret)
            {
                // transforms can only add columns
                ret = Math.Min(0, updated.Columns.Count - this.Columns.Count);

                // compare name, type, and length of each column
                for (int i = 0; 0 == ret && this.Columns.Count > i; i++)
                {
                    ColumnDefinition thisColumnDef = this.Columns[i];
                    ColumnDefinition updatedColumnDef = updated.Columns[i];

                    ret = thisColumnDef.CompareTo(updatedColumnDef);
                }
            }

            return ret;
        }
    }
}
