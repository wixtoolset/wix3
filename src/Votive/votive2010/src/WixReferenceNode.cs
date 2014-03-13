//-------------------------------------------------------------------------------------------------
// <copyright file="WixReferenceNode.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixReferenceNode class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.BuildEngine;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.Tools.WindowsInstallerXml.Build.Tasks;

    using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
    using VsMenus = Microsoft.VisualStudio.Package.VsMenus;

    /// <summary>
    /// Abstract base class for a Wix reference node.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class WixReferenceNode : ReferenceNode
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixReferenceNode"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="WixProjectNode"/> that contains this node.</param>
        /// <param name="element">The element that contains MSBuild properties.</param>
        protected WixReferenceNode(WixProjectNode root, ProjectElement element)
            : base(root, element)
        {
            string includeValue = this.ItemNode.GetMetadata(ProjectFileConstants.Include);
            bool referenceNameNotPresent = String.IsNullOrEmpty(this.ItemNode.GetMetadata(ProjectFileConstants.Name));
            string newReferenceName = includeValue;

            if (String.IsNullOrEmpty(this.ItemNode.GetMetadata(ProjectFileConstants.HintPath)))
            {
                this.ItemNode.SetMetadata(ProjectFileConstants.HintPath, includeValue);
            }

            if (includeValue.Contains(Path.DirectorySeparatorChar.ToString()))
            {
                this.ItemNode.Rename(Path.GetFileNameWithoutExtension(includeValue));
                newReferenceName = Path.GetFileNameWithoutExtension(includeValue);
            }

            if (referenceNameNotPresent)
            {
                // this will fail if the node was included from a targets file
                try
                {
                    this.ItemNode.SetMetadata(ProjectFileConstants.Name, newReferenceName);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WixReferenceNode"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="WixProjectNode"/> that contains this node.</param>
        /// <param name="referencePath">The path to the wixlib reference file.</param>
        /// <param name="msBuildElementName">The element name of the reference in an MSBuild file.</param>
        protected WixReferenceNode(WixProjectNode root, string referencePath, string msBuildElementName)
            : this(root, new ProjectElement(root, referencePath, msBuildElementName))
        {
            this.ItemNode.Rename(Path.GetFileNameWithoutExtension(referencePath));
            this.ItemNode.SetMetadata(ProjectFileConstants.Name, Path.GetFileNameWithoutExtension(referencePath));
            this.ItemNode.SetMetadata(ProjectFileConstants.HintPath, referencePath);
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the caption shown in the Solution Explorer.
        /// </summary>
        /// <value>The caption shown in the Solution Explorer.</value>
        public override string Caption
        {
            get
            {
                // use name metadata if present
                string caption = this.ItemNode.GetMetadata(ProjectFileConstants.Name);
                if (String.IsNullOrEmpty(caption))
                {
                    // otherwise use include
                    caption = this.ItemNode.GetMetadata(ProjectFileConstants.Include);
                    if (caption.Contains(Path.DirectorySeparatorChar.ToString()))
                    {
                        caption = Path.GetFileNameWithoutExtension(caption);
                    }
                }

                return caption;
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
                string fullPath = this.ItemNode.GetMetadata(ProjectFileConstants.HintPath);

                if (String.IsNullOrEmpty(fullPath))
                {
                    fullPath = this.ItemNode.GetMetadata(ProjectFileConstants.Include);
                }

                if (String.IsNullOrEmpty(fullPath))
                {
                    return String.Empty;
                }

                fullPath = this.ReplacePropertiesInPath(fullPath);

                if (!Path.IsPathRooted(fullPath))
                {
                    fullPath = Path.Combine(this.ProjectMgr.ProjectFolder, fullPath);
                }

                if (!File.Exists(fullPath))
                {
                    string userReferencePath = (string)this.ProjectMgr.GetProjectProperty(ProjectFileConstants.ReferencePath, false);
                    if (!String.IsNullOrEmpty(userReferencePath))
                    {
                        fullPath = FileSearchHelperMethods.SearchFilePaths(userReferencePath.Split(';'), fullPath);
                    }
                }

                Url url;
                if (Path.IsPathRooted(fullPath))
                {
                    // use absolute path
                    url = new Url(fullPath);
                }
                else
                {
                    // path is relative, so make it relative to project path
                    url = new Url(this.ProjectMgr.BaseURI, fullPath);
                }

                return url.AbsoluteUrl;
            }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Links a reference node to the project and hierarchy.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Tools.WindowsInstallerXml.VisualStudio.WixHelperMethods.ShipAssert(System.Boolean,System.String,System.Object[])")]
        protected override void BindReferenceData()
        {
            WixHelperMethods.ShipAssert(this.ItemNode != null, "The MSBuild ItemNode should have been set by now.");

            // resolve the references, which will copy the files locally if the Private flag is set
            this.ProjectMgr.Build(WixProjectFileConstants.MsBuildTarget.ResolveWixLibraryReferences);
        }

        /// <summary>
        /// Determines if this is node a valid node for painting the default reference icon.
        /// </summary>
        /// <returns>true if the node is a valid node for painting the default reference icon; false if no icon should be shown.</returns>
        protected override bool CanShowDefaultIcon()
        {
            return (!String.IsNullOrEmpty(this.Url) && File.Exists(this.Url));
        }

        /// <summary>
        /// Replaces build properties in the path. Primary purpose is to support path customization by derrived Reference classes.
        /// </summary>
        /// <param name="path">Input path with build propeties.</param>
        /// <returns>Path with build properties evaluated and substituted.</returns>
        protected virtual string ReplacePropertiesInPath(string path)
        {
            this.ProjectMgr.SetCurrentConfiguration();

            int startIndex, endIndex;
            while ((startIndex = path.IndexOf("$(", StringComparison.Ordinal)) >= 0 && (endIndex = path.IndexOf(Convert.ToString(')', CultureInfo.InvariantCulture), startIndex + 2, StringComparison.Ordinal)) >= 0)
            {
                string propertyName = path.Substring(startIndex + 2, endIndex - startIndex - 2);

                string propertyValue = this.ProjectMgr.GetProjectProperty(propertyName, false);
                if (propertyValue == null)
                {
                    propertyValue = ((WixProjectNode)this.ProjectMgr).CurrentConfig.GetConfigurationProperty(propertyName, false);
                    if (propertyValue == null)
                    {
                        propertyValue = String.Empty;
                    }
                }

                path = path.Substring(0, startIndex) + propertyValue + path.Substring(endIndex + 1);
            }

            return path;
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
            else if (cmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                if ((VsCommands2K)cmd == VsCommands2K.QUICKOBJECTSEARCH)
                {
                    result |= QueryStatusResult.NOTSUPPORTED;
                    return VSConstants.S_OK;
                }
            }

            return base.QueryStatusOnNode(cmdGroup, cmd, pCmdText, ref result);
        }

        /// <summary>
        /// Shows invalid Wix reference message.
        /// </summary>
        protected void ShowInvalidWixReferenceMessage()
        {
            string errorMessage = String.Format(CultureInfo.CurrentUICulture, WixStrings.WixReferenceInvalid, this.Url);
            WixHelperMethods.ShowErrorMessageBox(this.ProjectMgr.Site, errorMessage);
        }
    }
}
