// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Data;
    using System.IO;
    using System.Security.Permissions;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Combines a listbox, an edit control and a few buttons to allow selection of multiple folders
    /// </summary>
    public partial class FoldersSelector : UserControl
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private Image moveUpOriginalImage;
        private Image moveUpImage;
        private Image moveUpGrayImage;
        private Image moveDownOriginalImage;
        private Image moveDownImage;
        private Image moveDownGrayImage;
        private Image deleteOriginalImage;
        private Image deleteImage;
        private Image deleteGrayImage;
        private bool highContrastMode;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance
        /// </summary>
        public FoldersSelector()
        {
            this.InitializeComponent();
            this.moveUpOriginalImage = this.upButton.Image;
            this.moveDownOriginalImage = this.downButton.Image;
            this.deleteOriginalImage = this.deleteButton.Image;

            this.GenerateButtonImages();
            this.UpdateButtonImages();
            this.EnableControls();
        }

        // =========================================================================================
        // Events
        // =========================================================================================

        /// <summary>
        /// Folders changed event
        /// </summary>
        public event EventHandler FoldersChanged;

        /// <summary>
        /// Folder validating event
        /// </summary>
        public event CancelEventHandler FolderValidating;

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Sets the description of the folders being selected
        /// </summary>
        /// <value>Description of the folders being selected</value>
        [Localizable(true)]
        public string Description
        {
            get { return this.referencePathsLabel.Text; }
            set { this.referencePathsLabel.Text = value; }
        }

        /// <summary>
        /// Sets the folder caption at the top of the control
        /// </summary>
        /// <value>Text for the caption with the correct keyboard accelerator</value>
        [Localizable(true)]
        public string FolderLabel
        {
            get { return this.folderLabel.Text; }
            set { this.folderLabel.Text = value; }
        }

        /// <summary>
        /// Gets or sets the text for Add Folder button.
        /// </summary>
        /// <value>Text for the caption of Add Folder button.</value>
        [Localizable(true)]
        public string AddFolderButtonText
        {
            get { return this.addFolderButton.Text; }
            set { this.addFolderButton.Text = value; }
        }

        /// <summary>
        /// Gets or sets the text for Update button.
        /// </summary>
        /// <value>Text for the caption of Update button.</value>
        [Localizable(true)]
        public string UpdateButtonText
        {
            get { return this.updateButton.Text; }
            set { this.updateButton.Text = value; }
        }

        /// <summary>
        /// Gets the text box for the current folder to be added or modified.
        /// </summary>
        /// <value>Folder text box.</value>
        public TextBox TextBox
        {
            get { return this.folderTextBox; }
        }

        /// <summary>
        /// Populates the list box or returns the content of the list box as a semi-colon delimited list
        /// </summary>
        /// <value>The list of the selected folders</value>
        public override string Text
        {
            get
            {
                StringBuilder accumulatedPaths = new StringBuilder();
                for (int i = 0; i < this.pathsListBox.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        accumulatedPaths.Append(";");
                    }

                    string escapedPath = this.pathsListBox.Items[i].ToString().Replace(";", "%3B");
                    accumulatedPaths.Append(escapedPath);
                }

                return accumulatedPaths.ToString();
            }

            set
            {
                this.pathsListBox.Items.Clear();

                if (String.IsNullOrEmpty(value))
                {
                    return;
                }

                string[] pathsArray = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string path in pathsArray)
                {
                    string trimmedPath = path.Trim();
                    if (trimmedPath.Length > 0)
                    {
                        string unescapedPath = trimmedPath.Replace("%3B", ";");
                        this.pathsListBox.Items.Add(unescapedPath);
                    }
                }
            }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Raises an event when the folder selection has changed
        /// </summary>
        /// <param name="e">Argument for the event, empty</param>
        protected virtual void OnFoldersChanged(EventArgs e)
        {
            if (this.FoldersChanged != null)
            {
                this.FoldersChanged(this, e);
            }
        }

        /// <summary>
        /// Raises an event when a folder is about to be added to the list
        /// </summary>
        /// <param name="e">Argument for the event</param>
        protected virtual void OnFolderValidating(CancelEventArgs e)
        {
            if (this.FolderValidating != null)
            {
                this.FolderValidating(this, e);
            }
        }

        /// <summary>
        /// Override ProcessDialogKey to handle the enter key when in the edit box
        /// </summary>
        /// <param name="keyData">Information about the key pressed</param>
        /// <returns>true if the key was handled</returns>
        [UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows)]
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & (Keys.Alt | Keys.Control)) == Keys.None)
            {
                Keys keyCode = keyData & Keys.KeyCode;
                if (keyCode == Keys.Enter)
                {
                    if (this.ProcessEnterKey())
                    {
                        return true;
                    }
                }
            }

            return base.ProcessDialogKey(keyData);
        }

        private void EnableControls()
        {
            this.deleteButton.Enabled = this.pathsListBox.SelectedIndices.Count > 0;
            this.addFolderButton.Enabled = this.folderTextBox.Text.Length > 0;
            this.updateButton.Enabled = this.folderTextBox.Text.Length > 0 &&
                                            this.pathsListBox.SelectedIndices.Count == 1 &&
                                            !String.Equals(this.folderTextBox.Text, this.pathsListBox.SelectedItem.ToString(), StringComparison.OrdinalIgnoreCase);
            this.upButton.Enabled = this.pathsListBox.SelectedIndices.Count == 1 && this.pathsListBox.SelectedIndex > 0;
            this.downButton.Enabled = this.pathsListBox.SelectedIndices.Count == 1 && this.pathsListBox.SelectedIndex < this.pathsListBox.Items.Count - 1;
        }

        private void GenerateButtonImages()
        {
            Color grayColor = SystemColors.ControlDark;
            this.highContrastMode = SystemInformation.HighContrast;

            if (this.highContrastMode)
            {
                grayColor = SystemColors.Control;
            }

            this.moveUpImage = WixHelperMethods.MapBitmapColor(this.moveUpOriginalImage, Color.Black, SystemColors.ControlText);
            this.moveUpGrayImage = WixHelperMethods.MapBitmapColor(this.moveUpOriginalImage, Color.Black, grayColor);
            this.moveDownImage = WixHelperMethods.MapBitmapColor(this.moveDownOriginalImage, Color.Black, SystemColors.ControlText);
            this.moveDownGrayImage = WixHelperMethods.MapBitmapColor(this.moveDownOriginalImage, Color.Black, grayColor);
            this.deleteImage = WixHelperMethods.MapBitmapColor(this.deleteOriginalImage, Color.Black, SystemColors.ControlText);
            this.deleteGrayImage = WixHelperMethods.MapBitmapColor(this.deleteOriginalImage, Color.Black, grayColor);
        }

        private bool ProcessEnterKey()
        {
            if (this.ActiveControl == this && this.addFolderButton.Enabled)
            {
                this.addFolderButton.PerformClick();
                return true;
            }

            return false;
        }

        private void RemoveCurrentPath()
        {
            int selectedFolder = this.pathsListBox.SelectedIndex;

            if (selectedFolder >= 0)
            {
                this.pathsListBox.BeginUpdate();
                try
                {
                    this.pathsListBox.Items.RemoveAt(this.pathsListBox.SelectedIndex);
                }
                finally
                {
                    this.pathsListBox.EndUpdate();
                }

                this.OnFoldersChanged(EventArgs.Empty);
            }
        }

        private void UpdateButtonImages()
        {
            if (this.upButton.Enabled)
            {
                this.upButton.Image = this.moveUpImage;
            }
            else
            {
                this.upButton.Image = this.moveUpGrayImage;
            }

            if (this.downButton.Enabled)
            {
                this.downButton.Image = this.moveDownImage;
            }
            else
            {
                this.downButton.Image = this.moveDownGrayImage;
            }

            if (this.deleteButton.Enabled)
            {
                this.deleteButton.Image = this.deleteImage;
            }
            else
            {
                this.deleteButton.Image = this.deleteGrayImage;
            }
        }

        private void AddFolderButton_Click(object sender, EventArgs e)
        {
            string folder = WixHelperMethods.EnsureTrailingDirectoryChar(this.folderTextBox.Text);
            if (this.pathsListBox.FindStringExact(folder) == ListBox.NoMatches)
            {
                CancelEventArgs ce = new CancelEventArgs();
                this.OnFolderValidating(ce);
                if (!ce.Cancel)
                {
                    this.folderTextBox.Text = folder;
                    this.pathsListBox.SelectedIndex = this.pathsListBox.Items.Add(folder);
                    this.OnFoldersChanged(EventArgs.Empty);
                }
            }
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            string folder = WixHelperMethods.EnsureTrailingDirectoryChar(this.folderTextBox.Text);
            int selectedIndex = this.pathsListBox.SelectedIndex;
            if (this.pathsListBox.SelectedItem.ToString() != folder)
            {
                CancelEventArgs ce = new CancelEventArgs();
                this.OnFolderValidating(ce);
                if (!ce.Cancel)
                {
                    this.pathsListBox.Items[selectedIndex] = folder;
                    if (this.FoldersChanged != null)
                    {
                        this.FoldersChanged(this, new EventArgs());
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            this.RemoveCurrentPath();
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = this.pathsListBox.SelectedIndex;
            object selectedItem = this.pathsListBox.SelectedItem;

            if (selectedIndex > 0)
            {
                this.pathsListBox.BeginUpdate();

                // To prevent the flashing of the Remove button, Insert new item, change selection, then remove old item
                this.pathsListBox.Items.Insert(selectedIndex - 1, selectedItem);
                this.pathsListBox.SelectedIndex = selectedIndex - 1;
                this.pathsListBox.Items.RemoveAt(selectedIndex + 1);
                this.pathsListBox.EndUpdate();
            }
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = this.pathsListBox.SelectedIndex;
            object selectedItem = this.pathsListBox.SelectedItem;

            if (selectedIndex < this.pathsListBox.Items.Count - 1)
            {
                this.pathsListBox.BeginUpdate();

                // To prevent the flashing of the Remove button, Insert new item, change selection, then remove old item
                this.pathsListBox.Items.Insert(selectedIndex + 2, selectedItem);
                this.pathsListBox.SelectedIndex = selectedIndex + 2;
                this.pathsListBox.Items.RemoveAt(selectedIndex);
                this.pathsListBox.EndUpdate();
            }
        }

        private void PathsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string folderText = "";
            if (this.pathsListBox.SelectedIndex >= 0)
            {
                folderText = this.pathsListBox.SelectedItem.ToString();
            }

            if (this.folderTextBox.Text != folderText)
            {
                this.folderTextBox.Text = folderText;
                if (this.folderTextBox.Focused)
                {
                    this.folderTextBox.SelectionLength = 0;
                    this.folderTextBox.SelectionStart = folderText.Length;
                }
            }
            
            this.EnableControls();
        }

        private void PathsListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                int selectedIndex = this.pathsListBox.SelectedIndex;
                this.RemoveCurrentPath();
                if (selectedIndex < this.pathsListBox.Items.Count)
                {
                    this.pathsListBox.SelectedIndex = selectedIndex;
                }
                else if (selectedIndex > 0)
                {
                    this.pathsListBox.SelectedIndex = selectedIndex - 1;
                }
            }
        }

        private void FolderTextBox_TextChanged(object sender, EventArgs e)
        {
            this.EnableControls();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
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
            }
        }

        private void FoldersSelector_SystemColorsChanged(object sender, EventArgs e)
        {
            this.GenerateButtonImages();
            this.UpdateButtonImages();
        }

        private void GraphicButton_EnabledChanged(object sender, EventArgs e)
        {
            this.UpdateButtonImages();
        }
    }
}
