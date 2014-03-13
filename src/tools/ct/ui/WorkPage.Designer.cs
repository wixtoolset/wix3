//-------------------------------------------------------------------------------------------------
// <copyright file="WorkPage.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Page where steps are displayed.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough
{
    /// <summary>
    /// Page where all of the extension step panels are displayed.
    /// </summary>
    internal partial class WorkPage
    {
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel;
        private System.Windows.Forms.LinkLabel saveLink;
        private System.Windows.Forms.LinkLabel backLink;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
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
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.saveLink = new System.Windows.Forms.LinkLabel();
            this.backLink = new System.Windows.Forms.LinkLabel();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.wixBanner = new Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp.WixBanner();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.AutoScroll = true;
            this.flowLayoutPanel.AutoSize = true;
            this.flowLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.flowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel.Location = new System.Drawing.Point(0, 90);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(392, 110);
            this.flowLayoutPanel.TabIndex = 0;
            // 
            // saveLink
            // 
            this.saveLink.AutoSize = true;
            this.saveLink.Location = new System.Drawing.Point(129, 54);
            this.saveLink.Name = "saveLink";
            this.saveLink.Size = new System.Drawing.Size(58, 13);
            this.saveLink.TabIndex = 2;
            this.saveLink.TabStop = true;
            this.saveLink.Text = "&Save work";
            this.saveLink.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.saveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SaveLink_LinkClicked);
            // 
            // backLink
            // 
            this.backLink.AutoSize = true;
            this.backLink.Location = new System.Drawing.Point(3, 54);
            this.backLink.Name = "backLink";
            this.backLink.Size = new System.Drawing.Size(120, 13);
            this.backLink.TabIndex = 3;
            this.backLink.TabStop = true;
            this.backLink.Text = "Back to &Welcome Page";
            this.backLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.backLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.BackLink_LinkClicked);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "ctd";
            this.saveFileDialog.Filter = "ClickThrough Data|*.ctd|All files|*.*";
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
            this.wixBanner.Size = new System.Drawing.Size(392, 90);
            this.wixBanner.TabIndex = 4;
            this.wixBanner.Text = "ClickThrough For Something";
            // 
            // WorkPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.flowLayoutPanel);
            this.Controls.Add(this.wixBanner);
            this.Controls.Add(this.saveLink);
            this.Controls.Add(this.backLink);
            this.Name = "WorkPage";
            this.Size = new System.Drawing.Size(392, 200);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
