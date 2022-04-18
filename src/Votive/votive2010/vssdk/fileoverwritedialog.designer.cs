// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.VisualStudio.Package
{
    partial class FileOverwriteDialog
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
            try
            {
                if (disposing)
                {
                    if (components != null)
                    {
                        components.Dispose();
                    }

                    if (this.bitmap != null)
                    {
                        this.bitmap.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileOverwriteDialog));
            this.messageText = new System.Windows.Forms.Label();
            this.applyToAll = new System.Windows.Forms.CheckBox();
            this.yesButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.noButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // messageText
            // 
            this.messageText.AutoEllipsis = true;
            resources.ApplyResources(this.messageText, "messageText");
            this.messageText.Name = "messageText";
            // 
            // applyToAll
            // 
            resources.ApplyResources(this.applyToAll, "applyToAll");
            this.applyToAll.Name = "applyToAll";
            this.applyToAll.UseVisualStyleBackColor = true;
            // 
            // yesButton
            // 
            resources.ApplyResources(this.yesButton, "yesButton");
            this.yesButton.Name = "yesButton";
            this.yesButton.UseVisualStyleBackColor = true;
            this.yesButton.Click += new System.EventHandler(this.OnYesButtonClicked);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.OnCancelButtonClicked);
            // 
            // noButton
            // 
            resources.ApplyResources(this.noButton, "noButton");
            this.noButton.Name = "noButton";
            this.noButton.UseVisualStyleBackColor = true;
            this.noButton.Click += new System.EventHandler(this.OnNoButtonClicked);
            // 
            // FileOverwriteDialog
            // 
            this.AcceptButton = this.yesButton;
            this.CancelButton = this.cancelButton;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.noButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.yesButton);
            this.Controls.Add(this.applyToAll);
            this.Controls.Add(this.messageText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FileOverwriteDialog";
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.OnHelpButtonClicked);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.OnHelpRequested);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label messageText;
        private System.Windows.Forms.CheckBox applyToAll;
        private System.Windows.Forms.Button yesButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button noButton;
    }
}
