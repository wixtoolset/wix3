// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
