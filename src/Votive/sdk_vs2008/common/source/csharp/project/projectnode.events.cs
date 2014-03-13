//-------------------------------------------------------------------------------------------------
// <copyright file="projectnode.events.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Package
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Shell.Interop;

    public partial class ProjectNode
    {
        #region fields
        private EventHandler<ProjectPropertyChangedArgs> projectPropertiesListeners;
        #endregion

        #region events
        public event EventHandler<ProjectPropertyChangedArgs> OnProjectPropertyChanged
        {
            add { projectPropertiesListeners += value; }
            remove { projectPropertiesListeners -= value; }
        }
        #endregion

        #region methods
        protected void RaiseProjectPropertyChanged(string propertyName, string oldValue, string newValue)
        {
            if (null != projectPropertiesListeners)
            {
                projectPropertiesListeners(this, new ProjectPropertyChangedArgs(propertyName, oldValue, newValue));
            }
        }
        #endregion
    }

}