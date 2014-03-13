//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildEventsPropertyPagePanel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixBuildEventsPropertyPagePanel class.
// </summary>
//-------------------------------------------------------------------------------------------------

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

        private WixBuildEventEditorForm editorForm = new WixBuildEventEditorForm();

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
