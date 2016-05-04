// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Denotes that a particular test case requires running natively in a 64-bit process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class Is64BitSpecificTestAttribute : Attribute // TODO: Implement ITraitAttribute when Xunit releases it.
    {
        /// <summary>
        /// Gets whether the current OS is a 64-bit OS.
        /// </summary>
        public static bool Is64BitOperatingSystem
        {
            get
            {
                if (4 == IntPtr.Size)
                {
                    bool isWow64Process;
                    Is64BitSpecificTestAttribute.IsWow64Process(Process.GetCurrentProcess().Handle, out isWow64Process);

                    return isWow64Process;
                }

                return 8 == IntPtr.Size;
            }
        }

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
    }
}
