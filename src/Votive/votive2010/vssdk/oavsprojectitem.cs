// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using EnvDTE;
using VSLangProj;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Package.Automation
{
    /// <summary>
    /// Represents a language-specific project item
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OAVS")]
    [ComVisible(true), CLSCompliant(false)]
    public class OAVSProjectItem : VSProjectItem
    {
        #region fields
        private FileNode fileNode;
        #endregion

        #region ctors
        public OAVSProjectItem(FileNode fileNode)
        {
            this.FileNode = fileNode;
        }
        #endregion

        #region VSProjectItem Members

        public virtual Project ContainingProject
        {
            get { return fileNode.ProjectMgr.GetAutomationObject() as Project; }
        }

        public virtual ProjectItem ProjectItem
        {
            get { return fileNode.GetAutomationObject() as ProjectItem; }
        }

        public virtual DTE DTE
        {
            get { return (DTE)this.fileNode.ProjectMgr.Site.GetService(typeof(DTE)); }
        }

        public virtual void RunCustomTool()
        {
            this.FileNode.RunGenerator();
        }

        #endregion

        #region public properties
        /// <summary>
        /// File Node property
        /// </summary>
        public FileNode FileNode
        {
            get
            {
                return fileNode;
            }
            set
            {
                fileNode = value;
            }
        }
        #endregion

    }
}
