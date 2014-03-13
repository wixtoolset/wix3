//-------------------------------------------------------------------------------------------------
// <copyright file="ProgressDialog.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Progress dialog for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Summary description for ProgressDialog.
    /// </summary>
    public class ProgressDialog : System.Windows.Forms.Form
    {
        private bool canceled;

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label progressMessage;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public ProgressDialog()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
        }

        /// <summary>
        /// Gets whether the progress dialog has been canceled.
        /// </summary>
        public bool Canceled
        {
            get { return this.canceled; }
        }

        /// <summary>
        /// Gets or sets the current progress in the dialog.
        /// </summary>
        public int Current
        {
            get { return this.progressBar.Value; }
            set { this.progressBar.Value = value; }
        }

        /// <summary>
        /// Gets or sets the upper bound of the progress dialog.
        /// </summary>
        public int UpperBound
        {
            get { return this.progressBar.Maximum; }
            set { this.progressBar.Maximum = value; }
        }

        /// <summary>
        /// Gets or sets the message on the progress dialog.
        /// </summary>
        public string Message
        {
            get { return this.progressMessage.Text; }
            set { this.progressMessage.Text = value; }
        }

        public void Reset()
        {
            this.canceled = false;

            this.SuspendLayout();
            this.progressBar.Value = 0;
            this.progressBar.Maximum = 10;
            this.progressMessage.Text = "Initializing package builder...";
            this.ResumeLayout();
        }

        public void packageBuilder_Progress(object sender, ProgressEventArgs e)
        {
            e.Cancel = this.canceled;

            this.SuspendLayout();
            this.progressMessage.Text = e.Message;
            this.progressBar.Value = e.Current;
            this.progressBar.Maximum = e.UpperBound;
            this.Refresh();
            this.ResumeLayout();

            //System.Threading.Thread.Sleep(2000);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cancelButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(285, 72);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(16, 40);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(344, 23);
            this.progressBar.TabIndex = 1;
            // 
            // progressMessage
            // 
            this.progressMessage.Location = new System.Drawing.Point(16, 16);
            this.progressMessage.Name = "progressMessage";
            this.progressMessage.Size = new System.Drawing.Size(344, 16);
            this.progressMessage.TabIndex = 2;
            this.progressMessage.Text = "Initializing package builder...";
            // 
            // ProgressDialog
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(370, 112);
            this.Controls.Add(this.progressMessage);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ProgressDialog";
            this.Text = "Package Build Progress";
            this.ResumeLayout(false);

        }
        #endregion

        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            this.cancelButton.Enabled = false;
            this.canceled = true;
        }
    }
}
