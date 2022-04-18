// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.Tools.WindowsInstallerXml.VisualStudio.Forms;

    /// <summary>
    /// Property page contents for the Candle Settings page.
    /// </summary>
    internal partial class WixBuildEventsPropertyPagePanel : WixPropertyPagePanel
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private WixBuildEventEditorForm editorForm = new WixBuildEventEditorForm(null);

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBuildEventsPropertyPagePanel"/> class.
        /// </summary>
        /// <param name="parentPropertyPage">The parent property page to which this is bound.</param>
        public WixBuildEventsPropertyPagePanel(WixPropertyPage parentPropertyPage)
            : base(parentPropertyPage)
        {
            this.InitializeComponent();

            // hook up the form to both editors
            this.preBuildEditor.Initialize(parentPropertyPage.ProjectMgr, this.editorForm);
            this.postBuildEditor.Initialize(parentPropertyPage.ProjectMgr, this.editorForm);

            this.preBuildEditor.TextBox.Tag = WixProjectFileConstants.PreBuildEvent;
            this.postBuildEditor.TextBox.Tag = WixProjectFileConstants.PostBuildEvent;
            this.runPostBuildComboBox.Tag = WixProjectFileConstants.RunPostBuildEvent;
        }
    }
}
