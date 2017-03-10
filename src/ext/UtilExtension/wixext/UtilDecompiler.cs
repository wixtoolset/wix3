// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    using Util = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Util;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset Utility Extension.
    /// </summary>
    public sealed class UtilDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void InitializeDecompile(TableCollection tables)
        {
            this.CleanupSecureCustomProperties(tables);
            this.CleanupInternetShortcutRemoveFileTables(tables);
        }

        /// <summary>
        /// Decompile the SecureCustomProperties field to PropertyRefs for known extension properties.
        /// </summary>
        /// <remarks>
        /// If we've referenced any of the suite or directory properties, add
        /// a PropertyRef to refer to the Property (and associated custom action)
        /// from the extension's library. Then remove the property from
        /// SecureCustomExtensions property so later decompilation won't create
        /// new Property elements.
        /// </remarks>
        /// <param name="tables">The collection of all tables.</param>
        private void CleanupSecureCustomProperties(TableCollection tables)
        {
            Table propertyTable = tables["Property"];

            if (null != propertyTable)
            {
                foreach (Row row in propertyTable.Rows)
                {
                    if ("SecureCustomProperties" == row[0].ToString())
                    {
                        StringBuilder remainingProperties = new StringBuilder();
                        string[] secureCustomProperties = row[1].ToString().Split(';');
                        foreach (string property in secureCustomProperties)
                        {
                            if (property.StartsWith("WIX_SUITE_", StringComparison.Ordinal) || property.StartsWith("WIX_DIR_", StringComparison.Ordinal)
                                || property.StartsWith("WIX_ACCOUNT_", StringComparison.Ordinal))
                            {
                                Wix.PropertyRef propertyRef = new Wix.PropertyRef();
                                propertyRef.Id = property;
                                this.Core.RootElement.AddChild(propertyRef);
                            }
                            else
                            {
                                if (0 < remainingProperties.Length)
                                {
                                    remainingProperties.Append(";");
                                }
                                remainingProperties.Append(property);
                            }
                        }

                        row[1] = remainingProperties.ToString();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Remove RemoveFile rows that the InternetShortcut compiler extension adds for us.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void CleanupInternetShortcutRemoveFileTables(TableCollection tables)
        {
            // index the WixInternetShortcut table
            Table wixInternetShortcutTable = tables["WixInternetShortcut"];
            Hashtable wixInternetShortcuts = new Hashtable();
            if (null != wixInternetShortcutTable)
            {
                foreach (Row row in wixInternetShortcutTable.Rows)
                {
                    wixInternetShortcuts.Add(row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), row);
                }
            }

            // remove the RemoveFile rows with primary keys that match the WixInternetShortcut table's
            Table removeFileTable = tables["RemoveFile"];
            if (null != removeFileTable)
            {
                for (int i = removeFileTable.Rows.Count - 1; 0 <= i; i--)
                {
                    if (null != wixInternetShortcuts[removeFileTable.Rows[i][0]])
                    {
                        removeFileTable.Rows.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "WixCloseApplication":
                    this.DecompileWixCloseApplicationTable(table);
                    break;
                case "WixRemoveFolderEx":
                    this.DecompileWixRemoveFolderExTable(table);
                    break;
                case "WixRestartResource":
                    this.DecompileWixRestartResourceTable(table);
                    break;
                case "FileShare":
                    this.DecompileFileShareTable(table);
                    break;
                case "FileSharePermissions":
                    this.DecompileFileSharePermissionsTable(table);
                    break;
                case "WixInternetShortcut":
                    this.DecompileWixInternetShortcutTable(table);
                    break;
                case "Group":
                    this.DecompileGroupTable(table);
                    break;
                case "Perfmon":
                    this.DecompilePerfmonTable(table);
                    break;
                case "PerfmonManifest":
                    this.DecompilePerfmonManifestTable(table);
                    break;
                case "EventManifest":
                    this.DecompileEventManifestTable(table);
                    break;
                case "SecureObjects":
                    this.DecompileSecureObjectsTable(table);
                    break;
                case "ServiceConfig":
                    this.DecompileServiceConfigTable(table);
                    break;
                case "User":
                    this.DecompileUserTable(table);
                    break;
                case "UserGroup":
                    this.DecompileUserGroupTable(table);
                    break;
                case "XmlConfig":
                    this.DecompileXmlConfigTable(table);
                    break;
                case "XmlFile":
                    // XmlFile decompilation has been moved to FinalizeXmlFileTable function
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
            this.FinalizePerfmonTable(tables);
            this.FinalizePerfmonManifestTable(tables);
            this.FinalizeSecureObjectsTable(tables);
            this.FinalizeServiceConfigTable(tables);
            this.FinalizeXmlConfigTable(tables);
            this.FinalizeXmlFileTable(tables);
            this.FinalizeEventManifestTable(tables);
        }

        /// <summary>
        /// Decompile the WixCloseApplication table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixCloseApplicationTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.CloseApplication closeApplication = new Util.CloseApplication();

                closeApplication.Id = (string)row[0];

                closeApplication.Target = (string)row[1];

                if (null != row[2])
                {
                    closeApplication.Description = (string)row[2];
                }

                if (null != row[3])
                {
                    closeApplication.Content = (string)row[3];
                }

                // set defaults
                closeApplication.CloseMessage = Util.YesNoType.no;
                closeApplication.RebootPrompt = Util.YesNoType.yes;
                closeApplication.ElevatedCloseMessage = Util.YesNoType.no;

                if (null != row[4])
                {
                    int attribute = (int)row[4];

                    closeApplication.CloseMessage = (0x1 == (attribute & 0x1)) ? Util.YesNoType.yes : Util.YesNoType.no;
                    closeApplication.RebootPrompt = (0x2 == (attribute & 0x2)) ? Util.YesNoType.yes : Util.YesNoType.no;
                    closeApplication.ElevatedCloseMessage = (0x4 == (attribute & 0x4)) ? Util.YesNoType.yes : Util.YesNoType.no;
                }

                if (null != row[5])
                {
                    closeApplication.Sequence = (int)row[5];
                }

                if (null != row[6])
                {
                    closeApplication.Property = (string)row[6];
                }

                this.Core.RootElement.AddChild(closeApplication);
            }
        }

        /// <summary>
        /// Decompile the WixRemoveFolderEx table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixRemoveFolderExTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                // Set the Id even if auto-generated previously.
                Util.RemoveFolderEx removeFolder = new Util.RemoveFolderEx();
                removeFolder.Id = (string)row[0];
                removeFolder.Property = (string)row[2];

                int installMode = (int)row[3];
                switch ((UtilCompiler.WixRemoveFolderExOn)installMode)
                {
                    case UtilCompiler.WixRemoveFolderExOn.Install:
                        removeFolder.On = Util.RemoveFolderEx.OnType.install;
                        break;
                      
                    case UtilCompiler.WixRemoveFolderExOn.Uninstall:
                        removeFolder.On = Util.RemoveFolderEx.OnType.uninstall;
                        break;
                      
                    case UtilCompiler.WixRemoveFolderExOn.Both:
                        removeFolder.On = Util.RemoveFolderEx.OnType.both;
                        break;

                    default:
                        this.Core.OnMessage(WixWarnings.UnrepresentableColumnValue(row.SourceLineNumbers, table.Name, "InstallMode", installMode));
                        break;
                }

                // Add to the appropriate Component or section element.
                string componentId = (string)row[1];
                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", componentId);
                if (null != component)
                {
                    component.AddChild(removeFolder);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", componentId, "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the WixRestartResource table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixRestartResourceTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                // Set the Id even if auto-generated previously.
                Util.RestartResource restartResource = new Util.RestartResource();
                restartResource.Id = (string)row[0];

                // Determine the resource type and set accordingly.
                string resource = (string)row[2];
                int attributes = (int)row[3];
                UtilCompiler.WixRestartResourceAttributes type = (UtilCompiler.WixRestartResourceAttributes)(attributes & (int)UtilCompiler.WixRestartResourceAttributes.TypeMask);

                switch (type)
                {
                    case UtilCompiler.WixRestartResourceAttributes.Filename:
                        restartResource.Path = resource;
                        break;

                    case UtilCompiler.WixRestartResourceAttributes.ProcessName:
                        restartResource.ProcessName = resource;
                        break;

                    case UtilCompiler.WixRestartResourceAttributes.ServiceName:
                        restartResource.ServiceName = resource;
                        break;

                    default:
                        this.Core.OnMessage(WixWarnings.UnrepresentableColumnValue(row.SourceLineNumbers, table.Name, "Attributes", attributes));
                        break;
                }

                // Add to the appropriate Component or section element.
                string componentId = (string)row[1];
                if (!String.IsNullOrEmpty(componentId))
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", componentId);
                    if (null != component)
                    {
                        component.AddChild(restartResource);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", componentId, "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(restartResource);
                }
            }
        }

        /// <summary>
        /// Decompile the FileShare table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFileShareTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.FileShare fileShare = new Util.FileShare();

                fileShare.Id = (string)row[0];

                fileShare.Name = (string)row[1];

                if (null != row[3])
                {
                    fileShare.Description = (string)row[3];
                }

                // the Directory_ column is set by the parent Component

                // the User_ and Permissions columns are deprecated

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);
                if (null != component)
                {
                    component.AddChild(fileShare);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
                this.Core.IndexElement(row, fileShare);
            }
        }

        /// <summary>
        /// Decompile the FileSharePermissions table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFileSharePermissionsTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.FileSharePermission fileSharePermission = new Util.FileSharePermission();

                fileSharePermission.User = (string)row[1];

                string[] specialPermissions = UtilExtension.FolderPermissions;
                int permissions = (int)row[2];
                for (int i = 0; i < 32; i++)
                {
                    if (0 != ((permissions >> i) & 1))
                    {
                        string name = null;

                        if (16 > i && specialPermissions.Length > i)
                        {
                            name = specialPermissions[i];
                        }
                        else if (28 > i && UtilExtension.StandardPermissions.Length > (i - 16))
                        {
                            name = UtilExtension.StandardPermissions[i - 16];
                        }
                        else if (0 <= (i - 28) && UtilExtension.GenericPermissions.Length > (i - 28))
                        {
                            name = UtilExtension.GenericPermissions[i - 28];
                        }

                        if (null == name)
                        {
                            this.Core.OnMessage(WixWarnings.UnknownPermission(row.SourceLineNumbers, row.Table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), i));
                        }
                        else
                        {
                            switch (name)
                            {
                                case "ChangePermission":
                                    fileSharePermission.ChangePermission = Util.YesNoType.yes;
                                    break;
                                case "CreateChild":
                                    fileSharePermission.CreateChild = Util.YesNoType.yes;
                                    break;
                                case "CreateFile":
                                    fileSharePermission.CreateFile = Util.YesNoType.yes;
                                    break;
                                case "Delete":
                                    fileSharePermission.Delete = Util.YesNoType.yes;
                                    break;
                                case "DeleteChild":
                                    fileSharePermission.DeleteChild = Util.YesNoType.yes;
                                    break;
                                case "GenericAll":
                                    fileSharePermission.GenericAll = Util.YesNoType.yes;
                                    break;
                                case "GenericExecute":
                                    fileSharePermission.GenericExecute = Util.YesNoType.yes;
                                    break;
                                case "GenericRead":
                                    fileSharePermission.GenericRead = Util.YesNoType.yes;
                                    break;
                                case "GenericWrite":
                                    fileSharePermission.GenericWrite = Util.YesNoType.yes;
                                    break;
                                case "Read":
                                    fileSharePermission.Read = Util.YesNoType.yes;
                                    break;
                                case "ReadAttributes":
                                    fileSharePermission.ReadAttributes = Util.YesNoType.yes;
                                    break;
                                case "ReadExtendedAttributes":
                                    fileSharePermission.ReadExtendedAttributes = Util.YesNoType.yes;
                                    break;
                                case "ReadPermission":
                                    fileSharePermission.ReadPermission = Util.YesNoType.yes;
                                    break;
                                case "Synchronize":
                                    fileSharePermission.Synchronize = Util.YesNoType.yes;
                                    break;
                                case "TakeOwnership":
                                    fileSharePermission.TakeOwnership = Util.YesNoType.yes;
                                    break;
                                case "Traverse":
                                    fileSharePermission.Traverse = Util.YesNoType.yes;
                                    break;
                                case "WriteAttributes":
                                    fileSharePermission.WriteAttributes = Util.YesNoType.yes;
                                    break;
                                case "WriteExtendedAttributes":
                                    fileSharePermission.WriteExtendedAttributes = Util.YesNoType.yes;
                                    break;
                                default:
                                    Debug.Fail(String.Format("Unknown permission '{0}'.", name));
                                    break;
                            }
                        }
                    }
                }

                Util.FileShare fileShare = (Util.FileShare)this.Core.GetIndexedElement("FileShare", (string)row[0]);
                if (null != fileShare)
                {
                    fileShare.AddChild(fileSharePermission);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "FileShare_", (string)row[0], "FileShare"));
                }
            }
        }

        /// <summary>
        /// Decompile the Group table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileGroupTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.Group group = new Util.Group();

                group.Id = (string)row[0];

                if (null != row[1])
                {
                    this.Core.OnMessage(WixWarnings.UnrepresentableColumnValue(row.SourceLineNumbers, table.Name, "Component_", (string)row[1]));
                }

                group.Name = (string)row[2];

                if (null != row[3])
                {
                    group.Domain = (string)row[3];
                }

                this.Core.RootElement.AddChild(group);
            }
        }

        /// <summary>
        /// Decompile the WixInternetShortcut table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixInternetShortcutTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.InternetShortcut internetShortcut = new Util.InternetShortcut();
                internetShortcut.Id = (string)row[0];
                internetShortcut.Directory = (string)row[2];
                // remove .lnk/.url extension because compiler extension adds it back for us
                internetShortcut.Name = Path.ChangeExtension((string)row[3], null);
                internetShortcut.Target = (string)row[4];
                internetShortcut.IconFile = (string)row[6];
                internetShortcut.IconIndex = (int)row[7];

                UtilCompiler.InternetShortcutType shortcutType = (UtilCompiler.InternetShortcutType)row[5];
                switch (shortcutType)
                {
                    case UtilCompiler.InternetShortcutType.Link:
                        internetShortcut.Type = Util.InternetShortcut.TypeType.link;
                        break;
                    case UtilCompiler.InternetShortcutType.Url:
                        internetShortcut.Type = Util.InternetShortcut.TypeType.url;
                        break;
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(internetShortcut);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }

                this.Core.IndexElement(row, internetShortcut);
            }
        }

        /// <summary>
        /// Decompile the Perfmon table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePerfmonTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.PerfCounter perfCounter = new Util.PerfCounter();

                perfCounter.Name = (string)row[2];

                this.Core.IndexElement(row, perfCounter);
            }
        }
        
        /// <summary>
        /// Decompile the PerfmonManifest table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePerfmonManifestTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.PerfCounterManifest perfCounterManifest = new Util.PerfCounterManifest();

                perfCounterManifest.ResourceFileDirectory = (string)row[2];

                this.Core.IndexElement(row, perfCounterManifest);
            }
        }
        
        /// <summary>
        /// Decompile the EventManifest table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileEventManifestTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.EventManifest eventManifest = new Util.EventManifest();
                this.Core.IndexElement(row, eventManifest);
            }
        }
        
        /// <summary>
        /// Decompile the SecureObjects table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSecureObjectsTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.PermissionEx permissionEx = new Util.PermissionEx();

                string[] specialPermissions;
                switch ((string)row[1])
                {
                    case "CreateFolder":
                        specialPermissions = UtilExtension.FolderPermissions;
                        break;
                    case "File":
                        specialPermissions = UtilExtension.FilePermissions;
                        break;
                    case "Registry":
                        specialPermissions = UtilExtension.RegistryPermissions;
                        break;
                    case "ServiceInstall":
                        specialPermissions = UtilExtension.ServicePermissions;
                        break;
                    default:
                        this.Core.OnMessage(WixWarnings.IllegalColumnValue(row.SourceLineNumbers, row.Table.Name, row.Fields[1].Column.Name, row[1]));
                        return;
                }

                int permissionBits = (int)row[4];
                for (int i = 0; i < 32; i++)
                {
                    if (0 != ((permissionBits >> i) & 1))
                    {
                        string name = null;

                        if (16 > i && specialPermissions.Length > i)
                        {
                            name = specialPermissions[i];
                        }
                        else if (28 > i && UtilExtension.StandardPermissions.Length > (i - 16))
                        {
                            name = UtilExtension.StandardPermissions[i - 16];
                        }
                        else if (0 <= (i - 28) && UtilExtension.GenericPermissions.Length > (i - 28))
                        {
                            name = UtilExtension.GenericPermissions[i - 28];
                        }

                        if (null == name)
                        {
                            this.Core.OnMessage(WixWarnings.UnknownPermission(row.SourceLineNumbers, row.Table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), i));
                        }
                        else
                        {
                            switch (name)
                            {
                                case "Append":
                                    permissionEx.Append = Util.YesNoType.yes;
                                    break;
                                case "ChangePermission":
                                    permissionEx.ChangePermission = Util.YesNoType.yes;
                                    break;
                                case "CreateChild":
                                    permissionEx.CreateChild = Util.YesNoType.yes;
                                    break;
                                case "CreateFile":
                                    permissionEx.CreateFile = Util.YesNoType.yes;
                                    break;
                                case "CreateLink":
                                    permissionEx.CreateLink = Util.YesNoType.yes;
                                    break;
                                case "CreateSubkeys":
                                    permissionEx.CreateSubkeys = Util.YesNoType.yes;
                                    break;
                                case "Delete":
                                    permissionEx.Delete = Util.YesNoType.yes;
                                    break;
                                case "DeleteChild":
                                    permissionEx.DeleteChild = Util.YesNoType.yes;
                                    break;
                                case "EnumerateSubkeys":
                                    permissionEx.EnumerateSubkeys = Util.YesNoType.yes;
                                    break;
                                case "Execute":
                                    permissionEx.Execute = Util.YesNoType.yes;
                                    break;
                                case "GenericAll":
                                    permissionEx.GenericAll = Util.YesNoType.yes;
                                    break;
                                case "GenericExecute":
                                    permissionEx.GenericExecute = Util.YesNoType.yes;
                                    break;
                                case "GenericRead":
                                    permissionEx.GenericRead = Util.YesNoType.yes;
                                    break;
                                case "GenericWrite":
                                    permissionEx.GenericWrite = Util.YesNoType.yes;
                                    break;
                                case "Notify":
                                    permissionEx.Notify = Util.YesNoType.yes;
                                    break;
                                case "Read":
                                    permissionEx.Read = Util.YesNoType.yes;
                                    break;
                                case "ReadAttributes":
                                    permissionEx.ReadAttributes = Util.YesNoType.yes;
                                    break;
                                case "ReadExtendedAttributes":
                                    permissionEx.ReadExtendedAttributes = Util.YesNoType.yes;
                                    break;
                                case "ReadPermission":
                                    permissionEx.ReadPermission = Util.YesNoType.yes;
                                    break;
                                case "ServiceChangeConfig":
                                    permissionEx.ServiceChangeConfig = Util.YesNoType.yes;
                                    break;
                                case "ServiceEnumerateDependents":
                                    permissionEx.ServiceEnumerateDependents = Util.YesNoType.yes;
                                    break;
                                case "ServiceInterrogate":
                                    permissionEx.ServiceInterrogate = Util.YesNoType.yes;
                                    break;
                                case "ServicePauseContinue":
                                    permissionEx.ServicePauseContinue = Util.YesNoType.yes;
                                    break;
                                case "ServiceQueryConfig":
                                    permissionEx.ServiceQueryConfig = Util.YesNoType.yes;
                                    break;
                                case "ServiceQueryStatus":
                                    permissionEx.ServiceQueryStatus = Util.YesNoType.yes;
                                    break;
                                case "ServiceStart":
                                    permissionEx.ServiceStart = Util.YesNoType.yes;
                                    break;
                                case "ServiceStop":
                                    permissionEx.ServiceStop = Util.YesNoType.yes;
                                    break;
                                case "ServiceUserDefinedControl":
                                    permissionEx.ServiceUserDefinedControl = Util.YesNoType.yes;
                                    break;
                                case "Synchronize":
                                    permissionEx.Synchronize = Util.YesNoType.yes;
                                    break;
                                case "TakeOwnership":
                                    permissionEx.TakeOwnership = Util.YesNoType.yes;
                                    break;
                                case "Traverse":
                                    permissionEx.Traverse = Util.YesNoType.yes;
                                    break;
                                case "Write":
                                    permissionEx.Write = Util.YesNoType.yes;
                                    break;
                                case "WriteAttributes":
                                    permissionEx.WriteAttributes = Util.YesNoType.yes;
                                    break;
                                case "WriteExtendedAttributes":
                                    permissionEx.WriteExtendedAttributes = Util.YesNoType.yes;
                                    break;
                                default:
                                    throw new InvalidOperationException(String.Format("Unknown permission attribute '{0}'.", name));
                            }
                        }
                    }
                }

                if (null != row[2])
                {
                    permissionEx.Domain = (string)row[2];
                }

                permissionEx.User = (string)row[3];

                this.Core.IndexElement(row, permissionEx);
            }
        }

        /// <summary>
        /// Decompile the ServiceConfig table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileServiceConfigTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.ServiceConfig serviceConfig = new Util.ServiceConfig();

                serviceConfig.ServiceName = (string)row[0];

                switch ((string)row[3])
                {
                    case "none":
                        serviceConfig.FirstFailureActionType = Util.ServiceConfig.FirstFailureActionTypeType.none;
                        break;
                    case "reboot":
                        serviceConfig.FirstFailureActionType = Util.ServiceConfig.FirstFailureActionTypeType.reboot;
                        break;
                    case "restart":
                        serviceConfig.FirstFailureActionType = Util.ServiceConfig.FirstFailureActionTypeType.restart;
                        break;
                    case "runCommand":
                        serviceConfig.FirstFailureActionType = Util.ServiceConfig.FirstFailureActionTypeType.runCommand;
                        break;
                    default:
                        this.Core.OnMessage(WixWarnings.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[3].Column.Name, row[3]));
                        break;
                }

                switch ((string)row[4])
                {
                    case "none":
                        serviceConfig.SecondFailureActionType = Util.ServiceConfig.SecondFailureActionTypeType.none;
                        break;
                    case "reboot":
                        serviceConfig.SecondFailureActionType = Util.ServiceConfig.SecondFailureActionTypeType.reboot;
                        break;
                    case "restart":
                        serviceConfig.SecondFailureActionType = Util.ServiceConfig.SecondFailureActionTypeType.restart;
                        break;
                    case "runCommand":
                        serviceConfig.SecondFailureActionType = Util.ServiceConfig.SecondFailureActionTypeType.runCommand;
                        break;
                    default:
                        this.Core.OnMessage(WixWarnings.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                        break;
                }

                switch ((string)row[5])
                {
                    case "none":
                        serviceConfig.ThirdFailureActionType = Util.ServiceConfig.ThirdFailureActionTypeType.none;
                        break;
                    case "reboot":
                        serviceConfig.ThirdFailureActionType = Util.ServiceConfig.ThirdFailureActionTypeType.reboot;
                        break;
                    case "restart":
                        serviceConfig.ThirdFailureActionType = Util.ServiceConfig.ThirdFailureActionTypeType.restart;
                        break;
                    case "runCommand":
                        serviceConfig.ThirdFailureActionType = Util.ServiceConfig.ThirdFailureActionTypeType.runCommand;
                        break;
                    default:
                        this.Core.OnMessage(WixWarnings.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[5].Column.Name, row[5]));
                        break;
                }

                if (null != row[6])
                {
                    serviceConfig.ResetPeriodInDays = (int)row[6];
                }

                if (null != row[7])
                {
                    serviceConfig.RestartServiceDelayInSeconds = (int)row[7];
                }

                if (null != row[8])
                {
                    serviceConfig.ProgramCommandLine = (string)row[8];
                }

                if (null != row[9])
                {
                    serviceConfig.RebootMessage = (string)row[9];
                }

                this.Core.IndexElement(row, serviceConfig);
            }
        }

        /// <summary>
        /// Decompile the User table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUserTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.User user = new Util.User();

                user.Id = (string)row[0];

                user.Name = (string)row[2];

                if (null != row[3])
                {
                    user.Domain = (string)row[3];
                }

                if (null != row[4])
                {
                    user.Password = (string)row[4];
                }

                if (null != row[5])
                {
                    int attributes = (int)row[5];

                    if (UtilCompiler.UserDontExpirePasswrd == (attributes & UtilCompiler.UserDontExpirePasswrd))
                    {
                        user.PasswordNeverExpires = Util.YesNoType.yes;
                    }

                    if (UtilCompiler.UserPasswdCantChange == (attributes & UtilCompiler.UserPasswdCantChange))
                    {
                        user.CanNotChangePassword = Util.YesNoType.yes;
                    }

                    if (UtilCompiler.UserPasswdChangeReqdOnLogin == (attributes & UtilCompiler.UserPasswdChangeReqdOnLogin))
                    {
                        user.PasswordExpired = Util.YesNoType.yes;
                    }

                    if (UtilCompiler.UserDisableAccount == (attributes & UtilCompiler.UserDisableAccount))
                    {
                        user.Disabled = Util.YesNoType.yes;
                    }

                    if (UtilCompiler.UserFailIfExists == (attributes & UtilCompiler.UserFailIfExists))
                    {
                        user.FailIfExists = Util.YesNoType.yes;
                    }

                    if (UtilCompiler.UserUpdateIfExists == (attributes & UtilCompiler.UserUpdateIfExists))
                    {
                        user.UpdateIfExists = Util.YesNoType.yes;
                    }

                    if (UtilCompiler.UserLogonAsService == (attributes & UtilCompiler.UserLogonAsService))
                    {
                        user.LogonAsService = Util.YesNoType.yes;
                    }
                    
                    if (UtilCompiler.UserDontRemoveOnUninstall == (attributes & UtilCompiler.UserDontRemoveOnUninstall))
                    {
                        user.RemoveOnUninstall = Util.YesNoType.no;
                    }

                    if (UtilCompiler.UserDontCreateUser == (attributes & UtilCompiler.UserDontCreateUser))
                    {
                        user.CreateUser = Util.YesNoType.no;
                    }

                    if (UtilCompiler.UserNonVital == (attributes & UtilCompiler.UserNonVital))
                    {
                        user.Vital = Util.YesNoType.no;
                    }
                }

                if (null != row[1])
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);

                    if (null != component)
                    {
                        component.AddChild(user);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(user);
                }
                this.Core.IndexElement(row, user);
            }
        }

        /// <summary>
        /// Decompile the UserGroup table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUserGroupTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.User user = (Util.User)this.Core.GetIndexedElement("User", (string)row[0]);

                if (null != user)
                {
                    Util.GroupRef groupRef = new Util.GroupRef();

                    groupRef.Id = (string)row[1];

                    user.AddChild(groupRef);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Group_", (string)row[0], "Group"));
                }
            }
        }

        /// <summary>
        /// Decompile the XmlConfig table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileXmlConfigTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Util.XmlConfig xmlConfig = new Util.XmlConfig();

                xmlConfig.Id = (string)row[0];

                xmlConfig.File = (string)row[1];

                xmlConfig.ElementPath = (string)row[2];

                if (null != row[3])
                {
                    xmlConfig.VerifyPath = (string)row[3];
                }

                if (null != row[4])
                {
                    xmlConfig.Name = (string)row[4];
                }

                if (null != row[5])
                {
                    xmlConfig.Value = (string)row[5];
                }

                int flags = (int)row[6];

                if (0x1 == (flags & 0x1))
                {
                    xmlConfig.Node = Util.XmlConfig.NodeType.element;
                }
                else if (0x2 == (flags & 0x2))
                {
                    xmlConfig.Node = Util.XmlConfig.NodeType.value;
                }
                else if (0x4 == (flags & 0x4))
                {
                    xmlConfig.Node = Util.XmlConfig.NodeType.document;
                }

                if (0x10 == (flags & 0x10))
                {
                    xmlConfig.Action = Util.XmlConfig.ActionType.create;
                }
                else if (0x20 == (flags & 0x20))
                {
                    xmlConfig.Action = Util.XmlConfig.ActionType.delete;
                }

                if (0x100 == (flags & 0x100))
                {
                    xmlConfig.On = Util.XmlConfig.OnType.install;
                }
                else if (0x200 == (flags & 0x200))
                {
                    xmlConfig.On = Util.XmlConfig.OnType.uninstall;
                }

                if (0x00001000 == (flags & 0x00001000))
                {
                    xmlConfig.PreserveModifiedDate = Util.YesNoType.yes;
                }

                if (null != row[8])
                {
                    xmlConfig.Sequence = (int)row[8];
                }

                this.Core.IndexElement(row, xmlConfig);
            }
        }

        /// <summary>
        /// Finalize the Perfmon table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since the PerfCounter element nests under a File element, but
        /// the Perfmon table does not have a foreign key relationship with
        /// the File table (instead it has a formatted string that usually
        /// refers to a file row - but doesn't have to), the nesting must
        /// be inferred during finalization.
        /// </remarks>
        private void FinalizePerfmonTable(TableCollection tables)
        {
            Table perfmonTable = tables["Perfmon"];

            if (null != perfmonTable)
            {
                foreach (Row row in perfmonTable.Rows)
                {
                    string formattedFile = (string)row[1];
                    Util.PerfCounter perfCounter = (Util.PerfCounter)this.Core.GetIndexedElement(row);

                    // try to "de-format" the File column's value to determine the proper parent File element
                    if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                        && formattedFile.EndsWith("]", StringComparison.Ordinal))
                    {
                        string fileId = formattedFile.Substring(2, formattedFile.Length - 3);

                        Wix.File file = (Wix.File)this.Core.GetIndexedElement("File", fileId);
                        if (null != file)
                        {
                            file.AddChild(perfCounter);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, perfmonTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "File", formattedFile, "File"));
                        }
                    }
                    else
                    {
                        this.Core.OnMessage(UtilErrors.IllegalFileValueInPerfmonOrManifest(formattedFile, "Perfmon"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the PerfmonManifest table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void FinalizePerfmonManifestTable(TableCollection tables)
        {
            Table perfmonManifestTable = tables["PerfmonManifest"];

            if (null != perfmonManifestTable)
            {
                foreach (Row row in perfmonManifestTable.Rows)
                {
                    string formattedFile = (string)row[1];
                    Util.PerfCounterManifest perfCounterManifest = (Util.PerfCounterManifest)this.Core.GetIndexedElement(row);

                    // try to "de-format" the File column's value to determine the proper parent File element
                    if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                        && formattedFile.EndsWith("]", StringComparison.Ordinal))
                    {
                        string fileId = formattedFile.Substring(2, formattedFile.Length - 3);

                        Wix.File file = (Wix.File)this.Core.GetIndexedElement("File", fileId);
                        if (null != file)
                        {
                            file.AddChild(perfCounterManifest);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, perfCounterManifest.ResourceFileDirectory, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "File", formattedFile, "File"));
                        }
                    }
                    else
                    {
                        this.Core.OnMessage(UtilErrors.IllegalFileValueInPerfmonOrManifest(formattedFile, "PerfmonManifest"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the SecureObjects table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Nests the PermissionEx elements below their parent elements.  There are no declared foreign
        /// keys for the parents of the SecureObjects table.
        /// </remarks>
        private void FinalizeSecureObjectsTable(TableCollection tables)
        {
            Table createFolderTable = tables["CreateFolder"];
            Table secureObjectsTable = tables["SecureObjects"];

            Hashtable createFolders = new Hashtable();

            // index the CreateFolder table because the foreign key to this table from the
            // LockPermissions table is only part of the primary key of this table
            if (null != createFolderTable)
            {
                foreach (Row row in createFolderTable.Rows)
                {
                    Wix.CreateFolder createFolder = (Wix.CreateFolder)this.Core.GetIndexedElement(row);
                    string directoryId = (string)row[0];

                    if (!createFolders.Contains(directoryId))
                    {
                        createFolders.Add(directoryId, new ArrayList());
                    }
                    ((ArrayList)createFolders[directoryId]).Add(createFolder);
                }
            }

            if (null != secureObjectsTable)
            {
                foreach (Row row in secureObjectsTable.Rows)
                {
                    string id = (string)row[0];
                    string table = (string)row[1];

                    Util.PermissionEx permissionEx = (Util.PermissionEx)this.Core.GetIndexedElement(row);

                    if ("CreateFolder" == table)
                    {
                        ArrayList createFolderElements = (ArrayList)createFolders[id];

                        if (null != createFolderElements)
                        {
                            foreach (Wix.CreateFolder createFolder in createFolderElements)
                            {
                                createFolder.AddChild(permissionEx);
                            }
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, "SecureObjects", row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "LockObject", id, table));
                        }
                    }
                    else
                    {
                        Wix.IParentElement parentElement = (Wix.IParentElement)this.Core.GetIndexedElement(table, id);

                        if (null != parentElement)
                        {
                            parentElement.AddChild(permissionEx);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, "SecureObjects", row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "LockObject", id, table));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the ServiceConfig table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since there is no foreign key from the ServiceName column to the
        /// ServiceInstall table, this relationship must be handled late.
        /// </remarks>
        private void FinalizeServiceConfigTable(TableCollection tables)
        {
            Table serviceConfigTable = tables["ServiceConfig"];
            Table serviceInstallTable = tables["ServiceInstall"];

            Hashtable serviceInstalls = new Hashtable();

            // index the ServiceInstall table because the foreign key used by the ServiceConfig
            // table is actually the ServiceInstall.Name, not the ServiceInstall.ServiceInstall
            // this is unfortunate because the service Name is not guaranteed to be unique, so
            // decompiler must assume there could be multiple matches and add the ServiceConfig to each
            // TODO: the Component column information should be taken into acount to accurately identify
            // the correct column to use
            if (null != serviceInstallTable)
            {
                foreach (Row row in serviceInstallTable.Rows)
                {
                    string name = (string)row[1];
                    Wix.ServiceInstall serviceInstall = (Wix.ServiceInstall)this.Core.GetIndexedElement(row);

                    if (!serviceInstalls.Contains(name))
                    {
                        serviceInstalls.Add(name, new ArrayList());
                    }

                    ((ArrayList)serviceInstalls[name]).Add(serviceInstall);
                }
            }

            if (null != serviceConfigTable)
            {
                foreach (Row row in serviceConfigTable.Rows)
                {
                    Util.ServiceConfig serviceConfig = (Util.ServiceConfig)this.Core.GetIndexedElement(row);

                    if (0 == (int)row[2])
                    {
                        Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);

                        if (null != component)
                        {
                            component.AddChild(serviceConfig);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, serviceConfigTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                        }
                    }
                    else
                    {
                        ArrayList serviceInstallElements = (ArrayList)serviceInstalls[row[0]];

                        if (null != serviceInstallElements)
                        {
                            foreach (Wix.ServiceInstall serviceInstall in serviceInstallElements)
                            {
                                serviceInstall.AddChild(serviceConfig);
                            }
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, serviceConfigTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ServiceName", (string)row[0], "ServiceInstall"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the XmlConfig table.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void FinalizeXmlConfigTable(TableCollection tables)
        {
            Table xmlConfigTable = tables["XmlConfig"];

            if (null != xmlConfigTable)
            {
                foreach (Row row in xmlConfigTable.Rows)
                {
                    Util.XmlConfig xmlConfig = (Util.XmlConfig)this.Core.GetIndexedElement(row);

                    if (null == row[6] || 0 == (int)row[6])
                    {
                        Util.XmlConfig parentXmlConfig = (Util.XmlConfig)this.Core.GetIndexedElement("XmlConfig", (string)row[2]);

                        if (null != parentXmlConfig)
                        {
                            parentXmlConfig.AddChild(xmlConfig);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, xmlConfigTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ElementPath", (string)row[2], "XmlConfig"));
                        }
                    }
                    else
                    {
                        Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[7]);

                        if (null != component)
                        {
                            component.AddChild(xmlConfig);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, xmlConfigTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[7], "Component"));
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Finalize the XmlFile table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Some of the XmlFile table rows are compiler generated from util:EventManifest node
        /// These rows should not be appended to component.
        /// </remarks>
        private void FinalizeXmlFileTable(TableCollection tables)
        {
            Table xmlFileTable = tables["XmlFile"];
            Table eventManifestTable = tables["EventManifest"];

            if (null != xmlFileTable)
            {
                foreach (Row row in xmlFileTable.Rows)
                {
                    bool bManifestGenerated = false;
                    string xmlFileConfigId = (string)row[0];
                    if (null != eventManifestTable)
                    {
                        foreach (Row emrow in eventManifestTable.Rows)
                        {
                            string formattedFile = (string)emrow[1];
                            if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                                && formattedFile.EndsWith("]", StringComparison.Ordinal))
                            {
                                string fileId = formattedFile.Substring(2, formattedFile.Length - 3);
                                if (String.Equals(String.Concat("Config_", fileId, "ResourceFile"), xmlFileConfigId))
                                {
                                    Util.EventManifest eventManifest = (Util.EventManifest)this.Core.GetIndexedElement(emrow);
                                    if (null != eventManifest)
                                    {
                                        eventManifest.ResourceFile = (string)row[4];
                                    }
                                    bManifestGenerated = true;
                                }
                                
                                else if (String.Equals(String.Concat("Config_", fileId, "MessageFile"), xmlFileConfigId))
                                {
                                    Util.EventManifest eventManifest = (Util.EventManifest)this.Core.GetIndexedElement(emrow);
                                    if (null != eventManifest)
                                    {
                                        eventManifest.MessageFile = (string)row[4];
                                    }
                                    bManifestGenerated = true;
                                }
                            }
                        }
                    }

                    if (true == bManifestGenerated)
                        continue;
                    
                    Util.XmlFile xmlFile = new Util.XmlFile();

                    xmlFile.Id = (string)row[0];
                    xmlFile.File = (string)row[1];
                    xmlFile.ElementPath = (string)row[2];

                    if (null != row[3])
                    {
                        xmlFile.Name = (string)row[3];
                    }

                    if (null != row[4])
                    {
                        xmlFile.Value = (string)row[4];
                    }

                    int flags = (int)row[5];
                    if (0x1 == (flags & 0x1) && 0x2 == (flags & 0x2))
                    {
                        this.Core.OnMessage(WixWarnings.IllegalColumnValue(row.SourceLineNumbers, xmlFileTable.Name, row.Fields[5].Column.Name, row[5]));
                    }
                    else if (0x1 == (flags & 0x1))
                    {
                        xmlFile.Action = Util.XmlFile.ActionType.createElement;
                    }
                    else if (0x2 == (flags & 0x2))
                    {
                        xmlFile.Action = Util.XmlFile.ActionType.deleteValue;
                    }
                    else
                    {
                        xmlFile.Action = Util.XmlFile.ActionType.setValue;
                    }

                    if (0x100 == (flags & 0x100))
                    {
                        xmlFile.SelectionLanguage = Util.XmlFile.SelectionLanguageType.XPath;
                    }

                    if (0x00001000 == (flags & 0x00001000))
                    {
                        xmlFile.PreserveModifiedDate = Util.YesNoType.yes;
                    }

                    if (0x00010000 == (flags & 0x00010000))
                    {
                        xmlFile.Permanent = Util.YesNoType.yes;
                    }

                    if (null != row[7])
                    {
                        xmlFile.Sequence = (int)row[7];
                    }

                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[6]);

                    if (null != component)
                    {
                        component.AddChild(xmlFile);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, xmlFileTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[6], "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the eventManifest table.
        /// This function must be called after FinalizeXmlFileTable
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void FinalizeEventManifestTable(TableCollection tables)
        {
            Table eventManifestTable = tables["EventManifest"];

            if (null != eventManifestTable)
            {
                foreach (Row row in eventManifestTable.Rows)
                {
                    string formattedFile = (string)row[1];
                    Util.EventManifest eventManifest = (Util.EventManifest)this.Core.GetIndexedElement(row);

                    // try to "de-format" the File column's value to determine the proper parent File element
                    if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                        && formattedFile.EndsWith("]", StringComparison.Ordinal))
                    {
                        string fileId = formattedFile.Substring(2, formattedFile.Length - 3);

                        Wix.File file = (Wix.File)this.Core.GetIndexedElement("File", fileId);
                        if (null != file)
                        {
                            file.AddChild(eventManifest);
                        }
                    }
                    else
                    {
                       this.Core.OnMessage(UtilErrors.IllegalFileValueInPerfmonOrManifest(formattedFile, "EventManifest"));
                    }
                }
            }
        }
    }
}


