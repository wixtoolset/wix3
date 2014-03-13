//-------------------------------------------------------------------------------------------------
// <copyright file="WixFolderBrowserDialog.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Wrapper for the folder browser dialog.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Wrapper for the folder browser dialog.
    /// </summary>
    public class WixFolderBrowserDialog : Component
    {
        private string description;
        private string selectedPath;

        /// <summary>
        /// Instantiate a new WixFolderBrowserDialog class.
        /// </summary>
        public WixFolderBrowserDialog()
        {
            this.description = String.Empty;
        }

        /// <summary>
        /// Gets or sets the description to display in the dialog.
        /// </summary>
        /// <value>The description to display in the dialog.</value>
        public string Description
        {
            get { return this.description; }
            set { this.description = (value != null ? value : String.Empty); }
        }

        /// <summary>
        /// Gets the path selected by the user.
        /// </summary>
        /// <value>The path selected by the user.</value>
        public string SelectedPath
        {
            get { return this.selectedPath; }
            set { this.selectedPath = value; }
        }

        /// <summary>
        /// Show the folder browser dialog.
        /// </summary>
        /// <param name="owner">Any object that implements IWin32Window that represents the top-level window that will own the modal dialog box.</param>
        /// <returns>DialogResult.OK if the user clicks OK in the dialog box; otherwise, DialogResult.Cancel.</returns>
        public DialogResult ShowDialog(IWin32Window owner)
        {
            string displayName = new string('\0', NativeMethods.MAX_PATH);
            NativeMethods.BrowseCallBackProc callback = new NativeMethods.BrowseCallBackProc(this.BrowseCallBackProc);
            NativeMethods.BROWSEINFO bi = new NativeMethods.BROWSEINFO();
            IntPtr returnedPidl = IntPtr.Zero;

            try
            {
                bi.hwndOwner = owner.Handle;
                bi.pidlRoot = IntPtr.Zero;
                bi.pszDisplayName = displayName;
                bi.lpszTitle = this.description;
                bi.ulFlags = (int)NativeMethods.BrowseInfoFlags.BIF_EDITBOX | (int)NativeMethods.BrowseInfoFlags.BIF_NEWDIALOGSTYLE | (int)NativeMethods.BrowseInfoFlags.BIF_NONEWFOLDERBUTTON;
                bi.lpfn = callback;
                bi.lParam = IntPtr.Zero;
                bi.iImage = 0;

                returnedPidl = NativeMethods.SHBrowseForFolder(ref bi);

                if (returnedPidl != IntPtr.Zero)
                {
                    StringBuilder tempSelectedPath = new StringBuilder(NativeMethods.MAX_PATH);

                    if (NativeMethods.SHGetPathFromIDList(returnedPidl, tempSelectedPath))
                    {
                        this.SelectedPath = tempSelectedPath.ToString();

                        return DialogResult.OK;
                    }
                }

                return DialogResult.Cancel;
            }
            finally
            {
                if (returnedPidl != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(returnedPidl);
                }
            }
        }

        /// <summary>
        /// Specifies an application-defined callback function used to send messages to and process messages from a Browse dialog box displayed in response to a call to SHBrowseForFolder.
        /// </summary>
        /// <param name="hwnd">Window handle of the browse dialog box.</param>
        /// <param name="msg">Dialog box event that generated the message.</param>
        /// <param name="leftParam">Value whose meaning depends on the event specified.</param>
        /// <param name="data">Application-defined value that was specified in the lParam member of the BROWSEINFO structure used in the call to SHBrowseForFolder.</param>
        /// <returns>Returns zero except in the case of BFFM_VALIDATEFAILED. For that flag, returns zero to dismiss the dialog or nonzero to keep the dialog displayed.</returns>
        private int BrowseCallBackProc(IntPtr hwnd, int msg, IntPtr leftParam, IntPtr data)
        {
            switch (msg)
            {
                case (int)NativeMethods.BrowseCallBackMessages.BFFM_INITIALIZED:
                    NativeMethods.SendMessage(hwnd, (int)NativeMethods.BrowseCallBackMessages.BFFM_SETSELECTIONW, 1, this.selectedPath);
                    break;
                case (int)NativeMethods.BrowseCallBackMessages.BFFM_SELCHANGED:
                    if (leftParam != IntPtr.Zero)
                    {
                        StringBuilder tempSelectedPath = new StringBuilder(NativeMethods.MAX_PATH);
                        bool available = NativeMethods.SHGetPathFromIDList(leftParam, tempSelectedPath);

                        NativeMethods.SendMessage(hwnd, (int)NativeMethods.BrowseCallBackMessages.BFFM_ENABLEOK, 0, available ? 1 : 0);
                    }
                    break;
            }

            return 0;
        }
    }
}
