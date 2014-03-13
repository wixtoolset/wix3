//-------------------------------------------------------------------------------------------------
// <copyright file="WelcomePage.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//   Wlecome page for ClickThrough UI.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough
{
    /// <summary>
    /// ClickThrough UI welcome page.
    /// </summary>
    internal partial class WelcomePage
    {
        private System.Windows.Forms.ComboBox extensionComboBox;
        private System.Windows.Forms.Label introLabel;
        private System.Windows.Forms.LinkLabel openLink;
        private System.Windows.Forms.Label selectLabel;
        private Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp.WixBanner wixBanner;

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
            this.openLink = new System.Windows.Forms.LinkLabel();
            this.extensionComboBox = new System.Windows.Forms.ComboBox();
            this.introLabel = new System.Windows.Forms.Label();
            this.selectLabel = new System.Windows.Forms.Label();
            this.wixBanner = new Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp.WixBanner();
            this.SuspendLayout();
            // 
            // openLink
            // 
            this.openLink.AutoSize = true;
            this.openLink.Location = new System.Drawing.Point(31, 224);
            this.openLink.Name = "openLink";
            this.openLink.Size = new System.Drawing.Size(192, 13);
            this.openLink.TabIndex = 7;
            this.openLink.TabStop = true;
            this.openLink.Text = "&Open Existing ClickThrough Data File...";
            this.openLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OpenLink_LinkClicked);
            // 
            // fabricatorComboBox
            // 
            this.extensionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.extensionComboBox.FormattingEnabled = true;
            this.extensionComboBox.Items.AddRange(new object[] {
            "ClickThrough for Isolated Applications",
            "ClickThrough for Office Add-ins"});
            this.extensionComboBox.Location = new System.Drawing.Point(15, 160);
            this.extensionComboBox.Name = "fabricatorComboBox";
            this.extensionComboBox.Size = new System.Drawing.Size(333, 21);
            this.extensionComboBox.TabIndex = 6;
            this.extensionComboBox.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SelectedIndexChanged);
            // 
            // introLabel
            // 
            this.introLabel.Location = new System.Drawing.Point(12, 89);
            this.introLabel.Name = "introLabel";
            this.introLabel.Size = new System.Drawing.Size(336, 41);
            this.introLabel.TabIndex = 5;
            this.introLabel.Text = "ClickThrough makes it easy to create installation packages for your applications." +
                "  This UI provides step-by-step instructions on how to proceed.";
            // 
            // selectLabel
            // 
            this.selectLabel.Location = new System.Drawing.Point(12, 141);
            this.selectLabel.Name = "selectLabel";
            this.selectLabel.Size = new System.Drawing.Size(347, 16);
            this.selectLabel.TabIndex = 8;
            this.selectLabel.Text = "Select an application type below or open a previously saved document:";
            // 
            // wixBanner
            // 
            this.wixBanner.BackColor = System.Drawing.Color.Transparent;
            this.wixBanner.Dock = System.Windows.Forms.DockStyle.Top;
            this.wixBanner.Font = new System.Drawing.Font("Verdana", 14F, System.Drawing.FontStyle.Bold);
            this.wixBanner.ForeColor = System.Drawing.Color.White;
            this.wixBanner.Location = new System.Drawing.Point(0, 0);
            this.wixBanner.MinimumSize = new System.Drawing.Size(121, 90);
            this.wixBanner.Name = "wixBanner";
            this.wixBanner.Size = new System.Drawing.Size(418, 90);
            this.wixBanner.TabIndex = 9;
            this.wixBanner.Text = "ClickThrough";
            // 
            // WelcomePage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.wixBanner);
            this.Controls.Add(this.selectLabel);
            this.Controls.Add(this.openLink);
            this.Controls.Add(this.extensionComboBox);
            this.Controls.Add(this.introLabel);
            this.Name = "WelcomePage";
            this.Size = new System.Drawing.Size(418, 247);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
