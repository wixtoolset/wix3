//-------------------------------------------------------------------------------------------------
// <copyright file="WixInvalidIdtException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// WiX invalid idt exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// WiX invalid idt exception.
    /// </summary>
    [Serializable]
    public sealed class WixInvalidIdtException : WixException
    {
        /// <summary>
        /// Instantiate a new WixInvalidIdtException.
        /// </summary>
        /// <param name="idtFile">The invalid idt file.</param>
        public WixInvalidIdtException(string idtFile) :
            base(WixErrors.InvalidIdt(SourceLineNumberCollection.FromFileName(idtFile), idtFile))
        {
        }

        /// <summary>
        /// Instantiate a new WixInvalidIdtException.
        /// </summary>
        /// <param name="idtFile">The invalid idt file.</param>
        /// <param name="tableName">The table name of the invalid idt file.</param>
        public WixInvalidIdtException(string idtFile, string tableName) :
            base(WixErrors.InvalidIdt(SourceLineNumberCollection.FromFileName(idtFile), idtFile, tableName))
        {
        }
    }
}
