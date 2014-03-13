//-------------------------------------------------------------------------------------------------
// <copyright file="metautil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Metabase helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// okay, this may look a little weird, but metautil.h cannot be in the 
// pre-compiled header because we need to #define these things so the
// correct GUID's get pulled into this object file
#include <initguid.h>
#include "metautil.h"


// prototypes
static void Sort(
    __in_ecount(cArray) DWORD dwArray[], 
    __in int cArray
    );


/********************************************************************
 MetaFindWebBase - finds a metabase base string that matches IP, Port and Header

********************************************************************/
extern "C" HRESULT DAPI MetaFindWebBase(
    __in IMSAdminBaseW* piMetabase, 
    __in_z LPCWSTR wzIP, 
    __in int iPort, 
    __in_z LPCWSTR wzHeader,
    __in BOOL fSecure,
    __out_ecount(cchWebBase) LPWSTR wzWebBase, 
    __in DWORD cchWebBase
    )
{
    Assert(piMetabase && cchWebBase);

    HRESULT hr = S_OK;

    BOOL fFound = FALSE;

    WCHAR wzKey[METADATA_MAX_NAME_LEN];
    WCHAR wzSubkey[METADATA_MAX_NAME_LEN];
    DWORD dwIndex = 0;

    METADATA_RECORD mr;
    METADATA_RECORD mrAddress;

    LPWSTR pwzExists = NULL;
    LPWSTR pwzIPExists = NULL;
    LPWSTR pwzPortExists = NULL;
    int iPortExists = 0;
    LPCWSTR pwzHeaderExists = NULL;

    memset(&mr, 0, sizeof(mr));
    mr.dwMDIdentifier = MD_KEY_TYPE;
    mr.dwMDAttributes = METADATA_INHERIT;
    mr.dwMDUserType = IIS_MD_UT_SERVER;
    mr.dwMDDataType = ALL_METADATA;

    memset(&mrAddress, 0, sizeof(mrAddress));
    mrAddress.dwMDIdentifier = (fSecure) ? MD_SECURE_BINDINGS : MD_SERVER_BINDINGS;
    mrAddress.dwMDAttributes = METADATA_INHERIT;
    mrAddress.dwMDUserType = IIS_MD_UT_SERVER;
    mrAddress.dwMDDataType = ALL_METADATA;

    // loop through the "web keys" looking for the "IIsWebServer" key that matches wzWeb
    for (dwIndex = 0; SUCCEEDED(hr); ++dwIndex)
    { 
        hr = piMetabase->EnumKeys(METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC", wzSubkey, dwIndex); 
        if (FAILED(hr))
            break;

        ::StringCchPrintfW(wzKey, countof(wzKey), L"/LM/W3SVC/%s", wzSubkey);
        hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mr);
        if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
        {
            hr = S_FALSE;  // didn't find anything, try next one
            continue;
        }
        ExitOnFailure(hr, "failed to get key from metabase while searching for web servers");

        // if we have an IIsWebServer store the key
        if (0 == lstrcmpW(L"IIsWebServer", (LPCWSTR)mr.pbMDData))
        {
            hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mrAddress);
            if (MD_ERROR_DATA_NOT_FOUND == hr)
                hr = S_FALSE;
            ExitOnFailure(hr, "failed to get address from metabase while searching for web servers");

            // break down the first address into parts
            pwzIPExists = reinterpret_cast<LPWSTR>(mrAddress.pbMDData);
            pwzExists = wcsstr(pwzIPExists, L":");
            if (NULL == pwzExists)
                continue;

            *pwzExists = L'\0';

            pwzPortExists = pwzExists + 1;
            pwzExists = wcsstr(pwzPortExists, L":");
            if (NULL == pwzExists)
                continue;

            *pwzExists = L'\0';
            iPortExists = wcstol(pwzPortExists, NULL, 10);

            pwzHeaderExists = pwzExists + 1;

            // compare the passed in address with the address listed for this web
            if (S_OK == hr && 
                (0 == lstrcmpW(wzIP, pwzIPExists) || 0 == lstrcmpW(wzIP, L"*")) &&
                iPort == iPortExists &&
                0 == lstrcmpW(wzHeader, pwzHeaderExists))
            {
                // if the passed in buffer wasn't big enough
                hr = ::StringCchCopyW(wzWebBase, cchWebBase, wzKey);
                ExitOnFailure1(hr, "failed to copy in web base: %ls", wzKey);

                fFound = TRUE;
                break;
            }
        }
    } 

    if (E_NOMOREITEMS == hr)
    {
        Assert(!fFound);
        hr = S_FALSE;
    }

LExit:
    MetaFreeValue(&mrAddress);
    MetaFreeValue(&mr);

    if (!fFound && SUCCEEDED(hr))
        hr = S_FALSE;

    return hr;
}


/********************************************************************
 MetaFindFreeWebBase - finds the next metabase base string

********************************************************************/
extern "C" HRESULT DAPI MetaFindFreeWebBase(
    __in IMSAdminBaseW* piMetabase, 
    __out_ecount(cchWebBase) LPWSTR wzWebBase, 
    __in DWORD cchWebBase
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;

    WCHAR wzKey[METADATA_MAX_NAME_LEN];
    WCHAR wzSubkey[METADATA_MAX_NAME_LEN];
    DWORD dwSubKeys[100];
    int cSubKeys = 0;
    DWORD dwIndex = 0;

    int i;
    DWORD dwKey;

    METADATA_RECORD mr;

    memset(&mr, 0, sizeof(mr));
    mr.dwMDIdentifier = MD_KEY_TYPE;
    mr.dwMDAttributes = 0;
    mr.dwMDUserType = IIS_MD_UT_SERVER;
    mr.dwMDDataType = STRING_METADATA;

    // loop through the "web keys" looking for the "IIsWebServer" key that matches wzWeb
    for (dwIndex = 0; SUCCEEDED(hr); ++dwIndex)
    { 
        hr = piMetabase->EnumKeys(METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC", wzSubkey, dwIndex); 
        if (FAILED(hr))
            break;

        ::StringCchPrintfW(wzKey, countof(wzKey), L"/LM/W3SVC/%s", wzSubkey);

        hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mr);
        if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
        {
            hr = S_FALSE;  // didn't find anything, try next one
            continue;
        }
        ExitOnFailure(hr, "failed to get key from metabase while searching for free web root");

        // if we have a IIsWebServer get the address information
        if (0 == lstrcmpW(L"IIsWebServer", reinterpret_cast<LPCWSTR>(mr.pbMDData)))
        {
            if (cSubKeys >= countof(dwSubKeys))
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                ExitOnFailure(hr, "Insufficient buffer to track all sub-WebSites");
            }

            dwSubKeys[cSubKeys] = wcstol(wzSubkey, NULL, 10);
            ++cSubKeys;
            Sort(dwSubKeys, cSubKeys);
        }
    } 

    if (E_NOMOREITEMS == hr)
        hr = S_OK;
    ExitOnFailure(hr, "failed to find free web root");

    // find the lowest free web root
    dwKey  = 1;
    for (i = 0; i < cSubKeys; ++i)
    {
        if (dwKey < dwSubKeys[i])
            break;

        dwKey = dwSubKeys[i] + 1;
    }

    hr = ::StringCchPrintfW(wzWebBase, cchWebBase, L"/LM/W3SVC/%u", dwKey);
LExit:
    MetaFreeValue(&mr);
    return hr;
}


/********************************************************************
 MetaOpenKey - open key

********************************************************************/
extern "C" HRESULT DAPI MetaOpenKey(
    __in IMSAdminBaseW* piMetabase, 
    __in METADATA_HANDLE mhKey, 
    __in_z LPCWSTR wzKey,
    __in DWORD dwAccess,
    __in DWORD cRetries,
    __out METADATA_HANDLE* pmh
    )
{
    Assert(piMetabase && pmh);

    HRESULT hr = S_OK;

    // loop while the key is busy
    do
    {
        hr = piMetabase->OpenKey(mhKey, wzKey, dwAccess, 10, pmh);
        if (HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr)
            ::SleepEx(1000, TRUE);
    } while (HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr && 0 < cRetries--);

    return hr;
}


/********************************************************************
 MetaGetValue - finds the next metabase base string

 NOTE: piMetabase is optional
********************************************************************/
extern "C" HRESULT DAPI MetaGetValue(
    __in IMSAdminBaseW* piMetabase, 
    __in METADATA_HANDLE mhKey, 
    __in_z LPCWSTR wzKey, 
    __inout METADATA_RECORD* pmr
    )
{
    Assert(pmr);

    HRESULT hr = S_OK;
    BOOL fInitialized = FALSE;
    DWORD cbRequired = 0;

    if (!piMetabase)
    {
        hr = ::CoInitialize(NULL);
        ExitOnFailure(hr, "failed to initialize COM");
        fInitialized = TRUE;

        hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<LPVOID*>(&piMetabase)); 
        ExitOnFailure(hr, "failed to get IID_IMSAdminBaseW object");
    }

    if (!pmr->pbMDData)
    {
        pmr->dwMDDataLen = 256;
        pmr->pbMDData = static_cast<BYTE*>(MemAlloc(pmr->dwMDDataLen, TRUE));
        ExitOnNull(pmr->pbMDData, hr, E_OUTOFMEMORY, "failed to allocate memory for metabase value");
    }
    else  // set the size of the data to the actual size of the memory
        pmr->dwMDDataLen = (DWORD)MemSize(pmr->pbMDData);

    hr = piMetabase->GetData(mhKey, wzKey, pmr, &cbRequired);
    if (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) == hr)
    {
        pmr->dwMDDataLen = cbRequired;
        BYTE* pb = static_cast<BYTE*>(MemReAlloc(pmr->pbMDData, pmr->dwMDDataLen, TRUE));
        ExitOnNull(pb, hr, E_OUTOFMEMORY, "failed to reallocate memory for metabase value");

        pmr->pbMDData = pb;
        hr = piMetabase->GetData(mhKey, wzKey, pmr, &cbRequired);
    }
    ExitOnFailure(hr, "failed to get metabase data");

LExit:
    if (fInitialized)
    {
        ReleaseObject(piMetabase);
        ::CoUninitialize();
    }

    return hr;
}


/********************************************************************
 MetaFreeValue - frees data in METADATA_RECORD remove MetaGetValue()

 NOTE: METADATA_RECORD must have been returned from MetaGetValue() above
********************************************************************/
extern "C" void DAPI MetaFreeValue(
    __in METADATA_RECORD* pmr
    )
{
    Assert(pmr);

    ReleaseNullMem(pmr->pbMDData);
}


//
// private
//

/********************************************************************
 Sort - quick and dirty insertion sort

********************************************************************/
static void Sort(
    __in_ecount(cArray) DWORD dwArray[], 
    __in int cArray
    )
{
    int i, j;
    DWORD dwData;

    for (i = 1; i < cArray; ++i)
    {
        dwData = dwArray[i];

        j = i - 1;
        while (0 <= j && dwArray[j] > dwData)
        {
            dwArray[j + 1] = dwArray[j];
            j--;
        }

        dwArray[j + 1] = dwData;
    }
}
