// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Hash table collection of symbols.
    /// </summary>
    public sealed class SymbolCollection : ICollection
    {
        private Hashtable collection;

        /// <summary>
        /// Created a new SymbolCollection.
        /// </summary>
        public SymbolCollection()
        {
            this.collection = new Hashtable();
        }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        /// <value>Number of elements in collection.</value>
        public int Count
        {
            get { return this.collection.Count; }
        }

        /// <summary>
        /// Gets the keys of the hash table.
        /// </summary>
        /// <value>Collection of keys.</value>
        public ICollection Keys
        {
            get { return this.collection.Keys; }
        }

        /// <summary>
        /// Gets if this collection has been synchronized.
        /// </summary>
        /// <value>true if collection has been synchronized.</value>
        public bool IsSynchronized
        {
            get { return this.collection.IsSynchronized; }
        }

        /// <summary>
        /// Gets the synchronization object for this collection.
        /// </summary>
        /// <value>Object for synchronization.</value>
        public object SyncRoot
        {
            get { return this.collection.SyncRoot; }
        }

        /// <summary>
        /// Gets a symbol by name from the collection.
        /// </summary>
        /// <param name="symbolName">Name of symbol to find.</param>
        /// <exception cref="DuplicateSymbolsException">If the symbol is duplicated a DuplicateSymbolsException is thrown.</exception>
        public Symbol this[string symbolName]
        {
            get
            {
                Symbol symbol = this.collection[symbolName] as Symbol;
                if (null == symbol)
                {
                    ArrayList symbols = (ArrayList)this.collection[symbolName];
                    if (null != symbols)
                    {
                        throw new DuplicateSymbolsException(symbols);
                    }
                }

                return symbol;
            }
        }

        /// <summary>
        /// Adds a symbol to the collection.
        /// </summary>
        /// <param name="symbol">Symbol to add collection.</param>
        /// <remarks>Add symbol to hash by name.</remarks>
        public void Add(Symbol symbol)
        {
            if (null == symbol)
            {
                throw new ArgumentNullException("symbol");
            }

            this.collection.Add(symbol.Name, symbol);
        }

        /// <summary>
        /// Adds a symbol to the collection.
        /// </summary>
        /// <param name="symbol">Symbol to add collection.</param>
        /// <remarks>Add symbol to hash by name.</remarks>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public void AddDuplicate(Symbol symbol)
        {
            if (null == symbol)
            {
                throw new ArgumentNullException("symbol");
            }

            ArrayList symbols;
            object o = this.collection[symbol.Name];
            if (null == o)
            {
                throw new InvalidOperationException(WixStrings.EXP_DidnotFindDuplicateSymbol);
            }
            else
            {
                symbols = o as ArrayList;
                if (null == symbols)
                {
                    symbols = new ArrayList();
                    symbols.Add((Symbol) o);
                }
            }

            symbols.Add(symbol);
            this.collection[symbol.Name] = symbols;
        }

        /// <summary>
        /// Checks if collection contains a symbol name.
        /// </summary>
        /// <param name="symbolName">Symbol name to check in collection.</param>
        /// <returns>true if collection contains the symbol name.</returns>
        public bool Contains(string symbolName)
        {
            return this.collection.Contains(symbolName);
        }

        /// <summary>
        /// Copies collection to array.
        /// </summary>
        /// <param name="array">Array to copy collection into.</param>
        /// <param name="index">Index to start copying at.</param>
        public void CopyTo(System.Array array, int index)
        {
            this.collection.CopyTo(array, index);
        }

        /// <summary>
        /// Gets an enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator for collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Gets the symbol for a reference.
        /// </summary>
        /// <param name="wixSimpleReferenceRow">Simple references to resolve.</param>
        /// <param name="messageHandler">Message handler to report errors through.</param>
        /// <returns>Symbol if it was found or null if the symbol was not specified.</returns>
        internal Symbol GetSymbolForSimpleReference(WixSimpleReferenceRow wixSimpleReferenceRow, IMessageHandler messageHandler)
        {
            Symbol symbol = null;

            try
            {
                symbol = this[wixSimpleReferenceRow.SymbolicName];
            }
            catch (DuplicateSymbolsException e)
            {
                Hashtable uniqueSourceLineNumbers = new Hashtable();
                Symbol[] duplicateSymbols = e.GetDuplicateSymbols();
                Debug.Assert(1 < duplicateSymbols.Length);

                // index the row source line numbers to determine how many are unique
                foreach (Symbol duplicateSymbol in duplicateSymbols)
                {
                    if (null != duplicateSymbol.Row && null != duplicateSymbol.Row.SourceLineNumbers)
                    {
                        uniqueSourceLineNumbers[duplicateSymbol.Row.SourceLineNumbers] = null;
                    }
                }

                // if only 1 unique source line number was found, switch to the section source line numbers
                // (sections use the file name of the intermediate, library, or extension they came from)
                if (1 >= uniqueSourceLineNumbers.Count)
                {
                    uniqueSourceLineNumbers.Clear();

                    foreach (Symbol duplicateSymbol in duplicateSymbols)
                    {
                        if (null != duplicateSymbol.Section.SourceLineNumbers)
                        {
                            uniqueSourceLineNumbers[duplicateSymbol.Section.SourceLineNumbers] = null;
                        }
                    }
                }

                // display errors for the unique source line numbers
                bool displayedFirstError = false;
                foreach (SourceLineNumberCollection sourceLineNumbers in uniqueSourceLineNumbers.Keys)
                {
                    if (!displayedFirstError)
                    {
                        messageHandler.OnMessage(WixErrors.DuplicateSymbol(sourceLineNumbers, duplicateSymbols[0].Name));
                        displayedFirstError = true;
                    }
                    else
                    {
                        messageHandler.OnMessage(WixErrors.DuplicateSymbol2(sourceLineNumbers));
                    }
                }

                // display an error, even if no source line information was found
                if (!displayedFirstError)
                {
                    messageHandler.OnMessage(WixErrors.DuplicateSymbol(null, duplicateSymbols[0].Name));
                }
            }

            return symbol;
        }

        /// <summary>
        /// Gets an array of matching symbols for a reference.
        /// </summary>
        /// <param name="wixSimpleReferenceRow">Simple references to resolve.</param>
        /// <returns>Symbols if they were found or empty array if the symbols were not found.</returns>
        internal Symbol[] GetSymbolsForSimpleReference(WixSimpleReferenceRow wixSimpleReferenceRow)
        {
            Symbol[] symbols = null;

            try
            {
                Symbol symbol = this[wixSimpleReferenceRow.SymbolicName];

                if (null == symbol)
                {
                    symbols = new Symbol[0];
                }
                else
                {
                    symbols = new Symbol[1];
                    symbols[0] = symbol;
                }
            }
            catch (DuplicateSymbolsException e)
            {
                symbols = e.GetDuplicateSymbols();
            }

            return symbols;
        }
        /// <summary>
        /// Outputs the symbols loaded in the SymbolCollection along with other information.
        /// </summary>
        /// <param name="outputPath">Path to create symbols file.</param>
        internal void OutputSymbols(string outputPath)
        {
            if (null == outputPath)
            {
                throw new ArgumentNullException("outputPath");
            }

            string filename = Path.GetFileName(outputPath);
            if (null == filename)
            {
                return;
            }

            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            FileMode fileMode = FileMode.Create;
            using (FileStream fs = new FileStream(outputPath, fileMode))
            {
                XmlWriter writer = null;
                try
                {
                    writer = new XmlTextWriter(fs, System.Text.Encoding.UTF8);
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Symbols");
                    // Output the contents
                    foreach (Symbol symbol in this.collection.Values)
                    {
                        writer.WriteStartElement("Symbol");
                        writer.WriteAttributeString("Id", symbol.Name);
                        writer.WriteAttributeString("SourceLineNumber", symbol.Row.SourceLineNumbers.EncodedSourceLineNumbers);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
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
    }
}
