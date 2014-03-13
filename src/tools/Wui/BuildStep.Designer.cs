// <copyright file="BuildStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Build step.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    /// <summary>
    /// Build step.
    /// </summary>
    public sealed partial class BuildStep
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BuildStep));
            this.buildButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressLabel = new System.Windows.Forms.Label();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // buildButton
            // 
            resources.ApplyResources(this.buildButton, "buildButton");
            this.buildButton.MinimumSize = new System.Drawing.Size(86, 24);
            this.buildButton.Name = "buildButton";
            this.buildButton.Click += new System.EventHandler(this.BuildButton_Click);
            // 
            // progressBar
            // 
            resources.ApplyResources(this.progressBar, "progressBar");
            this.progressBar.Name = "progressBar";
            // 
            // progressLabel
            // 
            resources.ApplyResources(this.progressLabel, "progressLabel");
            this.progressLabel.AutoEllipsis = true;
            this.progressLabel.Name = "progressLabel";
            // 
            // BuildStep
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.progressLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.buildButton);
            this.Name = "BuildStep";
            this.Tag = "Build the application syndication feed.  All other steps must be completed before" +
                " the Build button will be activated.";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
