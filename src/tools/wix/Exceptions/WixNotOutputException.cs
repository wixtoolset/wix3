// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Exception thrown when trying to create an output from a file that is not an output file.
    /// </summary>
    [Serializable]
    public sealed class WixNotOutputException : WixException
    {
        /// <summary>
        /// Instantiate a new WixNotOutputException.
        /// </summary>
        /// <param name="error">Localized error information.</param>
        public WixNotOutputException(WixErrorEventArgs error)
            : base(error)
        {
        }
    }
}
