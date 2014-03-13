//-------------------------------------------------------------------------------------------------
// <copyright file="WixExtensionReferenceNode.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixExtensionReferenceNode class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Build.Tasks;
    using Microsoft.VisualStudio.Package;
    using Utilities = Microsoft.VisualStudio.Package.Utilities;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Represents a Wix extension reference node.
    /// </summary>
    [CLSCompliant(false)]
    public class WixExtensionReferenceNode : WixReferenceNode
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private const string ExtensionDirectoryToken = "$(" + WixProjectFileConstants.WixExtDir + ")";

        // Extension directory is the default location where Wix extension dlls are located.
        private static string extensionDirectory;

        private Version version;

        /// <summary>
        /// Defines the listener that would listen on file changes.
        /// </summary>
        private FileChangeManager fileChangeListener;

        /// <summary>
        /// A flag for specifying if the object was disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// The file being watched.
        /// </summary>
        private string observedFile;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixExtensionReferenceNode"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="WixProjectNode"/> that contains this node.</param>
        /// <param name="element">The element that contains MSBuild properties.</param>
        public WixExtensionReferenceNode(WixProjectNode root, ProjectElement element)
            : base(root, element)
        {
            this.InitializeFileChangeEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WixExtensionReferenceNode"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="WixProjectNode"/> that contains this node.</param>
        /// <param name="referencePath">The path to the wixlib reference file.</param>
        public WixExtensionReferenceNode(WixProjectNode root, string referencePath)
            : base(root, referencePath, WixProjectFileConstants.WixExtension)
        {
            referencePath = WixHelperMethods.ReplacePathWithBuildProperty(referencePath, ExtensionDirectoryToken, this.ExtensionDirectory);

            if (!referencePath.StartsWith(ExtensionDirectoryToken, StringComparison.Ordinal) && null != root)
            {
                referencePath = root.GetRelativePath(referencePath);
            }

            this.ItemNode.SetMetadata(ProjectFileConstants.HintPath, referencePath);

            this.InitializeFileChangeEvents();
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the version of the WiX extension file.
        /// </summary>
        /// <value>The version of the WiX extension file.</value>
        public Version Version
        {
            get
            {
                if (this.version == null)
                {
                    this.ExtractPropertiesFromFile();
                }

                return this.version;
            }
        }

        /// <summary>
        /// Gets the absolute path to the reference file.
        /// </summary>
        /// <value>The absolute path to the reference file.</value>
        public override string Url
        {
            get
            {
                string ret = base.Url;
                if (null != this.fileChangeListener)
                {
                    if (String.IsNullOrEmpty(this.observedFile))
                    {
                        // start watching the file
                        this.fileChangeListener.ObserveItem(ret);
                        this.observedFile = ret;
                    }
                    else if (!String.Equals(ret, this.observedFile))
                    {
                        // The URL changed. Watch the new file and stop
                        // watching the old one.
                        // This could happen if base.Url changes because the
                        // project reference path changed for example.
                        this.fileChangeListener.StopObservingItem(this.observedFile);
                        this.fileChangeListener.ObserveItem(ret);
                        this.observedFile = ret;
                    }
                }

                return ret;
            }
        }

        /// <summary>
        /// Lazy loads evaluated Wix Extension Directory value. Calling this property in the constructor 
        /// will result in returning empty value. 
        /// </summary>
        private string ExtensionDirectory
        {
            get
            {
                if (extensionDirectory == null)
                {
                    extensionDirectory = (string)this.ProjectMgr.GetProjectProperty(WixProjectFileConstants.WixExtDir);
                    if (extensionDirectory == null)
                    {
                        extensionDirectory = String.Empty;
                    }
                }

                return extensionDirectory;
            }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Validates that a reference can be added.
        /// </summary>
        /// <param name="errorHandler">A CannotAddReferenceErrorMessage delegate to show the error message.</param>
        /// <returns>true if the reference can be added.</returns>
        protected override bool CanAddReference(out CannotAddReferenceErrorMessage errorHandler)
        {
            if (!base.CanAddReference(out errorHandler))
            {
                return false;
            }

            errorHandler = null;
            if (!WixReferenceValidator.IsValidWixExtension(this.Url, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), this.ExtensionDirectory))
            {
                errorHandler = new CannotAddReferenceErrorMessage(this.ShowInvalidWixReferenceMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a reference is already added. The method parses all references and compares the filename.
        /// </summary>
        /// <returns>true if the extension reference has already been added.</returns>
        protected override bool IsAlreadyAdded(out ReferenceNode existingNode)
        {
            ReferenceContainerNode referencesFolder = this.ProjectMgr.FindChild(ReferenceContainerNode.ReferencesNodeVirtualName) as ReferenceContainerNode;
            Debug.Assert(referencesFolder != null, "Could not find the References node");

            string thisName = Path.GetFileNameWithoutExtension(this.ItemNode.Item.Xml.Include);
            for (HierarchyNode n = referencesFolder.FirstChild; n != null; n = n.NextSibling)
            {
                WixExtensionReferenceNode otherReference = n as WixExtensionReferenceNode;
                if (otherReference != null)
                {
                    string otherName = Path.GetFileNameWithoutExtension(otherReference.Url);
                    if (String.Equals(thisName, otherName, StringComparison.OrdinalIgnoreCase))
                    {
                        existingNode = otherReference;
                        return true;
                    }
                }
            }

            existingNode = null;
            return false;
        }

        /// <summary>
        /// Creates an object derived from <see cref="NodeProperties"/> that will be used to expose
        /// properties specific for this object to the property browser.
        /// </summary>
        /// <returns>A new <see cref="WixExtensionReferenceNodeProperties"/> object.</returns>
        protected override NodeProperties CreatePropertiesObject()
        {
            return new WixExtensionReferenceNodeProperties(this);
        }

        /// <summary>
        /// Replaces build properties in the path.
        /// </summary>
        /// <param name="path">Input path with build propeties.</param>
        /// <returns>Path with build properties evaluated and substituted.</returns>
        protected override string ReplacePropertiesInPath(string path)
        {
            path = WixHelperMethods.ReplaceBuildPropertyWithPath(path, ExtensionDirectoryToken, this.ExtensionDirectory);

            return base.ReplacePropertiesInPath(path);
        }

        /// <summary>
        /// Disposes the node
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            try
            {
                this.UnregisterFromFileChangeService();
            }
            finally
            {
                base.Dispose(disposing);
                this.isDisposed = true;
            }
        }

        /// <summary>
        /// Opens the wixlib file and read properties from the file.
        /// </summary>
        private void ExtractPropertiesFromFile()
        {
            if (!String.IsNullOrEmpty(this.Url) && File.Exists(this.Url))
            {
                try
                {
                    byte[] rawAssembly = File.ReadAllBytes(this.Url);
                    Assembly extensionAssembly = Assembly.ReflectionOnlyLoad(rawAssembly);
                    this.version = extensionAssembly.GetName().Version;
                    return;
                }
                catch (UnauthorizedAccessException e)
                {
                    CCITracing.Trace(e);
                }
                catch (FileLoadException e)
                {
                    CCITracing.Trace(e);
                }
                catch (BadImageFormatException e)
                {
                    CCITracing.Trace(e);
                }
                catch (IOException e)
                {
                    CCITracing.Trace(e);
                }
                catch (SecurityException e)
                {
                    CCITracing.Trace(e);
                }
            }

            this.version = new Version();
        }

        /// <summary>
        /// Registers with File change events
        /// </summary>
        private void InitializeFileChangeEvents()
        {
            if (null == this.fileChangeListener)
            {
                this.fileChangeListener = new FileChangeManager(this.ProjectMgr.Site);
                this.fileChangeListener.FileChangedOnDisk += this.OnAssemblyReferenceChangedOnDisk;
            }
        }

        /// <summary>
        /// Unregisters this node from file change notifications.
        /// </summary>
        private void UnregisterFromFileChangeService()
        {
            this.fileChangeListener.FileChangedOnDisk -= this.OnAssemblyReferenceChangedOnDisk;
            this.fileChangeListener.Dispose();
        }

        /// <summary>
        /// Event callback. Called when one of the assembly file is changed.
        /// </summary>
        /// <param name="sender">The FileChangeManager object.</param>
        /// <param name="e">Event args containing the file name that was updated.</param>
        private void OnAssemblyReferenceChangedOnDisk(object sender, FileChangedOnDiskEventArgs e)
        {
            Debug.Assert(e != null, "No event args specified for the FileChangedOnDisk event");

            // We only care about file deletes and adds so check before enumerating references.
            // We also need to watch time because a rename operation from an old name to the
            // watched name will generate this event (in which case we want to show that the
            // file now exists).
            if ((e.FileChangeFlag & (_VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Time)) == 0)
            {
                return;
            }

            if (Microsoft.VisualStudio.NativeMethods.IsSamePath(e.FileName, this.Url))
            {
                this.OnInvalidateItems(this.Parent);
            }
        }
    }
}
