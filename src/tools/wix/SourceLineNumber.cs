// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Text;

    /// <summary>
    /// Represents file name and line number for source file
    /// </summary>
    public sealed class SourceLineNumber
    {
        private bool hasLineNumber;
        private string fileName;
        private int lineNumber;

        /// <summary>
        /// Constructor for a source with no line information.
        /// </summary>
        /// <param name="fileName">File name of the source.</param>
        public SourceLineNumber(string fileName)
        {
            this.fileName = fileName;
        }

        /// <summary>
        /// Constructor for a source with line information.
        /// </summary>
        /// <param name="fileName">File name of the source.</param>
        /// <param name="lineNumber">Line number of the source.</param>
        public SourceLineNumber(string fileName, int lineNumber)
        {
            this.hasLineNumber = true;
            this.fileName = fileName;
            this.lineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the file name of the source.
        /// </summary>
        /// <value>File name for the source.</value>
        public string FileName
        {
            get { return this.fileName; }
        }

        /// <summary>
        /// Gets flag for if the source has line number information.
        /// </summary>
        /// <value>Flag if source has line number information.</value>
        public bool HasLineNumber
        {
            get { return this.hasLineNumber; }
        }

        /// <summary>
        /// Gets and sets the line number of the source.
        /// </summary>
        /// <value>Line number of the source.</value>
        public int LineNumber
        {
            get
            {
                return this.lineNumber;
            }

            set
            {
                this.hasLineNumber = true;
                this.lineNumber = value;
            }
        }

        /// <summary>
        /// Gets the file name and line information.
        /// </summary>
        /// <value>File name and line information.</value>
        public string QualifiedFileName
        {
            get
            {
                if (this.hasLineNumber)
                {
                    return String.Concat(this.fileName, "*", this.lineNumber);
                }
                else
                {
                    return this.fileName;
                }
            }
        }

        /// <summary>
        /// Determines if two SourceLineNumbers are equivalent.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True if SourceLineNumbers are equivalent.</returns>
        public override bool Equals(object obj)
        {
            SourceLineNumber otherSourceLineNumber = obj as SourceLineNumber;

            if (null != otherSourceLineNumber)
            {
                if (this.fileName != otherSourceLineNumber.fileName)
                {
                    return false;
                }

                if (this.hasLineNumber != otherSourceLineNumber.hasLineNumber)
                {
                    return false;
                }

                if (this.hasLineNumber && this.lineNumber != otherSourceLineNumber.lineNumber)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Serves as a hash code for a particular type.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
