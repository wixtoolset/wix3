//-------------------------------------------------------------------------------------------------
// <copyright file="VerifyInterop.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Platform Invoke functions for using WinVerifyTrust
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal class VerifyInterop
    {
        internal const string GenericVerify2 = "00AAC56B-CD44-11d0-8CC2-00C04FC295EE";
        internal const uint WTD_UI_NONE = 2;
        internal const uint WTD_REVOKE_NONE = 0;
        internal const uint WTD_CHOICE_CATALOG = 2;
        internal const uint WTD_STATEACTION_VERIFY = 1;
        internal const uint WTD_REVOCATION_CHECK_NONE = 0x10;
        internal const int ErrorInsufficientBuffer = 122;

        [StructLayout(LayoutKind.Sequential)]
        internal struct WinTrustData
        {
            internal uint cbStruct;
            internal IntPtr pPolicyCallbackData;
            internal IntPtr pSIPClientData;
            internal uint dwUIChoice;
            internal uint fdwRevocationChecks;
            internal uint dwUnionChoice;
            internal IntPtr pCatalog;
            internal uint dwStateAction;
            internal IntPtr hWVTStateData;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszURLReference;
            internal uint dwProvFlags;
            internal uint dwUIContext;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WinTrustCatalogInfo
        {
            internal uint cbStruct;
            internal uint dwCatalogVersion;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pcwszCatalogFilePath;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pcwszMemberTag;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pcwszMemberFilePath;
            internal IntPtr hMemberFile;
            internal IntPtr pbCalculatedFileHash;
            internal uint cbCalculatedFileHash;
            internal IntPtr pcCatalogContext;
        }

        [DllImport("wintrust.dll", SetLastError = true)]
        internal static extern long WinVerifyTrust(IntPtr windowHandle, ref Guid actionGuid, ref WinTrustData trustData);

        [DllImport("wintrust.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptCATAdminCalcHashFromFileHandle(
            IntPtr fileHandle,
            [In, Out]
            ref uint hashSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            byte[] hashBytes,
            uint flags);
    }
}