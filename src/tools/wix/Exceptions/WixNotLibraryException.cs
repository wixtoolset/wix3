//-------------------------------------------------------------------------------------------------
// <copyright file="WixNotLibraryException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Exception thrown when trying to create an library from a file that is not an library file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Exception thrown when trying to create an library from a file that is not an library file.
    /// </summary>
    [Serializable]
    public sealed class WixNotLibraryException : WixException
    {
        /// <summary>
        /// Instantiate a new WixNotLibraryException.
        /// </summary>
        /// <param name="error">Localized error information.</param>
        public WixNotLibraryException(WixErrorEventArgs error)
            : base(error)
        {
        }
    }
}
