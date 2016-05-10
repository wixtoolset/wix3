// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
