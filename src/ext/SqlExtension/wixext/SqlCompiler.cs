// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset SQL Server Extension.
    /// </summary>
    public sealed class SqlCompiler : CompilerExtension
    {
        // sql database attributes definitions (from sca.h)
        internal const int DbCreateOnInstall = 0x00000001;
        internal const int DbDropOnUninstall = 0x00000002;
        internal const int DbContinueOnError = 0x00000004;
        internal const int DbDropOnInstall = 0x00000008;
        internal const int DbCreateOnUninstall = 0x00000010;
        internal const int DbConfirmOverwrite = 0x00000020;
        internal const int DbCreateOnReinstall = 0x00000040;
        internal const int DbDropOnReinstall = 0x00000080;

        // sql string/script attributes definitions (from sca.h)
        internal const int SqlExecuteOnInstall = 0x00000001;
        internal const int SqlExecuteOnUninstall = 0x00000002;
        internal const int SqlContinueOnError = 0x00000004;
        internal const int SqlRollback = 0x00000008;
        internal const int SqlExecuteOnReinstall = 0x00000010;

        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new SqlCompiler.
        /// </summary>
        public SqlCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.sql.xsd");
        }

        /// <summary>
        /// Gets the schema for this extension.
        /// </summary>
        /// <value>Schema for this extension.</value>
        public override XmlSchema Schema
        {
            get { return this.schema; }
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element, params string[] contextValues)
        {
            switch (parentElement.LocalName)
            {
                case "Component":
                    string componentId = contextValues[0];
                    string directoryId = contextValues[1];

                    switch (element.LocalName)
                    {
                        case "SqlDatabase":
                            this.ParseSqlDatabaseElement(element, componentId);
                            break;
                        case "SqlScript":
                            this.ParseSqlScriptElement(element, componentId, null);
                            break;
                        case "SqlString":
                            this.ParseSqlStringElement(element, componentId, null);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.LocalName)
                    {
                        case "SqlDatabase":
                            this.ParseSqlDatabaseElement(element, null);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a sql database element
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseSqlDatabaseElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int attributes = 0;
            string database = null;
            string fileSpec = null;
            string instance = null;
            string logFileSpec = null;
            string server = null;
            string user = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ConfirmOverwrite":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbConfirmOverwrite;
                            }
                            break;
                        case "ContinueOnError":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbContinueOnError;
                            }
                            break;
                        case "CreateOnInstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbCreateOnInstall;
                            }
                            break;
                        case "CreateOnReinstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbCreateOnReinstall;
                            }
                            break;
                        case "CreateOnUninstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbCreateOnUninstall;
                            }
                            break;
                        case "Database":
                            database = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DropOnInstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbDropOnInstall;
                            }
                            break;
                        case "DropOnReinstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbDropOnReinstall;
                            }
                            break;

                        case "DropOnUninstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbDropOnUninstall;
                            }
                            break;
                        case "Instance":
                            instance = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Server":
                            server = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "User":
                            user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!CompilerCore.ContainsProperty(user))
                            {
                                user = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
                }
            }

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
            }

            if (null == database)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Database"));
            }
            else if (128 < database.Length)
            {
                this.Core.OnMessage(WixErrors.IdentifierTooLongError(sourceLineNumbers, node.Name, "Database", database, 128));
            }

            if (null == server)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Server"));
            }

            if (0 == attributes && null != componentId)
            {
                this.Core.OnMessage(SqlErrors.OneOfAttributesRequiredUnderComponent(sourceLineNumbers, node.Name, "CreateOnInstall", "CreateOnUninstall", "DropOnInstall", "DropOnUninstall"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                        switch (child.LocalName)
                        {
                            case "SqlScript":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseSqlScriptElement(child, componentId, id);
                                break;
                            case "SqlString":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseSqlStringElement(child, componentId, id);
                                break;
                            case "SqlFileSpec":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }
                                else if (null != fileSpec)
                                {
                                    this.Core.OnMessage(WixErrors.TooManyElements(sourceLineNumbers, node.Name, child.Name, 1));
                                }

                                fileSpec = this.ParseSqlFileSpecElement(child);
                                break;
                            case "SqlLogFileSpec":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }
                                else if (null != logFileSpec)
                                {
                                    this.Core.OnMessage(WixErrors.TooManyElements(sourceLineNumbers, node.Name, child.Name, 1));
                                }

                                logFileSpec = this.ParseSqlFileSpecElement(child);
                                break;
                            default:
                                this.Core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.Core.UnsupportedExtensionElement(node, child);
                    }
                }
            }

            if (null != componentId)
            {
                // Reference InstallSqlData and UninstallSqlData since nothing will happen without it
                this.Core.CreateCustomActionReference(sourceLineNumbers, "InstallSqlData", Platforms.ARM64);
                this.Core.CreateCustomActionReference(sourceLineNumbers, "UninstallSqlData", Platforms.ARM64);
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "SqlDatabase");
                row[0] = id;
                row[1] = server;
                row[2] = instance;
                row[3] = database;
                row[4] = componentId;
                row[5] = user;
                row[6] = fileSpec;
                row[7] = logFileSpec;
                if (0 != attributes)
                {
                    row[8] = attributes;
                }
            }
        }

        /// <summary>
        /// Parses a sql file specification element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Identifier of sql file specification.</returns>
        private string ParseSqlFileSpecElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string fileName = null;
            string growthSize = null;
            string maxSize = null;
            string name = null;
            string size = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Filename":
                            fileName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Size":
                            size = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MaxSize":
                            maxSize = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "GrowthSize":
                            growthSize = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
                }
            }

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }

            if (null == fileName)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Filename"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }
                    else
                    {
                        this.Core.UnsupportedExtensionElement(node, child);
                    }
                }
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "SqlFileSpec");
                row[0] = id;
                row[1] = name;
                row[2] = fileName;
                if (null != size)
                {
                    row[3] = size;
                }

                if (null != maxSize)
                {
                    row[4] = maxSize;
                }

                if (null != growthSize)
                {
                    row[5] = growthSize;
                }
            }

            return id;
        }

        /// <summary>
        /// Parses a sql script element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="sqlDb">Optional database to execute script against.</param>
        private void ParseSqlScriptElement(XmlNode node, string componentId, string sqlDb)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int attributes = 0;
            bool rollbackAttribute = false;
            bool nonRollbackAttribute = false;
            string binary = null;
            int sequence = CompilerCore.IntegerNotSet;
            string user = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                switch (attrib.LocalName)
                {
                    case "Id":
                        id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "BinaryKey":
                        binary = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Binary", binary);
                        break;
                    case "Sequence":
                        sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                        break;
                    case "SqlDb":
                        if (null != sqlDb)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        sqlDb = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "SqlDatabase", sqlDb);
                        break;
                    case "User":
                        user = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                        break;

                    // Flag-setting attributes
                    case "ContinueOnError":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlContinueOnError;
                        }
                        break;
                    case "ExecuteOnInstall":
                        if (rollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                        }
                        nonRollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnInstall;
                        }
                        break;
                    case "ExecuteOnReinstall":
                        if (rollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                        }
                        nonRollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnReinstall;
                        }
                        break;
                    case "ExecuteOnUninstall":
                        if (rollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                        }
                        nonRollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnUninstall;
                        }
                        break;
                    case "RollbackOnInstall":
                        if (nonRollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                        }
                        rollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnInstall;
                            attributes |= SqlRollback;
                        }
                        break;
                    case "RollbackOnReinstall":
                        if (nonRollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                        }
                        rollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnReinstall;
                            attributes |= SqlRollback;
                        }
                        break;
                    case "RollbackOnUninstall":
                        if (nonRollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                        }
                        rollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnUninstall;
                            attributes |= SqlRollback;
                        }
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
                    }
                else
                {
                    this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
                }
            }

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
            }

            if (null == binary)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "BinaryKey"));
            }

            if (null == sqlDb)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SqlDb"));
            }

            if (0 == attributes)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall", "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }
                    else
                    {
                        switch (child.LocalName)
                        {
                            case "Binary":
                                this.Core.OnMessage(SqlErrors.DeprecatedBinaryChildElement(sourceLineNumbers, node.Name));
                                break;
                            default:
                                this.Core.UnsupportedExtensionElement(node, child);
                                break;
                        }
                    }
                }
            }

            // Reference InstallSqlData and UninstallSqlData since nothing will happen without it
            this.Core.CreateCustomActionReference(sourceLineNumbers, "InstallSqlData", Platforms.ARM64);
            this.Core.CreateCustomActionReference(sourceLineNumbers, "UninstallSqlData", Platforms.ARM64);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "SqlScript");
                row[0] = id;
                row[1] = sqlDb;
                row[2] = componentId;
                row[3] = binary;
                row[4] = user;
                row[5] = attributes;
                if (CompilerCore.IntegerNotSet != sequence)
                {
                    row[6] = sequence;
                }
            }
        }

        /// <summary>
        /// Parses a sql string element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="sqlDb">Optional database to execute string against.</param>
        private void ParseSqlStringElement(XmlNode node, string componentId, string sqlDb)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int attributes = 0;
            bool rollbackAttribute = false;
            bool nonRollbackAttribute = false;
            int sequence = CompilerCore.IntegerNotSet;
            string sql = null;
            string user = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                switch (attrib.LocalName)
                {
                    case "Id":
                        id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "ContinueOnError":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlContinueOnError;
                        }
                        break;
                    case "ExecuteOnInstall":
                        if (rollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                        }
                        nonRollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnInstall;
                        }
                        break;
                    case "ExecuteOnReinstall":
                        if (rollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                        }
                        nonRollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnReinstall;
                        }
                        break;
                    case "ExecuteOnUninstall":
                        if (rollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                        }
                        nonRollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnUninstall;
                        }
                        break;
                    case "RollbackOnInstall":
                        if (nonRollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                        }
                        rollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnInstall;
                            attributes |= SqlRollback;
                        }
                        break;
                    case "RollbackOnReinstall":
                        if (nonRollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                        }
                        rollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnReinstall;
                            attributes |= SqlRollback;
                        }
                        break;
                    case "RollbackOnUninstall":
                        if (nonRollbackAttribute)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                        }
                        rollbackAttribute = true;

                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= SqlExecuteOnUninstall;
                            attributes |= SqlRollback;
                        }
                        break;
                    case "Sequence":
                        sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                        break;
                    case "SQL":
                        sql = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SqlDb":
                        if (null != sqlDb)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, "SqlDb", "SqlDatabase"));
                        }

                        sqlDb = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "SqlDatabase", sqlDb);
                        break;
                    case "User":
                        user = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }
            else
            {
                this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
            }
            }

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
            }

            if (null == sql)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SQL"));
            }

            if (null == sqlDb)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SqlDb"));
            }

            if (0 == attributes)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall", "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }
                    else
                    {
                        this.Core.UnsupportedExtensionElement(node, child);
                    }
                }
            }

            // Reference InstallSqlData and UninstallSqlData since nothing will happen without it
            this.Core.CreateCustomActionReference(sourceLineNumbers, "InstallSqlData", Platforms.ARM64);
            this.Core.CreateCustomActionReference(sourceLineNumbers, "UninstallSqlData", Platforms.ARM64);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "SqlString");
                row[0] = id;
                row[1] = sqlDb;
                row[2] = componentId;
                row[3] = sql;
                row[4] = user;
                row[5] = attributes;
                if (CompilerCore.IntegerNotSet != sequence)
                {
                    row[6] = sequence;
                }
            }
        }
    }
}
