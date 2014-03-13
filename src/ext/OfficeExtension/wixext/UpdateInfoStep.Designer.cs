//-------------------------------------------------------------------------------------------------
// <copyright file="UpdateInfoStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Provide update information about Office Addin.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    /// <summary>
    /// Provide update information about Office Addin.
    /// </summary>
    internal partial class UpdateInfoStep
    {
        private System.Windows.Forms.TextBox previousPathTextBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.RadioButton noUpgradeRadioButton;
        private System.Windows.Forms.RadioButton feedUpgradeRadioButton;
        private System.Windows.Forms.RadioButton upgradePathRadioButton;

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
            this.previousPathTextBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.noUpgradeRadioButton = new System.Windows.Forms.RadioButton();
            this.feedUpgradeRadioButton = new System.Windows.Forms.RadioButton();
            this.upgradePathRadioButton = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // previousPathTextBox
            // 
            this.previousPathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.previousPathTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.previousPathTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.previousPathTextBox.Enabled = false;
            this.previousPathTextBox.Location = new System.Drawing.Point(3, 72);
            this.previousPathTextBox.Name = "previousPathTextBox";
            this.previousPathTextBox.Size = new System.Drawing.Size(293, 20);
            this.previousPathTextBox.TabIndex = 19;
            this.previousPathTextBox.Validated += new System.EventHandler(this.PreviousPathTextBox_Validated);
            this.previousPathTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.PreviousPathTextBox_Validating);
            // 
            // browseButton
            // 
            this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseButton.Enabled = false;
            this.browseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.browseButton.Location = new System.Drawing.Point(223, 98);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(73, 23);
            this.browseButton.TabIndex = 20;
            this.browseButton.Text = "B&rowse...";
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Feed files|*.feed|MSI packages|*.msi|Executable packages|*.exe|All files|*.*";
            // 
            // noUpgradeRadioButton
            // 
            this.noUpgradeRadioButton.AutoSize = true;
            this.noUpgradeRadioButton.Location = new System.Drawing.Point(3, 3);
            this.noUpgradeRadioButton.Name = "noUpgradeRadioButton";
            this.noUpgradeRadioButton.Size = new System.Drawing.Size(195, 17);
            this.noUpgradeRadioButton.TabIndex = 21;
            this.noUpgradeRadioButton.TabStop = true;
            this.noUpgradeRadioButton.Text = "No upgrade from previous package.";
            this.noUpgradeRadioButton.UseVisualStyleBackColor = true;
            this.noUpgradeRadioButton.Validated += new System.EventHandler(this.AnyRadioButton_Validated);
            this.noUpgradeRadioButton.CheckedChanged += new System.EventHandler(this.NoUpgradeRadioButton_CheckedChanged);
            // 
            // feedUpgradeRadioButton
            // 
            this.feedUpgradeRadioButton.AutoSize = true;
            this.feedUpgradeRadioButton.Location = new System.Drawing.Point(3, 26);
            this.feedUpgradeRadioButton.Name = "feedUpgradeRadioButton";
            this.feedUpgradeRadioButton.Size = new System.Drawing.Size(300, 17);
            this.feedUpgradeRadioButton.TabIndex = 22;
            this.feedUpgradeRadioButton.TabStop = true;
            this.feedUpgradeRadioButton.Text = "Download previous package from feed from previous step.";
            this.feedUpgradeRadioButton.UseVisualStyleBackColor = true;
            this.feedUpgradeRadioButton.Validated += new System.EventHandler(this.AnyRadioButton_Validated);
            this.feedUpgradeRadioButton.CheckedChanged += new System.EventHandler(this.FeedUpgradeRadioButton_CheckedChanged);
            // 
            // upgradePathRadioButton
            // 
            this.upgradePathRadioButton.AutoSize = true;
            this.upgradePathRadioButton.Location = new System.Drawing.Point(3, 49);
            this.upgradePathRadioButton.Name = "upgradePathRadioButton";
            this.upgradePathRadioButton.Size = new System.Drawing.Size(188, 17);
            this.upgradePathRadioButton.TabIndex = 23;
            this.upgradePathRadioButton.TabStop = true;
            this.upgradePathRadioButton.Text = "Provide path to previous package:";
            this.upgradePathRadioButton.UseVisualStyleBackColor = true;
            this.upgradePathRadioButton.Validated += new System.EventHandler(this.AnyRadioButton_Validated);
            this.upgradePathRadioButton.CheckedChanged += new System.EventHandler(this.UpgradePathRadioButton_CheckedChanged);
            // 
            // UpdateInfoStep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.upgradePathRadioButton);
            this.Controls.Add(this.feedUpgradeRadioButton);
            this.Controls.Add(this.noUpgradeRadioButton);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.previousPathTextBox);
            this.Name = "UpdateInfoStep";
            this.Size = new System.Drawing.Size(300, 125);
            this.Tag = "Optionally, you can provide the path to the previous version of the application a" +
                "nd an upgrade package will be created.";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
