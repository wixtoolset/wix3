//-------------------------------------------------------------------------------------------------
// <copyright file="LineInfoAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Wrapper for XmlAttribute that implements IXmlLineInfo.
// Originally from www.gotdotnet.com/userfiles/XMLDom/extendDOM.zip
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools 
{
    using System;
    using System.Xml;

    /// <summary>
    /// Wrapper for XmlAttribute that implements IXmlLineInfo.
    /// </summary>
    public class LineInfoAttribute : XmlAttribute, IXmlLineInfo
    {
        private int lineNumber = -1;
        private int linePosition = -1;

        /// <summary>
        /// Instantiate a new LineInfoAttribute class.
        /// </summary>
        /// <param name="prefix">The namespace prefix of this node.</param>
        /// <param name="localname">The local name of the node.</param>
        /// <param name="namespaceURI">The namespace URI of this node.</param>
        /// <param name="doc">The document that owns this node.</param>
        internal LineInfoAttribute(string prefix, string localname, string namespaceURI, XmlDocument doc) : base(prefix, localname, namespaceURI, doc)
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