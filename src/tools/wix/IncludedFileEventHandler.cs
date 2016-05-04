// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Text;

    /// <summary>
    /// Included file event handler delegate.
    /// </summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="ea">Arguments for the included file event.</param>
    public delegate void IncludedFileEventHandler(object sender, IncludedFileEventArgs e);

    /// <summary>
    /// Event args for included file event.
    /// </summary>
    public class IncludedFileEventArgs : EventArgs
    {
        private SourceLineNumberCollection sourceLineNumbers;
        private string fullName;

        /// <summary>
        /// Creates a new IncludedFileEventArgs.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for the included file.</param>
        /// <param name="fullName">The full path of the included file.</param>
        public IncludedFileEventArgs(SourceLineNumberCollection sourceLineNumbers, string fullName)
        {
            this.sourceLineNumbers = sourceLineNumbers;
            this.fullName = fullName;
        }

        /// <summary>
        /// Gets the full path of the included file.
        /// </summary>
        /// <value>The full path of the included file.</value>
        public string FullName
        {
            get { return this.fullName; }
        }

        /// <summary>
        /// Gets the source line numbers.
        /// </summary>
        /// <value>The source line numbers.</value>
        public SourceLineNumberCollection SourceLineNumbers
        {
            get { return this.sourceLineNumbers; }
        }
    }
}
