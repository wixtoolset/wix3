//-------------------------------------------------------------------------------------------------
// <copyright file="oavsproject.cs" company="Outercurve Foundation">
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
    /// Represents an automation friendly version of a language-specific project.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OAVS")]
	[ComVisible(true), CLSCompliant(false)]
	public class OAVSProject : VSProject
	{
		#region fields
		private ProjectNode project;
		private OAVSProjectEvents events;
		#endregion

		#region ctors
		public OAVSProject(ProjectNode project)
		{
			this.project = project;
		}
		#endregion

		#region VSProject Members

		public ProjectItem AddWebReference(string bstrUrl)
		{
			Debug.Fail("VSProject.AddWebReference not implemented");
			throw new Exception("The method or operation is not implemented.");
		}

		public BuildManager BuildManager
		{
			get
			{
				return new OABuildManager(this.project);
			}
		}

		public void CopyProject(string bstrDestFolder, string bstrDestUNCPath, prjCopyProjectOption copyProjectOption, string bstrUsername, string bstrPassword)
		{
			Debug.Fail("VSProject.References not implemented");
			throw new Exception("The method or operation is not implemented.");
		}

		public ProjectItem CreateWebReferencesFolder()
		{
			Debug.Fail("VSProject.CreateWebReferencesFolder not implemented");
			throw new Exception("The method or operation is not implemented.");
		}

		public DTE DTE
		{
			get
			{
				return (EnvDTE.DTE)this.project.Site.GetService(typeof(EnvDTE.DTE));
			}
		}

		public VSProjectEvents Events
		{
			get
			{
				if (events == null)
					events = new OAVSProjectEvents(this);
				return events;
			}
		}

		public void Exec(prjExecCommand command, int bSuppressUI, object varIn, out object pVarOut)
		{
			Debug.Fail("VSProject.Exec not implemented");
			throw new Exception("The method or operation is not implemented.");
		}

		public void GenerateKeyPairFiles(string strPublicPrivateFile, string strPublicOnlyFile)
		{
			Debug.Fail("VSProject.GenerateKeyPairFiles not implemented");
			throw new Exception("The method or operation is not implemented.");
		}

		public string GetUniqueFilename(object pDispatch, string bstrRoot, string bstrDesiredExt)
		{
			Debug.Fail("VSProject.GetUniqueFilename not implemented");
			throw new Exception("The method or operation is not implemented.");
		}

		public Imports Imports
		{
			get
			{
				Debug.Fail("VSProject.Imports not implemented");
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public Project Project
		{
			get
			{
				return this.project.GetAutomationObject() as Project;
			}
		}

		public References References
		{
			get
			{
				ReferenceContainerNode references = project.GetReferenceContainer() as ReferenceContainerNode;
				if (null == references)
				{
					return null;
				}
				return references.Object as References;
			}
		}

		public void Refresh()
		{
			Debug.Fail("VSProject.Refresh not implemented");
			throw new Exception("The method or operation is not implemented.");
		}

		public string TemplatePath
		{
			get
			{
				Debug.Fail("VSProject.TemplatePath not implemented");
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public ProjectItem WebReferencesFolder
		{
			get
			{
				Debug.Fail("VSProject.WebReferencesFolder not implemented");
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public bool WorkOffline
		{
			get
			{
				Debug.Fail("VSProject.WorkOffLine not implemented");
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				Debug.Fail("VSProject.Set_WorkOffLine not implemented");
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}

    /// <summary>
    /// Provides access to language-specific project events
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OAVS")]
	[ComVisible(true), CLSCompliant(false)]
	public class OAVSProjectEvents : VSProjectEvents
	{
		#region fields
		private OAVSProject vsProject;
		#endregion

		#region ctors
		public OAVSProjectEvents(OAVSProject vsProject)
		{
			this.vsProject = vsProject;
		}
		#endregion

		#region VSProjectEvents Members

		public BuildManagerEvents BuildManagerEvents
		{
			get
			{
				return vsProject.BuildManager as BuildManagerEvents;
			}
		}

		public ImportsEvents ImportsEvents
		{
			get
			{
				Debug.Fail("VSProjectEvents.ImportsEvents not implemented");
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public ReferencesEvents ReferencesEvents
		{
			get
			{
				return vsProject.References as ReferencesEvents;
			}
		}

		#endregion
	}

}
