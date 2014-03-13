//-------------------------------------------------------------------------------------------------
// <copyright file="PackageBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Package builder for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml.ApplicationModel;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;
    using Microsoft.Tools.WindowsInstallerXml.Extensions;
    using Microsoft.Tools.WindowsInstallerXml.Msi;

    /// <summary>
    /// Summary description for PackageBuilder.
    /// </summary>
    sealed public class PackageBuilder
    {
        // application values
        private Serialize.Name applicationName;
        private Serialize.Source applicationRoot;
        private Serialize.EntryPoint applicationEntry;
        private Wix.File applicationEntryFile;
        private Serialize.Icon icon;

        // package values
        private Serialize.Manufacturer manufacturerName;
        private Serialize.Description description;
        private Uri updateUrl;

        // previous package values
        private string previousPackagePath;

        // generated package values
        DirectoryHarvester directoryHarvester;
        UtilMutator utilMutator;
        UtilFinalizeHarvesterMutator finalMutator;

        private Wix.Directory applicationRootDirectory;
        private Guid upgradeCode;
        private Version version;

        // build error
        ClickThroughError buildError;

        /// <summary>
        /// Event fired any time a change is made to the package builder.
        /// </summary>
        public event PropertyChangedEventHandler Changed;

        /// <summary>
        /// Event fired any time progress is made during building.
        /// </summary>
        public event ProgressEventHandler Progress;

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Creates a new package builder
        /// </summary>
        public PackageBuilder()
        {
            this.applicationName = new Serialize.Name();
            this.applicationRoot = new Serialize.Source();
            this.applicationEntry = new Serialize.EntryPoint();
            this.icon = new Serialize.Icon();

            this.manufacturerName = new Serialize.Manufacturer();
            this.description = new Serialize.Description();

            this.upgradeCode = Guid.Empty;

            this.directoryHarvester = new DirectoryHarvester();
            this.utilMutator = new UtilMutator();
            this.utilMutator.GenerateGuids = true;
            this.utilMutator.SetUniqueIdentifiers = true;
            this.finalMutator = new UtilFinalizeHarvesterMutator();
        }

        /// <summary>
        /// Gets or sets the name of the manufacturer.
        /// </summary>
        public string ManufacturerName
        {
            get { return this.manufacturerName.Content; }
            set
            {
                this.manufacturerName.Content = value;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("ManufacturerName"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        public string ApplicationName
        {
            get { return this.applicationName.Content; }
            set
            {
                this.applicationName.Content = value;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("ApplicationName"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the description of the package.
        /// </summary>
        public string Description
        {
            get { return this.description.Content; }
            set
            {
                this.description.Content = value;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("Description"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the version of the package.
        /// </summary>
        public Version Version
        {
            get { return this.version; }
            set
            {
                this.version = value;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("Version"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the upgrade code for the package.
        /// </summary>
        public Guid UpgradeCode
        {
            get { return this.upgradeCode; }
            set
            {
                this.upgradeCode = value;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("UpgradeCode"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the RSS update feed for the package.
        /// </summary>
        public Uri UpdateUrl
        {
            get { return this.updateUrl; }
            set
            {
                this.updateUrl = value;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("UpdateUrl"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the path to the root of the application.
        /// </summary>
        public string ApplicationRoot
        {
            get { return this.applicationRoot.Content; }
            set
            {
                if (this.applicationRoot.Content != value)
                {
                    this.applicationRoot.Content = value;

                    this.applicationRootDirectory = null;
                    this.applicationEntry.Content = null;
                    this.applicationEntryFile = null;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("ApplicationRoot"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the directory object for the ApplicationRoot.
        /// </summary>
        public Wix.Directory GetApplicationRootDirectory()
        {
            if (null != this.applicationRoot && null == this.applicationRootDirectory)
            {
                this.applicationRootDirectory = this.directoryHarvester.HarvestDirectory(this.applicationRoot.Content, true);

                Wix.Wix wix = new Wix.Wix();
                Wix.Fragment fragment = new Wix.Fragment();
                wix.AddChild(fragment);
                fragment.AddChild(this.applicationRootDirectory);

                this.utilMutator.Mutate(wix);
                this.finalMutator.Mutate(wix);
            }

            return this.applicationRootDirectory;
        }

        /// <summary>
        /// Gets or sets the name of the executable that will act as the application's entry point.
        /// </summary>
        public string ApplicationEntry
        {
            get { return this.applicationEntry.Content; }
            set
            {
                if (this.applicationEntry.Content != value)
                {
                    this.applicationEntry.Content = value;
                    this.applicationEntryFile = null;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("ApplicationEntry"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the file object for the ApplicationEntry.
        /// </summary>
        public Wix.File GetApplicationEntryFile()
        {
            if (null != this.applicationRoot && null != this.applicationEntry.Content && null == this.applicationEntryFile)
            {
                Wix.Directory applicationDir = this.GetApplicationRootDirectory();
                this.applicationEntryFile = this.GetFile(this.applicationEntry.Content, this.applicationRootDirectory);
            }

            return this.applicationEntryFile;
        }

        /// <summary>
        /// Gets or sets the path to the previous package.
        /// </summary>
        public string PreviousPackage
        {
            get { return this.previousPackagePath; }
            set
            {
                this.previousPackagePath = value;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("PreviousPackage"));
                }
            }

        }

        /// <summary>
        /// Builds a setup package to the specified output path.
        /// </summary>
        /// <param name="outputPath">Location to build the setup package to.</param>
        public bool Build(string outputPath)
        {
            return this.Build(outputPath, null);
        }

        /// <summary>
        /// Builds a setup package to the specified output path.
        /// </summary>
        /// <param name="outputPath">Location to build the setup package to.</param>
        /// <param name="outputSourcePath">Optional path where the package's .wxs file will be written.</param>
        public bool Build(string outputPath, string outputSourcePath)
        {
            this.buildError = null; // clear out any previous errors

            int currentProgress = 0;
            int totalProgress = 7;

            // calculate the upper progress
            if (outputSourcePath != null)
            {
                ++totalProgress;
            }
            if (this.previousPackagePath != null)
            {
                ++totalProgress;
            }

            this.VerifyRequiredInformation();

            if (!this.OnProgress(currentProgress++, totalProgress, "Initialized package builder..."))
            {
                return false;
            }

            // Calculate where everything is going
            string localSetupExe = outputPath;
            string localSetupFeed = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileName(this.updateUrl.AbsolutePath));

            Uri urlSetupExe = new Uri(this.updateUrl, Path.GetFileName(localSetupExe));
            Uri urlSetupFeed = new Uri(this.updateUrl, Path.GetFileName(localSetupFeed));

            Guid previousUpgradeCode = Guid.Empty;
            Version previousVersion = null;
            Uri previousSetupFeed = null;

            // if a previous package was provided, go read the key information out of it now
            if (this.previousPackagePath != null)
            {
                if (!this.OnProgress(currentProgress++, totalProgress, "Reading previous package..."))
                {
                    return false;
                }

                this.ReadPreviousPackage(this.previousPackagePath, out previousUpgradeCode, out previousVersion, out previousSetupFeed);
            }

            //
            // if a upgrade code and/or version has not been specified use one
            // from the previous package or create new.
            //
            if (this.upgradeCode == Guid.Empty)
            {
                if (previousUpgradeCode == Guid.Empty)
                {
                    this.upgradeCode = Guid.NewGuid();
                }
                else
                {
                    this.upgradeCode = previousUpgradeCode;
                }
            }

            if (this.version == null)
            {
                if (previousVersion == null)
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(this.applicationRoot.Content, this.applicationEntry.Content));
                    this.version = new Version(fileVersionInfo.FileVersion);
                }
                else
                {
                    this.version = previousVersion;
                }
            }

            // verify that new data is okay when compared to previous package
            if (previousUpgradeCode != Guid.Empty && previousUpgradeCode != this.upgradeCode)
            {
                this.OnMessage(ClickThroughErrors.UpgradeCodeChanged(previousUpgradeCode, this.upgradeCode));
            }
            if (previousVersion != null && previousVersion >= this.version)
            {
                this.OnMessage(ClickThroughErrors.NewVersionIsNotGreater(previousVersion, this.version));
            }

            if (this.buildError != null)
            {
                throw new InvalidOperationException(String.Format(this.buildError.ResourceManager.GetString(this.buildError.ResourceName), this.buildError.MessageArgs));
            }
            else if (!this.OnProgress(currentProgress++, totalProgress, "Processing package information..."))
            {
                return false;
            }

            // Product information
            Application application = new Application();
            application.Product.Id = Guid.NewGuid().ToString();
            application.Product.Language = "1033";
            application.Product.Manufacturer = this.manufacturerName.Content;
            application.Product.Name = this.applicationName.Content;
            application.Package.Description = this.description.Content;
            application.Product.UpgradeCode = this.upgradeCode.ToString();
            application.Product.Version = this.version.ToString();

            Wix.WixVariable variable = new Wix.WixVariable();
            variable = new Wix.WixVariable();
            variable.Id = "ProductName";
            variable.Value = application.Product.Name;
            application.Product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ProductCode";
            variable.Value = application.Product.Id;
            application.Product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ProductVersion";
            variable.Value = application.Product.Version;
            application.Product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ShortcutFileId";
            variable.Value = "todoFileIdHere";
            application.Product.AddChild(variable);

            // Upgrade logic
            Wix.Upgrade upgrade = new Wix.Upgrade();
            upgrade.Id = application.Product.UpgradeCode;
            application.Product.AddChild(upgrade);

            Wix.UpgradeVersion minUpgrade = new Wix.UpgradeVersion();
            minUpgrade.Minimum = application.Product.Version;
            minUpgrade.OnlyDetect = Wix.YesNoType.yes;
            minUpgrade.Property = "NEWERVERSIONDETECTED";
            upgrade.AddChild(minUpgrade);

            Wix.UpgradeVersion maxUpgrade = new Wix.UpgradeVersion();
            maxUpgrade.Maximum = application.Product.Version;
            maxUpgrade.IncludeMaximum = Wix.YesNoType.no;
            maxUpgrade.Property = "OLDERVERSIONBEINGUPGRADED";
            upgrade.AddChild(maxUpgrade);

            // Update Feed
            Wix.Property property = new Wix.Property();
            property.Id = "ARPURLUPDATEINFO";
            property.Value = urlSetupFeed.AbsoluteUri;
            application.Product.AddChild(property);

#if false
            // Directory tree
            Wix.DirectoryRef applicationCacheRef = new Wix.DirectoryRef();
            applicationCacheRef.Id = "ApplicationsCacheFolder";
            application.Product.AddChild(applicationCacheRef);
#endif

            Wix.DirectoryRef directoryRef = new Wix.DirectoryRef();
            directoryRef.Id = "ApplicationsFolder";
            application.Product.AddChild(directoryRef);

            this.applicationRootDirectory.Name = String.Concat(application.Product.UpgradeCode, "v", application.Product.Version);
            directoryRef.AddChild(this.applicationRootDirectory);

#if false
            // System registry keys
            Wix.Component registryComponent = new Wix.Component();
            registryComponent.Id = "SystemVersionRegistryKeyComponent";
            registryComponent.Guid = Guid.NewGuid().ToString();
            directoryRef.AddChild(registryComponent);

            Wix.Registry productRegKey = new Wix.Registry();
            productRegKey.Root = Wix.RegistryRootType.HKCU;
            productRegKey.Key = @"Software\WiX\ClickThrough\Applications\[UpgradeCode]";
            productRegKey.Action = Wix.Registry.ActionType.createKeyAndRemoveKeyOnUninstall; 
            registryComponent.AddChild(productRegKey);

            Wix.Registry versionRegKey = new Wix.Registry();
            versionRegKey.Name = "Version";
            versionRegKey.Type = Wix.Registry.TypeType.@string;
            versionRegKey.Value = "[ProductVersion]";
            productRegKey.AddChild(versionRegKey);

            Wix.Registry sourceRegKey = new Wix.Registry();
            sourceRegKey.Name = "UpdateInfoSource";
            sourceRegKey.Type = Wix.Registry.TypeType.@string;
            sourceRegKey.Value = "[ARPURLUPDATEINFO]";
            productRegKey.AddChild(sourceRegKey);

            // Shortcut
            Wix.DirectoryRef programMenuRef = new Wix.DirectoryRef();
            programMenuRef.Id = "ProgramMenuFolder";
            Wix.Directory shortcutsDirectory = new Wix.Directory();
            shortcutsDirectory.Id = "ThisAppShortcuts";
            shortcutsDirectory.LongName = application.Product.Name;
            shortcutsDirectory.Name = "AppSCDir";
            programMenuRef.AddChild(shortcutsDirectory);
            application.Product.AddChild(programMenuRef);

            Wix.Component shortcutsComponent = new Wix.Component();
            shortcutsComponent.Id = "ThisApplicationShortcutComponent";
            shortcutsComponent.Guid = Guid.NewGuid().ToString();
            shortcutsComponent.KeyPath = Wix.YesNoType.yes;
            shortcutsDirectory.AddChild(shortcutsComponent);

            Wix.CreateFolder shortcutsCreateFolder = new Wix.CreateFolder();
            shortcutsComponent.AddChild(shortcutsCreateFolder);

            Wix.Shortcut shortcut = this.GetShortcut(this.applicationEntry.Content, rootDirectory, shortcutsDirectory);
            shortcutsComponent.AddChild(shortcut);

            // Remove cached MSI file.
            Wix.Component removeComponent = new Wix.Component();
            removeComponent.Id = "ThisApplicationRemoveComponent";
            removeComponent.Guid = Guid.NewGuid().ToString();
            removeComponent.KeyPath = Wix.YesNoType.yes;
            applicationCacheRef.AddChild(removeComponent);

            Wix.RemoveFile cacheRemoveFile = new Wix.RemoveFile();
            cacheRemoveFile.Id = "ThisApplicationRemoveCachedMsi";
            cacheRemoveFile.Directory = "ApplicationsCacheFolder";
            cacheRemoveFile.Name = "unknown.msi";
            cacheRemoveFile.LongName = String.Concat("{", application.Product.Id.ToUpper(CultureInfo.InvariantCulture), "}v", application.Version.ToString(), ".msi");
            cacheRemoveFile.On = Wix.RemoveFile.OnType.uninstall;
            removeComponent.AddChild(cacheRemoveFile);

            Wix.RemoveFile cacheRemoveFolder = new Wix.RemoveFile();
            cacheRemoveFolder.Id = "ThisApplicationRemoveCacheFolder";
            cacheRemoveFolder.Directory = "ApplicationsCacheFolder";
            cacheRemoveFolder.On = Wix.RemoveFile.OnType.uninstall;
            removeComponent.AddChild(cacheRemoveFolder);

            Wix.RemoveFile applicationRemoveFolder = new Wix.RemoveFile();
            applicationRemoveFolder.Id = "ThisApplicationRemoveApplicationsFolder";
            applicationRemoveFolder.Directory = "ApplicationsFolder";
            applicationRemoveFolder.On = Wix.RemoveFile.OnType.uninstall;
            removeComponent.AddChild(applicationRemoveFolder);
#endif
            // Feature tree
            Wix.FeatureRef applicationFeatureRef = new Wix.FeatureRef();
            applicationFeatureRef.Id = "ApplicationFeature";
            application.Product.AddChild(applicationFeatureRef);

#if false
            Wix.Feature applicationFeature = new Wix.Feature();
            applicationFeature.Id = "ApplicationFeature";
            applicationFeature.Display = "expand";
            applicationFeature.Level = 1;
            applicationFeature.Absent = Wix.Feature.AbsentType.disallow;
            applicationFeature.AllowAdvertise = Wix.Feature.AllowAdvertiseType.yes;
            applicationFeature.InstallDefault = Wix.Feature.InstallDefaultType.local;
            applicationFeature.TypicalDefault = Wix.Feature.TypicalDefaultType.install;
            application.Product.AddChild(applicationFeature);

            Wix.ComponentRef shortcutsComponentRef = new Wix.ComponentRef();
            shortcutsComponentRef.Id = shortcutsComponent.Id;
            applicationFeature.AddChild(shortcutsComponentRef);

            Wix.ComponentRef removeComponentRef = new Wix.ComponentRef();
            removeComponentRef.Id = removeComponent.Id;
            applicationFeature.AddChild(removeComponentRef);
#endif

            Wix.ComponentRef[] componentRefs = this.GetComponentRefs(this.applicationRootDirectory);
            foreach (Wix.ComponentRef componentRef in componentRefs)
            {
                applicationFeatureRef.AddChild(componentRef);
            }

            if (!this.OnProgress(currentProgress++, totalProgress, "Serializing package information into XML..."))
            {
                return false;
            }

            // serialize to an xml string
            string xml;
            using (StringWriter sw = new StringWriter())
            {
                XmlTextWriter writer = null;
                try
                {
                    writer = new XmlTextWriter(sw);

                    application.WixRoot.OutputXml(writer);

                    xml = sw.ToString();
                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Close();
                    }
                }
            }

            // load the xml into a document
            XmlDocument sourceDoc = new XmlDocument();
            sourceDoc.LoadXml(xml);

            if (outputSourcePath != null)
            {
                if (!this.OnProgress(currentProgress++, totalProgress, "Saving .wxs file..."))
                {
                    return false;
                }

                sourceDoc.Save(outputSourcePath);
            }

            // generate the MSI, create the setup.exe, and generate the RSS feed.
            string outputMsi = null;
            try
            {
                outputMsi = Path.GetTempFileName();

                if (!this.OnProgress(currentProgress++, totalProgress, "Generating .msi file..."))
                {
                    return false;
                }

                this.GenerateMsi(sourceDoc, outputMsi);
                if (this.buildError != null)
                {
                    throw new InvalidOperationException(String.Format(this.buildError.ResourceManager.GetString(this.buildError.ResourceName), this.buildError.MessageArgs));
                }

                string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                if (!this.OnProgress(currentProgress++, totalProgress, "Generating setup bootstrapper..."))
                {
                    return false;
                }

                /*
                NativeMethods.CREATE_SETUP_PACKAGE[] createSetup = new Microsoft.Tools.WindowsInstallerXml.ClickThrough.NativeMethods.CREATE_SETUP_PACKAGE[1];
                createSetup[0].fPrivileged = false;
                createSetup[0].fCache = true;
                createSetup[0].wzSourcePath = outputMsi;

                int hr = NativeMethods.CreateSetup(Path.Combine(assemblyPath, "setup.exe"), createSetup, createSetup.Length, localSetupExe);
                */
                int hr = NativeMethods.CreateSimpleSetup(Path.Combine(assemblyPath, "setup.exe"), outputMsi, localSetupExe);
                if (hr != 0)
                {
                    this.OnMessage(ClickThroughErrors.FailedSetupExeCreation(Path.Combine(assemblyPath, "setup.exe"), localSetupExe));
                }

                if (!this.OnProgress(currentProgress++, totalProgress, "Generating update feed..."))
                {
                    return false;
                }
                this.GenerateRssFeed(localSetupFeed, localSetupExe, urlSetupExe, application.Product.Id, application.Product.UpgradeCode, application.Product.Version);
            }
            finally
            {
                this.OnProgress(currentProgress++, totalProgress, "Cleaning up...");
                if (outputMsi != null)
                {
                    File.Delete(outputMsi);
                }
            }

            if (this.buildError != null)
            {
                throw new InvalidOperationException(String.Format(this.buildError.ResourceManager.GetString(this.buildError.ResourceName), this.buildError.MessageArgs));
            }
            else if (!this.OnProgress(currentProgress++, totalProgress, "Package build complete."))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loads the package builder data from disk.
        /// </summary>
        /// <param name="outputPath">Path to load the output builder information from.</param>
        public void Load(string outputPath)
        {
        }

        /// <summary>
        /// Saves the package builder data to disk.
        /// </summary>
        /// <param name="outputPath">Path to save the output builder to.</param>
        public void Save(string outputPath)
        {
            Serialize.ClickThrough ct = new Serialize.ClickThrough();

            // package serialization
            Serialize.Package package = new Serialize.Package();
            package.AddChild(this.manufacturerName);

            Serialize.Feed feed = new Serialize.Feed();
            if (this.updateUrl != null)
            {
                feed.Content = this.updateUrl.AbsoluteUri;
            }
            package.AddChild(feed);

            if (this.description.Content != null && this.description.Content.Length > 0)
            {
                package.AddChild(this.description);
            }
            ct.AddChild(package);

            // application serialization
            Serialize.Application application = new Serialize.Application();
            application.AddChild(this.applicationName);
            application.AddChild(this.applicationRoot);
            application.AddChild(this.applicationEntry);

            if (this.icon.Content != null && this.icon.Content.Length > 0)
            {
                application.AddChild(icon);
            }
            ct.AddChild(application);

            Serialize.PreviousPackage previousPackage = new Serialize.PreviousPackage();
            previousPackage.Content = this.previousPackagePath;
            ct.AddChild(previousPackage);

            // serialize the data to disk
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                XmlTextWriter writer = new XmlTextWriter(sw);
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 4;

                ct.OutputXml(writer);
            }
        }

        /// <summary>
        /// Ensures all of the required properties were populated before 
        /// trying to execute any operations on the package builder.
        /// </summary>
        private void VerifyRequiredInformation()
        {
            if (null == this.updateUrl)
            {
                throw new InvalidOperationException("UpdateUrl must be specified before saving the package builder.");
            }
            else if (null == this.manufacturerName.Content)
            {
                throw new InvalidOperationException("ManufacturerName must be specified before saving the package builder.");
            }
            else if (null == this.applicationName.Content)
            {
                throw new InvalidOperationException("ApplicationName must be specified before saving the package builder.");
            }
            else if (null == this.applicationEntry.Content)
            {
                throw new InvalidOperationException("ApplicationEntry must be specified before saving the package builder.");
            }
            else if (null == this.applicationRoot.Content)
            {
                throw new InvalidOperationException("ApplicationRoot must be specified before saving the package builder.");
            }
        }

        /// <summary>
        /// Opens the previous package (.msi or .exe) and reads the interesting information from it.
        /// </summary>
        /// <param name="filePath">Path to the package.</param>
        /// <param name="previousUpgradeCode">Upgrade code of the package.</param>
        /// <param name="previousVersion">Version of the package.</param>
        /// <param name="previousUri">Update URL of the package.</param>
        private void ReadPreviousPackage(string filePath, out Guid previousUpgradeCode, out Version previousVersion, out Uri previousUri)
        {
            // assume nothing about the previous package
            previousUpgradeCode = Guid.Empty;
            previousVersion = null;
            previousUri = null;

            string tempFileName = null;
            Database db = null;
            View view = null;

            try
            {
                string msiPath = filePath; // assume the file path is the path to the MSI

                // if the extension on the file path is ".exe" try to extract the MSI out of it
                if (String.Compare(Path.GetExtension(filePath), ".exe", true) == 0)
                {
                    tempFileName = Path.GetTempFileName();

                    Process process = new Process();
                    process.StartInfo.FileName = filePath;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Arguments = String.Concat("-out ", tempFileName);

                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new ApplicationException(String.Concat("Failed to extract MSI from ", process.StartInfo.FileName));
                    }

                    msiPath = tempFileName; // the MSI is now at the temp filename location
                }

                db = new Database(msiPath, OpenDatabase.ReadOnly);
                view = db.OpenView("SELECT `Value` FROM `Property` WHERE `Property`=?");

                string propertyValue;

                // get the UpgradeCode
                propertyValue = this.FetchPropertyValue(view, "UpgradeCode");
                if (propertyValue != null)
                {
                    previousUpgradeCode = new Guid(propertyValue);
                }

                // get the Version
                propertyValue = this.FetchPropertyValue(view, "ProductVersion");
                if (propertyValue != null)
                {
                    previousVersion = new Version(propertyValue);
                }

                // get the Update URL
                propertyValue = this.FetchPropertyValue(view, "ARPURLUPDATEINFO");
                if (propertyValue != null)
                {
                    previousUri = new Uri(propertyValue);
                }
            }
            finally
            {
                if (view != null)
                {
                    view.Close();
                }

                if (db != null)
                {
                    db.Close();
                }

                if (tempFileName != null)
                {
                    File.Delete(tempFileName);
                }
            }
        }

        /// <summary>
        /// Executes the view to fetch the value for a specified property.
        /// </summary>
        /// <param name="view">View that is already open on the Property table.</param>
        /// <param name="propertyName">Name of the property to get the value for.</param>
        /// <returns>String value of the property.</returns>
        private string FetchPropertyValue(View view, string propertyName)
        {
            string propertyValue = null;
            using (Record recIn = new Record(1))
            {
                recIn[1] = propertyName;
                view.Execute(recIn);

                Record recOut = null;
                try
                {
                    if ((recOut = view.Fetch()) != null)
                    {
                        propertyValue = recOut[1];
                    }
                }
                finally
                {
                    if (recOut != null)
                    {
                        recOut.Close();
                    }
                }
            }

            return propertyValue;
        }

        /// <summary>
        /// Returns a ComponentRef for each Component in the Directory tree.
        /// </summary>
        /// <param name="directory">The root Directory of the components.</param>
        private Wix.ComponentRef[] GetComponentRefs(Wix.Directory directory)
        {
            ArrayList componentRefs = new ArrayList();

            foreach (Wix.ISchemaElement element in directory.Children)
            {
                if (element is Wix.Component)
                {
                    Wix.Component component = (Wix.Component)element;

                    Wix.ComponentRef componentRef = new Wix.ComponentRef();
                    componentRef.Id = component.Id;

                    componentRefs.Add(componentRef);
                }
                else if (element is Wix.Directory)
                {
                    componentRefs.AddRange(this.GetComponentRefs((Wix.Directory)element));
                }
            }

            return (Wix.ComponentRef[])componentRefs.ToArray(typeof(Wix.ComponentRef));
        }

        /// <summary>
        /// Returns the File matching the relative path in the Directory tree.
        /// </summary>
        /// <param name="relativePath">Relative path to the file to find in the directory.</param>
        /// <param name="directory">Directory tree to search for relative path in.</param>
        private Wix.File GetFile(string relativePath, Wix.Directory rootDirectory)
        {
            IEnumerable enumerable = rootDirectory.Children;
            string[] directory = relativePath.Split(Path.DirectorySeparatorChar);

            for (int i = 0; i < directory.Length; ++i)
            {
                bool found = false;

                if (i < directory.Length - 1)
                {
                    foreach (Wix.ISchemaElement element in enumerable)
                    {
                        Wix.Directory childDirectory = element as Wix.Directory;
                        if (null != childDirectory && directory[i] == childDirectory.Name)
                        {
                            enumerable = childDirectory.Children;

                            found = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (Wix.ISchemaElement element in enumerable)
                    {
                        Wix.Component component = element as Wix.Component;
                        if (null != component)
                        {
                            foreach (Wix.ISchemaElement child in component.Children)
                            {
                                Wix.File file = child as Wix.File;
                                if (null != file && directory[i] == file.Name)
                                {
                                    return file;
                                }
                            }
                        }
                    }
                }

                if (!found)
                {
                    throw new ApplicationException("Did not find file name");
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a Shortcut for each Component in the Directory tree.
        /// </summary>
        /// <param name="">The root Directory of the components.</param>
        private Wix.Shortcut GetShortcut(string relativePath, Wix.Directory directory, Wix.Directory shortcutDirectory)
        {
            Wix.Shortcut shortcut = null;
            IEnumerable enumerable = directory.Children;
            string[] dir = relativePath.Split(Path.DirectorySeparatorChar);

            for (int i = 0; i < dir.Length; ++i)
            {
                bool found = false;

                if (i < dir.Length - 1)
                {
                    foreach (Wix.ISchemaElement element in enumerable)
                    {
                        if (element is Wix.Directory)
                        {
                            Wix.Directory dirx = (Wix.Directory)element;
                            if (dir[i] == dirx.LongName)
                            {
                                enumerable = dirx.Children;

                                found = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (Wix.ISchemaElement element in enumerable)
                    {
                        if (element is Wix.Component)
                        {
                            enumerable = ((Wix.Component)element).Children;
                            foreach (Wix.ISchemaElement elementx in enumerable)
                            {
                                if (elementx is Wix.File)
                                {
                                    Wix.File fil = (Wix.File)elementx;
                                    if (dir[i] == fil.LongName)
                                    {
                                        shortcut = new Wix.Shortcut();

                                        shortcut.Id = String.Concat(fil.Id, "Shortcut");
                                        shortcut.Directory = shortcutDirectory.Id;
                                        shortcut.Target = "[!SystemApplicationUpdateExeFile]";
                                        shortcut.Name = "shortcu1";
                                        shortcut.LongName = Path.GetFileNameWithoutExtension(fil.LongName);
                                        shortcut.Arguments = String.Format(CultureInfo.InvariantCulture, "-ac [UpgradeCode] -cl \"[#{0}]\"", fil.Id);

                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }

                if (!found)
                {
                    throw new ApplicationException("did not find file name");
                }
            }

            return shortcut;
        }

        /// <summary>
        /// Generates the appropriate MSI file for the package.
        /// </summary>
        /// <param name="sourceDoc">WiX document to create MSI from.</param>
        /// <param name="filePath">File path for the RSS feed.</param>
        private void GenerateMsi(XmlDocument sourceDoc, string filePath)
        {
            // Compile
            Compiler compiler = new Compiler();
            compiler.Message += new MessageEventHandler(this.MessageHandler);

            Intermediate intermediate = compiler.Compile(sourceDoc);
            if (intermediate == null)
            {
                return;
            }

            // locate the applib.wixlib
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string applibPath = Path.Combine(assemblyPath, "applib.wixlib");

            if (!File.Exists(applibPath))
            {
                this.OnMessage(ClickThroughErrors.CannotLoadApplib(applibPath));
                return;
            }

            WixVariableResolver wixVariableResolver = new WixVariableResolver();
            wixVariableResolver.Message += new MessageEventHandler(this.MessageHandler);

            // create the linker
            Linker linker = new Linker();
            linker.Message += new MessageEventHandler(this.MessageHandler);
            linker.WixVariableResolver = wixVariableResolver;

            // load applib.wixlib
            Library lowImpactAppLib = Library.Load(applibPath, linker.TableDefinitions, false, false);
            SectionCollection sections = new SectionCollection();
            sections.AddRange(intermediate.Sections);
            sections.AddRange(lowImpactAppLib.Sections);

            // Link
            Output output = linker.Link(sections);
            if (output == null)
            {
                return;
            }

            Table components = output.Tables["Component"];
            foreach (Row row in components.Rows)
            {
                switch ((string)row[0])
                {
                    case "ThisApplicationVersionRegistryKeyComponent":
                        row[1] = String.Concat("{", Guid.NewGuid().ToString().ToUpper(), "}");
                        break;
                    case "ThisApplicationCacheFolderComponent":
                        row[1] = String.Concat("{", Guid.NewGuid().ToString().ToUpper(), "}");
                        break;
                    case "ThisApplicationShortcutComponent":
                        row[1] = String.Concat("{", Guid.NewGuid().ToString().ToUpper(), "}");
                        break;
                }
            }

            // Bind
            Binder binder = new Binder();
            binder.Extension = new BinderExtension();
            binder.Extension.SourcePaths.Add(Path.GetDirectoryName(filePath));
            binder.Extension.SourcePaths.Add(assemblyPath);
            binder.WixVariableResolver = wixVariableResolver;
            binder.Message += new MessageEventHandler(this.MessageHandler);
            binder.Bind(output, filePath);

            return;
        }

        /// <summary>
        /// Generates the appropriate RSS Feed for the package.
        /// </summary>
        /// <param name="filePath">File path for the RSS feed.</param>
        /// <param name="enclosurePath">Path to the file referred to by the enclosure.</param>
        /// <param name="enclosureUrl">Final URL expected for the enclosure.</param>
        private void GenerateRssFeed(string filePath, string enclosurePath, Uri enclosureUrl, string productCode, string upgradeCode, string version)
        {
            XmlTextWriter writer = null;
            DateTime moment = DateTime.UtcNow;
            string formattedMoment = String.Format("{0:ddd, d MMM yyyy hh:mm:ss} GMT", moment);

            try
            {
                FileInfo enclosureFileInfo = new FileInfo(enclosurePath);

                writer = new XmlTextWriter(filePath, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartElement("rss"); // <rss>
                writer.WriteAttributeString("version", "2.0");
                writer.WriteAttributeString("xmlns", "msi", null, "http://schemas.microsoft.com/wix/2005/09/rss/msi");
                writer.WriteStartElement("channel"); // <channel>
                writer.WriteElementString("title", String.Concat(this.manufacturerName.Content, "'s ", this.applicationName.Content));
                if (null != this.description)
                {
                    writer.WriteElementString("description", this.description.Content);
                }
                writer.WriteElementString("generator", "WiX ClickThrough");
                writer.WriteElementString("lastBuildDate", formattedMoment);

                writer.WriteStartElement("item"); // <item>
                writer.WriteStartElement("guid"); // <guid>
                writer.WriteAttributeString("isPermaLink", "false");
                writer.WriteString(String.Format("urn:msi:{0}/{1}", this.upgradeCode, this.version));
                writer.WriteEndElement(); // </guid>
                writer.WriteElementString("title", String.Concat(this.applicationName.Content, " v", this.version));
                writer.WriteElementString("pubDate", formattedMoment);

                writer.WriteElementString("productCode", "http://schemas.microsoft.com/wix/2005/09/rss/msi", productCode);
                writer.WriteElementString("upgradeCode", "http://schemas.microsoft.com/wix/2005/09/rss/msi", upgradeCode);
                writer.WriteElementString("version", "http://schemas.microsoft.com/wix/2005/09/rss/msi", version);

                writer.WriteStartElement("enclosure"); //<enclosure>
                writer.WriteAttributeString("url", enclosureUrl.AbsoluteUri);
                writer.WriteAttributeString("length", enclosureFileInfo.Length.ToString());
                writer.WriteAttributeString("type", "application/octet-stream");
                writer.WriteEndElement(); // </enclosure>

                writer.WriteEndElement(); // </item>

                writer.WriteEndElement(); // </channel>
                writer.WriteEndElement(); // </rss>
            }
            finally
            {
                if (null != writer)
                {
                    writer.Close();
                }
            }
        }

        /// <summary>
        /// Display a package creation message to the user.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="mea">Arguments for the message event.</param>
        private void MessageHandler(object sender, MessageEventArgs mea)
        {
            if (mea is WixErrorEventArgs)
            {
                string message = String.Format(mea.ResourceManager.GetString(mea.ResourceName), mea.MessageArgs);
                this.OnMessage(ClickThroughErrors.WixError(message));
            }
            else if (mea is WixWarningEventArgs)
            {
                string message = String.Format(mea.ResourceManager.GetString(mea.ResourceName), mea.MessageArgs);
                this.OnMessage(ClickThroughWarnings.WixWarning(message));
            }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        private void OnMessage(MessageEventArgs mea)
        {
            if (this.buildError == null && mea is ClickThroughError)
            {
                this.buildError = mea as ClickThroughError;
            }

            if (this.Message != null)
            {
                this.Message(this, mea);
            }
        }

        /// <summary>
        /// Sends a proress message to the client
        /// </summary>
        /// <param name="current">Current progress.</param>
        /// <param name="upperBound">Upper bound of the progress bar.</param>
        /// <param name="message">Progress message.</param>
        /// <returns>True if the client wants to continue, false to cancel progress.</returns>
        private bool OnProgress(int current, int upperBound, string message)
        {
            bool cancel = false;

            if (this.Progress != null)
            {
                ProgressEventArgs e = new ProgressEventArgs(current, upperBound, message);
                this.Progress(this, e);

                cancel = e.Cancel;
            }

            return !cancel;
        }
    }
}
