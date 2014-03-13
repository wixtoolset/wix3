// <copyright file="IsolatedMsiBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Isolated applications MSI builder for ClickThrough.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Builds isolated application MSI.
    /// </summary>
    sealed internal class IsolatedMsiBuilder : WixExtension
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
        private Guid upgradeCode;
        private Uri updateUrl;
        private Version version;

        private string entryFileRelativePath;
        private string source;
        private Wix.Directory rootDirectory;

        /// <summary>
        /// Creates a new IsoaltedMsiBuilder object.
        /// </summary>
        /// <param name="core">Core build object for message handling.</param>
        public IsolatedMsiBuilder(FabricatorCore core)
        {
            this.core = core;
            this.language = "1033";
            this.productCode = Guid.Empty;
            this.upgradeCode = Guid.Empty;
            this.previousUpgradeCode = Guid.Empty;
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
            get { return this.productCode; }
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
        /// <returns>True if build success, false if any failure occurs.</returns>
        public bool Build(string outputFile, string outputSourceFile)
        {
            this.productCode = Guid.Empty;

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
            if (this.previousUpgradeCode != Guid.Empty && this.previousUpgradeCode != this.upgradeCode && this.core != null)
            {
                this.core.OnMessage(IsolatedAppErrors.UpgradeCodeChanged(this.previousUpgradeCode, this.upgradeCode));
                return false;
            }

            if (this.previousVersion != null && this.previousVersion >= this.version && this.core != null)
            {
                this.core.OnMessage(IsolatedAppErrors.NewVersionIsNotGreater(this.previousVersion, this.version));
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
        /// <param name="recalculate">Recalculate root directory.</param>
        /// <returns>Directory for root.</returns>
        public Wix.Directory GetRootDirectory(bool recalculate)
        {
            if (null == this.source)
            {
                throw new ArgumentNullException("Source");
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

            this.productCode = Guid.NewGuid();

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
            package.InstallPrivileges = Wix.Package.InstallPrivilegesType.limited;
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

            // Find the entry File/@Id for the Shortcut to point at.
            Wix.File entryFile = this.GetFile(this.rootDirectory, this.entryFileRelativePath);
            variable = new Wix.WixVariable();
            variable.Id = "ShortcutFileId";
            variable.Value = entryFile.Id;
            product.AddChild(variable);

            // Set the target Component's GUID to be the same as the ProductCode for easy
            // lookup by the update.exe.
            Wix.Component targetComponent = (Wix.Component)entryFile.ParentElement;
            targetComponent.Guid = product.Id;

            variable = new Wix.WixVariable();
            variable.Id = "TargetComponentId";
            variable.Value = targetComponent.Guid;
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
        /// Returns a ComponentRef for each Component in the Directory tree.
        /// </summary>
        /// <param name="directory">The root Directory of the components.</param>
        /// <returns>All Components in directory.</returns>
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
        /// <param name="rootDirectory">Directory tree to search for relative path in.</param>
        /// <param name="relativePath">Relative path to the file to find in the directory.</param>
        /// <returns>File at relativePath in rootDirectory.</returns>
        private Wix.File GetFile(Wix.Directory rootDirectory, string relativePath)
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
        /// <returns>True if generation worked, false if there were any failures.</returns>
        private bool GenerateMsi(XmlDocument sourceDoc, string outputFile)
        {
            // Create the Compiler.
            Compiler compiler = new Compiler();
            if (this.core != null)
            {
                compiler.Message += this.core.MessageEventHandler;
            }

            // Compile the source document.
            Intermediate intermediate = compiler.Compile(sourceDoc);
            if (intermediate == null)
            {
                return false;
            }

            // Create the variable resolver that will be used in the Linker and Binder.
            WixVariableResolver wixVariableResolver = new WixVariableResolver();
            if (this.core != null)
            {
                wixVariableResolver.Message += this.core.MessageEventHandler;
            }

            // Create the Linker.
            Linker linker = new Linker();
            if (this.core != null)
            {
                linker.Message += this.core.MessageEventHandler;
            }
            linker.WixVariableResolver = wixVariableResolver;

            // Load the isolatedapp.wixlib.
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Library appLib = LoadLibraryHelper(assembly, "Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp.Data.IsolatedApp.wixlib", linker.TableDefinitions);

            // Link the compiled source document and the isolatedapp.wixlib together.
            SectionCollection sections = new SectionCollection();
            sections.AddRange(intermediate.Sections);
            sections.AddRange(appLib.Sections);

            Output output = linker.Link(sections);
            if (output == null)
            {
                return false;
            }

            // Tweak the compiled output to add a few GUIDs for Components from the isolatedapp.wixlib.
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

            // Bind the final output.
            Binder binder = new Binder();
            binder.FileManager = new BinderFileManager();
            binder.FileManager.SourcePaths.Add(Path.GetDirectoryName(outputFile));
            binder.FileManager.SourcePaths.Add(this.source);
            binder.FileManager.SourcePaths.Add(Path.GetDirectoryName(assembly.Location));
            if (this.core != null)
            {
                binder.Message += this.core.MessageEventHandler;
            }
            binder.WixVariableResolver = wixVariableResolver;
            return binder.Bind(output, outputFile);
        }
    }
}
