// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Field containing data for a column in a row.
    /// </summary>
    public class Field
    {
        private ColumnDefinition columnDefinition;
        private object data;
        private bool modified;
        private string previousData;

        /// <summary>
        /// Instantiates a new Field.
        /// </summary>
        /// <param name="columnDefinition">Column definition for this field.</param>
        protected Field(ColumnDefinition columnDefinition)
        {
            this.columnDefinition = columnDefinition;
        }

        /// <summary>
        /// Gets or sets the column definition for this field.
        /// </summary>
        /// <value>Column definition.</value>
        public ColumnDefinition Column
        {
            get { return this.columnDefinition; }
            set { this.columnDefinition = value; }
        }

        /// <summary>
        /// Gets or sets the data for this field.
        /// </summary>
        /// <value>Data in the field.</value>
        public object Data
        {
            get
            {
                return this.data;
            }

            set
            {
                // validate the value before setting it
                this.columnDefinition.ValidateValue(value);

                this.data = value;
            }
        }

        /// <summary>
        /// Gets or sets whether this field is modified.
        /// </summary>
        /// <value>Whether this field is modified.</value>
        public bool Modified
        {
            get { return this.modified; }
            set { this.modified = value; }
        }

        /// <summary>
        /// Gets or sets the previous data.
        /// </summary>
        /// <value>The previous data.</value>
        public string PreviousData
        {
            get { return this.previousData; }
            set { this.previousData = value; }
        }

        /// <summary>
        /// Sets the value of a particular field in the row without validating.
        /// </summary>
        /// <param name="field">field index.</param>
        /// <param name="value">Value of a field in the row.</param>
        /// <returns>True if successful, false if validation failed.</returns>
        public bool BestEffortSet(object value)
        {
            bool success = true;

            try
            {
                this.columnDefinition.ValidateValue(value);
            }
            catch (InvalidOperationException)
            {
                success = false;
            }

            this.data = value;
            return success;
        }

        /// <summary>
        /// Instantiate a new Field object of the correct type.
        /// </summary>
        /// <param name="columnDefinition">The column definition for the field.</param>
        /// <returns>The new Field object.</returns>
        public static Field NewField(ColumnDefinition columnDefinition)
        {
            if (ColumnType.Object == columnDefinition.Type)
            {
                return new ObjectField(columnDefinition);
            }
            else
            {
                return new Field(columnDefinition);
            }
        }

        /// <summary>
        /// Determine if this field is identical to another field.
        /// </summary>
        /// <param name="field">The other field to compare to.</param>
        /// <returns>true if they are equal; false otherwise.</returns>
        public bool IsIdentical(Field field)
        {
            return (this.columnDefinition.Name == field.columnDefinition.Name &&
                ((null != this.data && this.data.Equals(field.data)) || (null == this.data && null == field.data)));
        }

        /// <summary>
        /// Parse a field from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        internal virtual void Parse(XmlReader reader)
        {
            Debug.Assert("field" == reader.LocalName);

            bool empty = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "modified":
                        this.modified = Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "field", reader.Name, reader.Value);
                        break;
                    case "previousData":
                        this.previousData = reader.Value;
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "field", reader.Name));
                        }
                        break;
                }
            }

            if (!empty)
            {
                bool done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "field", reader.Name));
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                            if (0 < reader.Value.Length)
                            {
                                if (ColumnType.Number == this.columnDefinition.Type && !this.columnDefinition.IsLocalizable)
                                {
                                    // older wix files could persist data as a long value (which would overflow an int)
                                    // since the Convert class always throws exceptions for overflows, read in integral
                                    // values as a long to avoid the overflow, then cast it to an int (this operation can
                                    // overflow without throwing an exception inside an unchecked block)
                                    this.data = unchecked((int)Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    this.data = reader.Value;
                                }
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "field"));
                }
            }
        }

        /// <summary>
        /// Persists a field in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Field should persist itself as XML.</param>
        internal virtual void Persist(XmlWriter writer)
        {
            string text;

            // convert the data to a string that will persist nicely
            if (null == this.data)
            {
                text = String.Empty;
            }
            else
            {
                text = Convert.ToString(this.data, CultureInfo.InvariantCulture);
            }

            writer.WriteStartElement("field", Intermediate.XmlNamespaceUri);

            if (this.modified)
            {
                writer.WriteAttributeString("modified", "yes");
            }

            if (null != this.previousData)
            {
                writer.WriteAttributeString("previousData", this.previousData);
            }

            if (this.columnDefinition.UseCData)
            {
                writer.WriteCData(text);
            }
            else
            {
                writer.WriteString(text);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Returns the field data in a format usable in IDT files.
        /// </summary>
        /// <returns>Field data in string IDT format.</returns>
        internal string ToIdtValue()
        {
            if (null == this.data)
            {
                return null;
            }
            else
            {
                string fieldData = Convert.ToString(this.data, CultureInfo.InvariantCulture);

                // special idt-specific escaping
                if (this.columnDefinition.EscapeIdtCharacters)
                {
                    fieldData = fieldData.Replace('\t', '\x10');
                    fieldData = fieldData.Replace('\r', '\x11');
                    fieldData = fieldData.Replace('\n', '\x19');
                }

                return fieldData;
            }
        }
    }
}
