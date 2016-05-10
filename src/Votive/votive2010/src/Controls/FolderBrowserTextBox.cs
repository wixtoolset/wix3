// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Windows.Forms;

    /// <summary>
    /// Extends a simple text box specialized for browsing to folders. Supports auto-complete and
    /// a browse button that brings up the folder browse dialog.
    /// </summary>
    internal partial class FolderBrowserTextBox : UserControl
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderBrowserTextBox"/> class.
        /// </summary>
        public FolderBrowserTextBox()
        {
            this.InitializeComponent();
        }

        // =========================================================================================
        // Events
        // =========================================================================================

        /// <summary>
        /// Occurs when the text has changed.
        /// </summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public new event EventHandler TextChanged
        {
            add { base.TextChanged += value; }
            remove { base.TextChanged -= value; }
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the path of the selected folder.
        /// </summary>
        [Bindable(true)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text
        {
            get { return this.folderTextBox.Text; }
            set { this.folderTextBox.Text = value; }
        }

        /// <summary>
        /// Gets the inner TextBox control of the UserControl.
        /// </summary>
        [Browsable(false)]
        public TextBox TextBox
        {
            get { return this.folderTextBox; }
        }

        /// <summary>
        /// Gets or sets the descriptive text displayed above the tree view in the folder browser dialog.
        /// </summary>
        [Localizable(true)]
        public string DialogDescription
        {
            get { return this.folderBrowserDialog.Description; }
            set { this.folderBrowserDialog.Description = value; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Sets the bounds of the control. In this case, we fix the height to the text box's height.
        /// </summary>
        /// <param name="x">The new x value.</param>
        /// <param name="y">The new y value.</param>
        /// <param name="width">The new width value.</param>
        /// <param name="height">The height value.</param>
        /// <param name="specified">A set of flags indicating which bounds to set.</param>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if ((specified & BoundsSpecified.Height) == BoundsSpecified.Height)
            {
                height = this.folderTextBox.Height + 1;
            }

            base.SetBoundsCore(x, y, width, height, specified);
        }

        /// <summary>
        /// Brings up the browse folder dialog.
        /// </summary>
        /// <param name="sender">The browse button.</param>
        /// <param name="e">The <see cref="EventArgs"/> object that contains the event data.</param>
        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            // initialize the dialog to the current directory (if it exists)
            if (!String.IsNullOrEmpty(this.folderTextBox.Text) && Directory.Exists(this.folderTextBox.Text))
            {
                this.folderBrowserDialog.SelectedPath = this.folderTextBox.Text;
            }

            // show the dialog
            if (this.folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.folderTextBox.Text = this.folderBrowserDialog.SelectedPath;
                this.folderTextBox.Modified = true;
            }
        }

        /// <summary>
        /// Raises the <see cref="TextChanged"/> event.
        /// </summary>
        /// <param name="sender">The folder text box.</param>
        /// <param name="e">The <see cref="EventArgs"/> object that contains the event data.</param>
        private void OnFolderTextBoxTextChanged(object sender, EventArgs e)
        {
            this.OnTextChanged(EventArgs.Empty);
        }
    }
}
