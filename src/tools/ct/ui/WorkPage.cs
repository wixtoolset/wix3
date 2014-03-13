//-------------------------------------------------------------------------------------------------
// <copyright file="WorkPage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Page where steps are displayed.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Page where all of the extension step panels are displayed.
    /// </summary>
    internal partial class WorkPage : UserControl
    {
        private ClickThroughUIExtension extension;
        private StepPanel[] stepPanels;

        /// <summary>
        /// Creates a new workpage.
        /// </summary>
        public WorkPage()
        {
            this.extension = null;
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets and sets the extension displayed in this work page.
        /// </summary>
        public ClickThroughUIExtension Extension
        {
            get
            {
                return this.extension;
            }
            set
            {
                this.extension = value;
                if (this.extension != null)
                {
                    this.InitializeFromFabricator();
                }
            }
        }

        /// <summary>
        /// Called to re-layout the work page when an extension is loaded.
        /// </summary>
        private void InitializeFromFabricator()
        {
            this.SuspendLayout();
            this.flowLayoutPanel.SuspendLayout();

            this.flowLayoutPanel.Controls.Clear();

            if (this.extension != null)
            {
                this.wixBanner.Text = this.extension.Fabricator.Title;

                Control[] controls = this.extension.GetControls();
                if (null != controls)
                {
                    this.stepPanels = new StepPanel[controls.Length];

                    for (int i = 0; i < controls.Length; ++i)
                    {
                        this.stepPanels[i] = new StepPanel();
                        this.stepPanels[i].Step = i + 1;
                        this.stepPanels[i].ContainedControl = controls[i];

                        this.stepPanels[i].InitializeComponent();
                    }

                        this.flowLayoutPanel.Controls.AddRange(this.stepPanels);
                }
            }

            this.flowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(true);
        }

        /// <summary>
        /// Event handler for when the back link is clicked to go back to Welcome page.
        /// </summary>
        /// <param name="sender">Control that sent the click request.</param>
        /// <param name="e">Event arguments for click.</param>
        private void BackLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ClickThroughForm form = (ClickThroughForm)this.ParentForm;
            form.ShowWelcomePage(sender);
        }

        /// <summary>
        /// Event handler for when the back link is clicked to save open document.
        /// </summary>
        /// <param name="sender">Control that sent the click request.</param>
        /// <param name="e">Event arguments for click.</param>
        private void SaveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (DialogResult.OK == this.saveFileDialog.ShowDialog())
            {
                this.extension.Fabricator.Save(this.saveFileDialog.FileName);
            }
        }
    }
}
