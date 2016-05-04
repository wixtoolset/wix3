// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools 
{
    using System;
    using System.Xml;
    using System.IO;

    /// <summary>
    /// Wrapper for XmlDocument that implements IXmlLineInfo.
    /// </summary>
    public class LineInfoDocument : XmlDocument
    {
        private XmlReader reader;

        /// <summary>
        /// Loads the specified XML data.
        /// </summary>
        /// <param name="reader">The TextReader used to feed the XML data into the document.</param>
        public override void Load(XmlReader reader)
        {
            this.reader = reader;
            base.Load(this.reader);
        }

        /// <summary>
        /// Creates an XmlElement.
        /// </summary>
        /// <param name="prefix">The prefix of the new element (if any). String.Empty and a null reference (Nothing in Visual Basic) are equivalent.</param>
        /// <param name="localname">The local name of the new element.</param>
        /// <param name="namespaceuri">The namespace URI of the new element (if any). String.Empty and a null reference (Nothing in Visual Basic) are equivalent.</param>
        /// <returns>The new XmlElement.</returns>
        public override XmlElement CreateElement(string prefix, string localname, string namespaceuri)
        {
            LineInfoElement elem = new LineInfoElement(prefix, localname, namespaceuri, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                elem.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return elem;
        }

        /// <summary>
        /// Creates an XmlAttribute with the specified Prefix, LocalName, and NamespaceURI.
        /// </summary>
        /// <param name="prefix">The prefix of the attribute (if any). String.Empty and a null reference (Nothing in Visual Basic) are equivalent.</param>
        /// <param name="localname">The local name of the attribute.</param>
        /// <param name="namespaceuri">The namespace URI of the attribute (if any). String.Empty and a null reference (Nothing in Visual Basic) are equivalent. If prefix is xmlns, then this parameter must be http://www.w3.org/2000/xmlns/; otherwise an exception is thrown.</param>
        /// <returns>The new XmlAttribute.</returns>
        public override XmlAttribute CreateAttribute(string prefix, string localname, string namespaceuri)
        {
            LineInfoAttribute attr = new LineInfoAttribute(prefix, localname, namespaceuri, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                attr.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return attr;
        }

        /// <summary>
        /// Creates an XmlCDataSection containing the specified data.
        /// </summary>
        /// <param name="data">The content of the new XmlCDataSection.</param>
        /// <returns>The new XmlCDataSection.</returns>
        public override XmlCDataSection CreateCDataSection(string data)
        {
            LineInfoCData cd = new LineInfoCData(data, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                cd.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return cd;
        }

        /// <summary>
        /// Creates an XmlText with the specified text.
        /// </summary>
        /// <param name="data">The text for the Text node.</param>
        /// <returns>The new XmlText node.</returns>
        public override XmlText CreateTextNode(string data)
        {
            LineInfoText t = new LineInfoText(data, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                t.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return t;
        }

        /// <summary>
        /// Creates an XmlComment containing the specified data.
        /// </summary>
        /// <param name="data">The content of the new XmlComment.</param>
        /// <returns>The new XmlComment.</returns>
        public override XmlComment CreateComment(string data)
        {
            LineInfoComment tc = new LineInfoComment(data, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                tc.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return tc;
        }

        /// <summary>
        /// Creates an XmlEntityReference with the specified name.
        /// </summary>
        /// <param name="name">The name of the entity reference.</param>
        /// <returns>The new XmlEntityReference.</returns>
        public override XmlEntityReference CreateEntityReference(string name)
        {
            LineInfoEntityReference ter = new LineInfoEntityReference(name, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                ter.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return ter;
        }

        /// <summary>
        /// Creates a new XmlDocumentType object.
        /// </summary>
        /// <param name="name">Name of the document type.</param>
        /// <param name="publicId">The public identifier of the document type or a null reference (Nothing in Visual Basic).</param>
        /// <param name="systemId">The system identifier of the document type or a null reference (Nothing in Visual Basic).</param>
        /// <param name="internalSubset">The DTD internal subset of the document type or a null reference (Nothing in Visual Basic).</param>
        /// <returns>The new XmlDocumentType.</returns>
        public override XmlDocumentType CreateDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
            LineInfoDocumentType tdt = new LineInfoDocumentType(name, publicId, systemId, internalSubset, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                tdt.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return tdt;
        }

        /// <summary>
        /// Creates an XmlProcessingInstruction with the specified name and data.
        /// </summary>
        /// <param name="target">The name of the processing instruction.</param>
        /// <param name="data">The data for the processing instruction.</param>
        /// <returns>The new XmlProcessingInstruction.</returns>
        public override XmlProcessingInstruction CreateProcessingInstruction(string target, string data)
        {
            LineInfoProcessingInstruction pi = new LineInfoProcessingInstruction(target, data, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                pi.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return pi;
        }

        /// <summary>
        /// Creates an XmlDeclaration node with the specified values.
        /// </summary>
        /// <param name="version">The version must be "1.0".</param>
        /// <param name="encoding">The value of the encoding attribute. This is the encoding that is used when you save the XmlDocument to a file or a stream.</param>
        /// <param name="standalone">The value must be either "yes" or "no". If this is a null reference (Nothing in Visual Basic) or String.Empty, the Save method does not write a standalone attribute on the XML declaration.</param>
        /// <returns>The new XmlDeclaration node.</returns>
        public override XmlDeclaration CreateXmlDeclaration(string version, string encoding, string standalone)
        {
            LineInfoDeclaration td = new LineInfoDeclaration(version, encoding, standalone, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                td.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return td;
        }

        /// <summary>
        /// Creates an XmlSignificantWhitespace node.
        /// </summary>
        /// <param name="data">The string must contain only the following characters &#20; &#10; &#13; and &#9;</param>
        /// <returns>A new XmlSignificantWhitespace node.</returns>
        public override XmlSignificantWhitespace CreateSignificantWhitespace(string data)
        {
            LineInfoSignificantWhitespace sw = new LineInfoSignificantWhitespace(data, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                sw.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return sw;
        }

        /// <summary>
        /// Creates an XmlWhitespace node.
        /// </summary>
        /// <param name="data">The string must contain only the following characters &#20; &#10; &#13; and &#9;</param>
        /// <returns>A new XmlWhitespace node.</returns>
        public override XmlWhitespace CreateWhitespace(string data)
        {
            LineInfoWhitespace ws = new LineInfoWhitespace(data, this);
            IXmlLineInfo lineInfo = this.reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                ws.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }
            return ws;
        }
    }
}
