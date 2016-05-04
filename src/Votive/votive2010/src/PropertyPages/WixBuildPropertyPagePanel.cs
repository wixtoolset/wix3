// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Windows.Forms;

    using Microsoft.Tools.WindowsInstallerXml.VisualStudio.Forms;
    using System.ComponentModel;
    using System.Collections.Generic;

    /// <summary>
    /// Property page contents for the Candle Settings page.
    /// </summary>
    internal partial class WixBuildPropertyPagePanel : WixPropertyPagePanel
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBuildPropertyPagePanel"/> class.
        /// </summary>
        /// <param name="parentPropertyPage">The parent property page to which this is bound.</param>
        public WixBuildPropertyPagePanel(WixPropertyPage parentPropertyPage)
            : base(parentPropertyPage)
        {
            this.InitializeComponent();

            this.culturesTextBox.Tag = WixProjectFileConstants.Cultures;
            this.defineConstantsTextBox.Tag = WixProjectFileConstants.DefineConstants;
            this.defineDebugCheckBox.Tag = WixProjectFileConstants.DefineDebugConstant;
            this.defineVariablesTextBox.Tag = WixProjectFileConstants.WixVariables;
            this.leaveTempFilesCheckBox.Tag = WixProjectFileConstants.LeaveTemporaryFiles;
            this.outputPathFolderBrowser.TextBox.Tag = WixProjectFileConstants.OutputPath;
            this.suppressedWarningsText.Tag = WixProjectFileConstants.SuppressSpecificWarnings;
            this.suppressWixPdbCheckBox.Tag = WixProjectFileConstants.SuppressPdbOutput;
            this.verboseOutputCheckBox.Tag = WixProjectFileConstants.VerboseOutput;
            this.warningsAsErrorsCheckBox.Tag = WixProjectFileConstants.TreatWarningsAsErrors;
            this.warningLevelCombo.Tag = WixProjectFileConstants.WarningLevel;
            this.bindFilesCheckBox.Tag = WixProjectFileConstants.LibBindFiles;

            this.warningLevelCombo.SelectedIndexChanged += delegate
            {
                this.suppressedWarningsText.Enabled = this.warningLevelCombo.SelectedIndex != 0;
                this.warningsAsErrorsCheckBox.Enabled = this.warningLevelCombo.SelectedIndex != 0;
            };
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Binds the properties from the MSBuild project file to the controls on the property page.
        /// </summary>
        protected internal override void BindProperties()
        {
            base.BindProperties();

            WixOutputType projectType = this.ParentPropertyPage.ProjectMgr.OutputType;
            this.defineVariablesTextBox.Enabled = (projectType != WixOutputType.Library);
            this.culturesTextBox.Enabled = (projectType != WixOutputType.Library && projectType != WixOutputType.Bundle);
            this.leaveTempFilesCheckBox.Enabled = (projectType != WixOutputType.Library);
            this.suppressWixPdbCheckBox.Enabled = (projectType != WixOutputType.Library);
            this.bindFilesCheckBox.Enabled = (projectType == WixOutputType.Library);

            this.suppressedWarningsText.Enabled = this.warningLevelCombo.SelectedIndex != 0;
            this.warningsAsErrorsCheckBox.Enabled = this.warningLevelCombo.SelectedIndex != 0;
        }

        /// <summary>
        /// Called when a control has been successfully validated.
        /// This handles the case of user entering "Debug" in defineConstantsTextBox.
        /// In this case, we will remove "Debug" and set defineDebugCheckBox before passing
        /// to the handler.
        /// </summary>
        /// <param name="sender">The control that was validated.</param>
        /// <param name="e">Parameters for the event.</param>
        protected override void HandleControlValidated(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            string propertyName = (string)control.Tag;
            if (propertyName == WixProjectFileConstants.DefineConstants)
            {
                string value = control.Text;
                string constantsString = value.Trim();
                List<string> constants = new List<string>(constantsString.Split(';'));

                if (WixHelperMethods.RemoveAllMatch(constants, WixBuildPropertyPage.DebugDefine) > 0)
                {
                    constantsString = String.Join(";", constants.ToArray());
                    this.defineConstantsTextBox.Text = constantsString;
                    this.defineConstantsTextBox.Modified = true;

                    if (this.defineDebugCheckBox.CheckState != CheckState.Checked)
                    {
                        // setting the checkbox will call the handler for the checkbox
                        this.defineDebugCheckBox.CheckState = CheckState.Checked;
                    }
                }
            }

            base.HandleControlValidated(sender, e);
        }
    }
}
