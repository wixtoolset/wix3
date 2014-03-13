//-------------------------------------------------------------------------------------------------
// <copyright file="Localization.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Object that represents a localization file.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Object that represents a localization file.
    /// </summary>
    public sealed class Localization
    {
        public const string XmlNamespaceUri = "http://schemas.microsoft.com/wix/2006/localization";
        private static string XmlElementName = "WixLocalization";
        private static XmlSchemaCollection schemas;

        private int codepage;
        private string culture;
        private Hashtable variables = new Hashtable();
        private TableDefinitionCollection tableDefinitions;
        private Dictionary<string, LocalizedControl> localizedControls = new Dictionary<string, LocalizedControl>();

        /// <summary>
        /// Protect the constructor.
        /// </summary>
        private Localization()
        {
        }

        /// <summary>
        /// Gets the codepage.
        /// </summary>
        /// <value>The codepage.</value>
        public int Codepage
        {
            get { return this.codepage; }
        }

        /// <summary>
        /// Gets the culture.
        /// </summary>
        /// <value>The culture.</value>
        public string Culture
        {
            get { return this.culture; }
        }

        /// <summary>
        /// Gets the variables.
        /// </summary>
        /// <value>The variables.</value>
        public ICollection Variables
        {
            get { return this.variables.Values; }
        }

        /// <summary>
        /// Gets the localized controls.
        /// </summary>
        /// <value>The localized controls.</value>
        public ICollection<KeyValuePair<string, LocalizedControl>> LocalizedControls
        {
            get { return this.localizedControls; }
        }

        /// <summary>
        /// Loads a localization file from a path on disk.
        /// </summary>
        /// <param name="path">Path to library file saved on disk.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when loading the localization file.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>Returns the loaded localization file.</returns>
        public static Localization Load(string path, TableDefinitionCollection tableDefinitions, bool suppressSchema)
        {
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    return Load(stream, new Uri(Path.GetFullPath(path)), tableDefinitions, suppressSchema);
                }
            }
            catch (FileNotFoundException)
            {
                throw new WixException(WixErrors.FileNotFound(null, Path.GetFullPath(path)));
            }
        }

        /// <summary>
        /// Persists a localization file into an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the localization file should persist itself as XML.</param>
        public void Persist(XmlWriter writer)
        {
            writer.WriteStartElement(Localization.XmlElementName, XmlNamespaceUri);

            if (-1 != this.codepage)
            {
                writer.WriteAttributeString("Codepage", this.codepage.ToString(CultureInfo.InvariantCulture));
            }

            if (!String.IsNullOrEmpty(this.culture))
            {
                writer.WriteAttributeString("Culture", this.culture);
            }

            foreach (WixVariableRow wixVariableRow in this.variables.Values)
            {
                writer.WriteStartElement("String", XmlNamespaceUri);

                writer.WriteAttributeString("Id", wixVariableRow.Id);

                if (wixVariableRow.Overridable)
                {
                    writer.WriteAttributeString("Overridable", "yes");
                }

                writer.WriteCData(wixVariableRow.Value);

                writer.WriteEndElement();
            }

            foreach (string controlKey in this.localizedControls.Keys)
            {
                writer.WriteStartElement("UI", XmlNamespaceUri);

                string[] controlKeys = controlKey.Split('/');
                string dialog = controlKeys[0];
                string control = controlKeys[1];

                if (!String.IsNullOrEmpty(dialog))
                {
                    writer.WriteAttributeString("Dialog", dialog);
                }

                if (!String.IsNullOrEmpty(control))
                {
                    writer.WriteAttributeString("Control", control);
                }

                LocalizedControl localizedControl = this.localizedControls[controlKey];

                if (CompilerCore.IntegerNotSet != localizedControl.X)
                {
                    writer.WriteAttributeString("X", localizedControl.X.ToString());
                }

                if (CompilerCore.IntegerNotSet != localizedControl.Y)
                {
                    writer.WriteAttributeString("Y", localizedControl.Y.ToString());
                }

                if (CompilerCore.IntegerNotSet != localizedControl.Width)
                {
                    writer.WriteAttributeString("Width", localizedControl.Width.ToString());
                }

                if (CompilerCore.IntegerNotSet != localizedControl.Height)
                {
                    writer.WriteAttributeString("Height", localizedControl.Height.ToString());
                }

                if (MsiInterop.MsidbControlAttributesRTLRO == (localizedControl.Attributes & MsiInterop.MsidbControlAttributesRTLRO))
                {
                    writer.WriteAttributeString("RightToLeft", "yes");
                }

                if (MsiInterop.MsidbControlAttributesRightAligned == (localizedControl.Attributes & MsiInterop.MsidbControlAttributesRightAligned))
                {
                    writer.WriteAttributeString("RightAligned", "yes");
                }

                if (MsiInterop.MsidbControlAttributesLeftScroll == (localizedControl.Attributes & MsiInterop.MsidbControlAttributesLeftScroll))
                {
                    writer.WriteAttributeString("LeftScroll", "yes");
                }

                if (!String.IsNullOrEmpty(localizedControl.Text))
                {
                    writer.WriteCData(localizedControl.Text);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Merge the information from another localization object into this one.
        /// </summary>
        /// <param name="localization">The localization object to be merged into this one.</param>
        public void Merge(Localization localization)
        {
            foreach (WixVariableRow wixVariableRow in localization.Variables)
            {
                WixVariableRow existingWixVariableRow = (WixVariableRow)variables[wixVariableRow.Id];

                if (null == existingWixVariableRow || (existingWixVariableRow.Overridable && !wixVariableRow.Overridable))
                {
                    variables[wixVariableRow.Id] = wixVariableRow;
                }
                else if (!wixVariableRow.Overridable)
                {
                    throw new WixException(WixErrors.DuplicateLocalizationIdentifier(wixVariableRow.SourceLineNumbers, wixVariableRow.Id));
                }
            }
        }

        /// <summary>
        /// Loads a localization file from a stream.
        /// </summary>
        /// <param name="stream">Stream containing the localization file.</param>
        /// <param name="uri">Uri for finding this stream.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when loading the localization file.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>Returns the loaded localization file.</returns>
        internal static Localization Load(Stream stream, Uri uri, TableDefinitionCollection tableDefinitions, bool suppressSchema)
        {
            XmlReader reader = null;

            try
            {
                reader = new XmlTextReader(uri.AbsoluteUri, stream);

                if (!suppressSchema)
                {
                    reader = new XmlValidatingReader(reader);
                    ((XmlValidatingReader)reader).Schemas.Add(GetSchemas());
                }

                return Localization.Parse(reader, tableDefinitions);
            }
            catch (XmlException xe)
            {
                throw new WixException(WixErrors.InvalidXml(SourceLineNumberCollection.FromUri(reader.BaseURI), "localization", xe.Message));
            }
            catch (XmlSchemaException xse)
            {
                throw new WixException(WixErrors.SchemaValidationFailed(SourceLineNumberCollection.FromUri(reader.BaseURI), xse.Message, xse.LineNumber, xse.LinePosition));
            }
            finally
            {
                if (null != reader)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Get the schemas required to validate a library.
        /// </summary>
        /// <returns>The schemas required to validate a library.</returns>
        internal static XmlSchemaCollection GetSchemas()
        {
            if (null == Localization.schemas)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream localizationSchemaStream = assembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Xsd.wixloc.xsd"))
                {
                    schemas = new XmlSchemaCollection();
                    XmlSchema localizationSchema = XmlSchema.Read(localizationSchemaStream, null);
                    schemas.Add(localizationSchema);
                }
            }

            return schemas;
        }

        /// <summary>
        /// Parse a localization file from an XML format.
        /// </summary>
        /// <param name="document">XmlDocument where the localization file is persisted.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when parsing the localization file.</param>
        /// <returns>The parsed localization.</returns>
        internal static Localization Parse(XmlReader reader, TableDefinitionCollection tableDefinitions)
        {
            XmlDocument document = new XmlDocument();
            reader.MoveToContent();
            XmlNode node = document.ReadNode(reader);
            document.AppendChild(node);

            Localization localization = new Localization();
            localization.tableDefinitions = tableDefinitions;
            localization.Parse(document);

            return localization;
        }

        /// <summary>
        /// Parse a localization file from an XML document.
        /// </summary>
        /// <param name="document">XmlDocument where the localization file is persisted.</param>
        internal void Parse(XmlDocument document)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(document.DocumentElement);
            if (Localization.XmlElementName == document.DocumentElement.LocalName)
            {
                if (Localization.XmlNamespaceUri == document.DocumentElement.NamespaceURI)
                {
                    this.ParseWixLocalizationElement(document.DocumentElement);
                }
                else // invalid or missing namespace
                {
                    if (0 == document.DocumentElement.NamespaceURI.Length)
                    {
                        throw new WixException(WixErrors.InvalidWixXmlNamespace(sourceLineNumbers, Localization.XmlElementName, Localization.XmlNamespaceUri));
                    }
                    else
                    {
                        throw new WixException(WixErrors.InvalidWixXmlNamespace(sourceLineNumbers, Localization.XmlElementName, document.DocumentElement.NamespaceURI, Localization.XmlNamespaceUri));
                    }
                }
            }
            else
            {
                throw new WixException(WixErrors.InvalidDocumentElement(sourceLineNumbers, document.DocumentElement.Name, "localization", Localization.XmlElementName));
            }
        }

        /// <summary>
        /// Parses the WixLocalization element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseWixLocalizationElement(XmlNode node)
        {
            int codepage = -1;
            string culture = null;
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == Localization.XmlNamespaceUri)
                {
                    switch (attrib.LocalName)
                    {
                        case "Codepage":
                            codepage = Common.GetValidCodePage(attrib.Value, true);
                            break;
                        case "Culture":
                            culture = attrib.Value;
                            break;
                        case "Language":
                            // do nothing; @Language is used for locutil which can't convert Culture to lcid
                            break;
                        default:
                            throw new WixException(WixErrors.UnexpectedAttribute(sourceLineNumbers, attrib.OwnerElement.Name, attrib.Name));
                    }
                }
                else
                {
                    Common.UnsupportedExtensionAttribute(sourceLineNumbers, attrib, Localization.OnMessage);
                }
            }

            this.codepage = codepage;
            this.culture = String.IsNullOrEmpty(culture) ? String.Empty : culture.ToLower(CultureInfo.InvariantCulture);

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == Localization.XmlNamespaceUri)
                    {
                        switch (child.LocalName)
                        {
                            case "String":
                                this.ParseString(child);
                                break;
                            case "UI":
                                this.ParseUI(child);
                                break;
                            default:
                                throw new WixException(WixErrors.UnexpectedElement(sourceLineNumbers, node.Name, child.Name));
                        }
                    }
                    else
                    {
                        throw new WixException(WixErrors.UnsupportedExtensionElement(sourceLineNumbers, node.Name, child.Name));
                    }
                }
            }
        }


        /// <summary>
        /// Parse a localization string.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseString(XmlNode node)
        {
            string id = null;
            bool overridable = false;
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == Localization.XmlNamespaceUri)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = Common.GetAttributeIdentifierValue(sourceLineNumbers, attrib, null);
                            break;
                        case "Overridable":
                            overridable = Common.IsYes(sourceLineNumbers, "String", attrib.Name, attrib.Value);
                            break;
                        case "Localizable":
                            ; // do nothing
                            break;
                        default:
                            throw new WixException(WixErrors.UnexpectedAttribute(sourceLineNumbers, attrib.OwnerElement.Name, attrib.Name));
                    }
                }
                else
                {
                    throw new WixException(WixErrors.UnsupportedExtensionAttribute(sourceLineNumbers, attrib.OwnerElement.Name, attrib.Name));
                }
            }

            string value = node.InnerText;

            if (null == id)
            {
                throw new WixException(WixErrors.ExpectedAttribute(sourceLineNumbers, "String", "Id"));
            }
            else if (0 == id.Length)
            {
                throw new WixException(WixErrors.IllegalIdentifier(sourceLineNumbers, "String", "Id", 0));
            }

            WixVariableRow wixVariableRow = new WixVariableRow(sourceLineNumbers, this.tableDefinitions["WixVariable"]);
            wixVariableRow.Id = id;
            wixVariableRow.Overridable = overridable;
            wixVariableRow.Value = value;

            WixVariableRow existingWixVariableRow = (WixVariableRow)this.variables[id];
            if (null == existingWixVariableRow || (existingWixVariableRow.Overridable && !overridable))
            {
                this.variables.Add(id, wixVariableRow);
            }
            else if (!overridable)
            {
                throw new WixException(WixErrors.DuplicateLocalizationIdentifier(sourceLineNumbers, id));
            }
        }

        /// <summary>
        /// Parse a localized control.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="localization">The localization being parsed.</param>
        private void ParseUI(XmlNode node)
        {
            string dialog = null;
            string control = null;
            int x = CompilerCore.IntegerNotSet;
            int y = CompilerCore.IntegerNotSet;
            int width = CompilerCore.IntegerNotSet;
            int height = CompilerCore.IntegerNotSet;
            int attribs = 0;
            string text = null;
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == Localization.XmlNamespaceUri)
                {
                    switch (attrib.LocalName)
                    {
                        case "Dialog":
                            dialog = Common.GetAttributeIdentifierValue(sourceLineNumbers, attrib, null);
                            break;
                        case "Control":
                            control = Common.GetAttributeIdentifierValue(sourceLineNumbers, attrib, null);
                            break;
                        case "X":
                            x = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue, null);
                            break;
                        case "Y":
                            y = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue, null);
                            break;
                        case "Width":
                            width = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue, null);
                            break;
                        case "Height":
                            height = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue, null);
                            break;
                        case "RightToLeft":
                            if (YesNoType.Yes == Common.GetAttributeYesNoValue(sourceLineNumbers, attrib, null))
                            {
                                attribs |= MsiInterop.MsidbControlAttributesRTLRO;
                            }
                            break;
                        case "RightAligned":
                            if (YesNoType.Yes == Common.GetAttributeYesNoValue(sourceLineNumbers, attrib, null))
                            {
                                attribs |= MsiInterop.MsidbControlAttributesRightAligned;
                            }
                            break;
                        case "LeftScroll":
                            if (YesNoType.Yes == Common.GetAttributeYesNoValue(sourceLineNumbers, attrib, null))
                            {
                                attribs |= MsiInterop.MsidbControlAttributesLeftScroll;
                            }
                            break;
                        default:
                            throw new WixException(WixErrors.UnexpectedAttribute(sourceLineNumbers, attrib.OwnerElement.Name, attrib.Name));
                    }
                }
                else
                {
                    throw new WixException(WixErrors.UnsupportedExtensionAttribute(sourceLineNumbers, attrib.OwnerElement.Name, attrib.Name));
                }
            }

            text = node.InnerText;

            if (String.IsNullOrEmpty(control) && 0 < attribs)
            {
                if (MsiInterop.MsidbControlAttributesRTLRO == (attribs & MsiInterop.MsidbControlAttributesRTLRO))
                {
                    throw new WixException(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "RightToLeft", "Control"));
                }
                else if (MsiInterop.MsidbControlAttributesRightAligned == (attribs & MsiInterop.MsidbControlAttributesRightAligned))
                {
                    throw new WixException(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "RightAligned", "Control"));
                }
                else if (MsiInterop.MsidbControlAttributesLeftScroll == (attribs & MsiInterop.MsidbControlAttributesLeftScroll))
                {
                    throw new WixException(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "LeftScroll", "Control"));
                }
            }

            if (String.IsNullOrEmpty(control) && String.IsNullOrEmpty(dialog))
            {
                throw new WixException(WixErrors.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name, "Dialog", "Control"));
            }

            string key = LocalizedControl.GetKey(dialog, control);
            if (this.localizedControls.ContainsKey(key))
            {
                if (String.IsNullOrEmpty(control))
                {
                    throw new WixException(WixErrors.DuplicatedUiLocalization(sourceLineNumbers, dialog));
                }
                else
                {
                    throw new WixException(WixErrors.DuplicatedUiLocalization(sourceLineNumbers, dialog, control));
                }
            }

            this.localizedControls.Add(key, new LocalizedControl(x, y, width, height, attribs, text));
        }

        /// <summary>
        /// Throws an exception for an error message event.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public static void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;
            if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
    }

    public class LocalizedControl
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Attributes { get; private set; }
        public string Text { get; private set; }

        public LocalizedControl(int x, int y, int width, int height, int attribs, string text)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.Attributes = attribs;
            this.Text = text;
        }

        /// <summary>
        /// Get key for a localized control.
        /// </summary>
        /// <param name="dialog">The optional id of the control's dialog.</param>
        /// <param name="control">The id of the control.</param>
        /// <returns>The localized control id.</returns>
        public static string GetKey(string dialog, string control)
        {
            return String.Concat(String.IsNullOrEmpty(dialog) ? String.Empty : dialog, "/", String.IsNullOrEmpty(control) ? String.Empty : control);
        }
    }
}
