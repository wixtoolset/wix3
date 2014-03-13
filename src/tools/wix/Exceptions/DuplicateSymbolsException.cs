//-------------------------------------------------------------------------------------------------
// <copyright file="DuplicateSymbolsException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Duplicate symbols exception.
// </summary>
//-------------------------------------------------------------------------------------------------

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

