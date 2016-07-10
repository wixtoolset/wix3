// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
    using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
    using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
    using VsPkgMenus = Microsoft.VisualStudio.Package.VsMenus;

    /// <summary>
    /// Represents a file node (Wix or non-Wix) in a Wix project.
    /// </summary>
    [CLSCompliant(false)]
    public class WixFileNode : FileNode, IProjectSourceNode
    {
        // =========================================================================================
        // Member variables
        // =========================================================================================

        /// <summary>
        /// Flag that indicates if this folder is not a member of the project.
        /// </summary>
        private bool isNonMemberItem;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixFileNode"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="WixProjectNode"/> that contains this node.</param>
        /// <param name="element">The element that contains MSBuild properties.</param>
        public WixFileNode(WixProjectNode root, ProjectElement element)
            : this(root, element, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WixFileNode"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="WixProjectNode"/> that contains this node.</param>
        /// <param name="element">The element that contains MSBuild properties.</param>
        /// <param name="isNonMemberItem">Flag that indicates if the file is not part of the project.</param>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "NonMember")]
        public WixFileNode(WixProjectNode root, ProjectElement element, bool isNonMemberItem)
            : base(root, element)
        {
            this.isNonMemberItem = isNonMemberItem;
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets an index into the default <b>ImageList</b> of the icon to show for this file.
        /// </summary>
        /// <value>An index into the default  <b>ImageList</b> of the icon to show for this file.</value>
        public override int ImageIndex
        {
            get
            {
                if (this.IsNonMemberItem)
                {
                    return (int)ProjectNode.ImageName.ExcludedFile;
                }
                else if (!File.Exists(this.Url))
                {
                    return (int)ProjectNode.ImageName.MissingFile;
                }

                return base.ImageIndex;
            }
        }

        /// <summary>
        /// Menu Command Id for Wix File item.
        /// </summary>
        /// <value>Menu Command Id for Wix File item.</value>
        public override int MenuCommandId
        {
            get
            {
                if (this.IsNonMemberItem)
                {
                    return VsPkgMenus.IDM_VS_CTXT_XPROJ_MULTIITEM;
                }

                return base.MenuCommandId;
            }
        }

        /// <summary>
        /// Specifies if a Node is under source control.
        /// </summary>
        /// <value>Specifies if a Node is under source control.</value>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Scc")]
        public override bool ExcludeNodeFromScc
        {
            get
            {
                // Non member items donot participate in SCC.
                if (this.IsNonMemberItem)
                {
                    return true;
                }

                return base.ExcludeNodeFromScc;
            }

            set
            {
                base.ExcludeNodeFromScc = value;
            }
        }

        // =========================================================================================
        // IProjectSourceNode Properties
        // =========================================================================================

        /// <summary>
        /// Flag that indicates if this node is not a member of the project.
        /// </summary>
        /// <value>true if the item is not a member of the project build, false otherwise.</value>
        public bool IsNonMemberItem
        {
            get
            {
                return this.isNonMemberItem;
            }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Sets the node property.
        /// </summary>
        /// <param name="propid">Property id.</param>
        /// <param name="value">Property value.</param>
        /// <returns>Returns success or failure code.</returns>
        public override int SetProperty(int propid, object value)
        {
            int result;
            __VSHPROPID id = (__VSHPROPID)propid;
            switch (id)
            {
                case __VSHPROPID.VSHPROPID_IsNonMemberItem:
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }

                    bool boolValue;
                    CCITracing.TraceCall(this.ID + "," + id.ToString());
                    if (Boolean.TryParse(value.ToString(), out boolValue))
                    {
                        this.isNonMemberItem = boolValue;

                        // Reset exclude from scc
                        this.ExcludeNodeFromScc = this.IsNonMemberItem;
                    }
                    else
                    {
                        WixHelperMethods.TraceFail("Could not parse the IsNonMemberItem property value.");
                    }

                    result = VSConstants.S_OK;
                    break;

                default:
                    result = base.SetProperty(propid, value);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets the node property.
        /// </summary>
        /// <param name="propId">Property id.</param>
        /// <returns>The property value.</returns>
        public override object GetProperty(int propId)
        {
            __VSHPROPID id = (__VSHPROPID)propId;
            switch (id)
            {
                case __VSHPROPID.VSHPROPID_IsNonMemberItem:
                    return this.IsNonMemberItem;
            }

            return base.GetProperty(propId);
        }

        /// <summary>
        /// Provides the node name for inline editing of caption. 
        /// Overriden to diable this fuctionality for non member fodler node.
        /// </summary>
        /// <returns>Caption of the file node if the node is a member item, null otherwise.</returns>
        public override string GetEditLabel()
        {
            if (this.IsNonMemberItem)
            {
                return null;
            }

            return base.GetEditLabel();
        }

        /// <summary>
        /// Exclude the item from the project system.
        /// </summary>
        /// <returns>Returns success or failure code.</returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        int IProjectSourceNode.ExcludeFromProject()
        {
            if (this.ProjectMgr == null || this.ProjectMgr.IsClosed)
            {
                return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
            }
            else if (this.IsNonMemberItem)
            {
                return VSConstants.S_OK; // do nothing, just ignore it.
            }

            using (WixHelperMethods.NewWaitCursor())
            {
                // Ask Document tracker listeners if we can remove the item.
                { // just to limit the scope.
                    string documentToRemove = this.GetMkDocument();
                    string[] filesToBeDeleted = new string[1] { documentToRemove };
                    VSQUERYREMOVEFILEFLAGS[] queryRemoveFlags = this.GetQueryRemoveFileFlags(filesToBeDeleted);
                    if (!this.ProjectMgr.Tracker.CanRemoveItems(filesToBeDeleted, queryRemoveFlags))
                    {
                        return (int)OleConstants.OLECMDERR_E_CANCELED;
                    }

                    // Close the document if it has a manager.
                    DocumentManager manager = this.GetDocumentManager();
                    if (manager != null)
                    {
                        if (manager.Close(__FRAMECLOSE.FRAMECLOSE_PromptSave) == VSConstants.E_ABORT)
                        {
                            // User cancelled operation in message box.
                            return VSConstants.OLE_E_PROMPTSAVECANCELLED;
                        }
                    }

                    // Check out the project file.
                    if (!this.ProjectMgr.QueryEditProjectFile(false))
                    {
                        throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
                    }
                }

                // close the document window if open.
                this.CloseDocumentWindow(this);

                WixProjectNode projectNode = this.ProjectMgr as WixProjectNode;

                if (projectNode != null && projectNode.ShowAllFilesEnabled && File.Exists(this.Url))
                {
                    // need to store before removing the node.
                    string url = this.Url;
                    string include = this.ItemNode.GetMetadata(ProjectFileConstants.Include);

                    this.ItemNode.RemoveFromProjectFile();
                    this.ProjectMgr.Tracker.OnItemRemoved(url, VSREMOVEFILEFLAGS.VSREMOVEFILEFLAGS_NoFlags);
                    this.SetProperty((int)__VSHPROPID.VSHPROPID_IsNonMemberItem, true); // Set it as non member item
                    this.ItemNode = new ProjectElement(this.ProjectMgr, null, true); // now we have to set a new ItemNode to indicate that this is virtual node.
                    this.ItemNode.Rename(include);
                    this.ItemNode.SetMetadata(ProjectFileConstants.Name, url);

                    ////this.ProjectMgr.OnItemAdded(this.Parent, this);
                    this.ReDraw(UIHierarchyElement.Icon); // We have to redraw the icon of the node as it is now not a member of the project and should be drawn using a different icon.
                    this.ReDraw(UIHierarchyElement.SccState); // update the SCC state icon.
                }
                else if (this.Parent != null) // the project node has no parentNode
                {
                    // Remove from the Hierarchy
                    this.OnItemDeleted();
                    this.Parent.RemoveChild(this);
                    this.ItemNode.RemoveFromProjectFile();
                }

                this.ResetProperties();

                // refresh property browser...
                WixHelperMethods.RefreshPropertyBrowser();
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Include the item into the project system.
        /// </summary>
        /// <returns>Returns success or failure code.</returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        int IProjectSourceNode.IncludeInProject()
        {
            WixProjectNode projectNode = this.ProjectMgr as WixProjectNode;
            if (projectNode == null || projectNode.IsClosed)
            {
                return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
            }
            else if (!this.IsNonMemberItem)
            {
                return VSConstants.S_OK; // do nothing, just ignore it.
            }

            using (WixHelperMethods.NewWaitCursor())
            {
                // Check out the project file.
                if (!projectNode.QueryEditProjectFile(false))
                {
                    throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
                }

                // make sure that all parent folders are included in the project
                WixHelperMethods.EnsureParentFolderIncluded(this);

                // now add this node to the project.
                this.SetProperty((int)__VSHPROPID.VSHPROPID_IsNonMemberItem, false);
                this.ItemNode = projectNode.CreateMsBuildFileProjectElement(this.Url);
                this.ProjectMgr.Tracker.OnItemAdded(this.Url, VSADDFILEFLAGS.VSADDFILEFLAGS_NoFlags);

                // notify others
                ////projectNode.OnItemAdded(this.Parent, this);
                this.ReDraw(UIHierarchyElement.Icon); // We have to redraw the icon of the node as it is now a member of the project and should be drawn using a different icon.
                this.ReDraw(UIHierarchyElement.SccState); // update the SCC state icon.

                this.ResetProperties();

                // refresh property browser...
                WixHelperMethods.RefreshPropertyBrowser();
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Include the item into the project system recursively.
        /// </summary>
        /// <param name="recursive">Flag that indicates if the inclusion should be recursive or not.</param>
        /// <returns>Returns success or failure code.</returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        int IProjectSourceNode.IncludeInProject(bool recursive)
        {
            // recursive doesn't make any sense in case of a file item. so just include this item.
            return ((IProjectSourceNode)this).IncludeInProject();
        }

        /// <summary>
        /// Creates an object derived from <see cref="NodeProperties"/> that will be used to expose
        /// properties specific for this object to the property browser.
        /// </summary>
        /// <returns>A new <see cref="WixFileNodeProperties"/> object.</returns>
        protected override NodeProperties CreatePropertiesObject()
        {
            if (this.IsNonMemberItem)
            {
                return new WixFileNodeNonMemberProperties(this);
            }
            else if (!String.IsNullOrEmpty(this.ItemNode.GetMetadata("Link")))
            {
                return new WixLinkedFileNodeProperties(this);
            }
            else
            {
                return new WixFileNodeProperties(this);
            }
        }

        /// <summary>
        /// Handles command status on a node. Should be overridden by descendant nodes. If a command cannot be handled then the base should be called.
        /// </summary>
        /// <param name="guidCmdGroup">A unique identifier of the command group. The pguidCmdGroup parameter can be NULL to specify the standard group.</param>
        /// <param name="cmd">The command to query status for.</param>
        /// <param name="pCmdText">Pointer to an OLECMDTEXT structure in which to return the name and/or status information of a single command. Can be NULL to indicate that the caller does not require this information.</param>
        /// <param name="result">An out parameter specifying the QueryStatusResult of the command.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Suppressing to avoid conflict with style cop.")]
        protected override int QueryStatusOnNode(Guid guidCmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
        {
            if (VsPkgMenus.guidStandardCommandSet97 == guidCmdGroup && this.IsNonMemberItem)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.ViewCode:
                        result = QueryStatusResult.NOTSUPPORTED;
                        return (int)OleConstants.MSOCMDERR_E_NOTSUPPORTED;
                }
            }

            int returnCode;
            if (WixHelperMethods.QueryStatusOnProjectSourceNode(this, guidCmdGroup, cmd, ref result, out returnCode))
            {
                return returnCode;
            }

            return base.QueryStatusOnNode(guidCmdGroup, cmd, pCmdText, ref result);
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
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#", Justification = "Suppressing to avoid conflict with style cop.")]
        protected override int ExecCommandOnNode(Guid cmdGroup, uint cmd, uint cmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (cmdGroup == VsPkgMenus.guidStandardCommandSet2K)
            {
                switch ((VsCommands2K)cmd)
                {
                    case VsCommands2K.INCLUDEINPROJECT:
                        return ((IProjectSourceNode)this).IncludeInProject();

                    case VsCommands2K.EXCLUDEFROMPROJECT:
                        return ((IProjectSourceNode)this).ExcludeFromProject();

                    case VsCommands2K.SLNREFRESH:
                        WixHelperMethods.RefreshProject(this);
                        return VSConstants.S_OK;
                }
            }

            if (cmdGroup == VsPkgMenus.guidStandardCommandSet97)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.Refresh:
                        WixHelperMethods.RefreshProject(this);
                        return VSConstants.S_OK;
                }
            }

            return base.ExecCommandOnNode(cmdGroup, cmd, cmdexecopt, pvaIn, pvaOut);
        }

        /// <summary>
        /// Resets the Node properties for file node item.
        /// </summary>
        protected void ResetProperties()
        {
            bool changed = false;

            if (this.IsNonMemberItem)
            {
                if (!(this.NodeProperties is WixFileNodeNonMemberProperties))
                {
                    this.NodeProperties = new WixFileNodeNonMemberProperties(this);
                    changed = true;
                }
            }
            else
            {
                if (!(this.NodeProperties is WixFileNodeProperties))
                {
                    this.NodeProperties = new WixFileNodeProperties(this);
                    changed = true;
                }
            }

            if (changed)
            {
                // notify others.
                this.OnPropertyChanged(this, (int)__VSHPROPID.VSHPROPID_BrowseObject, 0);
            }
        }
    }
}
