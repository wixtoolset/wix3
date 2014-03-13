//-------------------------------------------------------------------------------------------------
// <copyright file="scafilter.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    IIS Filter functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// prototypes
static HRESULT ReadFilterLoadOrder(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzFilterRoot,
    __out LPWSTR *ppwzLoadOrder
    );
static HRESULT AddFilterToLoadOrder(
    __in LPCWSTR wzFilter,
    __in int iLoadOrder,
    __inout LPWSTR *ppwzLoadOrder
    );
static HRESULT RemoveFilterFromLoadOrder(
    __in LPCWSTR wzFilter,
    __inout LPWSTR *ppwzLoadOrder
    );


UINT __stdcall ScaFiltersRead(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in WCA_WRAPQUERY_HANDLE hWebBaseQuery,
    __inout SCA_FILTER** ppsfList,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec;
    INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
    INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

    LPWSTR pwzData = NULL;

    SCA_FILTER* psf = NULL;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaAppPoolRead");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaFiltersRead() - no IIsFilter table");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the filters
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        // Get the Component first.  If the component is not being modified during
        // this transaction, skip processing this whole record.
        hr = WcaGetRecordString(hRec, fqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get IIsFilter.Component");

        hr = WcaGetRecordInteger(hRec, fqInstalled, (int *)&isInstalled);
        ExitOnFailure(hr, "Failed to get Component installed state for IIs filter");

        hr = WcaGetRecordInteger(hRec, fqAction, (int *)&isAction);
        ExitOnFailure(hr, "Failed to get Component action state for IIs filter");

        if (!WcaIsInstalling(isInstalled, isAction) &&
            !WcaIsReInstalling(isInstalled, isAction) &&
            !WcaIsUninstalling(isInstalled, isAction))
        {
            continue; // skip this record.
        }

        hr = AddFilterToList(ppsfList);
        ExitOnFailure(hr, "failed to add filter to list");

        psf = *ppsfList;

        hr = ::StringCchCopyW(psf->wzComponent, countof(psf->wzComponent), pwzData);
        ExitOnFailure1(hr, "failed to copy component name: %ls", pwzData);

        psf->isInstalled = isInstalled;
        psf->isAction = isAction;

        hr = WcaGetRecordString(hRec, fqWeb, &pwzData);
        ExitOnFailure(hr, "Failed to get Web for VirtualDir");

        if (*pwzData)
        {
            hr = ScaWebsGetBase(piMetabase, pswList, pwzData, psf->wzWebBase, countof(psf->wzWebBase), hWebBaseQuery);
            if (FAILED(hr) && WcaIsUninstalling(isInstalled, isAction))
            {
                // If we're uninstalling, don't bother finding the existing web, just leave the filter root empty
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to get base of web for Filter");

            if (0 != lstrlenW(psf->wzWebBase))
            {
                hr = ::StringCchPrintfW(psf->wzFilterRoot, countof(psf->wzFilterRoot), L"%s/Filters", psf->wzWebBase);
                ExitOnFailure(hr, "Failed to allocate filter base string");
            }
        }
        else
        {
            hr = ::StringCchCopyW(psf->wzFilterRoot, countof(psf->wzFilterRoot), L"/LM/W3SVC/Filters");
            ExitOnFailure(hr, "Failed to allocate global filter base string");
        }

        // filter key
        hr = WcaGetRecordString(hRec, fqFilter, &pwzData);
        ExitOnFailure(hr, "Failed to get Filter.Filter");
        hr = ::StringCchCopyW(psf->wzKey, countof(psf->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to filter object");

        // filter path
        hr = WcaGetRecordString(hRec, fqPath, &pwzData);
        ExitOnFailure(hr, "Failed to get Filter.Path");
        hr = ::StringCchCopyW(psf->wzPath, countof(psf->wzPath), pwzData);
        ExitOnFailure(hr, "Failed to copy path string to filter object");

        // filter description
        hr = WcaGetRecordString(hRec, fqDescription, &pwzData);
        ExitOnFailure(hr, "Failed to get Filter.Description");
        hr = ::StringCchCopyW(psf->wzDescription, countof(psf->wzDescription), pwzData);
        ExitOnFailure(hr, "Failed to copy description string to filter object");

        // filter flags
        hr = WcaGetRecordInteger(hRec, fqFlags, &psf->iFlags);
        ExitOnFailure(hr, "Failed to get Filter.Flags");

        // filter load order
        hr = WcaGetRecordInteger(hRec, fqLoadOrder, &psf->iLoadOrder);
        ExitOnFailure(hr, "Failed to get Filter.LoadOrder");
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing filters");

LExit:
    WcaFinishUnwrapQuery(hWrapQuery);

    ReleaseStr(pwzData);
    return hr;
}


HRESULT ScaFiltersInstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_FILTER* psfList
    )
{
    HRESULT hr = S_OK;
    SCA_FILTER* psf = psfList;
    LPCWSTR wzPreviousFilterRoot = NULL;
    LPWSTR pwzLoadOrder = NULL;

    while (psf)
    {
        if (WcaIsInstalling(psf->isInstalled, psf->isAction))
        {
            if (!wzPreviousFilterRoot || CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, wzPreviousFilterRoot, -1, psf->wzFilterRoot, -1))
            {
                if (pwzLoadOrder)
                {
                    hr = ScaWriteMetabaseValue(piMetabase, wzPreviousFilterRoot, L"", MD_FILTER_LOAD_ORDER, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)pwzLoadOrder);
                    ExitOnFailure(hr, "Failed to write filter load order to metabase");

                    ReleaseNullStr(pwzLoadOrder);
                }

                hr = ReadFilterLoadOrder(piMetabase, psf->wzFilterRoot, &pwzLoadOrder);
                ExitOnFailure(hr, "Failed to read filter load order.");

                wzPreviousFilterRoot = psf->wzFilterRoot;
            }

            hr = ScaCreateMetabaseKey(piMetabase, psf->wzFilterRoot, psf->wzKey);
            ExitOnFailure1(hr, "Failed to create key for filter '%ls'", psf->wzKey);

            hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsFilter");
            ExitOnFailure1(hr, "Failed to write key type for filter '%ls'", psf->wzKey);

            // filter path
            hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_FILTER_IMAGE_PATH, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)psf->wzPath);
            ExitOnFailure1(hr, "Failed to write Path for filter '%ls'", psf->wzKey);

            // filter description
            hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_FILTER_DESCRIPTION, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)psf->wzDescription);
            ExitOnFailure1(hr, "Failed to write Description for filter '%ls'", psf->wzKey);

            // filter flags
            if (MSI_NULL_INTEGER != psf->iFlags)
            {
                hr = ScaWriteMetabaseValue(piMetabase, psf->wzFilterRoot, psf->wzKey, MD_FILTER_FLAGS, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)psf->iFlags));
                ExitOnFailure1(hr, "Failed to write Flags for filter '%ls'", psf->wzKey);
            }

            // filter load order
            if (MSI_NULL_INTEGER != psf->iLoadOrder)
            {
                hr = AddFilterToLoadOrder(psf->wzKey, psf->iLoadOrder, &pwzLoadOrder);
                ExitOnFailure1(hr, "Failed to add filter '%ls' to load order.", psf->wzKey);
            }
        }

        psf = psf->psfNext;
    }

    if (pwzLoadOrder)
    {
        Assert(wzPreviousFilterRoot && *wzPreviousFilterRoot);

        hr = ScaWriteMetabaseValue(piMetabase, wzPreviousFilterRoot, L"", MD_FILTER_LOAD_ORDER, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)pwzLoadOrder);
        ExitOnFailure(hr, "Failed to write filter load order to metabase");
    }

LExit:
    ReleaseStr(pwzLoadOrder);
    return hr;
}


HRESULT ScaFiltersUninstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_FILTER* psfList
    )
{
    HRESULT hr = S_OK;
    SCA_FILTER* psf = psfList;
    LPCWSTR wzPreviousFilterRoot = NULL;
    LPWSTR pwzLoadOrder = NULL;

    while (psf)
    {
        if (WcaIsUninstalling(psf->isInstalled, psf->isAction))
        {
            if (!wzPreviousFilterRoot || CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, wzPreviousFilterRoot, -1, psf->wzFilterRoot, -1))
            {
                if (pwzLoadOrder)
                {
                    hr = ScaWriteMetabaseValue(piMetabase, wzPreviousFilterRoot, L"", MD_FILTER_LOAD_ORDER, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)pwzLoadOrder);
                    ExitOnFailure(hr, "Failed to write filter load order to metabase");

                    ReleaseNullStr(pwzLoadOrder);
                }

                hr = ReadFilterLoadOrder(piMetabase, psf->wzFilterRoot, &pwzLoadOrder);
                ExitOnFailure(hr, "Failed to read filter load order.");

                wzPreviousFilterRoot = psf->wzFilterRoot;
            }

            hr = RemoveFilterFromLoadOrder(psf->wzKey, &pwzLoadOrder);
            ExitOnFailure1(hr, "Failed to remove filter '%ls' from load order", psf->wzKey);

            // remove the filter from the load order and remove the filter's key
            if (0 != lstrlenW(psf->wzFilterRoot))
            {
                hr = ScaDeleteMetabaseKey(piMetabase, psf->wzFilterRoot, psf->wzKey);
                ExitOnFailure1(hr, "Failed to remove web '%ls' from metabase", psf->wzKey);
            }
        }

        psf = psf->psfNext;
    }

    if (pwzLoadOrder)
    {
        Assert(wzPreviousFilterRoot && *wzPreviousFilterRoot);

        hr = ScaWriteMetabaseValue(piMetabase, wzPreviousFilterRoot, L"", MD_FILTER_LOAD_ORDER, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)pwzLoadOrder);
        ExitOnFailure(hr, "Failed to write filter load order to metabase");
    }

LExit:
    return hr;
}


void ScaFiltersFreeList(
    __in SCA_FILTER* psfList
    )
{
    SCA_FILTER* psfDelete = psfList;
    while (psfList)
    {
        psfDelete = psfList;
        psfList = psfList->psfNext;

        MemFree(psfDelete);
    }
}

HRESULT AddFilterToList(
    __inout SCA_FILTER** ppsfList)
{
    HRESULT hr = S_OK;
    SCA_FILTER* psf = static_cast<SCA_FILTER*>(MemAlloc(sizeof(SCA_FILTER), TRUE));
    ExitOnNull(psf, hr, E_OUTOFMEMORY, "failed to add filter to filter list");

    psf->psfNext = *ppsfList;
    *ppsfList = psf;

LExit:
    return hr;
}

// private helper functions


static HRESULT ReadFilterLoadOrder(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzFilterRoot,
    __out LPWSTR *ppwzLoadOrder
    )
{
    HRESULT hr = S_OK;
    METADATA_HANDLE mhRoot = NULL;

    METADATA_RECORD mr;
    DWORD dwRequired = 0;
    DWORD cchData = 255;

    ::ZeroMemory(&mr, sizeof(mr));
    mr.dwMDIdentifier = MD_FILTER_LOAD_ORDER;
    mr.dwMDAttributes = METADATA_NO_ATTRIBUTES;
    mr.dwMDUserType = IIS_MD_UT_SERVER;
    mr.dwMDDataType = ALL_METADATA;
    mr.dwMDDataLen = cchData;
    mr.pbMDData = static_cast<BYTE*>(MemAlloc(mr.dwMDDataLen * sizeof(WCHAR), TRUE));
    ExitOnNull(mr.pbMDData, hr, E_OUTOFMEMORY, "failed to allocate memory for MDData in metadata record");

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, wzFilterRoot, METADATA_PERMISSION_READ | METADATA_PERMISSION_WRITE, 10, &mhRoot);
    for (int i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "Failed to open root key, retrying %d time(s)...", i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, wzFilterRoot, METADATA_PERMISSION_READ | METADATA_PERMISSION_WRITE, 10, &mhRoot);
    }

    if (SUCCEEDED(hr))
    {
        hr = piMetabase->GetData(mhRoot, L"", &mr, &dwRequired);
        if (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) == hr)
        {
            mr.dwMDDataLen = cchData = dwRequired;

            LPVOID pv = MemReAlloc(mr.pbMDData, mr.dwMDDataLen * sizeof(WCHAR), TRUE);
            ExitOnNull(pv, hr, E_OUTOFMEMORY, "failed to allocate memory for MDData in metadata record");
            mr.pbMDData = static_cast<BYTE*>(pv);

            hr = piMetabase->GetData(mhRoot, L"", &mr, &dwRequired);
        }
    }

    // The /Filters node or /Filters/FilterLoadOrder property might not exist (yet).
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || MD_ERROR_DATA_NOT_FOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get filter load order.");

    hr = StrAllocString(ppwzLoadOrder, reinterpret_cast<LPCWSTR>(mr.pbMDData), 0);
    ExitOnFailure(hr, "Failed to allocate string for filter load order.");

LExit:
    ReleaseMem(mr.pbMDData);

    if (mhRoot)
    {
        piMetabase->CloseKey(mhRoot);
    }

    return hr;
}


static HRESULT AddFilterToLoadOrder(
    __in LPCWSTR wzFilter,
    __in int iLoadOrder,
    __inout LPWSTR *ppwzLoadOrder
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzLoadOrder = *ppwzLoadOrder;
    int cchFilter = lstrlenW(wzFilter);
    LPWSTR pwzTemp = NULL;

    // If the filter name ends with '\0' or ',' and
    // the filter name begins at the beginning of the list or with ','
    // Then we've found the exact filter by name.
    //
    // If the filter isn't already in the load order, add it
    if (wzLoadOrder && *wzLoadOrder)
    {
        LPCWSTR pwz = wcsstr(wzLoadOrder, wzFilter);

        if (NULL != pwz &&
            (L'\0' == *(pwz + cchFilter) || L',' == *(pwz + cchFilter)) &&
            (pwz == wzLoadOrder || L',' == *(pwz - 1)))
        {
            // Filter already in the load order, no work to do.
        }
        else
        {
            pwz = NULL;
            if (0 <= iLoadOrder)
            {
                pwz = wzLoadOrder;
                for (int i = 0; i < iLoadOrder && pwz; ++i)
                {
                    pwz = wcsstr(pwz, L",");
                }
            }

            if (NULL == pwz) // put the filter at the end of the order
            {
                Assert(wzLoadOrder && *wzLoadOrder);

                // tack on a comma since there are other filters in the order
                hr = StrAllocConcat(ppwzLoadOrder, L",", 1);
                ExitOnFailure(hr, "Failed to append a comma to filter load order.");

                hr = StrAllocConcat(ppwzLoadOrder, wzFilter, cchFilter);
                ExitOnFailure(hr, "Failed to append the filter on to the load order.");
            }
            else if (L',' == *pwz) // put the filter in the middle of the order
            {
                hr = StrAllocString(&pwzTemp, wzLoadOrder, pwz - wzLoadOrder + 1);
                ExitOnFailure(hr, "Failed to copy first half of filter load order to temp string.");

                hr = StrAllocConcat(&pwzTemp, wzFilter, 0);
                ExitOnFailure(hr, "Failed to copy filter into load order.");

                hr = StrAllocConcat(&pwzTemp, pwz, 0);
                ExitOnFailure(hr, "Failed to copy remaining filter load order back onto load order.");

                hr = StrAllocString(ppwzLoadOrder, pwzTemp, 0);
                ExitOnFailure(hr, "Failed to copy temp string to load order string.");
            }
            else // put the filter at the beginning of the order
            {
                hr = StrAllocPrefix(ppwzLoadOrder, L",", 1);
                ExitOnFailure(hr, "Failed to prepend a comma to filter load order.");

                hr = StrAllocPrefix(ppwzLoadOrder, wzFilter, cchFilter);
                ExitOnFailure(hr, "Failed to prepend the filter on to the load order.");
            }
        }
    }
    else
    {
        hr = StrAllocString(ppwzLoadOrder, wzFilter, cchFilter);
        ExitOnFailure(hr, "Failed to add filter to load order.");
    }

LExit:
    ReleaseStr(pwzTemp);
    return hr;
}


static HRESULT RemoveFilterFromLoadOrder(
    __in LPCWSTR wzFilter,
    __inout LPWSTR *ppwzLoadOrder
    )
{
    HRESULT hr = S_OK;
    int cchFilter = lstrlenW(wzFilter);

    LPCWSTR pwzStart = NULL;
    LPWSTR pwzFind = NULL;

    pwzStart = pwzFind = *ppwzLoadOrder;
    while (NULL != (pwzFind = wcsstr(pwzFind, wzFilter)))
    {
        // Make sure to only match [wzFilter] and NOT foo[wzFilter]bar
        if (pwzFind == pwzStart || L',' == *(pwzFind - 1))
        {
            int cchRemainder = lstrlenW(pwzFind) - cchFilter + 1; // add one to include the null terminator

            if (L'\0' == *(pwzFind + cchFilter))
            {
                if (pwzFind == pwzStart)
                {
                    memmove(pwzFind, pwzFind + cchFilter, cchRemainder * sizeof(WCHAR)); // copy down the null terminator
                }
                else
                {
                    memmove(pwzFind - 1, pwzFind + cchFilter, cchRemainder * sizeof(WCHAR)); // copy down the null terminator over top the trailing ","
                }
            }
            else if (L',' == *(pwzFind + cchFilter))
            {
                memmove(pwzFind, pwzFind + cchFilter + 1, (cchRemainder - 1) * sizeof(WCHAR)); // skip copying the ","
            }
            else // skip past the partial match
            {
                pwzFind = pwzFind + cchFilter;
            }
        }
        else // skip past the partial match
        {
            pwzFind = pwzFind + cchFilter;
        }
    }

//LExit:
    return hr;
}
