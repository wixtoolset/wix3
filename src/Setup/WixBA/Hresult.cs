// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.UX
{
    using System;

    /// <summary>
    /// Utility class to work with HRESULTs
    /// </summary>
    internal class Hresult
    {
        /// <summary>
        /// Determines if an HRESULT was a success code or not.
        /// </summary>
        /// <param name="status">HRESULT to verify.</param>
        /// <returns>True if the status is a success code.</returns>
        public static bool Succeeded(int status)
        {
            return status >= 0;
        }
    }
}
