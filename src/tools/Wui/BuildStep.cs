//-------------------------------------------------------------------------------------------------
// <copyright file="BuildStep.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//  Sixth step in the isolated applications UI for MSI builder for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;
    using System.Threading;
    using System.IO;
    using System.Globalization;

    /// <summary>
    /// Build step.
    /// </summary>
    public sealed partial class BuildStep : UserControl
    {
        private Fabricator fabricator;
        private Uri updateUri;
        private string buildPath;

        private WaitCallback buildCallback;
        private BuildProgressCallback buildProgressCallback;

        /// <summary>
        /// Creates a new BuildStep.
        /// </summary>
        public BuildStep()
        {
            this.InitializeComponent();
            this.buildCallback = new WaitCallback(this.Build);
            this.buildProgressCallback = new BuildProgressCallback(this.BuildProgress);
        }

        /// <summary>
        /// Delegate for progress during the build.
        /// </summary>
        /// <param name="message">.</param>
        /// <param name="current">.</param>
        /// <param name="maximum">.</param>
        private delegate void BuildProgressCallback(string message, int current, int maximum);

        /// <summary>
        /// Gets and sets the fabricator for this step.
        /// </summary>
        /// <value>Fabricator.</value>
        public Fabricator Fabricator
        {
            get { return this.fabricator; }
            set { this.fabricator = value; }
        }

        public Uri UpdateUri
        {
            get
            {
                return this.updateUri;
            }

            set
            {
                if (this.updateUri != value)
                {
                    this.updateUri = value;

                    if (this.updateUri == null)
                    {
                        this.buildButton.Enabled = false;
                    }
                    else
                    {
                        this.buildButton.Enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for when the build button is clicked.
        /// </summary>
        /// <param name="sender">Control that sent the click request.</param>
        /// <param name="e">Event arguments for click.</param>
        private void BuildButton_Click(object sender, EventArgs e)
        {
            if (this.updateUri == null)
            {
                MessageBox.Show("Must specify an update url.", this.fabricator.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                this.saveFileDialog.FileName = Path.GetFileName(this.updateUri.AbsolutePath);
                if (DialogResult.OK == this.saveFileDialog.ShowDialog())
                {
                    this.buildPath = this.saveFileDialog.FileName;
                    ThreadPool.QueueUserWorkItem(this.buildCallback);
                }
            }
        }

        /// <summary>
        /// Callback for building the package and feed.
        /// </summary>
        /// <param name="context">Context for invoke.</param>
        private void Build(object context)
        {
            try
            {
                this.Invoke(new MethodInvoker(this.PrepareBuildUI));

                BuildStepMessageHandler messageHandler = new BuildStepMessageHandler(this);
                this.fabricator.Message += messageHandler.Display;
                if (this.fabricator.Fabricate(this.buildPath))
                {
                    string message = String.Concat("Successfully created application feed: ", this.buildPath);
                    this.Invoke(this.buildProgressCallback, new object[] { message, -1, -1 });
                }
            }
            finally
            {
                this.Invoke(new MethodInvoker(this.EndBuildUI));
            }
        }

        /// <summary>
        /// Callback called on build progress.
        /// </summary>
        /// <param name="message">Message to show with the progress bar.</param>
        /// <param name="current">Current progress in the progress bar.</param>
        /// <param name="maximum">Maximum for the progress bar.</param>
        private void BuildProgress(string message, int current, int maximum)
        {
            this.progressLabel.Text = message;
            if (0 <= current)
            {
                this.progressBar.Value = current;
            }

            if (0 <= maximum)
            {
                this.progressBar.Maximum = maximum;
            }
        }

        /// <summary>
        /// Callback called at the beginning of the build.
        /// </summary>
        private void PrepareBuildUI()
        {
            this.buildButton.Enabled = false;
            this.progressBar.Value = 0;
            this.progressBar.Visible = true;
            this.progressLabel.Visible = true;
        }

        /// <summary>
        /// Callback called at the end of the build.
        /// </summary>
        private void EndBuildUI()
        {
            if (this.progressLabel.Text == null && this.progressLabel.Text == String.Empty)
            {
                this.progressLabel.Visible = false;
            }
            this.progressBar.Visible = false;
            this.progressBar.Value = 0;

            this.buildButton.Enabled = true;
        }

        /// <summary>
        /// Private message handler for building.
        /// </summary>
        private class BuildStepMessageHandler : MessageHandler
        {
            private BuildStep buildStep;

            /// <summary>
            /// Instantiate a new message handler.
            /// </summary>
            /// <param name="buildStep">BuildStep that should get the messages.</param>
            public BuildStepMessageHandler(BuildStep buildStep)
            {
                this.buildStep = buildStep;
            }

            /// <summary>
            /// Display a message to the build step.
            /// </summary>
            /// <param name="sender">Sender of the message.</param>
            /// <param name="mea">Arguments for the message event.</param>
            public void Display(object sender, MessageEventArgs mea)
            {
                MessageLevel messageLevel = this.CalculateMessageLevel(mea);
                if (MessageLevel.Nothing != messageLevel)
                {
                    string message = this.GenerateMessageString(messageLevel, mea);
                    this.buildStep.Invoke(this.buildStep.buildProgressCallback, new object[] { message, -1, -1 });
                }
            }
        }
    }
}
