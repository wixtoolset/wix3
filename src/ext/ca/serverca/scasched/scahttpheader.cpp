//-------------------------------------------------------------------------------------------------
// <copyright file="scahttpheader.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS HTTP Header functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

enum eHttpHeaderQuery { hhqName = 1, hhqParentType, hhqParentValue, hhqValue, hhqAttributes};

static HRESULT AddHttpHeaderToList(
    __in SCA_HTTP_HEADER** ppshhList
    );


void ScaHttpHeaderFreeList(
    __in SCA_HTTP_HEADER* pshhList
    )
{
    SCA_HTTP_HEADER* pshhDelete = pshhList;
    while (pshhList)
    {
        pshhDelete = pshhList;
        pshhList = pshhList->pshhNext;

        MemFree(pshhDelete);
    }
}


HRESULT ScaHttpHeaderRead(
    __in SCA_HTTP_HEADER** ppshhList,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    Assert(ppshhList);

    HRESULT hr = S_OK;
        MSIHANDLE hRec;
    LPWSTR pwzData = NULL;
    SCA_HTTP_HEADER* pshh = NULL;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaAppPoolRead");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaHttpHeaderRead() - required tables not present.");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the HTTP headers
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        hr = AddHttpHeaderToList(ppshhList);
        ExitOnFailure(hr, "failed to add http header to list");

        pshh = *ppshhList;

        hr = WcaGetRecordInteger(hRec, hhqParentType, &(pshh->iParentType));
        ExitOnFailure(hr, "failed to get IIsHttpHeader.ParentType");

        hr = WcaGetRecordString(hRec, hhqParentValue, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsHttpHeader.ParentValue");
        hr = ::StringCchCopyW(pshh->wzParentValue, countof(pshh->wzParentValue), pwzData);
        ExitOnFailure(hr, "Failed to copy IIsHttpHeader.ParentValue");

        hr = WcaGetRecordString(hRec, hhqName, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsHttpHeader.Name");
        hr = ::StringCchCopyW(pshh->wzName, countof(pshh->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy IIsHttpHeader.Name");

        hr = WcaGetRecordString(hRec, hhqValue, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsHttpHeader.Value");
        hr = ::StringCchCopyW(pshh->wzValue, countof(pshh->wzValue), pwzData);
        ExitOnFailure(hr, "Failed to copy IIsHttpHeader.Value");

        hr = WcaGetRecordInteger(hRec, hhqAttributes, &(pshh->iAttributes));
        ExitOnFailure(hr, "failed to get IIsHttpHeader.Attributes");
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing web errors");

LExit:
    WcaFinishUnwrapQuery(hWrapQuery);

    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaGetHttpHeader(
    __in int iParentType,
    __in LPCWSTR wzParentValue,
    __in SCA_HTTP_HEADER** ppshhList,
    __out SCA_HTTP_HEADER** ppshhOut
    )
{
    HRESULT hr = S_OK;
    SCA_HTTP_HEADER* pshhAdd = NULL;
    SCA_HTTP_HEADER* pshhLast = NULL;

    *ppshhOut = NULL;

    if (!*ppshhList)
    {
        return hr;
    }

    SCA_HTTP_HEADER* pshh = *ppshhList;
    while (pshh)
    {
        if (iParentType == pshh->iParentType && CSTR_EQUAL == ::CompareStringW(LOCALE_SYSTEM_DEFAULT, 0, wzParentValue, -1, pshh->wzParentValue, -1))
        {
            // Found a match, take this one out of the list and add it to the matched out list
            pshhAdd = pshh;

            if (pshhLast)
            {
                // If we're not at the beginning of the list tell the last node about it's new next (since we're taking away it's current next)
                pshhLast->pshhNext = pshhAdd->pshhNext;
            }
            else
            {
                // If we are at the beginning (no pshhLast) update the beginning (since we're taking it)
                *ppshhList = pshh->pshhNext;
            }
            pshh = pshh->pshhNext; // move on

            // Add the one we've removed to the beginning of the out list
            pshhAdd->pshhNext = *ppshhOut;
            *ppshhOut = pshhAdd;
        }
        else
        {
            pshhLast = pshh; // remember the last we that didn't match
            pshh = pshh->pshhNext; // move on
        }
    }

    return hr;
}


HRESULT ScaWriteHttpHeader(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzRoot,
    __in SCA_HTTP_HEADER* pshhList
    )
{
    Assert(piMetabase && pshhList);

    HRESULT hr = S_OK;
    METADATA_RECORD mr = { 0 };
    DWORD cchData = 0;
    LPWSTR pwzSearchKey = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzHeaders = NULL;
    LPWSTR pwzNewHeader = NULL;
    DWORD dwFoundHeaderIndex = 0;
    LPCWSTR wzFoundHeader = NULL;
    BOOL fOldValueFound = FALSE;

    ExitOnNull(wzRoot, hr, E_INVALIDARG, "Failed to write HTTP header, because no root was provided");

    Assert(*wzRoot);

    // Check if HTTP header already exist here.
    mr.dwMDIdentifier = MD_HTTP_CUSTOM;
    mr.dwMDAttributes = METADATA_INHERIT;
    mr.dwMDUserType = IIS_MD_UT_SERVER;
    mr.dwMDDataType = ALL_METADATA;
    mr.dwMDDataLen = cchData = 0;
    mr.pbMDData = NULL;

    hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzRoot, &mr);
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || MD_ERROR_DATA_NOT_FOUND == hr)
    {
        //
        // If we don't have any HTTP Headers already, move up to get the parent headers.
        // TODO: Make it configurable to not inherit HTTP Headers
        //
        hr = StrAllocConcat(&pwzSearchKey, wzRoot, 0);
        ExitOnFailure1(hr, "Failed to copy root string: %ls", wzRoot);

        pwz = pwzSearchKey + lstrlenW(pwzSearchKey);
        while (NULL == pwzHeaders)
        {
            // find the last slash
            while (*pwz != '/' && pwz != pwzSearchKey)
            {
                --pwz;
            }

            if (pwz == pwzSearchKey)
            {
                break;
            }

            *pwz = L'\0';

            // Try here.  If it's not found, keep walking up the path
            hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, pwzSearchKey, &mr);
            if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || MD_ERROR_DATA_NOT_FOUND == hr)
            {
                hr = S_FALSE;
            }
            ExitOnFailure1(hr, "Failed to find search for HTTP headers for web root: %ls while walking up the tree", wzRoot);

            if (S_OK == hr)
            {
                hr = StrAllocString(&pwzHeaders, reinterpret_cast<LPWSTR>(mr.pbMDData), 0);
                ExitOnFailure(hr, "Failed to allocate parent HTTP header string");
                break;
            }
        }
    }
    else
    {
        hr = StrAllocString(&pwzHeaders, reinterpret_cast<LPWSTR>(mr.pbMDData), 0);
        ExitOnFailure(hr, "Failed to allocate HTTP header string");
    }
    ExitOnFailure1(hr, "Failed while searching for default HTTP headers to start with for web root: %ls", wzRoot);

    // Loop through the HTTP headers
    for (SCA_HTTP_HEADER* pshh = pshhList; pshh; pshh = pshh->pshhNext)
    {
        fOldValueFound = FALSE; // assume a HTTP Header match will not be found

        hr = StrAllocFormatted(&pwzNewHeader, L"%s: ", pshh->wzName);
        ExitOnFailure(hr, "Failed to allocate header name");

        if (NULL != pwzHeaders && *pwzHeaders)
        {
            // Try to find a matching header already in the list
            hr = MultiSzFindSubstring(pwzHeaders, pwzNewHeader, &dwFoundHeaderIndex, &wzFoundHeader);
            ExitOnFailure(hr, "Failed while searching for existing HTTP header.");

            // If there was a substring HTTP header match, make sure the match was at the beginning 
            // of the string because that is the HTTP header name.
            if (S_OK == hr)
            {
                DWORD cchMatch = lstrlenW(pwzNewHeader);
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_SYSTEM_DEFAULT, NORM_IGNORECASE, pwzNewHeader, cchMatch, wzFoundHeader, cchMatch))
                {
                    fOldValueFound = TRUE;
                    break;
                }
            }
        }

        // Add the value on to the header name now.
        hr = StrAllocConcat(&pwzNewHeader, pshh->wzValue, 0);
        ExitOnFailure(hr, "Failed to add value on to HTTP header name.");

        // If we have something to replace, replace it, otherwise, put it at the beginning (order shouldn't matter)
        if (fOldValueFound)
        {
            if (NULL == pwzHeaders)
            {
                ExitOnFailure(hr = E_INVALIDARG, "While attempting to replace old HTTP header with new HTTP header, it was discovered that the old HTTP header was NULL!");
            }
            hr = MultiSzReplaceString(&pwzHeaders, dwFoundHeaderIndex, pwzNewHeader);
            ExitOnFailure(hr, "Failed to replace old HTTP header with new HTTP header");
        }
        else
        {
            hr = MultiSzPrepend(&pwzHeaders, NULL, pwzNewHeader);
            ExitOnFailure(hr, "Failed to prepend new HTTP header");
        }
    }

    // now write the HttpCustom to the metabase
    hr = ScaWriteMetabaseValue(piMetabase, wzRoot, NULL, MD_HTTP_CUSTOM, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, pwzHeaders);
    ExitOnFailure(hr, "Failed to write HTTP Headers to metabase");

LExit:
    MetaFreeValue(&mr);

    ReleaseStr(pwzNewHeader);
    ReleaseStr(pwzHeaders);
    ReleaseStr(pwzSearchKey);

    return hr;
}


HRESULT ScaHttpHeaderCheckList(
    __in SCA_HTTP_HEADER* pshhList
    )
{
    if (!pshhList)
    {
        return S_OK;
    }

    while (pshhList)
    {
        WcaLog(LOGMSG_STANDARD, "Http Header: %ls for parent: %ls not used!", pshhList->wzName, pshhList->wzParentValue);
        pshhList = pshhList->pshhNext;
    }

    return E_FAIL;
}


static HRESULT AddHttpHeaderToList(
    __in SCA_HTTP_HEADER** ppshhList
    )
{
    HRESULT hr = S_OK;

    SCA_HTTP_HEADER* pshh = static_cast<SCA_HTTP_HEADER*>(MemAlloc(sizeof(SCA_HTTP_HEADER), TRUE));
    ExitOnNull(pshh, hr, E_OUTOFMEMORY, "failed to allocate memory for new http header list element");

    pshh->pshhNext = *ppshhList;
    *ppshhList = pshh;

LExit:
    return hr;
}
