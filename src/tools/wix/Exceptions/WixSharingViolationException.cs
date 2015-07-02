//-------------------------------------------------------------------------------------------------
// <copyright file="WixSharingViolationException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// WixException thrown when a sharing violation occurs.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// WixException thrown when a sharing violation.
    /// </summary>
    [Serializable]
    public sealed class WixSharingViolationException : WixException
    {
        /// <summary>
        /// Instantiate a new WixSharingViolationException.
        /// </summary>
        /// <param name="file">The file triggering the sharing violation.</param>
        public WixSharingViolationException(string file)
            : this(null, file, null)
        {
        }

        /// <summary>
        /// Instantiate a new WixSharingViolationException.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information pertaining to the file triggering the sharing violation.</param>
        /// <param name="file">The file triggering the sharing violation.</param>
        public WixSharingViolationException(SourceLineNumberCollection sourceLineNumbers, string file) :
            base(WixErrors.FileInUse(sourceLineNumbers, file))
        {
        }

        /// <summary>
        /// Instantiate a new WixSharingViolationException.
        /// </summary>
        /// <param name="file">The file triggering the sharing violation.</param>
        /// <param name="fileType">The type of file triggering the sharing violation.</param>
        public WixSharingViolationException(string file, string fileType)
            : this(null, file, fileType)
        {
        }

        /// <summary>
        /// Instantiate a new WixSharingViolationException.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information pertaining to the file triggering the sharing violation.</param>
        /// <param name="file">The file triggering the sharing violation.</param>
        /// <param name="fileType">The type of file triggering the sharing violation.</param>
        public WixSharingViolationException(SourceLineNumberCollection sourceLineNumbers, string file, string fileType) :
            base(WixErrors.FileNotFound(sourceLineNumbers, file, fileType))
        {
        }
    }
}
