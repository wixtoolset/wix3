//-------------------------------------------------------------------------------------------------
// <copyright file="oareferenceitem.cs" company="Outercurve Foundation">
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
	/// <summary>
	/// Represents the automation object equivalent to a ReferenceNode object
	/// </summary>
	[SuppressMessage("Microsoft.Interoperability", "CA1405:ComVisibleTypeBaseTypesShouldBeComVisible")]
	[ComVisible(true), CLSCompliant(false)]
	public class OAReferenceItem : OAProjectItem<ReferenceNode>
	{
		#region ctors
		public OAReferenceItem(OAProject project, ReferenceNode node)
			: base(project, node)
		{
		}

		#endregion

		#region overridden methods
		/// <summary>
		/// Not implemented. If called throws invalid operation exception.
		/// </summary>	
		public override void Delete()
		{
			throw new InvalidOperationException();
		}


		/// <summary>
		/// Not implemented. If called throws invalid operation exception.
		/// </summary>
		/// <param name="viewKind"> A Constants. vsViewKind indicating the type of view to use.</param>
		/// <returns></returns>
		public override EnvDTE.Window Open(string viewKind)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Gets or sets the name of the object.
		/// </summary>
		public override string Name
		{
			get
			{
				return base.Name;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Gets the ProjectItems collection containing the ProjectItem object supporting this property.
		/// </summary>
		public override EnvDTE.ProjectItems Collection
		{
			get
			{
				// Get the parent node (ReferenceContainerNode)
				ReferenceContainerNode parentNode = this.Node.Parent as ReferenceContainerNode;
				Debug.Assert(parentNode != null, "Failed to get the parent node");

				// Get the ProjectItems object for the parent node
				if (parentNode != null)
				{
					// The root node for the project
					return ((OAReferenceFolderItem)parentNode.GetAutomationObject()).ProjectItems;
				}

				return null;
			}
		}
		#endregion
	}
}
