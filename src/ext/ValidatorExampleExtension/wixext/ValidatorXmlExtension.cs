//-------------------------------------------------------------------------------------------------
// <copyright file="ValidatorXmlExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// A simple validator extension to output XML.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The example validator extension.
    /// </summary>
    /// <remarks>
    /// <para>This example extension writes an XML using an
    /// <see cref="XmlWriter"/> to avoid the overhead of reflection and
    /// XML serialization.</para>
    /// </remarks>
    public sealed class ValidatorXmlExtension : ValidatorExtension
    {
        private readonly string[] elements;
        private XmlWriter writer;

        /// <summary>
        /// Creates an instance of the <see cref="ValidatorXmlExtension"/> class.
        /// </summary>
        public ValidatorXmlExtension()
        {
            elements = new string[] {
                "ICE",
                "Type",
                "Description",
                "URL",
                "Table",
                "Column",
                "Key"
            };
        }

        /// <summary>
        /// Initialize the extension.
        /// </summary>
        public override void InitializeValidator()
        {
            base.InitializeValidator();

            // Computer the XML output file name based on the database file
            // and open the file for writing XML.
            string filename = Path.ChangeExtension(this.DatabaseFile, ".xml");
            this.writer = new XmlTextWriter(filename, Encoding.UTF8);

            // Write the declaration and root element.
            this.writer.WriteStartDocument(true);
            this.writer.WriteStartElement("Validation");
        }

        /// <summary>
        /// Finalizes the extension.
        /// </summary>
        public override void FinalizeValidator()
        {
            if (null != this.writer)
            {
                // Write any open elements and close the file.
                this.writer.WriteEndDocument();
                this.writer.Close();

                this.writer = null;
            }

            base.FinalizeValidator();
        }

        /// <summary>
        /// Logs the messages for ICE errors, warnings, and information.
        /// </summary>
        /// <param name="message">The entire string sent from the validator.</param>
        public override void Log(string message)
        {
            this.Log(message, null);
        }

        /// <summary>
        /// Logs the messages for ICE errors, warnings, and information.
        /// </summary>
        /// <param name="message">The entire string sent from the validator.</param>
        /// <param name="action">The name of the ICE action.</param>
        public override void Log(string message, string action)
        {
            if (null == message) return;

            // Open the message element.
            this.writer.WriteStartElement("Message");

            // Write the database file name.
            this.writer.WriteElementString("File", this.DatabaseFile);

            // Write elements for each message part.
            string[] messageParts = message.Split('\t');
            for (int i = 0; i < messageParts.Length; i++)
            {
                this.writer.WriteElementString(
                    elements[Math.Min(i, elements.Length - 1)],
                    messageParts[i]);
            }

            // Write source line number information.
            string tableName = null;
            string[] primaryKeys = new string[] {String.Empty};

            if (6 < messageParts.Length)
            {
                tableName = messageParts[4];

                primaryKeys = new string[messageParts.Length - 6];
                Array.Copy(messageParts, 6, primaryKeys, 0, primaryKeys.Length);
            }

            SourceLineNumberCollection messageSourceLineNumbers = base.GetSourceLineNumbers(tableName, primaryKeys);
            if (null != messageSourceLineNumbers && 0 < messageSourceLineNumbers.Count)
            {
                SourceLineNumber messageSourceLineNumber = messageSourceLineNumbers[0];
                this.writer.WriteElementString("SourceFileName", messageSourceLineNumber.FileName);

                if (messageSourceLineNumber.HasLineNumber)
                {
                    this.writer.WriteElementString("SourceLineNumber", messageSourceLineNumber.LineNumber.ToString(CultureInfo.InvariantCulture));
                }
            }

            // Close the element.
            this.writer.WriteEndElement();
        }
    }
}
