// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools 
{
    using System;
    using System.Xml;

    /// <summary>
    /// Wrapper for XmlCDataSection that implements IXmlLineInfo.
    /// </summary>
    public class LineInfoCData : XmlCDataSection, IXmlLineInfo
    {
        private int lineNumber = -1;
        private int linePosition = -1;

        /// <summary>
        /// Instantiate a new LineInfoCData class.
        /// </summary>
        /// <param name="data">The data of the node.</param>
        /// <param name="doc">The document that owns this node.</param>
        internal LineInfoCData(string data, XmlDocument doc) : base(data, doc)
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
