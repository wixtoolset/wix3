//-------------------------------------------------------------------------------------------------
// <copyright file="FeedStep.cs" company="Outercurve Foundation">
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
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    /// <summary>
    /// Step to provide a feed for the application.
    /// </summary>
    internal partial class FeedStep : UserControl
    {
        private readonly int[] rates = { 10080, 4320, 1440, 720, 360, 60, 1 };
        private OfficeAddinFabricator fabricator;

        /// <summary>
        /// Creates a FeedStep.
        /// </summary>
        public FeedStep()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets and sets the fabricator for this step.
        /// </summary>
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
        /// Event handler for when the update text box is validating
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void UpdateTextBox_Validating(object sender, CancelEventArgs e)
        {
            if (this.updateTextBox.Text != null && this.updateTextBox.Text != String.Empty)
            {
                try
                {
                    Uri uri = new Uri(this.updateTextBox.Text);
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
        private void UpdateTextBox_Validated(object sender, EventArgs e)
        {
            if (this.updateTextBox.Text == null || this.updateTextBox.Text == String.Empty)
            {
                this.fabricator.UpdateUrl = null;
            }
            else
            {
                this.fabricator.UpdateUrl = new Uri(this.updateTextBox.Text);

                if (this.fabricator.UpdateRate == 0)
                {
                    this.fabricator.UpdateRate = this.rates[0];
                }
            }
        }

        /// <summary>
        /// Event handler for when the update rate combo box is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void UpdateRateComboBox_Validated(object sender, EventArgs e)
        {
            if (this.updateRateComboBox.SelectedIndex == -1)
            {
                this.fabricator.UpdateRate = 0;
            }
            else
            {
                this.fabricator.UpdateRate = this.rates[this.updateRateComboBox.SelectedIndex];
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
                    if (this.fabricator.UpdateUrl == null)
                    {
                        this.updateTextBox.Text = null;
                    }
                    else
                    {
                        this.updateTextBox.Text = this.fabricator.UpdateUrl.AbsoluteUri;
                    }

                    if (this.fabricator.UpdateRate == 0)
                    {
                        this.updateRateComboBox.SelectedIndex = -1;
                    }
                    else
                    {
                        bool found = false;
                        for (int i = 0; i < this.rates.Length; ++i)
                        {
                            if (this.rates[i] == this.fabricator.UpdateRate)
                            {
                                this.updateRateComboBox.SelectedIndex = i;
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            this.updateRateComboBox.SelectedIndex = -1;
                        }
                    }
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
                                this.updateTextBox.Text = null;
                            }
                            else
                            {
                                this.updateTextBox.Text = this.fabricator.UpdateUrl.AbsoluteUri;
                            }

                            break;
                        case "UpdateRate":
                            if (0 == this.fabricator.UpdateRate)
                            {
                                this.updateRateComboBox.SelectedIndex = -1;
                            }
                            else
                            {
                                bool found = false;
                                for (int i = 0; i < this.rates.Length; ++i)
                                {
                                    if (this.rates[i] == this.fabricator.UpdateRate)
                                    {
                                        this.updateRateComboBox.SelectedIndex = i;
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    this.updateRateComboBox.SelectedIndex = -1;
                                }
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
