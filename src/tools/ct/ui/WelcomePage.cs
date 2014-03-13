//-------------------------------------------------------------------------------------------------
// <copyright file="WelcomePage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//   Wlecome page for ClickThrough UI.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// ClickThrough UI welcome page.
    /// </summary>
    internal partial class WelcomePage : UserControl
    {
        /// <summary>
        /// Creates a new welcome page.
        /// </summary>
        public WelcomePage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the welcome page with the list of extensions.
        /// </summary>
        /// <param name="extensions">Array of fabricators to display in welcome page.</param>
        internal void AddExtensions(ClickThroughUIExtension[] extensions)
        {
            this.SuspendLayout();
            this.extensionComboBox.SuspendLayout();

            this.extensionComboBox.Items.Clear();
            for (int i = 0; i < extensions.Length; ++i)
            {
                Fabricator f = extensions[i].Fabricator;
                this.extensionComboBox.Items.Add(f.Title);
            }

            this.extensionComboBox.Tag = extensions;

            this.extensionComboBox.ResumeLayout(false);
            this.ResumeLayout(true);
        }

        /// <summary>
        /// Method called when open link is clicked.
        /// </summary>
        /// <param name="sender">Control that sent the message.</param>
        /// <param name="e">Parameters accompanying message.</param>
        private void OpenLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ClickThroughForm form = (ClickThroughForm)this.ParentForm;
            form.Open(sender);
        }

        /// <summary>
        /// Method called when fabricator combo box selection changes.
        /// </summary>
        /// <param name="sender">Control that sent the message.</param>
        /// <param name="e">Parameters accompanying message.</param>
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClickThroughForm form = (ClickThroughForm)this.ParentForm;
            ClickThroughUIExtension[] extensions = (ClickThroughUIExtension[])this.extensionComboBox.Tag;

            int selected = this.extensionComboBox.SelectedIndex;
            form.ShowWorkPage(sender, extensions[selected]);
        }
    }
}
