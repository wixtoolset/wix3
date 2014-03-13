//-------------------------------------------------------------------------------------------------
// <copyright file="WixFoldersSelector.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls
{
    partial class FoldersSelector
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FoldersSelector));
            this.referencePathsLabel = new System.Windows.Forms.Label();
            this.folderTextBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.pathsListBox = new System.Windows.Forms.ListBox();
            this.addFolderButton = new System.Windows.Forms.Button();
            this.updateButton = new System.Windows.Forms.Button();
            this.upButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.downButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.folderLabel = new System.Windows.Forms.Label();
            this.addUpdateFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.addUpdateFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // referencePathsLabel
            // 
            resources.ApplyResources(this.referencePathsLabel, "referencePathsLabel");
            this.referencePathsLabel.Name = "referencePathsLabel";
            // 
            // folderTextBox
            // 
            resources.ApplyResources(this.folderTextBox, "folderTextBox");
            this.folderTextBox.MinimumSize = new System.Drawing.Size(4, 20);
            this.folderTextBox.Name = "folderTextBox";
            this.folderTextBox.TextChanged += new System.EventHandler(this.FolderTextBox_TextChanged);
            // 
            // browseButton
            // 
            resources.ApplyResources(this.browseButton, "browseButton");
            this.browseButton.MinimumSize = new System.Drawing.Size(29, 23);
            this.browseButton.Name = "browseButton";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // pathsListBox
            // 
            resources.ApplyResources(this.pathsListBox, "pathsListBox");
            this.pathsListBox.FormattingEnabled = true;
            this.pathsListBox.Name = "pathsListBox";
            this.pathsListBox.SelectedIndexChanged += new System.EventHandler(this.PathsListBox_SelectedIndexChanged);
            this.pathsListBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PathsListBox_KeyDown);
            // 
            // addFolderButton
            // 
            resources.ApplyResources(this.addFolderButton, "addFolderButton");
            this.addFolderButton.MinimumSize = new System.Drawing.Size(75, 23);
            this.addFolderButton.Name = "addFolderButton";
            this.addFolderButton.UseVisualStyleBackColor = true;
            this.addFolderButton.Click += new System.EventHandler(this.AddFolderButton_Click);
            // 
            // updateButton
            // 
            resources.ApplyResources(this.updateButton, "updateButton");
            this.updateButton.MinimumSize = new System.Drawing.Size(75, 23);
            this.updateButton.Name = "updateButton";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new System.EventHandler(this.UpdateButton_Click);
            // 
            // upButton
            // 
            resources.ApplyResources(this.upButton, "upButton");
            this.upButton.Image = global::Microsoft.Tools.WindowsInstallerXml.VisualStudio.WixStrings.UpArrow;
            this.upButton.MinimumSize = new System.Drawing.Size(26, 25);
            this.upButton.Name = "upButton";
            this.upButton.UseVisualStyleBackColor = true;
            this.upButton.Click += new System.EventHandler(this.UpButton_Click);
            this.upButton.EnabledChanged += new System.EventHandler(this.GraphicButton_EnabledChanged);
            // 
            // deleteButton
            // 
            resources.ApplyResources(this.deleteButton, "deleteButton");
            this.deleteButton.Image = global::Microsoft.Tools.WindowsInstallerXml.VisualStudio.WixStrings.Delete;
            this.deleteButton.MinimumSize = new System.Drawing.Size(26, 25);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            this.deleteButton.EnabledChanged += new System.EventHandler(this.GraphicButton_EnabledChanged);
            // 
            // downButton
            // 
            resources.ApplyResources(this.downButton, "downButton");
            this.downButton.Image = global::Microsoft.Tools.WindowsInstallerXml.VisualStudio.WixStrings.DownArrow;
            this.downButton.MinimumSize = new System.Drawing.Size(26, 25);
            this.downButton.Name = "downButton";
            this.downButton.UseVisualStyleBackColor = true;
            this.downButton.Click += new System.EventHandler(this.DownButton_Click);
            this.downButton.EnabledChanged += new System.EventHandler(this.GraphicButton_EnabledChanged);
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // folderLabel
            // 
            resources.ApplyResources(this.folderLabel, "folderLabel");
            this.folderLabel.Name = "folderLabel";
            // 
            // addUpdateFlowLayoutPanel
            // 
            resources.ApplyResources(this.addUpdateFlowLayoutPanel, "addUpdateFlowLayoutPanel");
            this.addUpdateFlowLayoutPanel.Controls.Add(this.addFolderButton);
            this.addUpdateFlowLayoutPanel.Controls.Add(this.updateButton);
            this.addUpdateFlowLayoutPanel.Name = "addUpdateFlowLayoutPanel";
            // 
            // FoldersSelector
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.addUpdateFlowLayoutPanel);
            this.Controls.Add(this.folderLabel);
            this.Controls.Add(this.downButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.upButton);
            this.Controls.Add(this.referencePathsLabel);
            this.Controls.Add(this.pathsListBox);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.folderTextBox);
            this.Name = "FoldersSelector";
            this.SystemColorsChanged += new System.EventHandler(this.FoldersSelector_SystemColorsChanged);
            this.addUpdateFlowLayoutPanel.ResumeLayout(false);
            this.addUpdateFlowLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox folderTextBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.ListBox pathsListBox;
        private System.Windows.Forms.Button addFolderButton;
        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.Button upButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button downButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Label referencePathsLabel;
        private System.Windows.Forms.Label folderLabel;
        private System.Windows.Forms.FlowLayoutPanel addUpdateFlowLayoutPanel;
    }
}
