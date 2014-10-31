//-------------------------------------------------------------------------------------------------
// <copyright file="projectdocumentslistener.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Text;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IServiceProvider = System.IServiceProvider;
using ShellConstants = Microsoft.VisualStudio.Shell.Interop.Constants;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.Package
{

	[CLSCompliant(false)]
	public abstract class ProjectDocumentsListener : IVsTrackProjectDocumentsEvents2, IDisposable
	{
		#region fields
		private uint eventsCookie = (uint)ShellConstants.VSCOOKIE_NIL;
		private IVsTrackProjectDocuments2 projectDocTracker = null;
		private ServiceProvider serviceProvider = null;
		private bool isDisposed;
		/// <summary>
		/// Defines an object that will be a mutex for this object for synchronizing thread calls.
		/// </summary>
		private static volatile object Mutex = new object();
		#endregion

		#region ctors
		protected ProjectDocumentsListener(ServiceProvider serviceProvider)
		{

			this.serviceProvider = serviceProvider;
			this.projectDocTracker = serviceProvider.GetService(typeof(SVsTrackProjectDocuments)) as IVsTrackProjectDocuments2;

			Debug.Assert(this.projectDocTracker != null, "Could not get the IVsTrackProjectDocuments2 object from the services exposed by this project");

			if (this.projectDocTracker == null)
			{
				throw new InvalidOperationException();
			}
		}
		#endregion

		#region properties
		protected uint EventsCookie
		{
			get
			{
				return this.eventsCookie;
			}
		}

		protected IVsTrackProjectDocuments2 ProjectDocumentTracker2
		{
			get
			{
				return this.projectDocTracker;
			}
		}

		protected ServiceProvider ServiceProvider
		{
			get
			{
				return this.serviceProvider;
			}
		}
		#endregion

		#region IVsTrackProjectDocumentsEvents2 Members

		public virtual int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
		{
			return VSConstants.E_NOTIMPL;
		}

		public virtual int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
		{
			return VSConstants.E_NOTIMPL;
		}

		#endregion

		#region IDisposable Members
		/// <summary>
		/// The IDispose interface Dispose method for disposing the object determinastically.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region methods
		public void Init()
		{
			if (this.ProjectDocumentTracker2 != null)
			{
				this.ProjectDocumentTracker2.AdviseTrackProjectDocumentsEvents(this, out this.eventsCookie);
			}
		}

		/// <summary>
		/// The method that does the cleanup.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			// Everybody can go here.
			if (!this.isDisposed)
			{
				// Synchronize calls to the Dispose simulteniously.
				lock (Mutex)
				{
					if (disposing && this.eventsCookie != (uint)ShellConstants.VSCOOKIE_NIL && this.ProjectDocumentTracker2 != null)
					{
						this.ProjectDocumentTracker2.UnadviseTrackProjectDocumentsEvents((uint)this.eventsCookie);
						this.eventsCookie = (uint)ShellConstants.VSCOOKIE_NIL;
					}

					this.isDisposed = true;
				}
			}
		}
		#endregion
	}
}
