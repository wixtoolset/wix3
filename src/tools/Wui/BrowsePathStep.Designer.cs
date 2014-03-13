// <copyright file="BrowsePathStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Browses for source step.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    /// <summary>
    /// Browses for source step.
    /// </summary>
    sealed public partial class BrowsePathStep
    {
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.TextBox textBox;

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
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowsePathStep));
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.browseButton = new System.Windows.Forms.Button();
            this.textBox = new System.Windows.Forms.TextBox();
            this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.buttonTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // browseButton
            // 
            resources.ApplyResources(this.browseButton, "browseButton");
            this.browseButton.MinimumSize = new System.Drawing.Size(75, 22);
            this.browseButton.Name = "browseButton";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // textBox
            // 
            resources.ApplyResources(this.textBox, "textBox");
            this.textBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.textBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.textBox.Name = "textBox";
            this.textBox.Validated += new System.EventHandler(this.TextBox_Validated);
            this.textBox.Validating += new System.ComponentModel.CancelEventHandler(this.TextBox_Validating);
            // 
            // buttonTableLayoutPanel
            // 
            resources.ApplyResources(this.buttonTableLayoutPanel, "buttonTableLayoutPanel");
            this.buttonTableLayoutPanel.Controls.Add(this.browseButton, 0, 0);
            this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
            // 
            // BrowsePathStep
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.buttonTableLayoutPanel);
            this.Controls.Add(this.textBox);
            this.Name = "BrowsePathStep";
            this.Tag = "Provide the path to the directory that contains your built isolated application.";
            this.buttonTableLayoutPanel.ResumeLayout(false);
            this.buttonTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel buttonTableLayoutPanel;
    }
}
