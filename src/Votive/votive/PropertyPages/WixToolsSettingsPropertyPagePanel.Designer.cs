//-------------------------------------------------------------------------------------------------
// <copyright file="WixToolsSettingsPropertyPagePanel.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    partial class WixToolsSettingsPropertyPagePanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WixToolsSettingsPropertyPagePanel));
            this.iceValidationGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            this.suppressIceCheckbox = new System.Windows.Forms.CheckBox();
            this.iceValidationTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.suppressSpecificIceLabel = new System.Windows.Forms.Label();
            this.specificIceTextBox = new System.Windows.Forms.TextBox();
            this.specificIceExampleLabel = new System.Windows.Forms.Label();
            this.additionalParamsGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            this.compilerLabel = new System.Windows.Forms.Label();
            this.linkerTextBox = new System.Windows.Forms.TextBox();
            this.librarianTextBox = new System.Windows.Forms.TextBox();
            this.linkerLibrarianLabel = new System.Windows.Forms.Label();
            this.compilerTextBox = new System.Windows.Forms.TextBox();
            this.iceValidationGroupBox.SuspendLayout();
            this.iceValidationTableLayoutPanel.SuspendLayout();
            this.additionalParamsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // iceValidationGroupBox
            // 
            this.iceValidationGroupBox.Controls.Add(this.suppressIceCheckbox);
            this.iceValidationGroupBox.Controls.Add(this.iceValidationTableLayoutPanel);
            resources.ApplyResources(this.iceValidationGroupBox, "iceValidationGroupBox");
            this.iceValidationGroupBox.Name = "iceValidationGroupBox";
            // 
            // suppressIceCheckbox
            // 
            resources.ApplyResources(this.suppressIceCheckbox, "suppressIceCheckbox");
            this.suppressIceCheckbox.Name = "suppressIceCheckbox";
            this.suppressIceCheckbox.UseVisualStyleBackColor = true;
            // 
            // iceValidationTableLayoutPanel
            // 
            resources.ApplyResources(this.iceValidationTableLayoutPanel, "iceValidationTableLayoutPanel");
            this.iceValidationTableLayoutPanel.Controls.Add(this.suppressSpecificIceLabel, 0, 0);
            this.iceValidationTableLayoutPanel.Controls.Add(this.specificIceTextBox, 1, 0);
            this.iceValidationTableLayoutPanel.Controls.Add(this.specificIceExampleLabel, 1, 1);
            this.iceValidationTableLayoutPanel.Name = "iceValidationTableLayoutPanel";
            // 
            // suppressSpecificIceLabel
            // 
            resources.ApplyResources(this.suppressSpecificIceLabel, "suppressSpecificIceLabel");
            this.suppressSpecificIceLabel.MinimumSize = new System.Drawing.Size(170, 13);
            this.suppressSpecificIceLabel.Name = "suppressSpecificIceLabel";
            // 
            // specificIceTextBox
            // 
            resources.ApplyResources(this.specificIceTextBox, "specificIceTextBox");
            this.specificIceTextBox.Name = "specificIceTextBox";
            // 
            // specificIceExampleLabel
            // 
            resources.ApplyResources(this.specificIceExampleLabel, "specificIceExampleLabel");
            this.specificIceExampleLabel.Name = "specificIceExampleLabel";
            // 
            // additionalParamsGroupBox
            // 
            this.additionalParamsGroupBox.Controls.Add(this.compilerLabel);
            this.additionalParamsGroupBox.Controls.Add(this.linkerTextBox);
            this.additionalParamsGroupBox.Controls.Add(this.librarianTextBox);
            this.additionalParamsGroupBox.Controls.Add(this.linkerLibrarianLabel);
            this.additionalParamsGroupBox.Controls.Add(this.compilerTextBox);
            resources.ApplyResources(this.additionalParamsGroupBox, "additionalParamsGroupBox");
            this.additionalParamsGroupBox.Name = "additionalParamsGroupBox";
            // 
            // compilerLabel
            // 
            resources.ApplyResources(this.compilerLabel, "compilerLabel");
            this.compilerLabel.MinimumSize = new System.Drawing.Size(170, 13);
            this.compilerLabel.Name = "compilerLabel";
            // 
            // linkerTextBox
            // 
            resources.ApplyResources(this.linkerTextBox, "linkerTextBox");
            this.linkerTextBox.Name = "linkerTextBox";
            this.linkerTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            // 
            // librarianTextBox
            // 
            resources.ApplyResources(this.librarianTextBox, "librarianTextBox");
            this.librarianTextBox.Name = "librarianTextBox";
            this.librarianTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            // 
            // linkerLibrarianLabel
            // 
            resources.ApplyResources(this.linkerLibrarianLabel, "linkerLibrarianLabel");
            this.linkerLibrarianLabel.MinimumSize = new System.Drawing.Size(170, 13);
            this.linkerLibrarianLabel.Name = "linkerLibrarianLabel";
            // 
            // compilerTextBox
            // 
            resources.ApplyResources(this.compilerTextBox, "compilerTextBox");
            this.compilerTextBox.Name = "compilerTextBox";
            this.compilerTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            // 
            // WixToolsSettingsPropertyPagePanel
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.additionalParamsGroupBox);
            this.Controls.Add(this.iceValidationGroupBox);
            this.Name = "WixToolsSettingsPropertyPagePanel";
            this.iceValidationGroupBox.ResumeLayout(false);
            this.iceValidationGroupBox.PerformLayout();
            this.iceValidationTableLayoutPanel.ResumeLayout(false);
            this.iceValidationTableLayoutPanel.PerformLayout();
            this.additionalParamsGroupBox.ResumeLayout(false);
            this.additionalParamsGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox iceValidationGroupBox;
        private System.Windows.Forms.CheckBox suppressIceCheckbox;
        private System.Windows.Forms.Label specificIceExampleLabel;
        private System.Windows.Forms.TextBox specificIceTextBox;
        private System.Windows.Forms.Label suppressSpecificIceLabel;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox additionalParamsGroupBox;
        private System.Windows.Forms.Label compilerLabel;
        private System.Windows.Forms.Label linkerLibrarianLabel;
        private System.Windows.Forms.TextBox linkerTextBox;
        private System.Windows.Forms.TextBox librarianTextBox;
        private System.Windows.Forms.TextBox compilerTextBox;
        private System.Windows.Forms.TableLayoutPanel iceValidationTableLayoutPanel;
    }
}
