// <copyright file="UpdateInfoStep.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Step to provide update information for application.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    /// <summary>
    /// Step to provide update information for application.
    /// </summary>
    public sealed partial class UpdateInfoStep : UserControl
    {
        private Fabricator fabricator;
        private string updateUri;
        private Uri previousFeedUri;
        ////private bool externalChange;
        ////private bool updateChanged;
        ////private bool previousPathChanged;

        /// <summary>
        /// Create new UpdateInfoStep.
        /// </summary>
        public UpdateInfoStep()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Event fired any time a change is made to the step's properties.
        /// </summary>
        public event PropertyChangedEventHandler Changed;

        /// <summary>
        /// Gets and sets the fabricator for this step.
        /// </summary>
        /// <value>Fabricator.</value>
        public Fabricator Fabricator
        {
            get { return this.fabricator; }
            set { this.fabricator = value; }
        }

        /// <summary>
        /// Gets and sets the update uri for this step.
        /// </summary>
        /// <value>String update uri.</value>
        public string UpdateUri
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
                        if (this.feedUpgradeRadioButton.Checked)
                        {
                            this.noUpgradeRadioButton.Checked = true;
                        }
                    }
                    else
                    {
                        if (this.feedUpgradeRadioButton.Checked)
                        {
                            this.previousPathTextBox.Text = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the previous feed uri for this step.
        /// </summary>
        /// <value>String previous feed uri.</value>
        public Uri PreviousFeedUri
        {
            get
            {
                return this.previousFeedUri;
            }

            set
            {
                if (this.previousFeedUri != value)
                {
                    if (value != null)
                    {
                        this.previousPathTextBox.Text = value.AbsoluteUri;
                    }
                    else
                    {
                        this.previousPathTextBox.Text = null;
                    }
                }
            }
        }
        /// <summary>
        /// Event handler for when any radio button is selected/deselected.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void NoUpgradeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (this.noUpgradeRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = false;
                this.browseButton.Enabled = false;

                this.SetPreviousFeedUri();
            }
        }

        /// <summary>
        /// Event handler for when any radio button is selected/deselected.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void FeedUpgradeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (this.feedUpgradeRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = false;
                this.browseButton.Enabled = false;

                this.SetPreviousFeedUri();
            }
        }

        /// <summary>
        /// Event handler for when any radio button is selected/deselected.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void UpgradePathRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (this.upgradePathRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = true;
                this.browseButton.Enabled = true;

                this.SetPreviousFeedUri();

                this.previousPathTextBox.Focus();
            }
        }

        /// <summary>
        /// Event handler for when any radio button is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void AnyRadioButton_Validated(object sender, EventArgs e)
        {
            if (this.noUpgradeRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = false;
                this.browseButton.Enabled = false;
            }
            else if (this.feedUpgradeRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = false;
                this.browseButton.Enabled = false;
            }
            else if (this.upgradePathRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = true;
                this.browseButton.Enabled = true;
            }

            this.SetPreviousFeedUri();
        }

        /// <summary>
        /// Event handler for when the update text box is validating
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void PreviousPathTextBox_Validating(object sender, CancelEventArgs e)
        {
            if (this.previousPathTextBox.Text != null && this.previousPathTextBox.Text != String.Empty)
            {
                try
                {
                    Uri uri = new Uri(this.previousPathTextBox.Text);
                }
                catch (UriFormatException)
                {
                    e.Cancel = true;
                }
                finally
                {
                    if (e.Cancel)
                    {
                        MessageBox.Show("Invalid Update URL.", this.fabricator.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for when the update text box is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void PreviousPathTextBox_Validated(object sender, EventArgs e)
        {
            if (this.previousPathTextBox.Text == null || this.previousPathTextBox.Text == String.Empty)
            {
                this.previousFeedUri = null;
            }
            else
            {
                this.previousFeedUri = new Uri(this.previousPathTextBox.Text);
            }

            if (this.Changed != null)
            {
                this.Changed(this, new PropertyChangedEventArgs("PreviousFeedUri"));
            }
        }

        /// <summary>
        /// Event handler for when the browse button is clicked.
        /// </summary>
        /// <param name="sender">Control that sent the click request.</param>
        /// <param name="e">Event arguments for click.</param>
        private void BrowseButton_Click(object sender, EventArgs e)
        {
            DialogResult result = this.openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                Uri uri = new Uri(this.openFileDialog.FileName);
                this.previousPathTextBox.Text = uri.AbsoluteUri;
            }
        }

        /// <summary>
        /// Sets the internal previous feed uri value from the UI.
        /// </summary>
        private void SetPreviousFeedUri()
        {
            try
            {
                this.previousFeedUri = new Uri(this.previousPathTextBox.Text);
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("PreviousFeedUri"));
                }
            }
            catch (ArgumentNullException)
            {
                this.previousFeedUri = null;
                this.previousPathTextBox.Text = null;
            }
            catch (UriFormatException)
            {
                this.previousFeedUri = null;
                this.previousPathTextBox.Text = null;
            }
        }
    }
}
