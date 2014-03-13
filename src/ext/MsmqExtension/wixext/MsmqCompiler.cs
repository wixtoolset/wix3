//-------------------------------------------------------------------------------------------------
// <copyright file="MsmqCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The compiler for the Windows Installer XML Toolset MSMQ Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class MsmqCompiler : CompilerExtension
    {
        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new MsmqCompiler.
        /// </summary>
        public MsmqCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.msmq.xsd");
        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum MqiMessageQueueAttributes
        {
            Authenticate  = (1 << 0),
            Journal       = (1 << 1),
            Transactional = (1 << 2)
        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum MqiMessageQueuePrivacyLevel
        {
            None     = 0,
            Optional = 1,
            Body     = 2
        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum MqiMessageQueuePermission
        {
            DeleteMessage          = (1 << 0),
            PeekMessage            = (1 << 1),
            WriteMessage           = (1 << 2),
            DeleteJournalMessage   = (1 << 3),
            SetQueueProperties     = (1 << 4),
            GetQueueProperties     = (1 << 5),
            DeleteQueue            = (1 << 6),
            GetQueuePermissions    = (1 << 7),
            ChangeQueuePermissions = (1 << 8),
            TakeQueueOwnership     = (1 << 9),
            ReceiveMessage         = (1 << 10),
            ReceiveJournalMessage  = (1 << 11),
            QueueGenericRead       = (1 << 12),
            QueueGenericWrite      = (1 << 13),
            QueueGenericExecute    = (1 << 14),
            QueueGenericAll        = (1 << 15)
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
                        case "MessageQueue":
                            this.ParseMessageQueueElement(element, componentId);
                            break;
                        case "MessageQueuePermission":
                            this.ParseMessageQueuePermissionElement(element, componentId, null);
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

        ///	<summary>
        ///	Parses an MSMQ message queue element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        private void ParseMessageQueueElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string id = null;
            int basePriority = CompilerCore.IntegerNotSet;
            int journalQuota = CompilerCore.IntegerNotSet;
            string label = null;
            string multicastAddress = null;
            string pathName = null;
            int privLevel = CompilerCore.IntegerNotSet;
            int quota = CompilerCore.IntegerNotSet;
            string serviceTypeGuid = null;
            int attributes = 0;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Authenticate":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= (int)MqiMessageQueueAttributes.Authenticate;
                        }
                        else
                        {
                            attributes &= ~(int)MqiMessageQueueAttributes.Authenticate;
                        }
                        break;
                    case "BasePriority":
                        basePriority = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                        break;
                    case "Journal":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= (int)MqiMessageQueueAttributes.Journal;
                        }
                        else
                        {
                            attributes &= ~(int)MqiMessageQueueAttributes.Journal;
                        }
                        break;
                    case "JournalQuota":
                        journalQuota = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                        break;
                    case "Label":
                        label = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MulticastAddress":
                        multicastAddress = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "PathName":
                        pathName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "PrivLevel":
                        string privLevelAttr = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (privLevelAttr)
                        {
                            case "none":
                                privLevel = (int)MqiMessageQueuePrivacyLevel.None;
                                break;
                            case "optional":
                                privLevel = (int)MqiMessageQueuePrivacyLevel.Optional;
                                break;
                            case "body":
                                privLevel = (int)MqiMessageQueuePrivacyLevel.Body;
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "MessageQueue", "PrivLevel", privLevelAttr, "none", "body", "optional"));
                                break;
                        }
                        break;
                    case "Quota":
                        quota = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                        break;
                    case "Transactional":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= (int)MqiMessageQueueAttributes.Transactional;
                        }
                        else
                        {
                            attributes &= ~(int)MqiMessageQueueAttributes.Transactional;
                        }
                        break;
                    case "ServiceTypeGuid":
                        serviceTypeGuid = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "MessageQueuePermission":
                            this.ParseMessageQueuePermissionElement(child, componentId, id);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "MessageQueue");
            row[0] = id;
            row[1] = componentId;
            if (CompilerCore.IntegerNotSet != basePriority)
            {
                row[2] = basePriority;
            }
            if (CompilerCore.IntegerNotSet != journalQuota)
            {
                row[3] = journalQuota;
            }
            row[4] = label;
            row[5] = multicastAddress;
            row[6] = pathName;
            if (CompilerCore.IntegerNotSet != privLevel)
            {
                row[7] = privLevel;
            }
            if (CompilerCore.IntegerNotSet != quota)
            {
                row[8] = quota;
            }
            row[9] = serviceTypeGuid;
            row[10] = attributes;

            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "MessageQueuingInstall");
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "MessageQueuingUninstall");
        }

        ///	<summary>
        ///	Parses an MSMQ message queue permission element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent message queue.</param>
        private void ParseMessageQueuePermissionElement(XmlNode node, string componentId, string messageQueueId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string id = null;
            string user = null;
            string group = null;
            int permissions = 0;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "MessageQueue":
                        if (null != messageQueueId)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        messageQueueId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "MessageQueue", messageQueueId);
                        break;
                    case "User":
                        if (null != group)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "User", "Group"));
                        }
                        user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                        break;
                    case "Group":
                        if (null != user)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Group", "User"));
                        }
                        group = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Group", group);
                        break;
                    case "DeleteMessage":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.DeleteMessage;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.DeleteMessage;
                        }
                        break;
                    case "PeekMessage":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.PeekMessage;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.PeekMessage;
                        }
                        break;
                    case "WriteMessage":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.WriteMessage;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.WriteMessage;
                        }
                        break;
                    case "DeleteJournalMessage":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.DeleteJournalMessage;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.DeleteJournalMessage;
                        }
                        break;
                    case "SetQueueProperties":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.SetQueueProperties;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.SetQueueProperties;
                        }
                        break;
                    case "GetQueueProperties":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.GetQueueProperties;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.GetQueueProperties;
                        }
                        break;
                    case "DeleteQueue":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.DeleteQueue;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.DeleteQueue;
                        }
                        break;
                    case "GetQueuePermissions":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.GetQueuePermissions;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.GetQueuePermissions;
                        }
                        break;
                    case "ChangeQueuePermissions":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.ChangeQueuePermissions;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.ChangeQueuePermissions;
                        }
                        break;
                    case "TakeQueueOwnership":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.TakeQueueOwnership;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.TakeQueueOwnership;
                        }
                        break;
                    case "ReceiveMessage":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.ReceiveMessage;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.ReceiveMessage;
                        }
                        break;
                    case "ReceiveJournalMessage":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.ReceiveJournalMessage;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.ReceiveJournalMessage;
                        }
                        break;
                    case "QueueGenericRead":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.QueueGenericRead;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.QueueGenericRead;
                        }
                        break;
                    case "QueueGenericWrite":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.QueueGenericWrite;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.QueueGenericWrite;
                        }
                        break;
                    case "QueueGenericExecute":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.QueueGenericExecute;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.QueueGenericExecute;
                        }
                        break;
                    case "QueueGenericAll":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            permissions |= (int)MqiMessageQueuePermission.QueueGenericAll;
                        }
                        else
                        {
                            permissions &= ~(int)MqiMessageQueuePermission.QueueGenericAll;
                        }
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == messageQueueId)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "MessageQueue"));
            }
            if (null == user && null == group)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "User", "Group"));
            }

            if (null != user)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "MessageQueueUserPermission");
                row[0] = id;
                row[1] = componentId;
                row[2] = messageQueueId;
                row[3] = user;
                row[4] = permissions;
            }
            if (null != group)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "MessageQueueGroupPermission");
                row[0] = id;
                row[1] = componentId;
                row[2] = messageQueueId;
                row[3] = group;
                row[4] = permissions;
            }
        }

        /// <summary>
        /// Attempts to parse the input value as a GUID, and in case the value is a valid
        /// GUID returnes it in the format "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}".
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        string TryFormatGuidValue(string val)
        {
            try
            {
                Guid guid = new Guid(val);
                return guid.ToString("B").ToUpper();
            }
            catch (FormatException)
            {
                return val;
            }
            catch (OverflowException)
            {
                return val;
            }
        }
    }
}
