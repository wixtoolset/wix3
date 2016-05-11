// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// WixException thrown when a file cannot be found.
    /// </summary>
    [Serializable]
    public sealed class WixFileNotFoundException : WixException
    {
        /// <summary>
        /// Instantiate a new WixFileNotFoundException.
        /// </summary>
        /// <param name="file">The file that could not be found.</param>
        public WixFileNotFoundException(string file) : this(null, file, null)
        {
        }

        /// <summary>
        /// Instantiate a new WixFileNotFoundException.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information pertaining to the file that cannot be found.</param>
        /// <param name="file">The file that could not be found.</param>
        public WixFileNotFoundException(SourceLineNumberCollection sourceLineNumbers, string file) :
            base(WixErrors.FileNotFound(sourceLineNumbers, file))
        {
        }

        /// <summary>
        /// Instantiate a new WixFileNotFoundException.
        /// </summary>
        /// <param name="file">The file that could not be found.</param>
        /// <param name="fileType">The type of file that cannot be found.</param>
        public WixFileNotFoundException(string file, string fileType) : this(null, file, fileType)
        {
        }

        /// <summary>
        /// Instantiate a new WixFileNotFoundException.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information pertaining to the file that cannot be found.</param>
        /// <param name="file">The file that could not be found.</param>
        /// <param name="fileType">The type of file that cannot be found.</param>
        public WixFileNotFoundException(SourceLineNumberCollection sourceLineNumbers, string file, string fileType) :
            base(WixErrors.FileNotFound(sourceLineNumbers, file, fileType))
        {
        }
    }
}
