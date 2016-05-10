// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The template type.
    /// </summary>
    public enum TemplateType
    {
        /// <summary>
        /// A fragment template.
        /// </summary>
        Fragment,

        /// <summary>
        /// A module template.
        /// </summary>
        Module,

        /// <summary>
        /// A product template.
        /// </summary>
        Product
    }

    /// <summary>
    /// The mutator for the Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class UtilMutator : MutatorExtension
    {
        private ArrayList components;
        private ArrayList componentGroups;
        private string componentGroupName;
        private bool createFragments;
        private ArrayList directories;
        private ArrayList directoryRefs;
        private ArrayList files;
        private ArrayList features;
        private SortedList fragments;
        private bool autogenerateGuids;
        private bool generateGuids;
        private string guidFormat = "B"; // Defaults to guid in {}
        private Wix.IParentElement rootElement;
        private bool setUniqueIdentifiers;
        private TemplateType templateType;

        /// <summary>
        /// Instantiate a new UtilMutator.
        /// </summary>
        public UtilMutator()
        {
            this.components = new ArrayList();
            this.componentGroups = new ArrayList();
            this.directories = new ArrayList();
            this.directoryRefs = new ArrayList();
            this.features = new ArrayList();
            this.files = new ArrayList();
            this.fragments = new SortedList();
        }

        /// <summary>
        /// Gets or sets the value of the component group name.
        /// </summary>
        /// <value>The component group name.</value>
        public string ComponentGroupName
        {
            get { return this.componentGroupName; }
            set { this.componentGroupName = value; }
        }

        /// <summary>
        /// Gets or sets the option to create fragments.
        /// </summary>
        /// <value>The option to create fragments.</value>
        public bool CreateFragments
        {
            get { return this.createFragments; }
            set { this.createFragments = value; }
        }

        /// <summary>
        /// Gets or sets the option to autogenerate component guids at compile time.
        /// </summary>
        /// <value>The option to autogenerate component guids.</value>
        public bool AutogenerateGuids
        {
            get { return this.autogenerateGuids; }
            set { this.autogenerateGuids = value; }
        }

        /// <summary>
        /// Gets or sets the option to generate missing guids.
        /// </summary>
        /// <value>The option to generate missing guids.</value>
        public bool GenerateGuids
        {
            get { return this.generateGuids; }
            set { this.generateGuids = value; }
        }

        /// <summary>
        /// Gets or sets the option to set the format of guids.
        /// D - 32 digits separated by hyphens: 
        ///     xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 
        /// B - 32 digits separated by hyphens, enclosed in brackets: 
        ///     {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx} 
        /// </summary>
        /// <value>Guid format either B or D.</value>
        public string GuidFormat
        {
            get { return this.guidFormat; }
            set { this.guidFormat = value; }
        }

        /// <summary>
        /// Gets the sequence of the extension.
        /// </summary>
        /// <value>The sequence of the extension.</value>
        public override int Sequence
        {
            get { return 1000; }
        }

        /// <summary>
        /// Gets of sets the option to set unique identifiers.
        /// </summary>
        /// <value>The option to set unique identifiers.</value>
        public bool SetUniqueIdentifiers
        {
            get { return this.setUniqueIdentifiers; }
            set { this.setUniqueIdentifiers = value; }
        }

        /// <summary>
        /// Gets or sets the template type.
        /// </summary>
        /// <value>The template type.</value>
        public TemplateType TemplateType
        {
            get { return this.templateType; }
            set { this.templateType = value; }
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        public override void Mutate(Wix.Wix wix)
        {
            this.components.Clear();
            this.directories.Clear();
            this.directoryRefs.Clear();
            this.features.Clear();
            this.files.Clear();
            this.fragments.Clear();
            this.rootElement = null;

            // index elements in this wix document
            this.IndexElement(wix);

            this.MutateWix(wix);

            this.MutateFiles();

            this.MutateDirectories();

            this.MutateComponents();

            if (null != this.componentGroupName)
            {
                this.CreateComponentGroup(wix);
            }

            // add the components to the product feature after all the identifiers have been set
            if (TemplateType.Product == this.templateType)
            {
                Wix.Feature feature = (Wix.Feature)this.features[0];

                foreach (Wix.ComponentGroup group in this.componentGroups)
                {
                    Wix.ComponentGroupRef componentGroupRef = new Wix.ComponentGroupRef();
                    componentGroupRef.Id = group.Id;

                    feature.AddChild(componentGroupRef);
                }
            }
            else if (TemplateType.Module == this.templateType)
            {
                foreach (Wix.ISchemaElement element in wix.Children)
                {
                    if (element is Wix.Module)
                    {
                        foreach (Wix.ComponentGroup group in this.componentGroups)
                        {
                            Wix.ComponentGroupRef componentGroupRef = new Wix.ComponentGroupRef();
                            componentGroupRef.Id = group.Id;

                            ((Wix.IParentElement)element).AddChild(componentGroupRef);
                        }
                        break;
                    }
                }
            }

            //if(!this.createFragments && TemplateType.Product
            foreach (Wix.Fragment fragment in this.fragments.Values)
            {
                wix.AddChild(fragment);
            }
        }

        /// <summary>
        /// Creates a component group with a given name.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        private void CreateComponentGroup(Wix.Wix wix)
        {
            Wix.ComponentGroup componentGroup = new Wix.ComponentGroup();
            componentGroup.Id = this.componentGroupName;
            this.componentGroups.Add(componentGroup);

            Wix.Fragment cgFragment = new Wix.Fragment();
            cgFragment.AddChild(componentGroup);
            wix.AddChild(cgFragment);

            int componentCount = 0;
            for (; componentCount < this.components.Count; componentCount++)
            {
                Wix.Component c = this.components[componentCount] as Wix.Component;

                if (this.createFragments)
                {
                    if (c.ParentElement is Wix.Directory)
                    {
                        Wix.Directory parentDirectory = c.ParentElement as Wix.Directory;

                        componentGroup.AddChild(c);
                        c.Directory = parentDirectory.Id;
                        parentDirectory.RemoveChild(c);
                    }
                    else if (c.ParentElement is Wix.DirectoryRef)
                    {
                        Wix.DirectoryRef parentDirectory = c.ParentElement as Wix.DirectoryRef;

                        componentGroup.AddChild(c);
                        c.Directory = parentDirectory.Id;
                        parentDirectory.RemoveChild(c);

                        // Remove whole fragment if moving the component to the component group just leaves an empty DirectoryRef
                        if (0 < fragments.Count && parentDirectory.ParentElement is Wix.Fragment)
                        {
                            Wix.Fragment parentFragment = parentDirectory.ParentElement as Wix.Fragment;
                            int childCount = 0;
                            foreach (Wix.ISchemaElement element in parentFragment.Children)
                            {
                                childCount++;
                            }

                            // Component should always have an Id but the SortedList creation allows for null and bases the name on the fragment count which we cannot reverse engineer here.
                            if (1 == childCount && !String.IsNullOrEmpty(c.Id))
                            {
                                int removeIndex = fragments.IndexOfKey(String.Concat("Component:", c.Id));
                                if (0 <= removeIndex)
                                {
                                    fragments.RemoveAt(removeIndex);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Wix.ComponentRef componentRef = new Wix.ComponentRef();
                    componentRef.Id = c.Id;
                    componentGroup.AddChild(componentRef);
                }
            }
        }

        /// <summary>
        /// Index an element.
        /// </summary>
        /// <param name="element">The element to index.</param>
        private void IndexElement(Wix.ISchemaElement element)
        {
            if (element is Wix.Component)
            {
                this.components.Add(element);
            }
            else if (element is Wix.ComponentGroup)
            {
                this.componentGroups.Add(element);
            }
            else if (element is Wix.Directory)
            {
                this.directories.Add(element);
            }
            else if (element is Wix.DirectoryRef)
            {
                this.directoryRefs.Add(element);
            }
            else if (element is Wix.Feature)
            {
                this.features.Add(element);
            }
            else if (element is Wix.File)
            {
                this.files.Add(element);
            }
            else if (element is Wix.Module || element is Wix.PatchCreation || element is Wix.Product)
            {
                Debug.Assert(null == this.rootElement);
                this.rootElement = (Wix.IParentElement)element;
            }

            // index the child elements
            if (element is Wix.IParentElement)
            {
                foreach (Wix.ISchemaElement childElement in ((Wix.IParentElement)element).Children)
                {
                    this.IndexElement(childElement);
                }
            }
        }

        /// <summary>
        /// Mutate the components.
        /// </summary>
        private void MutateComponents()
        {
            IdentifierGenerator identifierGenerator = new IdentifierGenerator("Component");
            if (TemplateType.Module == this.templateType)
            {
                identifierGenerator.MaxIdentifierLength = IdentifierGenerator.MaxModuleIdentifierLength;
            }

            foreach (Wix.Component component in this.components)
            {
                if (null == component.Id)
                {
                    string firstFileId = string.Empty;

                    // attempt to create a possible identifier from the first file identifier in the component
                    foreach (Wix.File file in component[typeof(Wix.File)])
                    {
                        firstFileId = file.Id;
                        break;
                    }

                    if (string.IsNullOrEmpty(firstFileId))
                    {
                        firstFileId = GetGuid();
                    }

                    component.Id = identifierGenerator.GetIdentifier(firstFileId);
                }

                if (null == component.Guid)
                {
                    if (this.AutogenerateGuids)
                    {
                        component.Guid = "*";
                    }
                    else
                    {
                        component.Guid = this.GetGuid();
                    }
                }

                if (this.createFragments && component.ParentElement is Wix.Directory)
                {
                    Wix.Directory directory = (Wix.Directory)component.ParentElement;

                    // parent directory must have an identifier to create a reference to it
                    if (null == directory.Id)
                    {
                        break;
                    }

                    if (this.rootElement is Wix.Module)
                    {
                        // add a ComponentRef for the Component
                        Wix.ComponentRef componentRef = new Wix.ComponentRef();
                        componentRef.Id = component.Id;
                        this.rootElement.AddChild(componentRef);
                    }

                    // create a new Fragment
                    Wix.Fragment fragment = new Wix.Fragment();
                    this.fragments.Add(String.Concat("Component:", (null != component.Id ? component.Id : this.fragments.Count.ToString())), fragment);

                    // create a new DirectoryRef
                    Wix.DirectoryRef directoryRef = new Wix.DirectoryRef();
                    directoryRef.Id = directory.Id;
                    fragment.AddChild(directoryRef);

                    // move the Component from the the Directory to the DirectoryRef
                    directory.RemoveChild(component);
                    directoryRef.AddChild(component);
                }
            }
        }

        /// <summary>
        /// Mutate the directories.
        /// </summary>
        private void MutateDirectories()
        {
            if (!this.setUniqueIdentifiers)
            {
                // assign all identifiers before fragmenting (because fragmenting requires them all to be present)
                IdentifierGenerator identifierGenerator = new IdentifierGenerator("Directory");
                if (TemplateType.Module == this.templateType)
                {
                    identifierGenerator.MaxIdentifierLength = IdentifierGenerator.MaxModuleIdentifierLength;
                }

                foreach (Wix.Directory directory in this.directories)
                {
                    if (null == directory.Id)
                    {
                        directory.Id = identifierGenerator.GetIdentifier(directory.Name);
                    }
                }
            }

            if (this.createFragments)
            {
                foreach (Wix.Directory directory in this.directories)
                {
                    if (directory.ParentElement is Wix.Directory)
                    {
                        Wix.Directory parentDirectory = (Wix.Directory)directory.ParentElement;

                        // parent directory must have an identifier to create a reference to it
                        if (null == parentDirectory.Id)
                        {
                            return;
                        }

                        // create a new Fragment
                        Wix.Fragment fragment = new Wix.Fragment();
                        this.fragments.Add(String.Concat("Directory:", ("TARGETDIR" == directory.Id ? null : (null != directory.Id ? directory.Id : this.fragments.Count.ToString()))), fragment);

                        // create a new DirectoryRef
                        Wix.DirectoryRef directoryRef = new Wix.DirectoryRef();
                        directoryRef.Id = parentDirectory.Id;
                        fragment.AddChild(directoryRef);

                        // move the Directory from the parent Directory to DirectoryRef
                        parentDirectory.RemoveChild(directory);
                        directoryRef.AddChild(directory);
                    }
                    else if (directory.ParentElement is Wix.Fragment)
                    {
                        // When creating fragments, remove any top-level Directory elements;
                        // the fragments should be pulled in by their DirectoryRefs instead.
                        Wix.Fragment parent = (Wix.Fragment)directory.ParentElement;
                        parent.RemoveChild(directory);

                        // Remove the fragment if it is empty.
                        if (parent.Children.GetEnumerator().Current == null && parent.ParentElement != null)
                        {
                            ((Wix.IParentElement)parent.ParentElement).RemoveChild(parent);
                        }
                    }
                    else if (directory.ParentElement == this.rootElement)
                    {
                        // create a new Fragment
                        Wix.Fragment fragment = new Wix.Fragment();
                        this.fragments.Add(String.Concat("Directory:", ("TARGETDIR" == directory.Id ? null : (null != directory.Id ? directory.Id : this.fragments.Count.ToString()))), fragment);

                        // move the Directory from the root element to the Fragment
                        this.rootElement.RemoveChild(directory);
                        fragment.AddChild(directory);
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the files.
        /// </summary>
        private void MutateFiles()
        {
            IdentifierGenerator identifierGenerator = new IdentifierGenerator("File");
            if (TemplateType.Module == this.templateType)
            {
                identifierGenerator.MaxIdentifierLength = IdentifierGenerator.MaxModuleIdentifierLength;
            }

            foreach (Wix.File file in this.files)
            {
                if (null == file.Id)
                {
                    file.Id = identifierGenerator.GetIdentifier(Path.GetFileName(file.Source));
                }
            }
        }

        /// <summary>
        /// Mutate a Wix element.
        /// </summary>
        /// <param name="wix">The Wix element to mutate.</param>
        private void MutateWix(Wix.Wix wix)
        {
            if (TemplateType.Fragment != this.templateType)
            {
                if (null != this.rootElement || 0 != this.features.Count)
                {
                    throw new Exception("The template option cannot be used with Feature, Product, or Module elements present.");
                }

                // create a package element although it won't always be used
                Wix.Package package = new Wix.Package();
                if (TemplateType.Module == this.templateType)
                {
                    package.Id = this.GetGuid();
                }
                else
                {
                    package.Compressed = Wix.YesNoType.yes;
                }

                package.InstallerVersion = 200;

                Wix.Directory targetDir = new Wix.Directory();
                targetDir.Id = "TARGETDIR";
                targetDir.Name = "SourceDir";

                foreach (Wix.DirectoryRef directoryRef in this.directoryRefs)
                {
                    if (String.Equals(directoryRef.Id, "TARGETDIR", StringComparison.OrdinalIgnoreCase))
                    {
                        Wix.IParentElement parent = directoryRef.ParentElement as Wix.IParentElement;

                        foreach (Wix.ISchemaElement element in directoryRef.Children)
                        {
                            targetDir.AddChild(element);
                        }

                        parent.RemoveChild(directoryRef);

                        if (null != ((Wix.ISchemaElement)parent).ParentElement)
                        {
                            int i = 0;

                            foreach (Wix.ISchemaElement element in parent.Children)
                            {
                                i++;
                            }

                            if (0 == i)
                            {
                                Wix.IParentElement supParent = (Wix.IParentElement)((Wix.ISchemaElement)parent).ParentElement;
                                supParent.RemoveChild((Wix.ISchemaElement)parent);
                            }
                        }

                        break;
                    }
                }

                if (TemplateType.Module == this.templateType)
                {
                    Wix.Module module = new Wix.Module();
                    module.Id = "PUT-MODULE-NAME-HERE";
                    module.Language = "1033";
                    module.Version = "1.0.0.0";

                    package.Manufacturer = "PUT-COMPANY-NAME-HERE";
                    module.AddChild(package);
                    module.AddChild(targetDir);

                    wix.AddChild(module);
                    this.rootElement = module;
                }
                else // product
                {
                    Wix.Product product = new Wix.Product();
                    product.Id = this.GetGuid();
                    product.Language = "1033";
                    product.Manufacturer = "PUT-COMPANY-NAME-HERE";
                    product.Name = "PUT-PRODUCT-NAME-HERE";
                    product.UpgradeCode = this.GetGuid();
                    product.Version = "1.0.0.0";
                    product.AddChild(package);
                    product.AddChild(targetDir);

                    Wix.Media media = new Wix.Media();
                    media.Id = "1";
                    media.Cabinet = "product.cab";
                    media.EmbedCab = Wix.YesNoType.yes;
                    product.AddChild(media);

                    Wix.Feature feature = new Wix.Feature();
                    feature.Id = "ProductFeature";
                    feature.Title = "PUT-FEATURE-TITLE-HERE";
                    feature.Level = 1;
                    product.AddChild(feature);
                    this.features.Add(feature);

                    wix.AddChild(product);
                    this.rootElement = product;
                }
            }
        }

        /// <summary>
        /// Get a generated guid or a placeholder for a guid.
        /// </summary>
        /// <returns>A generated guid or placeholder.</returns>
        private string GetGuid()
        {
            if (this.generateGuids)
            {
                return Guid.NewGuid().ToString(guidFormat, CultureInfo.InvariantCulture).ToUpper(CultureInfo.InvariantCulture);
            }
            else
            {
                return "PUT-GUID-HERE";
            }
        }
    }
}
