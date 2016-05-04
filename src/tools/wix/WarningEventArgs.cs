// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
