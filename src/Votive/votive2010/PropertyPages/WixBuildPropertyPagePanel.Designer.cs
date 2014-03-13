//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildPropertyPagePanel.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    partial class WixBuildPropertyPagePanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WixBuildPropertyPagePanel));
            Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox generalGroupBox;
            this.generalTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.defineDebugCheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.defineConstantsTextBox = new System.Windows.Forms.TextBox();
            this.defineVariablesTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.culturesTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.outputTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.outputPathFolderBrowser = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.FolderBrowserTextBox();
            this.bindFilesCheckBox = new System.Windows.Forms.CheckBox();
            this.leaveTempFilesCheckBox = new System.Windows.Forms.CheckBox();
            this.suppressWixPdbCheckBox = new System.Windows.Forms.CheckBox();
            this.warningsAsErrorsCheckBox = new System.Windows.Forms.CheckBox();
            this.messagesGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            this.messageTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.verboseOutputCheckBox = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.warningLevelCombo = new System.Windows.Forms.ComboBox();
            this.suppressedWarningsText = new System.Windows.Forms.TextBox();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.defineConstantsExampleLabel = new System.Windows.Forms.Label();
            this.defineConstantsLabel = new System.Windows.Forms.Label();
            generalGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            this.outputPathLabel = new System.Windows.Forms.Label();
            this.outputGroupBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox();
            generalGroupBox.SuspendLayout();
            this.generalTableLayoutPanel.SuspendLayout();
            this.outputGroupBox.SuspendLayout();
            this.outputTableLayoutPanel.SuspendLayout();
            this.messagesGroupBox.SuspendLayout();
            this.messageTableLayoutPanel.SuspendLayout();
            this.mainTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // defineConstantsExampleLabel
            // 
            resources.ApplyResources(this.defineConstantsExampleLabel, "defineConstantsExampleLabel");
            this.defineConstantsExampleLabel.Name = "defineConstantsExampleLabel";
            // 
            // defineConstantsLabel
            // 
            resources.ApplyResources(this.defineConstantsLabel, "defineConstantsLabel");
            this.defineConstantsLabel.MinimumSize = new System.Drawing.Size(170, 13);
            this.defineConstantsLabel.Name = "defineConstantsLabel";
            // 
            // generalGroupBox
            // 
            resources.ApplyResources(generalGroupBox, "generalGroupBox");
            generalGroupBox.Controls.Add(this.generalTableLayoutPanel);
            generalGroupBox.Name = "generalGroupBox";
            // 
            // generalTableLayoutPanel
            // 
            resources.ApplyResources(this.generalTableLayoutPanel, "generalTableLayoutPanel");
            this.generalTableLayoutPanel.Controls.Add(this.defineConstantsLabel, 1, 2);
            this.generalTableLayoutPanel.Controls.Add(this.label3, 1, 6);
            this.generalTableLayoutPanel.Controls.Add(this.defineDebugCheckBox, 1, 0);
            this.generalTableLayoutPanel.Controls.Add(this.label1, 1, 4);
            this.generalTableLayoutPanel.Controls.Add(this.defineConstantsTextBox, 2, 2);
            this.generalTableLayoutPanel.Controls.Add(this.defineConstantsExampleLabel, 2, 3);
            this.generalTableLayoutPanel.Controls.Add(this.defineVariablesTextBox, 2, 4);
            this.generalTableLayoutPanel.Controls.Add(this.label2, 2, 5);
            this.generalTableLayoutPanel.Controls.Add(this.culturesTextBox, 2, 6);
            this.generalTableLayoutPanel.Controls.Add(this.label4, 2, 7);
            this.generalTableLayoutPanel.Name = "generalTableLayoutPanel";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.MinimumSize = new System.Drawing.Size(170, 13);
            this.label3.Name = "label3";
            // 
            // defineDebugCheckBox
            // 
            resources.ApplyResources(this.defineDebugCheckBox, "defineDebugCheckBox");
            this.generalTableLayoutPanel.SetColumnSpan(this.defineDebugCheckBox, 2);
            this.defineDebugCheckBox.Name = "defineDebugCheckBox";
            this.defineDebugCheckBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.MinimumSize = new System.Drawing.Size(170, 13);
            this.label1.Name = "label1";
            // 
            // defineConstantsTextBox
            // 
            resources.ApplyResources(this.defineConstantsTextBox, "defineConstantsTextBox");
            this.defineConstantsTextBox.Name = "defineConstantsTextBox";
            // 
            // defineVariablesTextBox
            // 
            resources.ApplyResources(this.defineVariablesTextBox, "defineVariablesTextBox");
            this.defineVariablesTextBox.Name = "defineVariablesTextBox";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // culturesTextBox
            // 
            resources.ApplyResources(this.culturesTextBox, "culturesTextBox");
            this.culturesTextBox.Name = "culturesTextBox";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // outputPathLabel
            // 
            resources.ApplyResources(this.outputPathLabel, "outputPathLabel");
            this.outputPathLabel.MinimumSize = new System.Drawing.Size(170, 23);
            this.outputPathLabel.Name = "outputPathLabel";
            // 
            // outputGroupBox
            // 
            resources.ApplyResources(this.outputGroupBox, "outputGroupBox");
            this.outputGroupBox.Controls.Add(this.outputTableLayoutPanel);
            this.outputGroupBox.Name = "outputGroupBox";
            // 
            // outputTableLayoutPanel
            // 
            resources.ApplyResources(this.outputTableLayoutPanel, "outputTableLayoutPanel");
            this.outputTableLayoutPanel.Controls.Add(this.outputPathFolderBrowser, 2, 0);
            this.outputTableLayoutPanel.Controls.Add(this.outputPathLabel, 1, 0);
            this.outputTableLayoutPanel.Controls.Add(this.bindFilesCheckBox, 1, 3);
            this.outputTableLayoutPanel.Controls.Add(this.leaveTempFilesCheckBox, 1, 1);
            this.outputTableLayoutPanel.Controls.Add(this.suppressWixPdbCheckBox, 1, 2);
            this.outputTableLayoutPanel.Name = "outputTableLayoutPanel";
            // 
            // outputPathFolderBrowser
            // 
            resources.ApplyResources(this.outputPathFolderBrowser, "outputPathFolderBrowser");
            this.outputPathFolderBrowser.MinimumSize = new System.Drawing.Size(200, 23);
            this.outputPathFolderBrowser.Name = "outputPathFolderBrowser";
            this.outputPathFolderBrowser.Tag = "";
            // 
            // bindFilesCheckBox
            // 
            resources.ApplyResources(this.bindFilesCheckBox, "bindFilesCheckBox");
            this.outputTableLayoutPanel.SetColumnSpan(this.bindFilesCheckBox, 2);
            this.bindFilesCheckBox.Name = "bindFilesCheckBox";
            this.bindFilesCheckBox.UseVisualStyleBackColor = true;
            // 
            // leaveTempFilesCheckBox
            // 
            resources.ApplyResources(this.leaveTempFilesCheckBox, "leaveTempFilesCheckBox");
            this.outputTableLayoutPanel.SetColumnSpan(this.leaveTempFilesCheckBox, 2);
            this.leaveTempFilesCheckBox.Name = "leaveTempFilesCheckBox";
            this.leaveTempFilesCheckBox.UseVisualStyleBackColor = true;
            // 
            // suppressWixPdbCheckBox
            // 
            resources.ApplyResources(this.suppressWixPdbCheckBox, "suppressWixPdbCheckBox");
            this.outputTableLayoutPanel.SetColumnSpan(this.suppressWixPdbCheckBox, 2);
            this.suppressWixPdbCheckBox.Name = "suppressWixPdbCheckBox";
            this.suppressWixPdbCheckBox.UseVisualStyleBackColor = true;
            // 
            // warningsAsErrorsCheckBox
            // 
            resources.ApplyResources(this.warningsAsErrorsCheckBox, "warningsAsErrorsCheckBox");
            this.messageTableLayoutPanel.SetColumnSpan(this.warningsAsErrorsCheckBox, 2);
            this.warningsAsErrorsCheckBox.Name = "warningsAsErrorsCheckBox";
            // 
            // messagesGroupBox
            // 
            resources.ApplyResources(this.messagesGroupBox, "messagesGroupBox");
            this.messagesGroupBox.Controls.Add(this.messageTableLayoutPanel);
            this.messagesGroupBox.Name = "messagesGroupBox";
            // 
            // messageTableLayoutPanel
            // 
            resources.ApplyResources(this.messageTableLayoutPanel, "messageTableLayoutPanel");
            this.messageTableLayoutPanel.Controls.Add(this.label5, 1, 0);
            this.messageTableLayoutPanel.Controls.Add(this.verboseOutputCheckBox, 1, 3);
            this.messageTableLayoutPanel.Controls.Add(this.label6, 1, 1);
            this.messageTableLayoutPanel.Controls.Add(this.warningLevelCombo, 2, 0);
            this.messageTableLayoutPanel.Controls.Add(this.warningsAsErrorsCheckBox, 1, 2);
            this.messageTableLayoutPanel.Controls.Add(this.suppressedWarningsText, 2, 1);
            this.messageTableLayoutPanel.Name = "messageTableLayoutPanel";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.MinimumSize = new System.Drawing.Size(170, 13);
            this.label5.Name = "label5";
            // 
            // verboseOutputCheckBox
            // 
            resources.ApplyResources(this.verboseOutputCheckBox, "verboseOutputCheckBox");
            this.messageTableLayoutPanel.SetColumnSpan(this.verboseOutputCheckBox, 2);
            this.verboseOutputCheckBox.Name = "verboseOutputCheckBox";
            this.verboseOutputCheckBox.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // warningLevelCombo
            // 
            this.warningLevelCombo.AllowDrop = true;
            resources.ApplyResources(this.warningLevelCombo, "warningLevelCombo");
            this.warningLevelCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.warningLevelCombo.FormattingEnabled = true;
            this.warningLevelCombo.Items.AddRange(new object[] {
            resources.GetString("warningLevelCombo.Items"),
            resources.GetString("warningLevelCombo.Items1"),
            resources.GetString("warningLevelCombo.Items2")});
            this.warningLevelCombo.Name = "warningLevelCombo";
            // 
            // suppressedWarningsText
            // 
            resources.ApplyResources(this.suppressedWarningsText, "suppressedWarningsText");
            this.suppressedWarningsText.Name = "suppressedWarningsText";
            // 
            // mainTableLayoutPanel
            // 
            resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
            this.mainTableLayoutPanel.Controls.Add(generalGroupBox, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.outputGroupBox, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.messagesGroupBox, 0, 1);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            // 
            // WixBuildPropertyPagePanel
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Name = "WixBuildPropertyPagePanel";
            generalGroupBox.ResumeLayout(false);
            this.generalTableLayoutPanel.ResumeLayout(false);
            this.generalTableLayoutPanel.PerformLayout();
            this.outputGroupBox.ResumeLayout(false);
            this.outputTableLayoutPanel.ResumeLayout(false);
            this.outputTableLayoutPanel.PerformLayout();
            this.messagesGroupBox.ResumeLayout(false);
            this.messagesGroupBox.PerformLayout();
            this.messageTableLayoutPanel.ResumeLayout(false);
            this.messageTableLayoutPanel.PerformLayout();
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox warningsAsErrorsCheckBox;
        private System.Windows.Forms.TextBox defineConstantsTextBox;
        private System.Windows.Forms.CheckBox defineDebugCheckBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox defineVariablesTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox culturesTextBox;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox messagesGroupBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox warningLevelCombo;
        private System.Windows.Forms.CheckBox verboseOutputCheckBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox suppressedWarningsText;
        private System.Windows.Forms.CheckBox bindFilesCheckBox;
        private System.Windows.Forms.CheckBox suppressWixPdbCheckBox;
        private System.Windows.Forms.CheckBox leaveTempFilesCheckBox;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.FolderBrowserTextBox outputPathFolderBrowser;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel generalTableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel messageTableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel outputTableLayoutPanel;
        private System.Windows.Forms.Label defineConstantsExampleLabel;
        private System.Windows.Forms.Label defineConstantsLabel;
        private System.Windows.Forms.Label outputPathLabel;
        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixGroupBox outputGroupBox;
    }
}
