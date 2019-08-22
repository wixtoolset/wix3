// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Contains native constants, functions, and structures for this assembly.
    /// </summary>
    internal static class NativeMethods
    {
        #region Error Constants
        internal const int S_OK = 0;
        internal const int E_MOREDATA = unchecked((int)0x800700ea);
        internal const int E_INSUFFICIENT_BUFFER = unchecked((int)0x8007007a);
        internal const int E_CANCELLED = unchecked((int)0x800704c7);
        internal const int E_ALREADYINITIALIZED = unchecked((int)0x800704df);
        internal const int E_NOTFOUND = unchecked((int)0x80070490);
        internal const int E_UNEXPECTED = unchecked((int)0x8000ffff);
        #endregion

        #region Functions
        [DllImport("shell32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
            out int pNumArgs
            );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr LocalFree(
            IntPtr hMem
            );
        #endregion
    }
}
