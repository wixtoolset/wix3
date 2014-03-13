//-------------------------------------------------------------------------------------------------
// <copyright file="MsmqDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the Windows Installer XML Toolset MSMQ Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;

    using Msmq = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Msmq;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset MSMQ Extension.
    /// </summary>
    public sealed class MsmqDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "MessageQueue":
                    this.DecompileMessageQueueTable(table);
                    break;
                case "MessageQueueUserPermission":
                    this.DecompileMessageQueueUserPermissionTable(table);
                    break;
                case "MessageQueueGroupPermission":
                    this.DecompileMessageQueueGroupPermissionTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void FinalizeDecompile(TableCollection tables)
        {
        }

        /// <summary>
        /// Decompile the MessageQueue table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMessageQueueTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Msmq.MessageQueue queue = new Msmq.MessageQueue();

                queue.Id = (string)row[0];

                if (null != row[2])
                {
                    queue.BasePriority = (int)row[2];
                }

                if (null != row[3])
                {
                    queue.JournalQuota = (int)row[3];
                }

                queue.Label = (string)row[4];

                if (null != row[5])
                {
                    queue.MulticastAddress = (string)row[5];
                }

                queue.PathName = (string)row[6];

                if (null != row[7])
                {
                    switch ((MsmqCompiler.MqiMessageQueuePrivacyLevel)row[7])
                    {
                        case MsmqCompiler.MqiMessageQueuePrivacyLevel.None:
                            queue.PrivLevel = Msmq.MessageQueue.PrivLevelType.none;
                            break;
                        case MsmqCompiler.MqiMessageQueuePrivacyLevel.Optional:
                            queue.PrivLevel = Msmq.MessageQueue.PrivLevelType.optional;
                            break;
                        case MsmqCompiler.MqiMessageQueuePrivacyLevel.Body:
                            queue.PrivLevel = Msmq.MessageQueue.PrivLevelType.body;
                            break;
                        default:
                            break;
                    }
                }

                if (null != row[8])
                {
                    queue.Quota = (int)row[8];
                }

                if (null != row[9])
                {
                    queue.ServiceTypeGuid = (string)row[9];
                }

                int attributes = (int)row[10];

                if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueueAttributes.Authenticate))
                {
                    queue.Authenticate = Msmq.YesNoType.yes;
                }

                if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueueAttributes.Journal))
                {
                    queue.Journal = Msmq.YesNoType.yes;
                }

                if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueueAttributes.Transactional))
                {
                    queue.Transactional = Msmq.YesNoType.yes;
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(queue);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the MessageQueueUserPermission table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMessageQueueUserPermissionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Msmq.MessageQueuePermission queuePermission = new Msmq.MessageQueuePermission();

                queuePermission.Id = (string)row[0];

                if (null != row[2])
                {
                    queuePermission.MessageQueue = (string)row[2];
                }

                queuePermission.User = (string)row[3];

                DecompileMessageQueuePermissionAttributes(row, queuePermission);

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(queuePermission);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the MessageQueueGroupPermission table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMessageQueueGroupPermissionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Msmq.MessageQueuePermission queuePermission = new Msmq.MessageQueuePermission();

                queuePermission.Id = (string)row[0];

                if (null != row[2])
                {
                    queuePermission.MessageQueue = (string)row[2];
                }

                queuePermission.Group = (string)row[3];

                DecompileMessageQueuePermissionAttributes(row, queuePermission);

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(queuePermission);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile row attributes for the MessageQueueUserPermission and MessageQueueGroupPermission tables.
        /// </summary>
        /// <param name="row">The row to decompile.</param>
        /// <param name="table">Target element.</param>
        private void DecompileMessageQueuePermissionAttributes(Row row, Msmq.MessageQueuePermission queuePermission)
        {
            int attributes = (int)row[4];

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.DeleteMessage))
            {
                queuePermission.DeleteMessage = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.PeekMessage))
            {
                queuePermission.PeekMessage = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.WriteMessage))
            {
                queuePermission.WriteMessage = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.DeleteJournalMessage))
            {
                queuePermission.DeleteJournalMessage = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.SetQueueProperties))
            {
                queuePermission.SetQueueProperties = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.GetQueueProperties))
            {
                queuePermission.GetQueueProperties = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.DeleteQueue))
            {
                queuePermission.DeleteQueue = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.GetQueuePermissions))
            {
                queuePermission.GetQueuePermissions = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.ChangeQueuePermissions))
            {
                queuePermission.ChangeQueuePermissions = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.TakeQueueOwnership))
            {
                queuePermission.TakeQueueOwnership = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.ReceiveMessage))
            {
                queuePermission.ReceiveMessage = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.ReceiveJournalMessage))
            {
                queuePermission.ReceiveJournalMessage = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericRead))
            {
                queuePermission.QueueGenericRead = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericWrite))
            {
                queuePermission.QueueGenericWrite = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericExecute))
            {
                queuePermission.QueueGenericExecute = Msmq.YesNoType.yes;
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericAll))
            {
                queuePermission.QueueGenericAll = Msmq.YesNoType.yes;
            }
        }
    }
}
