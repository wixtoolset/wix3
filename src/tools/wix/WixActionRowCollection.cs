//-------------------------------------------------------------------------------------------------
// <copyright file="WixActionRowCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// A collection of action rows sorted by their sequence table and action name.
// </summary>
//-------------------------------------------------------------------------------------------------

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
    /// A collection of action rows sorted by their sequence table and action name.
    /// </summary>
    internal sealed class WixActionRowCollection : ICollection
    {
        private static XmlSchemaCollection schemas;

        private SortedList collection;

        /// <summary>
        /// Creates a new action table object.
        /// </summary>
        public WixActionRowCollection()
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
            get { return this; }
        }

        /// <summary>
        /// Get an ActionRow by its sequence table and action name.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        public WixActionRow this[SequenceTable sequenceTable, string action]
        {
            get { return (WixActionRow)this.collection[GetKey(sequenceTable, action)]; }
        }

        /// <summary>
        /// Add an ActionRow to the collection.
        /// </summary>
        /// <param name="actionRow">The ActionRow to add.</param>
        /// <param name="overwrite">true to overwrite an existing ActionRow; false otherwise.</param>
        public void Add(WixActionRow actionRow, bool overwrite)
        {
            string key = GetKey(actionRow.SequenceTable, actionRow.Action);

            if (overwrite)
            {
                this.collection[key] = actionRow;
            }
            else
            {
                this.collection.Add(key, actionRow);
            }
        }

        /// <summary>
        /// Add an ActionRow to the collection.
        /// </summary>
        /// <param name="actionRow">The ActionRow to add.</param>
        public void Add(WixActionRow actionRow)
        {
            this.Add(actionRow, false);
        }

        /// <summary>
        /// Determines if the collection contains an ActionRow with a specific sequence table and name.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        /// <returns>true if the ActionRow was found; false otherwise.</returns>
        public bool Contains(SequenceTable sequenceTable, string action)
        {
            return this.collection.Contains(GetKey(sequenceTable, action));
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">Array to copy the collection into.</param>
        /// <param name="index">Index to start copying from.</param>
        public void CopyTo(System.Array array, int index)
        {
            this.collection.Values.CopyTo(array, index);
        }

        /// <summary>
        /// Gets the enumerator for the collection.
        /// </summary>
        /// <returns>The enumerator for the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Remove an ActionRow from the collection.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        public void Remove(SequenceTable sequenceTable, string action)
        {
            this.collection.Remove(GetKey(sequenceTable, action));
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

                using (Stream schemaStream = assembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Xsd.actions.xsd"))
                {
                    XmlSchema schema = XmlSchema.Read(schemaStream, null);
                    schemas = new XmlSchemaCollection();
                    schemas.Add(schema);
                }
            }

            return schemas;
        }

        /// <summary>
        /// Load an action table from an XmlReader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>The ActionRowCollection represented by the xml.</returns>
        internal static WixActionRowCollection Load(XmlReader reader, bool suppressSchema)
        {
            if (null == reader)
            {
                throw new ArgumentNullException("reader");
            }

            if (!suppressSchema)
            {
                reader = new XmlValidatingReader(reader);
                ((XmlValidatingReader)reader).Schemas.Add(GetSchemas());
            }

            reader.MoveToContent();

            if ("actions" != reader.LocalName)
            {
                throw new WixException(WixErrors.InvalidDocumentElement(SourceLineNumberCollection.FromUri(reader.BaseURI), reader.Name, "actions", "actions"));
            }

            return Parse(reader);
        }

        /// <summary>
        /// Creates a new action table object and populates it from an Xml reader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>The parsed ActionTable.</returns>
        private static WixActionRowCollection Parse(XmlReader reader)
        {
            Debug.Assert("actions" == reader.LocalName);

            WixActionRowCollection actionRows = new WixActionRowCollection();
            bool empty = reader.IsEmptyElement;

            // there are no legal attributes
            while (reader.MoveToNextAttribute())
            {
                if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                {
                    throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "actions", reader.Name));
                }
            }

            if (!empty)
            {
                bool done = false;

                // loop through all the fields in a row
                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "action":
                                WixActionRow[] parsedActionRows = WixActionRow.Parse(reader);

                                foreach (WixActionRow actionRow in parsedActionRows)
                                {
                                    actionRows.Add(actionRow);
                                }
                                break;
                            default:
                                throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "actions", reader.Name));
                        }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "actions"));
                }
            }

            return actionRows;
        }

        /// <summary>
        /// Get the key for storing an ActionRow.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        /// <returns>The string key.</returns>
        private static string GetKey(SequenceTable sequenceTable, string action)
        {
            return GetKey(sequenceTable.ToString(), action);
        }

        /// <summary>
        /// Get the key for storing an ActionRow.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        /// <returns>The string key.</returns>
        private static string GetKey(string sequenceTable, string action)
        {
            return String.Concat(sequenceTable, '/', action);
        }
    }
}