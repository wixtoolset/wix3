//-------------------------------------------------------------------------------------------------
// <copyright file="oabuildmanager.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Package;
using VSLangProj;

namespace Microsoft.VisualStudio.Package.Automation
{
    public class OABuildManager : ConnectionPointContainer,
                                    IEventSource<_dispBuildManagerEvents>,
                                    BuildManager, 
                                    BuildManagerEvents
    {
        private ProjectNode projectManager;

        public OABuildManager(ProjectNode project)
        {
            projectManager = project;
            AddEventSource<_dispBuildManagerEvents>(this as IEventSource<_dispBuildManagerEvents>);
        }


        #region BuildManager Members

        public string BuildDesignTimeOutput(string bstrOutputMoniker)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public EnvDTE.Project ContainingProject
        {
            get { return projectManager.GetAutomationObject() as EnvDTE.Project; }
        }

        public EnvDTE.DTE DTE
        {
            get { return projectManager.Site.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE; }
        }

        public object DesignTimeOutputMonikers
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public object Parent
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion

        #region _dispBuildManagerEvents_Event Members

        public event _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler DesignTimeOutputDeleted;

#pragma warning disable 67
        public event _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler DesignTimeOutputDirty;

        #endregion

        private void OnDesignTimeOutputDeleted(object sender, EventArgs args)
        {
            if (DesignTimeOutputDeleted == null)
                return;

            string moniker = OABuildManager.GetOutputMoniker(sender);
            if (!String.IsNullOrEmpty(moniker))
                DesignTimeOutputDeleted(moniker);
        }
        private static string GetOutputMoniker(object sender)
        {
            IVsOutput2 output = sender as IVsOutput2;
            if (output == null)
                return null;
            string moniker;
            output.get_CanonicalName(out moniker);
            return moniker;
        }

        #region IEventSource<_dispBuildManagerEvents> Members

        void IEventSource<_dispBuildManagerEvents>.OnSinkAdded(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted += new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty += new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        void IEventSource<_dispBuildManagerEvents>.OnSinkRemoved(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted -= new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty -= new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        #endregion
    }
}
