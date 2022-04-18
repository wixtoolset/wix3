// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Specialized text box for editing build event command lines.
    /// </summary>
    internal class WixBuildEventTextBox : TextBox
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBuildEventTextBox"/> class.
        /// </summary>
        public WixBuildEventTextBox()
        {
            this.Multiline = true;
            this.ScrollBars = ScrollBars.Both;
            this.WordWrap = false;
            this.AcceptsReturn = true;
            this.AcceptsTab = false;

            // set our initial size
            this.Size = new Size(300, 200);
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets a value indicating whether pressing the TAB key in a multiline
        /// <see cref="TextBox"/> control types a TAB character in the control instead of moving the
        /// focus to the next control in the tab order.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public new bool AcceptsTab
        {
            get { return base.AcceptsTab; }
            set { base.AcceptsTab = false; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether pressing ENTER in a multiline <see cref="TextBox"/>
        /// control creates a new line of text in the control or activates the default button for
        /// the form.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public new bool AcceptsReturn
        {
            get { return base.AcceptsReturn; }
            set { base.AcceptsReturn = true; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is a multiline <see cref="TextBox"/> control.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        public override bool Multiline
        {
            get { return base.Multiline; }
            set { base.Multiline = true; }
        }

        /// <summary>
        /// Gets or sets which scroll bars should appear in a multiline <see cref="TextBox"/> control.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(ScrollBars.Both)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public new ScrollBars ScrollBars
        {
            get { return base.ScrollBars; }
            set { base.ScrollBars = ScrollBars.Both; }
        }

        /// <summary>
        /// Indicates whether a multiline text box control automatically word wraps to the beginning
        /// of the next line when necessary.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public new bool WordWrap
        {
            get { return base.WordWrap; }
            set { base.WordWrap = false; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Raises the <see cref="Control.GotFocus"/> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> object that contains the event data.</param>
        protected override void OnGotFocus(EventArgs e)
        {
            int oldSelectionLength = this.SelectionLength;
            int oldSelectionStart = this.SelectionStart;

            base.OnGotFocus(e);

            // the base method selects all of the text by default, but we don't want that because
            // it's way too easy to delete a big chunk of command line text if it's selected
            if (oldSelectionLength == 0)
            {
                this.SelectionLength = 0;
                this.SelectionStart = oldSelectionStart;
            }
        }
    }
}
