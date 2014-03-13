//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildEventEditorForm.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Forms
{
    partial class WixBuildEventEditorForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.ColumnHeader nameColumnHeader;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WixBuildEventEditorForm));
            System.Windows.Forms.ColumnHeader valueColumnHeader;
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.macrosListView = new System.Windows.Forms.ListView();
            this.insertButton = new System.Windows.Forms.Button();
            this.centerPanel = new System.Windows.Forms.Panel();
            this.contentTextBoxPanel = new System.Windows.Forms.Panel();
            this.contentTextBox = new Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventTextBox();
            this.bottomButtonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            nameColumnHeader = new System.Windows.Forms.ColumnHeader();
            valueColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.centerPanel.SuspendLayout();
            this.contentTextBoxPanel.SuspendLayout();
            this.bottomButtonTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // nameColumnHeader
            // 
            resources.ApplyResources(nameColumnHeader, "nameColumnHeader");
            // 
            // valueColumnHeader
            // 
            resources.ApplyResources(valueColumnHeader, "valueColumnHeader");
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.MinimumSize = new System.Drawing.Size(90, 23);
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.MinimumSize = new System.Drawing.Size(90, 23);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // macrosListView
            // 
            this.macrosListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            nameColumnHeader,
            valueColumnHeader});
            resources.ApplyResources(this.macrosListView, "macrosListView");
            this.macrosListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.macrosListView.HideSelection = false;
            this.macrosListView.MultiSelect = false;
            this.macrosListView.Name = "macrosListView";
            this.macrosListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.macrosListView.UseCompatibleStateImageBehavior = false;
            this.macrosListView.View = System.Windows.Forms.View.Details;
            this.macrosListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnMacrosListViewMouseDoubleClick);
            this.macrosListView.SelectedIndexChanged += new System.EventHandler(this.OnMacrosListViewSelectedIndexChanged);
            // 
            // insertButton
            // 
            resources.ApplyResources(this.insertButton, "insertButton");
            this.insertButton.MinimumSize = new System.Drawing.Size(90, 23);
            this.insertButton.Name = "insertButton";
            this.insertButton.UseVisualStyleBackColor = true;
            this.insertButton.Click += new System.EventHandler(this.OnInsertButtonClick);
            // 
            // centerPanel
            // 
            resources.ApplyResources(this.centerPanel, "centerPanel");
            this.centerPanel.Controls.Add(this.contentTextBoxPanel);
            this.centerPanel.Controls.Add(this.macrosListView);
            this.centerPanel.Name = "centerPanel";
            // 
            // contentTextBoxPanel
            // 
            this.contentTextBoxPanel.Controls.Add(this.contentTextBox);
            resources.ApplyResources(this.contentTextBoxPanel, "contentTextBoxPanel");
            this.contentTextBoxPanel.Name = "contentTextBoxPanel";
            // 
            // contentTextBox
            // 
            resources.ApplyResources(this.contentTextBox, "contentTextBox");
            this.contentTextBox.Name = "contentTextBox";
            // 
            // bottomButtonTableLayoutPanel
            // 
            resources.ApplyResources(this.bottomButtonTableLayoutPanel, "bottomButtonTableLayoutPanel");
            this.bottomButtonTableLayoutPanel.Controls.Add(this.okButton, 0, 0);
            this.bottomButtonTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.bottomButtonTableLayoutPanel.Name = "bottomButtonTableLayoutPanel";
            // 
            // WixBuildEventEditorForm
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.bottomButtonTableLayoutPanel);
            this.Controls.Add(this.centerPanel);
            this.Controls.Add(this.insertButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WixBuildEventEditorForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.centerPanel.ResumeLayout(false);
            this.contentTextBoxPanel.ResumeLayout(false);
            this.contentTextBoxPanel.PerformLayout();
            this.bottomButtonTableLayoutPanel.ResumeLayout(false);
            this.bottomButtonTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls.WixBuildEventTextBox contentTextBox;
        private System.Windows.Forms.ListView macrosListView;
        private System.Windows.Forms.Button insertButton;
        private System.Windows.Forms.Panel centerPanel;
        private System.Windows.Forms.Panel contentTextBoxPanel;
        private System.Windows.Forms.TableLayoutPanel bottomButtonTableLayoutPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}