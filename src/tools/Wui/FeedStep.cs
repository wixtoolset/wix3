// <copyright file="FeedStep.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Feed step.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    /// <summary>
    /// Feed step.
    /// </summary>
    public sealed partial class FeedStep : UserControl
    {
        private readonly int[] rates = { 10080, 4320, 1440, 720, 360, 60, 1 };
        private Fabricator fabricator;
        private string updateUri;
        private int updateRate;
        ////private bool externalChange;
        ////private bool updateChanged;
        ////private bool previousPathChanged;

        /// <summary>
        /// Creates a new FeedStep.
        /// </summary>
        public FeedStep()
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
                    this.updateTextBox.Text = value;
                }
            }
        }

        /// <summary>
        /// Gets and sets the update rate in minutes for this step.
        /// </summary>
        /// <value>Update rate (in minutes).</value>
        public int UpdateRate
        {
            get
            {
                return this.updateRate;
            }

            set
            {
                if (this.updateRate != value)
                {
                    this.updateRate = value;
                    if (0 == this.updateRate)
                    {
                        this.updateRateComboBox.SelectedIndex = -1;
                    }
                    else
                    {
                        bool found = false;
                        for (int i = 0; i < this.rates.Length; ++i)
                        {
                            if (this.rates[i] == this.updateRate)
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
                this.updateUri = null;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("UpdateUri"));
                }
            }
            else
            {
                this.updateUri = this.updateTextBox.Text;
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("UpdateUri"));
                }

                if (this.updateRate == 0)
                {
                    this.updateRateComboBox.SelectedIndex = 0;
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
                this.updateRate = 0;
            }
            else
            {
                this.updateRate = this.rates[this.updateRateComboBox.SelectedIndex];
            }

            if (this.Changed != null)
            {
                this.Changed(this, new PropertyChangedEventArgs("UpdateRate"));
            }
        }
    }
}
