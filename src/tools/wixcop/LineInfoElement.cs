// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools 
{
    using System;
    using System.Xml;

    /// <summary>
    /// Wrapper for XmlElement that implements IXmlLineInfo.
    /// </summary>
    public class LineInfoElement : XmlElement, IXmlLineInfo
    {
        private int lineNumber = -1;
        private int linePosition = -1;

        /// <summary>
        /// Instantiate a new LineInfoElement class.
        /// </summary>
        /// <param name="prefix">The namespace prefix of this node.</param>
        /// <param name="localname">The local name of the node.</param>
        /// <param name="namespaceURI">The namespace URI of this node.</param>
        /// <param name="doc">The document that owns this node.</param>
        internal LineInfoElement(string prefix, string localname, string namespaceURI, XmlDocument doc) : base(prefix, localname, namespaceURI, doc)
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
