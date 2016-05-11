// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools 
{
    using System;
    using System.Xml;

    /// <summary>
    /// Wrapper for XmlSignificantWhitespace that implements IXmlLineInfo.
    /// </summary>
    public class LineInfoSignificantWhitespace : XmlSignificantWhitespace, IXmlLineInfo
    {
        private int lineNumber = -1;
        private int linePosition = -1;

        /// <summary>
        /// Instantiate a new LineInfoSignificantWhitespace class.
        /// </summary>
        /// <param name="data">The string must contain only the following characters &#20; &#10; &#13; and &#9;</param>
        /// <param name="doc">The document that owns this node.</param>
        internal LineInfoSignificantWhitespace(string data, XmlDocument doc) : base(data, doc)
        {
        }

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public int LineNumber
        {
            get { return this.lineNumber; }
        }

        /// <summary>
        /// Gets the line position.
        /// </summary>
        /// <value>The line position.</value>
        public int LinePosition
        {
            get { return this.linePosition; }
        }

        /// <summary>
        /// Set the line information for this node.
        /// </summary>
        /// <param name="localLineNumber">The line number.</param>
        /// <param name="localLinePosition">The line position.</param>
        public void SetLineInfo(int localLineNumber, int localLinePosition)
        {
            this.lineNumber = localLineNumber;
            this.linePosition = localLinePosition;
        }

        /// <summary>
        /// Determines if this node has line information.
        /// </summary>
        /// <returns>true.</returns>
        public bool HasLineInfo()
        {
            return true;
        }
    }
}
