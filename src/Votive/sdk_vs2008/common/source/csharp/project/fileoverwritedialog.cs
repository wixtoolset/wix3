//-------------------------------------------------------------------------------------------------
// <copyright file="fileoverwritedialog.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------


namespace Microsoft.VisualStudio.Package
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Text;
    using System.Windows.Forms;
    using System.IO;
    using Microsoft.VisualStudio.Shell.Interop;
    using System.Globalization;
    using Microsoft.VisualStudio.Shell;
    using System.Windows.Forms.Design;
    using System.Diagnostics;
    using System.ComponentModel.Design;
    using Microsoft.Win32;

    /// <summary>
    /// Defines a genric don't show again dilaog.
    /// </summary>
    internal partial class FileOverwriteDialog : Form
    {
        #region fields
        /// <summary>
        /// Defines the bitmap to be drawn
        /// </summary>
        private Bitmap bitmap;

        /// <summary>
        /// The associated service provider
        /// </summary>
        private IServiceProvider serviceProvider;

        /// <summary>
        /// The help topic associated.
        /// </summary>
        private string helpTopic;

        /// <summary>
        /// The value of the don't show again check box
        /// </summary>
        private bool applyToAllValue;
        #endregion

        #region constructors
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="serviceProvider">The associated service provider.</param>
        /// <param name="messageText">Thetext to be shown on the dialog</param>
        /// <param name="title">The title of the dialog</param>
        /// <param name="helpTopic">The associated help topic</param>
        /// <param name="button">The default button</param>
        internal FileOverwriteDialog(IServiceProvider serviceProvider, string messageText, string title, string helpTopic, DefaultButton button)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            this.serviceProvider = serviceProvider;
            this.InitializeComponent();

            if (button == DefaultButton.Yes)
            {
                this.AcceptButton = this.yesButton;
            }
            else
            {
                this.AcceptButton = this.cancelButton;
            }

            this.Text = title;
            this.SetupComponents(messageText, helpTopic);
        }
        #endregion

        #region properties
        /// <summary>
        /// The value of the dont' show again checkbox before the dialog is closed.
        /// </summary>
        internal bool ApplyToAllValue
        {
            get
            {
                return this.applyToAllValue;
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// Shows help for the help topic.
        /// </summary>
        protected virtual void ShowHelp()
        {
            Microsoft.VisualStudio.VSHelp.Help help = this.serviceProvider.GetService(typeof(Microsoft.VisualStudio.VSHelp.Help)) as Microsoft.VisualStudio.VSHelp.Help;

            if (help != null)
            {
                help.DisplayTopicFromF1Keyword(this.helpTopic);
            }
        }

        /// <summary>
        /// Launches a FileOverwriteDialog.
        /// </summary>
        /// <param name="serviceProvider">An associated serviceprovider.</param>
        /// <param name="messageText">The text the dialog box will contain.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="helpTopic">The associated help topic.</param>
        /// <param name="button">The default button.</param>
        /// <param name="applyAllValue">The value of the apply all checkbox.</param>
        /// <returns>A Dialog result.</returns>
        internal static DialogResult LaunchFileOverwriteDialog(IServiceProvider serviceProvider, string messageText, string title, string helpTopic, DefaultButton button, out bool applyAllValue)
        {
            DialogResult result = DialogResult.Yes;

            FileOverwriteDialog dialog = new FileOverwriteDialog(serviceProvider, messageText, title, helpTopic, button);
            result = dialog.ShowDialog();
            applyAllValue = dialog.ApplyToAllValue;

            return result;
        }

        /// <summary>
        /// Shows the dialog if possible hosted by the IUIService.
        /// </summary>
        /// <returns>A DialogResult</returns>
        internal new DialogResult ShowDialog()
        {
            Debug.Assert(this.serviceProvider != null, "The service provider should not be null at this time");
            IUIService uiService = this.serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uiService == null)
            {
                return this.ShowDialog();
            }

            return uiService.ShowDialog(this);
        }

        /// <summary>
        /// Defines the event delegate when help is requested.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="hlpevent"></param>
        private void OnHelpRequested(object sender, HelpEventArgs hlpevent)
        {
            if (String.IsNullOrEmpty(this.helpTopic))
            {
                return;
            }

            this.ShowHelp();
            hlpevent.Handled = true;
        }

        /// <summary>
        /// Defines the delegate that responds to the help button clicked event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">An instance of canceleventargs </param>
        private void OnHelpButtonClicked(object sender, CancelEventArgs e)
        {
            if (String.IsNullOrEmpty(this.helpTopic))
            {
                return;
            }

            e.Cancel = true;
            this.ShowHelp();
        }

        /// <summary>
        /// Called when the dialog box is repainted.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The associated paint event args.</param>
        private void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(this.bitmap, new Point(7, this.messageText.Location.Y));
        }


        /// <summary>
        /// Sets up the components that are not done through teh Initialize components.
        /// </summary>
        /// <param name="helpTopicParam">The associated help topic</param>
        /// <param name="messageTextParam">The message to show on the dilaog.</param>
        private void SetupComponents(string messageTextParam, string helpTopicParam)
        {  
            // Compute the Distance to the bottom of the dialog 
            int distanceToBottom = this.Size.Height - this.cancelButton.Location.Y;

            // The height of the messageText before it assigned its value.
            int oldHeight = this.messageText.Size.Height;

            // Set the maximum size as the CancelButtonEndX - MessageTextStartX. This way it wil never pass by the button.
            this.messageText.MaximumSize = new Size(this.cancelButton.Location.X + this.cancelButton.Size.Width - this.messageText.Location.X, 0);
            this.messageText.Text = messageTextParam;

            // How much it has changed?
            int deltaY = this.messageText.Size.Height - oldHeight;
            this.AdjustSizesVertically(deltaY, distanceToBottom);

            if (String.IsNullOrEmpty(helpTopicParam))
            {
                this.HelpButton = false;
            }
            else
            {
                this.helpTopic = helpTopicParam;
            }

            // Create the system icon that will be drawn on the dialog page.
            Icon icon = new Icon(SystemIcons.Exclamation, 40, 40);

            // Call ToBitmap to convert it.
            this.bitmap = icon.ToBitmap();

            this.CenterToScreen();
        }

        /// <summary>
        /// Handles the cancel button clicked event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args associated to the event.</param>
        private void OnCancelButtonClicked(object sender, EventArgs e)
        {
            this.applyToAllValue = this.applyToAll.Checked;
            this.DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Handles the no button clicked event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args associated to the event.</param>
        private void OnNoButtonClicked(object sender, EventArgs e)
        {
            this.applyToAllValue = this.applyToAll.Checked;
            this.DialogResult = DialogResult.No;
            this.Close();
        }

        /// <summary>
        /// Handles the yes button clicked event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args associated to the event.</param>
        private void OnYesButtonClicked(object sender, EventArgs e)
        {
            this.applyToAllValue = this.applyToAll.Checked;
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

        /// <summary>
        ///  Moves controls vertically because of a vertical change in the messagetext.
        /// </summary>
        private void AdjustSizesVertically(int deltaY, int distanceToBottom)
        {
            // Move the checkbox to its new location determined by the height the label.
            this.applyToAll.Location = new Point(this.applyToAll.Location.X, this.applyToAll.Location.Y + deltaY);

            // Move the buttons to their new location; The X coordinate is fixed.
            int newSizeY = this.cancelButton.Location.Y + deltaY;
            this.cancelButton.Location = new Point(this.cancelButton.Location.X, newSizeY);

            newSizeY = this.yesButton.Location.Y + deltaY;
            this.yesButton.Location = new Point(this.yesButton.Location.X, newSizeY);

            newSizeY = this.noButton.Location.Y + deltaY;
            this.noButton.Location = new Point(this.noButton.Location.X, newSizeY);

            // Now resize the dialog itself.
            this.Size = new Size(this.Size.Width, this.cancelButton.Location.Y + distanceToBottom);
        }
        #endregion

        #region nested types
        /// <summary>
        /// Defines which button to serve as the default button.
        /// </summary>
        internal enum DefaultButton
        {
            Yes,
            No,
            Cancel
        }
        #endregion
    }
}