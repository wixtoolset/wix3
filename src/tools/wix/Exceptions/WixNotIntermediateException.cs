// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Exception thrown when trying to create an intermediate from a source that is not an intermediate.
    /// </summary>
    [Serializable]
    public sealed class WixNotIntermediateException : WixException
    {
        /// <summary>
        /// Instantiate a new WixNotIntermediateException.
        /// </summary>
        /// <param name="error">Localized error information.</param>
        public WixNotIntermediateException(WixErrorEventArgs error)
            : base(error)
        {
        }
    }
}
