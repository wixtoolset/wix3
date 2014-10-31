//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildEventsPropertyPagePanel.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    partial class WixBuildEventsPropertyPagePanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WixBuildEventsPropertyPagePanel));
            this.preBuildEditor = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor();
            this.postBuildEditor = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor();
            this.runPostBuildComboBox = new System.Windows.Forms.ComboBox();
            this.preBuildGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            this.postBuildGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            this.runLabel = new System.Windows.Forms.Label();
            this.preBuildGroupBox.SuspendLayout();
            this.postBuildGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // preBuildGroupBox
            // 
            this.preBuildGroupBox.Controls.Add(this.preBuildEditor);
            resources.ApplyResources(preBuildGroupBox, "preBuildGroupBox");
            this.preBuildGroupBox.Name = "preBuildGroupBox";
            // 
            // preBuildEditor
            // 
            resources.ApplyResources(this.preBuildEditor, "preBuildEditor");
            this.preBuildEditor.Name = "preBuildEditor";
            // 
            // postBuildGroupBox
            // 
            this.postBuildGroupBox.Controls.Add(this.postBuildEditor);
            this.postBuildGroupBox.Controls.Add(this.runPostBuildComboBox);
            this.postBuildGroupBox.Controls.Add(this.runLabel);
            resources.ApplyResources(this.postBuildGroupBox, "postBuildGroupBox");
            this.postBuildGroupBox.Name = "postBuildGroupBox";
            // 
            // postBuildEditor
            // 
            resources.ApplyResources(this.postBuildEditor, "postBuildEditor");
            this.postBuildEditor.Name = "postBuildEditor";
            // 
            // runPostBuildComboBox
            // 
            resources.ApplyResources(this.runPostBuildComboBox, "runPostBuildComboBox");
            this.runPostBuildComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.runPostBuildComboBox.FormattingEnabled = true;
            this.runPostBuildComboBox.Items.AddRange(new object[] {
            resources.GetString("runPostBuildComboBox.Items"),
            resources.GetString("runPostBuildComboBox.Items1"),
            resources.GetString("runPostBuildComboBox.Items2")});
            this.runPostBuildComboBox.Name = "runPostBuildComboBox";
            // 
            // runLabel
            // 
            resources.ApplyResources(this.runLabel, "runLabel");
            this.runLabel.AutoEllipsis = true;
            this.runLabel.Name = "runLabel";
            // 
            // WixBuildEventsPropertyPagePanel
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.postBuildGroupBox);
            this.Controls.Add(this.preBuildGroupBox);
            this.Name = "WixBuildEventsPropertyPagePanel";
            this.preBuildGroupBox.ResumeLayout(false);
            this.postBuildGroupBox.ResumeLayout(false);
            this.postBuildGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor preBuildEditor;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor postBuildEditor;
        private System.Windows.Forms.ComboBox runPostBuildComboBox;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox preBuildGroupBox;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox postBuildGroupBox;
        private System.Windows.Forms.Label runLabel;

    }
}
