// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;

    using IIs = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.IIs;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "Certificate":
                    this.DecompileCertificateTable(table);
                    break;
                case "CertificateHash":
                    // There is nothing to do for this table, it contains no authored data
                    // to be decompiled.
                    break;
                case "IIsAppPool":
                    this.DecompileIIsAppPoolTable(table);
                    break;
                case "IIsFilter":
                    this.DecompileIIsFilterTable(table);
                    break;
                case "IIsProperty":
                    this.DecompileIIsPropertyTable(table);
                    break;
                case "IIsHttpHeader":
                    this.DecompileIIsHttpHeaderTable(table);
                    break;
                case "IIsMimeMap":
                    this.DecompileIIsMimeMapTable(table);
                    break;
                case "IIsWebAddress":
                    this.DecompileIIsWebAddressTable(table);
                    break;
                case "IIsWebApplication":
                    this.DecompileIIsWebApplicationTable(table);
                    break;
                case "IIsWebDirProperties":
                    this.DecompileIIsWebDirPropertiesTable(table);
                    break;
                case "IIsWebError":
                    this.DecompileIIsWebErrorTable(table);
                    break;
                case "IIsWebLog":
                    this.DecompileIIsWebLogTable(table);
                    break;
                case "IIsWebServiceExtension":
                    this.DecompileIIsWebServiceExtensionTable(table);
                    break;
                case "IIsWebSite":
                    this.DecompileIIsWebSiteTable(table);
                    break;
                case "IIsWebVirtualDir":
                    this.DecompileIIsWebVirtualDirTable(table);
                    break;
                case "IIsWebSiteCertificates":
                    this.DecompileIIsWebSiteCertificatesTable(table);
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
            this.FinalizeIIsMimeMapTable(tables);
            this.FinalizeIIsHttpHeaderTable(tables);
            this.FinalizeIIsWebApplicationTable(tables);
            this.FinalizeIIsWebErrorTable(tables);
            this.FinalizeIIsWebVirtualDirTable(tables);
            this.FinalizeIIsWebSiteCertificatesTable(tables);
            this.FinalizeWebAddressTable(tables);
        }

        /// <summary>
        /// Decompile the Certificate table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileCertificateTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.Certificate certificate = new IIs.Certificate();

                certificate.Id = (string)row[0];
                certificate.Name = (string)row[2];

                switch ((int)row[3])
                {
                    case 1:
                        certificate.StoreLocation = IIs.Certificate.StoreLocationType.currentUser;
                        break;
                    case 2:
                        certificate.StoreLocation = IIs.Certificate.StoreLocationType.localMachine;
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                switch ((string)row[4])
                {
                    case "CA":
                        certificate.StoreName = IIs.Certificate.StoreNameType.ca;
                        break;
                    case "MY":
                        certificate.StoreName = IIs.Certificate.StoreNameType.my;
                        break;
                    case "REQUEST":
                        certificate.StoreName = IIs.Certificate.StoreNameType.request;
                        break;
                    case "Root":
                        certificate.StoreName = IIs.Certificate.StoreNameType.root;
                        break;
                    case "AddressBook":
                        certificate.StoreName = IIs.Certificate.StoreNameType.otherPeople;
                        break;
                    case "TrustedPeople":
                        certificate.StoreName = IIs.Certificate.StoreNameType.trustedPeople;
                        break;
                    case "TrustedPublisher":
                        certificate.StoreName = IIs.Certificate.StoreNameType.trustedPublisher;
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                int attribute = (int)row[5];

                if (0x1 == (attribute & 0x1))
                {
                    certificate.Request = IIs.YesNoType.yes;
                }

                if (0x2 == (attribute & 0x2))
                {
                    if (null != row[6])
                    {
                        certificate.BinaryKey = (string)row[6];
                    }
                    else
                    {
                        // TODO: warn about expected value in row 5
                    }
                }
                else if (null != row[7])
                {
                    certificate.CertificatePath = (string)row[7];
                }

                if (0x4 == (attribute & 0x4))
                {
                    certificate.Overwrite = IIs.YesNoType.yes;
                }

                if (null != row[8])
                {
                    certificate.PFXPassword = (string)row[8];
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(certificate);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the IIsAppPool table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsAppPoolTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebAppPool webAppPool = new IIs.WebAppPool();

                webAppPool.Id = (string)row[0];

                webAppPool.Name = (string)row[1];

                switch ((int)row[3] & 0x1F)
                {
                    case 1:
                        webAppPool.Identity = IIs.WebAppPool.IdentityType.networkService;
                        break;
                    case 2:
                        webAppPool.Identity = IIs.WebAppPool.IdentityType.localService;
                        break;
                    case 4:
                        webAppPool.Identity = IIs.WebAppPool.IdentityType.localSystem;
                        break;
                    case 8:
                        webAppPool.Identity = IIs.WebAppPool.IdentityType.other;
                        break;
                    case 0x10:
                        webAppPool.Identity = IIs.WebAppPool.IdentityType.applicationPoolIdentity;
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                if (null != row[4])
                {
                    webAppPool.User = (string)row[4];
                }

                if (null != row[5])
                {
                    webAppPool.RecycleMinutes = (int)row[5];
                }

                if (null != row[6])
                {
                    webAppPool.RecycleRequests = (int)row[6];
                }

                if (null != row[7])
                {
                    string[] recycleTimeValues = ((string)row[7]).Split(',');

                    foreach (string recycleTimeValue in recycleTimeValues)
                    {
                        IIs.RecycleTime recycleTime = new IIs.RecycleTime();

                        recycleTime.Value = recycleTimeValue;

                        webAppPool.AddChild(recycleTime);
                    }
                }

                if (null != row[8])
                {
                    webAppPool.IdleTimeout = (int)row[8];
                }

                if (null != row[9])
                {
                    webAppPool.QueueLimit = (int)row[9];
                }

                if (null != row[10])
                {
                    string[] cpuMon = ((string)row[10]).Split(',');

                    if (0 < cpuMon.Length && "0" != cpuMon[0])
                    {
                        webAppPool.MaxCpuUsage = Convert.ToInt32(cpuMon[0], CultureInfo.InvariantCulture);
                    }

                    if (1 < cpuMon.Length)
                    {
                        webAppPool.RefreshCpu = Convert.ToInt32(cpuMon[1], CultureInfo.InvariantCulture);
                    }

                    if (2 < cpuMon.Length)
                    {
                        switch (Convert.ToInt32(cpuMon[2], CultureInfo.InvariantCulture))
                        {
                            case 0:
                                webAppPool.CpuAction = IIs.WebAppPool.CpuActionType.none;
                                break;
                            case 1:
                                webAppPool.CpuAction = IIs.WebAppPool.CpuActionType.shutdown;
                                break;
                            default:
                                // TODO: warn
                                break;
                        }
                    }

                    if (3 < cpuMon.Length)
                    {
                        // TODO: warn
                    }
                }

                if (null != row[11])
                {
                    webAppPool.MaxWorkerProcesses = (int)row[11];
                }

                if (null != row[12])
                {
                    webAppPool.VirtualMemory = (int)row[12];
                }

                if (null != row[13])
                {
                    webAppPool.PrivateMemory = (int)row[13];
                }

                if (null != row[14])
                {
                    webAppPool.ManagedRuntimeVersion = (string)row[14];
                }

                if (null != row[15])
                {
                    webAppPool.ManagedPipelineMode = (string)row[15];
                }

                if (null != row[2])
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);

                    if (null != component)
                    {
                        component.AddChild(webAppPool);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(webAppPool);
                }
            }
        }

        /// <summary>
        /// Decompile the IIsProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsPropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebProperty webProperty = new IIs.WebProperty();

                switch ((string)row[0])
                {
                    case "ETagChangeNumber":
                        webProperty.Id = IIs.WebProperty.IdType.ETagChangeNumber;
                        break;
                    case "IIs5IsolationMode":
                        webProperty.Id = IIs.WebProperty.IdType.IIs5IsolationMode;
                        break;
                    case "LogInUTF8":
                        webProperty.Id = IIs.WebProperty.IdType.LogInUTF8;
                        break;
                    case "MaxGlobalBandwidth":
                        webProperty.Id = IIs.WebProperty.IdType.MaxGlobalBandwidth;
                        break;
                }

                if (0 != (int)row[2])
                {
                    // TODO: warn about value in unused column
                }

                if (null != row[3])
                {
                    webProperty.Value = (string)row[3];
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(webProperty);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the IIsHttpHeader table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsHttpHeaderTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.HttpHeader httpHeader = new IIs.HttpHeader();

                httpHeader.Name = (string)row[3];

                // the ParentType and Parent columns are handled in FinalizeIIsHttpHeaderTable

                httpHeader.Value = (string)row[4];

                this.Core.IndexElement(row, httpHeader);
            }
        }

        /// <summary>
        /// Decompile the IIsMimeMap table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsMimeMapTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.MimeMap mimeMap = new IIs.MimeMap();

                mimeMap.Id = (string)row[0];

                // the ParentType and ParentValue columns are handled in FinalizeIIsMimeMapTable

                mimeMap.Type = (string)row[3];

                mimeMap.Extension = (string)row[4];

                this.Core.IndexElement(row, mimeMap);
            }
        }

        /// <summary>
        /// Decompile the IIsWebAddress table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebAddressTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebAddress webAddress = new IIs.WebAddress();

                webAddress.Id = (string)row[0];

                if (null != row[2])
                {
                    webAddress.IP = (string)row[2];
                }

                webAddress.Port = (string)row[3];

                if (null != row[4])
                {
                    webAddress.Header = (string)row[4];
                }

                if (null != row[5] && 1 == (int)row[5])
                {
                    webAddress.Secure = IIs.YesNoType.yes;
                }

                this.Core.IndexElement(row, webAddress);
            }
        }

        /// <summary>
        /// Decompile the IIsWebApplication table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebApplicationTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebApplication webApplication = new IIs.WebApplication();

                webApplication.Id = (string)row[0];

                webApplication.Name = (string)row[1];

                // these are not listed incorrectly - the order is low, high, medium
                switch ((int)row[2])
                {
                    case 0:
                        webApplication.Isolation = IIs.WebApplication.IsolationType.low;
                        break;
                    case 1:
                        webApplication.Isolation = IIs.WebApplication.IsolationType.high;
                        break;
                    case 2:
                        webApplication.Isolation = IIs.WebApplication.IsolationType.medium;
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                if (null != row[3])
                {
                    switch ((int)row[3])
                    {
                        case 0:
                            webApplication.AllowSessions = IIs.YesNoDefaultType.no;
                            break;
                        case 1:
                            webApplication.AllowSessions = IIs.YesNoDefaultType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[4])
                {
                    webApplication.SessionTimeout = (int)row[4];
                }

                if (null != row[5])
                {
                    switch ((int)row[5])
                    {
                        case 0:
                            webApplication.Buffer = IIs.YesNoDefaultType.no;
                            break;
                        case 1:
                            webApplication.Buffer = IIs.YesNoDefaultType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[6])
                {
                    switch ((int)row[6])
                    {
                        case 0:
                            webApplication.ParentPaths = IIs.YesNoDefaultType.no;
                            break;
                        case 1:
                            webApplication.ParentPaths = IIs.YesNoDefaultType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[7])
                {
                    switch ((string)row[7])
                    {
                        case "JScript":
                            webApplication.DefaultScript = IIs.WebApplication.DefaultScriptType.JScript;
                            break;
                        case "VBScript":
                            webApplication.DefaultScript = IIs.WebApplication.DefaultScriptType.VBScript;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[8])
                {
                    webApplication.ScriptTimeout = (int)row[8];
                }

                if (null != row[9])
                {
                    switch ((int)row[9])
                    {
                        case 0:
                            webApplication.ServerDebugging = IIs.YesNoDefaultType.no;
                            break;
                        case 1:
                            webApplication.ServerDebugging = IIs.YesNoDefaultType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[10])
                {
                    switch ((int)row[10])
                    {
                        case 0:
                            webApplication.ClientDebugging = IIs.YesNoDefaultType.no;
                            break;
                        case 1:
                            webApplication.ClientDebugging = IIs.YesNoDefaultType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[11])
                {
                    webApplication.WebAppPool = (string)row[11];
                }

                this.Core.IndexElement(row, webApplication);
            }
        }

        /// <summary>
        /// Decompile the IIsWebDirProperties table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebDirPropertiesTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebDirProperties webDirProperties = new IIs.WebDirProperties();

                webDirProperties.Id = (string)row[0];

                if (null != row[1])
                {
                    int access = (int)row[1];

                    if (0x1 == (access & 0x1))
                    {
                        webDirProperties.Read = IIs.YesNoType.yes;
                    }

                    if (0x2 == (access & 0x2))
                    {
                        webDirProperties.Write = IIs.YesNoType.yes;
                    }

                    if (0x4 == (access & 0x4))
                    {
                        webDirProperties.Execute = IIs.YesNoType.yes;
                    }

                    if (0x200 == (access & 0x200))
                    {
                        webDirProperties.Script = IIs.YesNoType.yes;
                    }
                }

                if (null != row[2])
                {
                    int authorization = (int)row[2];

                    if (0x1 == (authorization & 0x1))
                    {
                        webDirProperties.AnonymousAccess = IIs.YesNoType.yes;
                    }
                    else // set one of the properties to 'no' to force the output value to be '0' if not other attributes are set
                    {
                        webDirProperties.AnonymousAccess = IIs.YesNoType.no;
                    }

                    if (0x2 == (authorization & 0x2))
                    {
                        webDirProperties.BasicAuthentication = IIs.YesNoType.yes;
                    }

                    if (0x4 == (authorization & 0x4))
                    {
                        webDirProperties.WindowsAuthentication = IIs.YesNoType.yes;
                    }

                    if (0x10 == (authorization & 0x10))
                    {
                        webDirProperties.DigestAuthentication = IIs.YesNoType.yes;
                    }

                    if (0x40 == (authorization & 0x40))
                    {
                        webDirProperties.PassportAuthentication = IIs.YesNoType.yes;
                    }
                }

                if (null != row[3])
                {
                    webDirProperties.AnonymousUser = (string)row[3];
                }

                if (null != row[4] && 1 == (int)row[4])
                {
                    webDirProperties.IIsControlledPassword = IIs.YesNoType.yes;
                }

                if (null != row[5])
                {
                    switch ((int)row[5])
                    {
                        case 0:
                            webDirProperties.LogVisits = IIs.YesNoType.no;
                            break;
                        case 1:
                            webDirProperties.LogVisits = IIs.YesNoType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[6])
                {
                    switch ((int)row[6])
                    {
                        case 0:
                            webDirProperties.Index = IIs.YesNoType.no;
                            break;
                        case 1:
                            webDirProperties.Index = IIs.YesNoType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[7])
                {
                    webDirProperties.DefaultDocuments = (string)row[7];
                }

                if (null != row[8])
                {
                    switch ((int)row[8])
                    {
                        case 0:
                            webDirProperties.AspDetailedError = IIs.YesNoType.no;
                            break;
                        case 1:
                            webDirProperties.AspDetailedError = IIs.YesNoType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[9])
                {
                    webDirProperties.HttpExpires = (string)row[9];
                }

                if (null != row[10])
                {
                    // force the value to be a positive number
                    webDirProperties.CacheControlMaxAge = unchecked((uint)(int)row[10]);
                }

                if (null != row[11])
                {
                    webDirProperties.CacheControlCustom = (string)row[11];
                }

                if (null != row[12])
                {
                    switch ((int)row[8])
                    {
                        case 0:
                            webDirProperties.ClearCustomError = IIs.YesNoType.no;
                            break;
                        case 1:
                            webDirProperties.ClearCustomError = IIs.YesNoType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[13])
                {
                    int accessSSLFlags = (int)row[13];

                    if (0x8 == (accessSSLFlags & 0x8))
                    {
                        webDirProperties.AccessSSL = IIs.YesNoType.yes;
                    }

                    if (0x20 == (accessSSLFlags & 0x20))
                    {
                        webDirProperties.AccessSSLNegotiateCert = IIs.YesNoType.yes;
                    }

                    if (0x40 == (accessSSLFlags & 0x40))
                    {
                        webDirProperties.AccessSSLRequireCert = IIs.YesNoType.yes;
                    }

                    if (0x80 == (accessSSLFlags & 0x80))
                    {
                        webDirProperties.AccessSSLMapCert = IIs.YesNoType.yes;
                    }

                    if (0x100 == (accessSSLFlags & 0x100))
                    {
                        webDirProperties.AccessSSL128 = IIs.YesNoType.yes;
                    }
                }

                if (null != row[14])
                {
                    webDirProperties.AuthenticationProviders = (string)row[14];
                }

                this.Core.RootElement.AddChild(webDirProperties);
            }
        }

        /// <summary>
        /// Decompile the IIsWebError table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebErrorTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebError webError = new IIs.WebError();

                webError.ErrorCode = (int)row[0];

                webError.SubCode = (int)row[1];

                // the ParentType and ParentValue columns are handled in FinalizeIIsWebErrorTable

                if (null != row[4])
                {
                    webError.File = (string)row[4];
                }

                if (null != row[5])
                {
                    webError.URL = (string)row[5];
                }

                this.Core.IndexElement(row, webError);
            }
        }

        /// <summary>
        /// Decompile the IIsFilter table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsFilterTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebFilter webFilter = new IIs.WebFilter();

                webFilter.Id = (string)row[0];

                webFilter.Name = (string)row[1];

                if (null != row[3])
                {
                    webFilter.Path = (string)row[3];
                }

                if (null != row[5])
                {
                    webFilter.Description = (string)row[5];
                }

                webFilter.Flags = (int)row[6];

                if (null != row[7])
                {
                    switch ((int)row[7])
                    {
                        case (-1):
                            webFilter.LoadOrder = "last";
                            break;
                        case 0:
                            webFilter.LoadOrder = "first";
                            break;
                        default:
                            webFilter.LoadOrder = Convert.ToString((int)row[7], CultureInfo.InvariantCulture);
                            break;
                    }
                }

                if (null != row[4])
                {
                    IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement("IIsWebSite", (string)row[4]);

                    if (null != webSite)
                    {
                        webSite.AddChild(webFilter);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Web_", (string)row[4], "IIsWebSite"));
                    }
                }
                else // Component parent
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);

                    if (null != component)
                    {
                        component.AddChild(webFilter);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the IIsWebLog table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebLogTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebLog webLog = new IIs.WebLog();

                webLog.Id = (string)row[0];

                switch ((string)row[1])
                {
                    case "Microsoft IIS Log File Format":
                        webLog.Type = IIs.WebLog.TypeType.IIS;
                        break;
                    case "NCSA Common Log File Format":
                        webLog.Type = IIs.WebLog.TypeType.NCSA;
                        break;
                    case "none":
                        webLog.Type = IIs.WebLog.TypeType.none;
                        break;
                    case "ODBC Logging":
                        webLog.Type = IIs.WebLog.TypeType.ODBC;
                        break;
                    case "W3C Extended Log File Format":
                        webLog.Type = IIs.WebLog.TypeType.W3C;
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                this.Core.RootElement.AddChild(webLog);
            }
        }

        /// <summary>
        /// Decompile the IIsWebServiceExtension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebServiceExtensionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebServiceExtension webServiceExtension = new IIs.WebServiceExtension();

                webServiceExtension.Id = (string)row[0];

                webServiceExtension.File = (string)row[2];

                if (null != row[3])
                {
                    webServiceExtension.Description = (string)row[3];
                }

                if (null != row[4])
                {
                    webServiceExtension.Group = (string)row[4];
                }

                int attributes = (int)row[5];

                if (0x1 == (attributes & 0x1))
                {
                    webServiceExtension.Allow = IIs.YesNoType.yes;
                }
                else
                {
                    webServiceExtension.Allow = IIs.YesNoType.no;
                }

                if (0x2 == (attributes & 0x2))
                {
                    webServiceExtension.UIDeletable = IIs.YesNoType.yes;
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(webServiceExtension);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the IIsWebSite table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebSiteTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebSite webSite = new IIs.WebSite();

                webSite.Id = (string)row[0];

                if (null != row[2])
                {
                    webSite.Description = (string)row[2];
                }

                if (null != row[3])
                {
                    webSite.ConnectionTimeout = (int)row[3];
                }

                if (null != row[4])
                {
                    webSite.Directory = (string)row[4];
                }

                if (null != row[5])
                {
                    switch ((int)row[5])
                    {
                        case 0:
                            // this is the default
                            break;
                        case 1:
                            webSite.StartOnInstall = IIs.YesNoType.yes;
                            break;
                        case 2:
                            webSite.AutoStart = IIs.YesNoType.yes;
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[6])
                {
                    int attributes = (int)row[6];

                    if (0x2 == (attributes & 0x2))
                    {
                        webSite.ConfigureIfExists = IIs.YesNoType.no;
                    }
                }

                // the KeyAddress_ column is handled in FinalizeWebAddressTable

                if (null != row[8])
                {
                    webSite.DirProperties = (string)row[8];
                }

                // the Application_ column is handled in FinalizeIIsWebApplicationTable

                if (null != row[10])
                {
                    if (-1 != (int)row[10])
                    {
                        webSite.Sequence = (int)row[10];
                    }
                }

                if (null != row[11])
                {
                    webSite.WebLog = (string)row[11];
                }

                if (null != row[12])
                {
                    webSite.SiteId = (string)row[12];
                }

                if (null != row[1])
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);

                    if (null != component)
                    {
                        component.AddChild(webSite);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(webSite);
                }
                this.Core.IndexElement(row, webSite);
            }
        }

        /// <summary>
        /// Decompile the IIsWebVirtualDir table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebVirtualDirTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.WebVirtualDir webVirtualDir = new IIs.WebVirtualDir();

                webVirtualDir.Id = (string)row[0];

                // the Component_ and Web_ columns are handled in FinalizeIIsWebVirtualDirTable

                webVirtualDir.Alias = (string)row[3];

                webVirtualDir.Directory = (string)row[4];

                if (null != row[5])
                {
                    webVirtualDir.DirProperties = (string)row[5];
                }

                // the Application_ column is handled in FinalizeIIsWebApplicationTable

                this.Core.IndexElement(row, webVirtualDir);
            }
        }

        /// <summary>
        /// Decompile the IIsWebSiteCertificates table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebSiteCertificatesTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                IIs.CertificateRef certificateRef = new IIs.CertificateRef();

                certificateRef.Id = (string)row[1];

                this.Core.IndexElement(row, certificateRef);
            }
        }

        /// <summary>
        /// Finalize the IIsHttpHeader table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// The IIsHttpHeader table supports multiple parent types so no foreign key
        /// is declared and thus nesting must be done late.
        /// </remarks>
        private void FinalizeIIsHttpHeaderTable(TableCollection tables)
        {
            Table iisHttpHeaderTable = tables["IIsHttpHeader"];

            if (null != iisHttpHeaderTable)
            {
                foreach (Row row in iisHttpHeaderTable.Rows)
                {
                    IIs.HttpHeader httpHeader = (IIs.HttpHeader)this.Core.GetIndexedElement(row);

                    if (1 == (int)row[1])
                    {
                        IIs.WebVirtualDir webVirtualDir = (IIs.WebVirtualDir)this.Core.GetIndexedElement("IIsWebVirtualDir", (string)row[2]);
                        if (null != webVirtualDir)
                        {
                            webVirtualDir.AddChild(httpHeader);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisHttpHeaderTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ParentValue", (string)row[2], "IIsWebVirtualDir"));
                        }
                    }
                    else if (2 == (int)row[1])
                    {
                        IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement("IIsWebSite", (string)row[2]);
                        if (null != webSite)
                        {
                            webSite.AddChild(httpHeader);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisHttpHeaderTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ParentValue", (string)row[2], "IIsWebSite"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsMimeMap table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// The IIsMimeMap table supports multiple parent types so no foreign key
        /// is declared and thus nesting must be done late.
        /// </remarks>
        private void FinalizeIIsMimeMapTable(TableCollection tables)
        {
            Table iisMimeMapTable = tables["IIsMimeMap"];

            if (null != iisMimeMapTable)
            {
                foreach (Row row in iisMimeMapTable.Rows)
                {
                    IIs.MimeMap mimeMap = (IIs.MimeMap)this.Core.GetIndexedElement(row);

                    if (2 < (int)row[1] || 0 >= (int)row[1])
                    {
                        // TODO: warn about unknown parent type
                    }

                    IIs.WebVirtualDir webVirtualDir = (IIs.WebVirtualDir)this.Core.GetIndexedElement("IIsWebVirtualDir", (string)row[2]);
                    IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement("IIsWebSite", (string)row[2]);
                    if (null != webVirtualDir)
                    {
                        webVirtualDir.AddChild(mimeMap);
                    }
                    else if (null != webSite)
                    {
                        webSite.AddChild(mimeMap);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisMimeMapTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ParentValue", (string)row[2], "IIsWebVirtualDir"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebApplication table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since WebApplication elements may nest under a specific WebSite or
        /// WebVirtualDir (or just the root element), the nesting must be done late.
        /// </remarks>
        private void FinalizeIIsWebApplicationTable(TableCollection tables)
        {
            Table iisWebApplicationTable = tables["IIsWebApplication"];
            Table iisWebSiteTable = tables["IIsWebSite"];
            Table iisWebVirtualDirTable = tables["IIsWebVirtualDir"];

            Hashtable addedWebApplications = new Hashtable();

            if (null != iisWebSiteTable)
            {
                foreach (Row row in iisWebSiteTable.Rows)
                {
                    if (null != row[9])
                    {
                        IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement(row);

                        IIs.WebApplication webApplication = (IIs.WebApplication)this.Core.GetIndexedElement("IIsWebApplication", (string)row[9]);
                        if (null != webApplication)
                        {
                            webSite.AddChild(webApplication);
                            addedWebApplications[webApplication] = null;
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebSiteTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Application_", (string)row[9], "IIsWebApplication"));
                        }
                    }
                }
            }

            if (null != iisWebVirtualDirTable)
            {
                foreach (Row row in iisWebVirtualDirTable.Rows)
                {
                    if (null != row[6])
                    {
                        IIs.WebVirtualDir webVirtualDir = (IIs.WebVirtualDir)this.Core.GetIndexedElement(row);

                        IIs.WebApplication webApplication = (IIs.WebApplication)this.Core.GetIndexedElement("IIsWebApplication", (string)row[6]);
                        if (null != webApplication)
                        {
                            webVirtualDir.AddChild(webApplication);
                            addedWebApplications[webApplication] = null;
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebVirtualDirTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Application_", (string)row[6], "IIsWebApplication"));
                        }
                    }
                }
            }

            if (null != iisWebApplicationTable)
            {
                foreach (Row row in iisWebApplicationTable.Rows)
                {
                    IIs.WebApplication webApplication = (IIs.WebApplication)this.Core.GetIndexedElement(row);

                    if (!addedWebApplications.Contains(webApplication))
                    {
                        this.Core.RootElement.AddChild(webApplication);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebError table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since there is no foreign key relationship declared for this table
        /// (because it takes various parent types), it must be nested late.
        /// </remarks>
        private void FinalizeIIsWebErrorTable(TableCollection tables)
        {
            Table iisWebErrorTable = tables["IIsWebError"];

            if (null != iisWebErrorTable)
            {
                foreach (Row row in iisWebErrorTable.Rows)
                {
                    IIs.WebError webError = (IIs.WebError)this.Core.GetIndexedElement(row);

                    if (1 == (int)row[2]) // WebVirtualDir parent
                    {
                        IIs.WebVirtualDir webVirtualDir = (IIs.WebVirtualDir)this.Core.GetIndexedElement("IIsWebVirtualDir", (string)row[3]);

                        if (null != webVirtualDir)
                        {
                            webVirtualDir.AddChild(webError);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebErrorTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ParentValue", (string)row[3], "IIsWebVirtualDir"));
                        }
                    }
                    else if (2 == (int)row[2]) // WebSite parent
                    {
                        IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement("IIsWebSite", (string)row[3]);

                        if (null != webSite)
                        {
                            webSite.AddChild(webError);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebErrorTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "ParentValue", (string)row[3], "IIsWebSite"));
                        }
                    }
                    else
                    {
                        // TODO: warn unknown parent type
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebVirtualDir table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// WebVirtualDir elements nest under either a WebSite or component
        /// depending upon whether the component in the IIsWebVirtualDir row
        /// is the same as the one in the parent IIsWebSite row.
        /// </remarks>
        private void FinalizeIIsWebVirtualDirTable(TableCollection tables)
        {
            Table iisWebSiteTable = tables["IIsWebSite"];
            Table iisWebVirtualDirTable = tables["IIsWebVirtualDir"];

            Hashtable iisWebSiteRows = new Hashtable();

            // index the IIsWebSite rows by their primary keys
            if (null != iisWebSiteTable)
            {
                foreach (Row row in iisWebSiteTable.Rows)
                {
                    iisWebSiteRows.Add(row[0], row);
                }
            }

            if (null != iisWebVirtualDirTable)
            {
                foreach (Row row in iisWebVirtualDirTable.Rows)
                {
                    IIs.WebVirtualDir webVirtualDir = (IIs.WebVirtualDir)this.Core.GetIndexedElement(row);
                    Row iisWebSiteRow = (Row)iisWebSiteRows[row[2]];

                    if (null != iisWebSiteRow)
                    {
                        if ((string)iisWebSiteRow[1] == (string)row[1])
                        {
                            IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement(iisWebSiteRow);

                            webSite.AddChild(webVirtualDir);
                        }
                        else
                        {
                            Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);

                            if (null != component)
                            {
                                webVirtualDir.WebSite = (string)row[2];
                                component.AddChild(webVirtualDir);
                            }
                            else
                            {
                                this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebVirtualDirTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                            }
                        }
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebVirtualDirTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Web_", (string)row[2], "IIsWebSite"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebSiteCertificates table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// This table creates CertificateRef elements which nest under WebSite
        /// elements.
        /// </remarks>
        private void FinalizeIIsWebSiteCertificatesTable(TableCollection tables)
        {
            Table IIsWebSiteCertificatesTable = tables["IIsWebSiteCertificates"];

            if (null != IIsWebSiteCertificatesTable)
            {
                foreach (Row row in IIsWebSiteCertificatesTable.Rows)
                {
                    IIs.CertificateRef certificateRef = (IIs.CertificateRef)this.Core.GetIndexedElement(row);
                    IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement("IIsWebSite", (string)row[0]);

                    if (null != webSite)
                    {
                        webSite.AddChild(certificateRef);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, IIsWebSiteCertificatesTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Web_", (string)row[0], "IIsWebSite"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the WebAddress table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// There is a circular dependency between the WebAddress and WebSite
        /// tables, so nesting must be handled here.
        /// </remarks>
        private void FinalizeWebAddressTable(TableCollection tables)
        {
            Table iisWebAddressTable = tables["IIsWebAddress"];
            Table iisWebSiteTable = tables["IIsWebSite"];

            Hashtable addedWebAddresses = new Hashtable();

            if (null != iisWebSiteTable)
            {
                foreach (Row row in iisWebSiteTable.Rows)
                {
                    IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement(row);

                    IIs.WebAddress webAddress = (IIs.WebAddress)this.Core.GetIndexedElement("IIsWebAddress", (string)row[7]);
                    if (null != webAddress)
                    {
                        webSite.AddChild(webAddress);
                        addedWebAddresses[webAddress] = null;
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebSiteTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "KeyAddress_", (string)row[7], "IIsWebAddress"));
                    }
                }
            }

            if (null != iisWebAddressTable)
            {
                foreach (Row row in iisWebAddressTable.Rows)
                {
                    IIs.WebAddress webAddress = (IIs.WebAddress)this.Core.GetIndexedElement(row);

                    if (!addedWebAddresses.Contains(webAddress))
                    {
                        IIs.WebSite webSite = (IIs.WebSite)this.Core.GetIndexedElement("IIsWebSite", (string)row[1]);

                        if (null != webSite)
                        {
                            webSite.AddChild(webAddress);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, iisWebAddressTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Web_", (string)row[1], "IIsWebSite"));
                        }
                    }
                }
            }
        }
    }
}
