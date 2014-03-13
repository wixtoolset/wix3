//-------------------------------------------------------------------------------------------------
// <copyright file="FeedStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Step to provide a feed for the application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    /// <summary>
    /// Step to provide a feed for the application.
    /// </summary>
    internal partial class FeedStep
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
            this.updateTextBox = new System.Windows.Forms.TextBox();
            this.updateRateComboBox = new System.Windows.Forms.ComboBox();
            this.updateRateLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // updateTextBox
            // 
            this.updateTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.updateTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.updateTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.AllUrl;
            this.updateTextBox.Location = new System.Drawing.Point(4, 3);
            this.updateTextBox.Name = "updateTextBox";
            this.updateTextBox.Size = new System.Drawing.Size(293, 20);
            this.updateTextBox.TabIndex = 2;
            this.updateTextBox.Validated += new System.EventHandler(this.UpdateTextBox_Validated);
            this.updateTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.UpdateTextBox_Validating);
            // 
            // updateRateComboBox
            // 
            this.updateRateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.updateRateComboBox.FormattingEnabled = true;
            this.updateRateComboBox.Items.AddRange(new object[] {
            "7 days",
            "3 days",
            "1 day",
            "12 hours",
            "6 hours",
            "1 hour",
            "1 minute"});
            this.updateRateComboBox.Location = new System.Drawing.Point(160, 29);
            this.updateRateComboBox.Name = "updateRateComboBox";
            this.updateRateComboBox.Size = new System.Drawing.Size(137, 21);
            this.updateRateComboBox.TabIndex = 3;
            this.updateRateComboBox.Validated += new System.EventHandler(this.UpdateRateComboBox_Validated);
            // 
            // updateRateLabel
            // 
            this.updateRateLabel.AutoSize = true;
            this.updateRateLabel.Location = new System.Drawing.Point(3, 32);
            this.updateRateLabel.Name = "updateRateLabel";
            this.updateRateLabel.Size = new System.Drawing.Size(151, 13);
            this.updateRateLabel.TabIndex = 4;
            this.updateRateLabel.Text = "Time between update checks:";
            // 
            // FeedStep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.updateRateLabel);
            this.Controls.Add(this.updateRateComboBox);
            this.Controls.Add(this.updateTextBox);
            this.Name = "FeedStep";
            this.Size = new System.Drawing.Size(300, 59);
            this.Tag = "Specify the URL to act as the update feed for this application.";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
