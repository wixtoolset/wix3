//-----------------------------------------------------------------------
// <copyright file="UacUtilities.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains common methods used to manipulate files and directories.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Utilities
{
    using Microsoft.Win32;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

    /// <summary>
    /// Utility methods for UAC.
    /// </summary>
    public static class UacUtilities
    {
        // Following code was adapted from http://stackoverflow.com/questions/1220213/detect-if-running-as-administrator-with-or-without-elevated-privileges.
        private static readonly string uacRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
        private static readonly string uacRegistryValue = "EnableLUA";

        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, TokenInformationClass TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

        private enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        private enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        /// <summary>
        /// Gets whether UAC is enabled.
        /// </summary>
        public static bool IsUacEnabled
        {
            get
            {
                using (RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(UacUtilities.uacRegistryKey, false))
                {
                    return (1).Equals(uacKey.GetValue(UacUtilities.uacRegistryValue));
                }
            }
        }

        /// <summary>
        /// Gets whether the current process is elevated.
        /// </summary>
        public static bool IsProcessElevated
        {
            get
            {
                if (UacUtilities.IsUacEnabled)
                {
                    IntPtr tokenHandle;
                    if (!UacUtilities.OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_READ, out tokenHandle))
                    {
                        throw new Win32Exception("Could not get the token handle of the current process.");
                    }

                    TokenElevationType elevationResult = TokenElevationType.TokenElevationTypeDefault;

                    int elevationResultSize = Marshal.SizeOf((int)elevationResult);
                    uint returnedSize = 0;
                    IntPtr elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);

                    if (UacUtilities.GetTokenInformation(tokenHandle, TokenInformationClass.TokenElevationType, elevationTypePtr, (uint)elevationResultSize, out returnedSize))
                    {
                        elevationResult = (TokenElevationType)Marshal.ReadInt32(elevationTypePtr);
                        return elevationResult == TokenElevationType.TokenElevationTypeFull;
                    }
                    else
                    {
                        throw new Exception("Could not determine whether the current process is elevated.");
                    }
                }
                else
                {
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    {
                        WindowsPrincipal principal = new WindowsPrincipal(identity);
                        return principal.IsInRole(WindowsBuiltInRole.Administrator);
                    }
                }
            }
        }
    }
}
