//-------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Native methods for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Native methods for ClickThrough.
    /// </summary>
    public class NativeMethods
    {
        public const int MAX_PATH = 260;

        public const uint SHGFI_ICON = 0x100; // retrieve icon
        public const uint SHGFI_LARGEICON = 0x0; // large icon
        public const uint SHGFI_OPENICON = 0x02; // open container icon
        public const uint SHGFI_SMALLICON = 0x1; // small icon
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        public const int WM_USER = 0x0400;

        /// <summary>
        /// Private contructor to make this a static class.
        /// </summary>
        private NativeMethods()
        {
        }

        /// <summary>
        /// Specifies an application-defined callback function used to send messages to and process messages from a Browse dialog box.
        /// </summary>
        /// <param name="hwnd">Window handle of the browse dialog box.</param>
        /// <param name="uMsg">Dialog box event that generated the message.</param>
        /// <param name="lParam">Value whose meaning depends on the event specified.</param>
        /// <param name="lpData">Application-defined value that was specified in the lParam member of the BROWSEINFO structure used in the call to SHBrowseForFolder.</param>
        /// <returns>Returns zero except in the case of BFFM_VALIDATEFAILED. For that flag, returns zero to dismiss the dialog or nonzero to keep the dialog displayed.</returns>
        public delegate int BrowseCallBackProc(IntPtr hwnd, int uMsg, IntPtr lParam, IntPtr lpData);

        /// <summary>
        /// Flags in the BROWSEINFO structure.
        /// </summary>
        public enum BrowseInfoFlags
        {
            /// <summary>
            /// Include an edit control in the browse dialog box that allows the user to type the name of an item.
            /// </summary>
            BIF_EDITBOX           = 0x0010,

            /// <summary>
            /// Use the new user interface.
            /// </summary>
            BIF_NEWDIALOGSTYLE    = 0x0040,

            /// <summary>
            /// Do not include the New Folder button in the browse dialog box.
            /// </summary>
            BIF_NONEWFOLDERBUTTON = 0x0200,
        }

        /// <summary>
        /// Messages sent to the BrowseCallBackProc.
        /// </summary>
        public enum BrowseCallBackMessages
        {
            /// <summary>
            /// The dialog box has finished initializing.
            /// </summary>
            BFFM_INITIALIZED   = 1,

            /// <summary>
            /// The selection has changed in the dialog box.
            /// </summary>
            BFFM_SELCHANGED    = 2,

            /// <summary>
            /// Enables or disables the dialog box's OK button.
            /// </summary>
            BFFM_ENABLEOK      = (WM_USER + 101),

            /// <summary>
            /// Specifies the path of a folder to select.
            /// </summary>
            BFFM_SETSELECTIONW = (WM_USER + 103),
        }

        /// <summary>
        /// Get the icon for a directory.
        /// </summary>
        /// <param name="small">true to retrieve a small icon; false otherwise.</param>
        /// <param name="open">true to retrieve an icon depicting an open item; false otherwise.</param>
        /// <returns>The directory icon.</returns>
        public static Icon GetDirectoryIcon(bool small, bool open)
        {
            uint flags = SHGFI_ICON;

            if (small)
            {
                flags |= SHGFI_SMALLICON;
            }
            else
            {
                flags |= SHGFI_LARGEICON;
            }

            if (open)
            {
                flags |= SHGFI_OPENICON;
            }

            SHFILEINFO shellFileInfo = new SHFILEINFO();
            IntPtr success = SHGetFileInfo(null, 0, ref shellFileInfo, (uint)Marshal.SizeOf(shellFileInfo), flags);

            // hack to ensure the icon handle is properly freed
            Icon tempIcon = Icon.FromHandle(shellFileInfo.hIcon);
            Icon icon = (Icon)tempIcon.Clone();
            tempIcon.Dispose();
            NativeMethods.DestroyIcon(shellFileInfo.hIcon);

            return icon;
        }

        /// <summary>
        /// Get the icon for a file.
        /// </summary>
        /// <param name="file">The file whose icon is to be retrieved.</param>
        /// <param name="small">true to retrieve a small icon; false otherwise.</param>
        /// <param name="open">true to retrieve an icon depicting an open item; false otherwise.</param>
        /// <returns>The file icon.</returns>
        public static Icon GetFileIcon(string file, bool small, bool open)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

            if (small)
            {
                flags |= SHGFI_SMALLICON;
            }
            else
            {
                flags |= SHGFI_LARGEICON;
            }

            if (open)
            {
                flags |= SHGFI_OPENICON;
            }

            // SHGetFileInfo doesn't work if the file path begins with '\'
            if (file.Length > 0 && Path.GetPathRoot(file) == @"\")
            {
                file = Path.GetFullPath(file);
            }

            SHFILEINFO shellFileInfo = new SHFILEINFO();
            SHGetFileInfo(file, 0, ref shellFileInfo, (uint)Marshal.SizeOf(shellFileInfo), flags);

            // hack to ensure the icon handle is properly freed
            Icon tempIcon = Icon.FromHandle(shellFileInfo.hIcon);
            Icon icon = (Icon)tempIcon.Clone();
            tempIcon.Dispose();
            NativeMethods.DestroyIcon(shellFileInfo.hIcon);

            return icon;
        }

        /// <summary>
        /// Destroys an icon and frees any memory the icon occupied.
        /// </summary>
        /// <param name="hIcon">Handle to the icon to be destroyed. The icon must not be in use.</param>
        /// <returns>true if the function succeeds; false otherwise.</returns>
        [DllImport("user32.dll", EntryPoint="DestroyIcon", SetLastError=true)]
        public static extern int DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Retrieves a handle to an icon from the specified executable file, dynamic-link library (DLL), or icon file.
        /// </summary>
        /// <param name="hInst">Handle to the instance of the application calling the function.</param>
        /// <param name="lpszExeFileName">Pointer to a null-terminated string specifying the name of an executable file, DLL, or icon file.</param>
        /// <param name="nIconIndex">Specifies the zero-based index of the icon to retrieve.</param>
        /// <returns>The return value is a handle to an icon.</returns>
        [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
        public static extern IntPtr ExtractIconW(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        /// <summary>
        /// Send the specified message to a window or windows
        /// </summary>
        /// <param name="hWnd">Handle to the window whose window procedure will receive the message.</param>
        /// <param name="msg">Specifies the message to be sent.</param>
        /// <param name="wParam">Specifies additional message-specific information.</param>
        /// <param name="lParam">Specifies additional message-specific information.</param>
        /// <returns>The return value specifies the result of the message processing; it depends on the message sent.</returns>
        [DllImport("user32.dll", CharSet=CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        /// <summary>
        /// Send the specified message to a window or windows
        /// </summary>
        /// <param name="hWnd">Handle to the window whose window procedure will receive the message.</param>
        /// <param name="msg">Specifies the message to be sent.</param>
        /// <param name="wParam">Specifies additional message-specific information.</param>
        /// <param name="lParam">Specifies additional message-specific information.</param>
        /// <returns>The return value specifies the result of the message processing; it depends on the message sent.</returns>
        [DllImport("user32.dll", CharSet=CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        /// <summary>
        /// Displays a dialog box that allows a user to select an icon from a module.
        /// </summary>
        /// <param name="hwnd">The handle of the parent window.</param>
        /// <param name="pszIconPath">A null-terminated string that contains the fully-qualified path of the default dynamic-link library (DLL) that contains the icons.</param>
        /// <param name="cchIconPath">The number of characters in pszIconPath, including the terminating NULL character.</param>
        /// <param name="piIconIndex">A pointer to an integer that, on entry, specified the index of the initial selection. On exit, the integer specifies the index of the icon that was selected.</param>
        /// <returns>Returns 1 if successful, or 0 if it fails.</returns>
        [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
        public static extern int PickIconDlg(IntPtr hwnd, StringBuilder pszIconPath, int cchIconPath, ref int piIconIndex);

        /// <summary>
        /// Displays a dialog box enabling the user to select a Shell folder.
        /// </summary>
        /// <param name="lpbi">Pointer to a BROWSEINFO structure.</param>
        /// <returns>Returns a pointer to an item identifier list (PIDL) specifying the location of the selected folder relative to the root of the namespace.</returns>
        [DllImport("shell32.dll")]
        public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

        /// <summary>
        /// Retrieves information about an object in the file system, such as a file, a folder, a directory, or a drive root.
        /// </summary>
        /// <param name="pszPath">The path of the object in the file system.</param>
        /// <param name="dwFileAttributes">Combination of one or more file attribute flags.</param>
        /// <param name="psfi">Address of a SHFILEINFO structure to receive the file information.</param>
        /// <param name="cbSizeFileInfo">Size, in bytes, of the SHFILEINFO structure pointed to by the psfi parameter.</param>
        /// <param name="uFlags">Flags that specify the file information to retrieve.</param>
        /// <returns>A value whose meaning depends on the uFlags parameter.</returns>
        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        /// <summary>
        /// Converts an item identifier list to a file system path.
        /// </summary>
        /// <param name="pidl">Address of an item identifier list that specifies a file or directory location relative to the root of the namespace (the desktop).</param>
        /// <param name="pszPath">Address of a buffer to receive the file system path. This buffer must be at least MAX_PATH characters in size.</param>
        /// <returns>Returns TRUE if successful, or FALSE otherwise.</returns>
        [DllImport("shell32.dll")]
        public static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        /// <summary>
        /// Creates a self-extracting installation package.
        /// </summary>
        /// <param name="wzSetupStub">Path to the input setup stub exe.</param>
        /// <param name="wzMsi">Path to the input MSI.</param>
        /// <param name="wzOutput">Path to the output setup exe.</param>
        [DllImport("setupbuilder.dll", EntryPoint="CreateSetup", CharSet=CharSet.Unicode, ExactSpelling=true)]
        public static extern int CreateSetup([MarshalAs(UnmanagedType.LPWStr)]string wzSetupStub, CREATE_SETUP_PACKAGE[] rgPackages, int cPackages, [MarshalAs(UnmanagedType.LPWStr)]string wzOutput);

        /// <summary>
        /// Creates a self-extracting installation package.
        /// </summary>
        /// <param name="wzSetupStub">Path to the input setup stub exe.</param>
        /// <param name="wzMsi">Path to the input MSI.</param>
        /// <param name="wzOutput">Path to the output setup exe.</param>
        [DllImport("setupbuilder.dll", EntryPoint="CreateSimpleSetup", CharSet=CharSet.Unicode, ExactSpelling=true)]
        public static extern int CreateSimpleSetup([MarshalAs(UnmanagedType.LPWStr)]string wzSetupStub, [MarshalAs(UnmanagedType.LPWStr)]string wzMsi, [MarshalAs(UnmanagedType.LPWStr)]string wzOutput);

        /// <summary>
        /// Contains parameters for the SHBrowseForFolder function and receives information about the folder selected by the user.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public string pszDisplayName;
            public string lpszTitle;
            public int ulFlags;
            public BrowseCallBackProc lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        /// <summary>
        /// Contains information about a file object.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        /// <summary>
        /// Contains parameters for the CreateSetup function and receives information about the folder selected by the user.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CREATE_SETUP_PACKAGE
        {
            public string wzSourcePath;
            public bool fPrivileged;
            public bool fCache;
        }
    }
}
