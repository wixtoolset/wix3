// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Globalization;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Shell.Interop;

    using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
    using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;

    /// <summary>
    /// Represents the project's "References" node.
    /// </summary>
    [CLSCompliant(false)]
    public class WixReferenceContainerNode : ReferenceContainerNode
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private static readonly string[] supportedReferenceTypes = new string[]
            {
                WixProjectFileConstants.WixExtension,
                WixProjectFileConstants.WixLibrary,
                ProjectFileConstants.ProjectReference,
            };

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixReferenceContainerNode"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="WixProjectNode"/> that contains this node.</param>
        public WixReferenceContainerNode(WixProjectNode root)
            : base(root)
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the caption to show in Solution Explorer.
        /// </summary>
        /// <value>The caption to show in Solution Explorer.</value>
        public override string Caption
        {
            get { return WixStrings.WixReferencesFolderName; }
        }

        /// <summary>
        /// Gets the list of reference types (element names in the .wixproj file) that the Wix project supports.
        /// </summary>
        /// <value>The list of reference types that the Wix project supports.</value>
        protected override string[] SupportedReferenceTypes
        {
            get { return supportedReferenceTypes; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Creates a new <see cref="WixLibraryReferenceNode"/> object corresponding to the file selected via the Add Reference dialog.
        /// </summary>
        /// <param name="selectorData">The data coming from the Add Reference dialog.</param>
        /// <returns>A new <see cref="WixLibraryReferenceNode"/>.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Tools.WindowsInstallerXml.VisualStudio.WixHelperMethods.ShipAssert(System.Boolean,System.String,System.Object[])")]
        protected override ReferenceNode CreateFileComponent(VSCOMPONENTSELECTORDATA selectorData)
        {
            if (!File.Exists(selectorData.bstrFile))
            {
                return null;
            }

            WixProjectNode projectNode = this.ProjectMgr as WixProjectNode;
            WixHelperMethods.ShipAssert(projectNode != null, "ProjectMgr is null or is not WixProjectNode.");

            switch (Path.GetExtension(selectorData.bstrFile))
            {
                case ".wixlib":
                    return new WixLibraryReferenceNode(projectNode, projectNode.GetRelativePath(selectorData.bstrFile));

                case ".dll":
                    return new WixExtensionReferenceNode(projectNode, selectorData.bstrFile);

                default:
                    string message = String.Format(CultureInfo.CurrentUICulture, WixStrings.WixReferenceInvalid, selectorData.bstrFile);
                    WixHelperMethods.ShowErrorMessageBox(this.ProjectMgr.Site, message);
                    return null;
            }
        }

        /// <summary>
        /// Creates a new <see cref="WixLibraryReferenceNode"/> if the element is a "WixLibraryReference",
        /// a new <see cref="WixExtensionReferenceNode"/> if the element is a "WixExtensionReference",
        /// or a new <see cref="WixProjectReferenceNode"/> if the element is a "ProjectReference".
        /// </summary>
        /// <param name="referenceType">The type of reference to be created.</param>
        /// <param name="element">The MSBuild element pertaining to the reference.</param>
        /// <returns>A <see cref="WixLibraryReferenceNode"/>, <see cref="WixExtensionReferenceNode"/>,
        /// or a <see cref="WixProjectReferenceNode"/>.</returns>
        protected override ReferenceNode CreateReferenceNode(string referenceType, ProjectElement element)
        {
            switch (element.ItemName)
            {
                case WixProjectFileConstants.WixLibrary:
                    return new WixLibraryReferenceNode(this.ProjectMgr as WixProjectNode, element);

                case WixProjectFileConstants.WixExtension:
                    return new WixExtensionReferenceNode(this.ProjectMgr as WixProjectNode, element);
            }

            return base.CreateReferenceNode(referenceType, element);
        }

        /// <summary>
        /// Creates a project reference node given an existing project element.
        /// </summary>
        /// <param name="element">MSBuild properties for the project.</param>
        /// <returns>A <see cref="WixProjectReferenceNode"/> instance.</returns>
        protected override ProjectReferenceNode CreateProjectReferenceNode(ProjectElement element)
        {
            return new WixProjectReferenceNode(this.ProjectMgr as WixProjectNode, element);
        }

        /// <summary>
        /// Create a Project to Project reference given a VSCOMPONENTSELECTORDATA structure.
        /// </summary>
        /// <param name="selectorData">Structure containing the project name, file path, and GUID.</param>
        /// <returns>A <see cref="WixProjectReferenceNode"/> instance.</returns>
        protected override ProjectReferenceNode CreateProjectReferenceNode(VSCOMPONENTSELECTORDATA selectorData)
        {
            return new WixProjectReferenceNode(this.ProjectMgr as WixProjectNode, selectorData.bstrTitle, selectorData.bstrFile, selectorData.bstrProjRef);
        }

        /// <summary>
        /// Handles command status on a node. Should be overridden by descendant nodes. If a command cannot be handled then the base should be called.
        /// </summary>
        /// <param name="cmdGroup">A unique identifier of the command group. The pguidCmdGroup parameter can be NULL to specify the standard group.</param>
        /// <param name="cmd">The command to query status for.</param>
        /// <param name="pCmdText">Pointer to an OLECMDTEXT structure in which to return the name and/or status information of a single command. Can be NULL to indicate that the caller does not require this information.</param>
        /// <param name="result">An out parameter specifying the QueryStatusResult of the command.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Suppressing to avoid conflict with style cop.")]
        protected override int QueryStatusOnNode(Guid cmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
        {
            WixProjectNode projectNode = this.ProjectMgr as WixProjectNode;
            if (projectNode != null && projectNode.QueryStatusOnProjectNode(cmdGroup, cmd, ref result))
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
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#", Justification = "Suppressing to avoid conflict with style cop.")]
        protected override int ExecCommandOnNode(Guid cmdGroup, uint cmd, uint cmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (cmdGroup == VsMenus.guidStandardCommandSet97)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.Refresh:
                        WixHelperMethods.RefreshProject(this);
                        return VSConstants.S_OK;
                }
            }

            if (cmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                switch ((VsCommands2K)cmd)
                {
                    case VsCommands2K.SLNREFRESH:
                        WixHelperMethods.RefreshProject(this);
                        return VSConstants.S_OK;
                }
            }

            return base.ExecCommandOnNode(cmdGroup, cmd, cmdexecopt, pvaIn, pvaOut);
        }
    }
}
