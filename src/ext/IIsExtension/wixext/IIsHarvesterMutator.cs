//-------------------------------------------------------------------------------------------------
// <copyright file="IIsHarvesterMutator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The harvester mutator for the Windows Installer XML Toolset Internet Information Services Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.IO;

    using IIs = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.IIs;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The harvester mutator for the Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsHarvesterMutator : MutatorExtension
    {
        private ArrayList components;
        private DirectoryHarvester directoryHarvester;
        private Hashtable directoryPaths;
        private FileHarvester fileHarvester;
        private Wix.IParentElement rootElement;
        private bool setUniqueIdentifiers;
        private ArrayList webAddresses;
        private ArrayList webDirs;
        private ArrayList webDirProperties;
        private ArrayList webFilters;
        private ArrayList webSites;
        private ArrayList webVirtualDirs;

        /// <summary>
        /// Instantiate a new IIsHarvesterMutator.
        /// </summary>
        public IIsHarvesterMutator()
        {
            this.components = new ArrayList();
            this.directoryHarvester = new DirectoryHarvester();
            this.directoryPaths = CollectionsUtil.CreateCaseInsensitiveHashtable();
            this.fileHarvester = new FileHarvester();
            this.webAddresses = new ArrayList();
            this.webDirs = new ArrayList();
            this.webDirProperties = new ArrayList();
            this.webFilters = new ArrayList();
            this.webSites = new ArrayList();
            this.webVirtualDirs = new ArrayList();
        }

        /// <summary>
        /// Gets the sequence of this mutator extension.
        /// </summary>
        /// <value>The sequence of this mutator extension.</value>
        public override int Sequence
        {
            get { return 100; }
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
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        public override void Mutate(Wix.Wix wix)
        {
            this.components.Clear();
            this.directoryPaths.Clear();
            this.webAddresses.Clear();
            this.webDirs.Clear();
            this.webDirProperties.Clear();
            this.webFilters.Clear();
            this.webSites.Clear();
            this.webVirtualDirs.Clear();
            this.rootElement = null;

            this.IndexElement(wix);

            this.MutateWebAddresses();

            this.MutateWebDirs();

            this.MutateWebDirProperties();

            this.MutateWebSites();

            this.MutateWebVirtualDirs();

            // this must come after the web virtual dirs in case they harvest a directory containing a web filter file
            this.MutateWebFilters();

            // this must come after the web site identifiers are created
            this.MutateComponents();
        }

        /// <summary>
        /// Harvest a new directory or return one that was previously harvested.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="harvestChildren">The option to harvest the children of the directory.</param>
        /// <returns>The harvested directory.</returns>
        private Wix.Directory HarvestUniqueDirectory(string path, bool harvestChildren)
        {
            if (this.directoryPaths.Contains(path))
            {
                return (Wix.Directory)this.directoryPaths[path];
            }
            else
            {
                Wix.Directory directory = this.directoryHarvester.HarvestDirectory(path, harvestChildren);

                this.rootElement.AddChild(directory);

                // index this new directory and all of its children
                this.IndexElement(directory);

                return directory;
            }
        }

        /// <summary>
        /// Index an element.
        /// </summary>
        /// <param name="element">The element to index.</param>
        private void IndexElement(Wix.ISchemaElement element)
        {
            if (element is IIs.WebAddress)
            {
                this.webAddresses.Add(element);
            }
            else if (element is IIs.WebDir)
            {
                this.webDirs.Add(element);
            }
            else if (element is IIs.WebDirProperties)
            {
                this.webDirProperties.Add(element);
            }
            else if (element is IIs.WebFilter)
            {
                this.webFilters.Add(element);
            }
            else if (element is IIs.WebSite)
            {
                this.webSites.Add(element);
            }
            else if (element is IIs.WebVirtualDir)
            {
                this.webVirtualDirs.Add(element);
            }
            else if (element is Wix.Component)
            {
                this.components.Add(element);
            }
            else if (element is Wix.Directory)
            {
                Wix.Directory directory = (Wix.Directory)element;

                if (null != directory.FileSource)
                {
                    this.directoryPaths.Add(directory.FileSource, directory);
                }
            }
            else if (element is Wix.Fragment || element is Wix.Module || element is Wix.PatchCreation || element is Wix.Product)
            {
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
        /// Mutate the Component elements.
        /// </summary>
        private void MutateComponents()
        {
            if (this.setUniqueIdentifiers)
            {
                IdentifierGenerator identifierGenerator = new IdentifierGenerator("Component");

                // index all the existing identifiers
                foreach (Wix.Component component in this.components)
                {
                    if (null != component.Id)
                    {
                        identifierGenerator.IndexExistingIdentifier(component.Id);
                    }
                }

                // index all the web site identifiers
                foreach (IIs.WebSite webSite in this.webSites)
                {
                    if (webSite.ParentElement is Wix.Component)
                    {
                        identifierGenerator.IndexName(webSite.Id);
                    }
                }

                // create an identifier for each component based on its child web site identifier
                foreach (IIs.WebSite webSite in this.webSites)
                {
                    Wix.Component component = webSite.ParentElement as Wix.Component;

                    if (null != component)
                    {
                        component.Id = identifierGenerator.GetIdentifier(webSite.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the WebAddress elements.
        /// </summary>
        private void MutateWebAddresses()
        {
            if (this.setUniqueIdentifiers)
            {
                IdentifierGenerator identifierGenerator = new IdentifierGenerator("WebAddress");

                // index all the existing identifiers and names
                foreach (IIs.WebAddress webAddress in this.webAddresses)
                {
                    if (null != webAddress.Id)
                    {
                        identifierGenerator.IndexExistingIdentifier(webAddress.Id);
                    }
                    else
                    {
                        identifierGenerator.IndexName(String.Concat(webAddress.IP, "_", webAddress.Port));
                    }
                }

                foreach (IIs.WebAddress webAddress in this.webAddresses)
                {
                    if (null == webAddress.Id)
                    {
                        webAddress.Id = identifierGenerator.GetIdentifier(String.Concat(webAddress.IP, "_", webAddress.Port));
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the WebDir elements.
        /// </summary>
        private void MutateWebDirs()
        {
            if (this.setUniqueIdentifiers)
            {
                IdentifierGenerator identifierGenerator = new IdentifierGenerator("WebDir");

                // index all the existing identifiers and names
                foreach (IIs.WebDir webDir in this.webDirs)
                {
                    if (null != webDir.Id)
                    {
                        identifierGenerator.IndexExistingIdentifier(webDir.Id);
                    }
                    else
                    {
                        identifierGenerator.IndexName(webDir.Path);
                    }
                }

                foreach (IIs.WebDir webDir in this.webDirs)
                {
                    if (null == webDir.Id)
                    {
                        webDir.Id = identifierGenerator.GetIdentifier(webDir.Path);
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the WebDirProperties elements.
        /// </summary>
        private void MutateWebDirProperties()
        {
            if (this.setUniqueIdentifiers)
            {
                IdentifierGenerator identifierGenerator = new IdentifierGenerator("WebDirProperties");

                // index all the existing identifiers and names
                foreach (IIs.WebDirProperties webDirProperties in this.webDirProperties)
                {
                    if (null != webDirProperties.Id)
                    {
                        identifierGenerator.IndexExistingIdentifier(webDirProperties.Id);
                    }
                }

                foreach (IIs.WebDirProperties webDirProperties in this.webDirProperties)
                {
                    if (null == webDirProperties.Id)
                    {
                        webDirProperties.Id = identifierGenerator.GetIdentifier(String.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the WebFilter elements.
        /// </summary>
        private void MutateWebFilters()
        {
            IdentifierGenerator identifierGenerator = null;

            if (this.setUniqueIdentifiers)
            {
                identifierGenerator = new IdentifierGenerator("WebFilter");

                // index all the existing identifiers and names
                foreach (IIs.WebFilter webFilter in this.webFilters)
                {
                    if (null != webFilter.Id)
                    {
                        identifierGenerator.IndexExistingIdentifier(webFilter.Id);
                    }
                    else
                    {
                        identifierGenerator.IndexName(webFilter.Name);
                    }
                }
            }

            foreach (IIs.WebFilter webFilter in this.webFilters)
            {
                if (this.setUniqueIdentifiers && null == webFilter.Id)
                {
                    webFilter.Id = identifierGenerator.GetIdentifier(webFilter.Name);
                }

                // harvest the file for this WebFilter
                Wix.Directory directory = this.HarvestUniqueDirectory(Path.GetDirectoryName(webFilter.Path), false);

                Wix.Component component = new Wix.Component();
                directory.AddChild(component);

                Wix.File file = this.fileHarvester.HarvestFile(webFilter.Path);
                component.AddChild(file);
            }
        }

        /// <summary>
        /// Mutate the WebSite elements.
        /// </summary>
        private void MutateWebSites()
        {
            if (this.setUniqueIdentifiers)
            {
                IdentifierGenerator identifierGenerator = new IdentifierGenerator("WebSite");

                // index all the existing identifiers and names
                foreach (IIs.WebSite webSite in this.webSites)
                {
                    if (null != webSite.Id)
                    {
                        identifierGenerator.IndexExistingIdentifier(webSite.Id);
                    }
                    else
                    {
                        identifierGenerator.IndexName(webSite.Description);
                    }
                }

                foreach (IIs.WebSite webSite in this.webSites)
                {
                    if (null == webSite.Id)
                    {
                        webSite.Id = identifierGenerator.GetIdentifier(webSite.Description);
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the WebVirtualDir elements.
        /// </summary>
        private void MutateWebVirtualDirs()
        {
            IdentifierGenerator identifierGenerator = null;

            if (this.setUniqueIdentifiers)
            {
                identifierGenerator = new IdentifierGenerator("WebVirtualDir");

                // index all the existing identifiers and names
                foreach (IIs.WebVirtualDir webVirtualDir in this.webVirtualDirs)
                {
                    if (null != webVirtualDir.Id)
                    {
                        identifierGenerator.IndexExistingIdentifier(webVirtualDir.Id);
                    }
                    else
                    {
                        identifierGenerator.IndexName(webVirtualDir.Alias);
                    }
                }
            }

            foreach (IIs.WebVirtualDir webVirtualDir in this.webVirtualDirs)
            {
                if (this.setUniqueIdentifiers && null == webVirtualDir.Id)
                {
                    webVirtualDir.Id = identifierGenerator.GetIdentifier(webVirtualDir.Alias);
                }

                // harvest the directory for this WebVirtualDir
                this.HarvestUniqueDirectory(webVirtualDir.Directory, true);
            }
        }
    }
}