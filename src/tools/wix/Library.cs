//-------------------------------------------------------------------------------------------------
// <copyright file="Library.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Object that represents a library file.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml.Cab;

    /// <summary>
    /// Object that represents a library file.
    /// </summary>
    public sealed class Library
    {
        public const string XmlNamespaceUri = "http://schemas.microsoft.com/wix/2006/libraries";
        private static readonly Version currentVersion = new Version("3.0.2002.0");
        private static XmlSchemaCollection schemas;

        private Hashtable localizations;
        private SectionCollection sections;

        /// <summary>
        /// Instantiate a new Library.
        /// </summary>
        public Library()
        {
            this.localizations = new Hashtable();
            this.sections = new SectionCollection();
        }

        /// <summary>
        /// Get the sections contained in this library.
        /// </summary>
        /// <value>Sections contained in this library.</value>
        public SectionCollection Sections
        {
            get { return this.sections; }
        }

        /// <summary>
        /// Loads a library from a path on disk.
        /// </summary>
        /// <param name="path">Path to library file saved on disk.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
        /// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>Returns the loaded library.</returns>
        /// <remarks>This method will set the Path and SourcePath properties to the appropriate values on successful load.</remarks>
        public static Library Load(string path, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck, bool suppressSchema)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                return Load(stream, new Uri(Path.GetFullPath(path)), tableDefinitions, suppressVersionCheck, suppressSchema);
            }
        }

        /// <summary>
        /// Add a localization file to this library.
        /// </summary>
        /// <param name="localization">The localization file to add.</param>
        public void AddLocalization(Localization localization)
        {
            if (!this.localizations.Contains(localization.Culture))
            {
                this.localizations.Add(localization.Culture, localization);
            }
            else
            {
                Localization existingCulture = (Localization)this.localizations[localization.Culture];
                existingCulture.Merge(localization);
            }
        }

        /// <summary>
        /// Gets localization files from this library that match the cultures passed in, in the order of the array of cultures.
        /// </summary>
        /// <param name="cultures">The list of cultures to get localizations for.</param>
        /// <returns>All localizations contained in this library that match the set of cultures provided, in the same order.</returns>
        public ReadOnlyCollection<Localization> GetLocalizations(string[] cultures)
        {
            List<Localization> resultLocs = new List<Localization>();

            foreach (string culture in cultures)
            {
                if (this.localizations.Contains(culture))
                {
                    resultLocs.Add((Localization)this.localizations[culture]);
                }
            }

            ReadOnlyCollection<Localization> returnLocs = new ReadOnlyCollection<Localization>(resultLocs);

            return (ReadOnlyCollection<Localization>)returnLocs;
        }

        /// <summary>
        /// Adds to a localizer the localization files from this library that match the cultures passed in, in the order of the array of cultures.
        /// </summary>
        /// <param name="cultures">The list of cultures to get localizations for.</param>
        /// <param name="localizer">The localizer to add the localizations to.</param>
        public void GetLocalizations(string[] cultures, Localizer localizer)
        {
            if (null != cultures && null != localizer)
            {
                foreach (string culture in cultures)
                {
                    if (this.localizations.Contains(culture))
                    {
                        localizer.AddLocalization((Localization)this.localizations[culture]);
                    }
                }
            }
        }

        /// <summary>
        /// Saves a library to a path on disk.
        /// </summary>
        /// <param name="path">Path to save library file to on disk.</param>
        /// <param name="binderFileManager">If provided, the binder file manager is used to bind files into the library.</param>
        /// <param name="wixVariableResolver">The Wix variable resolver.</param>
        public void Save(string path, BinderFileManager binderFileManager, WixVariableResolver wixVariableResolver)
        {
            FileMode fileMode = FileMode.Create;
            StringCollection fileIds = new StringCollection();
            StringCollection files = new StringCollection();
            int index = 0;

            // resolve paths to files and create the library cabinet file
            foreach (Section section in this.sections)
            {
                foreach (Table table in section.Tables)
                {
                    foreach (Row row in table.Rows)
                    {
                        foreach (Field field in row.Fields)
                        {
                            ObjectField objectField = field as ObjectField;

                            if (null != objectField)
                            {
                                if (null != binderFileManager && null != objectField.Data)
                                {
                                    string cabinetFileId = (index++).ToString(CultureInfo.InvariantCulture);

                                    // resolve wix variables
                                    string resolvedValue = wixVariableResolver.ResolveVariables(row.SourceLineNumbers, (string)objectField.Data, false);
                                    files.Add(binderFileManager.ResolveFile(resolvedValue, table.Name, row.SourceLineNumbers, BindStage.Normal));

                                    // File was successfully resolved so track this cabient file id.
                                    objectField.CabinetFileId = cabinetFileId;
                                    fileIds.Add(cabinetFileId);
                                }
                                else // clear out a previous cabinet file id value
                                {
                                    objectField.CabinetFileId = null;
                                }
                            }
                        }
                    }
                }
            }

            // do not save the library if errors were found while resolving object paths
            if (wixVariableResolver.EncounteredError)
            {
                return;
            }

            // create the cabinet file
            if (0 < fileIds.Count)
            {
                using (WixCreateCab cab = new WixCreateCab(Path.GetFileName(path), Path.GetDirectoryName(path), fileIds.Count, 0, 0, CompressionLevel.Mszip))
                {
                    for (int i = 0; i < fileIds.Count; i++)
                    {
                        cab.AddFile(files[i], fileIds[i]);
                    }
                    cab.Complete();
                }

                // append the library xml to the end of the newly created cabinet file
                fileMode = FileMode.Append;
            }

            // Assure the location to output the lib exists
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            // save the xml
            using (FileStream fs = new FileStream(path, fileMode))
            {
                XmlWriter writer = null;

                try
                {
                    writer = new XmlTextWriter(fs, System.Text.Encoding.UTF8);

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
        }

        /// <summary>
        /// Loads a library from a path on disk.
        /// </summary>
        /// <param name="stream">Stream containing the library file.</param>
        /// <param name="uri">Uri for finding this stream.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
        /// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>Returns the loaded library.</returns>
        /// <remarks>This method will set the Path and SourcePath properties to the appropriate values on successful load.</remarks>
        internal static Library Load(Stream stream, Uri uri, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck, bool suppressSchema)
        {
            XmlReader reader = null;

            // look for the Microsoft cabinet file header and skip past the cabinet data if found
            if ('M' == stream.ReadByte() && 'S' == stream.ReadByte() && 'C' == stream.ReadByte() && 'F' == stream.ReadByte())
            {
                long offset = 0;
                byte[] offsetBuffer = new byte[4];

                // skip the header checksum
                stream.Seek(4, SeekOrigin.Current);

                // read the cabinet file size
                stream.Read(offsetBuffer, 0, 4);
                offset = BitConverter.ToInt32(offsetBuffer, 0);

                // seek past the cabinet file to the xml
                stream.Seek(offset, SeekOrigin.Begin);
            }
            else // plain xml file - start reading xml at the beginning of the stream
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            // read the xml
            try
            {
                reader = new XmlTextReader(uri.AbsoluteUri, stream);

                if (!suppressSchema)
                {
                    reader = new XmlValidatingReader(reader);
                    ((XmlValidatingReader)reader).Schemas.Add(GetSchemas());
                }

                reader.MoveToContent();

                if ("wixLibrary" != reader.LocalName)
                {
                    throw new WixNotLibraryException(WixErrors.InvalidDocumentElement(SourceLineNumberCollection.FromUri(reader.BaseURI), reader.Name, "library", "wixLibrary"));
                }

                return Parse(reader, tableDefinitions, suppressVersionCheck);
            }
            catch (XmlException xe)
            {
                throw new WixException(WixErrors.InvalidXml(SourceLineNumberCollection.FromUri(reader.BaseURI), "object", xe.Message));
            }
            catch (XmlSchemaException xse)
            {
                throw new WixException(WixErrors.SchemaValidationFailed(SourceLineNumberCollection.FromUri(reader.BaseURI), xse.Message, xse.LineNumber, xse.LinePosition));
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
        /// Get the schemas required to validate a library.
        /// </summary>
        /// <returns>The schemas required to validate a library.</returns>
        internal static XmlSchemaCollection GetSchemas()
        {
            if (null == schemas)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream librarySchemaStream = assembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Xsd.libraries.xsd"))
                {
                    schemas = new XmlSchemaCollection();
                    schemas.Add(Intermediate.GetSchemas());
                    schemas.Add(Localization.GetSchemas());
                    XmlSchema librarySchema = XmlSchema.Read(librarySchemaStream, null);
                    schemas.Add(librarySchema);
                }
            }

            return schemas;
        }

        /// <summary>
        /// Parse the root library element.
        /// </summary>
        /// <param name="reader">XmlReader with library persisted as Xml.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
        /// <param name="suppressVersionCheck">Suppresses check for wix.dll version mismatch.</param>
        /// <returns>The parsed Library.</returns>
        private static Library Parse(XmlReader reader, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
        {
            Debug.Assert("wixLibrary" == reader.LocalName);

            bool empty = reader.IsEmptyElement;
            Library library = new Library();
            Version version = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "version":
                        version = new Version(reader.Value);
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "wixLibrary", reader.Name));
                        }
                        break;
                }
            }

            if (null != version && !suppressVersionCheck)
            {
                if (0 != currentVersion.CompareTo(version))
                {
                    throw new WixException(WixErrors.VersionMismatch(SourceLineNumberCollection.FromUri(reader.BaseURI), "library", version.ToString(), currentVersion.ToString()));
                }
            }

            if (!empty)
            {
                bool done = false;

                while (!done && (XmlNodeType.Element == reader.NodeType || reader.Read()))
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "section":
                                    library.sections.Add(Section.Parse(reader, tableDefinitions));
                                    break;
                                case "WixLocalization":
                                    Localization localization = Localization.Parse(reader, tableDefinitions);
                                    library.localizations.Add(localization.Culture, localization);
                                    break;
                                default:
                                    throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "wixLibrary", reader.Name));
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "wixLibrary"));
                }
            }

            return library;
        }

        /// <summary>
        /// Persists a library in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the library should persist itself as XML.</param>
        private void Persist(XmlWriter writer)
        {
            writer.WriteStartElement("wixLibrary", XmlNamespaceUri);

            writer.WriteAttributeString("version", currentVersion.ToString());

            foreach (Localization localization in this.localizations.Values)
            {
                localization.Persist(writer);
            }

            foreach (Section section in this.sections)
            {
                section.Persist(writer);
            }

            writer.WriteEndElement();
        }
    }
}
