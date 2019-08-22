// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Chain package info for binding Bundles.
    /// </summary>
    internal class ChainPackageInfo : Row
    {
        private const string PropertySqlFormat = "SELECT `Value` FROM `Property` WHERE `Property` = '{0}'";
        private const string PatchMetadataFormat = "SELECT `Value` FROM `MsiPatchMetadata` WHERE `Property` = '{0}'";

        private static readonly Encoding XmlOutputEncoding = new UTF8Encoding(false);

        private BinderCore core;
        private PayloadInfoRow packagePayload;

        public ChainPackageInfo(Row chainPackageRow, Table wixGroupTable, Dictionary<string, PayloadInfoRow> allPayloads, Dictionary<string, ContainerInfo> containers, BinderFileManager fileManager, BinderCore core, Output bundle) : base(chainPackageRow.SourceLineNumbers, bundle.Tables["ChainPackageInfo"])
        {
            string id = (string)chainPackageRow[0];
            string packageType = (string)chainPackageRow[1];
            string payloadId = (string)chainPackageRow[2];
            string installCondition = (string)chainPackageRow[3];
            string installCommand = (string)chainPackageRow[4];
            string repairCommand = (string)chainPackageRow[5];
            string uninstallCommand = (string)chainPackageRow[6];
            object cacheData = chainPackageRow[7];
            string cacheId = (string)chainPackageRow[8];
            object attributesData = chainPackageRow[9];
            object vitalData = chainPackageRow[10];
            object perMachineData = chainPackageRow[11];
            string detectCondition = (string)chainPackageRow[12];
            string msuKB = (string)chainPackageRow[13];
            object repairableData = chainPackageRow[14];
            string logPathVariable = (string)chainPackageRow[15];
            string rollbackPathVariable = (string)chainPackageRow[16];
            string protocol = (string)chainPackageRow[17];
            long installSize = (int)chainPackageRow[18];
            object suppressLooseFilePayloadGenerationData = chainPackageRow[19];
            object enableFeatureSelectionData = chainPackageRow[20];
            object forcePerMachineData = chainPackageRow[21];
            object displayInternalUIData = chainPackageRow[22];

            BundlePackageAttributes attributes = (null == attributesData) ? 0 : (BundlePackageAttributes)attributesData;

            YesNoAlwaysType cache = YesNoAlwaysType.NotSet;
            if (null != cacheData)
            {
                switch ((int)cacheData)
                {
                    case 0:
                        cache = YesNoAlwaysType.No;
                        break;
                    case 1:
                        cache = YesNoAlwaysType.Yes;
                        break;
                    case 2:
                        cache = YesNoAlwaysType.Always;
                        break;
                }
            }

            YesNoType vital = (null == vitalData || 1 == (int)vitalData) ? YesNoType.Yes : YesNoType.No;

            YesNoDefaultType perMachine = YesNoDefaultType.NotSet;
            if (null != perMachineData)
            {
                switch ((int)perMachineData)
                {
                    case 0:
                        perMachine = YesNoDefaultType.No;
                        break;
                    case 1:
                        perMachine = YesNoDefaultType.Yes;
                        break;
                    case 2:
                        perMachine = YesNoDefaultType.Default;
                        break;
                }
            }

            YesNoType repairable = YesNoType.NotSet;
            if (null != repairableData)
            {
                repairable = (1 == (int)repairableData) ? YesNoType.Yes : YesNoType.No;
            }

            YesNoType suppressLooseFilePayloadGeneration = YesNoType.NotSet;
            if (null != suppressLooseFilePayloadGenerationData)
            {
                suppressLooseFilePayloadGeneration = (1 == (int)suppressLooseFilePayloadGenerationData) ? YesNoType.Yes : YesNoType.No;
            }

            YesNoType enableFeatureSelection = YesNoType.NotSet;
            if (null != enableFeatureSelectionData)
            {
                enableFeatureSelection = (1 == (int)enableFeatureSelectionData) ? YesNoType.Yes : YesNoType.No;
            }

            YesNoType forcePerMachine = YesNoType.NotSet;
            if (null != forcePerMachineData)
            {
                forcePerMachine = (1 == (int)forcePerMachineData) ? YesNoType.Yes : YesNoType.No;
            }

            YesNoType displayInternalUI = YesNoType.NotSet;
            if (null != displayInternalUIData)
            {
                displayInternalUI = (1 == (int)displayInternalUIData) ? YesNoType.Yes : YesNoType.No;
            }

            this.core = core;

            this.Id = id;
            this.ChainPackageType = (Compiler.ChainPackageType)Enum.Parse(typeof(Compiler.ChainPackageType), packageType, true);
            PayloadInfoRow packagePayload;
            if (!allPayloads.TryGetValue(payloadId, out packagePayload))
            {
                this.core.OnMessage(WixErrors.IdentifierNotFound("Payload", payloadId));
                return;
            }
            this.PackagePayload = packagePayload;
            this.InstallCondition = installCondition;
            this.InstallCommand = installCommand;
            this.RepairCommand = repairCommand;
            this.UninstallCommand = uninstallCommand;

            this.PerMachine = perMachine;
            this.ProductCode = null;
            this.Cache = YesNoAlwaysType.NotSet == cache ? YesNoAlwaysType.Yes : cache; // The default is yes.
            this.CacheId = cacheId;
            this.Permanent = (BundlePackageAttributes.Permanent == (attributes & BundlePackageAttributes.Permanent));
            this.Visible = (BundlePackageAttributes.Visible == (attributes & BundlePackageAttributes.Visible));
            this.Slipstream = (BundlePackageAttributes.Slipstream == (attributes & BundlePackageAttributes.Slipstream));
            this.Vital = (YesNoType.Yes == vital); // true only when specifically requested.
            this.DetectCondition = detectCondition;
            this.MsuKB = msuKB;
            this.Protocol = protocol;
            this.Repairable = (YesNoType.Yes == repairable); // true only when specifically requested.
            this.LogPathVariable = logPathVariable;
            this.RollbackLogPathVariable = rollbackPathVariable;

            this.DisplayInternalUI = (YesNoType.Yes == displayInternalUI);

            this.Payloads = new List<PayloadInfoRow>();
            this.RelatedPackages = new List<RelatedPackage>();
            this.MsiFeatures = new List<MsiFeature>();
            this.MsiProperties = new List<MsiPropertyInfo>();
            this.SlipstreamMsps = new List<string>();
            this.ExitCodes = new List<ExitCodeInfo>();
            this.Provides = new ProvidesDependencyCollection();
            this.TargetCodes = new RowDictionary<WixBundlePatchTargetCodeRow>();

            // Default the display name and description to the package payload.
            this.DisplayName = this.PackagePayload.ProductName;
            this.Description = this.PackagePayload.Description;

            // Start the package size with the package's payload size.
            this.Size = this.PackagePayload.FileSize;

            // get all contained payloads...
            foreach (Row row in wixGroupTable.Rows)
            {
                string rowParentName = (string)row[0];
                string rowParentType = (string)row[1];
                string rowChildName = (string)row[2];
                string rowChildType = (string)row[3];

                if ("Package" == rowParentType && this.Id == rowParentName &&
                    "Payload" == rowChildType && this.PackagePayload.Id != rowChildName)
                {
                    PayloadInfoRow payload = allPayloads[rowChildName];
                    this.Payloads.Add(payload);

                    this.Size += payload.FileSize; // add each payload to the total size of the package.
                }
            }

            // Default the install size to the calculated package size.
            this.InstallSize = this.Size;

            switch (this.ChainPackageType)
            {
                case Compiler.ChainPackageType.Msi:
                    this.ResolveMsiPackage(fileManager, allPayloads, containers, suppressLooseFilePayloadGeneration, enableFeatureSelection, forcePerMachine, bundle);
                    break;
                case Compiler.ChainPackageType.Msp:
                    this.ResolveMspPackage(bundle);
                    break;
                case Compiler.ChainPackageType.Msu:
                    this.ResolveMsuPackage();
                    break;
                case Compiler.ChainPackageType.Exe:
                    this.ResolveExePackage();
                    break;
            }

            if (CompilerCore.IntegerNotSet != installSize)
            {
                this.InstallSize = installSize;
            }
        }

        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            private set { this.Fields[0].Data = value; }
        }

        public Compiler.ChainPackageType ChainPackageType
        {
            get { return (Compiler.ChainPackageType)Enum.Parse(typeof(Compiler.ChainPackageType), (string)this.Fields[1].Data, true); }
            private set { this.Fields[1].Data = value.ToString(); }
        }

        public PayloadInfoRow PackagePayload
        {
            get { return this.packagePayload; }
            private set
            {
                this.packagePayload = value;
                this.Payload = this.packagePayload.Id;
            }
        }

        public string Payload
        {
            get { return (string)this.Fields[2].Data; }
            private set { this.Fields[2].Data = value; }
        }

        public string InstallCondition
        {
            get { return (string)this.Fields[3].Data; }
            private set { this.Fields[3].Data = value;  }
        }

        public string InstallCommand
        {
            get { return (string)this.Fields[4].Data; }
            private set { this.Fields[4].Data = value; }
        }

        public string RepairCommand
        {
            get { return (string)this.Fields[5].Data; }
            private set { this.Fields[5].Data = value; }
        }

        public string UninstallCommand
        {
            get { return (string)this.Fields[6].Data; }
            private set { this.Fields[6].Data = value; }
        }

        public YesNoAlwaysType Cache
        {
            get
            {
                object cacheData = this.Fields[7].Data;

                if (null != cacheData)
                {
                    switch ((int)cacheData)
                    {
                        case 0:
                            return YesNoAlwaysType.No;
                        case 1:
                            return YesNoAlwaysType.Yes;
                        case 2:
                            return YesNoAlwaysType.Always;
                    }
                }

                return YesNoAlwaysType.NotSet;
            }

            private set
            {
                switch (value)
                {
                    case YesNoAlwaysType.No:
                        this.Fields[7].Data = 0;
                        break;
                    case YesNoAlwaysType.Yes:
                        this.Fields[7].Data = 1;
                        break;
                    case YesNoAlwaysType.Always:
                        this.Fields[7].Data = 2;
                        break;
                    default:
                        this.Fields[7].Data = null;
                        break;
                }
            }
        }

        public string CacheId
        {
            get { return (string)this.Fields[8].Data; }
            private set { this.Fields[8].Data = value; }
        }

        public BundlePackageAttributes Attributes
        {
            get
            {
                object data = this.Fields[9].Data;
                if (null != data)
                {
                    return (BundlePackageAttributes)(int)data;
                }

                return BundlePackageAttributes.None;
            }
            private set { this.Fields[9].Data = (int)value; }
        }

        public bool Permanent
        {
            get { return (BundlePackageAttributes.Permanent == (this.Attributes & BundlePackageAttributes.Permanent)); }
            private set
            {
                if (value)
                {
                    this.Attributes |= BundlePackageAttributes.Permanent;
                }
                else
                {
                    this.Attributes &= ~BundlePackageAttributes.Permanent;
                }
            }
        }

        public bool Visible
        {
            get { return (BundlePackageAttributes.Visible == (this.Attributes & BundlePackageAttributes.Visible)); }
            private set
            {
                if (value)
                {
                    this.Attributes |= BundlePackageAttributes.Visible;
                }
                else
                {
                    this.Attributes &= ~BundlePackageAttributes.Visible;
                }
            }
        }

        public bool Slipstream
        {
            get { return (BundlePackageAttributes.Slipstream == (this.Attributes & BundlePackageAttributes.Slipstream)); }
            private set
            {
                if (value)
                {
                    this.Attributes |= BundlePackageAttributes.Slipstream;
                }
                else
                {
                    this.Attributes &= ~BundlePackageAttributes.Slipstream;
                }
            }
        }

        public bool Vital
        {
            get { return (null != this.Fields[10].Data) && (1 == (int)this.Fields[10].Data); }
            private set { this.Fields[10].Data = value ? 1 : 0; }
        }

        public YesNoDefaultType PerMachine
        {
            get
            {
                object perMachineData = this.Fields[11].Data;

                if (null != perMachineData)
                {
                    switch ((int)perMachineData)
                    {
                        case 0:
                            return YesNoDefaultType.No;
                        case 1:
                            return YesNoDefaultType.Yes;
                        case 2:
                            return YesNoDefaultType.Default;
                    }
                }

                return YesNoDefaultType.NotSet;
            }

            set
            {
                switch (value)
                {
                    case YesNoDefaultType.No:
                        this.Fields[11].Data = 0;
                        break;
                    case YesNoDefaultType.Yes:
                        this.Fields[11].Data = 1;
                        break;
                    case YesNoDefaultType.Default:
                        this.Fields[11].Data = 2;
                        break;
                    default:
                        this.Fields[11].Data = null;
                        break;
                }
            }
        }

        public string DetectCondition
        {
            get { return (string)this.Fields[12].Data; }
            private set { this.Fields[12].Data = value; }
        }

        public string MsuKB
        {
            get { return (string)this.Fields[13].Data; }
            private set { this.Fields[13].Data = value; }
        }

        public bool Repairable
        {
            get { return (null != this.Fields[14].Data) && (1 == (int)this.Fields[14].Data); }
            private set { this.Fields[14].Data = value ? 1 : 0; }
        }

        public string LogPathVariable
        {
            get { return (string)this.Fields[15].Data; }
            private set { this.Fields[15].Data = value; }
        }

        public string RollbackLogPathVariable
        {
            get { return (string)this.Fields[16].Data; }
            private set { this.Fields[16].Data = value; }
        }

        public string Protocol
        {
            get { return (string)this.Fields[17].Data; }
            private set { this.Fields[17].Data = value; }
        }

        public long InstallSize
        {
            get
            {
                string value = (string)this.Fields[18].Data;
                return String.IsNullOrEmpty(value) ? 0L : Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }

            private set
            {
                this.Fields[18].Data = Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        public bool SuppressLooseFilePayloadGeneration
        {
            get { return (null != this.Fields[19].Data) && (1 == (int)this.Fields[19].Data); }
            private set { this.Fields[19].Data = value ? 1 : 0; }
        }

        public bool EnableFeatureSelection
        {
            get { return (null != this.Fields[20].Data) && (1 == (int)this.Fields[20].Data); }
            private set { this.Fields[20].Data = value ? 1 : 0; }
        }

        public bool ForcePerMachine
        {
            get { return (null != this.Fields[21].Data) && (1 == (int)this.Fields[21].Data); }
            private set { this.Fields[21].Data = value ? 1 : 0; }
        }

        public bool DisplayInternalUI
        {
            get { return (null != this.Fields[22].Data) && (1 == (int)this.Fields[22].Data); }
            private set { this.Fields[22].Data = value ? 1 : 0; }
        }

        public string ProductCode
        {
            get { return (string)this.Fields[23].Data; }
            private set { this.Fields[23].Data = value; }
        }

        public string UpgradeCode
        {
            get { return (string)this.Fields[24].Data; }
            private set { this.Fields[24].Data = value; }
        }

        public string Version
        {
            get { return (string)this.Fields[25].Data; }
            private set { this.Fields[25].Data = value; }
        }

        public string Language
        {
            get { return (string)this.Fields[26].Data; }
            private set { this.Fields[26].Data = value; }
        }

        public string DisplayName
        {
            get { return (string)this.Fields[27].Data; }
            private set { this.Fields[27].Data = value; }
        }

        public string Description
        {
            get { return (string)this.Fields[28].Data; }
            private set { this.Fields[28].Data = value; }
        }

        public string PatchCode
        {
            get { return (string)this.Fields[29].Data; }
            private set { this.Fields[29].Data = value; }
        }

        public string PatchXml
        {
            get { return (string)this.Fields[30].Data; }
            private set { this.Fields[30].Data = value; }
        }

        public string Manufacturer
        {
            get { return (string)this.Fields[31].Data; }
            private set { this.Fields[31].Data = value; }
        }

        public long Size { get; private set; }
        public List<PayloadInfoRow> Payloads { get; private set; }
        public List<RelatedPackage> RelatedPackages { get; private set; }
        public List<MsiFeature> MsiFeatures { get; private set; }
        public List<MsiPropertyInfo> MsiProperties { get; private set; }
        public List<string> SlipstreamMsps { get; private set; }
        public List<ExitCodeInfo> ExitCodes { get; private set; }
        public ProvidesDependencyCollection Provides { get; private set; }
        public RowDictionary<WixBundlePatchTargetCodeRow> TargetCodes { get; private set; }
        public bool TargetUnspecified { get; private set; }
        public RollbackBoundaryInfo RollbackBoundary { get; set; }
        public string RollbackBoundaryBackwardId { get; set; }

        /// <summary>
        /// Initializes package state from the MSI contents.
        /// </summary>
        private void ResolveMsiPackage(BinderFileManager fileManager, Dictionary<string, PayloadInfoRow> allPayloads, Dictionary<string, ContainerInfo> containers, YesNoType suppressLooseFilePayloadGeneration, YesNoType enableFeatureSelection, YesNoType forcePerMachine, Output bundle)
        {
            string sourcePath = this.PackagePayload.FullFileName;
            bool longNamesInImage = false;
            bool compressed = false;
            try
            {
                // Read data out of the msi database...
                using (Microsoft.Deployment.WindowsInstaller.SummaryInfo sumInfo = new Microsoft.Deployment.WindowsInstaller.SummaryInfo(sourcePath, false))
                {
                    // 1 is the Word Count summary information stream bit that means
                    // the MSI uses short file names when set. We care about long file
                    // names so check when the bit is not set.
                    longNamesInImage = 0 == (sumInfo.WordCount & 1);

                    // 2 is the Word Count summary information stream bit that means
                    // files are compressed in the MSI by default when the bit is set.
                    compressed = 2 == (sumInfo.WordCount & 2);

                    // 8 is the Word Count summary information stream bit that means
                    // "Elevated privileges are not required to install this package."
                    // in MSI 4.5 and below, if this bit is 0, elevation is required.
                    this.PerMachine = (0 == (sumInfo.WordCount & 8)) ? YesNoDefaultType.Yes : YesNoDefaultType.No;
                }

                using (Microsoft.Deployment.WindowsInstaller.Database db = new Microsoft.Deployment.WindowsInstaller.Database(sourcePath))
                {
                    this.ProductCode = ChainPackageInfo.GetProperty(db, "ProductCode");
                    this.Language = ChainPackageInfo.GetProperty(db, "ProductLanguage");
                    this.Version = ChainPackageInfo.GetProperty(db, "ProductVersion");

                    if (!Common.IsValidModuleOrBundleVersion(this.Version))
                    {
                        // not a proper .NET version (i.e., five fields); can we get a valid version number up to four fields?
                        string version = null;
                        string[] versionParts = this.Version.Split('.');
                        int count = versionParts.Length;
                        if (0 < count)
                        {
                            version = versionParts[0];
                            for (int i = 1; i < 4 && i < count; ++i)
                            {
                                version = String.Concat(version, ".", versionParts[i]);
                            }
                        }

                        if (!String.IsNullOrEmpty(version) && Common.IsValidModuleOrBundleVersion(version))
                        {
                            this.core.OnMessage(WixWarnings.VersionTruncated(this.PackagePayload.SourceLineNumbers, this.Version, sourcePath, version));
                            this.Version = version;
                        }
                        else
                        {
                            this.core.OnMessage(WixErrors.InvalidProductVersion(this.PackagePayload.SourceLineNumbers, this.Version, sourcePath));
                        }
                    }

                    if (String.IsNullOrEmpty(this.CacheId))
                    {
                        this.CacheId = String.Format("{0}v{1}", this.ProductCode, this.Version);
                    }

                    if (String.IsNullOrEmpty(this.DisplayName))
                    {
                        this.DisplayName = ChainPackageInfo.GetProperty(db, "ProductName");
                    }

                    this.Manufacturer = ChainPackageInfo.GetProperty(db, "Manufacturer");

                    if (YesNoType.Yes == forcePerMachine)
                    {
                        if (YesNoDefaultType.No == this.PerMachine)
                        {
                            this.core.OnMessage(WixWarnings.PerUserButForcingPerMachine(this.PackagePayload.SourceLineNumbers, sourcePath));
                            this.PerMachine = YesNoDefaultType.Yes; // ensure that we think the MSI is per-machine.
                        }

                        this.MsiProperties.Add(new MsiPropertyInfo(this.Id, "ALLUSERS", "1")); // force ALLUSERS=1 via the MSI command-line.
                    }
                    else if (ChainPackageInfo.HasProperty(db, "ALLUSERS"))
                    {
                        string allusers = ChainPackageInfo.GetProperty(db, "ALLUSERS");
                        if (allusers.Equals("1", StringComparison.Ordinal))
                        {
                            if (YesNoDefaultType.No == this.PerMachine)
                            {
                                this.core.OnMessage(WixErrors.PerUserButAllUsersEquals1(this.PackagePayload.SourceLineNumbers, sourcePath));
                            }
                        }
                        else if (allusers.Equals("2", StringComparison.Ordinal))
                        {
                            this.core.OnMessage(WixWarnings.DiscouragedAllUsersValue(this.PackagePayload.SourceLineNumbers, sourcePath, (YesNoDefaultType.Yes == this.PerMachine) ? "machine" : "user"));
                        }
                        else
                        {
                            this.core.OnMessage(WixErrors.UnsupportedAllUsersValue(this.PackagePayload.SourceLineNumbers, sourcePath, allusers));
                        }
                    }
                    else if (YesNoDefaultType.Yes == this.PerMachine) // not forced per-machine and no ALLUSERS property, flip back to per-user
                    {
                        this.core.OnMessage(WixWarnings.ImplicitlyPerUser(this.PackagePayload.SourceLineNumbers, sourcePath));
                        this.PerMachine = YesNoDefaultType.No;
                    }

                    if (String.IsNullOrEmpty(this.Description) && ChainPackageInfo.HasProperty(db, "ARPCOMMENTS"))
                    {
                        this.Description = ChainPackageInfo.GetProperty(db, "ARPCOMMENTS");
                    }

                    // Ensure the MSI package is appropriately marked visible or not.
                    bool alreadyVisible = !ChainPackageInfo.HasProperty(db, "ARPSYSTEMCOMPONENT");
                    if (alreadyVisible != this.Visible) // if not already set to the correct visibility.
                    {
                        // If the authoring specifically added "ARPSYSTEMCOMPONENT", don't do it again.
                        bool sysComponentSet = false;
                        foreach (MsiPropertyInfo propertyInfo in this.MsiProperties)
                        {
                            if ("ARPSYSTEMCOMPONENT".Equals(propertyInfo.Name, StringComparison.Ordinal))
                            {
                                sysComponentSet = true;
                                break;
                            }
                        }

                        if (!sysComponentSet)
                        {
                            this.MsiProperties.Add(new MsiPropertyInfo(this.Id, "ARPSYSTEMCOMPONENT", this.Visible ? "" : "1"));
                        }
                    }

                    // Unless the MSI or setup code overrides the default, set MSIFASTINSTALL for best performance.
                    if (!ChainPackageInfo.HasProperty(db, "MSIFASTINSTALL"))
                    {
                        bool fastInstallSet = false;
                        foreach (MsiPropertyInfo propertyInfo in this.MsiProperties)
                        {
                            if ("MSIFASTINSTALL".Equals(propertyInfo.Name, StringComparison.Ordinal))
                            {
                                fastInstallSet = true;
                                break;
                            }
                        }

                        if (!fastInstallSet)
                        {
                            this.MsiProperties.Add(new MsiPropertyInfo(this.Id, "MSIFASTINSTALL", "7"));
                        }
                    }

                    this.UpgradeCode = ChainPackageInfo.GetProperty(db, "UpgradeCode");

                        // Represent the Upgrade table as related packages.
                    if (db.Tables.Contains("Upgrade") && !String.IsNullOrEmpty(this.UpgradeCode))
                    {
                        using (Microsoft.Deployment.WindowsInstaller.View view = db.OpenView("SELECT `UpgradeCode`, `VersionMin`, `VersionMax`, `Language`, `Attributes` FROM `Upgrade`"))
                        {
                            view.Execute();
                            while (true)
                            {
                                using (Microsoft.Deployment.WindowsInstaller.Record record = view.Fetch())
                                {
                                    if (null == record)
                                    {
                                        break;
                                    }

                                    RelatedPackage related = new RelatedPackage();
                                    related.Id = record.GetString(1);
                                    related.MinVersion = record.GetString(2);
                                    related.MaxVersion = record.GetString(3);

                                    string languages = record.GetString(4);
                                    if (!String.IsNullOrEmpty(languages))
                                    {
                                        string[] splitLanguages = languages.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                        related.Languages.AddRange(splitLanguages);
                                    }

                                    int attributes = record.GetInteger(5);
                                    // when an Upgrade row has an upgrade code different than this package's upgrade code, don't count it as a possible downgrade to this package
                                    related.OnlyDetect = ((attributes & MsiInterop.MsidbUpgradeAttributesOnlyDetect) == MsiInterop.MsidbUpgradeAttributesOnlyDetect) && this.UpgradeCode.Equals(related.Id, StringComparison.OrdinalIgnoreCase);
                                    related.MinInclusive = (attributes & MsiInterop.MsidbUpgradeAttributesVersionMinInclusive) == MsiInterop.MsidbUpgradeAttributesVersionMinInclusive;
                                    related.MaxInclusive = (attributes & MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive) == MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive;
                                    related.LangInclusive = (attributes & MsiInterop.MsidbUpgradeAttributesLanguagesExclusive) == 0;

                                    this.RelatedPackages.Add(related);
                                }
                            }
                        }
                    }

                    // If feature selection is enabled, represent the Feature table in the manifest.
                    if (YesNoType.Yes == enableFeatureSelection && db.Tables.Contains("Feature"))
                    {
                        using (Microsoft.Deployment.WindowsInstaller.View featureView = db.OpenView("SELECT `Component_` FROM `FeatureComponents` WHERE `Feature_` = ?"),
                                    componentView = db.OpenView("SELECT `FileSize` FROM `File` WHERE `Component_` = ?"))
                        {
                            using (Microsoft.Deployment.WindowsInstaller.Record featureRecord = new Microsoft.Deployment.WindowsInstaller.Record(1),
                                          componentRecord = new Microsoft.Deployment.WindowsInstaller.Record(1))
                            {
                                using (Microsoft.Deployment.WindowsInstaller.View allFeaturesView = db.OpenView("SELECT * FROM `Feature`"))
                                {
                                    allFeaturesView.Execute();

                                    while (true)
                                    {
                                        using (Microsoft.Deployment.WindowsInstaller.Record allFeaturesResultRecord = allFeaturesView.Fetch())
                                        {
                                            if (null == allFeaturesResultRecord)
                                            {
                                                break;
                                            }

                                            MsiFeature feature = new MsiFeature();
                                            string featureName = allFeaturesResultRecord.GetString(1);
                                            feature.Name = featureName;
                                            feature.Size = 0;
                                            feature.Parent = allFeaturesResultRecord.GetString(2);
                                            feature.Title = allFeaturesResultRecord.GetString(3);
                                            feature.Description = allFeaturesResultRecord.GetString(4);
                                            feature.Display = allFeaturesResultRecord.GetInteger(5);
                                            feature.Level = allFeaturesResultRecord.GetInteger(6);
                                            feature.Directory = allFeaturesResultRecord.GetString(7);
                                            feature.Attributes = allFeaturesResultRecord.GetInteger(8);
                                            this.MsiFeatures.Add(feature);

                                            // Determine Feature Size
                                            featureRecord.SetString(1, featureName);
                                            featureView.Execute(featureRecord);

                                            // Loop over all the components
                                            while (true)
                                            {
                                                using (Microsoft.Deployment.WindowsInstaller.Record componentResultRecord = featureView.Fetch())
                                                {
                                                    if (null == componentResultRecord)
                                                    {
                                                        break;
                                                    }
                                                    string component = componentResultRecord.GetString(1);
                                                    componentRecord.SetString(1, component);
                                                    componentView.Execute(componentRecord);

                                                    // Loop over all the files

                                                    while (true)
                                                    {
                                                        using (Microsoft.Deployment.WindowsInstaller.Record fileResultRecord = componentView.Fetch())
                                                        {
                                                            if (null == fileResultRecord)
                                                            {
                                                                break;
                                                            }

                                                            string fileSize = fileResultRecord.GetString(1);
                                                            feature.Size += Convert.ToInt32(fileSize, CultureInfo.InvariantCulture.NumberFormat);
                                                        }
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Add all external cabinets as package payloads.
                    if (db.Tables.Contains("Media"))
                    {
                        foreach (string cabinet in db.ExecuteStringQuery("SELECT `Cabinet` FROM `Media`"))
                        {
                            if (!String.IsNullOrEmpty(cabinet) && !cabinet.StartsWith("#", StringComparison.Ordinal))
                            {
                                // If we didn't find the Payload as an existing child of the package, we need to
                                // add it.  We expect the file to exist on-disk in the same relative location as
                                // the MSI expects to find it...
                                string cabinetName = Path.Combine(Path.GetDirectoryName(this.PackagePayload.Name), cabinet);
                                if (!this.IsExistingPayload(cabinetName))
                                {
                                    string generatedId = Common.GenerateIdentifier("cab", true, this.PackagePayload.Id, cabinet);
                                    string payloadSourceFile = fileManager.ResolveRelatedFile(this.PackagePayload.UnresolvedSourceFile, cabinet, "Cabinet", this.PackagePayload.SourceLineNumbers, BindStage.Normal);

                                    PayloadInfoRow payloadNew = PayloadInfoRow.Create(this.SourceLineNumbers, bundle, generatedId, cabinetName, payloadSourceFile, true, this.PackagePayload.SuppressSignatureValidation, null, this.PackagePayload.Container, this.PackagePayload.Packaging);
                                    payloadNew.ParentPackagePayload = this.PackagePayload.Id;
                                    if (!String.IsNullOrEmpty(payloadNew.Container))
                                    {
                                        containers[payloadNew.Container].Payloads.Add(payloadNew);
                                    }

                                    this.Payloads.Add(payloadNew);
                                    allPayloads.Add(payloadNew.Id, payloadNew);

                                    this.Size += payloadNew.FileSize; // add the newly added payload to the package size.
                                }
                            }
                        }
                    }

                    // Add all external files as package payloads and calculate the total install size as the rollup of
                    // File table's sizes.
                    this.InstallSize = 0;
                    if (db.Tables.Contains("Component") && db.Tables.Contains("Directory") && db.Tables.Contains("File"))
                    {
                        Hashtable directories = new Hashtable();

                        // Load up the directory hash table so we will be able to resolve source paths
                        // for files in the MSI database.
                        using (Microsoft.Deployment.WindowsInstaller.View view = db.OpenView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
                        {
                            view.Execute();
                            while (true)
                            {
                                using (Microsoft.Deployment.WindowsInstaller.Record record = view.Fetch())
                                {
                                    if (null == record)
                                    {
                                        break;
                                    }
                                    string sourceName = Installer.GetName(record.GetString(3), true, longNamesInImage);
                                    directories.Add(record.GetString(1), new ResolvedDirectory(record.GetString(2), sourceName));
                                }
                            }
                        }

                        // Resolve the source paths to external files and add each file size to the total
                        // install size of the package.
                        using (Microsoft.Deployment.WindowsInstaller.View view = db.OpenView("SELECT `Directory_`, `File`, `FileName`, `File`.`Attributes`, `FileSize` FROM `Component`, `File` WHERE `Component`.`Component`=`File`.`Component_`"))
                        {
                            view.Execute();
                            while (true)
                            {
                                using (Microsoft.Deployment.WindowsInstaller.Record record = view.Fetch())
                                {
                                    if (null == record)
                                    {
                                        break;
                                    }

                                    // Skip adding the loose files as payloads if it was suppressed.
                                    if (suppressLooseFilePayloadGeneration != YesNoType.Yes)
                                    {
                                        // If the file is explicitly uncompressed or the MSI is uncompressed and the file is not
                                        // explicitly marked compressed then this is an external file.
                                        if (MsiInterop.MsidbFileAttributesNoncompressed == (record.GetInteger(4) & MsiInterop.MsidbFileAttributesNoncompressed) ||
                                            (!compressed && 0 == (record.GetInteger(4) & MsiInterop.MsidbFileAttributesCompressed)))
                                        {
                                            string generatedId = Common.GenerateIdentifier("f", true, this.PackagePayload.Id, record.GetString(2));
                                            string fileSourcePath = Binder.GetFileSourcePath(directories, record.GetString(1), record.GetString(3), compressed, longNamesInImage);
                                            string payloadSourceFile = fileManager.ResolveRelatedFile(this.PackagePayload.UnresolvedSourceFile, fileSourcePath, "File", this.PackagePayload.SourceLineNumbers, BindStage.Normal);
                                            string name = Path.Combine(Path.GetDirectoryName(this.PackagePayload.Name), fileSourcePath);

                                            if (!this.IsExistingPayload(name))
                                            {
                                                PayloadInfoRow payloadNew = PayloadInfoRow.Create(this.SourceLineNumbers, bundle, generatedId, name, payloadSourceFile, true, this.PackagePayload.SuppressSignatureValidation, null, this.PackagePayload.Container, this.PackagePayload.Packaging);
                                                payloadNew.ParentPackagePayload = this.PackagePayload.Id;
                                                if (!String.IsNullOrEmpty(payloadNew.Container))
                                                {
                                                    containers[payloadNew.Container].Payloads.Add(payloadNew);
                                                }

                                                this.Payloads.Add(payloadNew);
                                                allPayloads.Add(payloadNew.Id, payloadNew);

                                                this.Size += payloadNew.FileSize; // add the newly added payload to the package size.
                                            }
                                        }
                                    }

                                    this.InstallSize += record.GetInteger(5);
                                }
                            }
                        }
                    }

                    // Import any dependency providers from the MSI.
                    if (db.Tables.Contains("WixDependencyProvider"))
                    {
                        // Use the old schema (v1) if the Version column does not exist.
                        bool hasVersion = db.Tables["WixDependencyProvider"].Columns.Contains("Version");
                        string query = "SELECT `ProviderKey`, `Version`, `DisplayName`, `Attributes` FROM `WixDependencyProvider`";

                        if (!hasVersion)
                        {
                            query = "SELECT `ProviderKey`, `Attributes` FROM `WixDependencyProvider`";
                        }

                        using (Microsoft.Deployment.WindowsInstaller.View view = db.OpenView(query))
                        {
                            view.Execute();
                            while (true)
                            {
                                using (Microsoft.Deployment.WindowsInstaller.Record record = view.Fetch())
                                {
                                    if (null == record)
                                    {
                                        break;
                                    }

                                    // Import the provider key and attributes.
                                    ProvidesDependency dependency = null;
                                    string providerKey = record.GetString(1);

                                    if (hasVersion)
                                    {
                                        string version = record.GetString(2) ?? this.Version;
                                        string displayName = record.GetString(3) ?? this.DisplayName;
                                        int attributes = record.GetInteger(4);

                                        dependency = new ProvidesDependency(providerKey, version, displayName, attributes);
                                    }
                                    else
                                    {
                                        int attributes = record.GetInteger(2);

                                        dependency = new ProvidesDependency(providerKey, this.Version, this.DisplayName, attributes);
                                    }

                                    dependency.Imported = true;
                                    this.Provides.Add(dependency);
                                }
                            }
                        }
                    }
                }
            }
            catch (Microsoft.Deployment.WindowsInstaller.InstallerException e)
            {
                this.core.OnMessage(WixErrors.UnableToReadPackageInformation(this.PackagePayload.SourceLineNumbers, sourcePath, e.Message));
            }
        }

        /// <summary>
        /// Determines whether a payload with the same name already exists.
        /// </summary>
        /// <param name="payloadName">Payload to search for.</param>
        /// <returns>true if payload already exists; false otherwise</returns>
        private bool IsExistingPayload(string payloadName)
        {
            // Before adding the external file as another payload, we have to check to
            // see if it's already in the payload list. To do this, we have to match the
            // expected relative location of the external file specified in the MSI with
            // the destination @Name of the payload... the @SourceFile path on the payload
            // may be something completely different!
            foreach (PayloadInfoRow payload in this.Payloads)
            {
                if (payloadName.Equals(payload.Name, StringComparison.OrdinalIgnoreCase))
                {
                    payload.ParentPackagePayload = this.PackagePayload.Id;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Initializes package state from the MSP contents.
        /// </summary>
        private void ResolveMspPackage(Output bundle)
        {
            string sourcePath = this.PackagePayload.FullFileName;

            try
            {
                // Read data out of the msp database...
                using (Microsoft.Deployment.WindowsInstaller.SummaryInfo sumInfo = new Microsoft.Deployment.WindowsInstaller.SummaryInfo(sourcePath, false))
                {
                    this.PatchCode = sumInfo.RevisionNumber.Substring(0, 38);
                }

                using (Microsoft.Deployment.WindowsInstaller.Database db = new Microsoft.Deployment.WindowsInstaller.Database(sourcePath))
                {
                    if (String.IsNullOrEmpty(this.DisplayName))
                    {
                        this.DisplayName = ChainPackageInfo.GetPatchMetadataProperty(db, "DisplayName");
                    }

                    if (String.IsNullOrEmpty(this.Description))
                    {
                        this.Description = ChainPackageInfo.GetPatchMetadataProperty(db, "Description");
                    }

                    this.Manufacturer = ChainPackageInfo.GetPatchMetadataProperty(db, "ManufacturerName");
                }

                this.ProcessPatchXml(sourcePath, bundle);
            }
            catch (Microsoft.Deployment.WindowsInstaller.InstallerException e)
            {
                this.core.OnMessage(WixErrors.UnableToReadPackageInformation(this.PackagePayload.SourceLineNumbers, sourcePath, e.Message));
                return;
            }

            if (String.IsNullOrEmpty(this.CacheId))
            {
                this.CacheId = this.PatchCode;
            }
        }

        /// <summary>
        /// Initializes package state from the MSU contents.
        /// </summary>
        private void ResolveMsuPackage()
        {
            this.PerMachine = YesNoDefaultType.Yes; // MSUs are always per-machine.

            if (String.IsNullOrEmpty(this.CacheId))
            {
                this.CacheId = this.PackagePayload.Hash;
            }
        }

        /// <summary>
        /// Initializes package state from the EXE contents.
        /// </summary>
        private void ResolveExePackage()
        {
            if (String.IsNullOrEmpty(this.CacheId))
            {
                this.CacheId = this.PackagePayload.Hash;
            }

            this.Version = this.PackagePayload.Version;

            // TODO: Future version could add Manufacturer to table definition.
        }

        private void ProcessPatchXml(string sourcePath, Output bundle)
        {
            string patchXml = Microsoft.Deployment.WindowsInstaller.Installer.ExtractPatchXmlData(sourcePath);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(patchXml);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("p", "http://www.microsoft.com/msi/patch_applicability.xsd");

            // Determine target ProductCodes and/or UpgradeCodes.
            foreach (XmlNode node in doc.SelectNodes("/p:MsiPatch/p:TargetProduct", nsmgr))
            {
                // If this patch targets a product code, this is the best case.
                XmlNode targetCode = node.SelectSingleNode("p:TargetProductCode", nsmgr);
                WixBundlePatchTargetCodeAttributes attributes = WixBundlePatchTargetCodeAttributes.None;

                if (ChainPackageInfo.TargetsCode(targetCode))
                {
                    attributes = WixBundlePatchTargetCodeAttributes.TargetsProductCode;
                }
                else // maybe targets an upgrade code?
                {
                    targetCode = node.SelectSingleNode("p:UpgradeCode", nsmgr);
                    if (ChainPackageInfo.TargetsCode(targetCode))
                    {
                        attributes = WixBundlePatchTargetCodeAttributes.TargetsUpgradeCode;
                    }
                    else // this patch targets an unknown number of products
                    {
                        this.TargetUnspecified = true;
                    }
                }

                Table table = bundle.EnsureTable(this.core.TableDefinitions["WixBundlePatchTargetCode"]);
                WixBundlePatchTargetCodeRow row = (WixBundlePatchTargetCodeRow)table.CreateRow(this.PackagePayload.SourceLineNumbers, false);
                row.MspPackageId = this.PackagePayload.Id;
                row.TargetCode = targetCode.InnerText;
                row.Attributes = attributes;

                if (this.TargetCodes.TryAdd(row))
                {
                    table.Rows.Add(row);
                }
            }

            // Suppress patch sequence data for improved performance.
            if (this.core.GetProperty<bool>(Binder.PARAM_SPSD_NAME))
            {
                XmlNode root = doc.DocumentElement;
                foreach (XmlNode node in root.SelectNodes("p:SequenceData", nsmgr))
                {
                    root.RemoveChild(node);
                }
            }

            // Save the XML as compact as possible.
            using (StringWriter writer = new StringWriter())
            {
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Encoding = ChainPackageInfo.XmlOutputEncoding,
                    Indent = false,
                    NewLineChars = string.Empty,
                    NewLineHandling = NewLineHandling.Replace,
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
                {
                    doc.WriteTo(xmlWriter);
                }

                this.PatchXml = writer.ToString();
            }
        }

        /// <summary>
        /// Queries a Windows Installer database to determine if one or more rows exist in the Property table.
        /// </summary>
        /// <param name="db">Database to query.</param>
        /// <param name="property">Property to examine.</param>
        /// <returns>True if query matches at least one result.</returns>
        private static bool HasProperty(Microsoft.Deployment.WindowsInstaller.Database db, string property)
        {
            try
            {
                return 0 < db.ExecuteQuery(PropertyQuery(property)).Count;
            }
            catch (Microsoft.Deployment.WindowsInstaller.InstallerException)
            {
            }

            return false;
        }

        /// <summary>
        /// Queries a Windows Installer database for a Property value.
        /// </summary>
        /// <param name="db">Database to query.</param>
        /// <param name="property">Property to examine.</param>
        /// <returns>String value for result or null if query doesn't match a single result.</returns>
        private static string GetProperty(Microsoft.Deployment.WindowsInstaller.Database db, string property)
        {
            try
            {
                return db.ExecuteScalar(PropertyQuery(property)).ToString();
            }
            catch (Microsoft.Deployment.WindowsInstaller.InstallerException)
            {
            }

            return null;
        }

        private static string PropertyQuery(string property)
        {
            // quick sanity check that we'll be creating a valid query...
            // TODO: Are there any other special characters we should be looking for?
            Debug.Assert(!property.Contains("'"));
            return String.Format(CultureInfo.InvariantCulture, ChainPackageInfo.PropertySqlFormat, property);
        }

        /// <summary>
        /// Queries a Windows Installer patch database for a Property value from the MsiPatchMetadata table.
        /// </summary>
        /// <param name="db">Database to query.</param>
        /// <param name="property">Property to examine.</param>
        /// <returns>String value for result or null if query doesn't match a single result.</returns>
        private static string GetPatchMetadataProperty(Microsoft.Deployment.WindowsInstaller.Database db, string property)
        {
            try
            {
                return db.ExecuteScalar(PatchMetadataPropertyQuery(property)).ToString();
            }
            catch (Microsoft.Deployment.WindowsInstaller.InstallerException)
            {
            }

            return null;
        }

        private static string PatchMetadataPropertyQuery(string property)
        {
            // quick sanity check that we'll be creating a valid query...
            // TODO: Are there any other special characters we should be looking for?
            Debug.Assert(!property.Contains("'"));
            return String.Format(CultureInfo.InvariantCulture, ChainPackageInfo.PatchMetadataFormat, property);
        }

        private static bool TargetsCode(XmlNode node)
        {
            if (null != node)
            {
                XmlAttribute attr = node.Attributes["Validate"];
                return null != attr && "true".Equals(attr.Value);
            }

            return false;
        }
    }
}
