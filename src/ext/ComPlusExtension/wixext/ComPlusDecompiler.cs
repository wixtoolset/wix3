// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;

    using ComPlus = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.ComPlus;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset COM+ Extension.
    /// </summary>
    public sealed class ComPlusDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "ComPlusPartition":
                    this.DecompileComPlusPartitionTable(table);
                    break;
                case "ComPlusPartitionProperty":
                    this.DecompileComPlusPartitionPropertyTable(table);
                    break;
                case "ComPlusPartitionRole":
                    this.DecompileComPlusPartitionRoleTable(table);
                    break;
                case "ComPlusUserInPartitionRole":
                    this.DecompileComPlusUserInPartitionRoleTable(table);
                    break;
                case "ComPlusGroupInPartitionRole":
                    this.DecompileComPlusGroupInPartitionRoleTable(table);
                    break;
                case "ComPlusPartitionUser":
                    this.DecompileComPlusPartitionUserTable(table);
                    break;
                case "ComPlusApplication":
                    this.DecompileComPlusApplicationTable(table);
                    break;
                case "ComPlusApplicationProperty":
                    this.DecompileComPlusApplicationPropertyTable(table);
                    break;
                case "ComPlusApplicationRole":
                    this.DecompileComPlusApplicationRoleTable(table);
                    break;
                case "ComPlusApplicationRoleProperty":
                    this.DecompileComPlusApplicationRolePropertyTable(table);
                    break;
                case "ComPlusUserInApplicationRole":
                    this.DecompileComPlusUserInApplicationRoleTable(table);
                    break;
                case "ComPlusGroupInApplicationRole":
                    this.DecompileComPlusGroupInApplicationRoleTable(table);
                    break;
                case "ComPlusAssembly":
                    this.DecompileComPlusAssemblyTable(table);
                    break;
                case "ComPlusComponent":
                    this.DecompileComPlusComponentTable(table);
                    break;
                case "ComPlusComponentProperty":
                    this.DecompileComPlusComponentPropertyTable(table);
                    break;
                case "ComPlusRoleForComponent":
                    this.DecompileComPlusRoleForComponentTable(table);
                    break;
                case "ComPlusInterface":
                    this.DecompileComPlusInterfaceTable(table);
                    break;
                case "ComPlusInterfaceProperty":
                    this.DecompileComPlusInterfacePropertyTable(table);
                    break;
                case "ComPlusRoleForInterface":
                    this.DecompileComPlusRoleForInterfaceTable(table);
                    break;
                case "ComPlusMethod":
                    this.DecompileComPlusMethodTable(table);
                    break;
                case "ComPlusMethodProperty":
                    this.DecompileComPlusMethodPropertyTable(table);
                    break;
                case "ComPlusRoleForMethod":
                    this.DecompileComPlusRoleForMethodTable(table);
                    break;
                case "ComPlusSubscription":
                    this.DecompileComPlusSubscriptionTable(table);
                    break;
                case "ComPlusSubscriptionProperty":
                    this.DecompileComPlusSubscriptionPropertyTable(table);
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
        /// Decompile the ComPlusPartition table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusPartitionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusPartition partition = new ComPlus.ComPlusPartition();

                partition.Id = (string)row[0];

                if (null != row[2])
                {
                    partition.PartitionId = (string)row[2];
                }

                if (null != row[3])
                {
                    partition.Name = (string)row[3];
                }

                if (null != row[1])
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                    if (null != component)
                    {
                        component.AddChild(partition);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(partition);
                }
                this.Core.IndexElement(row, partition);
            }
        }

        /// <summary>
        /// Decompile the ComPlusPartitionProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusPartitionPropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusPartition partition = (ComPlus.ComPlusPartition)this.Core.GetIndexedElement("ComPlusPartition", (string)row[0]);
                if (null == partition)
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Partition_", (string)row[0], "ComPlusPartition"));
                }

                switch ((string)row[1])
                {
                    case "Changeable":
                        switch ((string)row[2])
                        {
                            case "1":
                                partition.Changeable = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                partition.Changeable = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Deleteable":
                        switch ((string)row[2])
                        {
                            case "1":
                                partition.Deleteable = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                partition.Deleteable = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Description":
                        partition.Description = (string)row[2];
                        break;
                    default:
                        // TODO: Warning
                        break;
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusPartitionRole table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusPartitionRoleTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusPartitionRole partitionRole = new ComPlus.ComPlusPartitionRole();

                partitionRole.Id = (string)row[0];
                partitionRole.Partition = (string)row[1];
                partitionRole.Name = (string)row[3];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(partitionRole);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusUserInPartitionRole table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusUserInPartitionRoleTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusUserInPartitionRole userInPartitionRole = new ComPlus.ComPlusUserInPartitionRole();

                userInPartitionRole.Id = (string)row[0];
                userInPartitionRole.PartitionRole = (string)row[1];
                userInPartitionRole.User = (string)row[3];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(userInPartitionRole);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusGroupInPartitionRole table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusGroupInPartitionRoleTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusGroupInPartitionRole groupInPartitionRole = new ComPlus.ComPlusGroupInPartitionRole();

                groupInPartitionRole.Id = (string)row[0];
                groupInPartitionRole.PartitionRole = (string)row[1];
                groupInPartitionRole.Group = (string)row[3];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(groupInPartitionRole);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusPartitionUser table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusPartitionUserTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusPartitionUser partitionUser = new ComPlus.ComPlusPartitionUser();

                partitionUser.Id = (string)row[0];
                partitionUser.Partition = (string)row[1];
                partitionUser.User = (string)row[3];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(partitionUser);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusApplication table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusApplicationTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusApplication application = new ComPlus.ComPlusApplication();

                application.Id = (string)row[0];
                application.Partition = (string)row[1];

                if (null != row[3])
                {
                    application.ApplicationId = (string)row[3];
                }

                if (null != row[4])
                {
                    application.Name = (string)row[4];
                }

                if (null != row[2])
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                    if (null != component)
                    {
                        component.AddChild(application);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(application);
                }
                this.Core.IndexElement(row, application);
            }
        }

        /// <summary>
        /// Decompile the ComPlusApplicationProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusApplicationPropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusApplication application = (ComPlus.ComPlusApplication)this.Core.GetIndexedElement("ComPlusApplication", (string)row[0]);
                if (null == application)
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Application_", (string)row[0], "ComPlusApplication"));
                }

                switch ((string)row[1])
                {
                    case "3GigSupportEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.ThreeGigSupportEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.ThreeGigSupportEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "AccessChecksLevel":
                        switch ((string)row[2])
                        {
                            case "0":
                                application.AccessChecksLevel = ComPlus.ComPlusApplication.AccessChecksLevelType.applicationLevel;
                                break;
                            case "1":
                                application.AccessChecksLevel = ComPlus.ComPlusApplication.AccessChecksLevelType.applicationComponentLevel;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Activation":
                        switch ((string)row[2])
                        {
                            case "Inproc":
                                application.Activation = ComPlus.ComPlusApplication.ActivationType.inproc;
                                break;
                            case "Local":
                                application.Activation = ComPlus.ComPlusApplication.ActivationType.local;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ApplicationAccessChecksEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.ApplicationAccessChecksEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.ApplicationAccessChecksEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ApplicationDirectory":
                        application.ApplicationDirectory = (string)row[2];
                        break;
                    case "Authentication":
                        switch ((string)row[2])
                        {
                            case "0":
                                application.Authentication = ComPlus.ComPlusApplication.AuthenticationType.@default;
                                break;
                            case "1":
                                application.Authentication = ComPlus.ComPlusApplication.AuthenticationType.none;
                                break;
                            case "2":
                                application.Authentication = ComPlus.ComPlusApplication.AuthenticationType.connect;
                                break;
                            case "3":
                                application.Authentication = ComPlus.ComPlusApplication.AuthenticationType.call;
                                break;
                            case "4":
                                application.Authentication = ComPlus.ComPlusApplication.AuthenticationType.packet;
                                break;
                            case "5":
                                application.Authentication = ComPlus.ComPlusApplication.AuthenticationType.integrity;
                                break;
                            case "6":
                                application.Authentication = ComPlus.ComPlusApplication.AuthenticationType.privacy;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "AuthenticationCapability":
                        switch ((string)row[2])
                        {
                            case "0":
                                application.AuthenticationCapability = ComPlus.ComPlusApplication.AuthenticationCapabilityType.none;
                                break;
                            case "2":
                                application.AuthenticationCapability = ComPlus.ComPlusApplication.AuthenticationCapabilityType.secureReference;
                                break;
                            case "32":
                                application.AuthenticationCapability = ComPlus.ComPlusApplication.AuthenticationCapabilityType.staticCloaking;
                                break;
                            case "64":
                                application.AuthenticationCapability = ComPlus.ComPlusApplication.AuthenticationCapabilityType.dynamicCloaking;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Changeable":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.Changeable = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.Changeable = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "CommandLine":
                        application.CommandLine = (string)row[2];
                        break;
                    case "ConcurrentApps":
                        int concurrentApps;
                        if (TryParseInt((string)row[2], out concurrentApps))
                        {
                            application.ConcurrentApps = concurrentApps;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "CreatedBy":
                        application.CreatedBy = (string)row[2];
                        break;
                    case "CRMEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.CRMEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.CRMEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "CRMLogFile":
                        application.CRMLogFile = (string)row[2];
                        break;
                    case "Deleteable":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.Deleteable = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.Deleteable = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Description":
                        application.Description = (string)row[2];
                        break;
                    case "DumpEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.DumpEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.DumpEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "DumpOnException":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.DumpOnException = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.DumpOnException = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "DumpOnFailfast":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.DumpOnFailfast = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.DumpOnFailfast = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "DumpPath":
                        application.DumpPath = (string)row[2];
                        break;
                    case "EventsEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.EventsEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.EventsEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Identity":
                        application.Identity = (string)row[2];
                        break;
                    case "ImpersonationLevel":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.ImpersonationLevel = ComPlus.ComPlusApplication.ImpersonationLevelType.anonymous;
                                break;
                            case "2":
                                application.ImpersonationLevel = ComPlus.ComPlusApplication.ImpersonationLevelType.identify;
                                break;
                            case "3":
                                application.ImpersonationLevel = ComPlus.ComPlusApplication.ImpersonationLevelType.impersonate;
                                break;
                            case "4":
                                application.ImpersonationLevel = ComPlus.ComPlusApplication.ImpersonationLevelType.@delegate;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "IsEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.IsEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.IsEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "MaxDumpCount":
                        int maxDumpCount;
                        if (TryParseInt((string)row[2], out maxDumpCount))
                        {
                            application.MaxDumpCount = maxDumpCount;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "Password":
                        application.Password = (string)row[2];
                        break;
                    case "QCAuthenticateMsgs":
                        switch ((string)row[2])
                        {
                            case "0":
                                application.QCAuthenticateMsgs = ComPlus.ComPlusApplication.QCAuthenticateMsgsType.secureApps;
                                break;
                            case "1":
                                application.QCAuthenticateMsgs = ComPlus.ComPlusApplication.QCAuthenticateMsgsType.off;
                                break;
                            case "2":
                                application.QCAuthenticateMsgs = ComPlus.ComPlusApplication.QCAuthenticateMsgsType.on;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "QCListenerMaxThreads":
                        int qcListenerMaxThreads;
                        if (TryParseInt((string)row[2], out qcListenerMaxThreads))
                        {
                            application.QCListenerMaxThreads = qcListenerMaxThreads;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "QueueListenerEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.QueueListenerEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.QueueListenerEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "QueuingEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.QueuingEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.QueuingEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "RecycleActivationLimit":
                        int recycleActivationLimit;
                        if (TryParseInt((string)row[2], out recycleActivationLimit))
                        {
                            application.RecycleActivationLimit = recycleActivationLimit;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "RecycleCallLimit":
                        int recycleCallLimit;
                        if (TryParseInt((string)row[2], out recycleCallLimit))
                        {
                            application.RecycleCallLimit = recycleCallLimit;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "RecycleExpirationTimeout":
                        int recycleExpirationTimeout;
                        if (TryParseInt((string)row[2], out recycleExpirationTimeout))
                        {
                            application.RecycleExpirationTimeout = recycleExpirationTimeout;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "RecycleLifetimeLimit":
                        int recycleLifetimeLimit;
                        if (TryParseInt((string)row[2], out recycleLifetimeLimit))
                        {
                            application.RecycleLifetimeLimit = recycleLifetimeLimit;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "RecycleMemoryLimit":
                        int recycleMemoryLimit;
                        if (TryParseInt((string)row[2], out recycleMemoryLimit))
                        {
                            application.RecycleMemoryLimit = recycleMemoryLimit;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "Replicable":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.Replicable = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.Replicable = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "RunForever":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.RunForever = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.RunForever = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ShutdownAfter":
                        int shutdownAfter;
                        if (TryParseInt((string)row[2], out shutdownAfter))
                        {
                            application.ShutdownAfter = shutdownAfter;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "SoapActivated":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.SoapActivated = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.SoapActivated = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "SoapBaseUrl":
                        application.SoapBaseUrl = (string)row[2];
                        break;
                    case "SoapMailTo":
                        application.SoapMailTo = (string)row[2];
                        break;
                    case "SoapVRoot":
                        application.SoapVRoot = (string)row[2];
                        break;
                    case "SRPEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                application.SRPEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                application.SRPEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "SRPTrustLevel":
                        switch ((string)row[2])
                        {
                            case "0":
                                application.SRPTrustLevel = ComPlus.ComPlusApplication.SRPTrustLevelType.disallowed;
                                break;
                            case "262144":
                                application.SRPTrustLevel = ComPlus.ComPlusApplication.SRPTrustLevelType.fullyTrusted;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    default:
                        // TODO: Warning
                        break;
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusApplicationRole table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusApplicationRoleTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusApplicationRole applicationRole = new ComPlus.ComPlusApplicationRole();

                applicationRole.Id = (string)row[0];
                applicationRole.Application = (string)row[1];

                if (null != row[3])
                {
                    applicationRole.Name = (string)row[3];
                }

                if (null != row[2])
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                    if (null != component)
                    {
                        component.AddChild(applicationRole);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(applicationRole);
                }
                this.Core.IndexElement(row, applicationRole);
            }
        }

        /// <summary>
        /// Decompile the ComPlusApplicationRoleProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusApplicationRolePropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusApplicationRole applicationRole = (ComPlus.ComPlusApplicationRole)this.Core.GetIndexedElement("ComPlusApplicationRole", (string)row[0]);
                if (null == applicationRole)
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ApplicationRole_", (string)row[0], "ComPlusApplicationRole"));
                }

                switch ((string)row[1])
                {
                    case "Description":
                        applicationRole.Description = (string)row[2];
                        break;
                    default:
                        // TODO: Warning
                        break;
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusUserInApplicationRole table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusUserInApplicationRoleTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusUserInApplicationRole userInApplicationRole = new ComPlus.ComPlusUserInApplicationRole();

                userInApplicationRole.Id = (string)row[0];
                userInApplicationRole.ApplicationRole = (string)row[1];
                userInApplicationRole.User = (string)row[3];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(userInApplicationRole);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusGroupInApplicationRole table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusGroupInApplicationRoleTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusGroupInApplicationRole groupInApplicationRole = new ComPlus.ComPlusGroupInApplicationRole();

                groupInApplicationRole.Id = (string)row[0];
                groupInApplicationRole.ApplicationRole = (string)row[1];
                groupInApplicationRole.Group = (string)row[3];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(groupInApplicationRole);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusAssembly table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusAssemblyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusAssembly assembly = new ComPlus.ComPlusAssembly();

                assembly.Id = (string)row[0];
                assembly.Application = (string)row[1];

                if (null != row[3])
                {
                    assembly.AssemblyName = (string)row[3];
                }

                if (null != row[4])
                {
                    assembly.DllPath = (string)row[4];
                }

                if (null != row[5])
                {
                    assembly.TlbPath = (string)row[5];
                }

                if (null != row[6])
                {
                    assembly.PSDllPath = (string)row[6];
                }

                int attributes = (int)row[7];

                if (0 != (attributes & (int)ComPlusCompiler.CpiAssemblyAttributes.EventClass))
                {
                    assembly.EventClass = ComPlus.YesNoType.yes;
                }

                if (0 != (attributes & (int)ComPlusCompiler.CpiAssemblyAttributes.DotNetAssembly))
                {
                    assembly.Type = ComPlus.ComPlusAssembly.TypeType.net;
                }

                if (0 != (attributes & (int)ComPlusCompiler.CpiAssemblyAttributes.DllPathFromGAC))
                {
                    assembly.DllPathFromGAC = ComPlus.YesNoType.yes;
                }

                if (0 != (attributes & (int)ComPlusCompiler.CpiAssemblyAttributes.RegisterInCommit))
                {
                    assembly.RegisterInCommit = ComPlus.YesNoType.yes;
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(assembly);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
                this.Core.IndexElement(row, assembly);
            }
        }

        /// <summary>
        /// Decompile the ComPlusAssemblyDependency table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusAssemblyDependencyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusAssemblyDependency assemblyDependency = new ComPlus.ComPlusAssemblyDependency();

                assemblyDependency.RequiredAssembly = (string)row[1];

                ComPlus.ComPlusAssembly assembly = (ComPlus.ComPlusAssembly)this.Core.GetIndexedElement("ComPlusAssembly", (string)row[0]);
                if (null != assembly)
                {
                    assembly.AddChild(assemblyDependency);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Assembly_", (string)row[0], "ComPlusAssembly"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusComponent table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusComponentTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusComponent comPlusComponent = new ComPlus.ComPlusComponent();

                comPlusComponent.Id = (string)row[0];

                try
                {
                    Guid clsid = new Guid((string)row[2]);
                    comPlusComponent.CLSID = clsid.ToString().ToUpper();
                }
                catch
                {
                    // TODO: Warning
                }

                ComPlus.ComPlusAssembly assembly = (ComPlus.ComPlusAssembly)this.Core.GetIndexedElement("ComPlusAssembly", (string)row[1]);
                if (null != assembly)
                {
                    assembly.AddChild(comPlusComponent);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Assembly_", (string)row[1], "ComPlusAssembly"));
                }
                this.Core.IndexElement(row, comPlusComponent);
            }
        }

        /// <summary>
        /// Decompile the ComPlusComponentProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusComponentPropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusComponent comPlusComponent = (ComPlus.ComPlusComponent)this.Core.GetIndexedElement("ComPlusComponent", (string)row[0]);
                if (null == comPlusComponent)
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ComPlusComponent_", (string)row[0], "ComPlusComponent"));
                }

                switch ((string)row[1])
                {
                    case "AllowInprocSubscribers":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.AllowInprocSubscribers = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.AllowInprocSubscribers = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ComponentAccessChecksEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.ComponentAccessChecksEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.ComponentAccessChecksEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ComponentTransactionTimeout":
                        int componentTransactionTimeout;
                        if (TryParseInt((string)row[2], out componentTransactionTimeout))
                        {
                            comPlusComponent.ComponentTransactionTimeout = componentTransactionTimeout;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "ComponentTransactionTimeoutEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.ComponentTransactionTimeoutEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.ComponentTransactionTimeoutEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "COMTIIntrinsics":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.COMTIIntrinsics = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.COMTIIntrinsics = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ConstructionEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.ConstructionEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.ConstructionEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ConstructorString":
                        comPlusComponent.ConstructorString = (string)row[2];
                        break;
                    case "CreationTimeout":
                        int creationTimeout;
                        if (TryParseInt((string)row[2], out creationTimeout))
                        {
                            comPlusComponent.CreationTimeout = creationTimeout;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "Description":
                        comPlusComponent.Description = (string)row[2];
                        break;
                    case "EventTrackingEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.EventTrackingEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.EventTrackingEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ExceptionClass":
                        comPlusComponent.ExceptionClass = (string)row[2];
                        break;
                    case "FireInParallel":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.FireInParallel = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.FireInParallel = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "IISIntrinsics":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.IISIntrinsics = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.IISIntrinsics = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "InitializesServerApplication":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.InitializesServerApplication = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.InitializesServerApplication = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "IsEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.IsEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.IsEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "IsPrivateComponent":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.IsPrivateComponent = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.IsPrivateComponent = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "JustInTimeActivation":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.JustInTimeActivation = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.JustInTimeActivation = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "LoadBalancingSupported":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.LoadBalancingSupported = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.LoadBalancingSupported = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "MaxPoolSize":
                        int maxPoolSize;
                        if (TryParseInt((string)row[2], out maxPoolSize))
                        {
                            comPlusComponent.MaxPoolSize = maxPoolSize;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "MinPoolSize":
                        int minPoolSize;
                        if (TryParseInt((string)row[2], out minPoolSize))
                        {
                            comPlusComponent.MinPoolSize = minPoolSize;
                        }
                        else
                        {
                            // TODO: Warning
                        }
                        break;
                    case "MultiInterfacePublisherFilterCLSID":
                        comPlusComponent.MultiInterfacePublisherFilterCLSID = (string)row[2];
                        break;
                    case "MustRunInClientContext":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.MustRunInClientContext = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.MustRunInClientContext = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "MustRunInDefaultContext":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.MustRunInDefaultContext = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.MustRunInDefaultContext = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "ObjectPoolingEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusComponent.ObjectPoolingEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusComponent.ObjectPoolingEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "PublisherID":
                        comPlusComponent.PublisherID = (string)row[2];
                        break;
                    case "SoapAssemblyName":
                        comPlusComponent.SoapAssemblyName = (string)row[2];
                        break;
                    case "SoapTypeName":
                        comPlusComponent.SoapTypeName = (string)row[2];
                        break;
                    case "Synchronization":
                        switch ((string)row[2])
                        {
                            case "0":
                                comPlusComponent.Synchronization = ComPlus.ComPlusComponent.SynchronizationType.ignored;
                                break;
                            case "1":
                                comPlusComponent.Synchronization = ComPlus.ComPlusComponent.SynchronizationType.none;
                                break;
                            case "2":
                                comPlusComponent.Synchronization = ComPlus.ComPlusComponent.SynchronizationType.supported;
                                break;
                            case "3":
                                comPlusComponent.Synchronization = ComPlus.ComPlusComponent.SynchronizationType.required;
                                break;
                            case "4":
                                comPlusComponent.Synchronization = ComPlus.ComPlusComponent.SynchronizationType.requiresNew;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Transaction":
                        switch ((string)row[2])
                        {
                            case "0":
                                comPlusComponent.Transaction = ComPlus.ComPlusComponent.TransactionType.ignored;
                                break;
                            case "1":
                                comPlusComponent.Transaction = ComPlus.ComPlusComponent.TransactionType.none;
                                break;
                            case "2":
                                comPlusComponent.Transaction = ComPlus.ComPlusComponent.TransactionType.supported;
                                break;
                            case "3":
                                comPlusComponent.Transaction = ComPlus.ComPlusComponent.TransactionType.required;
                                break;
                            case "4":
                                comPlusComponent.Transaction = ComPlus.ComPlusComponent.TransactionType.requiresNew;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "TxIsolationLevel":
                        switch ((string)row[2])
                        {
                            case "0":
                                comPlusComponent.TxIsolationLevel = ComPlus.ComPlusComponent.TxIsolationLevelType.any;
                                break;
                            case "1":
                                comPlusComponent.TxIsolationLevel = ComPlus.ComPlusComponent.TxIsolationLevelType.readUnCommitted;
                                break;
                            case "2":
                                comPlusComponent.TxIsolationLevel = ComPlus.ComPlusComponent.TxIsolationLevelType.readCommitted;
                                break;
                            case "3":
                                comPlusComponent.TxIsolationLevel = ComPlus.ComPlusComponent.TxIsolationLevelType.repeatableRead;
                                break;
                            case "4":
                                comPlusComponent.TxIsolationLevel = ComPlus.ComPlusComponent.TxIsolationLevelType.serializable;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    default:
                        // TODO: Warning
                        break;
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusRoleForComponent table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusRoleForComponentTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusRoleForComponent roleForComponent = new ComPlus.ComPlusRoleForComponent();

                roleForComponent.Id = (string)row[0];
                roleForComponent.Component = (string)row[1];
                roleForComponent.ApplicationRole = (string)row[2];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[3]);
                if (null != component)
                {
                    component.AddChild(roleForComponent);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[3], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusInterface table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusInterfaceTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusInterface comPlusInterface = new ComPlus.ComPlusInterface();

                comPlusInterface.Id = (string)row[0];

                try
                {
                    Guid iid = new Guid((string)row[2]);
                    comPlusInterface.IID = iid.ToString().ToUpper();
                }
                catch
                {
                    // TODO: Warning
                }

                ComPlus.ComPlusComponent comPlusComponent = (ComPlus.ComPlusComponent)this.Core.GetIndexedElement("ComPlusComponent", (string)row[1]);
                if (null != comPlusComponent)
                {
                    comPlusComponent.AddChild(comPlusInterface);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ComPlusComponent_", (string)row[1], "ComPlusComponent"));
                }
                this.Core.IndexElement(row, comPlusInterface);
            }
        }

        /// <summary>
        /// Decompile the ComPlusInterfaceProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusInterfacePropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusInterface comPlusInterface = (ComPlus.ComPlusInterface)this.Core.GetIndexedElement("ComPlusInterface", (string)row[0]);
                if (null == comPlusInterface)
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Interface_", (string)row[0], "ComPlusInterface"));
                }

                switch ((string)row[1])
                {
                    case "Description":
                        comPlusInterface.Description = (string)row[2];
                        break;
                    case "QueuingEnabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusInterface.QueuingEnabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusInterface.QueuingEnabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    default:
                        // TODO: Warning
                        break;
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusRoleForInterface table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusRoleForInterfaceTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusRoleForInterface roleForInterface = new ComPlus.ComPlusRoleForInterface();

                roleForInterface.Id = (string)row[0];
                roleForInterface.Interface = (string)row[1];
                roleForInterface.ApplicationRole = (string)row[2];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[3]);
                if (null != component)
                {
                    component.AddChild(roleForInterface);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[3], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusMethod table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusMethodTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusMethod comPlusMethod = new ComPlus.ComPlusMethod();

                comPlusMethod.Id = (string)row[0];

                if (null != row[2])
                {
                    comPlusMethod.Index = (int)row[2];
                }

                if (null != row[3])
                {
                    comPlusMethod.Name = (string)row[3];
                }

                ComPlus.ComPlusInterface comPlusInterface = (ComPlus.ComPlusInterface)this.Core.GetIndexedElement("ComPlusInterface", (string)row[1]);
                if (null != comPlusInterface)
                {
                    comPlusInterface.AddChild(comPlusMethod);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Interface_", (string)row[1], "ComPlusInterface"));
                }
                this.Core.IndexElement(row, comPlusMethod);
            }
        }

        /// <summary>
        /// Decompile the ComPlusMethodProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusMethodPropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusMethod comPlusMethod = (ComPlus.ComPlusMethod)this.Core.GetIndexedElement("ComPlusMethod", (string)row[0]);
                if (null == comPlusMethod)
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Method_", (string)row[0], "ComPlusMethod"));
                }

                switch ((string)row[1])
                {
                    case "AutoComplete":
                        switch ((string)row[2])
                        {
                            case "1":
                                comPlusMethod.AutoComplete = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                comPlusMethod.AutoComplete = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Description":
                        comPlusMethod.Description = (string)row[2];
                        break;
                    default:
                        // TODO: Warning
                        break;
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusRoleForMethod table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusRoleForMethodTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusRoleForMethod roleForMethod = new ComPlus.ComPlusRoleForMethod();

                roleForMethod.Id = (string)row[0];
                roleForMethod.Method = (string)row[1];
                roleForMethod.ApplicationRole = (string)row[2];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[3]);
                if (null != component)
                {
                    component.AddChild(roleForMethod);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[3], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ComPlusSubscription table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusSubscriptionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusSubscription subscription = new ComPlus.ComPlusSubscription();

                subscription.Id = (string)row[0];
                subscription.Component = (string)row[1];
                subscription.SubscriptionId = (string)row[3];
                subscription.Name = (string)row[4];
                subscription.EventCLSID = (string)row[5];
                subscription.PublisherID = (string)row[6];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(subscription);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
                this.Core.IndexElement(row, subscription);
            }
        }

        /// <summary>
        /// Decompile the ComPlusSubscriptionProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComPlusSubscriptionPropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                ComPlus.ComPlusSubscription subscription = (ComPlus.ComPlusSubscription)this.Core.GetIndexedElement("ComPlusSubscription", (string)row[0]);
                if (null == subscription)
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Subscription_", (string)row[0], "ComPlusSubscription"));
                }

                switch ((string)row[1])
                {
                    case "Description":
                        subscription.Description = (string)row[2];
                        break;
                    case "Enabled":
                        switch ((string)row[2])
                        {
                            case "1":
                                subscription.Enabled = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                subscription.Enabled = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "EventClassPartitionID":
                        subscription.EventClassPartitionID = (string)row[2];
                        break;
                    case "FilterCriteria":
                        subscription.FilterCriteria = (string)row[2];
                        break;
                    case "InterfaceID":
                        subscription.InterfaceID = (string)row[2];
                        break;
                    case "MachineName":
                        subscription.MachineName = (string)row[2];
                        break;
                    case "MethodName":
                        subscription.MethodName = (string)row[2];
                        break;
                    case "PerUser":
                        switch ((string)row[2])
                        {
                            case "1":
                                subscription.PerUser = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                subscription.PerUser = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "Queued":
                        switch ((string)row[2])
                        {
                            case "1":
                                subscription.Queued = ComPlus.YesNoType.yes;
                                break;
                            case "0":
                                subscription.Queued = ComPlus.YesNoType.no;
                                break;
                            default:
                                // TODO: Warning
                                break;
                        }
                        break;
                    case "SubscriberMoniker":
                        subscription.SubscriberMoniker = (string)row[2];
                        break;
                    case "UserName":
                        subscription.UserName = (string)row[2];
                        break;
                    default:
                        // TODO: Warning
                        break;
                }
            }
        }

        static bool TryParseInt(string s, out int result)
        {
            try
            {
                result = int.Parse(s);
                return true;
            }
            catch (FormatException)
            {
                result = 0;
                return false;
            }
            catch (OverflowException)
            {
                result = 0;
                return false;
            }
        }
    }
}
