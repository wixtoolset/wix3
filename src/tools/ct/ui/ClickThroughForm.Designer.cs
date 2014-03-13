//-------------------------------------------------------------------------------------------------
// <copyright file="ClickThroughForm.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//   Partial class form for ClickThrough UI.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Partial class for ClickThroughForm.
    /// </summary>
    internal partial class ClickThroughForm
    {
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatusLabel;
        private WelcomePage welcomePage;
        private OpenFileDialog openFileDialog;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.welcomePage = new Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough.WelcomePage();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 664);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip.Size = new System.Drawing.Size(812, 22);
            this.statusStrip.TabIndex = 1;
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ctd";
            this.openFileDialog.Filter = "ClickThrough data (*.ctd)|*.ctd|All files (*.*)|*.*";
            // 
            // welcomePage
            // 
            this.welcomePage.AutoScroll = true;
            this.welcomePage.BackColor = System.Drawing.Color.Transparent;
            this.welcomePage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.welcomePage.Location = new System.Drawing.Point(0, 0);
            this.welcomePage.Name = "welcomePage";
            this.welcomePage.Size = new System.Drawing.Size(812, 664);
            this.welcomePage.TabIndex = 2;
            // 
            // ClickThroughForm
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(812, 686);
            this.Controls.Add(this.welcomePage);
            this.Controls.Add(this.statusStrip);
            this.MinimumSize = new System.Drawing.Size(420, 200);
            this.Name = "ClickThroughForm";
            this.Text = "WiX - ClickThrough UI";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

    }
}

