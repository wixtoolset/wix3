//-------------------------------------------------------------------------------------------------
// <copyright file="ScanResult.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Windows Installer Xml toolset scanner result.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Generic;

    public class ScanResult
    {
        public ScanResult()
        {
            this.Unresolved = new List<ScannedUnresolvedReference>();

            this.ProjectFiles = new Dictionary<string, ScannedProject>();
            this.SourceFiles = new Dictionary<string, ScannedSourceFile>();
            this.Symbols = new Dictionary<string, ScannedSymbol>();
            this.UnknownFiles = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            //this.ProjectToProjectReferences = new SortedSet<ScannedProjectProjectReference>();
            //this.ProjectToSourceFileReferences = new SortedSet<ScannedProjectSourceFileReference>();
            //this.SourceFileToSymbolReference = new SortedSet<ScannedSourceFileSymbolReference>();
            //this.SymbolToSymbolReference = new SortedSet<ScannedSymbolSymbolReference>();
        }

        public IDictionary<string, ScannedProject> ProjectFiles { get; private set; }

        public IDictionary<string, ScannedSourceFile> SourceFiles { get; private set; }

        public IDictionary<string, ScannedSymbol> Symbols { get; private set; }

        public ISet<string> UnknownFiles { get; private set; }

        public List<ScannedUnresolvedReference> Unresolved { get; private set; }

        //public SortedSet<ScannedProjectProjectReference> ProjectToProjectReferences { get; private set; }

        //public SortedSet<ScannedProjectSourceFileReference> ProjectToSourceFileReferences { get; private set; }

        //public SortedSet<ScannedSourceFileSymbolReference> SourceFileToSymbolReference { get; private set; }

        //public SortedSet<ScannedSymbolSymbolReference> SymbolToSymbolReference { get; private set; }

        public void FilterSymbols(ISet<string> includes, ISet<string> excludes)
        {
            // If nothing was requested to be included then assume they want the whole set.
            IDictionary<string, ScannedSymbol> included;
            if (includes == null || includes.Count == 0)
            {
                included = this.Symbols;
            }
            else // include only the requested symbols.
            {
                includes.ExceptWith(excludes);

                included = new Dictionary<string, ScannedSymbol>();
                foreach (string include in includes)
                {
                    this.IncludeReferences(include, included);
                }

                this.Symbols = included;
            }

            if (excludes != null)
            {
                foreach (string exclude in excludes)
                {
                    this.ExcludeReferences(exclude);
                }
            }
        }

        private void IncludeReferences(string include, IDictionary<string, ScannedSymbol> included)
        {
            ScannedSymbol symbol;
            if (this.Symbols.TryGetValue(include, out symbol))
            {
                included.Add(symbol.Key, symbol);
                foreach (ScannedSymbol referencedSymbol in symbol.TargetSymbols)
                {
                    this.IncludeReferences(referencedSymbol.Key, included);
                }
            }
        }

        private void ExcludeReferences(string exclude)
        {
            ScannedSymbol symbol;
            if (this.Symbols.TryGetValue(exclude, out symbol))
            {
                // Remove references from the source symbols to this symbol.
                foreach (ScannedSymbol reference in symbol.SourceSymbols)
                {
                    reference.TargetSymbols.Remove(symbol);
                }

                symbol.SourceSymbols.Clear();

                // Recursively remove target symbols (which each will remove this
                // symbol from targeting them).
                while (symbol.TargetSymbols.Count > 0)
                {
                    ExcludeReferences(symbol.TargetSymbols[0].Key);
                }

                symbol.Excluded = true;
            }
        }
    }
}