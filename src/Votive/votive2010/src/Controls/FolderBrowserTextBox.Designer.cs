// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls
{
    partial class FolderBrowserTextBox
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FolderBrowserTextBox));
            this.folderTextBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // folderTextBox
            // 
            resources.ApplyResources(this.folderTextBox, "folderTextBox");
            this.folderTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.folderTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.folderTextBox.MinimumSize = new System.Drawing.Size(4, 21);
            this.folderTextBox.Name = "folderTextBox";
            this.folderTextBox.TextChanged += new System.EventHandler(this.OnFolderTextBoxTextChanged);
            // 
            // browseButton
            // 
            resources.ApplyResources(this.browseButton, "browseButton");
            this.browseButton.MinimumSize = new System.Drawing.Size(29, 23);
            this.browseButton.Name = "browseButton";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.OnBrowseButtonClick);
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // FolderBrowserTextBox
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.folderTextBox);
            this.MinimumSize = new System.Drawing.Size(64, 23);
            this.Name = "FolderBrowserTextBox";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox folderTextBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
    }
}
