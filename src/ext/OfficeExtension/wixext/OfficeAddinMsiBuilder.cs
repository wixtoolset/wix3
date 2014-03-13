// <copyright file="OfficeAddinMsiBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Office addin MSI builder for ClickThrough.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml.Tools;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Creates MSI files for the ClickThrough for Office Addins.
    /// </summary>
    sealed internal class OfficeAddinMsiBuilder : WixExtension
    {
        private FabricatorCore core;

        private string description;
        private string language;
        private string manufacturer;
        private string name;
        private Guid previousUpgradeCode;
        private Version previousVersion;
        private Uri previousUri;
        private Guid productCode;
        private bool productCodeSet;
        private Guid upgradeCode;
        private Uri updateUrl;
        private Version version;

        private string entryFileRelativePath;
        private Guid shimGuid;
        private string shimPath;
        private Guid shimClsid;
        private string shimProgid;
        private string source;
        private Wix.Directory rootDirectory;
        private ArrayList extendedOfficeApplications;

        /// <summary>
        /// Creates MSI files for the ClickThrough for Office Addins.
        /// </summary>
        /// <param name="core">Core build object for message handling.</param>
        public OfficeAddinMsiBuilder(FabricatorCore core)
        {
            this.core = core;
            this.language = "1033";
            this.productCode = Guid.Empty;
            this.upgradeCode = Guid.Empty;
            this.previousUpgradeCode = Guid.Empty;
            this.shimGuid = Guid.Empty;
            this.shimClsid = Guid.Empty;

            this.extendedOfficeApplications = new ArrayList();
        }

        /// <summary>
        /// Gets and sets the description of the MSI.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        /// <summary>
        /// Gets the Office applications extended.
        /// </summary>
        public ArrayList ExtendedOfficeApplications
        {
            get { return this.extendedOfficeApplications; }
        }

        /// <summary>
        /// Gets and sets the language of the MSI.
        /// </summary>
        public string Language
        {
            get { return this.language; }
            set { this.language = value; }
        }

        /// <summary>
        /// Gets and sets the manufacturer of the MSI.
        /// </summary>
        public string Manufacturer
        {
            get { return this.manufacturer; }
            set { this.manufacturer = value; }
        }

        /// <summary>
        /// Gets and sets the name inside the MSI.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        /// <summary>
        /// Gets the product code of the MSI.
        /// </summary>
        /// <remarks>This value is only valid after the Build() method is called.</remarks>
        public Guid ProductCode
        {
            get
            {
                return this.productCode;
            }

            set
            {
                if (this.productCode != value)
                {
                    if (value == Guid.Empty)
                    {
                        this.productCode = Guid.Empty;
                        this.productCodeSet = false;
                    }
                    else
                    {
                        this.productCode = value;
                        this.productCodeSet = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the upgrade code for the MSI.
        /// </summary>
        public Guid UpgradeCode
        {
            get { return this.upgradeCode; }
            set { this.upgradeCode = value; }
        }

        /// <summary>
        /// Gets and sets the update URL of the MSI.
        /// </summary>
        public Uri UpdateUrl
        {
            get { return this.updateUrl; }
            set { this.updateUrl = value; }
        }

        /// <summary>
        /// Gets and sets the version of the MSI.
        /// </summary>
        public Version Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// Gets and sets the relative path to the entry file for the MSI.
        /// </summary>
        public string EntryFileRelativePath
        {
            get { return this.entryFileRelativePath; }
            set { this.entryFileRelativePath = value; }
        }

        /// <summary>
        /// Gets and sets the relative path to the shim for the MSI.
        /// </summary>
        public string ShimPath
        {
            get { return this.shimPath; }
            set { this.shimPath = value; }
        }

        /// <summary>
        /// Gets and sets the CLSID for the shim.
        /// </summary>
        public Guid ShimClsid
        {
            get { return this.shimClsid; }
            set { this.shimClsid = value; }
        }

        /// <summary>
        /// Gets and sets the prog id for the shim.
        /// </summary>
        public string ShimProgid
        {
            get { return this.shimProgid; }
            set { this.shimProgid = value; }
        }

        /// <summary>
        /// Gets and sets the root path for the havesting into the MSI.
        /// </summary>
        public string Source
        {
            get
            {
                return this.source;
            }

            set
            {
                if (value != this.source)
                {
                    this.source = value;
                    if (null != this.rootDirectory)
                    {
                        this.rootDirectory = null;
                    }
                }
            }
        }

        /// <summary>
        /// Creates MSI.
        /// </summary>
        /// <param name="outputFile">Path to buid MSI file to.</param>
        /// <param name="outputSourceFile">Optional path to save source .wxs file to.</param>
        /// <returns>True if build succeeds, false if any failure occurs.</returns>
        public bool Build(string outputFile, string outputSourceFile)
        {
            if (!this.productCodeSet)
            {
                this.productCode = Guid.Empty;
            }

            // If the upgrade code has not been specified use one from the previous package or create new.
            if (this.upgradeCode == Guid.Empty)
            {
                if (this.previousUpgradeCode == Guid.Empty)
                {
                    this.upgradeCode = Guid.NewGuid();
                }
                else
                {
                    this.upgradeCode = this.previousUpgradeCode;
                }
            }

            // If the version has not be specified, use the version from the entry file.
            if (this.version == null)
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(this.source, this.entryFileRelativePath));
                this.version = new Version(fileVersionInfo.FileVersion);
            }

            // Verify that new data is okay when compared to previous package.
            if (this.previousUpgradeCode != Guid.Empty && this.previousUpgradeCode != this.upgradeCode)
            {
                this.core.OnMessage(OfficeErrors.UpgradeCodeChanged(this.previousUpgradeCode, this.upgradeCode));
                return false;
            }

            if (this.previousVersion != null && this.previousVersion >= this.version)
            {
                this.core.OnMessage(OfficeErrors.NewVersionIsNotGreater(this.previousVersion, this.version));
                return false;
            }

            // Generate the .wxs file and save it if provided an output file path.
            XmlDocument sourceDoc = this.GenerateSourceFile();
            if (null == sourceDoc)
            {
                return false;
            }

            if (null != outputSourceFile)
            {
                sourceDoc.Save(outputSourceFile);
            }

            // Generate the MSI.
            if (null != outputFile)
            {
                try
                {
                    if (!this.GenerateMsi(sourceDoc, outputFile))
                    {
                        return false;
                    }

                    outputFile = null;
                }
                finally
                {
                    if (null != outputFile)
                    {
                        File.Delete(outputFile);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the directory object for the provided application root path.
        /// </summary>
        /// <param name="recalculate">Flag to recalculate root directory.</param>
        /// <returns>Directory harvested from root.</returns>
        public Wix.Directory GetRootDirectory(bool recalculate)
        {
            if (null == this.source)
            {
                throw new ArgumentNullException("RootPath");
            }

            if (recalculate || null == this.rootDirectory)
            {
                DirectoryHarvester directoryHarvester = new DirectoryHarvester();
                this.rootDirectory = directoryHarvester.HarvestDirectory(this.source, true);

                Wix.Wix wix = new Wix.Wix();
                Wix.Fragment fragment = new Wix.Fragment();
                wix.AddChild(fragment);
                fragment.AddChild(this.rootDirectory);

                UtilMutator utilMutator = new UtilMutator();
                utilMutator.GenerateGuids = true;
                utilMutator.SetUniqueIdentifiers = true;
                utilMutator.Mutate(wix);

                UtilFinalizeHarvesterMutator finalMutator = new UtilFinalizeHarvesterMutator();
                finalMutator.Mutate(wix);
            }

            return this.rootDirectory;
        }

        /// <summary>
        /// Opens a previous MSI and populates default information from it.
        /// </summary>
        /// <param name="filePath">Path to previous MSI file.</param>
        public void OpenPrevious(string filePath)
        {
            // assume nothing about the previous package
            this.previousUpgradeCode = Guid.Empty;
            this.previousVersion = null;
            this.previousUri = null;

            if (filePath != null)
            {
                this.ReadPreviousPackage(filePath);
            }
        }

        /// <summary>
        /// Generates the .wxs file for the application.
        /// </summary>
        /// <returns>XmlDocument containing the .wxs file for the application.</returns>
        private XmlDocument GenerateSourceFile()
        {
            XmlDocument sourceDoc = null;

            // Ensure the root application directory has been calculated and the 
            // new PackageCode is generated.
            this.GetRootDirectory(false);

            if (this.productCode == Guid.Empty)
            {
                this.productCode = Guid.NewGuid();
            }

            // Build up the product information.
            Wix.Wix wix = new Wix.Wix();

            Wix.Product product = new Wix.Product();
            product.Id = this.productCode.ToString();
            product.Language = this.language;
            product.Manufacturer = this.manufacturer;
            product.Name = this.name;
            product.UpgradeCode = this.upgradeCode.ToString();
            product.Version = this.version.ToString();
            wix.AddChild(product);

            Wix.Package package = new Wix.Package();
            package.Compressed = Wix.YesNoType.yes;
            if (null != this.description)
            {
                package.Description = this.description;
            }

            package.InstallerVersion = 200;
            product.AddChild(package);

            Wix.WixVariable variable = new Wix.WixVariable();
            variable = new Wix.WixVariable();
            variable.Id = "ProductName";
            variable.Value = product.Name;
            product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ProductCode";
            variable.Value = product.Id;
            product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ProductVersion";
            variable.Value = product.Version;
            product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ShimPath";
            variable.Value = this.shimPath;
            product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ShimClsid";
            variable.Value = this.ShimClsid.ToString("B");
            product.AddChild(variable);

            variable = new Wix.WixVariable();
            variable.Id = "ShimProgId";
            variable.Value = this.shimProgid;
            product.AddChild(variable);

            // Upgrade logic.
            Wix.Upgrade upgrade = new Wix.Upgrade();
            upgrade.Id = product.UpgradeCode;
            product.AddChild(upgrade);

            Wix.UpgradeVersion minUpgrade = new Wix.UpgradeVersion();
            minUpgrade.Minimum = product.Version;
            minUpgrade.OnlyDetect = Wix.YesNoType.yes;
            minUpgrade.Property = "NEWERVERSIONDETECTED";
            upgrade.AddChild(minUpgrade);

            Wix.UpgradeVersion maxUpgrade = new Wix.UpgradeVersion();
            maxUpgrade.Maximum = product.Version;
            maxUpgrade.IncludeMaximum = Wix.YesNoType.no;
            maxUpgrade.Property = "OLDERVERSIONBEINGUPGRADED";
            upgrade.AddChild(maxUpgrade);

            // Update feed property.
            Wix.Property property = new Wix.Property();
            property.Id = "ARPURLUPDATEINFO";
            property.Value = this.updateUrl.AbsoluteUri;
            product.AddChild(property);

            // Root the application's directory tree in the applications folder.
            Wix.DirectoryRef applicationsFolderRef = new Wix.DirectoryRef();
            applicationsFolderRef.Id = "ApplicationsFolder";
            product.AddChild(applicationsFolderRef);

            this.rootDirectory.Name = String.Concat(product.Id, "v", product.Version);
            applicationsFolderRef.AddChild(this.rootDirectory);

            // Add the shim to the root directory.
            Wix.Component shimComponent = this.GenerateShimComponent();
            this.rootDirectory.AddChild(shimComponent);

            // Add all of the Components to the Feature tree.
            Wix.FeatureRef applicationFeatureRef = new Wix.FeatureRef();
            applicationFeatureRef.Id = "ApplicationFeature";
            product.AddChild(applicationFeatureRef);

            Wix.ComponentRef[] componentRefs = this.GetComponentRefs(this.rootDirectory);
            foreach (Wix.ComponentRef componentRef in componentRefs)
            {
                applicationFeatureRef.AddChild(componentRef);
            }

            // Serialize product information to an xml string.
            string xml;
            using (StringWriter sw = new StringWriter())
            {
                XmlTextWriter writer = null;
                try
                {
                    writer = new XmlTextWriter(sw);

                    wix.OutputXml(writer);

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

            // Load the xml into a document.
            sourceDoc = new XmlDocument();
            sourceDoc.LoadXml(xml);

            return sourceDoc;
        }

        /// <summary>
        /// Creates the shim component.
        /// </summary>
        /// <returns>Component for the shim.</returns>
        private Wix.Component GenerateShimComponent()
        {
            Wix.Component shimComponent = new Wix.Component();

            if (Guid.Empty == this.shimGuid)
            {
                this.shimGuid = Guid.NewGuid();
            }

            shimComponent.Id = "ThisApplicationShimDllComponent";
            shimComponent.Guid = this.shimGuid.ToString("B");

            Wix.File file = new Wix.File();
            file.Id = "ThisApplicationShimDll";
            file.Name = String.Concat(Path.GetFileNameWithoutExtension(this.entryFileRelativePath), "Shim.dll");
            file.Vital = Wix.YesNoType.yes;
            file.KeyPath = Wix.YesNoType.yes;
            file.Source = this.shimPath;
            shimComponent.AddChild(file);

            // Add the CLSID and ProgId to the component.
            Wix.Class classId = new Wix.Class();
            classId.Id = this.ShimClsid.ToString("B");
            classId.Context = Wix.Class.ContextType.InprocServer32;
            if (null != this.Description && String.Empty != this.Description)
            {
                classId.Description = this.Description;
            }

            classId.ThreadingModel = Wix.Class.ThreadingModelType.apartment;
            file.AddChild(classId);

            Wix.ProgId progId = new Wix.ProgId();
            progId.Id = this.ShimProgid;
            progId.Description = "Connect Class";
            classId.AddChild(progId);

            // Add the Addin to the extended Office applications.
            foreach (OfficeAddinFabricator.OfficeApplications extendedOfficeApp in this.extendedOfficeApplications)
            {
                Wix.RegistryKey registryKey = new Wix.RegistryKey();
                registryKey.Root = Wix.RegistryRootType.HKMU;
                registryKey.Key = String.Format("Software\\Microsoft\\Office\\{0}\\Addins\\{1}", OfficeAddinFabricator.OfficeApplicationStrings[(int)extendedOfficeApp], this.ShimProgid);
                shimComponent.AddChild(registryKey);

                Wix.RegistryValue registryValue = new Wix.RegistryValue();
                registryValue.Name = "Description";
                registryValue.Value = "[ProductName] v[ProductVersion]";
                registryValue.Type = Wix.RegistryValue.TypeType.@string;
                registryKey.AddChild(registryValue);

                registryValue = new Wix.RegistryValue();
                registryValue.Name = "FriendlyName";
                registryValue.Value = "[ProductName]";
                registryValue.Type = Wix.RegistryValue.TypeType.@string;
                registryKey.AddChild(registryValue);

                registryValue = new Wix.RegistryValue();
                registryValue.Name = "LoadBehavior";
                registryValue.Value = "3";
                registryValue.Type = Wix.RegistryValue.TypeType.integer;
                registryKey.AddChild(registryValue);
            }

            return shimComponent;
        }

        /// <summary>
        /// Returns a ComponentRef for each Component in the Directory tree.
        /// </summary>
        /// <param name="directory">The root Directory of the components.</param>
        /// <returns>Returns all of the Components in a directory.</returns>
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
        /// Opens the previous package (.msi or .exe) and reads the interesting information from it.
        /// </summary>
        /// <param name="filePath">Path to the package.</param>
        private void ReadPreviousPackage(string filePath)
        {
            using (Database db = new Database(filePath, OpenDatabase.ReadOnly))
            {
                using (View view = db.OpenView("SELECT `Value` FROM `Property` WHERE `Property`=?"))
                {

                    string propertyValue;

                    // get the UpgradeCode
                    propertyValue = this.FetchPropertyValue(view, "UpgradeCode");
                    if (propertyValue != null)
                    {
                        this.previousUpgradeCode = new Guid(propertyValue);
                    }

                    // get the Version
                    propertyValue = this.FetchPropertyValue(view, "ProductVersion");
                    if (propertyValue != null)
                    {
                        this.previousVersion = new Version(propertyValue);
                    }

                    // get the Update URL
                    propertyValue = this.FetchPropertyValue(view, "ARPURLUPDATEINFO");
                    if (propertyValue != null)
                    {
                        this.previousUri = new Uri(propertyValue);
                    }
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

                using (Record recOut = view.Fetch())
                {
                    if (recOut != null)
                    {
                        propertyValue = recOut[1];
                    }
                }
            }

            return propertyValue;
        }

        /// <summary>
        /// Generates the appropriate MSI file for the package.
        /// </summary>
        /// <param name="sourceDoc">WiX document to create MSI from.</param>
        /// <param name="outputFile">File path for the MSI file.</param>
        /// <returns>True if generation works, false if anything goes wrong.</returns>
        private bool GenerateMsi(XmlDocument sourceDoc, string outputFile)
        {
            // Create the Compiler.
            Compiler compiler = new Compiler();
            compiler.Message += this.core.MessageEventHandler;

            // Compile the source document.
            Intermediate intermediate = compiler.Compile(sourceDoc);
            if (intermediate == null)
            {
                return false;
            }

            // Create the variable resolver that will be used in the Linker and Binder.
            WixVariableResolver wixVariableResolver = new WixVariableResolver();
            wixVariableResolver.Message += this.core.MessageEventHandler;

            // Create the Linker.
            Linker linker = new Linker();
            linker.Message += this.core.MessageEventHandler;
            linker.WixVariableResolver = wixVariableResolver;

            // Load the isolatedapp.wixlib.
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Library appLib = LoadLibraryHelper(assembly, "Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin.Data.OfficeAddin.wixlib", linker.TableDefinitions);

            // Link the compiled source document and the isolatedapp.wixlib together.
            SectionCollection sections = new SectionCollection();
            sections.AddRange(intermediate.Sections);
            sections.AddRange(appLib.Sections);

            Output output = linker.Link(sections);
            if (output == null)
            {
                return false;
            }

            // Tweak the compiled output to add a few GUIDs for Components from the oaddin.wixlib.
            Table components = output.Tables["Component"];
            foreach (Row row in components.Rows)
            {
                switch ((string)row[0])
                {
                    case "ThisApplicationVersionRegistryKeyComponent":
                        row[1] = Guid.NewGuid().ToString("B");
                        break;
                    case "ThisApplicationCacheFolderComponent":
                        row[1] = Guid.NewGuid().ToString("B");
                        break;
                    case "ThisApplicationShortcutComponent":
                        row[1] = Guid.NewGuid().ToString("B");
                        break;
                }
            }

            // Bind the final output.
            Binder binder = new Binder();
            binder.FileManager = new BinderFileManager();
            binder.FileManager.SourcePaths.Add(Path.GetDirectoryName(outputFile));
            binder.FileManager.SourcePaths.Add(this.source);
            binder.FileManager.SourcePaths.Add(Path.GetDirectoryName(assembly.Location));
            binder.Message += this.core.MessageEventHandler;
            binder.WixVariableResolver = wixVariableResolver;
            return binder.Bind(output, outputFile);
        }
    }
}
