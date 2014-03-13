//-------------------------------------------------------------------------------------------------
// <copyright file="PackageInfoStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//  Package info for the Office Addin UI for MSI builder for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    /// <summary>
    /// Step to get package information about Office Addin.
    /// </summary>
    internal partial class PackageInfoStep
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
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.manufacturerLabel = new System.Windows.Forms.Label();
            this.manufacturerTextBox = new System.Windows.Forms.TextBox();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.versionLabel = new System.Windows.Forms.Label();
            this.versionTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // nameLabel
            // 
            this.nameLabel.AutoSize = true;
            this.nameLabel.Location = new System.Drawing.Point(3, 6);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(93, 13);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "Application &Name:";
            // 
            // nameTextBox
            // 
            this.nameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.nameTextBox.Location = new System.Drawing.Point(102, 3);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(195, 20);
            this.nameTextBox.TabIndex = 1;
            this.nameTextBox.Validated += new System.EventHandler(this.NameTextBox_Validated);
            this.nameTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            // 
            // manufacturerLabel
            // 
            this.manufacturerLabel.AutoSize = true;
            this.manufacturerLabel.Location = new System.Drawing.Point(21, 33);
            this.manufacturerLabel.Name = "manufacturerLabel";
            this.manufacturerLabel.Size = new System.Drawing.Size(73, 13);
            this.manufacturerLabel.TabIndex = 2;
            this.manufacturerLabel.Text = "&Manufacturer:";
            // 
            // manufacturerTextBox
            // 
            this.manufacturerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.manufacturerTextBox.Location = new System.Drawing.Point(102, 30);
            this.manufacturerTextBox.Name = "manufacturerTextBox";
            this.manufacturerTextBox.Size = new System.Drawing.Size(195, 20);
            this.manufacturerTextBox.TabIndex = 3;
            this.manufacturerTextBox.Validated += new System.EventHandler(this.ManufacturerTextBox_Validated);
            this.manufacturerTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.AutoSize = true;
            this.descriptionLabel.Location = new System.Drawing.Point(3, 85);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(63, 13);
            this.descriptionLabel.TabIndex = 6;
            this.descriptionLabel.Text = "&Description:";
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionTextBox.Location = new System.Drawing.Point(72, 85);
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.Size = new System.Drawing.Size(225, 97);
            this.descriptionTextBox.TabIndex = 7;
            this.descriptionTextBox.Validated += new System.EventHandler(this.DescriptionTextBox_Validated);
            this.descriptionTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            // 
            // versionLabel
            // 
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point(21, 60);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(45, 13);
            this.versionLabel.TabIndex = 4;
            this.versionLabel.Text = "&Version:";
            // 
            // versionTextBox
            // 
            this.versionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.versionTextBox.Location = new System.Drawing.Point(72, 57);
            this.versionTextBox.Name = "versionTextBox";
            this.versionTextBox.Size = new System.Drawing.Size(225, 20);
            this.versionTextBox.TabIndex = 5;
            this.versionTextBox.Validated += new System.EventHandler(this.VersionTextBox_Validated);
            this.versionTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.VersionTextBox_Validating);
            this.versionTextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            // 
            // PackageInfoStep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.descriptionTextBox);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.versionTextBox);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.manufacturerTextBox);
            this.Controls.Add(this.manufacturerLabel);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.nameLabel);
            this.Name = "PackageInfoStep";
            this.Size = new System.Drawing.Size(300, 187);
            this.Tag = "Provide the general information about your applcation.  Most of this information " +
                "will be auto-populated for you after an entry point is selected in Step 2.";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
