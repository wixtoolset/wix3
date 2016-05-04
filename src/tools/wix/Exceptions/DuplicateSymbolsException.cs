// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;

    /// <summary>
    /// Duplicate symbols exception.
    /// </summary>
    [Serializable]
    public sealed class DuplicateSymbolsException : Exception
    {
        [NonSerialized]
        private Symbol[] duplicateSymbols;

        /// <summary>
        /// Instantiate a new DuplicateSymbolException.
        /// </summary>
        /// <param name="symbols">The duplicated symbols.</param>
        public DuplicateSymbolsException(ArrayList symbols)
        {
            this.duplicateSymbols = (Symbol[])symbols.ToArray(typeof(Symbol));
        }

        /// <summary>
        /// Gets the duplicate symbols.
        /// </summary>
        /// <returns>List of duplicate symbols.</returns>
        public Symbol[] GetDuplicateSymbols()
        {
            return this.duplicateSymbols;
        }
    }
}

