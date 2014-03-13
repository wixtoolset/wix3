//-------------------------------------------------------------------------------------------------
// <copyright file="Header.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// User control for displaying a pretty header.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    /// <summary>
    /// User control for displaying a pretty header.
    /// </summary>
    public class Header : System.Windows.Forms.UserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private int index;
        private Font titleFont;

        /// <summary>
        /// Instantiates a header.
        /// </summary>
        public Header()
        {
            // This call is required by the Windows.Forms Form Designer.
            this.InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);

            this.titleFont = new Font("Microsoft Sans Serif", 20f);
        }

        /// <summary>
        /// Gets or sets the index of this header.
        /// </summary>
        /// <value>The index of this header.</value>
        public int Index
        {
            get
            {
                return this.index;
            }

            set
            {
                this.index = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.components != null)
                {
                    this.components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Paints the background.
        /// </summary>
        /// <param name="pevent">The paint event arguments.</param>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Graphics grfx = pevent.Graphics;

            grfx.Clear(this.BackColor);

            Color blendColor;
            switch (this.index)
            {
                case 0:
                    blendColor = Color.Red;
                    break;
                case 1:
                    blendColor = Color.Orange;
                    break;
                default:
                    throw new ApplicationException();
            }

            if (this.ClientRectangle.Width > 0 && this.ClientRectangle.Height > 0)
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle, blendColor, this.BackColor, 90f, true))
                {
                    brush.SetSigmaBellShape(1.0f, 1.0f);
                    grfx.FillRectangle(brush, this.ClientRectangle);
                }
            }
        }

        /// <summary>
        /// Paints the foreground.
        /// </summary>
        /// <param name="e">The paint event arguments.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics grfx = e.Graphics;

            string title;
            string description;
            switch (this.index)
            {
                case 0:
                    title = "Files";
                    description = "Select the files to install.";
                    break;
                case 1:
                    title = "Package";
                    description = "Select the package settings and view a summary of the contents of this package.";
                    break;
                default:
                    throw new ApplicationException();
            }

            grfx.DrawString(title, this.titleFont, new SolidBrush(this.ForeColor), 20f, 10f);
            grfx.DrawString(description, this.Font, new SolidBrush(this.ForeColor), 20f, 60f);
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // Header
            // 
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.Name = "Header";
            this.Size = new System.Drawing.Size(664, 88);

        }
        #endregion
    }
}
