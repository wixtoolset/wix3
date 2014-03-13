//-------------------------------------------------------------------------------------------------
// <copyright file="WixInstallerPropertyPagePanel.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    partial class WixInstallerPropertyPagePanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WixInstallerPropertyPagePanel));
            this.outputNameTextBox = new System.Windows.Forms.TextBox();
            this.comboOutputType = new System.Windows.Forms.ComboBox();
            this.outputTypeLabel = new System.Windows.Forms.Label();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.outputNameLabel = new System.Windows.Forms.Label();
            this.mainTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // outputNameLabel
            // 
            resources.ApplyResources(this.outputNameLabel, "outputNameLabel");
            this.outputNameLabel.MinimumSize = new System.Drawing.Size(260, 18);
            this.outputNameLabel.Name = "outputNameLabel";
            // 
            // outputNameTextBox
            // 
            resources.ApplyResources(this.outputNameTextBox, "outputNameTextBox");
            this.outputNameTextBox.Name = "outputNameTextBox";
            // 
            // comboOutputType
            // 
            this.comboOutputType.AllowDrop = true;
            this.comboOutputType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboOutputType.FormattingEnabled = true;
            this.comboOutputType.Items.AddRange(new object[] {
            resources.GetString("comboOutputType.Items"),
            resources.GetString("comboOutputType.Items1"),
            resources.GetString("comboOutputType.Items2"),
            resources.GetString("comboOutputType.Items3")});
            resources.ApplyResources(this.comboOutputType, "comboOutputType");
            this.comboOutputType.Name = "comboOutputType";
            // 
            // outputTypeLabel
            // 
            resources.ApplyResources(this.outputTypeLabel, "outputTypeLabel");
            this.outputTypeLabel.MinimumSize = new System.Drawing.Size(266, 18);
            this.outputTypeLabel.Name = "outputTypeLabel";
            // 
            // mainTableLayoutPanel
            // 
            resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
            this.mainTableLayoutPanel.Controls.Add(this.outputNameLabel, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.comboOutputType, 0, 3);
            this.mainTableLayoutPanel.Controls.Add(this.outputTypeLabel, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.outputNameTextBox, 0, 1);
            this.mainTableLayoutPanel.MinimumSize = new System.Drawing.Size(270, 94);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            // 
            // WixInstallerPropertyPagePanel
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainTableLayoutPanel);
            this.MinimumSize = new System.Drawing.Size(270, 94);
            this.Name = "WixInstallerPropertyPagePanel";
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox outputNameTextBox;
        private System.Windows.Forms.ComboBox comboOutputType;
        private System.Windows.Forms.Label outputTypeLabel;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.Label outputNameLabel;
    }
}
