// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Native methods for Util extension.
    /// </summary>
    internal sealed class NativeMethods
    {
        /// <summary>
        /// Creates a self-extracting installation package.
        /// </summary>
        /// <param name="wzSetupStub">Path to the input setup stub exe.</param>
        /// <param name="rgPackages">Array of packages to put in setup stub exe.</param>
        /// <param name="cPackages">Count of packages to put in stub.</param>
        /// <param name="wzOutput">Path to the output setup exe.</param>
        /// <returns>HRESULT from function.</returns>
        [DllImport("setupbuilder.dll", EntryPoint = "CreateSetup", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int CreateSetup([MarshalAs(UnmanagedType.LPWStr)]string wzSetupStub, CREATE_SETUP_PACKAGE[] rgPackages, int cPackages, [MarshalAs(UnmanagedType.LPWStr)]string wzOutput);

        /// <summary>
        /// Creates a self-extracting installation package.
        /// </summary>
        /// <param name="wzSetupStub">Path to the input setup stub exe.</param>
        /// <param name="wzMsi">Path to the input MSI.</param>
        /// <param name="wzOutput">Path to the output setup exe.</param>
        /// <returns>HRESULT from function.</returns>
        [DllImport("setupbuilder.dll", EntryPoint = "CreateSimpleSetup", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int CreateSimpleSetup([MarshalAs(UnmanagedType.LPWStr)]string wzSetupStub, [MarshalAs(UnmanagedType.LPWStr)]string wzMsi, [MarshalAs(UnmanagedType.LPWStr)]string wzOutput);

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
