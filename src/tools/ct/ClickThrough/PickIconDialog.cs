//-------------------------------------------------------------------------------------------------
// <copyright file="PickIconDialog.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Wrapper for the system icon selection dialog.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Wrapper for the system icon selection dialog.
    /// </summary>
    public sealed class PickIconDialog : Component
    {
        private Icon icon;
        private string iconFile;
        private int iconIndex;

        /// <summary>
        /// Instantiate a new PickIconDialog class.
        /// </summary>
        public PickIconDialog()
        {
            this.iconIndex = -1;
        }

        /// <summary>
        /// Gets the selected icon.
        /// </summary>
        /// <value>The selected icon.</value>
        public Icon Icon
        {
            get { return this.icon; }
        }

        /// <summary>
        /// Gets the file containing the selected icon.
        /// </summary>
        /// <value>The file containing the selected icon.</value>
        public string IconFile
        {
            get { return this.iconFile; }
            set { this.iconFile = value; }
        }

        /// <summary>
        /// Gets the index of the selected icon.
        /// </summary>
        /// <value>The index of the selected icon.</value>
        public int IconIndex
        {
            get { return this.iconIndex; }
            set { this.iconIndex = value; }
        }

        /// <summary>
        /// Show the icon picker dialog.
        /// </summary>
        /// <param name="owner">Any object that implements IWin32Window that represents the top-level window that will own the modal dialog box.</param>
        /// <returns>DialogResult.OK if the user clicks OK in the dialog box; otherwise, DialogResult.Cancel.</returns>
        public DialogResult ShowDialog(IWin32Window owner)
        {
            if (this.IconFile == null || this.iconIndex == -1)
            {
                throw new ArgumentException("The icon file or index has not yet been specified.");
            }

            int tempIconIndex = this.iconIndex;
            StringBuilder iconFileBuffer = new StringBuilder(String.Empty);
            iconFileBuffer.EnsureCapacity(2048);

            if (NativeMethods.PickIconDlg(owner.Handle, iconFileBuffer, iconFileBuffer.Capacity, ref tempIconIndex) != 0)
            {
                string tempIconFile = Environment.ExpandEnvironmentVariables(iconFileBuffer.ToString());

                IntPtr iconHandle = NativeMethods.ExtractIconW(Process.GetCurrentProcess().Handle, tempIconFile, tempIconIndex);
                if (iconHandle == IntPtr.Zero || iconHandle.ToInt32() == 1)
                {
                    throw new ApplicationException(String.Format("Cannot load icon from '{0}' with index {1}.", tempIconFile, tempIconIndex));
                }

                // hack to ensure the icon handle is properly freed
                Icon tempIcon = Icon.FromHandle(iconHandle);
                this.icon = (Icon)tempIcon.Clone();
                tempIcon.Dispose();
                NativeMethods.DestroyIcon(iconHandle);

                // save the new values
                this.iconFile = tempIconFile;
                this.iconIndex = tempIconIndex;

                return DialogResult.OK;
            }
            else
            {
                return DialogResult.Cancel;
            }
        }
    }
}
