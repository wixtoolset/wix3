//-------------------------------------------------------------------------------------------------
// <copyright file="deputil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Common function definitions for the dependency/ref-counting feature.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define ARRAY_GROWTH_SIZE 5

static LPCWSTR vcszVersionValue = L"Version";
static LPCWSTR vcszDisplayNameValue = L"DisplayName";
static LPCWSTR vcszMinVersionValue = L"MinVersion";
static LPCWSTR vcszMaxVersionValue = L"MaxVersion";
static LPCWSTR vcszAttributesValue = L"Attributes";

enum eRequiresAttributes { RequiresAttributesMinVersionInclusive = 256, RequiresAttributesMaxVersionInclusive = 512 };

// We write to Software\Classes explicitly based on the current security context instead of HKCR.
// See http://msdn.microsoft.com/en-us/library/ms724475(VS.85).aspx for more information.
static LPCWSTR vsczRegistryRoot = L"Software\\Classes\\Installer\\Dependencies\\";
static LPCWSTR vsczRegistryDependents = L"Dependents";

static HRESULT AllocDependencyKeyName(
    __in_z LPCWSTR wzName,
    __deref_out_z LPWSTR* psczKeyName
    );

static HRESULT GetDependencyNameFromKey(
    __in HKEY hkHive,
    __in LPCWSTR wzKey,
    __deref_out_z LPWSTR* psczName
    );

DAPI_(HRESULT) DepGetProviderInformation(
    __in HKEY hkHive,
    __in_z LPCWSTR wzProviderKey,
    __deref_out_z_opt LPWSTR* psczId,
    __deref_out_z_opt LPWSTR* psczName,
    __out_opt DWORD64* pqwVersion
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    HKEY hkKey = NULL;

    // Format the provider dependency registry key.
    hr = AllocDependencyKeyName(wzProviderKey, &sczKey);
    ExitOnFailure1(hr, "Failed to allocate the registry key for dependency \"%ls\".", wzProviderKey);

    // Try to open the dependency key.
    hr = RegOpen(hkHive, sczKey, KEY_READ, &hkKey);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    ExitOnFailure1(hr, "Failed to open the registry key for the dependency \"%ls\".", wzProviderKey);

    // Get the Id if requested and available.
    if (psczId)
    {
        hr = RegReadString(hkKey, NULL, psczId);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure1(hr, "Failed to get the id for the dependency \"%ls\".", wzProviderKey);
    }

    // Get the DisplayName if requested and available.
    if (psczName)
    {
        hr = RegReadString(hkKey, vcszDisplayNameValue, psczName);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure1(hr, "Failed to get the name for the dependency \"%ls\".", wzProviderKey);
    }

    // Get the Version if requested and available.
    if (pqwVersion)
    {
        hr = RegReadVersion(hkKey, vcszVersionValue, pqwVersion);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure1(hr, "Failed to get the version for the dependency \"%ls\".", wzProviderKey);
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczKey);

    return hr;
}

DAPI_(HRESULT) DepCheckDependency(
    __in HKEY hkHive,
    __in_z LPCWSTR wzProviderKey,
    __in_z_opt LPCWSTR wzMinVersion,
    __in_z_opt LPCWSTR wzMaxVersion,
    __in int iAttributes,
    __in STRINGDICT_HANDLE sdDependencies,
    __deref_inout_ecount_opt(*pcDependencies) DEPENDENCY** prgDependencies,
    __inout LPUINT pcDependencies
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    HKEY hkKey = NULL;
    DWORD64 dw64Version = 0;
    int cchMinVersion = 0;
    DWORD64 dw64MinVersion = 0;
    int cchMaxVersion = 0;
    DWORD64 dw64MaxVersion = 0;
    BOOL fAllowEqual = FALSE;
    LPWSTR sczName = NULL;

    // Format the provider dependency registry key.
    hr = AllocDependencyKeyName(wzProviderKey, &sczKey);
    ExitOnFailure1(hr, "Failed to allocate the registry key for dependency \"%ls\".", wzProviderKey);

    // Try to open the key. If that fails, add the missing dependency key to the dependency array if it doesn't already exist.
    hr = RegOpen(hkHive, sczKey, KEY_READ, &hkKey);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to open the registry key for dependency \"%ls\".", wzProviderKey);

        // If there are no registry values, consider the key orphaned and treat it as missing.
        hr = RegReadVersion(hkKey, vcszVersionValue, &dw64Version);
        if (E_FILENOTFOUND != hr)
        {
            ExitOnFailure2(hr, "Failed to read the %ls registry value for dependency \"%ls\".", vcszVersionValue, wzProviderKey);
        }
    }

    // If the key was not found or the Version value was not found, add the missing dependency key to the dependency array.
    if (E_FILENOTFOUND == hr)
    {
        hr = DictKeyExists(sdDependencies, wzProviderKey);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure1(hr, "Failed to check the dictionary for missing dependency \"%ls\".", wzProviderKey);
        }
        else
        {
            hr = DepDependencyArrayAlloc(prgDependencies, pcDependencies, wzProviderKey, NULL);
            ExitOnFailure1(hr, "Failed to add the missing dependency \"%ls\" to the array.", wzProviderKey);

            hr = DictAddKey(sdDependencies, wzProviderKey);
            ExitOnFailure1(hr, "Failed to add the missing dependency \"%ls\" to the dictionary.", wzProviderKey);
        }

        // Exit since the check already failed.
        ExitFunction1(hr = E_NOTFOUND);
    }

    // Check MinVersion if provided.
    if (wzMinVersion)
    {
        cchMinVersion = lstrlenW(wzMinVersion);
        if (0 < cchMinVersion)
        {
            hr = FileVersionFromStringEx(wzMinVersion, cchMinVersion, &dw64MinVersion);
            ExitOnFailure1(hr, "Failed to get the 64-bit version number from \"%ls\".", wzMinVersion);

            fAllowEqual = iAttributes & RequiresAttributesMinVersionInclusive;
            if (!(fAllowEqual && dw64MinVersion <= dw64Version || dw64MinVersion < dw64Version))
            {
                hr = DictKeyExists(sdDependencies, wzProviderKey);
                if (E_NOTFOUND != hr)
                {
                    ExitOnFailure1(hr, "Failed to check the dictionary for the older dependency \"%ls\".", wzProviderKey);
                }
                else
                {
                    hr = RegReadString(hkKey, vcszDisplayNameValue, &sczName);
                    ExitOnFailure1(hr, "Failed to get the display name of the older dependency \"%ls\".", wzProviderKey);

                    hr = DepDependencyArrayAlloc(prgDependencies, pcDependencies, wzProviderKey, sczName);
                    ExitOnFailure1(hr, "Failed to add the older dependency \"%ls\" to the dependencies array.", wzProviderKey);

                    hr = DictAddKey(sdDependencies, wzProviderKey);
                    ExitOnFailure1(hr, "Failed to add the older dependency \"%ls\" to the unique dependency string list.", wzProviderKey);
                }

                // Exit since the check already failed.
                ExitFunction1(hr = E_NOTFOUND);
            }
        }
    }

    // Check MaxVersion if provided.
    if (wzMaxVersion)
    {
        cchMaxVersion = lstrlenW(wzMaxVersion);
        if (0 < cchMaxVersion)
        {
            hr = FileVersionFromStringEx(wzMaxVersion, cchMaxVersion, &dw64MaxVersion);
            ExitOnFailure1(hr, "Failed to get the 64-bit version number from \"%ls\".", wzMaxVersion);

            fAllowEqual = iAttributes & RequiresAttributesMaxVersionInclusive;
            if (!(fAllowEqual && dw64Version <= dw64MaxVersion || dw64Version < dw64MaxVersion))
            {
                hr = DictKeyExists(sdDependencies, wzProviderKey);
                if (E_NOTFOUND != hr)
                {
                    ExitOnFailure1(hr, "Failed to check the dictionary for the newer dependency \"%ls\".", wzProviderKey);
                }
                else
                {
                    hr = RegReadString(hkKey, vcszDisplayNameValue, &sczName);
                    ExitOnFailure1(hr, "Failed to get the display name of the newer dependency \"%ls\".", wzProviderKey);

                    hr = DepDependencyArrayAlloc(prgDependencies, pcDependencies, wzProviderKey, sczName);
                    ExitOnFailure1(hr, "Failed to add the newer dependency \"%ls\" to the dependencies array.", wzProviderKey);

                    hr = DictAddKey(sdDependencies, wzProviderKey);
                    ExitOnFailure1(hr, "Failed to add the newer dependency \"%ls\" to the unique dependency string list.", wzProviderKey);
                }

                // Exit since the check already failed.
                ExitFunction1(hr = E_NOTFOUND);
            }
        }
    }

LExit:
    ReleaseStr(sczName);
    ReleaseRegKey(hkKey);
    ReleaseStr(sczKey);

    return hr;
}

DAPI_(HRESULT) DepCheckDependents(
    __in HKEY hkHive,
    __in_z LPCWSTR wzProviderKey,
    __reserved int /*iAttributes*/,
    __in C_STRINGDICT_HANDLE sdIgnoredDependents,
    __deref_inout_ecount_opt(*pcDependents) DEPENDENCY** prgDependents,
    __inout LPUINT pcDependents
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    HKEY hkProviderKey = NULL;
    HKEY hkDependentsKey = NULL;
    LPWSTR sczDependentKey = NULL;
    LPWSTR sczDependentName = NULL;

    // Format the provider dependency registry key.
    hr = AllocDependencyKeyName(wzProviderKey, &sczKey);
    ExitOnFailure1(hr, "Failed to allocate the registry key for dependency \"%ls\".", wzProviderKey);

    // Try to open the key. If that fails, the dependency information is corrupt.
    hr = RegOpen(hkHive, sczKey, KEY_READ, &hkProviderKey);
    ExitOnFailure1(hr, "Failed to open the registry key \"%ls\". The dependency store is corrupt.", sczKey);

    // Try to open the dependencies key. If that does not exist, there are no dependents.
    hr = RegOpen(hkProviderKey, vsczRegistryDependents, KEY_READ, &hkDependentsKey);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to open the registry key for dependents of \"%ls\".", wzProviderKey);
    }
    else
    {
        ExitFunction1(hr = S_OK);
    }

    // Now enumerate the dependent keys. If they are not defined in the ignored list, add them to the array.
    for (DWORD dwIndex = 0; ; ++dwIndex)
    {
        hr = RegKeyEnum(hkDependentsKey, dwIndex, &sczDependentKey);
        if (E_NOMOREITEMS != hr)
        {
            ExitOnFailure1(hr, "Failed to enumerate the dependents key of \"%ls\".", wzProviderKey);
        }
        else
        {
            hr = S_OK;
            break;
        }

        // If the key isn't ignored, add it to the dependent array.
        hr = DictKeyExists(sdIgnoredDependents, sczDependentKey);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to check the dictionary of ignored dependents.");
        }
        else
        {
            // Get the name of the dependent from the key.
            hr = GetDependencyNameFromKey(hkHive, sczDependentKey, &sczDependentName);
            ExitOnFailure1(hr, "Failed to get the name of the dependent from the key \"%ls\".", sczDependentKey);

            hr = DepDependencyArrayAlloc(prgDependents, pcDependents, sczDependentKey, sczDependentName);
            ExitOnFailure1(hr, "Failed to add the dependent key \"%ls\" to the string array.", sczDependentKey);
        }
    }

LExit:
    ReleaseStr(sczDependentName);
    ReleaseStr(sczDependentKey);
    ReleaseRegKey(hkDependentsKey);
    ReleaseRegKey(hkProviderKey);
    ReleaseStr(sczKey);

    return hr;
}

DAPI_(HRESULT) DepRegisterDependency(
    __in HKEY hkHive,
    __in_z LPCWSTR wzProviderKey,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzDisplayName,
    __in_z_opt LPCWSTR wzId,
    __in int iAttributes
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    HKEY hkKey = NULL;
    BOOL fCreated = FALSE;

    // Format the provider dependency registry key.
    hr = AllocDependencyKeyName(wzProviderKey, &sczKey);
    ExitOnFailure1(hr, "Failed to allocate the registry key for dependency \"%ls\".", wzProviderKey);

    // Create the dependency key (or open it if it already exists).
    hr = RegCreateEx(hkHive, sczKey, KEY_WRITE, FALSE, NULL, &hkKey, &fCreated);
    ExitOnFailure1(hr, "Failed to create the dependency registry key \"%ls\".", sczKey);

    // Set the id if it was provided.
    if (wzId)
    {
        hr = RegWriteString(hkKey, NULL, wzId);
        ExitOnFailure2(hr, "Failed to set the %ls registry value to \"%ls\".", L"default", wzId);
    }

    // Set the version.
    hr = RegWriteString(hkKey, vcszVersionValue, wzVersion);
    ExitOnFailure2(hr, "Failed to set the %ls registry value to \"%ls\".", vcszVersionValue, wzVersion);

    // Set the display name.
    hr = RegWriteString(hkKey, vcszDisplayNameValue, wzDisplayName);
    ExitOnFailure2(hr, "Failed to set the %ls registry value to \"%ls\".", vcszDisplayNameValue, wzDisplayName);

    // Set the attributes if non-zero.
    if (0 != iAttributes)
    {
        hr = RegWriteNumber(hkKey, vcszAttributesValue, static_cast<DWORD>(iAttributes));
        ExitOnFailure2(hr, "Failed to set the %ls registry value to %d.", vcszAttributesValue, iAttributes);
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczKey);

    return hr;
}

DAPI_(HRESULT) DepDependentExists(
    __in HKEY hkHive,
    __in_z LPCWSTR wzDependencyProviderKey,
    __in_z LPCWSTR wzProviderKey
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDependentKey = NULL;
    HKEY hkDependentKey = NULL;

    // Format the provider dependents registry key.
    hr = StrAllocFormatted(&sczDependentKey, L"%ls%ls\\%ls\\%ls", vsczRegistryRoot, wzDependencyProviderKey, vsczRegistryDependents, wzProviderKey);
    ExitOnFailure(hr, "Failed to format registry key to dependent.");

    hr = RegOpen(hkHive, sczDependentKey, KEY_READ, &hkDependentKey);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to open the dependent registry key at: \"%ls\".", sczDependentKey);
    }

LExit:
    ReleaseRegKey(hkDependentKey);
    ReleaseStr(sczDependentKey);

    return hr;
}

DAPI_(HRESULT) DepRegisterDependent(
    __in HKEY hkHive,
    __in_z LPCWSTR wzDependencyProviderKey,
    __in_z LPCWSTR wzProviderKey,
    __in_z_opt LPCWSTR wzMinVersion,
    __in_z_opt LPCWSTR wzMaxVersion,
    __in int iAttributes
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDependencyKey = NULL;
    HKEY hkDependencyKey = NULL;
    LPWSTR sczKey = NULL;
    HKEY hkKey = NULL;
    BOOL fCreated = FALSE;

    // Format the provider dependency registry key.
    hr = AllocDependencyKeyName(wzDependencyProviderKey, &sczDependencyKey);
    ExitOnFailure1(hr, "Failed to allocate the registry key for dependency \"%ls\".", wzDependencyProviderKey);

    // Create the dependency key (or open it if it already exists).
    hr = RegCreateEx(hkHive, sczDependencyKey, KEY_WRITE, FALSE, NULL, &hkDependencyKey, &fCreated);
    ExitOnFailure1(hr, "Failed to create the dependency registry key \"%ls\".", sczDependencyKey);

    // Create the subkey to register the dependent.
    hr = StrAllocFormatted(&sczKey, L"%ls\\%ls", vsczRegistryDependents, wzProviderKey);
    ExitOnFailure2(hr, "Failed to allocate dependent subkey \"%ls\" under dependency \"%ls\".", wzProviderKey, wzDependencyProviderKey);

    hr = RegCreateEx(hkDependencyKey, sczKey, KEY_WRITE, FALSE, NULL, &hkKey, &fCreated);
    ExitOnFailure1(hr, "Failed to create the dependency subkey \"%ls\".", sczKey);

    // Set the minimum version if not NULL.
    hr = RegWriteString(hkKey, vcszMinVersionValue, wzMinVersion);
    ExitOnFailure2(hr, "Failed to set the %ls registry value to \"%ls\".", vcszMinVersionValue, wzMinVersion);

    // Set the maximum version if not NULL.
    hr = RegWriteString(hkKey, vcszMaxVersionValue, wzMaxVersion);
    ExitOnFailure2(hr, "Failed to set the %ls registry value to \"%ls\".", vcszMaxVersionValue, wzMaxVersion);

    // Set the attributes if non-zero.
    if (0 != iAttributes)
    {
        hr = RegWriteNumber(hkKey, vcszAttributesValue, static_cast<DWORD>(iAttributes));
        ExitOnFailure2(hr, "Failed to set the %ls registry value to %d.", vcszAttributesValue, iAttributes);
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczKey);
    ReleaseRegKey(hkDependencyKey);
    ReleaseStr(sczDependencyKey);

    return hr;
}

DAPI_(HRESULT) DepUnregisterDependency(
    __in HKEY hkHive,
    __in_z LPCWSTR wzProviderKey
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    HKEY hkKey = NULL;

    // Format the provider dependency registry key.
    hr = AllocDependencyKeyName(wzProviderKey, &sczKey);
    ExitOnFailure1(hr, "Failed to allocate the registry key for dependency \"%ls\".", wzProviderKey);

    // Delete the entire key including all sub-keys.
    hr = RegDelete(hkHive, sczKey, REG_KEY_DEFAULT, TRUE);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to delete the key \"%ls\".", sczKey);
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczKey);

    return hr;
}

DAPI_(HRESULT) DepUnregisterDependent(
    __in HKEY hkHive,
    __in_z LPCWSTR wzDependencyProviderKey,
    __in_z LPCWSTR wzProviderKey
    )
{
    HRESULT hr = S_OK;
    HKEY hkRegistryRoot = NULL;
    HKEY hkDependencyProviderKey = NULL;
    HKEY hkRegistryDependents = NULL;
    DWORD cSubKeys = 0;
    DWORD cValues = 0;

    // Open the root key. We may delete the wzDependencyProviderKey during clean up.
    hr = RegOpen(hkHive, vsczRegistryRoot, KEY_READ, &hkRegistryRoot);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to open root registry key \"%ls\".", vsczRegistryRoot);
    }
    else
    {
        ExitFunction();
    }

    // Try to open the dependency key. If that does not exist, simply return.
    hr = RegOpen(hkRegistryRoot, wzDependencyProviderKey, KEY_READ, &hkDependencyProviderKey);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to open the registry key for the dependency \"%ls\".", wzDependencyProviderKey);
    }
    else
    {
        ExitFunction();
    }

    // Try to open the dependents subkey to enumerate.
    hr = RegOpen(hkDependencyProviderKey, vsczRegistryDependents, KEY_READ, &hkRegistryDependents);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to open the dependents subkey under the dependency \"%ls\".", wzDependencyProviderKey);
    }
    else
    {
        ExitFunction();
    }

    // Delete the wzProviderKey dependent sub-key.
    hr = RegDelete(hkRegistryDependents, wzProviderKey, REG_KEY_DEFAULT, TRUE);
    ExitOnFailure2(hr, "Failed to delete the dependent \"%ls\" under the dependency \"%ls\".", wzProviderKey, wzDependencyProviderKey);

    // If there are no remaining dependents, delete the Dependents subkey.
    hr = RegQueryKey(hkRegistryDependents, &cSubKeys, NULL);
    ExitOnFailure1(hr, "Failed to get the number of dependent subkeys under the dependency \"%ls\".", wzDependencyProviderKey);
    
    if (0 < cSubKeys)
    {
        ExitFunction();
    }

    // Release the handle to make sure it's deleted immediately.
    ReleaseRegKey(hkRegistryDependents);

    // Fail if there are any subkeys since we just checked.
    hr = RegDelete(hkDependencyProviderKey, vsczRegistryDependents, REG_KEY_DEFAULT, FALSE);
    ExitOnFailure1(hr, "Failed to delete the dependents subkey under the dependency \"%ls\".", wzDependencyProviderKey);

    // If there are no values, delete the provider dependency key.
    hr = RegQueryKey(hkDependencyProviderKey, NULL, &cValues);
    ExitOnFailure1(hr, "Failed to get the number of values under the dependency \"%ls\".", wzDependencyProviderKey);

    if (0 == cValues)
    {
        // Release the handle to make sure it's deleted immediately.
        ReleaseRegKey(hkDependencyProviderKey);

        // Fail if there are any subkeys since we just checked.
        hr = RegDelete(hkRegistryRoot, wzDependencyProviderKey, REG_KEY_DEFAULT, FALSE);
        ExitOnFailure1(hr, "Failed to delete the dependency \"%ls\".", wzDependencyProviderKey);
    }

LExit:
    ReleaseRegKey(hkRegistryDependents);
    ReleaseRegKey(hkDependencyProviderKey);
    ReleaseRegKey(hkRegistryRoot);

    return hr;
}

DAPI_(HRESULT) DepDependencyArrayAlloc(
    __deref_inout_ecount_opt(*pcDependencies) DEPENDENCY** prgDependencies,
    __inout LPUINT pcDependencies,
    __in_z LPCWSTR wzKey,
    __in_z_opt LPCWSTR wzName
    )
{
    HRESULT hr = S_OK;
    UINT cRequired = 0;
    DEPENDENCY* pDependency = NULL;

    hr = ::UIntAdd(*pcDependencies, 1, &cRequired);
    ExitOnFailure(hr, "Failed to increment the number of elements required in the dependency array.");

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgDependencies), cRequired, sizeof(DEPENDENCY), ARRAY_GROWTH_SIZE);
    ExitOnFailure(hr, "Failed to allocate memory for the dependency array.");

    pDependency = static_cast<DEPENDENCY*>(&(*prgDependencies)[*pcDependencies]);
    ExitOnNull(pDependency, hr, E_POINTER, "The dependency element in the array is invalid.");

    hr = StrAllocString(&(pDependency->sczKey), wzKey, 0);
    ExitOnFailure(hr, "Failed to allocate the string key in the dependency array.");

    if (wzName)
    {
        hr = StrAllocString(&(pDependency->sczName), wzName, 0);
        ExitOnFailure(hr, "Failed to allocate the string name in the dependency array.");
    }

    // Update the number of current elements in the dependency array.
    *pcDependencies = cRequired;

LExit:
    return hr;
}

DAPI_(void) DepDependencyArrayFree(
    __in_ecount(cDependencies) DEPENDENCY* rgDependencies,
    __in UINT cDependencies
    )
{
    for (UINT i = 0; i < cDependencies; ++i)
    {
        ReleaseStr(rgDependencies[i].sczKey);
        ReleaseStr(rgDependencies[i].sczName);
    }

    ReleaseMem(rgDependencies);
}

/***************************************************************************
 AllocDependencyKeyName - Allocates and formats the root registry key name.

***************************************************************************/
static HRESULT AllocDependencyKeyName(
    __in_z LPCWSTR wzName,
    __deref_out_z LPWSTR* psczKeyName
    )
{
    HRESULT hr = S_OK;
    size_t cchName = 0;
    size_t cchKeyName = 0;

    // Get the length of the static registry root once.
    static size_t cchRegistryRoot = ::lstrlenW(vsczRegistryRoot);

    // Get the length of the dependency, and add to the length of the root.
    hr = ::StringCchLengthW(wzName, STRSAFE_MAX_CCH, &cchName);
    ExitOnFailure(hr, "Failed to get string length of dependency name.");

    // Add the sizes together to allocate memory once (callee will add space for nul).
    hr = ::SizeTAdd(cchRegistryRoot, cchName, &cchKeyName);
    ExitOnFailure(hr, "Failed to add the string lengths together.");

    // Allocate and concat the strings together.
    hr = StrAllocString(psczKeyName, vsczRegistryRoot, cchKeyName);
    ExitOnFailure(hr, "Failed to allocate string for dependency registry root.");

    hr = StrAllocConcat(psczKeyName, wzName, cchName);
    ExitOnFailure(hr, "Failed to concatenate the dependency key name.");

LExit:
    return hr;
}

/***************************************************************************
 GetDependencyNameFromKey - Attempts to name of the dependency from the key.

***************************************************************************/
static HRESULT GetDependencyNameFromKey(
    __in HKEY hkHive,
    __in LPCWSTR wzProviderKey,
    __deref_out_z LPWSTR* psczName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    HKEY hkKey = NULL;

    // Format the provider dependency registry key.
    hr = AllocDependencyKeyName(wzProviderKey, &sczKey);
    ExitOnFailure1(hr, "Failed to allocate the registry key for dependency \"%ls\".", wzProviderKey);

    // Try to open the dependency key.
    hr = RegOpen(hkHive, sczKey, KEY_READ, &hkKey);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to open the registry key for the dependency \"%ls\".", wzProviderKey);
    }
    else
    {
        ExitFunction1(hr = S_OK);
    }

    // Get the DisplayName if available.
    hr = RegReadString(hkKey, vcszDisplayNameValue, psczName);
    if (E_FILENOTFOUND != hr)
    {
        ExitOnFailure1(hr, "Failed to get the dependency name for the dependency \"%ls\".", wzProviderKey);
    }
    else
    {
        ExitFunction1(hr = S_OK);
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczKey);

    return hr;
}
