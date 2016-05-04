// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Type of section.
    /// </summary>
    public enum SectionType
    {
        /// <summary>Unknown section type, default and invalid.</summary>
        Unknown,

        /// <summary>Bundle section type.</summary>
        Bundle,

        /// <summary>Fragment section type.</summary>
        Fragment,

        /// <summary>Module section type.</summary>
        Module,

        /// <summary>Product section type.</summary>
        Product,

        /// <summary>Patch creation section type.</summary>
        PatchCreation,

        /// <summary>Patch section type.</summary>
        Patch
    }

    /// <summary>
    /// Section in an object file.
    /// </summary>
    public sealed class Section
    {
        private string id;
        private SectionType type;
        private int codepage;

        private TableCollection tables;

        private SourceLineNumberCollection sourceLineNumbers;
        private SymbolCollection symbols;

        /// <summary>
        /// Creates a new section as part of an intermediate.
        /// </summary>
        /// <param name="id">Identifier for section.</param>
        /// <param name="type">Type of section.</param>
        /// <param name="codepage">Codepage for resulting database.</param>
        public Section(string id, SectionType type, int codepage)
        {
            this.id = id;
            this.type = type;
            this.codepage = codepage;

            this.tables = new TableCollection();
        }

        /// <summary>
        /// Gets the identifier for the section.
        /// </summary>
        /// <value>Section identifier.</value>
        public string Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the type of the section.
        /// </summary>
        /// <value>Type of section.</value>
        public SectionType Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Gets the codepage for the section.
        /// </summary>
        /// <value>Codepage for the section.</value>
        public int Codepage
        {
            get { return this.codepage; }
        }

        /// <summary>
        /// Gets the tables in the section.
        /// </summary>
        /// <value>Tables in section.</value>
        public TableCollection Tables
        {
            get { return this.tables; }
        }

        /// <summary>
        /// Gets the source line information of the file containing this section.
        /// </summary>
        /// <value>The source line information of the file containing this section.</value>
        public SourceLineNumberCollection SourceLineNumbers
        {
            get { return this.sourceLineNumbers; }
        }

        /// <summary>
        /// Parse a section from the xml.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
        /// <returns>The parsed Section.</returns>
        internal static Section Parse(XmlReader reader, TableDefinitionCollection tableDefinitions)
        {
            Debug.Assert("section" == reader.LocalName);

            int codepage = 0;
            bool empty = reader.IsEmptyElement;
            string id = null;
            Section section = null;
            SectionType type = SectionType.Unknown;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "codepage":
                        codepage = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "id":
                        id = reader.Value;
                        break;
                    case "type":
                        switch (reader.Value)
                        {
                            case "bundle":
                                type = SectionType.Bundle;
                                break;
                            case "fragment":
                                type = SectionType.Fragment;
                                break;
                            case "module":
                                type = SectionType.Module;
                                break;
                            case "patchCreation":
                                type = SectionType.PatchCreation;
                                break;
                            case "product":
                                type = SectionType.Product;
                                break;
                            case "patch":
                                type = SectionType.Patch;
                                break;
                            default:
                                throw new WixException(WixErrors.IllegalAttributeValue(SourceLineNumberCollection.FromUri(reader.BaseURI), "section", reader.Name, reader.Value, "fragment", "module", "patchCreation", "product", "patch"));
                        }
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "section", reader.Name));
                        }
                        break;
                }
            }

            if (null == id && (SectionType.Unknown != type && SectionType.Fragment != type))
            {
                throw new WixException(WixErrors.ExpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "section", "id", "type", type.ToString()));
            }

            if (SectionType.Unknown == type)
            {
                throw new WixException(WixErrors.ExpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "section", "type"));
            }

            section = new Section(id, type, codepage);
            section.sourceLineNumbers = SourceLineNumberCollection.FromUri(reader.BaseURI);

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
                                case "table":
                                    section.Tables.Add(Table.Parse(reader, section, tableDefinitions));
                                    break;
                                default:
                                    throw new WixException(WixErrors.UnexpectedElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "section", reader.Name));
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new WixException(WixErrors.ExpectedEndElement(SourceLineNumberCollection.FromUri(reader.BaseURI), "section"));
                }
            }

            return section;
        }

        /// <summary>
        /// Persist the Section to an XmlWriter.
        /// </summary>
        /// <param name="writer">XmlWriter which reference will be persisted to.</param>
        internal void Persist(XmlWriter writer)
        {
            writer.WriteStartElement("section", Intermediate.XmlNamespaceUri);

            if (null != this.id)
            {
                writer.WriteAttributeString("id", this.id);
            }

            switch (this.type)
            {
                case SectionType.Bundle:
                    writer.WriteAttributeString("type", "bundle");
                    break;
                case SectionType.Fragment:
                    writer.WriteAttributeString("type", "fragment");
                    break;
                case SectionType.Module:
                    writer.WriteAttributeString("type", "module");
                    break;
                case SectionType.Product:
                    writer.WriteAttributeString("type", "product");
                    break;
                case SectionType.PatchCreation:
                    writer.WriteAttributeString("type", "patchCreation");
                    break;
                case SectionType.Patch:
                    writer.WriteAttributeString("type", "patch");
                    break;
            }

            if (0 != this.codepage)
            {
                writer.WriteAttributeString("codepage", this.codepage.ToString(CultureInfo.InvariantCulture));
            }

            // don't need to persist the symbols since they are recreated during load

            // save the rows in table order
            foreach (Table table in this.tables)
            {
                table.Persist(writer);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Gets the symbols for this section.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        /// <returns>Collection of symbols for this section.</returns>
        internal SymbolCollection GetSymbols(IMessageHandler messageHandler)
        {
            if (null == this.symbols)
            {
                this.symbols = new SymbolCollection();

                foreach (Table table in this.tables)
                {
                    foreach (Row row in table.Rows)
                    {
                        Symbol symbol = row.Symbol;
                        if (null != symbol)
                        {
                            try
                            {
                                this.symbols.Add(symbol);
                            }
                            catch (ArgumentException)
                            {
                                Symbol existingSymbol = this.symbols[symbol.Name];

                                messageHandler.OnMessage(WixErrors.DuplicateSymbol(existingSymbol.Row.SourceLineNumbers, existingSymbol.Name));

                                if (null != symbol.Row.SourceLineNumbers)
                                {
                                    messageHandler.OnMessage(WixErrors.DuplicateSymbol2(symbol.Row.SourceLineNumbers));
                                }
                            }
                        }
                    }
                }
            }

            return this.symbols;
        }

        /// <summary>
        /// Resolves all the simple references in a section.
        /// </summary>
        /// <param name="outputType">Parent output type that will get the resolved section collection.</param>
        /// <param name="allSymbols">Collection of all symbols from loaded intermediates.</param>
        /// <param name="referencedSymbols">Collection populated during resolution of all symbols referenced during linking.</param>
        /// <param name="unresolvedReferences">Collection populated during resolution of all references that are left unresolved.</param>
        /// <param name="messageHandler">Message handler to report any duplicate symbols that may be tripped across.</param>
        /// <returns>The resolved sections.</returns>
        internal SectionCollection ResolveReferences(
            OutputType outputType,
            SymbolCollection allSymbols,
            StringCollection referencedSymbols,
            ArrayList unresolvedReferences,
            IMessageHandler messageHandler)
        {
            SectionCollection sections = new SectionCollection();

            RecursivelyResolveReferences(this, outputType, allSymbols, sections, referencedSymbols, unresolvedReferences, messageHandler);
            return sections;
        }

        /// <summary>
        /// Recursive helper function to resolve all references of passed in section.
        /// </summary>
        /// <param name="section">Section with references to resolve.</param>
        /// <param name="outputType">Parent output type that will get the resolved section collection.</param>
        /// <param name="allSymbols">All symbols that can be used to resolve section's references.</param>
        /// <param name="sections">Collection to add sections to during processing.</param>
        /// <param name="referencedSymbols">Collection populated during resolution of all symbols referenced during linking.</param>
        /// <param name="unresolvedReferences">Collection populated during resolution of all references that are left unresolved.</param>
        /// <param name="messageHandler">Message handler to report any duplicate symbols that may be tripped across.</param>
        /// <remarks>Note: recursive function.</remarks>
        private static void RecursivelyResolveReferences(
            Section section,
            OutputType outputType,
            SymbolCollection allSymbols,
            SectionCollection sections,
            StringCollection referencedSymbols,
            ArrayList unresolvedReferences,
            IMessageHandler messageHandler)
        {
            // if we already have this section bail
            if (sections.Contains(section))
            {
                return;
            }

            // add the passed in section to the collection of sections
            sections.Add(section);

            // process all of the references contained in this section using the collection of
            // symbols provided.  Then recursively call this method to process the
            // located symbol's section.  All in all this is a very simple depth-first
            // search of the references per-section
            Table wixSimpleReferenceTable = section.Tables["WixSimpleReference"];
            if (null != wixSimpleReferenceTable)
            {
                foreach (WixSimpleReferenceRow wixSimpleReferenceRow in wixSimpleReferenceTable.Rows)
                {
                    // If we're building a Merge Module, ignore all references to the Media table
                    // because Merge Modules don't have Media tables.
                    if (OutputType.Module == outputType && "Media" == wixSimpleReferenceRow.TableName)
                    {
                        continue;
                    }

                    if ("WixAction" == wixSimpleReferenceRow.TableName)
                    {
                        Symbol[] symbols = allSymbols.GetSymbolsForSimpleReference(wixSimpleReferenceRow);
                        if (0 == symbols.Length)
                        {
                            if (null != unresolvedReferences)
                            {
                                unresolvedReferences.Add(new SimpleReferenceSection(section, wixSimpleReferenceRow));
                            }
                        }
                        else
                        {
                            foreach (Symbol symbol in symbols)
                            {
                                if (null != symbol.Section)
                                {
                                    // components are indexed in ResolveComplexReferences
                                    if (null != referencedSymbols && null != symbol.Row.TableDefinition.Name && "Component" != symbol.Row.TableDefinition.Name && !referencedSymbols.Contains(symbol.Name))
                                    {
                                        referencedSymbols.Add(symbol.Name);
                                    }

                                    RecursivelyResolveReferences(symbol.Section, outputType, allSymbols, sections, referencedSymbols, unresolvedReferences, messageHandler);
                                }
                            }
                        }
                    }
                    else
                    {
                        Symbol symbol = allSymbols.GetSymbolForSimpleReference(wixSimpleReferenceRow, messageHandler);
                        if (null == symbol)
                        {
                            if (null != unresolvedReferences)
                            {
                                unresolvedReferences.Add(new SimpleReferenceSection(section, wixSimpleReferenceRow));
                            }
                        }
                        else
                        {
                            // components are indexed in ResolveComplexReferences
                            if (null != referencedSymbols && null != symbol.Row.TableDefinition.Name && "Component" != symbol.Row.TableDefinition.Name && !referencedSymbols.Contains(symbol.Name))
                            {
                                referencedSymbols.Add(symbol.Name);
                            }

                            RecursivelyResolveReferences(symbol.Section, outputType, allSymbols, sections, referencedSymbols, unresolvedReferences, messageHandler);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper class to keep track of simple references in their section.
        /// </summary>
        internal struct SimpleReferenceSection
        {
            public Section Section;
            public WixSimpleReferenceRow WixSimpleReferenceRow;

            /// <summary>
            /// Creates an object that ties simple references to their section.
            /// </summary>
            /// <param name="section">Section that owns the simple reference.</param>
            /// <param name="wixSimpleReferenceRow">The simple reference in the section.</param>
            public SimpleReferenceSection(Section section, WixSimpleReferenceRow wixSimpleReferenceRow)
            {
                this.Section = section;
                this.WixSimpleReferenceRow = wixSimpleReferenceRow;
            }
        }
    }
}
