//-------------------------------------------------------------------------------------------------
// <copyright file="WixNotIntermediateException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Exception thrown when trying to create an intermediate from a source that is not an intermediate.
// </summary>
//-------------------------------------------------------------------------------------------------

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
