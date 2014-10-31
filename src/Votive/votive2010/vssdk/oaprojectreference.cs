//-------------------------------------------------------------------------------------------------
// <copyright file="oaprojectreference.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Package;
using VSLangProj;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Package.Automation
{
    /// <summary>
    /// Represents a project reference of the solution
    /// </summary>
    [SuppressMessage("Microsoft.Interoperability", "CA1405:ComVisibleTypeBaseTypesShouldBeComVisible")]
    [CLSCompliant(false), ComVisible(true)]
    public class OAProjectReference : OAReferenceBase<ProjectReferenceNode>
    {
        public OAProjectReference(ProjectReferenceNode projectReference) : 
            base(projectReference)
        {
        }

        #region Reference override
        public override string Culture
        {
            get { return string.Empty; }
        }
        public override string Name
        {
            get { return BaseReferenceNode.ReferencedProjectName; }
        }
        public override string Identity
        {
            get
            {
                return BaseReferenceNode.Caption;
            }
        }
        public override string Path
        {
            get
            {
                return BaseReferenceNode.ReferencedProjectOutputPath;
            }
        }
        public override EnvDTE.Project SourceProject
        {
            get
            {
                if (Guid.Empty == BaseReferenceNode.ReferencedProjectGuid)
                {
                    return null;
                }
                IVsHierarchy hierarchy = VsShellUtilities.GetHierarchy(BaseReferenceNode.ProjectMgr.Site, BaseReferenceNode.ReferencedProjectGuid);
                if (null == hierarchy)
                {
                    return null;
                }
                object extObject;
                if (Microsoft.VisualStudio.ErrorHandler.Succeeded(
                        hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out extObject)))
                {
                    return extObject as EnvDTE.Project;
                }
                return null;
            }
        }
        public override prjReferenceType Type
        {
            // TODO: Write the code that finds out the type of the output of the source project.
            get { return prjReferenceType.prjReferenceTypeAssembly; }
        }
        public override string Version
        {
            get { return string.Empty; }
        }
        #endregion
    }
}
