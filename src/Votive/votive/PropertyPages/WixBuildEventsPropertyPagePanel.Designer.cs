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
            Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox preBuildGroupBox;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WixBuildEventsPropertyPagePanel));
            Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox postBuildGroupBox;
            System.Windows.Forms.Label runLabel;
            this.preBuildEditor = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor();
            this.postBuildEditor = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor();
            this.runPostBuildComboBox = new System.Windows.Forms.ComboBox();
            preBuildGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            postBuildGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            runLabel = new System.Windows.Forms.Label();
            preBuildGroupBox.SuspendLayout();
            postBuildGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // preBuildGroupBox
            // 
            preBuildGroupBox.Controls.Add(this.preBuildEditor);
            resources.ApplyResources(preBuildGroupBox, "preBuildGroupBox");
            preBuildGroupBox.Name = "preBuildGroupBox";
            // 
            // preBuildEditor
            // 
            resources.ApplyResources(this.preBuildEditor, "preBuildEditor");
            this.preBuildEditor.Name = "preBuildEditor";
            // 
            // postBuildGroupBox
            // 
            postBuildGroupBox.Controls.Add(this.postBuildEditor);
            postBuildGroupBox.Controls.Add(this.runPostBuildComboBox);
            postBuildGroupBox.Controls.Add(runLabel);
            resources.ApplyResources(postBuildGroupBox, "postBuildGroupBox");
            postBuildGroupBox.Name = "postBuildGroupBox";
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
            resources.ApplyResources(runLabel, "runLabel");
            runLabel.AutoEllipsis = true;
            runLabel.Name = "runLabel";
            // 
            // WixBuildEventsPropertyPagePanel
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(postBuildGroupBox);
            this.Controls.Add(preBuildGroupBox);
            this.Name = "WixBuildEventsPropertyPagePanel";
            preBuildGroupBox.ResumeLayout(false);
            postBuildGroupBox.ResumeLayout(false);
            postBuildGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor preBuildEditor;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventEditor postBuildEditor;
        private System.Windows.Forms.ComboBox runPostBuildComboBox;


    }
}
