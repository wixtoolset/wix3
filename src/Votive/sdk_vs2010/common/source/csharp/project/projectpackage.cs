//-------------------------------------------------------------------------------------------------
// <copyright file="projectpackage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Package;
using Microsoft.Win32;
using EnvDTE;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Globalization;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.Package
{
    /// <summary>
    /// Defines abstract package.
    /// </summary>
    [ComVisible(true)]
    [CLSCompliant(false)]
    public abstract class ProjectPackage : Microsoft.VisualStudio.Shell.Package
    {
        #region fields
        /// <summary>
        /// This is the place to register all the solution listeners.
        /// </summary>
        private List<SolutionListener> solutionListeners = new List<SolutionListener>();
        #endregion

        #region properties
        /// <summary>
        /// Add your listener to this list. They should be added in the overridden Initialize befaore calling the base.
        /// </summary>
        protected internal IList<SolutionListener> SolutionListeners
        {
            get
            {
                return this.solutionListeners;
            }
        }
        #endregion

        #region ctor
        protected ProjectPackage()
        {
        }
        #endregion

        #region methods
        protected override void Initialize()
        {
            UIThread.CaptureSynchronizationContext();
            base.Initialize();

            // Subscribe to the solution events
            this.solutionListeners.Add(new SolutionListenerForProjectReferenceUpdate(this));
            this.solutionListeners.Add(new SolutionListenerForProjectOpen(this));
            this.solutionListeners.Add(new SolutionListenerForBuildDependencyUpdate(this));
            this.solutionListeners.Add(new SolutionListenerForProjectEvents(this));

            foreach (SolutionListener solutionListener in this.solutionListeners)
            {
                solutionListener.Init();
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Unadvise solution listeners.
            try
            {
                if (disposing)
                {
                    foreach (SolutionListener solutionListener in this.solutionListeners)
                    {
                        solutionListener.Dispose();
                    }
                }
            }
            finally
            {

                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Called by the base package to load solution options.
        /// </summary>
        /// <param name="key">Name of the stream.</param>
        /// <param name="stream">The stream from ehere the pachage should read user specific options.</param>
        protected override void OnLoadOptions(string key, Stream stream)
        {
            // Check if the .suo file is safe, i.e. created on this computer
            // This should really go on the Package.cs
            IVsSolution solution = this.GetService(typeof(SVsSolution)) as IVsSolution;

            if (solution != null)
            {
                object valueAsBool;
                int result = solution.GetProperty((int)__VSPROPID2.VSPROPID_SolutionUserFileCreatedOnThisComputer, out valueAsBool);

                if (ErrorHandler.Failed(result) || !(bool)valueAsBool)
                {
                    return;
                }
            }

            base.OnLoadOptions(key, stream);
        }

        /// <summary>
        /// Called by the base package when the solution save the options
        /// </summary>
        /// <param name="key">Name of the stream.</param>
        /// <param name="stream">The stream from ehere the pachage should read user specific options.</param>
        protected override void OnSaveOptions(string key, Stream stream)
        {
            base.OnSaveOptions(key, stream);
        }
        #endregion
    }
}
