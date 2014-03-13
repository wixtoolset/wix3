// <copyright file="UpdateInfoStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Step to provide update information for application.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    /// <summary>
    /// Step to provide update information for application.
    /// </summary>
    public sealed partial class UpdateInfoStep
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateInfoStep));
            this.previousPathTextBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.noUpgradeRadioButton = new System.Windows.Forms.RadioButton();
            this.feedUpgradeRadioButton = new System.Windows.Forms.RadioButton();
            this.upgradePathRadioButton = new System.Windows.Forms.RadioButton();
            this.bottomButtonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.bottomButtonTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // previousPathTextBox
            // 
            resources.ApplyResources(this.previousPathTextBox, "previousPathTextBox");
            this.previousPathTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.previousPathTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.previousPathTextBox.Name = "previousPathTextBox";
            this.previousPathTextBox.Validated += new System.EventHandler(this.PreviousPathTextBox_Validated);
            this.previousPathTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.PreviousPathTextBox_Validating);
            // 
            // browseButton
            // 
            resources.ApplyResources(this.browseButton, "browseButton");
            this.browseButton.MinimumSize = new System.Drawing.Size(73, 23);
            this.browseButton.Name = "browseButton";
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // openFileDialog
            // 
            resources.ApplyResources(this.openFileDialog, "openFileDialog");
            // 
            // noUpgradeRadioButton
            // 
            resources.ApplyResources(this.noUpgradeRadioButton, "noUpgradeRadioButton");
            this.noUpgradeRadioButton.MinimumSize = new System.Drawing.Size(195, 17);
            this.noUpgradeRadioButton.Name = "noUpgradeRadioButton";
            this.noUpgradeRadioButton.TabStop = true;
            this.noUpgradeRadioButton.UseVisualStyleBackColor = true;
            this.noUpgradeRadioButton.Validated += new System.EventHandler(this.AnyRadioButton_Validated);
            this.noUpgradeRadioButton.CheckedChanged += new System.EventHandler(this.NoUpgradeRadioButton_CheckedChanged);
            // 
            // feedUpgradeRadioButton
            // 
            resources.ApplyResources(this.feedUpgradeRadioButton, "feedUpgradeRadioButton");
            this.feedUpgradeRadioButton.MinimumSize = new System.Drawing.Size(300, 17);
            this.feedUpgradeRadioButton.Name = "feedUpgradeRadioButton";
            this.feedUpgradeRadioButton.TabStop = true;
            this.feedUpgradeRadioButton.UseVisualStyleBackColor = true;
            this.feedUpgradeRadioButton.Validated += new System.EventHandler(this.AnyRadioButton_Validated);
            this.feedUpgradeRadioButton.CheckedChanged += new System.EventHandler(this.FeedUpgradeRadioButton_CheckedChanged);
            // 
            // upgradePathRadioButton
            // 
            resources.ApplyResources(this.upgradePathRadioButton, "upgradePathRadioButton");
            this.upgradePathRadioButton.MinimumSize = new System.Drawing.Size(188, 17);
            this.upgradePathRadioButton.Name = "upgradePathRadioButton";
            this.upgradePathRadioButton.TabStop = true;
            this.upgradePathRadioButton.UseVisualStyleBackColor = true;
            this.upgradePathRadioButton.Validated += new System.EventHandler(this.AnyRadioButton_Validated);
            this.upgradePathRadioButton.CheckedChanged += new System.EventHandler(this.UpgradePathRadioButton_CheckedChanged);
            // 
            // bottomButtonTableLayoutPanel
            // 
            resources.ApplyResources(this.bottomButtonTableLayoutPanel, "bottomButtonTableLayoutPanel");
            this.bottomButtonTableLayoutPanel.Controls.Add(this.browseButton, 0, 0);
            this.bottomButtonTableLayoutPanel.Name = "bottomButtonTableLayoutPanel";
            // 
            // UpdateInfoStep
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.bottomButtonTableLayoutPanel);
            this.Controls.Add(this.upgradePathRadioButton);
            this.Controls.Add(this.feedUpgradeRadioButton);
            this.Controls.Add(this.noUpgradeRadioButton);
            this.Controls.Add(this.previousPathTextBox);
            this.Name = "UpdateInfoStep";
            this.Tag = "Optionally, you can provide the path to the previous version of the application a" +
                "nd an upgrade package will be created.";
            this.bottomButtonTableLayoutPanel.ResumeLayout(false);
            this.bottomButtonTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel bottomButtonTableLayoutPanel;
    }
}
