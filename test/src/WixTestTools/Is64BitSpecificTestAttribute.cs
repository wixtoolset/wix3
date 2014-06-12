//-----------------------------------------------------------------------
// <copyright file="Is64BitSpecificTestAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

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
