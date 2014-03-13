//-------------------------------------------------------------------------------------------------
// <copyright file="WarningEventArgs.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Event arguments for warning messages.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Event arguments for warning messages.
    /// </summary>
    public sealed class WarningEventArgs : EventArgs
    {
        private string message;

        /// <summary>
        /// WarningEventArgs Constructor.
        /// </summary>
        /// <param name="message">Warning message content.</param>
        public WarningEventArgs(string message)
        {
            this.message = message;
        }

        /// <summary>
        /// Getter for the message content.
        /// </summary>
        /// <value>The message content.</value>
        public string Message
        {
            get { return this.message; }
        }
    }
}