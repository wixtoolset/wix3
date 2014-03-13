//-------------------------------------------------------------------------------------------------
// <copyright file="PackageInfoStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Step to get package information for application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    /// <summary>
    /// Step to get package information for application.
    /// </summary>
    public sealed partial class PackageInfoStep
    {
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label manufacturerLabel;
        private System.Windows.Forms.TextBox manufacturerTextBox;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.TextBox versionTextBox;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PackageInfoStep));
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.manufacturerLabel = new System.Windows.Forms.Label();
            this.manufacturerTextBox = new System.Windows.Forms.TextBox();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.versionLabel = new System.Windows.Forms.Label();
            this.versionTextBox = new System.Windows.Forms.TextBox();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.topTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.mainTableLayoutPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // nameLabel
            // 
            resources.ApplyResources(this.nameLabel, "nameLabel");
            this.nameLabel.MinimumSize = new System.Drawing.Size(93, 13);
            this.nameLabel.Name = "nameLabel";
            // 
            // nameTextBox
            // 
            resources.ApplyResources(this.nameTextBox, "nameTextBox");
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.nameTextBox.Validated += new System.EventHandler(this.NameTextBox_Validated);
            // 
            // manufacturerLabel
            // 
            resources.ApplyResources(this.manufacturerLabel, "manufacturerLabel");
            this.manufacturerLabel.MinimumSize = new System.Drawing.Size(93, 13);
            this.manufacturerLabel.Name = "manufacturerLabel";
            // 
            // manufacturerTextBox
            // 
            resources.ApplyResources(this.manufacturerTextBox, "manufacturerTextBox");
            this.manufacturerTextBox.Name = "manufacturerTextBox";
            this.manufacturerTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.manufacturerTextBox.Validated += new System.EventHandler(this.ManufacturerTextBox_Validated);
            // 
            // descriptionLabel
            // 
            resources.ApplyResources(this.descriptionLabel, "descriptionLabel");
            this.descriptionLabel.MinimumSize = new System.Drawing.Size(63, 13);
            this.descriptionLabel.Name = "descriptionLabel";
            // 
            // descriptionTextBox
            // 
            resources.ApplyResources(this.descriptionTextBox, "descriptionTextBox");
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.descriptionTextBox.Validated += new System.EventHandler(this.DescriptionTextBox_Validated);
            // 
            // versionLabel
            // 
            resources.ApplyResources(this.versionLabel, "versionLabel");
            this.versionLabel.MinimumSize = new System.Drawing.Size(63, 13);
            this.versionLabel.Name = "versionLabel";
            // 
            // versionTextBox
            // 
            resources.ApplyResources(this.versionTextBox, "versionTextBox");
            this.versionTextBox.Name = "versionTextBox";
            this.versionTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.versionTextBox.Validated += new System.EventHandler(this.VersionTextBox_Validated);
            this.versionTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.VersionTextBox_Validating);
            // 
            // mainTableLayoutPanel
            // 
            resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
            this.mainTableLayoutPanel.Controls.Add(this.versionLabel, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.descriptionTextBox, 1, 2);
            this.mainTableLayoutPanel.Controls.Add(this.topTableLayoutPanel, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.versionTextBox, 1, 1);
            this.mainTableLayoutPanel.Controls.Add(this.descriptionLabel, 0, 2);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            // 
            // topTableLayoutPanel
            // 
            resources.ApplyResources(this.topTableLayoutPanel, "topTableLayoutPanel");
            this.mainTableLayoutPanel.SetColumnSpan(this.topTableLayoutPanel, 2);
            this.topTableLayoutPanel.Controls.Add(this.nameLabel, 0, 0);
            this.topTableLayoutPanel.Controls.Add(this.nameTextBox, 1, 0);
            this.topTableLayoutPanel.Controls.Add(this.manufacturerLabel, 0, 1);
            this.topTableLayoutPanel.Controls.Add(this.manufacturerTextBox, 1, 1);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            // 
            // PackageInfoStep
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Name = "PackageInfoStep";
            this.Tag = "Provide the general information about your applcation.  Most of this information " +
                "will be auto-populated for you after an entry point is selected in Step 2.";
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            this.topTableLayoutPanel.ResumeLayout(false);
            this.topTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel topTableLayoutPanel;
    }
}
