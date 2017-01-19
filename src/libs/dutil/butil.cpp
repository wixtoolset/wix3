// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "butil.h"

// constants
// From engine/registration.h
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = L"BundleUpgradeCode";
const LPCWSTR BUNDLE_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = L"BundleProviderKey";

// Forward declarations.
static HRESULT OpenBundleKey(
    __in LPCWSTR wzBundleId,
    __in BUNDLE_INSTALL_CONTEXT context, 
    __inout HKEY *key);

/********************************************************************
BundleGetBundleInfo - Queries the bundle installation metadata for a given property

RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT)
        The bundle is not installed
    HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY)
        The property is unrecognized
    HRESULT_FROM_WIN32(ERROR_MORE_DATA)
        A buffer is too small to hold the requested data.
    E_NOTIMPL:
        Tried to read a bundle attribute for a type which has not been implemented

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
extern "C" HRESULT DAPI BundleGetBundleInfo(
  __in LPCWSTR wzBundleId,
  __in LPCWSTR wzAttribute,
  __out_ecount_opt(*pcchValueBuf) LPWSTR lpValueBuf,
  __inout_opt LPDWORD pcchValueBuf
  )
{
    Assert(wzBundleId && wzAttribute);

    HRESULT hr = S_OK;
    BUNDLE_INSTALL_CONTEXT context = BUNDLE_INSTALL_CONTEXT_MACHINE;
    LPWSTR sczValue = NULL;
    HKEY hkBundle = NULL;
    DWORD cchSource = 0;
    DWORD dwType = 0;
    DWORD dwValue = 0;

    if ((lpValueBuf && !pcchValueBuf) || !wzBundleId || !wzAttribute)
    {
        ExitOnFailure(hr = E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    if (FAILED(hr = OpenBundleKey(wzBundleId, context = BUNDLE_INSTALL_CONTEXT_MACHINE, &hkBundle)) &&
        FAILED(hr = OpenBundleKey(wzBundleId, context = BUNDLE_INSTALL_CONTEXT_USER, &hkBundle)))
    {
        ExitOnFailure(E_FILENOTFOUND == hr ? HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) : hr, "Failed to locate bundle uninstall key path.");
    }

    // If the bundle doesn't have the property defined, return ERROR_UNKNOWN_PROPERTY
    hr = RegGetType(hkBundle, wzAttribute, &dwType);
    ExitOnFailure(E_FILENOTFOUND == hr ? HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) : hr, "Failed to locate bundle property.");

    switch (dwType)
    {
        case REG_SZ:
            hr = RegReadString(hkBundle, wzAttribute, &sczValue);
            ExitOnFailure(hr, "Failed to read string property.");
            break;
        case REG_DWORD:
            hr = RegReadNumber(hkBundle, wzAttribute, &dwValue);
            ExitOnFailure(hr, "Failed to read dword property.");

            hr = StrAllocFormatted(&sczValue, L"%d", dwValue);
            ExitOnFailure(hr, "Failed to format dword property as string.");
            break;
        default:
            ExitOnFailure1(hr = E_NOTIMPL, "Reading bundle info of type 0x%x not implemented.", dwType);

    }

    hr = ::StringCchLengthW(sczValue, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchSource));
    ExitOnFailure(hr, "Failed to calculate length of string");

    if (lpValueBuf)
    {
        // cchSource is the length of the string not including the terminating null character
        if (*pcchValueBuf <= cchSource)
        {
            *pcchValueBuf = ++cchSource;
            ExitOnFailure(hr = HRESULT_FROM_WIN32(ERROR_MORE_DATA), "A buffer is too small to hold the requested data.");
        }

        hr = ::StringCchCatNExW(lpValueBuf, *pcchValueBuf, sczValue, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
        ExitOnFailure(hr, "Failed to copy the property value to the output buffer.");
        
        *pcchValueBuf = cchSource++;        
    }

LExit:
    ReleaseRegKey(hkBundle);
    ReleaseStr(sczValue);

    return hr;
}

/********************************************************************
BundleEnumRelatedBundle - Queries the bundle installation metadata for installs with the given upgrade code

NOTE: lpBundleIdBuff is a buffer to receive the bundle GUID. This buffer must be 39 characters long. 
        The first 38 characters are for the GUID, and the last character is for the terminating null character.
RETURNS:
    E_INVALIDARG
        An invalid parameter was passed to the function.

    All other returns are unexpected returns from other dutil methods.
********************************************************************/
HRESULT DAPI BundleEnumRelatedBundle(
  __in LPCWSTR wzUpgradeCode,
  __in BUNDLE_INSTALL_CONTEXT context,
  __inout PDWORD pdwStartIndex,
  __out_ecount(MAX_GUID_CHARS+1) LPWSTR lpBundleIdBuf
    )
{
    HRESULT hr = S_OK;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == context ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    HKEY hkUninstall = NULL;
    HKEY hkBundle = NULL;
    LPWSTR sczUninstallSubKey = NULL;
    DWORD cchUninstallSubKey = 0;
    LPWSTR sczUninstallSubKeyPath = NULL;
    LPWSTR sczValue = NULL;
    DWORD dwType = 0;

    LPWSTR* rgsczBundleUpgradeCodes = NULL;
    DWORD cBundleUpgradeCodes = 0;
    BOOL fUpgradeCodeFound = FALSE;

    if (!wzUpgradeCode || !lpBundleIdBuf || !pdwStartIndex)
    {
        ExitOnFailure(hr = E_INVALIDARG, "An invalid parameter was passed to the function.");
    }

    hr = RegOpen(hkRoot, BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, KEY_READ, &hkUninstall);
    ExitOnFailure(hr, "Failed to open bundle uninstall key path.");

    for (DWORD dwIndex = *pdwStartIndex; !fUpgradeCodeFound; dwIndex++)
    {
        hr = RegKeyEnum(hkUninstall, dwIndex, &sczUninstallSubKey);
        ExitOnFailure(hr, "Failed to enumerate bundle uninstall key path.");

        hr = StrAllocFormatted(&sczUninstallSubKeyPath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, sczUninstallSubKey);
        ExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");
        
        hr = RegOpen(hkRoot, sczUninstallSubKeyPath, KEY_READ, &hkBundle);
        ExitOnFailure(hr, "Failed to open uninstall key path.");

        // If it's a bundle, it should have a BundleUpgradeCode value of type REG_SZ (old) or REG_MULTI_SZ
        hr = RegGetType(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &dwType);
        if (FAILED(hr))
        {
            ReleaseRegKey(hkBundle);
            ReleaseNullStr(sczUninstallSubKey);
            ReleaseNullStr(sczUninstallSubKeyPath);
            // Not a bundle
            continue;
        }

        switch (dwType)
        {
            case REG_SZ:
                hr = RegReadString(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &sczValue);
                ExitOnFailure(hr, "Failed to read BundleUpgradeCode string property.");
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczValue, -1, wzUpgradeCode, -1))
                {
                    *pdwStartIndex = dwIndex;
                    fUpgradeCodeFound = TRUE;
                    break;
                }

                ReleaseNullStr(sczValue);

                break;
            case REG_MULTI_SZ:
                hr = RegReadStringArray(hkBundle, BUNDLE_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, &rgsczBundleUpgradeCodes, &cBundleUpgradeCodes);
                ExitOnFailure(hr, "Failed to read BundleUpgradeCode  multi-string property.");

                for (DWORD i = 0; i < cBundleUpgradeCodes; i++)
                {
                    LPWSTR wzBundleUpgradeCode = rgsczBundleUpgradeCodes[i];
                    if (wzBundleUpgradeCode && *wzBundleUpgradeCode)
                    {
                        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzBundleUpgradeCode, -1, wzUpgradeCode, -1))
                        {
                            *pdwStartIndex = dwIndex;
                            fUpgradeCodeFound = TRUE;
                            break;
                        }
                    }
                }
                ReleaseNullStrArray(rgsczBundleUpgradeCodes, cBundleUpgradeCodes);

                break;

            default:
                ExitOnFailure1(hr = E_NOTIMPL, "BundleUpgradeCode of type 0x%x not implemented.", dwType);

        }

        if (fUpgradeCodeFound)
        {
            if (lpBundleIdBuf)
            {
                hr = ::StringCchLengthW(sczUninstallSubKey, STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchUninstallSubKey));
                ExitOnFailure(hr, "Failed to calculate length of string");

                hr = ::StringCchCopyNExW(lpBundleIdBuf, MAX_GUID_CHARS + 1, sczUninstallSubKey, cchUninstallSubKey, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
                ExitOnFailure(hr, "Failed to copy the property value to the output buffer.");
            }

            break;
        }

        // Cleanup before next iteration
        ReleaseRegKey(hkBundle);
        ReleaseNullStr(sczUninstallSubKey);
        ReleaseNullStr(sczUninstallSubKeyPath);
    }

LExit:
    ReleaseStr(sczValue);
    ReleaseStr(sczUninstallSubKey);
    ReleaseStr(sczUninstallSubKeyPath);
    ReleaseRegKey(hkBundle);
    ReleaseRegKey(hkUninstall);
    ReleaseStrArray(rgsczBundleUpgradeCodes, cBundleUpgradeCodes);

    return hr;
}

/********************************************************************
OpenBundleKey - Opens the bundle uninstallation key for a given bundle

NOTE: caller is responsible for closing key
********************************************************************/
HRESULT OpenBundleKey(
    __in LPCWSTR wzBundleId,
    __in BUNDLE_INSTALL_CONTEXT context, 
    __inout HKEY *key)
{
    Assert(key && wzBundleId);
    AssertSz(NULL == *key, "*key should be null");

    HRESULT hr = S_OK;
    HKEY hkRoot = BUNDLE_INSTALL_CONTEXT_USER == context ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
    LPWSTR sczKeypath = NULL;

    hr = StrAllocFormatted(&sczKeypath, L"%ls\\%ls", BUNDLE_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId);
    ExitOnFailure(hr, "Failed to allocate bundle uninstall key path.");
    
    hr = RegOpen(hkRoot, sczKeypath, KEY_READ, key);
    ExitOnFailure(hr, "Failed to open bundle uninstall key path.");

LExit:
    ReleaseStr(sczKeypath);

    return hr;
}
