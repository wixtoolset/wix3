// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools 
{
    using System;
    using System.Xml;

    /// <summary>
    /// Wrapper for XmlDocumentType that implements IXmlLineInfo.
    /// </summary>
    public class LineInfoDocumentType : XmlDocumentType, IXmlLineInfo
    {
        private int lineNumber = -1;
        private int linePosition = -1;

        /// <summary>
        /// Instantiate a new LineInfoDocumentType class.
        /// </summary>
        /// <param name="name">Name of the document type.</param>
        /// <param name="publicId">The public identifier of the document type or a null reference (Nothing in Visual Basic).</param>
        /// <param name="systemId">The system identifier of the document type or a null reference (Nothing in Visual Basic).</param>
        /// <param name="internalSubset">The DTD internal subset of the document type or a null reference (Nothing in Visual Basic).</param>
        /// <param name="doc">The document that owns this node.</param>
        internal LineInfoDocumentType(string name, string publicId, string systemId, string internalSubset, XmlDocument doc) : base(name, publicId, systemId, internalSubset, doc)
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
        /// <param name="lineNumber">The line number.</param>
        /// <param name="linePosition">The line position.</param>
        public void SetLineInfo(int lineNumber, int linePosition)
        {
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
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
