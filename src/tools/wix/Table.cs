//-------------------------------------------------------------------------------------------------
// <copyright file="Table.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Object that represents a table in a database.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The table transform operations.
    /// </summary>
    public enum TableOperation
    {
        /// <summary>
        /// No operation.
        /// </summary>
        None,

        /// <summary>
        /// Added table.
        /// </summary>
        Add,

        /// <summary>
        /// Dropped table.
        /// </summary>
        Drop,
    }

    /// <summary>
    /// Object that represents a table in a database.
    /// </summary>
    public sealed class Table
    {
        private Section section;
        private TableDefinition tableDefinition;
        private TableOperation operation;
        private RowCollection rows;

        /// <summary>
        /// Creates a table in a section.
        /// </summary>
        /// <param name="section">Section to add table to.</param>
        /// <param name="tableDefinition">Definition of the table.</param>
        public Table(Section section, TableDefinition tableDefinition)
        {
            this.section = section;
            this.tableDefinition = tableDefinition;
            this.rows = new RowCollection();
        }

        /// <summary>
        /// Gets the section for the table.
        /// </summary>
        /// <value>Section for the table.</value>
        public Section Section
        {
            get { return this.section; }
        }

        /// <summary>
        /// Gets the table definition.
        /// </summary>
        /// <value>Definition of the table.</value>
        public TableDefinition Definition
        {
            get { return this.tableDefinition; }
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>Name of the table.</value>
        public string Name
        {
            get { return this.tableDefinition.Name; }
        }

        /// <summary>
        /// Gets or sets the table transform operation.
        /// </summary>
        /// <value>The table transform operation.</value>
        public TableOperation Operation
        {
            get { return this.operation; }
            set { this.operation = value; }
        }

        /// <summary>
        /// Gets the rows contained in the table.
        /// </summary>
        /// <value>Rows contained in the table.</value>
        public RowCollection Rows
        {
            get { return this.rows; }
        }

        /// <summary>
        /// Creates a new row in the table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <returns>Row created in table.</returns>
        public Row CreateRow(SourceLineNumberCollection sourceLineNumbers)
        {
            return this.CreateRow(sourceLineNumbers, true);
        }

        /// <summary>
        /// Creates a new row in the table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="add">Specifies whether to only create the row or add it to the table automatically.</param>
        /// <returns>Row created in table.</returns>
        public Row CreateRow(SourceLineNumberCollection sourceLineNumbers, bool add)
        {
            Row row;

            switch (this.Name)
            {
                case "BBControl":
                    row = new BBControlRow(sourceLineNumbers, this);
                    break;
                case "ChainMsiPackage":
                    row = new ChainMsiPackageRow(sourceLineNumbers, this);
                    break;
                case "Component":
                    row = new ComponentRow(sourceLineNumbers, this);
                    break;
                case "Control":
                    row = new ControlRow(sourceLineNumbers, this);
                    break;
                case "File":
                    row = new FileRow(sourceLineNumbers, this);
                    break;
                case "Media":
                    row = new MediaRow(sourceLineNumbers, this);
                    break;
                case "PayloadInfo":
                    row = new PayloadInfoRow(sourceLineNumbers, this);
                    break;
                case "Upgrade":
                    row = new UpgradeRow(sourceLineNumbers, this);
                    break;
                case "Variable":
                    row = new VariableRow(sourceLineNumbers, this);
                    break;
                case "WixAction":
                    row = new WixActionRow(sourceLineNumbers, this);
                    break;
                case "WixApprovedExeForElevation":
                    row = new WixApprovedExeForElevationRow(sourceLineNumbers, this);
                    break;
                case "WixBundle":
                    row = new WixBundleRow(sourceLineNumbers, this);
                    break;
                case "WixBundlePatchTargetCode":
                    row = new WixBundlePatchTargetCodeRow(sourceLineNumbers, this);
                    break;
                case "WixBundleUpdate":
                    row = new WixBundleUpdateRow(sourceLineNumbers, this);
                    break;
                case "WixCatalog":
                    row = new WixCatalogRow(sourceLineNumbers, this);
                    break;
                case "WixComplexReference":
                    row = new WixComplexReferenceRow(sourceLineNumbers, this);
                    break;
                case "WixFile":
                    row = new WixFileRow(sourceLineNumbers, this);
                    break;
                case "WixMedia":
                    row = new WixMediaRow(sourceLineNumbers, this);
                    break;
                case "WixMediaTemplate":
                    row = new WixMediaTemplateRow(sourceLineNumbers, this);
                    break;
                case "WixMerge":
                    row = new WixMergeRow(sourceLineNumbers, this);
                    break;
                case "WixProperty":
                    row = new WixPropertyRow(sourceLineNumbers, this);
                    break;
                case "WixSimpleReference":
                    row = new WixSimpleReferenceRow(sourceLineNumbers, this);
                    break;
                case "WixUpdateRegistration":
                    row = new WixUpdateRegistrationRow(sourceLineNumbers, this);
                    break;
                case "WixVariable":
                    row = new WixVariableRow(sourceLineNumbers, this);
                    break;

                default:
                    row = new Row(sourceLineNumbers, this);
                    break;
            }

            if (add)
            {
                this.rows.Add(row);
            }

            return row;
        }

        /// <summary>
        /// Parse a table from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="section">Section to populate with persisted data.</param>
        /// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
        /// <returns>The parsed table.</returns>
        internal static Table Parse(XmlReader reader, Section section, TableDefinitionCollection tableDefinitions)
        {
            Debug.Assert("table" == reader.LocalName);

            bool empty = reader.IsEmptyElement;
            TableOperation operation = TableOperation.None;
            string name = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "name":
                        name = reader.Value;
                        break;
                    case "op":
                        switch (reader.Value)
                        {
                            case "add":
                                operation = TableOperation.Add;
                                break;
                            case "drop":
                                operation = TableOperation.Drop;
                                break;
                            default:
                                throw new WixException(WixErrors.IllegalAttributeValue(SourceLineNumberCollection.FromUri(reader.BaseURI), "table", reader.Name, reader.Value, "Add", "Drop"));
                        }
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "table", reader.Name));
                        }
                        break;
                }
            }

            if (null == name)
            {
                throw new WixException(WixErrors.ExpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "table", "name"));
            }

            TableDefinition tableDefinition = tableDefinitions[name];
            Table table = new Table(section, tableDefinition);
            table.Operation = operation;

            if (!empty)
            {
                bool done = false;

                // loop through all the rows in a table
                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "row":
                                    Row.Parse(reader, table);
                                    break;
                                default:
                                    throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "table", reader.Name));
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "table"));
                }
            }

            return table;
        }

        /// <summary>
        /// Modularize the table.
        /// </summary>
        /// <param name="modularizationGuid">String containing the GUID of the Merge Module, if appropriate.</param>
        /// <param name="suppressModularizationIdentifiers">Optional collection of identifiers that should not be modularized.</param>
        internal void Modularize(string modularizationGuid, Hashtable suppressModularizationIdentifiers)
        {
            ArrayList modularizedColumns = new ArrayList();

            // find the modularized columns
            for (int i = 0; i < this.Definition.Columns.Count; i++)
            {
                if (ColumnModularizeType.None != this.Definition.Columns[i].ModularizeType)
                {
                    modularizedColumns.Add(i);
                }
            }

            if (0 < modularizedColumns.Count)
            {
                foreach (Row row in this.rows)
                {
                    foreach (int modularizedColumn in modularizedColumns)
                    {
                        Field field = row.Fields[modularizedColumn];

                        if (null != field.Data)
                        {
                            field.Data = row.GetModularizedValue(field, modularizationGuid, suppressModularizationIdentifiers);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Persists a row in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Row should persist itself as XML.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the intermediate files are generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        internal void Persist(XmlWriter writer)
        {
            if (null == writer)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("table", Intermediate.XmlNamespaceUri);
            writer.WriteAttributeString("name", this.Name);

            if (TableOperation.None != this.operation)
            {
                writer.WriteAttributeString("op", this.operation.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
            }

            foreach (Row row in this.rows)
            {
                row.Persist(writer);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Returns the table in a format usable in IDT files.
        /// </summary>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <returns>null if OutputTable is unreal, or string with tab delimited field values otherwise</returns>
        internal void ToIdtDefinition(StreamWriter writer, IMessageHandler messageHandler, bool keepAddedColumns)
        {
            string rowString = String.Empty;
            byte[] rowBytes = {};
            // Create a new encoding that replaces characters with question marks, and doesn't throw, used in case of errors
            Encoding convertEncoding = Encoding.GetEncoding(writer.Encoding.CodePage);

            if (this.tableDefinition.IsUnreal)
            {
                return;
            }

            if (TableDefinition.MaxColumnsInRealTable < this.tableDefinition.Columns.Count)
            {
                throw new WixException(WixErrors.TooManyColumnsInRealTable(this.tableDefinition.Name, this.tableDefinition.Columns.Count, TableDefinition.MaxColumnsInRealTable));
            }

            // tack on the table header, and flush before we start writing bytes directly to the stream
            writer.Write(this.tableDefinition.ToIdtDefinition(keepAddedColumns));
            writer.Flush();

            BufferedStream buffStream = new BufferedStream(writer.BaseStream);

            foreach (Row row in this.rows)
            {
                rowString = row.ToIdtDefinition(keepAddedColumns);

                try
                {
                    // GetBytes will throw an exception if any character doesn't match our current encoding
                    rowBytes = writer.Encoding.GetBytes(rowString);
                }
                catch (EncoderFallbackException)
                {
                    rowBytes = convertEncoding.GetBytes(rowString);

                    messageHandler.OnMessage(WixErrors.InvalidStringForCodepage(row.SourceLineNumbers, Convert.ToString(writer.Encoding.WindowsCodePage, CultureInfo.InvariantCulture)));
                }

                buffStream.Write(rowBytes, 0, rowBytes.Length);
            }

            buffStream.Flush();
        }

        /// <summary>
        /// Validates the rows of this OutputTable and throws if it collides on
        /// primary keys.
        /// </summary>
        internal void ValidateRows()
        {
            Hashtable primaryKeys = new Hashtable(this.Rows.Count);

            foreach (Row row in this.Rows)
            {
                string primaryKey = row.GetPrimaryKey('/');

                // check for collisions
                if (primaryKeys.Contains(primaryKey))
                {
                    throw new WixException(WixErrors.DuplicatePrimaryKey((SourceLineNumberCollection)primaryKeys[primaryKey], primaryKey, this.tableDefinition.Name));
                }

                primaryKeys.Add(primaryKey, row.SourceLineNumbers);
            }
        }
    }
}
