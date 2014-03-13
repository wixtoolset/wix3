//-------------------------------------------------------------------------------------------------
// <copyright file="PackageInfoStep.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Step to get package information for application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    /// <summary>
    /// Step to get package information for application.
    /// </summary>
    public sealed partial class PackageInfoStep : UserControl
    {
        private Fabricator fabricator;
        private bool externalChange;
        private bool descriptionChanged;
        private bool manufacturerChanged;
        private bool nameChanged;
        private bool versionChanged;
        private Version version;

        /// <summary>
        /// Creates a package info setup.
        /// </summary>
        public PackageInfoStep()
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
        /// Gets and sets the description in this step.
        /// </summary>
        /// <value>String description.</value>
        public string Description
        {
            get
            {
                if (this.descriptionChanged)
                {
                    return this.descriptionTextBox.Text;
                }

                return null;
            }

            set
            {
                this.descriptionChanged = false;

                if (this.descriptionTextBox.Text != value)
                {
                    try
                    {
                        this.externalChange = true;
                        this.descriptionTextBox.Text = value;
                    }
                    finally
                    {
                        this.externalChange = false;
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the manufacturer in this step.
        /// </summary>
        /// <value>String manufacturer.</value>
        public string Manufacturer
        {
            get
            {
                if (this.manufacturerChanged)
                {
                    return this.manufacturerTextBox.Text;
                }

                return null;
            }

            set
            {
                this.manufacturerChanged = false;

                if (this.manufacturerTextBox.Text != value)
                {
                    try
                    {
                        this.externalChange = true;
                        this.manufacturerTextBox.Text = value;
                    }
                    finally
                    {
                        this.externalChange = false;
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the name in this step.
        /// </summary>
        /// <value>String application name</value>
        public string ApplicationName
        {
            get
            {
                if (this.nameChanged)
                {
                    return this.nameTextBox.Text;
                }

                return null;
            }

            set
            {
                this.nameChanged = false;

                if (this.nameTextBox.Text != value)
                {
                    try
                    {
                        this.externalChange = true;
                        this.nameTextBox.Text = value;
                    }
                    finally
                    {
                        this.externalChange = false;
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the version in this step.
        /// </summary>
        /// <value>Application version.</value>
        public Version Version
        {
            get
            {
                if (this.versionChanged)
                {
                    return this.version;
                }

                return null;
            }

            set
            {
                this.versionChanged = false;

                if (this.version != value)
                {
                    try
                    {
                        this.externalChange = true;

                        this.version = value;
                        this.versionTextBox.Text = this.version.ToString();
                    }
                    finally
                    {
                        this.externalChange = false;
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for when the description text box is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void DescriptionTextBox_Validated(object sender, EventArgs e)
        {
            if (this.descriptionChanged)
            {
                if (this.descriptionTextBox.Text == null || this.descriptionTextBox.Text == String.Empty)
                {
                    this.descriptionChanged = false;
                }

                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("Description"));
                }
            }
        }

        /// <summary>
        /// Event handler for when the manufacturer text box is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ManufacturerTextBox_Validated(object sender, EventArgs e)
        {
            if (this.manufacturerChanged)
            {
                if (this.manufacturerTextBox.Text == null || this.manufacturerTextBox.Text == String.Empty)
                {
                    this.manufacturerChanged = false;
                }

                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("Manufacturer"));
                }
            }
        }

        /// <summary>
        /// Event handler for when the name text box is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void NameTextBox_Validated(object sender, EventArgs e)
        {
            if (this.nameChanged)
            {
                if (this.nameTextBox.Text == null || this.nameTextBox.Text == String.Empty)
                {
                    this.nameChanged = false;
                }

                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("ApplicationName"));
                }
            }
        }

        /// <summary>
        /// Event handler for when the version text box is being validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void VersionTextBox_Validating(object sender, CancelEventArgs e)
        {
            if (this.versionChanged)
            {
                Version version;
                try
                {
                    if (this.versionTextBox.Text != null && this.versionTextBox.Text != String.Empty)
                    {
                        version = new Version(this.versionTextBox.Text);
                    }
                }
                catch (ArgumentException)
                {
                    e.Cancel = true;
                }
                catch (FormatException)
                {
                    e.Cancel = true;
                }
                catch (OverflowException)
                {
                    e.Cancel = true;
                }
                finally
                {
                    if (e.Cancel)
                    {
                        MessageBox.Show("Invalid version.  The version must be of the format of '#.#' or '#.#.#' or '#.#.#.#'.", this.fabricator.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for when the version text box is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void VersionTextBox_Validated(object sender, EventArgs e)
        {
            if (this.versionChanged)
            {
                if (this.versionTextBox.Text == null || this.versionTextBox.Text == String.Empty)
                {
                    this.versionChanged = false;
                    this.version = null;
                }
                else
                {
                    this.version = new Version(this.versionTextBox.Text);
                }

                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("Version"));
                }
            }
        }

        /// <summary>
        /// Event handler for when any text box is modified.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!this.externalChange)
            {
                if (sender == this.descriptionTextBox)
                {
                    this.descriptionChanged = true;
                }
                else if (sender == this.nameTextBox)
                {
                    this.nameChanged = true;
                }
                else if (sender == this.manufacturerTextBox)
                {
                    this.manufacturerChanged = true;
                }
                else if (sender == this.versionTextBox)
                {
                    this.versionChanged = true;
                }
            }
        }
    }
}
