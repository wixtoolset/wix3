// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;

    using IIs = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.IIs;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The finalize harvester mutator for the Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsFinalizeHarvesterMutator : MutatorExtension
    {
        private Hashtable directoryPaths;
        private Hashtable filePaths;
        private ArrayList webFilters;
        private ArrayList webSites;
        private ArrayList webVirtualDirs;

        /// <summary>
        /// Instantiate a new IIsFinalizeHarvesterMutator.
        /// </summary>
        public IIsFinalizeHarvesterMutator()
        {
            this.directoryPaths = CollectionsUtil.CreateCaseInsensitiveHashtable();
            this.filePaths = CollectionsUtil.CreateCaseInsensitiveHashtable();
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
            get { return 1900; }
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        public override void Mutate(Wix.Wix wix)
        {
            this.directoryPaths.Clear();
            this.filePaths.Clear();
            this.webFilters.Clear();
            this.webSites.Clear();
            this.webVirtualDirs.Clear();

            this.IndexElement(wix);

            this.MutateWebFilters();
            this.MutateWebSites();
            this.MutateWebVirtualDirs();
        }

        /// <summary>
        /// Index an element.
        /// </summary>
        /// <param name="element">The element to index.</param>
        private void IndexElement(Wix.ISchemaElement element)
        {
            if (element is IIs.WebFilter)
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
            else if (element is Wix.Directory)
            {
                Wix.Directory directory = (Wix.Directory)element;

                if (null != directory.Id && null != directory.FileSource)
                {
                    this.directoryPaths.Add(directory.FileSource, directory.Id);
                }
            }
            else if (element is Wix.File)
            {
                Wix.File file = (Wix.File)element;

                if (null != file.Id && null != file.Source)
                {
                    this.filePaths[file.Source] = String.Concat("[#", file.Id, "]");
                }
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
        /// Mutate the WebFilters.
        /// </summary>
        private void MutateWebFilters()
        {
            foreach (IIs.WebFilter webFilter in this.webFilters)
            {
                webFilter.Path = (string)this.filePaths[webFilter.Path];
            }
        }

        /// <summary>
        /// Mutate the WebSites.
        /// </summary>
        private void MutateWebSites()
        {
            foreach (IIs.WebSite webSite in this.webSites)
            {
                string path = (string)this.directoryPaths[webSite.Directory];
                if (null == path)
                {
                    this.Core.OnMessage(IIsWarnings.EncounteredNullDirectoryForWebSite(path));
                }
                else
                {
                    webSite.Directory = path;
                }
            }
        }

        /// <summary>
        /// Mutate the WebVirtualDirs.
        /// </summary>
        private void MutateWebVirtualDirs()
        {
            foreach (IIs.WebVirtualDir webVirtualDir in this.webVirtualDirs)
            {
                string path = (string)this.directoryPaths[webVirtualDir.Directory];
                if (null == path)
                {
                    this.Core.OnMessage(IIsWarnings.EncounteredNullDirectoryForWebSite(path));
                }
                else
                {
                    webVirtualDir.Directory = path;
                }
            }
        }
    }
}
