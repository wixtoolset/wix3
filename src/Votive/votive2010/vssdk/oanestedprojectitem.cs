//-------------------------------------------------------------------------------------------------
// <copyright file="oanestedprojectitem.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Reflection;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Package.Automation
{
	[SuppressMessage("Microsoft.Interoperability", "CA1405:ComVisibleTypeBaseTypesShouldBeComVisible")]
	[ComVisible(true), CLSCompliant(false)]
	public class OANestedProjectItem : OAProjectItem<NestedProjectNode>
	{
		#region fields
		EnvDTE.Project nestedProject = null;
		#endregion

		#region ctors
		public OANestedProjectItem(OAProject project, NestedProjectNode node)
			: base(project, node)
		{
			object nestedproject = null;
			if (ErrorHandler.Succeeded(node.NestedHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out nestedproject)))
			{
				this.nestedProject = nestedproject as EnvDTE.Project;
			}
		}

		#endregion

		#region overridden methods
		/// <summary>
		/// Returns the collection of project items defined in the nested project
		/// </summary>
		public override EnvDTE.ProjectItems ProjectItems
		{
			get
			{
				if (this.nestedProject != null)
				{
					return this.nestedProject.ProjectItems;
				}
				return null;
			}
		}

		/// <summary>
		/// Returns the nested project.
		/// </summary>
		public override EnvDTE.Project SubProject
		{
			get
			{
				return this.nestedProject;
			}
		}
		#endregion
	}
}
