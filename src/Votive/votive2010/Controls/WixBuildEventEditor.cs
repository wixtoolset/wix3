//--------------------------------------------------------------------------------------------------
// <copyright file="WixBuildEventEditor.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixBuildEventEditor class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls
{
    using System;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using Microsoft.Tools.WindowsInstallerXml.VisualStudio.Forms;

    /// <summary>
    /// Control for editing build event command lines.
    /// </summary>
    internal partial class WixBuildEventEditor : UserControl
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private WixBuildEventEditorForm editorForm;
        private string editorFormText;
        private WixProjectNode project;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBuildEventEditor"/> class.
        /// </summary>
        public WixBuildEventEditor()
        {
            this.InitializeComponent();
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the text for the edit button.
        /// </summary>
        [Localizable(true)]
        public string ButtonText
        {
            get { return this.editButton.Text; }
            set { this.editButton.Text = value; }
        }

        /// <summary>
        /// Gets the attached (or a new) editor form.
        /// </summary>
        public WixBuildEventEditorForm EditorForm
        {
            get
            {
                if (this.editorForm == null)
                {
                    this.editorForm = new WixBuildEventEditorForm();
                }

                return this.editorForm;
            }
        }

        /// <summary>
        /// Gets or sets the text that is displayed in the editor form's title bar.
        /// </summary>
        [Localizable(true)]
        public string EditorFormText
        {
            get { return this.editorFormText; }
            set { this.editorFormText = value; }
        }

        /// <summary>
        /// Gets the editor's text box.
        /// </summary>
        public TextBox TextBox
        {
            get { return this.contentTextBox; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Initializes the editor with the current project and an instance of an editor form (can be null).
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="editorForm">An instance of an editor form to share or null.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames")]
        public void Initialize(WixProjectNode project, WixBuildEventEditorForm editorForm)
        {
            this.project = project;
            this.editorForm = editorForm;
        }

        /// <summary>
        /// Shows the editor form, which contains advanced editing features.
        /// </summary>
        /// <param name="sender">The edit button.</param>
        /// <param name="e">The <see cref="EventArgs"/> object that contains the event data.</param>
        private void OnEditButtonClick(object sender, EventArgs e)
        {
            // set the form's caption and text box
            this.EditorForm.Text = this.EditorFormText;
            this.EditorForm.EditorText = this.contentTextBox.Text;

            // get the build macros currently defined - we do this every time rather than caching
            // the results because the configuration could change without us knowing about it.
            WixBuildMacroCollection buildMacros = new WixBuildMacroCollection(this.project);
            this.EditorForm.InitializeMacroList(buildMacros);

            // show the dialog and get the text back
            DialogResult result = this.editorForm.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                this.contentTextBox.Text = this.editorForm.EditorText;
                this.contentTextBox.Modified = true;
                this.contentTextBox.Select(this.contentTextBox.Text.Length, 0);
                this.contentTextBox.Focus();
            }
        }
    }
}
