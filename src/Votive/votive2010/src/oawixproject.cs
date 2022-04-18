// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using EnvDTE;
    using Microsoft.VisualStudio.Package.Automation;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Runtime.InteropServices;
 
    /// <summary>
    /// Represents automation object corresponding to a WiX project.
    /// </summary>
    [CLSCompliant(false), ComVisible(true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable")]
    public class OAWixProject : OAProject
    {
        // =========================================================================================
        // Member variables
        // =========================================================================================

        /// <summary>
        /// Properties associated with the WiX project.
        /// </summary>
        private OAProperties properties;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="OAWixProject"/> class.
        /// </summary>
        /// <param name="wixProject">The node to which this project belongs.</param>
        public OAWixProject(WixProjectNode wixProject)
            : base(wixProject)
        {
            if (wixProject != null)
            {
                this.properties = new OAProperties(wixProject.NodeProperties);
            }
        }

        /// <summary>
        /// Properties of the project
        /// </summary>
        /// <value>Collection of all project properties</value>
        public override EnvDTE.Properties Properties
        {
            get
            {
                return this.properties;
            }
        }
    }
}
