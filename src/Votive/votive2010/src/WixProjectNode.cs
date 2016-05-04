// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

[assembly: System.CLSCompliant(true)]

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using MSBuild = Microsoft.Build.Evaluation;
    using Microsoft.Build.Execution;
    using Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Package.Automation;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
    using Utilities = Microsoft.VisualStudio.Package.Utilities;
    using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
    using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
    using VsMenus = Microsoft.VisualStudio.Package.VsMenus;

    /// <summary>
    /// Represents the root node of a WiX project within a Solution Explorer hierarchy.
    /// </summary>
    [Guid("D79D1001-AD43-4a1d-AFD6-B6CBBE6B816B")]
    [CLSCompliant(false)]
    public class WixProjectNode : ProjectNode
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        internal const string ProjectTypeName = "WiX";

        private Icon nodeIcon;

        private WixPackage package;
        private bool showAllFilesEnabled;

        private MSBuild.Project userBuildProject;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixProjectNode"/> class.
        /// </summary>
        /// <param name="package">The <see cref="WixPackage"/> to which this project belongs.</param>
        public WixProjectNode(WixPackage package)
        {
            WixHelperMethods.VerifyNonNullArgument(package, "package");

            this.package = package;

            // We allow destructive deletes on the project
            this.CanProjectDeleteItems = true;

            this.CanFileNodesHaveChilds = true;

            this.InitializeCATIDs();
        }

        // =========================================================================================
        // Events
        // =========================================================================================

        /// <summary>
        /// Notifies listeners (property pages) when the output type changes.
        /// </summary>
        internal event PropertyChangedEventHandler OutputTypeChanged;

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the current project configuration.
        /// </summary>
        public WixProjectConfig CurrentConfig
        {
            get
            {
                EnvDTE.Project automationObject = this.GetAutomationObject() as EnvDTE.Project;
                return new WixProjectConfig(this, Utilities.GetActiveConfigurationName(automationObject), Utilities.GetActivePlatformName(automationObject));
            }
        }

        /// <summary>
        /// Gets the index of the node's image in the image list.
        /// </summary>
        /// <value>The index of the node's image in the image list.</value>
        public override int ImageIndex
        {
            get { return NoImage; }
        }

        /// <summary>
        /// Gets or sets the output type of the project.
        /// </summary>
        /// <value>One of the <see cref="WixOutputType"/> values.</value>
        public WixOutputType OutputType
        {
            get
            {
                string outputTypeString = this.GetProjectProperty(WixProjectFileConstants.OutputType);
                WixOutputType outputType = WixOutputType.Package;

                try
                {
                    outputType = (WixOutputType)Enum.Parse(typeof(WixOutputType), outputTypeString, true);
                }
                catch (ArgumentException)
                {
                    // do nothing...
                }

                return outputType;
            }
        }

        /// <summary>
        /// Gets the project type GUID, which is registered with Visual Studio.
        /// </summary>
        /// <value>The project type GUID, which is registered with Visual Studio.</value>
        public override Guid ProjectGuid
        {
            get { return typeof(WixProjectFactory).GUID; }
        }

        /// <summary>
        /// Returns a caption for VSHPROPID_TypeName.
        /// </summary>
        /// <value>A caption for VSHPROPID_TypeName.</value>
        public override string ProjectType
        {
            get { return ProjectTypeName; }
        }

        /// <summary>
        /// Returns the MSBuild project associated with the .user file
        /// </summary>
        /// <value>The MSBuild project associated with the .user file.</value>
        public MSBuild.Project UserBuildProject
        {
            get 
            {
                if (this.userBuildProject == null && File.Exists(this.UserFileName))
                {
                    this.CreateUserBuildProject();
                }
                
                return this.userBuildProject; 
            }
        }

        /// <summary>
        /// Gets if the ShowAllFiles is enabled or not.
        /// </summary>
        /// <value>true if the ShowAllFiles option is enabled, false otherwise.</value>
        public bool ShowAllFilesEnabled
        {
            get
            {
                return this.showAllFilesEnabled;
            }
        }

        /// <summary>
        /// Gets the WixPackage instance for this project.
        /// </summary>
        /// <remarks>The SDK2008 base ProjectNode class has a Package property defined,
        /// but we can't use it (with the  single codebase) because it isn't in SDK2005.</remarks>
        internal WixPackage WixPackage
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Gets a flag indicating if the project uses the Project Designer Editor instead of the property page frame to edit project properties.
        /// </summary>
        /// <value>A flag indicating if the project uses the Project Designer Editor instead of the property page frame to edit project properties.</value>
        protected override bool SupportsProjectDesigner
        {
            get
            {
                // use the VS 2005 style property pages (project designer) instead of the old VS 2003 dialog
                return true;
            }
        }

        /// <summary>
        /// Gets the path to the .user file
        /// </summary>
        private string UserFileName
        {
            get { return this.FileName + ProjectNode.PerUserFileExtension; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Hides all of the tabs in the Add Reference dialog except for the browse tab, which will search for wixlibs.
        /// </summary>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int AddProjectReference()
        {
            try
            {
                this.ShowProjectInSolutionPage = false;
                return WixProjectReferenceNode.AddProjectReference(this);
            }
            finally
            {
                this.ShowProjectInSolutionPage = true;
            }
        }

        /// <summary>
        /// Gets a handle for the icon for the project node.
        /// </summary>
        /// <param name="open">True if hierarchy item is expanded.</param>
        /// <returns>The node icon handle.</returns>
        public override object GetIconHandle(bool open)
        {
            if (this.nodeIcon == null)
            {
                switch (this.OutputType)
                {
                    case WixOutputType.Bundle:
                        this.nodeIcon = new Icon(WixStrings.WixBundleProjectIcon, new Size(16, 16));
                        break;
                    case WixOutputType.Library:
                        this.nodeIcon = new Icon(WixStrings.WixLibraryProjectIcon, new Size(16, 16));
                        break;
                    case WixOutputType.Module:
                        this.nodeIcon = new Icon(WixStrings.WixMergeModuleProjectIcon, new Size(16, 16));
                        break;
                    default:
                        this.nodeIcon = new Icon(WixStrings.ProjectFileIcon, new Size(16, 16));
                        break;
                }
            }

            return this.nodeIcon.Handle;
        }

        /// <summary>
        /// Override adding a file to the project from a template. So that we can compute custom variables.
        /// </summary>
        /// <param name="source">Full path of template file</param>
        /// <param name="target">Full path of file once added to the project</param>
        public override void AddFileFromTemplate(string source, string target)
        {
            base.AddFileFromTemplate(source, target);

            string extension = Path.GetExtension(source);
            if (extension.Equals(".wxs", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".wxi", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".wxl", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string output = null;
                    using (StreamReader textStreamReader = new StreamReader(target))
                    {
                        output = textStreamReader.ReadToEnd();
                    }

                    string projectName = Path.GetFileNameWithoutExtension(this.Url);
                    string safeProjectName = Regex.Replace(projectName, @"[^A-Za-z0-9_\.]|\.{2,}", "_"); // replace illegal characters with "_".

                    // MSI identifiers must begin with an alphabetic character or an
                    // underscore. Prefix all other values with an underscore.
                    if (Regex.IsMatch(safeProjectName, @"^[^a-zA-Z_]"))
                    {
                        safeProjectName = String.Concat("_", safeProjectName);
                    }

                    output = output.Replace("$wixsafeprojectname$", safeProjectName);

                    using (StreamWriter writer = new StreamWriter(target))
                    {
                        writer.Write(output);
                        writer.Close();
                    }
                }
                catch (IOException e)
                {
                    Trace.WriteLine("Exception : " + e.Message);
                }
                catch (UnauthorizedAccessException e)
                {
                    Trace.WriteLine("Exception : " + e.Message);
                }
                catch (ArgumentException e)
                {
                    Trace.WriteLine("Exception : " + e.Message);
                }
                catch (NotSupportedException e)
                {
                    Trace.WriteLine("Exception : " + e.Message);
                }
            }
        }

        /// <summary>
        /// Create a file node based on an MSBuild item.
        /// </summary>
        /// <param name="item">MSBuild item</param>
        /// <returns>The added <see cref="FileNode"/>.</returns>
        public override FileNode CreateFileNode(ProjectElement item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return new WixFileNode(this, item, item.IsVirtual);
        }

        /// <summary>
        /// Creates an MSBuild project to be associated with the .user location specific build file.
        /// </summary>
        public void CreateUserBuildProject()
        {
            if (File.Exists(this.UserFileName))
            {
                // Create the project from an XmlReader so that this file is
                // not checked for being dirty when closing the project.
                // If loaded directly from the file, Visual Studio will display
                // a save changes dialog if any changes are made to the user
                // project since it will have been added to the global project
                // collection. Loading from an XmlReader will prevent the 
                // project from being added to the global project collection
                // and thus prevent the save changes dialog on close.
                System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(this.UserFileName);
                this.userBuildProject = new MSBuild.Project(xmlReader);
            }
            else
            {
                this.userBuildProject = new MSBuild.Project();
            }
        }

        /// <summary>
        /// Returns a value indicating whether the specified file is a code file, i.e. compileable.
        /// </summary>
        /// <param name="fileName">The file to check.</param>
        /// <returns><see langword="true"/> if the file is compileable; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "In the 2005 SDK, it's called strFileName and in the 2008 SDK it's fileName.")]
        public override bool IsCodeFile(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            return String.Equals(".wxs", extension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a value indicating whether the specified file is an embedded resource.
        /// </summary>
        /// <param name="fileName">The file to check.</param>
        /// <returns><see langword="true"/> if the file is an embedded resource; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "In the 2005 SDK, it's called strFileName and in the 2008 SDK it's fileName.")]
        public override bool IsEmbeddedResource(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            return String.Equals(extension, ".wxl", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets the value of an MSBuild project property.
        /// </summary>
        /// <param name="propertyName">The name of the property to change.</param>
        /// <param name="propertyValue">The value to assign the property.</param>
        public override void SetProjectProperty(string propertyName, string propertyValue)
        {
            this.SetProjectProperty(propertyName, propertyValue, null);
        }

        /// <summary>
        /// Filter items that should not be processed as file items. Example: Folders and References.
        /// </summary>
        /// <param name="itemType">The type of the item being added.</param>
        /// <returns></returns>
        protected override bool FilterItemTypeToBeAddedToHierarchy(string itemType)
        {
            return (String.Compare(itemType, WixProjectFileConstants.WixExtension, StringComparison.OrdinalIgnoreCase) == 0) ||
                (String.Compare(itemType, WixProjectFileConstants.WixLibrary, StringComparison.OrdinalIgnoreCase) == 0) ||
                base.FilterItemTypeToBeAddedToHierarchy(itemType);
        }

        /// <summary>
        /// Called to save the project file
        /// </summary>
        /// <param name="fileToBeSaved">Name for the project file.</param>
        /// <param name="remember">Persist the dirty state after the save.</param>
        /// <param name="formatIndex">Format index</param>
        /// <returns>Native HRESULT</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "We cannot comply with FxCop and StyleCop simultaneously.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#", Justification = "We cannot comply with FxCop and StyleCop simultaneously.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#", Justification = "We cannot comply with FxCop and StyleCop simultaneously.")]
        public override int Save(string fileToBeSaved, int remember, uint formatIndex)
        {
            int result = base.Save(fileToBeSaved, remember, formatIndex);
            if (NativeMethods.S_OK == result && this.userBuildProject != null)
            {
                this.userBuildProject.Save(this.UserFileName);
            }

            return result;
        }

		/// <summary>
        /// Sets the value of an MSBuild project property.
        /// </summary>
        /// <param name="propertyName">The name of the property to change.</param>
        /// <param name="propertyValue">The value to assign the property.</param>
        /// <param name="condition">The condition to use on the property. Corresponds to the Condition attribute of the Property element.</param>
        public void SetProjectProperty(string propertyName, string propertyValue, string condition)
        {
            WixHelperMethods.VerifyStringArgument(propertyName, "propertyName");

            if (propertyValue == null)
            {
                propertyValue = String.Empty;
            }

            // see if the value is the same as what's already in the project so we
            // know whether to actually mark the project file dirty or not
            string oldValue = this.GetProjectProperty(propertyName, true);

            if (!String.Equals(oldValue, propertyValue, StringComparison.Ordinal))
            {
                // check out the project file
                if (this.ProjectMgr != null && !this.ProjectMgr.QueryEditProjectFile(false))
                {
                    throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
                }

				// Use condition! 
                this.BuildProject.SetProperty(propertyName, propertyValue); //, condition);

                // refresh the cached values
                this.SetCurrentConfiguration();
                this.SetProjectFileDirty(true);
            }
        }

        /// <summary>
        /// Override the automation object
        /// </summary>
        /// <returns>The DTE automation object for the WiX project</returns>
        public override object GetAutomationObject()
        {
            return new OAWixProject(this);
        }

        /// <summary>
        /// Converts the path to relative (if it is absolute) to the project folder.
        /// </summary>
        /// <param name="path">Path to be made relative.</param>
        /// <returns>Path relative to the project folder.</returns>
        public virtual string GetRelativePath(string path)
        {
            return WixHelperMethods.GetRelativePath(this.ProjectFolder, path);
        }

        /// <summary>
        /// Closes the project node.
        /// </summary>
        /// <returns>A success or failure value.</returns>
        public override int Close()
        {
            int result = base.Close();

            if (this.UserBuildProject != null && this.UserBuildProject.IsDirty)
            {
                this.UserBuildProject.Save(this.UserFileName);
            }

            return result;
        }

        /// <summary>
        /// Creates and returns the ProjectElement for a file item.
        /// </summary>
        /// <param name="file">Path of the file.</param>
        /// <returns>ProjectElement for the file item.</returns>
        internal ProjectElement CreateMsBuildFileProjectElement(string file)
        {
            return this.AddFileToMsBuild(file);
        }

        /// <summary>
        /// Creates and returns the ProjectElement for a folder item.
        /// </summary>
        /// <param name="folder">Path of the folder.</param>
        /// <returns>ProjectElement for the folder item.</returns>
        internal ProjectElement CreateMsBuildFolderProjectElement(string folder)
        {
            return this.AddFolderToMsBuild(folder);
        }

        /// <summary>
        /// This is similar to QueryStatusOnNode method but it is internal so that others within the assembley can call
        /// it.
        /// </summary>
        /// <param name="guidCmdGroup">A unique identifier of the command group. The pguidCmdGroup parameter can be NULL to specify the standard group.</param>
        /// <param name="cmd">The command to query status for.</param>
        /// <param name="result">An out parameter specifying the QueryStatusResult of the command.</param>
        /// <returns>It returns true if succeeded, false otherwise.</returns>
        internal bool QueryStatusOnProjectNode(Guid guidCmdGroup, uint cmd, ref QueryStatusResult result)
        {
            if (guidCmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                if ((VsCommands2K)cmd == VsCommands2K.SHOWALLFILES)
                {
                    result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                    if (this.showAllFilesEnabled)
                    {
                        result |= QueryStatusResult.LATCHED;
                    }

                    return true; // handled.
                }
            }

            return false; // not handled.
        }

        /// <summary>
        /// Called to fire a PropertyChangedEvent after the project output type changed.
        /// </summary>
        internal void OnOutputTypeChanged()
        {
            // Refresh the icon in the Solution Explorer.
            this.nodeIcon = null;
            this.ReDraw(UIHierarchyElement.Icon);

            if (this.OutputTypeChanged != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(WixProjectFileConstants.OutputType);
                this.OutputTypeChanged(this, e);
            }
        }

        /// <summary>
        /// Sets the configuration for the .user build file
        /// </summary>
        /// <param name="configCanonicalName">Configuration</param>
        protected internal override void SetConfiguration(ConfigCanonicalName configCanonicalName)
        {
            base.SetConfiguration(configCanonicalName);
            if (this.userBuildProject != null)
            {
                this.userBuildProject.SetGlobalProperty(ProjectFileConstants.Configuration, configCanonicalName.ConfigName);
                this.userBuildProject.SetGlobalProperty(ProjectFileConstants.Platform, configCanonicalName.MSBuildPlatform);
            }
        }

        /// <summary>
        /// Creates and returns the folder node object for Wix projects.
        /// </summary>
        /// <param name="path">Folder path.</param>
        /// <param name="element">MSBuild element.</param>
        /// <returns>Returns newly created Folder Node object.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#")]
        protected internal override FolderNode CreateFolderNode(string path, ProjectElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return new WixFolderNode(this, path, element, element.IsVirtual);
        }

        /// <summary>
        /// Enables / Disables the ShowAllFileMode.
        /// </summary>
        /// <returns>S_OK if it's possible to toggle the state, OLECMDERR_E_NOTSUPPORTED if not</returns>
        protected internal override int ShowAllFiles()
        {
            int result = this.ToggleShowAllFiles();

            if (result != NativeMethods.S_OK)
            {
                return result;
            }

            // Make sure that .user file is there. if not, create one.
            if (this.UserBuildProject == null)
            {
                this.CreateUserBuildProject();
            }

            // Save Project view in .user file.
            this.UserBuildProject.SetProperty(
                WixProjectFileConstants.ProjectView,
                (this.showAllFilesEnabled ? WixProjectFileConstants.ShowAllFiles : WixProjectFileConstants.ProjectFiles));

            return result;
        }

        /// <summary>
        /// Toggles the state of Show all files
        /// </summary>
        /// <returns>S_OK if it's possible to toggle the state, OLECMDERR_E_NOTSUPPORTED if not</returns>
        protected internal int ToggleShowAllFiles()
        {
            if (this.ProjectMgr == null || this.ProjectMgr.IsClosed)
            {
                return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
            }

            using (WixHelperMethods.NewWaitCursor())
            {
                this.showAllFilesEnabled = !this.showAllFilesEnabled; // toggle the flag

                if (this.showAllFilesEnabled)
                {
                    WixProjectMembers.AddNonMemberItems(this);
                }
                else
                {
                    WixProjectMembers.RemoveNonMemberItems(this);
                }
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Factory method for configuration provider.
        /// </summary>
        /// <returns>Configuration provider created.</returns>
        protected override ConfigProvider CreateConfigProvider()
        {
            return new WixConfigProvider(this);
        }

        /// <summary>
        /// Reload project from project file
        /// </summary>
        protected override void Reload()
        {
            base.Reload();

            // read .user file
            if (this.UserBuildProject != null)
            {
                // Read show all files flag
                string propertyValue = this.UserBuildProject.GetPropertyValue(WixProjectFileConstants.ProjectView);
                if (String.Equals(propertyValue, WixProjectFileConstants.ShowAllFiles, StringComparison.OrdinalIgnoreCase))
                {
                    this.ToggleShowAllFiles();
                }
            }
        }

        /// <summary>
        /// This method helps converting any non member node into the member one.
        /// </summary>
        /// <param name="node">Node to be added.</param>
        /// <returns>Returns the result of the conversion.</returns>
        /// <remarks>This method helps including the non-member items into the project when ShowAllFiles option is enabled.
        /// Normally, the project ignores "Add Existing Item" command if it is in ShowAllFiles mode and the non-member node
        /// exists for the item being added. Overriden to alter this behavior (now it includes the non-member node in the
        /// project)</remarks>
        protected override VSADDRESULT IncludeExistingNonMemberNode(HierarchyNode node)
        {
            IProjectSourceNode sourceNode = node as IProjectSourceNode;
            if (sourceNode != null && sourceNode.IsNonMemberItem)
            {
                if (sourceNode.IncludeInProject() == VSConstants.S_OK)
                {
                    return VSADDRESULT.ADDRESULT_Success;
                }
            }

            return base.IncludeExistingNonMemberNode(node);
        }

        /// <summary>
        /// Handle owerwriting of an existing item in the hierarchy.
        /// </summary>
        /// <param name="existingNode">The node that exists.</param>
        protected override void OverwriteExistingItem(HierarchyNode existingNode)
        {
            IProjectSourceNode sourceNode = existingNode as IProjectSourceNode;
            if (sourceNode != null && sourceNode.IsNonMemberItem)
            {
                sourceNode.IncludeInProject();
            }
            else
            {
                base.OverwriteExistingItem(existingNode);
            }
        }

        /// <summary>
        /// Adds a file to the MSBuild project.
        /// </summary>
        /// <param name="file">The file to be added.</param>
        /// <returns>A <see cref="ProjectElement"/> describing the newly added file.</returns>
        protected override ProjectElement AddFileToMsBuild(string file)
        {
            ProjectElement newItem;

            string itemPath = PackageUtilities.MakeRelativeIfRooted(file, this.BaseURI);

            if (this.IsCodeFile(itemPath))
            {
                newItem = this.CreateMsBuildFileItem(itemPath, ProjectFileConstants.Compile);
            }
            else if (this.IsEmbeddedResource(itemPath))
            {
                newItem = this.CreateMsBuildFileItem(itemPath, ProjectFileConstants.EmbeddedResource);
            }
            else
            {
                newItem = this.CreateMsBuildFileItem(itemPath, ProjectFileConstants.Content);
            }

            return newItem;
        }

        /// <summary>
        /// Creates an object derived from <see cref="NodeProperties"/> that will be used to expose
        /// properties specific for this object to the property browser.
        /// </summary>
        /// <returns>A new <see cref="WixProjectNodeProperties"/> object.</returns>
        protected override NodeProperties CreatePropertiesObject()
        {
            return new WixProjectNodeProperties(this);
        }

        /// <summary>
        /// Creates a type-specific <see cref="WixReferenceContainerNode"/> for the project.
        /// </summary>
        /// <returns>A new <see cref="WixReferenceContainerNode"/> instance.</returns>
        protected override ReferenceContainerNode CreateReferenceContainerNode()
        {
            return new WixReferenceContainerNode(this);
        }

        /// <summary>
        /// Gets an array of property page GUIDs that are configuration dependent.
        /// </summary>
        /// <returns>An array of property page GUIDs that are configuration dependent.</returns>
        protected override Guid[] GetConfigurationDependentPropertyPages()
        {
            Guid[] result = new Guid[]
            {
                typeof(WixBuildPropertyPage).GUID,
                typeof(WixToolsSettingsPropertyPage).GUID,
            };

            return result;
        }

        /// <summary>
        /// Gets an array of property page GUIDs that are common, or not dependent upon the configuration.
        /// </summary>
        /// <returns>An array of property page GUIDs that are independent upon the configuration.</returns>
        protected override Guid[] GetConfigurationIndependentPropertyPages()
        {
            Guid[] result = new Guid[]
            {
                typeof(WixInstallerPropertyPage).GUID,
                typeof(WixPathsPropertyPage).GUID,
                typeof(WixBuildEventsPropertyPage).GUID,
            };

            return result;
        }

        /// <summary>
        /// Initialize common project properties with default value if they are empty.
        /// </summary>
        /// <remarks>
        /// The following common project properties are set to default values: OutputName.
        /// </remarks>
        protected override void InitializeProjectProperties()
        {
            if (String.IsNullOrWhiteSpace(this.GetProjectProperty(WixProjectFileConstants.OutputName)))
            {
                string projectName = Path.GetFileNameWithoutExtension(this.FileName);
                this.SetProjectProperty(WixProjectFileConstants.OutputName, projectName);
            }
        }

        /// <summary>
        /// Executes an MSBuild target.
        /// </summary>
        /// <param name="target">Name of the MSBuild target to execute.</param>
        /// <returns>Result from executing the target (success/failure).</returns>
        protected override Microsoft.VisualStudio.Package.BuildResult InvokeMsBuild(string target)
        {
            WixBuildMacroCollection.DefineSolutionProperties(this);
            WixBuildMacroCollection.DefineProjectReferenceConfigurations(this);
            return base.InvokeMsBuild(target);
        }

        /// <summary>
        /// Handles menus originating from IOleCommandTarget.
        /// </summary>
        /// <param name="cmdGroup">Unique identifier of the command group</param>
        /// <param name="cmd">The command to be executed.</param>
        /// <param name="handled">Specifies whether the menu was handled.</param>
        /// <returns>A QueryStatusResult describing the status of the menu.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "In the 2005 SDK, it's called guidCmdGroup and in the 2008 SDK it's cmdGroup.")]
        protected override QueryStatusResult QueryStatusCommandFromOleCommandTarget(Guid cmdGroup, uint cmd, out bool handled)
        {
            if (cmdGroup == WixVsConstants.GuidGenerateCodeMetrics &&
                (cmd == WixVsConstants.CommandGenerateCodeMetricsContextMenu || cmd == WixVsConstants.CommandGenerateCodeMetricsAnalyzeMenu))
            {
                handled = true;
                return QueryStatusResult.INVISIBLE | QueryStatusResult.NOTSUPPORTED;
            }

            if (cmdGroup == VsMenus.guidStandardCommandSet97)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.Refresh:
                        handled = true;
                        return QueryStatusResult.ENABLED;

                    case VsCommands.ProjectProperties:
                        return base.QueryStatusCommandFromOleCommandTarget(cmdGroup, cmd, out handled);
                }
            }

            if (cmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                switch ((VsCommands2K)cmd)
                {
                    case VsCommands2K.SLNREFRESH:
                    case (VsCommands2K)WixVsConstants.CommandExploreFolderInWindows:
                        handled = true;
                        return QueryStatusResult.ENABLED | QueryStatusResult.SUPPORTED;

                    case VsCommands2K.PROJSTARTDEBUG:
                    case VsCommands2K.PROJSTEPINTO:
                        handled = true;
                        return QueryStatusResult.INVISIBLE | QueryStatusResult.SUPPORTED;
                }
            }

            if (cmdGroup == WixVsConstants.GuidRefreshToolbox)
            {
                switch (cmd)
                {
                    case WixVsConstants.CommandRefreshToolbox:
                        handled = true;
                        return QueryStatusResult.INVISIBLE | QueryStatusResult.SUPPORTED;
                }
            }

            return base.QueryStatusCommandFromOleCommandTarget(cmdGroup, cmd, out handled);
        }

        /// <summary>
        /// Handles command status on the project node. If a command cannot be handled then the base should be called.
        /// </summary>
        /// <param name="cmdGroup">A unique identifier of the command group. The pguidCmdGroup parameter can be NULL to specify the standard group.</param>
        /// <param name="cmd">The command to query status for.</param>
        /// <param name="pCmdText">Pointer to an OLECMDTEXT structure in which to return the name and/or status information of a single command. Can be NULL to indicate that the caller does not require this information.</param>
        /// <param name="result">An out parameter specifying the QueryStatusResult of the command.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "In the 2005 SDK, it's called guidCmdGroup and in the 2008 SDK it's cmdGroup")]
        protected override int QueryStatusOnNode(Guid cmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
        {
            if (cmdGroup == VsMenus.guidStandardCommandSet97)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.Copy:
                        result = QueryStatusResult.NOTSUPPORTED;
                        return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;

                    case VsCommands.ProjectProperties:
                        // Sets the menu command text to 'Projname &Properties' where Projname is the name of this project
                        string propertiesMenuCommandText = String.Format(CultureInfo.CurrentUICulture, WixStrings.ProjectPropertiesCommand, Path.GetFileNameWithoutExtension(this.ProjectFile));
                        NativeMethods.OLECMDTEXT.SetText(pCmdText, propertiesMenuCommandText);
                        result = QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                        return (int)VSConstants.S_OK;

                    case VsCommands.SetStartupProject:
                    case VsCommands.ToggleBreakpoint:
                        result = QueryStatusResult.SUPPORTED | QueryStatusResult.INVISIBLE;
                        return (int)VSConstants.S_OK;
                }
            }
            
            if (this.QueryStatusOnProjectNode(cmdGroup, cmd, ref result))
            {
                return VSConstants.S_OK;
            }

            return base.QueryStatusOnNode(cmdGroup, cmd, pCmdText, ref result);
        }

        /// <summary>
        /// Handles command execution.
        /// </summary>
        /// <param name="cmdGroup">Unique identifier of the command group</param>
        /// <param name="cmd">The command to be executed.</param>
        /// <param name="cmdexecopt">Values describe how the object should execute the command.</param>
        /// <param name="pvaIn">Pointer to a VARIANTARG structure containing input arguments. Can be NULL</param>
        /// <param name="pvaOut">VARIANTARG structure to receive command output. Can be NULL.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "We cannot comply with FxCop and StyleCop simultaneously.")]
        protected override int ExecCommandOnNode(Guid cmdGroup, uint cmd, uint cmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (cmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                switch ((VsCommands2K)cmd)
                {
                    case (VsCommands2K)WixVsConstants.CommandExploreFolderInWindows:
                        WixHelperMethods.ExploreFolderInWindows(this.ProjectFolder);
                        return VSConstants.S_OK;

                    case VsCommands2K.SLNREFRESH:
                        WixHelperMethods.RefreshProject(this);
                        return VSConstants.S_OK;
                }
            }

            if (cmdGroup == VsMenus.guidStandardCommandSet97)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.Refresh:
                        WixHelperMethods.RefreshProject(this);
                        return VSConstants.S_OK;
                    case VsCommands.F1Help:
                        // Prevent VS from showing keyword help
                        return VSConstants.S_OK;
                }
            }

            return base.ExecCommandOnNode(cmdGroup, cmd, cmdexecopt, pvaIn, pvaOut);
        }

        /// <summary>
        /// Determines the order in which the property pages are shown
        /// </summary>
        /// <returns>GUIDs of the property pages in the desired order</returns>
        protected override Guid[] GetPriorityProjectDesignerPages()
        {
            Guid[] result = new Guid[]
            {
                typeof(WixInstallerPropertyPage).GUID,
                typeof(WixBuildPropertyPage).GUID,
                typeof(WixBuildEventsPropertyPage).GUID,
                typeof(WixPathsPropertyPage).GUID,
                typeof(WixToolsSettingsPropertyPage).GUID,
            };

            return result;
        }

        /// <summary>
        /// Renames the project file
        /// </summary>
        /// <param name="newFile">The new name for the project file.</param>
        protected override void RenameProjectFile(string newFile)
        {
            string oldUserFileName = this.UserFileName;
            base.RenameProjectFile(newFile);

            if (this.UserBuildProject != null)
            {
                File.Move(oldUserFileName, this.UserFileName);
            }
        }

        /// <summary>
        /// Provide mapping from our browse objects and automation objects to our CATIDs.
        /// </summary>
        private void InitializeCATIDs()
        {
            // The following properties classes are specific to wix so we can use their GUIDs directly
            this.AddCATIDMapping(typeof(WixFileNodeProperties), typeof(WixFileNodeProperties).GUID);
            this.AddCATIDMapping(typeof(WixProjectNodeProperties), typeof(WixProjectNodeProperties).GUID);
            this.AddCATIDMapping(typeof(WixExtensionReferenceNodeProperties), typeof(WixExtensionReferenceNodeProperties).GUID);
            this.AddCATIDMapping(typeof(WixLibraryReferenceNodeProperties), typeof(WixLibraryReferenceNodeProperties).GUID);

            // The following are not specific to wix and as such we need a separate GUID (we simply used guidgen.exe to create new guids)
            this.AddCATIDMapping(typeof(FolderNodeProperties), new Guid("86AD5EAF-5629-4fe3-8F8E-E1D9DA0A81C3"));
            this.AddCATIDMapping(typeof(FileNodeProperties), new Guid("BC59A9F0-9C85-44f9-B133-4A019BDC9739"));
        }
    }
}
