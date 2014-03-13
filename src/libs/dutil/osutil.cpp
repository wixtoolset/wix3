//-------------------------------------------------------------------------------------------------
// <copyright file="osutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Operating system helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

OS_VERSION vOsVersion = OS_VERSION_UNKNOWN;
DWORD vdwOsServicePack = 0;

/********************************************************************
 OsGetVersion

********************************************************************/
extern "C" void DAPI OsGetVersion(
    __out OS_VERSION* pVersion,
    __out DWORD* pdwServicePack
    )
{
    OSVERSIONINFOEXW ovi = { };

    if (OS_VERSION_UNKNOWN == vOsVersion)
    {
        ovi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
        ::GetVersionExW(reinterpret_cast<OSVERSIONINFOW*>(&ovi)); // only fails if version info size is set incorrectly.

        vdwOsServicePack = static_cast<DWORD>(ovi.wServicePackMajor) << 16 | ovi.wServicePackMinor;
        if (4 == ovi.dwMajorVersion)
        {
            vOsVersion = OS_VERSION_WINNT;
        }
        else if (5 == ovi.dwMajorVersion)
        {
            if (0 == ovi.dwMinorVersion)
            {
                vOsVersion = OS_VERSION_WIN2000;
            }
            else if (1 == ovi.dwMinorVersion)
            {
                vOsVersion = OS_VERSION_WINXP;
            }
            else if (2 == ovi.dwMinorVersion)
            {
                vOsVersion = OS_VERSION_WIN2003;
            }
            else
            {
                vOsVersion = OS_VERSION_FUTURE;
            }
        }
        else if (6 == ovi.dwMajorVersion)
        {
            if (0 == ovi.dwMinorVersion)
            {
                vOsVersion = (VER_NT_WORKSTATION == ovi.wProductType) ? OS_VERSION_VISTA : OS_VERSION_WIN2008;
            }
            else if (1 == ovi.dwMinorVersion)
            {
                vOsVersion = (VER_NT_WORKSTATION == ovi.wProductType) ? OS_VERSION_WIN7 : OS_VERSION_WIN2008_R2;
            }
            else
            {
                vOsVersion = OS_VERSION_FUTURE;
            }
        }
        else
        {
            vOsVersion = OS_VERSION_FUTURE;
        }
    }

    *pVersion = vOsVersion;
    *pdwServicePack = vdwOsServicePack;
}

extern "C" HRESULT DAPI OsCouldRunPrivileged(
    __out BOOL* pfPrivileged
    )
{
    HRESULT hr = S_OK;
    BOOL fUacEnabled = FALSE;
    SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;
    PSID AdministratorsGroup = NULL;

    // Do a best effort check to see if UAC is enabled on this machine.
    OsIsUacEnabled(&fUacEnabled);

    // If UAC is enabled then the process could run privileged by asking to elevate.
    if (fUacEnabled)
    {
        *pfPrivileged = TRUE;
    }
    else // no UAC so only privilged if user is in administrators group.
    {
        *pfPrivileged = ::AllocateAndInitializeSid(&NtAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &AdministratorsGroup);
        if (*pfPrivileged)
        {
            if (!::CheckTokenMembership(NULL, AdministratorsGroup, pfPrivileged))
            {
                 *pfPrivileged = FALSE;
            }
        }
    }

    ReleaseSid(AdministratorsGroup);
    return hr;
}

extern "C" HRESULT DAPI OsIsRunningPrivileged(
    __out BOOL* pfPrivileged
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    HANDLE hToken = NULL;
    TOKEN_ELEVATION_TYPE elevationType = TokenElevationTypeDefault;
    DWORD dwSize = 0;
    SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;
    PSID AdministratorsGroup = NULL;

    if (!::OpenProcessToken(::GetCurrentProcess(), TOKEN_QUERY, &hToken))
    {
        ExitOnLastError(hr, "Failed to open process token.");
    }

    if (::GetTokenInformation(hToken, TokenElevationType, &elevationType, sizeof(TOKEN_ELEVATION_TYPE), &dwSize))
    {
        *pfPrivileged = (TokenElevationTypeFull == elevationType);
        ExitFunction1(hr = S_OK);
    }

    // If it's invalid argument, this means they don't support TokenElevationType, and we should fallback to another check
    er = ::GetLastError();
    if (ERROR_INVALID_FUNCTION == er)
    {
        er = ERROR_SUCCESS;
    }
    ExitOnWin32Error(er, hr, "Failed to get process token information.");

    // Fallback to this check for some OS's (like XP)
    *pfPrivileged = ::AllocateAndInitializeSid(&NtAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &AdministratorsGroup);
    if (*pfPrivileged)
    {
        if (!::CheckTokenMembership(NULL, AdministratorsGroup, pfPrivileged))
        {
             *pfPrivileged = FALSE;
        }
    }

LExit:
    ReleaseSid(AdministratorsGroup);

    if (hToken)
    {
        ::CloseHandle(hToken);
    }

    return hr;
}

extern "C" HRESULT DAPI OsIsUacEnabled(
    __out BOOL* pfUacEnabled
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;
    DWORD dwUacEnabled = 0;

    *pfUacEnabled = FALSE; // assume UAC not enabled.

    hr = RegOpen(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", KEY_READ, &hk);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to open system policy key to detect UAC.");

    hr = RegReadNumber(hk, L"EnableLUA", &dwUacEnabled);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to read registry value to detect UAC.");

    *pfUacEnabled = (0 != dwUacEnabled);

LExit:
    ReleaseRegKey(hk);

    return hr;
}
