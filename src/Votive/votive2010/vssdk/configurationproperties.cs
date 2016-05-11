// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.IO;
using MSBuild = Microsoft.Build.Evaluation;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Package
{
    /// <summary>
    /// Defines the config dependent properties exposed through automation
    /// </summary>
    [ComVisible(true)]
    [Guid("21f73a8f-91d7-4085-9d4f-c48ee235ee5b")]
    public interface IProjectConfigProperties
    {
        string OutputPath {get; set;}
    }

    /// <summary>
    /// Implements the configuration dependent properties interface
    /// </summary>
    [CLSCompliant(false), ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class ProjectConfigProperties : IProjectConfigProperties
    {
        #region fields
        private ProjectConfig projectConfig;
        #endregion

        #region ctors
        public ProjectConfigProperties(ProjectConfig projectConfig)
        {
            this.projectConfig = projectConfig;
        }
        #endregion

        #region IProjectConfigProperties Members

        public virtual string OutputPath
        {
            get
            {
                return this.projectConfig.GetConfigurationProperty(BuildPropertyPageTag.OutputPath.ToString(), true);
            }
            set
            {
                this.projectConfig.SetConfigurationProperty(BuildPropertyPageTag.OutputPath.ToString(), value);
            }
        }

        #endregion
    }
}
