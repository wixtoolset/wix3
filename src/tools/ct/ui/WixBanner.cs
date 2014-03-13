//-------------------------------------------------------------------------------------------------
// <copyright file="WixBanner.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixBanner class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp
{
    using System;
    using System.Drawing.Drawing2D;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// The banner shown at the top of each Wix-specific property page.
    /// </summary>
    public class WixBanner : Control
    {
        private static readonly Color LeftGradientColor = Color.FromArgb(unchecked((int)0xffbf1f25));
        private static readonly Color RightGradientColor = Color.FromArgb(unchecked((int)0x00f8f8f8));

        private Bitmap logo = Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough.StepPictures.Logo;

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBanner"/> class.
        /// </summary>
        public WixBanner()
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            this.ResizeRedraw = true;
            this.DoubleBuffered = true;

            this.Font = new Font("Verdana", 14.0f, FontStyle.Bold);
            this.ForeColor = Color.White;

            this.MinimumSize = this.logo.Size;
        }

        /// <summary>
        /// Gets or sets the banner text.
        /// </summary>
        /// <value>String for banner text.</value>
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                if (this.Text != value)
                {
                    base.Text = value;
                    this.Invalidate(this.GradientArea);
                }
            }
        }

        /// <summary>
        /// Gets the area occupied by the image.
        /// </summary>
        private Rectangle ImageArea
        {
            get { return new Rectangle(new Point(this.Width - this.logo.Width, 0), this.logo.Size); }
        }

        /// <summary>
        /// Gets the area covered by the gradient.
        /// </summary>
        private Rectangle GradientArea
        {
            get { return new Rectangle(0, 0, this.Width, 40); }
        }

        /// <summary>
        /// Paints the control.
        /// </summary>
        /// <param name="e">The <see cref="PaintEventArgs"/> object that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            // draw the gradient
            Rectangle gradientArea = this.GradientArea;
            using (LinearGradientBrush brush = new LinearGradientBrush(gradientArea, LeftGradientColor, RightGradientColor, LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, gradientArea);
            }

            // use of the TextRenderer prevents printing, but supports international characters much better
            TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            Rectangle textArea = this.GradientArea;
            textArea.Width -= this.logo.Width;
            TextRenderer.DrawText(g, this.Text, this.Font, textArea, this.ForeColor, flags);

            // draw the image
            g.DrawImageUnscaled(this.logo, this.ImageArea);
        }

        /// <summary>
        /// Occurs when the control has been resized.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> object that contains the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // set the control's region to only include the gradient and image areas
            GraphicsPath path = new GraphicsPath();
            Rectangle nonOverlappingImageArea = this.ImageArea;
            nonOverlappingImageArea.Y = this.GradientArea.Bottom;
            nonOverlappingImageArea.Height -= this.GradientArea.Height;

            path.AddRectangle(this.GradientArea);
            path.AddRectangle(nonOverlappingImageArea);
            this.Region = new Region(path);
        }
    }
}
