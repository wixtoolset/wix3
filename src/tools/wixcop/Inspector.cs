//-------------------------------------------------------------------------------------------------
// <copyright file="Inspector.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// WiX source code inspector.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// WiX source code inspector.
    /// </summary>
    public class Inspector
    {
        private const string Wix2NamespaceURI = "http://schemas.microsoft.com/wix/2003/01/wi"; // TODO: use this for migrating TypeLib elements later
        private const string WixNamespaceURI = "http://schemas.microsoft.com/wix/2006/wi";
        private const string WixLocalizationNamespaceURI = "http://schemas.microsoft.com/wix/2006/localization";
        private static readonly Regex WixVariableRegex = new Regex(@"(\!|\$)\((?<namespace>loc|wix)\.(?<name>[_A-Za-z][0-9A-Za-z_]+)\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
        private static readonly Regex AddPrefix = new Regex(@"^[^a-zA-Z_]", RegexOptions.Compiled);
        private static readonly Regex IllegalIdentifierCharacters = new Regex(@"[^A-Za-z0-9_\.\$\(\)]|\.{2,}", RegexOptions.Compiled); // non 'words' and assorted valid characters

        private int errors;
        private Hashtable errorsAsWarnings;
        private Hashtable ignoreErrors;
        private int indentationAmount;
        private string sourceFile;

        /// <summary>
        /// Instantiate a new Inspector class.
        /// </summary>
        /// <param name="errorsAsWarnings">Test errors to display as warnings.</param>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        public Inspector(string[] errorsAsWarnings, string[] ignoreErrors, int indentationAmount)
        {
            this.errorsAsWarnings = new Hashtable();
            this.ignoreErrors = new Hashtable();
            this.indentationAmount = indentationAmount;

            if (null != errorsAsWarnings)
            {
                foreach (string error in errorsAsWarnings)
                {
                    InspectorTestType itt = GetInspectorTestType(error);

                    if (itt != InspectorTestType.Unknown)
                    {
                        this.errorsAsWarnings.Add(itt, null);
                    }
                    else // not a known InspectorTestType
                    {
                        this.OnError(InspectorTestType.InspectorTestTypeUnknown, null, "Unknown error type: '{0}'.", error);
                    }
                }
            }

            if (null != ignoreErrors)
            {
                foreach (string error in ignoreErrors)
                {
                    InspectorTestType itt = GetInspectorTestType(error);

                    if (itt != InspectorTestType.Unknown)
                    {
                        this.ignoreErrors.Add(itt, null);
                    }
                    else // not a known InspectorTestType
                    {
                        this.OnError(InspectorTestType.InspectorTestTypeUnknown, null, "Unknown error type: '{0}'.", error);
                    }
                }
            }
        }

        /// <summary>
        /// Inspector test types.  These are used to condition error messages down to warnings.
        /// </summary>
        private enum InspectorTestType
        {
            /// <summary>
            /// Internal-only: returned when a string cannot be converted to an InspectorTestType.
            /// </summary>
            Unknown,

            /// <summary>
            /// Internal-only: displayed when a string cannot be converted to an InspectorTestType.
            /// </summary>
            InspectorTestTypeUnknown,

            /// <summary>
            /// Displayed when an XML loading exception has occurred.
            /// </summary>
            XmlException,

            /// <summary>
            /// Displayed when a file cannot be accessed; typically when trying to save back a fixed file.
            /// </summary>
            UnauthorizedAccessException,

            /// <summary>
            /// Displayed when the encoding attribute in the XML declaration is not 'UTF-8'.
            /// </summary>
            DeclarationEncodingWrong,

            /// <summary>
            /// Displayed when the XML declaration is missing from the source file.
            /// </summary>
            DeclarationMissing,

            /// <summary>
            /// Displayed when the whitespace preceding a CDATA node is wrong.
            /// </summary>
            WhitespacePrecedingCDATAWrong,

            /// <summary>
            /// Displayed when the whitespace preceding a node is wrong.
            /// </summary>
            WhitespacePrecedingNodeWrong,

            /// <summary>
            /// Displayed when an element is not empty as it should be.
            /// </summary>
            NotEmptyElement,

            /// <summary>
            /// Displayed when the whitespace following a CDATA node is wrong.
            /// </summary>
            WhitespaceFollowingCDATAWrong,

            /// <summary>
            /// Displayed when the whitespace preceding an end element is wrong.
            /// </summary>
            WhitespacePrecedingEndElementWrong,

            /// <summary>
            /// Displayed when the xmlns attribute is missing from the document element.
            /// </summary>
            XmlnsMissing,

            /// <summary>
            /// Displayed when the xmlns attribute on the document element is wrong.
            /// </summary>
            XmlnsValueWrong,

            /// <summary>
            /// Displayed when a Category element has an empty AppData attribute.
            /// </summary>
            CategoryAppDataEmpty,

            /// <summary>
            /// Displayed when a Registry element encounters an error while being converted
            /// to a strongly-typed WiX COM element.
            /// </summary>
            COMRegistrationTyper,

            /// <summary>
            /// Displayed when an UpgradeVersion element has an empty RemoveFeatures attribute.
            /// </summary>
            UpgradeVersionRemoveFeaturesEmpty,

            /// <summary>
            /// Displayed when a Feature element contains the deprecated FollowParent attribute.
            /// </summary>
            FeatureFollowParentDeprecated,

            /// <summary>
            /// Displayed when a RadioButton element is missing the Value attribute.
            /// </summary>
            RadioButtonMissingValue,

            /// <summary>
            /// Displayed when a TypeLib element contains a Description element with an empty
            /// string value.
            /// </summary>
            TypeLibDescriptionEmpty,

            /// <summary>
            /// Displayed when a RelativePath attribute occurs on an unadvertised Class element.
            /// </summary>
            ClassRelativePathMustBeAdvertised,

            /// <summary>
            /// Displayed when a Class element has an empty Description attribute.
            /// </summary>
            ClassDescriptionEmpty,

            /// <summary>
            /// Displayed when a ServiceInstall element has an empty LocalGroup attribute.
            /// </summary>
            ServiceInstallLocalGroupEmpty,

            /// <summary>
            /// Displayed when a ServiceInstall element has an empty Password attribute.
            /// </summary>
            ServiceInstallPasswordEmpty,

            /// <summary>
            /// Displayed when a Shortcut element has an empty WorkingDirectory attribute.
            /// </summary>
            ShortcutWorkingDirectoryEmpty,

            /// <summary>
            /// Displayed when a IniFile element has an empty Value attribute.
            /// </summary>
            IniFileValueEmpty,

            /// <summary>
            /// Displayed when a FileSearch element has a Name attribute that contains both the short
            /// and long versions of the file name.
            /// </summary>
            FileSearchNamesCombined,

            /// <summary>
            /// Displayed when a WebApplicationExtension element has a deprecated Id attribute.
            /// </summary>
            WebApplicationExtensionIdDeprecated,

            /// <summary>
            /// Displayed when a WebApplicationExtension element has an empty Id attribute.
            /// </summary>
            WebApplicationExtensionIdEmpty,

            /// <summary>
            /// Displayed when a Property element has an empty Value attribute.
            /// </summary>
            PropertyValueEmpty,

            /// <summary>
            /// Displayed when a Control element has an empty CheckBoxValue attribute.
            /// </summary>
            ControlCheckBoxValueEmpty,

            /// <summary>
            /// Displayed when a deprecated RadioGroup element is found.
            /// </summary>
            RadioGroupDeprecated,

            /// <summary>
            /// Displayed when a Progress element has an empty TextTemplate attribute.
            /// </summary>
            ProgressTextTemplateEmpty,

            /// <summary>
            /// Displayed when a RegistrySearch element has a Type attribute set to 'registry'.
            /// </summary>
            RegistrySearchTypeRegistryDeprecated,

            /// <summary>
            /// Displayed when a WebFilter/@LoadOrder attribute has a value that is not more stongly typed.
            /// </summary>
            WebFilterLoadOrderIncorrect,

            /// <summary>
            /// Displayed when an element contains a deprecated src attribute.
            /// </summary>
            SrcIsDeprecated,

            /// <summary>
            /// Displayed when a Component element is missing the required Guid attribute.
            /// </summary>
            RequireComponentGuid,

            /// <summary>
            /// Displayed when an element has a LongName attribute.
            /// </summary>
            LongNameDeprecated,

            /// <summary>
            /// Displayed when a RemoveFile element has no Name or LongName attribute.
            /// </summary>
            RemoveFileNameRequired,

            /// <summary>
            /// Displayed when a localization variable begins with the deprecated '$' character.
            /// </summary>
            DeprecatedLocalizationVariablePrefix,

            /// <summary>
            /// Displayed when the namespace of an element has changed.
            /// </summary>
            NamespaceChanged,

            /// <summary>
            /// Displayed when an UpgradeVersion element is missing the required Property attribute.
            /// </summary>
            UpgradeVersionPropertyAttributeRequired,

            /// <summary>
            /// Displayed when an Upgrade element contains a deprecated Property child element.
            /// </summary>
            UpgradePropertyChild,

            /// <summary>
            /// Displayed when a deprecated Registry element is found.
            /// </summary>
            RegistryElementDeprecated,

            /// <summary>
            /// Displayed when a PatchSequence/@Supersede attribute contains a deprecated integer value.
            /// </summary>
            PatchSequenceSupersedeTypeChanged,

            /// <summary>
            /// Displayed when a deprecated PatchSequence/@Target attribute is found.
            /// </summary>
            PatchSequenceTargetDeprecated,

            /// <summary>
            /// Displayed when a deprecated Verb/@Target attribute is found.
            /// </summary>
            VerbTargetDeprecated,

            /// <summary>
            /// Displayed when a ProgId/@Icon attribute value contains a formatted string.
            /// </summary>
            ProgIdIconFormatted,

            /// <summary>
            /// Displayed when a deprecated IgnoreModularization element is found.
            /// </summary>
            IgnoreModularizationDeprecated,

            /// <summary>
            /// Displayed when a Package/@Compressed attribute is found under a Module element.
            /// </summary>
            PackageCompressedIllegal,

            /// <summary>
            /// Displayed when a Package/@Platforms attribute is found.
            /// </summary>
            PackagePlatformsDeprecated,

            /// <summary>
            /// Displayed when a Package/@Platform attribute has the deprecated value "intel"
            /// </summary>
            PackagePlatformIntel,

            /// <summary>
            /// Displayed when a Package/@Platform attribute has the deprecated value "intel64"
            /// </summary>
            PackagePlatformIntel64,

            /// <summary>
            /// Displayed when a deprecated Module/@Guid attribute is found.
            /// </summary>
            ModuleGuidDeprecated,

            /// <summary>
            /// Displayed when a deprecated guid wildcard value is found.
            /// </summary>
            GuidWildcardDeprecated,

            /// <summary>
            /// Displayed when a FragmentRef Element is found.
            /// </summary>
            FragmentRefIllegal,

            /// <summary>
            /// Displayed when a File/@Name matches a File/@ShortName.
            /// </summary>
            FileRedundantNames,

            /// <summary>
            /// Displayed when a FileSearch(Ref) element has an invalid parent.
            /// </summary>
            FileSearchParentInvalid,

            /// <summary>
            /// Displayed when an optional attribute is specified with its default value.
            /// </summary>
            DefaultOptionalAttribute,

            /// <summary>
            /// Displayed when an attribute that WiX generates is specified with an explicit value.
            /// (Rarely an error but indicates authoring that likely can be simplified.)
            /// </summary>
            ExplicitGeneratedAttribute,

            /// <summary>
            /// Displayed when an identifier for a ComponentGroup or ComponentGroupRef contains invalid characters..
            /// </summary>
            InvalidIdentifier
        }

        /// <summary>
        /// Inspect a file.
        /// </summary>
        /// <param name="inspectSourceFile">The file to inspect.</param>
        /// <param name="fixErrors">Option to fix errors that are found.</param>
        /// <returns>The number of errors found.</returns>
        public int InspectFile(string inspectSourceFile, bool fixErrors)
        {
            XmlTextReader reader = null;
            XmlWriter writer = null;
            LineInfoDocument doc = null;

            try
            {
                // set the instance info
                this.errors = 0;
                this.sourceFile = inspectSourceFile;

                // load the xml
                reader = new XmlTextReader(this.sourceFile);
                doc = new LineInfoDocument();
                doc.PreserveWhitespace = true;
                doc.Load(reader);
            }
            catch (XmlException xe)
            {
                this.OnError(InspectorTestType.XmlException, null, "The xml is invalid.  Detail: '{0}'", xe.Message);
                return this.errors;
            }
            finally
            {
                if (null != reader)
                {
                    reader.Close();
                }
            }

            // inspect the document
            this.InspectDocument(doc);

            // fix errors if necessary
            if (fixErrors && 0 < this.errors)
            {
                try
                {
                    using (StreamWriter sw = File.CreateText(inspectSourceFile))
                    {
                        writer = new XmlTextWriter(sw);
                        doc.WriteTo(writer);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    this.OnError(InspectorTestType.UnauthorizedAccessException, null, "Could not write to file.");
                }
                finally
                {
                    if (null != writer)
                    {
                        writer.Close();
                    }
                }
            }

            return this.errors;
        }

        /// <summary>
        /// Get the strongly-typed InspectorTestType for a string representation of the same.
        /// </summary>
        /// <param name="inspectorTestType">The InspectorTestType represented by the string.</param>
        /// <returns>The InspectorTestType value if found; otherwise InspectorTestType.Unknown.</returns>
        private static InspectorTestType GetInspectorTestType(string inspectorTestType)
        {
            foreach (InspectorTestType itt in Enum.GetValues(typeof(InspectorTestType)))
            {
                if (itt.ToString() == inspectorTestType)
                {
                    return itt;
                }
            }

            return InspectorTestType.Unknown;
        }

        /// <summary>
        /// Fix the whitespace in a Whitespace node.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level of the desired whitespace.</param>
        /// <param name="whitespace">The whitespace node to fix.</param>
        private static void FixWhitespace(int indentationAmount, int level, XmlNode whitespace)
        {
            int newLineCount = 0;

            for (int i = 0; i + 1 < whitespace.Value.Length; i++)
            {
                if (Environment.NewLine == whitespace.Value.Substring(i, 2))
                {
                    i++; // skip an extra character
                    newLineCount++;
                }
            }

            if (0 == newLineCount)
            {
                newLineCount = 1;
            }

            // reset the whitespace value
            whitespace.Value = string.Empty;

            // add the correct number of newlines
            for (int i = 0; i < newLineCount; i++)
            {
                whitespace.Value = string.Concat(whitespace.Value, Environment.NewLine);
            }

            // add the correct number of spaces based on configured indentation amount
            whitespace.Value = string.Concat(whitespace.Value, new string(' ', level * indentationAmount));
        }

        /// <summary>
        /// Replace one element with another, copying all the attributes and child nodes.
        /// </summary>
        /// <param name="sourceElement">The source element.</param>
        /// <param name="destinationElement">The destination element.</param>
        private static void ReplaceElement(XmlElement sourceElement, XmlElement destinationElement)
        {
            if (sourceElement == sourceElement.OwnerDocument.DocumentElement)
            {
                sourceElement.OwnerDocument.ReplaceChild(destinationElement, sourceElement);
            }
            else
            {
                sourceElement.ParentNode.ReplaceChild(destinationElement, sourceElement);
            }

            // move all the attributes from the old element to the new element
            SortedList xmlnsAttributes = new SortedList();
            while (sourceElement.Attributes.Count > 0)
            {
                XmlAttribute attribute = sourceElement.Attributes[0];

                sourceElement.Attributes.Remove(attribute);

                // migrate any attribute other than xmlns
                if (attribute.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                {
                    // migrate prefix xmlns attribute after all normal attributes
                    if ("xmlns" == attribute.LocalName)
                    {
                        attribute.Value = destinationElement.NamespaceURI;
                        xmlnsAttributes.Add(String.Empty, attribute);
                    }
                    else
                    {
                        xmlnsAttributes.Add(attribute.LocalName, attribute);
                    }
                }
                else
                {
                    destinationElement.Attributes.Append(attribute);
                }
            }

            // add the xmlns attributes back in alphabetical order
            foreach (XmlAttribute attribute in xmlnsAttributes.Values)
            {
                destinationElement.Attributes.Append(attribute);
            }

            // move all the child nodes from the old element to the new element
            while (sourceElement.ChildNodes.Count > 0)
            {
                XmlNode node = sourceElement.ChildNodes[0];

                sourceElement.RemoveChild(node);
                destinationElement.AppendChild(node);
            }
        }

        /// <summary>
        /// Set the namespace URI for an element and all its children.
        /// </summary>
        /// <param name="element">The element which will get its namespace URI set.</param>
        /// <param name="namespaceURI">The namespace URI to set.</param>
        /// <returns>The modified element.</returns>
        private static XmlElement SetNamespaceURI(XmlElement element, string namespaceURI)
        {
            XmlElement newElement = element.OwnerDocument.CreateElement(element.LocalName, namespaceURI);

            ReplaceElement(element, newElement);

            for (int i = 0; i < newElement.ChildNodes.Count; i++)
            {
                XmlNode childNode = newElement.ChildNodes[i];

                if (XmlNodeType.Element == childNode.NodeType && childNode.NamespaceURI == element.NamespaceURI)
                {
                    SetNamespaceURI((XmlElement)childNode, namespaceURI);
                }
            }

            return newElement;
        }

        /// <summary>
        /// Determine if the whitespace preceding a node is appropriate for its depth level.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level that should match this whitespace.</param>
        /// <param name="whitespace">The whitespace to validate.</param>
        /// <returns>true if the whitespace is legal; false otherwise.</returns>
        private static bool IsLegalWhitespace(int indentationAmount, int level, string whitespace)
        {
            // strip off leading newlines; there can be an arbitrary number of these
            while (whitespace.StartsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                whitespace = whitespace.Substring(Environment.NewLine.Length);
            }

            // check the length
            if (whitespace.Length != level * indentationAmount)
            {
                return false;
            }

            // check the spaces
            foreach (char character in whitespace)
            {
                if (' ' != character)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Inspect an XML document.
        /// </summary>
        /// <param name="doc">The XML document to inspect.</param>
        private void InspectDocument(XmlDocument doc)
        {
            // inspect the declaration
            if (XmlNodeType.XmlDeclaration == doc.FirstChild.NodeType)
            {
                XmlDeclaration declaration = (XmlDeclaration)doc.FirstChild;

                if (!String.Equals("utf-8", declaration.Encoding, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.OnError(InspectorTestType.DeclarationEncodingWrong, declaration, "The XML declaration encoding is not properly set to 'utf-8'."))
                    {
                        declaration.Encoding = "utf-8";
                    }
                }
            }
            else // missing declaration
            {
                if (this.OnError(InspectorTestType.DeclarationMissing, null, "This file is missing an XML declaration on the first line."))
                {
                    doc.PrependChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
                }
            }

            // start inspecting the nodes at the document element
            this.InspectNode(doc.DocumentElement, 0);
        }

        /// <summary>
        /// Inspect a single xml node.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="level">The depth level of the node.</param>
        /// <returns>The inspected node.</returns>
        private XmlNode InspectNode(XmlNode node, int level)
        {
            // inspect this node's whitespace
            if ((XmlNodeType.Comment == node.NodeType && 0 > node.Value.IndexOf(Environment.NewLine, StringComparison.Ordinal)) ||
                XmlNodeType.CDATA == node.NodeType || XmlNodeType.Element == node.NodeType || XmlNodeType.ProcessingInstruction == node.NodeType)
            {
                this.InspectWhitespace(node, level);
            }

            // inspect this node
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    // first inspect the attributes of the node in a very generic fashion
                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        this.InspectAttribute(attribute);
                    }

                    // inspect the node in much greater detail
                    if ("http://schemas.microsoft.com/wix/2005/10/sca" == node.NamespaceURI)
                    {
                        switch (node.LocalName)
                        {
                            // IIS elements
                            case "Certificate":
                            case "CertificateRef":
                            case "MimeMap":
                            case "RecycleTime":
                            case "WebAddress":
                            case "WebApplication":
                            case "WebApplicationExtension":
                            case "WebAppPool":
                            case "WebDir":
                            case "WebDirProperties":
                            case "WebError":
                            case "WebFilter":
                            case "WebLog":
                            case "WebProperty":
                            case "WebServiceExtension":
                            case "WebSite":
                            case "WebVirtualDir":
                                node = this.InspectIIsElement((XmlElement)node);
                                break;

                            // SQL Server elements
                            case "SqlDatabase":
                            case "SqlFileSpec":
                            case "SqlLogFileSpec":
                            case "SqlScript":
                            case "SqlString":
                                node = this.InspectSqlElement((XmlElement)node);
                                break;

                            // Utility elements
                            case "FileShare":
                            case "FileSharePermission":
                            case "Group":
                            case "GroupRef":
                            case "PerfCounter":
                            case "Permission":
                            case "User":
                                node = this.InspectUtilElement((XmlElement)node);
                                break;
                            default:
                                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Unknown sca extension element '{0}'.", node.LocalName));
                        }
                    }
                    else if ("http://schemas.microsoft.com/wix/HelpExtension" == node.NamespaceURI)
                    {
                        node = this.InspectHelpElement((XmlElement)node);
                    }
                    else
                    {
                        XmlElement element = (XmlElement)node;

                        switch (node.LocalName)
                        {
                            case "Binary":
                                this.InspectBinaryElement(element);
                                break;
                            case "Category":
                                this.InspectCategoryElement(element);
                                break;
                            case "Class":
                                this.InspectClassElement(element);
                                break;
                            case "Component":
                                this.InspectComponentElement(element);
                                break;
                            case "ComponentGroup":
                                this.InspectComponentGroupElement(element);
                                break;
                            case "ComponentGroupRef":
                                this.InspectComponentGroupRefElement(element);
                                break;
                            case "Control":
                                this.InspectControlElement(element);
                                break;
                            case "CopyFile":
                                this.InspectCopyFileElement(element);
                                break;
                            case "DigitalCertificate":
                                this.InspectDigitalCertificateElement(element);
                                break;
                            case "DigitalSignature":
                                this.InspectDigitalSignatureElement(element);
                                break;
                            case "Directory":
                                this.InspectDirectoryElement(element);
                                break;
                            case "DirectoryRef":
                                this.InspectDirectoryRefElement(element);
                                break;
                            case "ExternalFile":
                                this.InspectExternalFileElement(element);
                                break;
                            case "Feature":
                                this.InspectFeatureElement(element);
                                break;
                            case "File":
                                this.InspectFileElement(element);
                                break;
                            case "FileSearch":
                                this.InspectFileSearchElement(element);
                                break;
                            case "FragmentRef":
                                this.InspectFragmentRefElement(element);
                                break;
                            case "Icon":
                                this.InspectIconElement(element);
                                break;
                            case "IgnoreModularization":
                                node = this.InspectIgnoreModularizationElement(element);
                                break;
                            case "Include":
                                node = this.InspectIncludeElement(element);
                                break;
                            case "IniFile":
                                this.InspectIniFileElement(element);
                                break;
                            case "IniFileSearch":
                                this.InspectIniFileSearchElement(element);
                                break;
                            case "Media":
                                this.InspectMediaElement(element);
                                break;
                            case "Merge":
                                this.InspectMergeElement(element);
                                break;
                            case "Module":
                                this.InspectModuleElement(element);
                                break;
                            case "Package":
                                this.InspectPackageElement(element);
                                break;
                            case "PatchSequence":
                                this.InspectPatchSequenceElement(element);
                                break;
                            case "Permission":
                                node = this.InspectPermissionElement(element);
                                break;
                            case "Product":
                                this.InspectProductElement(element);
                                break;
                            case "ProgId":
                                this.InspectProgIdElement(element);
                                break;
                            case "ProgressText":
                                this.InspectProgressTextElement(element);
                                break;
                            case "Property":
                                this.InspectPropertyElement(element);
                                break;
                            case "RadioButton":
                                this.InspectRadioButtonElement(element);
                                break;
                            case "RadioGroup":
                                this.InspectRadioGroupElement(element);
                                break;
                            case "Registry":
                                node = this.InspectRegistryElement(element);
                                break;
                            case "RegistrySearch":
                                this.InspectRegistrySearchElement(element);
                                break;
                            case "RemoveFile":
                                this.InspectRemoveFileElement(element);
                                break;
                            case "ServiceInstall":
                                this.InspectServiceInstallElement(element);
                                break;
                            case "SFPCatalog":
                                this.InspectSFPCatalogElement(element);
                                break;
                            case "Shortcut":
                                this.InspectShortcutElement(element);
                                break;
                            case "TargetImage":
                                this.InspectTargetImageElement(element);
                                break;
                            case "Text":
                                this.InspectTextElement(element);
                                break;
                            case "TypeLib":
                                this.InspectTypeLibElement(element);
                                break;
                            case "Upgrade":
                                this.InspectUpgradeElement(element);
                                break;
                            case "UpgradeImage":
                                this.InspectUpgradeImageElement(element);
                                break;
                            case "UpgradeVersion":
                                this.InspectUpgradeVersionElement(element);
                                break;
                            case "Verb":
                                this.InspectVerbElement(element);
                                break;
                            case "WebApplicationExtension":
                                node = this.InspectWebApplicationExtensionElement(element);
                                break;
                            case "WebFilter":
                                node = this.InspectWebFilterElement(element);
                                break;
                            case "Wix":
                                node = this.InspectWixElement(element);
                                break;
                            case "WixLocalization":
                                node = this.InspectWixLocalizationElement(element);
                                break;

                            // IIS elements
                            case "Certificate":
                            case "CertificateRef":
                            case "HttpHeader":
                            case "MimeMap":
                            case "RecycleTime":
                            case "WebAddress":
                            case "WebApplication":
                            case "WebAppPool":
                            case "WebDir":
                            case "WebDirProperties":
                            case "WebError":
                            case "WebLog":
                            case "WebProperty":
                            case "WebServiceExtension":
                            case "WebSite":
                            case "WebVirtualDir":
                                node = this.InspectIIsElement(element);
                                break;

                            // SQL Server elements
                            case "SqlDatabase":
                            case "SqlFileSpec":
                            case "SqlLogFileSpec":
                            case "SqlScript":
                            case "SqlString":
                                node = this.InspectSqlElement(element);
                                break;

                            // Utility elements
                            case "FileShare":
                            case "Group":
                            case "GroupRef":
                            case "PerfCounter":
                            case "ServiceConfig":
                            case "User":
                            case "XmlFile":
                            case "XmlConfig":
                                node = this.InspectUtilElement(element);
                                break;
                        }
                    }
                    break;
                case XmlNodeType.Text:
                    this.InspectText((XmlText)node);
                    break;
            }

            // inspect all children of this node
            if (null != node)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    XmlNode child = node.ChildNodes[i];

                    XmlNode inspectedNode = this.InspectNode(child, level + 1);

                    // inspected node was deleted, don't skip the next one
                    if (null == inspectedNode)
                    {
                        i--;
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// Inspect an attribute.
        /// </summary>
        /// <param name="attribute">The attribute to inspect.</param>
        private void InspectAttribute(XmlAttribute attribute)
        {
            MatchCollection matches = WixVariableRegex.Matches(attribute.Value);

            foreach (Match match in matches)
            {
                if ('$' == attribute.Value[match.Index])
                {
                    if (this.OnError(InspectorTestType.DeprecatedLocalizationVariablePrefix, attribute, "The localization variable $(loc.{0}) uses a deprecated prefix '$'.  Please use the '!' prefix instead.  Since the prefix '$' is also used by the preprocessor, it has been deprecated to avoid namespace collisions.", match.Groups["name"].Value))
                    {
                        attribute.Value = attribute.Value.Remove(match.Index, 1);
                        attribute.Value = attribute.Value.Insert(match.Index, "!");
                    }
                }
            }
        }

        /// <summary>
        /// Inspect a text node.
        /// </summary>
        /// <param name="text">The text node to inspect.</param>
        private void InspectText(XmlText text)
        {
            MatchCollection matches = WixVariableRegex.Matches(text.Value);

            foreach (Match match in matches)
            {
                if ('$' == text.Value[match.Index])
                {
                    if (this.OnError(InspectorTestType.DeprecatedLocalizationVariablePrefix, text, "The localization variable $(loc.{0}) uses a deprecated prefix '$'.  Please use the '!' prefix instead.  Since the prefix '$' is also used by the preprocessor, it has been deprecated to avoid namespace collisions.", match.Groups["name"].Value))
                    {
                        text.Value = text.Value.Remove(match.Index, 1);
                        text.Value = text.Value.Insert(match.Index, "!");
                    }
                }
            }
        }

        /// <summary>
        /// Inspect the whitespace adjacent to a node.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="level">The depth level of the node.</param>
        private void InspectWhitespace(XmlNode node, int level)
        {
            // fix the whitespace before this node
            XmlNode whitespace = node.PreviousSibling;
            if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
            {
                if (XmlNodeType.CDATA == node.NodeType)
                {
                    if (this.OnError(InspectorTestType.WhitespacePrecedingCDATAWrong, node, "There should be no whitespace preceding a CDATA node."))
                    {
                        whitespace.ParentNode.RemoveChild(whitespace);
                    }
                }
                else
                {
                    if (!IsLegalWhitespace(this.indentationAmount, level, whitespace.Value))
                    {
                        if (this.OnError(InspectorTestType.WhitespacePrecedingNodeWrong, node, "The whitespace preceding this node is incorrect."))
                        {
                            FixWhitespace(this.indentationAmount, level, whitespace);
                        }
                    }
                }
            }

            // fix the whitespace inside this node (except for Error which may contain just whitespace)
            if (XmlNodeType.Element == node.NodeType && "Error" != node.LocalName)
            {
                XmlElement element = (XmlElement)node;

                if (!element.IsEmpty && String.IsNullOrEmpty(element.InnerXml.Trim()))
                {
                    if (this.OnError(InspectorTestType.NotEmptyElement, element, "This should be an empty element since it contains nothing but whitespace."))
                    {
                        element.IsEmpty = true;
                    }
                }
            }

            // fix the whitespace before the end element or after for CDATA nodes
            if (XmlNodeType.CDATA == node.NodeType)
            {
                whitespace = node.NextSibling;
                if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
                {
                    if (this.OnError(InspectorTestType.WhitespaceFollowingCDATAWrong, node, "There should be no whitespace following a CDATA node."))
                    {
                        whitespace.ParentNode.RemoveChild(whitespace);
                    }
                }
            }
            else if (XmlNodeType.Element == node.NodeType)
            {
                whitespace = node.LastChild;

                // Error may contain just whitespace
                if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType && "Error" != node.LocalName)
                {
                    if (!IsLegalWhitespace(this.indentationAmount, level, whitespace.Value))
                    {
                        if (this.OnError(InspectorTestType.WhitespacePrecedingEndElementWrong, whitespace, "The whitespace preceding this end element is incorrect."))
                        {
                            FixWhitespace(this.indentationAmount, level, whitespace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a Binary element.
        /// </summary>
        /// <param name="element">The Binary element to inspect.</param>
        private void InspectBinaryElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Binary/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {
                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a Category element.
        /// </summary>
        /// <param name="element">The Category element to inspect.</param>
        private void InspectCategoryElement(XmlElement element)
        {
            if (element.HasAttribute("AppData"))
            {
                if (String.IsNullOrEmpty(element.GetAttribute("AppData")))
                {
                    if (this.OnError(InspectorTestType.CategoryAppDataEmpty, element, "The Category/@AppData attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute."))
                    {
                        element.RemoveAttribute("AppData");
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a Class element.
        /// </summary>
        /// <param name="element">The Class element to inspect.</param>
        private void InspectClassElement(XmlElement element)
        {
            bool advertised = false;
            XmlAttribute description = element.GetAttributeNode("Description");
            XmlAttribute relativePath = element.GetAttributeNode("RelativePath");

            if (null != description && String.IsNullOrEmpty(description.Value))
            {
                if (this.OnError(InspectorTestType.ClassDescriptionEmpty, element, "The Class/@Description attribute's value cannot be an empty string."))
                {
                    element.Attributes.Remove(description);
                }
            }

            if (null != relativePath)
            {
                // check if this element or any of its parents is advertised
                for (XmlNode node = element; null != node; node = node.ParentNode)
                {
                    if (node.Attributes != null)
                    {
                        XmlNode advertise = node.Attributes.GetNamedItem("Advertise");

                        if (null != advertise && "yes" == advertise.Value)
                        {
                            advertised = true;
                        }
                    }
                }
                
                if (advertised) // if advertised, then RelativePath="no" is unnecessary since its the default value
                {
                    if ("no" == relativePath.Value)
                    {
                        this.OnVerbose(element, "The Class/@RelativePath attribute with value 'no' is not necessary since this element is advertised.");
                        element.Attributes.Remove(relativePath);
                    }
                }
                else // if there is no advertising, then the RelativePath attribute is not allowed
                {
                    if (this.OnError(InspectorTestType.ClassRelativePathMustBeAdvertised, element, "The Class/@RelativePath attribute is not supported for non-advertised Class elements."))
                    {
                        element.Attributes.Remove(relativePath);
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a Component element.
        /// </summary>
        /// <param name="element">The Component element to inspect.</param>
        private void InspectComponentElement(XmlElement element)
        {
            XmlAttribute driverAddRemovePrograms = element.GetAttributeNode("DriverAddRemovePrograms");
            XmlAttribute driverDeleteFiles = element.GetAttributeNode("DriverDeleteFiles");
            XmlAttribute driverForceInstall = element.GetAttributeNode("DriverForceInstall");
            XmlAttribute driverLegacy = element.GetAttributeNode("DriverLegacy");
            XmlAttribute driverPlugAndPlayPrompt = element.GetAttributeNode("DriverPlugAndPlayPrompt");
            XmlAttribute driverSequence = element.GetAttributeNode("DriverSequence");
            XmlAttribute guid = element.GetAttributeNode("Guid");
            XmlAttribute diskId = element.GetAttributeNode("DiskId");

            if (null != driverAddRemovePrograms || null != driverDeleteFiles || null != driverForceInstall ||
                null != driverLegacy || null != driverPlugAndPlayPrompt || null != driverSequence)
            {
                XmlElement driver = element.OwnerDocument.CreateElement("difxapp", "Driver", "http://schemas.microsoft.com/wix/DifxAppExtension");

                if (this.OnError(InspectorTestType.NamespaceChanged, element, String.Format(CultureInfo.CurrentCulture, "The Component/@Driver* attributes are now set via the Driver element which is part of the DifxApp extension.  An xmlns:difxapp=\"http://schemas.microsoft.com/wix/DifxAppExtension\" attribute should be added to the Wix element and these attributes should be moved to a 'difxapp:Driver' element without the 'Driver' prefix.")))
                {
                    element.InsertAfter(driver, element.FirstChild);
                }

                // make a best-effort at handling the whitespace, this isn't guaranteed to work, so wixcop may need to be run more than once
                if (XmlNodeType.Whitespace == driver.PreviousSibling.NodeType)
                {
                    element.InsertAfter(driver.PreviousSibling.Clone(), driver);
                }

                if (null != driverAddRemovePrograms)
                {
                    element.Attributes.Remove(driverAddRemovePrograms);

                    XmlAttribute addRemovePrograms = element.OwnerDocument.CreateAttribute("AddRemovePrograms");
                    addRemovePrograms.Value = driverAddRemovePrograms.Value;
                    driver.Attributes.Append(addRemovePrograms);
                }

                if (null != driverDeleteFiles)
                {
                    element.Attributes.Remove(driverDeleteFiles);

                    XmlAttribute deleteFiles = element.OwnerDocument.CreateAttribute("DeleteFiles");
                    deleteFiles.Value = driverDeleteFiles.Value;
                    driver.Attributes.Append(deleteFiles);
                }

                if (null != driverForceInstall)
                {
                    element.Attributes.Remove(driverForceInstall);

                    XmlAttribute forceInstall = element.OwnerDocument.CreateAttribute("ForceInstall");
                    forceInstall.Value = driverForceInstall.Value;
                    driver.Attributes.Append(forceInstall);
                }

                if (null != driverLegacy)
                {
                    element.Attributes.Remove(driverLegacy);

                    XmlAttribute legacy = element.OwnerDocument.CreateAttribute("Legacy");
                    legacy.Value = driverLegacy.Value;
                    driver.Attributes.Append(legacy);
                }

                if (null != driverPlugAndPlayPrompt)
                {
                    element.Attributes.Remove(driverPlugAndPlayPrompt);

                    XmlAttribute plugAndPlayPrompt = element.OwnerDocument.CreateAttribute("PlugAndPlayPrompt");
                    plugAndPlayPrompt.Value = driverPlugAndPlayPrompt.Value;
                    driver.Attributes.Append(plugAndPlayPrompt);
                }

                if (null != driverSequence)
                {
                    element.Attributes.Remove(driverSequence);

                    XmlAttribute sequence = element.OwnerDocument.CreateAttribute("Sequence");
                    sequence.Value = driverSequence.Value;
                    driver.Attributes.Append(sequence);
                }

                // put the difxapp xmlns attribute on the root element
                if (null == element.OwnerDocument.DocumentElement.Attributes["xmlns:difxapp"])
                {
                    element.OwnerDocument.DocumentElement.SetAttribute("xmlns:difxapp", "http://schemas.microsoft.com/wix/DifxAppExtension");
                }
            }

            if (null != diskId)
            {
                int numericDiskId;
                if (Int32.TryParse(diskId.Value, out numericDiskId) && 1 == numericDiskId)
                {
                    if (this.OnError(InspectorTestType.DefaultOptionalAttribute, diskId, "The Component/@DiskId value is set to its default value of 1. Omit the DiskId attribute for simplified authoring."))
                    {
                        element.Attributes.Remove(diskId);
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a Control element.
        /// </summary>
        /// <param name="element">The Control element to inspect.</param>
        private void InspectControlElement(XmlElement element)
        {
            XmlAttribute checkBoxValue = element.GetAttributeNode("CheckBoxValue");

            if (null != checkBoxValue && String.IsNullOrEmpty(checkBoxValue.Value))
            {
                if (this.OnError(InspectorTestType.IniFileValueEmpty, element, "The Control/@CheckBoxValue attribute's value cannot be the empty string."))
                {
                    element.Attributes.Remove(checkBoxValue);
                }
            }
        }

        /// <summary>
        /// Inspects a ComponentGroup element.
        /// </summary>
        /// <param name="element">The ComponentGroup element to inspect.</param>
        private void InspectComponentGroupElement(XmlElement element)
        {
            XmlAttribute id = element.GetAttributeNode("Id");

            if (null != id)
            {
                string newIdentifier = GetIdentifierFromName(id.Value);
                if (!String.Equals(id.Value, newIdentifier,StringComparison.Ordinal) &&
                    this.OnError(InspectorTestType.InvalidIdentifier, id, "The ComponentGroup/@Id specified contains invalid characters. Remove or replace the invalid characters."))
                {
                    id.Value = newIdentifier;
                }
            }
        }

        /// <summary>
        /// Inspects a ComponentGroupRef element.
        /// </summary>
        /// <param name="element">The ComponentGroupRef element to inspect.</param>
        private void InspectComponentGroupRefElement(XmlElement element)
        {
            XmlAttribute id = element.GetAttributeNode("Id");

            if (null != id)
            {
                string newIdentifier = GetIdentifierFromName(id.Value);
                if (!String.Equals(id.Value, newIdentifier, StringComparison.Ordinal) &&
                    this.OnError(InspectorTestType.InvalidIdentifier, id, "The ComponentGroupRef/@Id specified contains invalid characters. Remove or replace the invalid characters."))
                {
                    id.Value = newIdentifier;
                }
            }
        }

        /// <summary>
        /// Inspects a CopyFile element.
        /// </summary>
        /// <param name="element">The CopyFile element to inspect.</param>
        private void InspectCopyFileElement(XmlElement element)
        {
            XmlAttribute longName = element.GetAttributeNode("DestinationLongName");
            XmlAttribute name = element.GetAttributeNode("DestinationName");
            XmlAttribute shortName = element.GetAttributeNode("DestinationShortName");
            XmlAttribute id = element.GetAttributeNode("Id");

            if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The CopyFile/@DestinationLongName attribute has been deprecated.  Use the DestinationName attribute instead."))
                {
                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("DestinationName");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("DestinationShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The CopyFile/@DestinationShortName and DestinationLongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }

            if (null != name && null != shortName)
            {
                if (String.Equals(name.Value, shortName.Value, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.OnError(InspectorTestType.FileRedundantNames, name, "The CopyFile/@DestinationName matches a CopyFile/@DestinationShortName.  These two attributes should not be duplicated."))
                    {
                        element.Attributes.Remove(shortName);
                    }
                }
            }

            if (null != name && null != id && String.Equals(name.Value, id.Value, StringComparison.OrdinalIgnoreCase))
            {
                if (this.OnError(InspectorTestType.DefaultOptionalAttribute, id, "The CopyFile/@Id value is identical to what WiX generates from the @Name value. Omit the Id attribute for simplified authoring."))
                {
                    element.Attributes.Remove(id);
                }
            }
        }

        /// <summary>
        /// Inspects a DigitalCertificate element.
        /// </summary>
        /// <param name="element">The DigitalCertificate element to inspect.</param>
        private void InspectDigitalCertificateElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The DigitalCertificate/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {
                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a DigitalSignature element.
        /// </summary>
        /// <param name="element">The DigitalSignature element to inspect.</param>
        private void InspectDigitalSignatureElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The DigitalSignature/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {
                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a Directory element.
        /// </summary>
        /// <param name="element">The Directory element to inspect.</param>
        private void InspectDirectoryElement(XmlElement element)
        {
            XmlAttribute longName = element.GetAttributeNode("LongName");
            XmlAttribute longSourceName = element.GetAttributeNode("LongSourceName");
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The Directory/@LongName attribute has been deprecated.  Use the Name attribute instead."))
                {
                    XmlAttribute name = element.GetAttributeNode("Name");


                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("Name");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        XmlAttribute shortName = element.GetAttributeNode("ShortName");

                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("ShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The Directory/@ShortName and LongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }

            if (null != longSourceName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longSourceName, "The Directory/@LongSource attribute has been deprecated.  Use the SourceName attribute instead."))
                {
                    XmlAttribute sourceName = element.GetAttributeNode("SourceName");

                    if (null == sourceName)
                    {
                        sourceName = element.OwnerDocument.CreateAttribute("SourceName");
                        sourceName.Value = longSourceName.Value;
                        element.Attributes.InsertAfter(sourceName, longSourceName);
                    }
                    else // move the value of SourceName to ShortSourceName
                    {
                        XmlAttribute shortSourceName = element.GetAttributeNode("ShortSourceName");

                        if (null == shortSourceName)
                        {
                            shortSourceName = element.OwnerDocument.CreateAttribute("ShortSourceName");
                            shortSourceName.Value = sourceName.Value;
                            element.Attributes.InsertBefore(shortSourceName, sourceName);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The Directory/@ShortSourceName and LongSource attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        sourceName.Value = longSourceName.Value;
                    }

                    element.Attributes.Remove(longSourceName);
                }
            }

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Directory/@src attribute has been deprecated.  Use the FileSource attribute instead."))
                {
                    XmlAttribute fileSource = element.OwnerDocument.CreateAttribute("FileSource");
                    fileSource.Value = src.Value;
                    element.Attributes.InsertAfter(fileSource, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a DirectoryRef element.
        /// </summary>
        /// <param name="element">The DirectoryRef element to inspect.</param>
        private void InspectDirectoryRefElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The DirectoryRef/@src attribute has been deprecated.  Use the FileSource attribute instead."))
                {
                    XmlAttribute fileSource = element.OwnerDocument.CreateAttribute("FileSource");
                    fileSource.Value = src.Value;
                    element.Attributes.InsertAfter(fileSource, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects an ExternalFile element.
        /// </summary>
        /// <param name="element">The ExternalFile element to inspect.</param>
        private void InspectExternalFileElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The ExternalFile/@src attribute has been deprecated.  Use the Source attribute instead."))
                {
                    XmlAttribute source = element.OwnerDocument.CreateAttribute("Source");
                    source.Value = src.Value;
                    element.Attributes.InsertAfter(source, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a Feature element.
        /// </summary>
        /// <param name="element">The Feature element to inspect.</param>
        private void InspectFeatureElement(XmlElement element)
        {
            XmlAttribute followParent = element.GetAttributeNode("FollowParent");
            XmlAttribute installDefault = element.GetAttributeNode("InstallDefault");

            if (null != followParent)
            {
                if (this.OnError(InspectorTestType.FeatureFollowParentDeprecated, followParent, "The Feature/@FollowParent attribute has been deprecated.  Value of 'yes' should now be represented with InstallDefault='followParent'."))
                {

                    // if InstallDefault is present, candle with display an error
                    if ("yes" == followParent.Value && null == installDefault)
                    {
                        installDefault = element.OwnerDocument.CreateAttribute("InstallDefault");
                        installDefault.Value = "followParent";
                        element.Attributes.Append(installDefault);
                    }

                    element.Attributes.Remove(followParent);
                }
            }
        }

        /// <summary>
        /// Inspects a File element.
        /// </summary>
        /// <param name="element">The File element to inspect.</param>
        private void InspectFileElement(XmlElement element)
        {
            XmlAttribute longName = element.GetAttributeNode("LongName");
            XmlAttribute name = element.GetAttributeNode("Name");
            XmlAttribute shortName = element.GetAttributeNode("ShortName");
            XmlAttribute src = element.GetAttributeNode("src");
            XmlAttribute source = element.GetAttributeNode("Source");
            XmlAttribute diskId = element.GetAttributeNode("DiskId");
            XmlAttribute id = element.GetAttributeNode("Id");
            XmlAttribute vital = element.GetAttributeNode("Vital");

            if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The File/@LongName attribute has been deprecated.  Use the Name attribute instead."))
                {
                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("Name");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("ShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The File/@ShortName and LongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }

            if (null != name && null != shortName)
            {
                if (String.Equals(name.Value, shortName.Value, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.OnError(InspectorTestType.FileRedundantNames, name, "The File/@Name matches a File/@ShortName.  These two attributes should not be duplicated."))
                    {
                        element.Attributes.Remove(shortName);
                    }
                }
            }

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The File/@src attribute has been deprecated.  Use the Source attribute instead."))
                {
                    XmlAttribute sourceAttr = element.OwnerDocument.CreateAttribute("Source");
                    sourceAttr.Value = src.Value;
                    element.Attributes.InsertAfter(sourceAttr, src);

                    element.Attributes.Remove(src);
                }
            }

            if (null != source && null != name && String.Equals(Path.GetFileName(source.Value), name.Value, StringComparison.OrdinalIgnoreCase))
            {
                if (this.OnError(InspectorTestType.DefaultOptionalAttribute, name, "The File/@Name value is identical to what WiX generates from the @Source value. Omit the Name attribute for simplified authoring."))
                {
                    element.Attributes.Remove(name);
                }
            }

            if (null != source && null != id && String.Equals(Path.GetFileName(source.Value), id.Value, StringComparison.OrdinalIgnoreCase))
            {
                if (this.OnError(InspectorTestType.DefaultOptionalAttribute, id, "The File/@Id value is identical to what WiX generates from the @Source value. Omit the Id attribute for simplified authoring."))
                {
                    element.Attributes.Remove(id);
                }
            }

            if (null != diskId)
            {
                int numericDiskId;
                if (Int32.TryParse(diskId.Value, out numericDiskId) && 1 == numericDiskId)
                {
                    if (this.OnError(InspectorTestType.DefaultOptionalAttribute, diskId, "The File/@DiskId value is set to its default value of 1. Omit the DiskId attribute for simplified authoring."))
                    {
                        element.Attributes.Remove(diskId);
                    }
                }
            }

            if (null != vital)
            {
                if ("yes" == vital.Value)
                {
                    if (this.OnError(InspectorTestType.DefaultOptionalAttribute, diskId, "The File/@Vital value is set to its default value of yes (unless -sfdvital switch is used). Omit the Vital attribute for simplified authoring."))
                    {
                        element.Attributes.Remove(vital);
                    }
                }
            }

            if (null != shortName)
            {
                if (this.OnError(InspectorTestType.ExplicitGeneratedAttribute, shortName, "The File/@ShortName value is specified but is almost never needed; WiX generates stable short names from the @Name value and the parent Component/@Id value. Omit @ShortName for simplified authoring."))
                {
                    element.Attributes.Remove(shortName);
                }
            }
        }

        /// <summary>
        /// Inspects a FileSearch element.
        /// </summary>
        /// <param name="element">The FileSearch element to inspect.</param>
        private void InspectFileSearchElement(XmlElement element)
        {
            XmlAttribute name = element.GetAttributeNode("Name");
            XmlAttribute longName = element.GetAttributeNode("LongName");

            // check for a short/long filename separator in the Name attribute
            if (null != name && 0 <= name.Value.IndexOf('|'))
            {
                if (null == longName) // long name is not present, split the Name if possible
                {
                    string[] names = name.Value.Split("|".ToCharArray());

                    // this appears to be splittable
                    if (2 == names.Length)
                    {
                        if (this.OnError(InspectorTestType.FileSearchNamesCombined, element, "The FileSearch/@Name attribute appears to contain both a short and long file name.  It may only contain an 8.3 file name.  Also use the LongName attribute to specify a longer name."))
                        {

                            // fix the short name
                            name.Value = names[0];

                            // insert the new LongName attribute after the previous Name attribute
                            longName = element.OwnerDocument.CreateAttribute("LongName");
                            longName.Value = names[1];
                            element.Attributes.InsertAfter(longName, name);
                        }
                    }
                }
            }

            if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The FileSearch/@LongName attribute has been deprecated.  Use the Name attribute instead."))
                {

                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("Name");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        XmlAttribute shortName = element.GetAttributeNode("ShortName");

                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("ShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The FileSearch/@ShortName and LongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }

            // Check that FileSearch has a valid parent element.
            XmlNode parentNode = element.ParentNode;
            if (0 == String.CompareOrdinal("ComplianceCheck", parentNode.LocalName) || 0 == String.CompareOrdinal("Property", parentNode.LocalName))
            {
                if (this.OnError(InspectorTestType.FileSearchParentInvalid, element, String.Format(CultureInfo.CurrentCulture, @"The FileSearch element is not supported under the parent element {0}. It must instead be located under a DirectorySearch element.", parentNode.LocalName)))
                {
                    // if the FileSearch/@Id is there, use it to generate the id
                    XmlAttribute dirSearchId = element.OwnerDocument.CreateAttribute("Id");
                    if (null != element.Attributes["Id"])
                    {
                        dirSearchId.Value = String.Concat(element.Attributes["Id"].Value, ".DirectorySearch");
                    }

                    // if the FileSearch/@Id was not found, use the parent id
                    if (String.IsNullOrEmpty(dirSearchId.Value) && null != parentNode.Attributes["Id"])
                    {
                        dirSearchId.Value = String.Concat(parentNode.Attributes["Id"].Value, ".DirectorySearch");
                    }

                    // if the parent @Id was not found, use an invalid id
                    if (String.IsNullOrEmpty(dirSearchId.Value))
                    {
                        dirSearchId.Value = "-PUT-ID-HERE-";
                    }

                    // generate a DirectorySearch element in between
                    XmlElement dirSearch = element.OwnerDocument.CreateElement("DirectorySearch", element.NamespaceURI);
                    dirSearch.Attributes.Append(dirSearchId);

                    XmlNode removed = parentNode.ReplaceChild(dirSearch, element);
                    dirSearch.AppendChild(removed);
                }
            }
        }

        /// <summary>
        /// Inspects a FragmentRef element.
        /// </summary>
        /// <param name="element">The FragmentRef element to inspect.</param>
        private void InspectFragmentRefElement(XmlElement element)
        {
            // error
            this.OnError(InspectorTestType.FragmentRefIllegal, element, "FragmentRef's are no longer supported. You must find a referencable element in the Fragment you are trying to reference and use the associated reference for that type instead. For example, if your Fragment has a Property in it, use a PropertyRef.");
            return;
        }

        /// <summary>
        /// Inspects an Icon element.
        /// </summary>
        /// <param name="element">The Icon element to inspect.</param>
        private void InspectIconElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Icon/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {

                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects an IgnoreModularization element.
        /// </summary>
        /// <param name="element">The IgnoreModularization element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlNode InspectIgnoreModularizationElement(XmlElement element)
        {
            string name = element.GetAttribute("Name");

            if (this.OnError(InspectorTestType.IgnoreModularizationDeprecated, element, "The IgnoreModularization element has been deprecated.  Use the CustomAction/@SuppressModularization or Property/@SuppressModularization attribute instead."))
            {
                if (0 < name.Length)
                {
                    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(element.OwnerDocument.NameTable);
                    namespaceManager.AddNamespace("wix", WixNamespaceURI);

                    XmlElement customAction = (XmlElement)element.OwnerDocument.SelectSingleNode(String.Format(CultureInfo.InvariantCulture, "//wix:CustomAction[@Id=\"{0}\"]", name), namespaceManager);
                    if (null != customAction)
                    {
                        customAction.SetAttribute("SuppressModularization", "yes");

                        XmlNode whitespace = element.NextSibling;
                        if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
                        {
                            whitespace.ParentNode.RemoveChild(whitespace);
                        }

                        element.ParentNode.RemoveChild(element);

                        return null;
                    }

                    XmlElement property = (XmlElement)element.OwnerDocument.SelectSingleNode(String.Format(CultureInfo.InvariantCulture, "//wix:Property[@Id=\"{0}\"]", name), namespaceManager);
                    if (null != property)
                    {
                        property.SetAttribute("SuppressModularization", "yes");

                        XmlNode whitespace = element.NextSibling;
                        if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
                        {
                            whitespace.ParentNode.RemoveChild(whitespace);
                        }

                        element.ParentNode.RemoveChild(element);

                        return null;
                    }
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects an Include element.
        /// </summary>
        /// <param name="element">The Include element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectIncludeElement(XmlElement element)
        {
            XmlAttribute xmlns = element.GetAttributeNode("xmlns");

            if (null == xmlns)
            {
                if (this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixNamespaceURI))
                {
                    return SetNamespaceURI(element, WixNamespaceURI);
                }
            }
            else if (WixNamespaceURI != xmlns.Value)
            {
                if (this.OnError(InspectorTestType.XmlnsValueWrong, xmlns, "The xmlns attribute's value is wrong.  It must be '{0}'.", WixNamespaceURI))
                {
                    return SetNamespaceURI(element, WixNamespaceURI);
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects an IniFile element.
        /// </summary>
        /// <param name="element">The IniFile element to inspect.</param>
        private void InspectIniFileElement(XmlElement element)
        {
            XmlAttribute longName = element.GetAttributeNode("LongName");
            XmlAttribute value = element.GetAttributeNode("Value");

            if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The IniFile/@LongName attribute has been deprecated.  Use the Name attribute instead."))
                {
                    XmlAttribute name = element.GetAttributeNode("Name");

                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("Name");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        XmlAttribute shortName = element.GetAttributeNode("ShortName");

                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("ShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The IniFile/@ShortName and LongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }

            if (null != value && String.IsNullOrEmpty(value.Value))
            {
                if (this.OnError(InspectorTestType.IniFileValueEmpty, element, "The IniFile/@Value attribute's value cannot be the empty string."))
                {
                    element.Attributes.Remove(value);
                }
            }
        }

        /// <summary>
        /// Inspects an IniFileSearch element.
        /// </summary>
        /// <param name="element">The IniFileSearch element to inspect.</param>
        private void InspectIniFileSearchElement(XmlElement element)
        {
            XmlAttribute longName = element.GetAttributeNode("LongName");

            if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The IniFileSearch/@LongName attribute has been deprecated.  Use the Name attribute instead."))
                {
                    XmlAttribute name = element.GetAttributeNode("Name");

                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("Name");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        XmlAttribute shortName = element.GetAttributeNode("ShortName");

                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("ShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The IniFileSearch/@ShortName and LongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }
        }

        /// <summary>
        /// Inspects a Media element.
        /// </summary>
        /// <param name="element">The Media element to inspect.</param>
        private void InspectMediaElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Media/@src attribute has been deprecated.  Use the Layout attribute instead."))
                {
                    XmlAttribute layout = element.OwnerDocument.CreateAttribute("Layout");
                    layout.Value = src.Value;
                    element.Attributes.InsertAfter(layout, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a Merge element.
        /// </summary>
        /// <param name="element">The Merge element to inspect.</param>
        private void InspectMergeElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Merge/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {

                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a Module element.
        /// </summary>
        /// <param name="element">The Module element to inspect.</param>
        private void InspectModuleElement(XmlElement element)
        {
            XmlAttribute guid = element.GetAttributeNode("Guid");

            if (null != guid)
            {
                if (this.OnError(InspectorTestType.ModuleGuidDeprecated, guid, "The Module/@Guid attribute is deprecated.  Use the Package/@Id attribute instead."))
                {

                    XmlElement packageElement = element["Package"];
                    if (null != packageElement)
                    {
                        packageElement.SetAttribute("Id", guid.Value);
                        element.Attributes.Remove(guid);
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a Package element.
        /// </summary>
        /// <param name="element">The Package element to inspect.</param>
        private void InspectPackageElement(XmlElement element)
        {
            XmlAttribute compressed = element.GetAttributeNode("Compressed");
            XmlAttribute id = element.GetAttributeNode("Id");
            XmlAttribute platforms = element.GetAttributeNode("Platforms");
            XmlAttribute platform = element.GetAttributeNode("Platform");

            if ("Module" == element.ParentNode.LocalName)
            {
                if (null != compressed)
                {
                    if (this.OnError(InspectorTestType.PackageCompressedIllegal, compressed, "The Package/@Compressed attribute is illegal under a Module element because merge modules must always be compressed."))
                    {
                        element.Attributes.Remove(compressed);
                    }
                }
            }

            if (null != id && 0 <= id.Value.IndexOf("????????-????-????-????-????????????", StringComparison.Ordinal))
            {
                if (this.OnError(InspectorTestType.GuidWildcardDeprecated, id, "The guid value '{0}' is deprecated.  Remove the Package/@Id attribute to get the same functionality.", id.Value))
                {
                    element.Attributes.Remove(id);
                }
            }

            if (null != platforms)
            {
                if (this.OnError(InspectorTestType.PackagePlatformsDeprecated, platforms, "The Package/@Platforms attribute is deprecated. Use Package/@Platform instead.  Platform accepts only a single platform (x86, x64, or ia64). If the value in Package/@Platforms corresponds to one of those values, it will be updated."))
                {
                    string platformsValue = platforms.Value.ToLower();
                    if ("intel" == platformsValue || "x64" == platformsValue || "intel64" == platformsValue)
                    {
                        XmlAttribute newPlatform = element.OwnerDocument.CreateAttribute("Platform");
                        switch (platformsValue)
                        {
                            case "intel":
                                platformsValue = "x86";
                                break;
                            case "x64":
                                break;
                            case "intel64":
                                platformsValue = "ia64";
                                break;
                        }
                        newPlatform.Value = platformsValue;
                        element.Attributes.InsertAfter(newPlatform, platforms);
                        element.Attributes.Remove(platforms);
                    }
                }
            }

            if (null != platform && platform.Value == "intel")
            {
                if (this.OnError(InspectorTestType.PackagePlatformIntel, platform, "The Package/@Platform attribute value 'intel' is deprecated. Use 'x86' instead."))
                {
                    platform.Value = "x86";
                }
            }

            if (null != platform && platform.Value == "intel64")
            {
                if (this.OnError(InspectorTestType.PackagePlatformIntel64, platform, "The Package/@Platform attribute value 'intel64' is deprecated. Use 'ia64' instead."))
                {
                    platform.Value = "ia64";
                }
            }
        }

        /// <summary>
        /// Inspects a PatchSequence element.
        /// </summary>
        /// <param name="element">The PatchSequence element to inspect.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void InspectPatchSequenceElement(XmlElement element)
        {
            XmlAttribute supersede = element.GetAttributeNode("Supersede");
            XmlAttribute target = element.GetAttributeNode("Target");

            if (null != supersede)
            {
                if ("1" == supersede.Value)
                {
                    if (this.OnError(InspectorTestType.PatchSequenceSupersedeTypeChanged, supersede, "The PatchSequence/@Supersede attribute no longer accepts an integer value.  The value of '1' should now be 'yes' and all other values should be 'no' or simply drop the attribute."))
                    {
                        supersede.Value = "yes";
                    }
                }
                else if ("yes" != supersede.Value && "no" != supersede.Value)
                {
                    if (this.OnError(InspectorTestType.PatchSequenceSupersedeTypeChanged, supersede, "The PatchSequence/@Supersede attribute no longer accepts an integer value.  Any previous value except for '1' should be authored by dropping the attribute."))
                    {
                        element.Attributes.Remove(supersede);
                    }
                }
            }

            if (null != target)
            {
                try
                {
                    if (this.OnError(InspectorTestType.PatchSequenceTargetDeprecated, target, "The PatchSequence/@Target attribute has been deprecated.  Use the ProductCode attribute instead."))
                    {
                        XmlAttribute productCode = element.OwnerDocument.CreateAttribute("ProductCode");
                        productCode.Value = target.Value;
                        element.Attributes.InsertAfter(productCode, target);
                        element.Attributes.Remove(target);
                    }
                }
                catch // non-guid value
                {
                    if (this.OnError(InspectorTestType.PatchSequenceTargetDeprecated, target, "The PatchSequence/@Target attribute has been deprecated.  Use the TargetImage attribute instead."))
                    {
                        XmlAttribute targetImage = element.OwnerDocument.CreateAttribute("TargetImage");
                        targetImage.Value = target.Value;
                        element.Attributes.InsertAfter(targetImage, target);
                        element.Attributes.Remove(target);
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a Permission element.
        /// </summary>
        /// <param name="element">The Permission element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectPermissionElement(XmlElement element)
        {
            // if this is a FileSharePermissions element, then process it accordingly
            if (null != element.ParentNode && "FileShare" == element.ParentNode.LocalName)
            {
                element = this.InspectUtilElement(element);
            }

            // if this is a SecureObjects element, then process it accordingly
            if (null != element.Attributes["Extended"] || (null != element.ParentNode && "ServiceInstall" == element.ParentNode.LocalName))
            {
                // remove the Extended attribute since its no longer needed
                if (null != element.Attributes["Extended"])
                {
                    element.RemoveAttribute("Extended");
                }

                element = this.InspectUtilElement(element);
            }

            return element;
        }

        /// <summary>
        /// Inspects a Product element.
        /// </summary>
        /// <param name="element">The Product element to inspect.</param>
        private void InspectProductElement(XmlElement element)
        {
            XmlAttribute id = element.GetAttributeNode("Id");

            if (null != id && 0 <= id.Value.IndexOf("????????-????-????-????-????????????", StringComparison.Ordinal))
            {
                if (this.OnError(InspectorTestType.GuidWildcardDeprecated, id, "The guid value '{0}' is deprecated.  Set the value to '*' instead.", id.Value))
                {
                    id.Value = "*";
                }
            }
        }

        /// <summary>
        /// Inspects a ProgId element.
        /// </summary>
        /// <param name="element">The ProgId element to inspect.</param>
        private void InspectProgIdElement(XmlElement element)
        {
            XmlAttribute icon = element.GetAttributeNode("Icon");

            if (null != icon)
            {
                // remove quotes first if they are present
                if (icon.Value.StartsWith("\"[", StringComparison.Ordinal) && icon.Value.EndsWith("]\"", StringComparison.Ordinal))
                {
                    icon.Value = icon.Value.Substring(1, icon.Value.Length - 2);
                }

                if ((icon.Value.StartsWith("[!", StringComparison.Ordinal) || icon.Value.StartsWith("[#", StringComparison.Ordinal))
                    && icon.Value.EndsWith("]", StringComparison.Ordinal))
                {
                    if (this.OnError(InspectorTestType.ProgIdIconFormatted, element, "The ProgId/@Icon attribute's expected value has changed.  It no longer supports a formatted string for non-advertised ProgIds.  Instead, specify just a file identifier."))
                    {
                        icon.Value = icon.Value.Substring(2, icon.Value.Length - 3);
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a ProgressText element.
        /// </summary>
        /// <param name="element">The ProgressText element to inspect.</param>
        private void InspectProgressTextElement(XmlElement element)
        {
            XmlAttribute template = element.GetAttributeNode("Template");

            if (null != template && String.IsNullOrEmpty(template.Value))
            {
                if (this.OnError(InspectorTestType.ProgressTextTemplateEmpty, element, "The ProgressText/@Template attribute's value cannot be the empty string."))
                {
                    element.Attributes.Remove(template);
                }
            }
        }

        /// <summary>
        /// Inspects a Property element.
        /// </summary>
        /// <param name="element">The Property element to inspect.</param>
        private void InspectPropertyElement(XmlElement element)
        {
            XmlAttribute value = element.GetAttributeNode("Value");

            if (null != value && String.IsNullOrEmpty(value.Value))
            {
                if (this.OnError(InspectorTestType.PropertyValueEmpty, element, "The Property/@Value attribute's value cannot be the empty string."))
                {
                    element.Attributes.Remove(value);
                }
            }
        }

        /// <summary>
        /// Inspects a RadioButton element.
        /// </summary>
        /// <param name="element">The RadioButton element to inspect.</param>
        private void InspectRadioButtonElement(XmlElement element)
        {
            if (!element.HasAttribute("Value"))
            {
                if (this.OnError(InspectorTestType.RadioButtonMissingValue, element, "The required attribute RadioButton/@Value is missing.  Inner text has been depreciated in favor of the this attribute."))
                {
                    element.SetAttribute("Value", element.InnerText);
                    element.InnerText = null;
                }
            }
        }

        /// <summary>
        /// Inspects a RadioGroup element.
        /// </summary>
        /// <param name="element">The RadioGroup element to inspect.</param>
        private void InspectRadioGroupElement(XmlElement element)
        {
            XmlElement radioButtonGroup = element.OwnerDocument.CreateElement("RadioButtonGroup", element.NamespaceURI);

            if (this.OnError(InspectorTestType.RadioGroupDeprecated, element, "The RadioGroup element is deprecated.  Use RadioButtonGroup instead."))
            {
                element.ParentNode.InsertAfter(radioButtonGroup, element);
                element.ParentNode.RemoveChild(element);

                // move all the attributes from the old element to the new element
                while (element.Attributes.Count > 0)
                {
                    XmlAttribute attribute = element.Attributes[0];

                    element.Attributes.Remove(attribute);
                    radioButtonGroup.Attributes.Append(attribute);
                }

                // move all the attributes from the old element to the new element
                while (element.ChildNodes.Count > 0)
                {
                    XmlNode node = element.ChildNodes[0];

                    element.RemoveChild(node);
                    radioButtonGroup.AppendChild(node);
                }
            }
        }

        /// <summary>
        /// Inspects a Registry element.
        /// </summary>
        /// <param name="element">The Registry element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectRegistryElement(XmlElement element)
        {
            XmlAttribute action = element.GetAttributeNode("Action");
            XmlAttribute type = element.GetAttributeNode("Type");
            XmlAttribute value = element.GetAttributeNode("Value");
            XmlAttribute id = element.GetAttributeNode("Id");

            if (null != id)
            {
                if (this.OnError(InspectorTestType.ExplicitGeneratedAttribute, id, "The Registry/@Id value is specified but is almost never needed; WiX automatically generates stable Registry table IDs. Omit @Id for simplified authoring."))
                {
                    element.Attributes.Remove(id);
                }
            }

            if (this.OnError(InspectorTestType.RegistryElementDeprecated, element, "The Registry element has been deprecated.  Please use one of the new elements which replaces its functionality: RegistryKey for creating registry keys, RegistryValue for writing registry values, RemoveRegistryKey for removing registry keys, and RemoveRegistryValue for removing registry values."))
            {
                if (null != action && ("createKey" == action.Value || "createKeyAndRemoveKeyOnUninstall" == action.Value)) // RegistryKey
                {
                    XmlElement registryKey = element.OwnerDocument.CreateElement("RegistryKey", WixNamespaceURI);

                    ReplaceElement(element, registryKey);
                    if (null != action)
                    {
                        if ("createKey" == action.Value)
                        {
                            action.Value = "create";
                        }
                        else
                        {
                            action.Value = "createAndRemoveOnUninstall";
                        }
                    }

                    return registryKey;
                }
                else if (null != action && ("removeKeyOnInstall" == action.Value || "removeKeyOnUninstall" == action.Value)) // RemoveRegistryKey
                {
                    XmlElement removeRegistryKey = element.OwnerDocument.CreateElement("RemoveRegistryKey", WixNamespaceURI);

                    ReplaceElement(element, removeRegistryKey);
                    if ("removeKeyOnInstall" == action.Value)
                    {
                        action.Value = "removeOnInstall";
                    }
                    else
                    {
                        action.Value = "removeOnUninstall";
                    }

                    return removeRegistryKey;
                }
                else if (null != action && "remove" == action.Value) // RemoveRegistryValue
                {
                    XmlElement removeRegistryValue = element.OwnerDocument.CreateElement("RemoveRegistryValue", WixNamespaceURI);

                    ReplaceElement(element, removeRegistryValue);
                    removeRegistryValue.RemoveAttributeNode(action);

                    return removeRegistryValue;
                }
                else // RegistryValue
                {
                    // the Type attribute is now required to reflect that this element always sets a value
                    if (null == element.GetAttributeNode("Type"))
                    {
                        element.SetAttribute("Type", "string");
                    }

                    // replace RegistryValue child elements with the new MultiStringValue elements
                    ArrayList registryValueChildren = new ArrayList();
                    foreach (XmlNode childNode in element.ChildNodes)
                    {
                        if (XmlNodeType.Element == childNode.NodeType && "RegistryValue" == childNode.LocalName)
                        {
                            registryValueChildren.Add(childNode);
                        }
                    }

                    if (1 == registryValueChildren.Count && null != type && "string" == type.Value && null == value)
                    {
                        XmlElement registryValueChild = (XmlElement)registryValueChildren[0];

                        // previously, a single value could be specified in the RegistryValue child element
                        // now this should just be placed in the Value attribute to keep things simplier
                        if (XmlNodeType.Whitespace == registryValueChild.PreviousSibling.NodeType)
                        {
                            element.RemoveChild(registryValueChild.PreviousSibling);
                        }
                        element.RemoveChild(registryValueChild);
                        if (!element.IsEmpty && 0 == element.InnerXml.Trim().Length)
                        {
                            element.IsEmpty = true;
                        }

                        element.SetAttribute("Value", registryValueChild.InnerText);
                    }
                    else
                    {
                        foreach (XmlElement registryValueChild in registryValueChildren)
                        {
                            XmlElement multiStringValue = element.OwnerDocument.CreateElement("MultiStringValue", WixNamespaceURI);

                            ReplaceElement(registryValueChild, multiStringValue);
                        }
                    }

                    // the Value attribute (or MultiStringValue child elements) are now required
                    // to reflect that this element always sets a value
                    if (0 == registryValueChildren.Count && null == value)
                    {
                        element.SetAttribute("Value", String.Empty);
                    }

                    // more complex fixing behavior is needed if this contains Registry children
                    bool foundRegistryChild = false;
                    foreach (XmlNode childNode in element.ChildNodes)
                    {
                        if (XmlNodeType.Element == childNode.NodeType && "Registry" == childNode.LocalName)
                        {
                            foundRegistryChild = true;
                            break;
                        }
                    }

                    if (foundRegistryChild)
                    {
                        XmlElement registryKey = element.OwnerDocument.CreateElement("RegistryKey", WixNamespaceURI);
                        XmlElement registryValue = element.OwnerDocument.CreateElement("RegistryValue", WixNamespaceURI);

                        ReplaceElement(element, registryKey);

                        XmlNode whitespace = null;
                        if (XmlNodeType.Whitespace == registryKey.FirstChild.NodeType)
                        {
                            whitespace = registryKey.FirstChild;
                        }

                        // make a best-effort at retaining the whitespace
                        registryKey.InsertBefore(registryValue, registryKey.FirstChild);
                        if (null != whitespace)
                        {
                            registryKey.InsertBefore(whitespace.Clone(), registryValue);
                        }

                        // move all the attributes except for Root and Key to the RegistryValue element
                        ArrayList attributes = new ArrayList();
                        foreach (XmlAttribute attribute in registryKey.Attributes)
                        {
                            attributes.Add(attribute);
                        }

                        foreach (XmlAttribute attribute in attributes)
                        {
                            if ("Root" != attribute.Name && "Key" != attribute.Name)
                            {
                                registryKey.Attributes.Remove(attribute);
                                registryValue.Attributes.Append(attribute);
                            }
                        }

                        return registryKey;
                    }
                    else
                    {
                        XmlElement registryValue = element.OwnerDocument.CreateElement("RegistryValue", WixNamespaceURI);

                        ReplaceElement(element, registryValue);

                        return registryValue;
                    }
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects a RegistrySearch element.
        /// </summary>
        /// <param name="element">The RegistrySearch element to inspect.</param>
        private void InspectRegistrySearchElement(XmlElement element)
        {
            XmlAttribute type = element.GetAttributeNode("Type");

            if (null != type && "registry" == type.Value)
            {
                if (this.OnError(InspectorTestType.RegistrySearchTypeRegistryDeprecated, element, "The RegistrySearch/@Type attribute's value 'registry' has been deprecated.  Please use the value 'raw' instead."))
                {
                    element.SetAttribute("Type", "raw");
                }
            }
        }

        /// <summary>
        /// Inspects a RemoveFile element.
        /// </summary>
        /// <param name="element">The RemoveFile element to inspect.</param>
        private void InspectRemoveFileElement(XmlElement element)
        {
            XmlAttribute name = element.GetAttributeNode("Name");
            XmlAttribute longName = element.GetAttributeNode("LongName");

            if (null == name && null == longName)
            {
                if (this.OnError(InspectorTestType.RemoveFileNameRequired, element, "The RemoveFile/@Name attribute is required.  Without this attribute specified, this is better represented as a RemoveFolder element."))
                {

                    XmlElement removeFolder = element.OwnerDocument.CreateElement("RemoveFolder", WixNamespaceURI);
                    element.ParentNode.InsertAfter(removeFolder, element);
                    element.ParentNode.RemoveChild(element);

                    // move all the attributes from the old element to the new element
                    while (element.Attributes.Count > 0)
                    {
                        XmlAttribute attribute = element.Attributes[0];

                        element.Attributes.Remove(attribute);
                        removeFolder.Attributes.Append(attribute);
                    }

                    // move all the child nodes from the old element to the new element
                    while (element.ChildNodes.Count > 0)
                    {
                        XmlNode node = element.ChildNodes[0];

                        element.RemoveChild(node);
                        removeFolder.AppendChild(node);
                    }
                }
            }
            else if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The RemoveFile/@LongName attribute has been deprecated.  Use the Name attribute instead."))
                {

                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("Name");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        XmlAttribute shortName = element.GetAttributeNode("ShortName");

                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("ShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The RemoveFile/@ShortName and LongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }

                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }
        }

        /// <summary>
        /// Inspects a ServiceInstall element.
        /// </summary>
        /// <param name="element">The ServiceInstall element to inspect.</param>
        private void InspectServiceInstallElement(XmlElement element)
        {
            XmlAttribute localGroup = element.GetAttributeNode("LocalGroup");
            XmlAttribute password = element.GetAttributeNode("Password");

            if (null != localGroup && String.IsNullOrEmpty(localGroup.Value))
            {
                if (this.OnError(InspectorTestType.ServiceInstallLocalGroupEmpty, element, "The ServiceInstall/@LocalGroup attribute's value cannot be the empty string."))
                {
                    element.Attributes.Remove(localGroup);
                }
            }

            if (null != password && String.IsNullOrEmpty(password.Value))
            {
                if (this.OnError(InspectorTestType.ServiceInstallPasswordEmpty, element, "The ServiceInstall/@Password attribute's value cannot be the empty string."))
                {
                    element.Attributes.Remove(password);
                }
            }
        }

        /// <summary>
        /// Inspects a SFPCatalog element.
        /// </summary>
        /// <param name="element">The SFPCatalog element to inspect.</param>
        private void InspectSFPCatalogElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The SFPCatalog/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {
                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a Shortcut element.
        /// </summary>
        /// <param name="element">The Shortcut element to inspect.</param>
        private void InspectShortcutElement(XmlElement element)
        {
            XmlAttribute longName = element.GetAttributeNode("LongName");
            XmlAttribute workingDirectory = element.GetAttributeNode("WorkingDirectory");

            if (null != longName)
            {
                if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The Shortcut/@LongName attribute has been deprecated.  Use the Name attribute instead."))
                {
                    XmlAttribute name = element.GetAttributeNode("Name");

                    if (null == name)
                    {
                        name = element.OwnerDocument.CreateAttribute("Name");
                        name.Value = longName.Value;
                        element.Attributes.InsertAfter(name, longName);
                    }
                    else // move the value of Name to ShortName
                    {
                        XmlAttribute shortName = element.GetAttributeNode("ShortName");

                        if (null == shortName)
                        {
                            shortName = element.OwnerDocument.CreateAttribute("ShortName");
                            shortName.Value = name.Value;
                            element.Attributes.InsertBefore(shortName, name);
                        }
                        else // error; abort
                        {
                            if (this.OnError(InspectorTestType.LongNameDeprecated, longName, "The Shortcut/@ShortName and LongName attributes are both present.  These two attributes cannot coexist."))
                            {
                                return;
                            }
                        }
                        name.Value = longName.Value;
                    }

                    element.Attributes.Remove(longName);
                }
            }

            if (null != workingDirectory && String.IsNullOrEmpty(workingDirectory.Value))
            {
                if (this.OnError(InspectorTestType.ShortcutWorkingDirectoryEmpty, element, "The Shortcut/@WorkingDirectory attribute's value cannot be the empty string."))
                {
                    element.Attributes.Remove(workingDirectory);
                }
            }
        }

        /// <summary>
        /// Inspects a TargetImage element.
        /// </summary>
        /// <param name="element">The TargetImage element to inspect.</param>
        private void InspectTargetImageElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The TargetImage/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {
                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a Text element.
        /// </summary>
        /// <param name="element">The Text element to inspect.</param>
        private void InspectTextElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Text/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {
                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }
        }

        /// <summary>
        /// Inspects a TypeLib element.
        /// </summary>
        /// <param name="element">The TypeLib element to inspect.</param>
        private void InspectTypeLibElement(XmlElement element)
        {
            XmlAttribute description = element.GetAttributeNode("Description");

            if (null != description && String.IsNullOrEmpty(description.Value))
            {
                if (this.OnError(InspectorTestType.TypeLibDescriptionEmpty, element, "The TypeLib/@Description attribute's value cannot be an empty string.  If you want the value to be null or empty, simply drop the entire attribute."))
                {
                    element.Attributes.Remove(description);
                }
            }
        }

        /// <summary>
        /// Inspects an Upgrade element.
        /// </summary>
        /// <param name="element">The Upgrade element to inspect.</param>
        private void InspectUpgradeElement(XmlElement element)
        {
            ArrayList properties = new ArrayList();

            // find the Property child elements of the Upgrade element
            foreach (XmlNode childNode in element.ChildNodes)
            {
                if ("Property" == childNode.LocalName)
                {
                    properties.Add(childNode);
                }
            }

            // keep track of the whitespace preceeding the Upgrade element
            XmlNode whitespace = null;
            if (XmlNodeType.Whitespace == element.PreviousSibling.NodeType)
            {
                whitespace = element.PreviousSibling;
            }

            // move each of the Property elements out from underneath the Upgrade element
            foreach (XmlNode property in properties)
            {
                if (this.OnError(InspectorTestType.UpgradePropertyChild, property, "The Upgrade element contains a deprecated child Property child element.  The Property element should be moved to the parent of the Upgrade element."))
                {
                    if (XmlNodeType.Whitespace == property.PreviousSibling.NodeType)
                    {
                        element.RemoveChild(property.PreviousSibling);
                    }

                    element.RemoveChild(property);
                    element.ParentNode.InsertBefore(property, element);

                    // make a best-effort at handling the whitespace, this isn't guaranteed to work, so wixcop may need to be run more than once
                    if (null != whitespace)
                    {
                        element.ParentNode.InsertBefore(whitespace.Clone(), element);
                    }
                }
            }

            if (0 < properties.Count)
            {
                foreach (XmlNode childNode in element.ChildNodes)
                {
                    this.InspectWhitespace(childNode, 3);
                }
            }
        }

        /// <summary>
        /// Inspects an UpgradeImage element.
        /// </summary>
        /// <param name="element">The UpgradeImage element to inspect.</param>
        private void InspectUpgradeImageElement(XmlElement element)
        {
            XmlAttribute src = element.GetAttributeNode("src");
            XmlAttribute srcPatch = element.GetAttributeNode("srcPatch");

            if (null != src)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The UpgradeImage/@src attribute has been deprecated.  Use the SourceFile attribute instead."))
                {
                    XmlAttribute sourceFileAttribute = element.OwnerDocument.CreateAttribute("SourceFile");
                    sourceFileAttribute.Value = src.Value;
                    element.Attributes.InsertAfter(sourceFileAttribute, src);

                    element.Attributes.Remove(src);
                }
            }

            if (null != srcPatch)
            {
                if (this.OnError(InspectorTestType.SrcIsDeprecated, src, "The UpgradeImage/@srcPatch attribute has been deprecated.  Use the SourcePatch attribute instead."))
                {

                    XmlAttribute sourcePatch = element.OwnerDocument.CreateAttribute("SourcePatch");
                    sourcePatch.Value = srcPatch.Value;
                    element.Attributes.InsertAfter(sourcePatch, srcPatch);

                    element.Attributes.Remove(srcPatch);
                }
            }
        }

        /// <summary>
        /// Inspects an UpgradeVersion element.
        /// </summary>
        /// <param name="element">The UpgradeVersion element to inspect.</param>
        private void InspectUpgradeVersionElement(XmlElement element)
        {
            XmlAttribute property = element.GetAttributeNode("Property");
            XmlAttribute removeFeatures = element.GetAttributeNode("RemoveFeatures");

            if (null == property)
            {
                if (this.OnError(InspectorTestType.UpgradeVersionPropertyAttributeRequired, element, "The UpgradeVersion/@Property attribute must be specified."))
                {
                    if (null != element.ParentNode.Attributes["Id"])
                    {
                        string upgradeId = element.ParentNode.Attributes["Id"].Value;

                        upgradeId = upgradeId.ToUpper(CultureInfo.InvariantCulture);
                        upgradeId = upgradeId.Replace("{", String.Empty);
                        upgradeId = upgradeId.Replace("-", String.Empty);
                        upgradeId = upgradeId.Replace("}", String.Empty);

                        property = element.OwnerDocument.CreateAttribute("Property");
                        property.Value = String.Concat("UC", upgradeId);
                        element.Attributes.Append(property);
                    }
                }
            }

            if (null != removeFeatures && String.IsNullOrEmpty(removeFeatures.Value))
            {
                if (this.OnError(InspectorTestType.TypeLibDescriptionEmpty, element, "The UpgradeVersion/@RemoveFeatures attribute's value cannot be an empty string.  If you want the value to be null or empty, simply drop the entire attribute."))
                {
                    element.Attributes.Remove(removeFeatures);
                }
            }
        }

        /// <summary>
        /// Inspects a Verb element.
        /// </summary>
        /// <param name="element">The Verb element to inspect.</param>
        private void InspectVerbElement(XmlElement element)
        {
            XmlAttribute target = element.GetAttributeNode("Target");

            if (null != target)
            {
                if (this.OnError(InspectorTestType.VerbTargetDeprecated, element, "The Verb/@Target attribute has been deprecated.  Use the TargetFile or TargetProperty attribute instead."))
                {
                    // remove quotes first if they are present
                    if (target.Value.StartsWith("\"[", StringComparison.Ordinal) && target.Value.EndsWith("]\"", StringComparison.Ordinal))
                    {
                        target.Value = target.Value.Substring(1, target.Value.Length - 2);
                    }

                    if (target.Value.StartsWith("[", StringComparison.Ordinal) && target.Value.EndsWith("]", StringComparison.Ordinal))
                    {
                        if (target.Value.StartsWith("[!", StringComparison.Ordinal) || target.Value.StartsWith("[#", StringComparison.Ordinal))
                        {
                            XmlAttribute targetFile = element.OwnerDocument.CreateAttribute("TargetFile");
                            targetFile.Value = target.Value.Substring(2, target.Value.Length - 3);

                            element.Attributes.InsertAfter(targetFile, target);
                        }
                        else
                        {
                            XmlAttribute targetProperty = element.OwnerDocument.CreateAttribute("TargetProperty");
                            targetProperty.Value = target.Value.Substring(1, target.Value.Length - 2);

                            element.Attributes.InsertAfter(targetProperty, target);
                        }

                        element.Attributes.Remove(target);
                    }
                }
            }
        }

        /// <summary>
        /// Inspects a WebApplicationExtension element.
        /// </summary>
        /// <param name="element">The WebApplicationExtension element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectWebApplicationExtensionElement(XmlElement element)
        {
            XmlAttribute extension = element.GetAttributeNode("Extension");
            XmlAttribute id = element.GetAttributeNode("Id");

            if (null != id)
            {
                // an empty Id attribute value should be replaced with "*"
                if (String.IsNullOrEmpty(id.Value))
                {
                    if (this.OnError(InspectorTestType.WebApplicationExtensionIdEmpty, element, "The WebApplicationExtension/@Id attribute's value cannot be an empty string.  Use '*' for the value instead."))
                    {
                        id.Value = "*";
                    }
                }

                // Id has been deprecated, use Extension instead
                if (null == extension)
                {
                    if (this.OnError(InspectorTestType.WebApplicationExtensionIdDeprecated, element, "The WebApplicationExtension/@Id attribute has been deprecated.  Use the Extension attribute instead."))
                    {
                        extension = element.OwnerDocument.CreateAttribute("Extension");
                        extension.Value = id.Value;
                        element.Attributes.InsertAfter(extension, id);
                        element.Attributes.Remove(id);
                    }
                }
            }

            return this.InspectIIsElement(element);
        }

        /// <summary>
        /// Inspects a WebFilter element.
        /// </summary>
        /// <param name="element">The WebFilter element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectWebFilterElement(XmlElement element)
        {
            XmlAttribute loadOrder = element.GetAttributeNode("LoadOrder");

            if (null != loadOrder)
            {
                if ("-1" == loadOrder.Value)
                {
                    if (this.OnError(InspectorTestType.WebFilterLoadOrderIncorrect, loadOrder, "The WebFilter/@LoadOrder attribute's value, '-1', is better represented with the value 'last'."))
                    {
                        loadOrder.Value = "last";
                    }
                }
                else if ("0" == loadOrder.Value)
                {
                    if (this.OnError(InspectorTestType.WebFilterLoadOrderIncorrect, loadOrder, "The WebFilter/@LoadOrder attribute's value, '0', is better represented with the value 'first'."))
                    {
                        loadOrder.Value = "first";
                    }
                }
            }

            return this.InspectIIsElement(element);
        }

        /// <summary>
        /// Inspects a Wix element.
        /// </summary>
        /// <param name="element">The Wix element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectWixElement(XmlElement element)
        {
            XmlAttribute xmlns = element.GetAttributeNode("xmlns");

            if (null == xmlns)
            {
                if (this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixNamespaceURI))
                {
                    return SetNamespaceURI(element, WixNamespaceURI);
                }
            }
            else if (WixNamespaceURI != xmlns.Value)
            {
                if (this.OnError(InspectorTestType.XmlnsValueWrong, xmlns, "The xmlns attribute's value is wrong.  It must be '{0}'.", WixNamespaceURI))
                {
                    return SetNamespaceURI(element, WixNamespaceURI);
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects a WixLocalization element.
        /// </summary>
        /// <param name="element">The Wix element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectWixLocalizationElement(XmlElement element)
        {
            XmlAttribute xmlns = element.GetAttributeNode("xmlns");

            if (null == xmlns)
            {
                if (this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixLocalizationNamespaceURI))
                {
                    return SetNamespaceURI(element, WixLocalizationNamespaceURI);
                }
            }
            else if (WixLocalizationNamespaceURI != xmlns.Value)
            {
                if (this.OnError(InspectorTestType.XmlnsValueWrong, xmlns, "The xmlns attribute's value is wrong.  It must be '{0}'.", WixLocalizationNamespaceURI))
                {
                    return SetNamespaceURI(element, WixLocalizationNamespaceURI);
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects a Help element.
        /// </summary>
        /// <param name="element">The Help element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectHelpElement(XmlElement element)
        {
            string newLocalName = element.LocalName;
            XmlElement visualStudioExtension = element.OwnerDocument.CreateElement("vs", newLocalName, "http://schemas.microsoft.com/wix/VSExtension");

            if (this.OnError(InspectorTestType.NamespaceChanged, element, String.Format(CultureInfo.CurrentCulture, "The {0} element is now part of the VS extension.  An xmlns:vs=\"http://schemas.microsoft.com/wix/VSExtension\" attribute should be added to the Wix element and this element should be renamed to 'vs:{0}'.", element.LocalName)))
            {
                element.ParentNode.InsertAfter(visualStudioExtension, element);
                element.ParentNode.RemoveChild(element);

                // move all the attributes from the old element to the new element
                while (element.Attributes.Count > 0)
                {
                    XmlAttribute attribute = element.Attributes[0];

                    element.Attributes.Remove(attribute);
                    visualStudioExtension.Attributes.Append(attribute);
                }

                // move all the child nodes from the old element to the new element
                while (element.ChildNodes.Count > 0)
                {
                    XmlNode node = element.ChildNodes[0];

                    element.RemoveChild(node);
                    visualStudioExtension.AppendChild(node);
                }

                // put the vs xmlns attribute on the root element
                if (null == element.OwnerDocument.DocumentElement.Attributes["xmlns:vs"])
                {
                    element.OwnerDocument.DocumentElement.SetAttribute("xmlns:vs", "http://schemas.microsoft.com/wix/VSExtension");
                }

                // remove the help xmlns attribute on the root element
                if (null != element.OwnerDocument.DocumentElement.Attributes["xmlns:help"])
                {
                    element.OwnerDocument.DocumentElement.RemoveAttribute("xmlns:help");
                }
            }

            return visualStudioExtension;
        }

        /// <summary>
        /// Inspects an Internet Information Services (IIS) element.
        /// </summary>
        /// <param name="element">The IIS element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectIIsElement(XmlElement element)
        {
            if ("http://schemas.microsoft.com/wix/IIsExtension" != element.NamespaceURI)
            {
                string newLocalName = element.LocalName;

                if (this.OnError(InspectorTestType.NamespaceChanged, element, String.Format(CultureInfo.CurrentCulture, "The {0} element is now part of the IIS extension.  An xmlns:iis=\"http://schemas.microsoft.com/wix/IIsExtension\" attribute should be added to the Wix element and this element should be renamed to 'iis:{0}'.", element.LocalName)))
                {
                    XmlElement iisExt = element.OwnerDocument.CreateElement("iis", newLocalName, "http://schemas.microsoft.com/wix/IIsExtension");
                    element.ParentNode.InsertAfter(iisExt, element);
                    element.ParentNode.RemoveChild(element);

                    // move all the attributes from the old element to the new element
                    while (element.Attributes.Count > 0)
                    {
                        XmlAttribute attribute = element.Attributes[0];

                        element.Attributes.Remove(attribute);
                        iisExt.Attributes.Append(attribute);
                    }

                    // move all the child nodes from the old element to the new element
                    while (element.ChildNodes.Count > 0)
                    {
                        XmlNode node = element.ChildNodes[0];

                        element.RemoveChild(node);
                        iisExt.AppendChild(node);
                    }

                    // put the iis xmlns attribute on the root element
                    if (null == element.OwnerDocument.DocumentElement.Attributes["xmlns:iis"])
                    {
                        element.OwnerDocument.DocumentElement.SetAttribute("xmlns:iis", "http://schemas.microsoft.com/wix/IIsExtension");
                    }

                    // remove the sca xmlns attribute on the root element
                    if (null != element.OwnerDocument.DocumentElement.Attributes["xmlns:sca"])
                    {
                        element.OwnerDocument.DocumentElement.RemoveAttribute("xmlns:sca");
                    }

                    return iisExt;
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects SQL Server element.
        /// </summary>
        /// <param name="element">The SQL element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectSqlElement(XmlElement element)
        {
            if ("http://schemas.microsoft.com/wix/SqlExtension" != element.NamespaceURI)
            {
                string newLocalName = element.LocalName;

                if (this.OnError(InspectorTestType.NamespaceChanged, element, String.Format(CultureInfo.CurrentCulture, "The {0} element is now part of the SQL extension.  An xmlns:sql=\"http://schemas.microsoft.com/wix/SqlExtension\" attribute should be added to the Wix element and this element should be renamed to 'sql:{0}'.", element.LocalName)))
                {
                    XmlElement sqlExt = element.OwnerDocument.CreateElement("sql", newLocalName, "http://schemas.microsoft.com/wix/SqlExtension");
                    element.ParentNode.InsertAfter(sqlExt, element);
                    element.ParentNode.RemoveChild(element);

                    // move all the attributes from the old element to the new element
                    while (element.Attributes.Count > 0)
                    {
                        XmlAttribute attribute = element.Attributes[0];

                        element.Attributes.Remove(attribute);
                        sqlExt.Attributes.Append(attribute);
                    }

                    // move all the child nodes from the old element to the new element
                    while (element.ChildNodes.Count > 0)
                    {
                        XmlNode node = element.ChildNodes[0];

                        element.RemoveChild(node);
                        sqlExt.AppendChild(node);
                    }

                    // put the sql xmlns attribute on the root element
                    if (null == element.OwnerDocument.DocumentElement.Attributes["xmlns:sql"])
                    {
                        element.OwnerDocument.DocumentElement.SetAttribute("xmlns:sql", "http://schemas.microsoft.com/wix/SqlExtension");
                    }

                    // remove the sca xmlns attribute on the root element
                    if (null != element.OwnerDocument.DocumentElement.Attributes["xmlns:sca"])
                    {
                        element.OwnerDocument.DocumentElement.RemoveAttribute("xmlns:sca");
                    }

                    return sqlExt;
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects a Utility element.
        /// </summary>
        /// <param name="element">The Utility element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectUtilElement(XmlElement element)
        {
            if ("http://schemas.microsoft.com/wix/UtilExtension" != element.NamespaceURI)
            {
                string newLocalName = element.LocalName;

                if ("Permission" == element.LocalName)
                {
                    if ("FileShare" == element.ParentNode.LocalName)
                    {
                        newLocalName = "FileSharePermission";
                    }
                    else
                    {
                        newLocalName = "PermissionEx";
                    }
                }
                else if ("ServiceConfig" == element.LocalName && null == element.Attributes["FirstFailureActionType"])
                {
                    // All Util:ServiceConfig elements must have a FirstFailureActionType attribute but the built-in MSI 5.0
                    // Serviceconfig element does not. Just return the element if this is the MSI 5.0 ServiceConfig element.
                    return element;
                }

                if (this.OnError(InspectorTestType.NamespaceChanged, element, String.Format(CultureInfo.CurrentCulture, "The {0} element is now part of the Utility extension.  An xmlns:util=\"http://schemas.microsoft.com/wix/UtilExtension\" attribute should be added to the Wix element and this element should be renamed to 'util:{0}'.", element.LocalName)))
                {
                    XmlElement utilExt = element.OwnerDocument.CreateElement("util", newLocalName, "http://schemas.microsoft.com/wix/UtilExtension");
                    element.ParentNode.InsertAfter(utilExt, element);
                    element.ParentNode.RemoveChild(element);

                    // move all the attributes from the old element to the new element
                    while (element.Attributes.Count > 0)
                    {
                        XmlAttribute attribute = element.Attributes[0];

                        element.Attributes.Remove(attribute);
                        utilExt.Attributes.Append(attribute);
                    }

                    // move all the child nodes from the old element to the new element
                    while (element.ChildNodes.Count > 0)
                    {
                        XmlNode node = element.ChildNodes[0];

                        element.RemoveChild(node);
                        utilExt.AppendChild(node);
                    }

                    // put the util xmlns attribute on the root element
                    if (null == element.OwnerDocument.DocumentElement.Attributes["xmlns:util"])
                    {
                        element.OwnerDocument.DocumentElement.SetAttribute("xmlns:util", "http://schemas.microsoft.com/wix/UtilExtension");
                    }

                    // remove the sca xmlns attribute on the root element
                    if (null != element.OwnerDocument.DocumentElement.Attributes["xmlns:sca"])
                    {
                        element.OwnerDocument.DocumentElement.RemoveAttribute("xmlns:sca");
                    }

                    return utilExt;
                }
            }

            return element;
        }

        /// <summary>
        /// Output an error message to the console.
        /// </summary>
        /// <param name="inspectorTestType">The type of inspector test.</param>
        /// <param name="node">The node that caused the error.</param>
        /// <param name="message">Detailed error message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        /// <returns>Returns true indicating that action should be taken on this error, and false if it should be ignored.</returns>
        private bool OnError(InspectorTestType inspectorTestType, XmlNode node, string message, params object[] args)
        {
            if (this.ignoreErrors.Contains(inspectorTestType)) // ignore the error
            {
                return false;
            }

            // increase the error count
            this.errors++;

            // set the warning/error part of the message
            string warningError;
            if (this.errorsAsWarnings.Contains(inspectorTestType)) // error as warning
            {
                warningError = "warning";
            }
            else // normal error
            {
                warningError = "error";
            }

            if (null != node)
            {
                Console.WriteLine("{0}({1}) : {2} WXCP{3:0000} : {4} ({5})", this.sourceFile, ((IXmlLineInfo)node).LineNumber, warningError, (int)inspectorTestType, String.Format(CultureInfo.CurrentCulture, message, args), inspectorTestType.ToString());
            }
            else
            {
                string source = (null == this.sourceFile ? "wixcop.exe" : this.sourceFile);

                Console.WriteLine("{0} : {1} WXCP{2:0000} : {3} ({4})", source, warningError, (int)inspectorTestType, String.Format(CultureInfo.CurrentCulture, message, args), inspectorTestType.ToString());
            }

            return true;
        }

        /// <summary>
        /// Output a message to the console.
        /// </summary>
        /// <param name="node">The node that caused the message.</param>
        /// <param name="message">Detailed message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        private void OnVerbose(XmlNode node, string message, params string[] args)
        {
            this.errors++;

            if (null != node)
            {
                Console.WriteLine("{0}({1}) : {2}", this.sourceFile, ((IXmlLineInfo)node).LineNumber, String.Format(CultureInfo.CurrentCulture, message, args));
            }
            else
            {
                string source = (null == this.sourceFile ? "wixcop.exe" : this.sourceFile);

                Console.WriteLine("{0} : {1}", source, String.Format(CultureInfo.CurrentCulture, message, args));
            }
        }

        /// <summary>
        /// Return an identifier based on passed file/directory name
        /// </summary>
        /// <param name="name">File/directory name to generate identifer from</param>
        /// <returns>A version of the name that is a legal identifier.</returns>
        /// <remarks>This is very similar to WiX's Common class, except that in wixcop's case,
        ///          the identifier hasn't been run through the preprocessor, so we are more
        ///          permissive to account for that.</remarks>
        private static string GetIdentifierFromName(string name)
        {
            string result = IllegalIdentifierCharacters.Replace(name, "_"); // replace illegal characters with "_".

            // MSI identifiers must begin with an alphabetic character or an
            // underscore. Prefix all other values with an underscore.
            if (AddPrefix.IsMatch(name))
            {
                result = String.Concat("_", result);
            }

            return result;
        }
    }
}
