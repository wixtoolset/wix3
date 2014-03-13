//-------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Native methods for OfficeAddin fabricator extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Native methods for OfficeAddin fabricator extension.
    /// </summary>
    internal sealed class NativeMethods
    {
        public const int MAX_PATH = 260;

        public const uint SHGFI_ICON = 0x100; // retrieve icon
        public const uint SHGFI_LARGEICON = 0x0; // large icon
        public const uint SHGFI_OPENICON = 0x02; // open container icon
        public const uint SHGFI_SMALLICON = 0x1; // small icon
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        public const int WM_USER = 0x0400;

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
        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        public static extern int DestroyIcon(IntPtr hIcon);

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
        /// Creates a self-extracting installation package.
        /// </summary>
        /// <param name="wzShimStub">Path to the input stub shim dll.</param>
        /// <param name="wzApplicationId">Application id of the add-in.</param>
        /// <param name="wzClrVersion">Version of the CLR the add-in requires.</param>
        /// <param name="wzAssemblyName">Name of the assembly the shim should load.</param>
        /// <param name="wzClassName">Name of the class the shim should instantiate.</param>
        /// <param name="wzOutput">Path to put the updated shim.dll.</param>
        /// <returns>HRESULT from shim update.</returns>
        [DllImport("shimbld.dll", EntryPoint = "UpdateShim", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int UpdateShim(string wzShimStub, string wzApplicationId, string wzClrVersion, string wzAssemblyName, string wzClassName, string wzOutput);

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
    }
}
