// <copyright file="FeedStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Feed step.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    /// <summary>
    /// Feed step.
    /// </summary>
    public sealed partial class FeedStep
    {
        private System.Windows.Forms.TextBox updateTextBox;
        private System.Windows.Forms.ComboBox updateRateComboBox;
        private System.Windows.Forms.Label updateRateLabel;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FeedStep));
            this.updateTextBox = new System.Windows.Forms.TextBox();
            this.updateRateComboBox = new System.Windows.Forms.ComboBox();
            this.updateRateLabel = new System.Windows.Forms.Label();
            this.bottomTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.bottomTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // updateTextBox
            // 
            resources.ApplyResources(this.updateTextBox, "updateTextBox");
            this.updateTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.updateTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.AllUrl;
            this.updateTextBox.Name = "updateTextBox";
            this.updateTextBox.Validated += new System.EventHandler(this.UpdateTextBox_Validated);
            this.updateTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.UpdateTextBox_Validating);
            // 
            // updateRateComboBox
            // 
            resources.ApplyResources(this.updateRateComboBox, "updateRateComboBox");
            this.updateRateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.updateRateComboBox.FormattingEnabled = true;
            this.updateRateComboBox.Items.AddRange(new object[] {
            resources.GetString("updateRateComboBox.Items"),
            resources.GetString("updateRateComboBox.Items1"),
            resources.GetString("updateRateComboBox.Items2"),
            resources.GetString("updateRateComboBox.Items3"),
            resources.GetString("updateRateComboBox.Items4"),
            resources.GetString("updateRateComboBox.Items5"),
            resources.GetString("updateRateComboBox.Items6")});
            this.updateRateComboBox.MinimumSize = new System.Drawing.Size(90, 0);
            this.updateRateComboBox.Name = "updateRateComboBox";
            this.updateRateComboBox.Validated += new System.EventHandler(this.UpdateRateComboBox_Validated);
            // 
            // updateRateLabel
            // 
            resources.ApplyResources(this.updateRateLabel, "updateRateLabel");
            this.updateRateLabel.Name = "updateRateLabel";
            // 
            // bottomTableLayoutPanel
            // 
            resources.ApplyResources(this.bottomTableLayoutPanel, "bottomTableLayoutPanel");
            this.bottomTableLayoutPanel.Controls.Add(this.updateRateLabel, 0, 0);
            this.bottomTableLayoutPanel.Controls.Add(this.updateRateComboBox, 1, 0);
            this.bottomTableLayoutPanel.Name = "bottomTableLayoutPanel";
            // 
            // FeedStep
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.bottomTableLayoutPanel);
            this.Controls.Add(this.updateTextBox);
            this.Name = "FeedStep";
            this.Tag = "Specify the URL to act as the update feed for this application.";
            this.bottomTableLayoutPanel.ResumeLayout(false);
            this.bottomTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel bottomTableLayoutPanel;
    }
}
