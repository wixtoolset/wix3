//-------------------------------------------------------------------------------------------------
// <copyright file="WixException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Base class for all Wix exceptions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Base class for all WiX exceptions.
    /// </summary>
    [Serializable]
    public class WixException : Exception
    {
        [NonSerialized]
        private WixErrorEventArgs error;

        /// <summary>
        /// Instantiate a new WixException with a given WixError.
        /// </summary>
        /// <param name="error">The localized error information.</param>
        public WixException(WixErrorEventArgs error)
        {
            this.error = error;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public WixErrorEventArgs Error
        {
            get { return this.error; }
        }
    }
}
