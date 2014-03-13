//-------------------------------------------------------------------------------------------------
// <copyright file="PackageInfoStep.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//  Package info for the Office Addin UI for MSI builder for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    /// <summary>
    /// Step to get package information about Office Addin.
    /// </summary>
    internal partial class PackageInfoStep : UserControl
    {
        private OfficeAddinFabricator fabricator;
        private bool externalChange;
        private bool descriptionChanged;
        private bool manufacturerChanged;
        private bool nameChanged;
        private bool versionChanged;

        /// <summary>
        /// Creates a package info setup.
        /// </summary>
        public PackageInfoStep()
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
                this.fabricator.Description = this.descriptionTextBox.Text;
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
                this.fabricator.Manufacturer = this.manufacturerTextBox.Text;
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
                this.fabricator.Name = this.nameTextBox.Text;
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
                Version version;
                if (this.versionTextBox.Text == null || this.versionTextBox.Text == String.Empty)
                {
                    version = null;
                    this.versionChanged = false;
                }
                else
                {
                    version = new Version(this.versionTextBox.Text);
                }
                this.fabricator.ApplicationVersion = version;
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
                    this.externalChange = true;
                    Version version = this.fabricator.ApplicationVersion;
                    if (version != null)
                    {
                        this.versionTextBox.Text = version.ToString();
                    }
                    else
                    {
                        this.versionTextBox.Text = null;
                    }
                    this.descriptionTextBox.Text = this.fabricator.Description;
                    this.manufacturerTextBox.Text = this.fabricator.Manufacturer;
                    this.nameTextBox.Text = this.fabricator.Name;
                }
                finally
                {
                    this.externalChange = false;
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
                    this.externalChange = true;
                    Version version;
                    switch (e.PropertyName)
                    {
                        case "ApplicationVersion":
                            version = this.fabricator.ApplicationVersion;
                            if (version != null)
                            {
                                this.versionTextBox.Text = version.ToString();
                            }
                            else
                            {
                                this.versionTextBox.Text = null;
                            }
                            break;
                        case "Description":
                            this.descriptionTextBox.Text = this.fabricator.Description;
                            break;
                        case "EntryPoint":
                            version = this.fabricator.ApplicationVersion;
                            if (version != null)
                            {
                                this.versionTextBox.Text = version.ToString();
                            }
                            else
                            {
                                this.versionTextBox.Text = null;
                            }
                            this.descriptionTextBox.Text = this.fabricator.Description;
                            this.manufacturerTextBox.Text = this.fabricator.Manufacturer;
                            this.nameTextBox.Text = this.fabricator.Name;
                            break;
                        case "Manufacturer":
                            this.manufacturerTextBox.Text = this.fabricator.Manufacturer;
                            break;
                        case "Name":
                            this.nameTextBox.Text = this.fabricator.Name;
                            break;
                    }
                }
                finally
                {
                    this.externalChange = false;
                }
            }
        }
    }
}
