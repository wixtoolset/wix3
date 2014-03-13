//-------------------------------------------------------------------------------------------------
// <copyright file="LineInfoWhitespace.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Wrapper for XmlWhitespace that implements IXmlLineInfo.
// Originally from www.gotdotnet.com/userfiles/XMLDom/extendDOM.zip
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
    using System;
    using System.Xml;

    /// <summary>
    /// Wrapper for XmlWhitespace that implements IXmlLineInfo.
    /// </summary>
    public class LineInfoWhitespace : XmlWhitespace, IXmlLineInfo
    {
        private int lineNumber = -1;
        private int linePosition = -1;

        /// <summary>
        /// Instantiate a new LineInfoWhitespace class.
        /// </summary>
        /// <param name="data">The data of the node.</param>
        /// <param name="doc">The document that owns this node.</param>
        internal LineInfoWhitespace(string data, XmlDocument doc) : base(data, doc)
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