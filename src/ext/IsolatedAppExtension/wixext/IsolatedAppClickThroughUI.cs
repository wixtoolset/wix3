//-------------------------------------------------------------------------------------------------
// <copyright file="IsolatedAppClickThroughUI.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Isolated application ClickThrough UI extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    using Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp;

    /// <summary>
    /// Creates a new isolated app ClickThrough UI extension.
    /// </summary>
    public sealed class IsolatedAppClickThroughUI : ClickThroughUIExtension
    {
        private IsolatedAppFabricator fabricator;
        private ClickThrough.BrowsePathStep step1;
        private ClickThrough.PickEntryStep step2;
        private ClickThrough.PackageInfoStep step3;
        private ClickThrough.FeedStep step4;
        private ClickThrough.UpdateInfoStep step5;
        private ClickThrough.BuildStep step6;
        private Control[] controls;

        /// <summary>
        /// Creates a new IsolatedAppClickThroughConsole.
        /// </summary>
        public IsolatedAppClickThroughUI()
        {
            this.fabricator = new IsolatedAppFabricator();
            this.fabricator.Changed += this.FabricatorProperty_Changed;
            this.fabricator.Opened += this.FabricatorProperty_Opened;
        }

        /// <summary>
        /// Gets the fabrictor for this extension.
        /// </summary>
        /// <value>Fabricator for IsolatedApp Add-in.</value>
        public override Fabricator Fabricator
        {
            get { return this.fabricator; }
        }

        /// <summary>
        /// Gets the UI Controls for this fabricator.
        /// </summary>
        /// <returns>Array of controls that make up the steps to feed the fabricator data.</returns>
        public override Control[] GetControls()
        {
            if (null == this.controls)
            {
                this.step1 = new ClickThrough.BrowsePathStep();
                this.step1.Fabricator = this.fabricator;
                this.step1.Changed += this.StepProperty_Changed;

                this.step2 = new ClickThrough.PickEntryStep();
                this.step2.Fabricator = this.fabricator;
                this.step2.Changed += this.StepProperty_Changed;

                this.step3 = new ClickThrough.PackageInfoStep();
                this.step3.Fabricator = this.fabricator;
                this.step3.Changed += this.StepProperty_Changed;

                this.step4 = new ClickThrough.FeedStep();
                this.step4.Fabricator = this.fabricator;
                this.step4.Changed += this.StepProperty_Changed;

                this.step5 = new ClickThrough.UpdateInfoStep();
                this.step5.Fabricator = this.fabricator;
                this.step5.Changed += this.StepProperty_Changed;

                this.step6 = new ClickThrough.BuildStep();
                this.step6.Fabricator = this.fabricator;

                this.controls = new Control[] { this.step1, this.step2, this.step3, this.step4, this.step5, this.step6 };
            }

            return this.controls;
        }

        /// <summary>
        /// Event handler for when the fabricator's property ("model" for this "controller") changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void FabricatorProperty_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (sender == this.fabricator)
            {
                switch (e.PropertyName)
                {
                    case "Source":
                        this.step1.Source = this.fabricator.Source;
                        this.step2.Source = this.fabricator.Source;
                        break;

                    case "EntryPoint":
                        this.step2.EntryPoint = this.fabricator.EntryPoint;

                        this.step3.Description = this.fabricator.Description;
                        this.step3.Manufacturer = this.fabricator.Manufacturer;
                        this.step3.ApplicationName = this.fabricator.Name;
                        this.step3.Version = this.fabricator.ApplicationVersion;
                        break;
                    case "Description":
                        this.step3.Description = this.fabricator.Description;
                        break;
                    case "Manufacturer":
                        this.step3.Manufacturer = this.fabricator.Manufacturer;
                        break;
                    case "Name":
                        this.step3.ApplicationName = this.fabricator.Name;
                        break;
                    case "ApplicationVersion":
                        this.step3.Version = this.fabricator.ApplicationVersion;
                        break;
                    case "UpdateUrl":
                        this.step4.UpdateUri = this.fabricator.UpdateUrl.AbsoluteUri;
                        this.step5.UpdateUri = this.fabricator.UpdateUrl.AbsoluteUri;
                        this.step6.UpdateUri = this.fabricator.UpdateUrl;
                        break;
                    case "UpdateRate":
                        this.step4.UpdateRate = this.fabricator.UpdateRate;
                        break;
                }
            }
        }

        /// <summary>
        /// Event handler for when the fabricator ("model" for this "controller") is opened.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void FabricatorProperty_Opened(object sender, EventArgs e)
        {
            if (sender == this.fabricator)
            {
                this.step1.Source = this.fabricator.Source;

                this.step2.EntryPoint = this.fabricator.EntryPoint;
                this.step2.Source = this.fabricator.Source;

                this.step3.Description = this.fabricator.Description;
                this.step3.Manufacturer = this.fabricator.Manufacturer;
                this.step3.ApplicationName = this.fabricator.Name;
                this.step3.Version = this.fabricator.ApplicationVersion;

                this.step4.UpdateUri = this.fabricator.UpdateUrl.AbsoluteUri;
                this.step4.UpdateRate = this.fabricator.UpdateRate;

                this.step5.UpdateUri = this.fabricator.UpdateUrl.AbsoluteUri;
                this.step5.PreviousFeedUri = this.fabricator.PreviousFeedUrl;

                this.step6.UpdateUri = this.fabricator.UpdateUrl;
            }
        }

        /// <summary>
        /// Event handler for when any step's property ("view" for this "controller") changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void StepProperty_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (this.step1 == sender)
            {
                this.fabricator.Source = step1.Source;
            }
            else if (this.step2 == sender)
            {
                this.fabricator.EntryPoint = this.step2.EntryPoint;
            }
            else if (this.step3 == sender)
            {
                switch (e.PropertyName)
                {
                    case "Description":
                        this.fabricator.Description = this.step3.Description;
                        break;
                    case "Manufacturer":
                        this.fabricator.Manufacturer = this.step3.Manufacturer;
                        break;
                    case "ApplicationName":
                        this.fabricator.Name = this.step3.ApplicationName;
                        break;
                    case "Version":
                        this.fabricator.ApplicationVersion = this.step3.Version;
                        break;
                }
            }
            else if (this.step4 == sender)
            {
                switch (e.PropertyName)
                {
                    case "UpdateUri":
                        if (null != this.step4.UpdateUri)
                        {
                            this.fabricator.UpdateUrl = new Uri(this.step4.UpdateUri);
                        }
                        break;
                    case "UpdateRate":
                        this.fabricator.UpdateRate = this.step4.UpdateRate;
                        break;
                }
            }
            else if (this.step5 == sender)
            {
                switch (e.PropertyName)
                {
                    case "PreviousFeedUri":
                        this.fabricator.PreviousFeedUrl = this.step5.PreviousFeedUri;
                        break;
                }
            }
        }
    }
}
