// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Hash table collection for table definitions.
    /// </summary>
    public sealed class TableDefinitionCollection : ICollection
    {
        public const string XmlNamespaceUri = "http://schemas.microsoft.com/wix/2006/tables";
        private static XmlSchemaCollection schemas;

        private SortedList collection;

        /// <summary>
        /// Instantiate a new TableDefinitionCollection class.
        /// </summary>
        public TableDefinitionCollection()
        {
            this.collection = new SortedList();
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>Number of items in collection.</value>
        public int Count
        {
            get { return this.collection.Count; }
        }

        /// <summary>
        /// Gets if the collection has been synchronized.
        /// </summary>
        /// <value>True if the collection has been synchronized.</value>
        public bool IsSynchronized
        {
            get { return this.collection.IsSynchronized; }
        }

        /// <summary>
        /// Gets the object used to synchronize the collection.
        /// </summary>
        /// <value>Oject used the synchronize the collection.</value>
        public object SyncRoot
        {
            get { return this.collection.SyncRoot; }
        }

        /// <summary>
        /// Gets a table definition by name.
        /// </summary>
        /// <param name="tableName">Name of table to locate.</param>
        public TableDefinition this[string tableName]
        {
            get
            {
                if (!this.collection.ContainsKey(tableName))
                {
                    throw new WixMissingTableDefinitionException(WixErrors.MissingTableDefinition(tableName));
                }

                return (TableDefinition)this.collection[tableName];
            }
        }

        /// <summary>
        /// Load a table definition collection from an XmlReader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>The TableDefinitionCollection represented by the xml.</returns>
        public static TableDefinitionCollection Load(XmlReader reader, bool suppressSchema)
        {
            if (null == reader)
            {
                throw new ArgumentNullException("reader");
            }

            if (!suppressSchema)
            {
                // validate the table definitions xml against its schema in debug
                reader = new XmlValidatingReader(reader);
                ((XmlValidatingReader)reader).Schemas.Add(GetSchemas());
            }

            reader.MoveToContent();

            if ("tableDefinitions" != reader.LocalName)
            {
                throw new WixException(WixErrors.InvalidDocumentElement(SourceLineNumberCollection.FromUri(reader.BaseURI), reader.Name, "table definitions", "tableDefinitions"));
            }

            return Parse(reader);
        }

        /// <summary>
        /// Adds a table definition to the collection.
        /// </summary>
        /// <param name="tableDefinition">Table definition to add to the collection.</param>
        /// <value>Indexes by table definition name.</value>
        public void Add(TableDefinition tableDefinition)
        {
            if (null == tableDefinition)
            {
                throw new ArgumentNullException("tableDefinition");
            }

            this.collection.Add(tableDefinition.Name, tableDefinition);
        }

        /// <summary>
        /// Removes all table definitions from the collection.
        /// </summary>
        public void Clear()
        {
            this.collection.Clear();
        }

        /// <summary>
        /// Creates a shallow copy of this table definition collection.
        /// </summary>
        /// <returns>A shallow copy of this table definition collection.</returns>
        public TableDefinitionCollection Clone()
        {
            TableDefinitionCollection tableDefinitionCollection = new TableDefinitionCollection();

            tableDefinitionCollection.collection = (SortedList)this.collection.Clone();

            return tableDefinitionCollection;
        }

        /// <summary>
        /// Checks if the collection contains a table.
        /// </summary>
        /// <param name="tableName">The table to check in the collection.</param>
        /// <returns>True if collection contains the table.</returns>
        public bool Contains(string tableName)
        {
            return this.collection.Contains(tableName);
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">Array to copy the collection into.</param>
        /// <param name="index">Index to start copying from.</param>
        public void CopyTo(System.Array array, int index)
        {
            this.collection.CopyTo(array, index);
        }

        /// <summary>
        /// Gets enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator for the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Get the schemas required to validate table definitions.
        /// </summary>
        /// <returns>The schemas required to validate table definitions.</returns>
        internal static XmlSchemaCollection GetSchemas()
        {
            if (null == schemas)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream schemaStream = assembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Xsd.tables.xsd"))
                {
                    XmlSchema schema = XmlSchema.Read(schemaStream, null);
                    schemas = new XmlSchemaCollection();
                    schemas.Add(schema);
                }
            }

            return schemas;
        }

        /// <summary>
        /// Loads a collection of table definitions from a XmlReader in memory.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>The TableDefinitionCollection represented by the xml.</returns>
        internal static TableDefinitionCollection Parse(XmlReader reader)
        {
            Debug.Assert("tableDefinitions" == reader.LocalName);

            bool empty = reader.IsEmptyElement;
            TableDefinitionCollection tableDefinitionCollection = new TableDefinitionCollection();

            while (reader.MoveToNextAttribute())
            {
                if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                {
                    throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinitions", reader.Name));
                }
            }

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
                                case "tableDefinition":
                                    tableDefinitionCollection.Add(TableDefinition.Parse(reader));
                                    break;
                                default:
                                    throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinitions", reader.Name));
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "tableDefinitions"));
                }
            }

            return tableDefinitionCollection;
        }

        /// <summary>
        /// Persists a TableDefinitionCollection in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the TableDefinitionCollection should persist itself as XML.</param>
        internal void Persist(XmlWriter writer)
        {
            writer.WriteStartElement("tableDefinitions", XmlNamespaceUri);

            foreach (TableDefinition tableDefinition in this.collection.Values)
            {
                tableDefinition.Persist(writer);
            }

            writer.WriteEndElement();
        }
    }
}
