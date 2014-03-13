//-------------------------------------------------------------------------------------------------
// <copyright file="BuildStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//  Seventh step in the office addin UI for MSI builder for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    /// <summary>
    /// Creates 
    /// </summary>
    internal partial class BuildStep
    {
        private System.Windows.Forms.Button buildButton;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label progressLabel;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;

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
            this.buildButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressLabel = new System.Windows.Forms.Label();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // buildButton
            // 
            this.buildButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buildButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buildButton.Location = new System.Drawing.Point(3, 3);
            this.buildButton.Name = "buildButton";
            this.buildButton.Size = new System.Drawing.Size(86, 24);
            this.buildButton.TabIndex = 18;
            this.buildButton.Text = "&Build...";
            this.buildButton.Click += new System.EventHandler(this.BuildButton_Click);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(3, 53);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(294, 23);
            this.progressBar.TabIndex = 19;
            this.progressBar.Visible = false;
            // 
            // progressLabel
            // 
            this.progressLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressLabel.AutoEllipsis = true;
            this.progressLabel.Location = new System.Drawing.Point(3, 37);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(294, 13);
            this.progressLabel.TabIndex = 20;
            this.progressLabel.Text = "Progress message...";
            // 
            // BuildStep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.progressLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.buildButton);
            this.Name = "BuildStep";
            this.Size = new System.Drawing.Size(300, 85);
            this.Tag = "Build the application syndication feed.  All other steps must be completed before" +
                " the Build button will be activated.";
            this.ResumeLayout(false);

        }

        #endregion
    }
}
