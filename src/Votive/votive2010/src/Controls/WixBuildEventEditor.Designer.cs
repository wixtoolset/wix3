// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls
{
    partial class WixBuildEventEditor
    {
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WixBuildEventEditor));
            this.editButton = new System.Windows.Forms.Button();
            this.contentTextBox = new WixBuildEventTextBox();
            this.bottomButtonPanel = new System.Windows.Forms.Panel();
            this.bottomButtonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // editButton
            // 
            resources.ApplyResources(this.editButton, "editButton");
            this.editButton.MinimumSize = new System.Drawing.Size(90, 23);
            this.editButton.Name = "editButton";
            this.editButton.UseVisualStyleBackColor = true;
            this.editButton.Click += new System.EventHandler(this.OnEditButtonClick);
            // 
            // contentTextBox
            // 
            resources.ApplyResources(this.contentTextBox, "contentTextBox");
            this.contentTextBox.Name = "contentTextBox";
            // 
            // bottomButtonPanel
            // 
            this.bottomButtonPanel.Controls.Add(this.editButton);
            resources.ApplyResources(this.bottomButtonPanel, "bottomButtonPanel");
            this.bottomButtonPanel.Name = "bottomButtonPanel";
            // 
            // WixBuildEventEditor
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.contentTextBox);
            this.Controls.Add(this.bottomButtonPanel);
            this.Name = "WixBuildEventEditor";
            this.bottomButtonPanel.ResumeLayout(false);
            this.bottomButtonPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventTextBox contentTextBox;
        private System.Windows.Forms.Button editButton;
        private System.Windows.Forms.Panel bottomButtonPanel;
    }
}
