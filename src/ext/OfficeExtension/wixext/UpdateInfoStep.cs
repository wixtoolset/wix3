//-------------------------------------------------------------------------------------------------
// <copyright file="UpdateInfoStep.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//  Fifth step in the office addin UI for MSI builder for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    /// <summary>
    /// Provides update information for the Office Addin.
    /// </summary>
    internal partial class UpdateInfoStep : UserControl
    {
        private OfficeAddinFabricator fabricator;
        // private bool externalChange;
        // private bool updateChanged;
        // private bool previousPathChanged;

        /// <summary>
        /// Creates a new UpdateInfoStep.
        /// </summary>
        public UpdateInfoStep()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets and sets the fabricator for this step.
        /// </summary>
        /// <value>Fabricator.</value>
        public OfficeAddinFabricator Fabricator
        {
            get
            {
                return this.fabricator;
            }

            set
            {
                if (this.fabricator != null)
                {
                    this.fabricator.Changed -= this.FabricatorProperty_Changed;
                    this.fabricator.Opened -= this.FabricatorOpened_Changed;
                }

                this.fabricator = value;
                this.fabricator.Changed += this.FabricatorProperty_Changed;
                this.fabricator.Opened += this.FabricatorOpened_Changed;
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

                this.fabricator.PreviousFeedUrl = null;
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

                this.fabricator.PreviousFeedUrl = this.fabricator.UpdateUrl;
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

                try
                {
                    this.fabricator.PreviousFeedUrl = new Uri(this.previousPathTextBox.Text);
                }
                catch (ArgumentNullException)
                {
                    this.fabricator.PreviousFeedUrl = null;
                    this.previousPathTextBox.Text = null;
                }
                catch (UriFormatException)
                {
                    this.fabricator.PreviousFeedUrl = null;
                    this.previousPathTextBox.Text = null;
                }

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

                this.fabricator.PreviousFeedUrl = null;
            }
            else if (this.feedUpgradeRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = false;
                this.browseButton.Enabled = false;

                this.fabricator.PreviousFeedUrl = this.fabricator.UpdateUrl;
            }
            else if (this.upgradePathRadioButton.Checked)
            {
                this.previousPathTextBox.Enabled = true;
                this.browseButton.Enabled = true;

                try
                {
                    this.fabricator.PreviousFeedUrl = new Uri(this.previousPathTextBox.Text);
                }
                catch (ArgumentNullException)
                {
                    this.fabricator.PreviousFeedUrl = null;
                    this.previousPathTextBox.Text = null;
                }
                catch (UriFormatException)
                {
                    this.fabricator.PreviousFeedUrl = null;
                    this.previousPathTextBox.Text = null;
                }
            }
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
                this.fabricator.PreviousFeedUrl = null;
            }
            else
            {
                this.fabricator.PreviousFeedUrl = new Uri(this.previousPathTextBox.Text);
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
                this.fabricator.PreviousFeedUrl = new Uri(this.openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Event handler for when the fabricator is re-opened ("model" for the "view") changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void FabricatorOpened_Changed(object sender, EventArgs e)
        {
            if (sender == this.fabricator)
            {
                try
                {
                    // this.externalChange = true;
                }
                finally
                {
                    // this.externalChange = false;
                }
            }
        }

        /// <summary>
        /// Event handler for when the fabricator's property ("model" for the "view") changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void FabricatorProperty_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (sender == this.fabricator)
            {
                try
                {
                    // this.externalChange = true;
                    switch (e.PropertyName)
                    {
                        case "UpdateUrl":
                            if (this.fabricator.UpdateUrl == null)
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
                                    this.fabricator.PreviousFeedUrl = this.fabricator.UpdateUrl;
                                }
                            }
                            break;
                        case "PreviousFeedUrl":
                            if (this.fabricator.PreviousFeedUrl == null)
                            {
                                this.previousPathTextBox.Text = null;
                            }
                            else
                            {
                                this.previousPathTextBox.Text = this.fabricator.PreviousFeedUrl.AbsoluteUri;
                            }
                            break;
                    }
                }
                finally
                {
                    // this.externalChange = false;
                }
            }
        }
    }
}
