// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Xml;

    /// <summary>
    /// Substorage inside an output.
    /// </summary>
    public sealed class SubStorage
    {
        private string name;
        private Output data;

        /// <summary>
        /// Instantiate a new substorage.
        /// </summary>
        /// <param name="name">The substorage name.</param>
        /// <param name="data">The substorage data.</param>
        public SubStorage(string name, Output data)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == data)
            {
                throw new ArgumentNullException("data");
            }

            this.name = name;
            this.data = data;
        }

        /// <summary>
        /// Gets the substorage name.
        /// </summary>
        /// <value>The substorage name.</value>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the substorage data.
        /// </summary>
        /// <value>The substorage data.</value>
        public Output Data
        {
            get { return this.data; }
        }

        /// <summary>
        /// Creates a SubStorage from the XmlReader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>New SubStorage object.</returns>
        internal static SubStorage Parse(XmlReader reader)
        {
            Debug.Assert("subStorage" == reader.LocalName);

            Output data = null;
            bool empty = reader.IsEmptyElement;
            string name = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "name":
                        name = reader.Value;
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "row", reader.Name));
                        }
                        break;
                }
            }

            // loop through all the fields in a row
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
                                case "wixOutput":
                                    data = Output.Parse(reader, true);
                                    break;
                                default:
                                    throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "row", reader.Name));
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "row"));
                }
            }

            return new SubStorage(name, data);
        }

        /// <summary>
        /// Persists a SubStorage in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the SubStorage should persist itself as XML.</param>
        internal void Persist(XmlWriter writer)
        {
            writer.WriteStartElement("subStorage", Output.XmlNamespaceUri);

            writer.WriteAttributeString("name", this.name);

            this.data.Persist(writer);

            writer.WriteEndElement();
        }
    }
}
