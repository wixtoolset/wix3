//-------------------------------------------------------------------------------------------------
// <copyright file="PickOfficeAppsStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Pick the Office Addin UI for MSI builder for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    /// <summary>
    /// Pick the Office Addin UI for MSI builder for ClickThrough.
    /// </summary>
    internal partial class PickOfficeAppsStep
    {
        private System.Windows.Forms.CheckBox excel2003CheckBox;
        private System.Windows.Forms.CheckBox outlook2003CheckBox;
        private System.Windows.Forms.CheckBox powerPoint2003CheckBox;
        private System.Windows.Forms.CheckBox word2003CheckBox;
        private System.Windows.Forms.CheckBox word2007CheckBox;
        private System.Windows.Forms.CheckBox powerPoint2007CheckBox;
        private System.Windows.Forms.CheckBox outlook2007CheckBox;
        private System.Windows.Forms.CheckBox excel2007CheckBox;

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
            this.excel2003CheckBox = new System.Windows.Forms.CheckBox();
            this.outlook2003CheckBox = new System.Windows.Forms.CheckBox();
            this.powerPoint2003CheckBox = new System.Windows.Forms.CheckBox();
            this.word2003CheckBox = new System.Windows.Forms.CheckBox();
            this.word2007CheckBox = new System.Windows.Forms.CheckBox();
            this.powerPoint2007CheckBox = new System.Windows.Forms.CheckBox();
            this.outlook2007CheckBox = new System.Windows.Forms.CheckBox();
            this.excel2007CheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // excel2003CheckBox
            // 
            this.excel2003CheckBox.AutoSize = true;
            this.excel2003CheckBox.Location = new System.Drawing.Point(3, 3);
            this.excel2003CheckBox.Name = "excel2003CheckBox";
            this.excel2003CheckBox.Size = new System.Drawing.Size(79, 17);
            this.excel2003CheckBox.TabIndex = 0;
            this.excel2003CheckBox.Text = "Excel 2003";
            this.excel2003CheckBox.UseVisualStyleBackColor = true;
            this.excel2003CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // outlook2003CheckBox
            // 
            this.outlook2003CheckBox.AutoSize = true;
            this.outlook2003CheckBox.Location = new System.Drawing.Point(3, 26);
            this.outlook2003CheckBox.Name = "outlook2003CheckBox";
            this.outlook2003CheckBox.Size = new System.Drawing.Size(90, 17);
            this.outlook2003CheckBox.TabIndex = 1;
            this.outlook2003CheckBox.Text = "Outlook 2003";
            this.outlook2003CheckBox.UseVisualStyleBackColor = true;
            this.outlook2003CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // powerPoint2003CheckBox
            // 
            this.powerPoint2003CheckBox.AutoSize = true;
            this.powerPoint2003CheckBox.Location = new System.Drawing.Point(3, 49);
            this.powerPoint2003CheckBox.Name = "powerPoint2003CheckBox";
            this.powerPoint2003CheckBox.Size = new System.Drawing.Size(107, 17);
            this.powerPoint2003CheckBox.TabIndex = 2;
            this.powerPoint2003CheckBox.Text = "PowerPoint 2003";
            this.powerPoint2003CheckBox.UseVisualStyleBackColor = true;
            this.powerPoint2003CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // word2003CheckBox
            // 
            this.word2003CheckBox.AutoSize = true;
            this.word2003CheckBox.Location = new System.Drawing.Point(3, 72);
            this.word2003CheckBox.Name = "word2003CheckBox";
            this.word2003CheckBox.Size = new System.Drawing.Size(79, 17);
            this.word2003CheckBox.TabIndex = 3;
            this.word2003CheckBox.Text = "Word 2003";
            this.word2003CheckBox.UseVisualStyleBackColor = true;
            this.word2003CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // word2007CheckBox
            // 
            this.word2007CheckBox.AutoSize = true;
            this.word2007CheckBox.Enabled = false;
            this.word2007CheckBox.Location = new System.Drawing.Point(189, 72);
            this.word2007CheckBox.Name = "word2007CheckBox";
            this.word2007CheckBox.Size = new System.Drawing.Size(79, 17);
            this.word2007CheckBox.TabIndex = 7;
            this.word2007CheckBox.Text = "Word 2007";
            this.word2007CheckBox.UseVisualStyleBackColor = true;
            this.word2007CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // powerPoint2007CheckBox
            // 
            this.powerPoint2007CheckBox.AutoSize = true;
            this.powerPoint2007CheckBox.Enabled = false;
            this.powerPoint2007CheckBox.Location = new System.Drawing.Point(189, 49);
            this.powerPoint2007CheckBox.Name = "powerPoint2007CheckBox";
            this.powerPoint2007CheckBox.Size = new System.Drawing.Size(107, 17);
            this.powerPoint2007CheckBox.TabIndex = 6;
            this.powerPoint2007CheckBox.Text = "PowerPoint 2007";
            this.powerPoint2007CheckBox.UseVisualStyleBackColor = true;
            this.powerPoint2007CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // outlook2007CheckBox
            // 
            this.outlook2007CheckBox.AutoSize = true;
            this.outlook2007CheckBox.Enabled = false;
            this.outlook2007CheckBox.Location = new System.Drawing.Point(189, 26);
            this.outlook2007CheckBox.Name = "outlook2007CheckBox";
            this.outlook2007CheckBox.Size = new System.Drawing.Size(90, 17);
            this.outlook2007CheckBox.TabIndex = 5;
            this.outlook2007CheckBox.Text = "Outlook 2007";
            this.outlook2007CheckBox.UseVisualStyleBackColor = true;
            this.outlook2007CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // excel2007CheckBox
            // 
            this.excel2007CheckBox.AutoSize = true;
            this.excel2007CheckBox.Enabled = false;
            this.excel2007CheckBox.Location = new System.Drawing.Point(189, 3);
            this.excel2007CheckBox.Name = "excel2007CheckBox";
            this.excel2007CheckBox.Size = new System.Drawing.Size(79, 17);
            this.excel2007CheckBox.TabIndex = 4;
            this.excel2007CheckBox.Text = "Excel 2007";
            this.excel2007CheckBox.UseVisualStyleBackColor = true;
            this.excel2007CheckBox.Validated += new System.EventHandler(this.AnyCheckBox_Validated);
            // 
            // PickOfficeAppsStep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.word2007CheckBox);
            this.Controls.Add(this.powerPoint2007CheckBox);
            this.Controls.Add(this.outlook2007CheckBox);
            this.Controls.Add(this.excel2007CheckBox);
            this.Controls.Add(this.word2003CheckBox);
            this.Controls.Add(this.powerPoint2003CheckBox);
            this.Controls.Add(this.outlook2003CheckBox);
            this.Controls.Add(this.excel2003CheckBox);
            this.Name = "PickOfficeAppsStep";
            this.Size = new System.Drawing.Size(300, 94);
            this.Tag = "Select the Microsoft Office Applications that your add-in supports.";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
