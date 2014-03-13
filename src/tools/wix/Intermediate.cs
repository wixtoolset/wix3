//-------------------------------------------------------------------------------------------------
// <copyright file="Intermediate.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Container class for an intermediate object.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Container class for an intermediate object.
    /// </summary>
    public sealed class Intermediate
    {
        public const string XmlNamespaceUri = "http://schemas.microsoft.com/wix/2006/objects";
        private static readonly Version currentVersion = new Version("3.0.2002.0");
        private static XmlSchemaCollection schemas;

        private SectionCollection sections;

        /// <summary>
        /// Instantiate a new Intermediate.
        /// </summary>
        public Intermediate()
        {
            this.sections = new SectionCollection();
        }

        /// <summary>
        /// Get the sections contained in this intermediate.
        /// </summary>
        /// <value>Sections contained in this intermediate.</value>
        public SectionCollection Sections
        {
            get { return this.sections; }
        }

        /// <summary>
        /// Loads an intermediate from a path on disk.
        /// </summary>
        /// <param name="path">Path to intermediate file saved on disk.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(string path, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck, bool suppressSchema)
        {
            XmlReader reader = null;

            try
            {
                reader = new XmlTextReader(path);

                if (!suppressSchema)
                {
                    reader = new XmlValidatingReader(reader);
                    ((XmlValidatingReader)reader).Schemas.Add(GetSchemas());
                }

                reader.MoveToContent();

                if ("wixObject" != reader.LocalName)
                {
                    throw new WixNotIntermediateException(WixErrors.InvalidDocumentElement(SourceLineNumberCollection.FromUri(reader.BaseURI), reader.Name, "object", "wixObject"));
                }

                return Parse(reader, tableDefinitions, suppressVersionCheck);
            }
            catch (XmlException xe)
            {
                throw new WixNotIntermediateException(WixErrors.InvalidXml(SourceLineNumberCollection.FromUri(reader.BaseURI), "object", xe.Message));
            }
            catch (XmlSchemaException xse)
            {
                throw new WixNotIntermediateException(WixErrors.SchemaValidationFailed(SourceLineNumberCollection.FromUri(reader.BaseURI), xse.Message, xse.LineNumber, xse.LinePosition));
            }
            finally
            {
                if (null != reader)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Saves an intermediate to a path on disk.
        /// </summary>
        /// <param name="path">Path to save intermediate file to disk.</param>
        public void Save(string path)
        {
            XmlWriter writer = null;
            try
            {
                // Assure the location to output the xml exists
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

                try
                {
                    writer = new XmlTextWriter(path, System.Text.Encoding.UTF8);
                }
                catch (UnauthorizedAccessException)
                {
                    throw new WixException(WixErrors.UnauthorizedAccess(path));
                }

                writer.WriteStartDocument();
                this.Persist(writer);
                writer.WriteEndDocument();
            }
            finally
            {
                if (null != writer)
                {
                    writer.Close();
                }
            }
        }

        /// <summary>
        /// Get the schemas required to validate an object.
        /// </summary>
        /// <returns>The schemas required to validate an object.</returns>
        internal static XmlSchemaCollection GetSchemas()
        {
            if (null == schemas)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream objectSchemaStream = assembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Xsd.objects.xsd"))
                {
                    XmlSchema objectSchema = XmlSchema.Read(objectSchemaStream, null);
                    schemas = new XmlSchemaCollection();
                    schemas.Add(objectSchema);
                }
            }

            return schemas;
        }

        /// <summary>
        /// Parse an intermediate from an XML format.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatch.</param>
        /// <returns>The parsed Intermediate.</returns>
        internal static Intermediate Parse(XmlReader reader, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
        {
            Debug.Assert("wixObject" == reader.LocalName);

            bool empty = reader.IsEmptyElement;
            Version objVersion = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "version":
                        objVersion = new Version(reader.Value);
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "wixObject", reader.Name));
                        }
                        break;
                }
            }

            if (null != objVersion && !suppressVersionCheck)
            {
                if (0 != currentVersion.CompareTo(objVersion))
                {
                    throw new WixException(WixErrors.VersionMismatch(SourceLineNumberCollection.FromUri(reader.BaseURI), "object", objVersion.ToString(), currentVersion.ToString()));
                }
            }

            Intermediate intermediate = new Intermediate();

            // loop through the rest of the xml building up the SectionCollection
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
                                case "section":
                                    intermediate.sections.Add(Section.Parse(reader, tableDefinitions));
                                    break;
                                default:
                                    throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "wixObject", reader.Name));
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "wixObject"));
                }
            }

            return intermediate;
        }

        /// <summary>
        /// Persists an intermediate in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
        private void Persist(XmlWriter writer)
        {
            writer.WriteStartElement("wixObject", XmlNamespaceUri);

            writer.WriteAttributeString("version", currentVersion.ToString());

            foreach (Section section in this.sections)
            {
                section.Persist(writer);
            }

            writer.WriteEndElement();
        }
    }
}
