// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

enum eWebErrorQuery { weqErrorCode = 1, weqSubCode, weqParentType, weqParentValue, weqFile, weqURL };

static HRESULT AddWebErrorToList(SCA_WEB_ERROR** ppsweList);

void ScaWebErrorFreeList(SCA_WEB_ERROR *psweList)
{
    SCA_WEB_ERROR *psweDelete = psweList;
    while (psweList)
    {
        psweDelete = psweList;
        psweList = psweList->psweNext;

        MemFree(psweDelete);
    }
}

HRESULT ScaWebErrorRead(
                        SCA_WEB_ERROR **ppsweList,
                        __inout LPWSTR *ppwzCustomActionData
                        )
{
//    AssertSz(0, "Debug ScaWebErrorRead here");
    HRESULT hr = S_OK;
    MSIHANDLE hRec;
    LPWSTR pwzData = NULL;
    SCA_WEB_ERROR* pswe;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    ExitOnNull(ppsweList, hr, E_INVALIDARG, "Failed to read web error, because no web error was provided to read");

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaAppPoolRead");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaWebErrorRead() - required tables not present.");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the web errors
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        hr = AddWebErrorToList(ppsweList);
        ExitOnFailure(hr, "failed to add web error to list");

        pswe = *ppsweList;

        hr = WcaGetRecordInteger(hRec, weqErrorCode, &(pswe->iErrorCode));
        ExitOnFailure(hr, "failed to get IIsWebError.ErrorCode");

        hr = WcaGetRecordInteger(hRec, weqSubCode, &(pswe->iSubCode));
        ExitOnFailure(hr, "failed to get IIsWebError.SubCode");

        hr = WcaGetRecordInteger(hRec, weqParentType, &(pswe->iParentType));
        ExitOnFailure(hr, "failed to get IIsWebError.ParentType");

        hr = WcaGetRecordString(hRec, weqParentValue, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebError.ParentValue");
        hr = ::StringCchCopyW(pswe->wzParentValue, countof(pswe->wzParentValue), pwzData);
        ExitOnFailure(hr, "Failed to copy IIsWebError.ParentValue");

        hr = WcaGetRecordString(hRec, weqFile, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebError.File");
        hr = ::StringCchCopyW(pswe->wzFile, countof(pswe->wzFile), pwzData);
        ExitOnFailure(hr, "Failed to copy IIsWebError.File");

        hr = WcaGetRecordString(hRec, weqURL, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebError.URL");
        hr = ::StringCchCopyW(pswe->wzURL, countof(pswe->wzURL), pwzData);
        ExitOnFailure(hr, "Failed to copy IIsWebError.URL");

        // If they've specified both a file and a URL, that's invalid
        if (*(pswe->wzFile) && *(pswe->wzURL))
            ExitOnFailure2(hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA), "Both File and URL specified for web error.  File: %ls, URL: %ls", pswe->wzFile, pswe->wzURL);
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

HRESULT ScaGetWebError(int iParentType, LPCWSTR wzParentValue, SCA_WEB_ERROR **ppsweList, SCA_WEB_ERROR **ppsweOut)
{
    HRESULT hr = S_OK;
    SCA_WEB_ERROR* psweAdd = NULL;
    SCA_WEB_ERROR* psweLast = NULL;

    *ppsweOut = NULL;
    
    if (!*ppsweList)
        return hr;

    SCA_WEB_ERROR* pswe = *ppsweList;
    while (pswe)
    {
        if (iParentType == pswe->iParentType && 0 == lstrcmpW(wzParentValue, pswe->wzParentValue))
        {
            // Found a match, take this one out of the list and add it to the matched out list
            psweAdd = pswe;

            if (psweLast)
            {
                // If we're not at the beginning of the list tell the last node about it's new next (since we're taking away it's current next)
                psweLast->psweNext = psweAdd->psweNext;
            }
            else
            {
                // If we are at the beginning (no psweLast) update the beginning (since we're taking it)
                *ppsweList = pswe->psweNext;
            }
            pswe = pswe->psweNext; // move on

            // Add the one we've removed to the beginning of the out list
            psweAdd->psweNext = *ppsweOut;
            *ppsweOut = psweAdd;
        }
        else
        {
            psweLast = pswe; // remember the last we that didn't match
            pswe = pswe->psweNext; // move on
        }
    }

    return hr;
}

HRESULT ScaWriteWebError(IMSAdminBase* piMetabase, int iParentType, LPCWSTR wzRoot, SCA_WEB_ERROR* psweList)
{
//    AssertSz(0, "Debug ScaWriteWebError here");
    Assert(*wzRoot && psweList);

    HRESULT hr = S_OK;

    DWORD cchData = 0;
    LPWSTR pwzSearchKey = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzErrors = NULL;

    LPWSTR pwzCodeSubCode = NULL;
    LPWSTR pwzAcceptableCodeSubCode = NULL;
    LPCWSTR wzFoundCodeSubCode = NULL;
    DWORD_PTR dwFoundCodeSubCodeIndex = 0xFFFFFFFF;
    BOOL fOldValueFound = FALSE;
    LPWSTR pwzAcceptableErrors = NULL;

    LPWSTR pwzNewError = NULL;

    METADATA_RECORD mr;
    ::ZeroMemory(&mr, sizeof(mr));

    ExitOnNull(piMetabase, hr, E_INVALIDARG, "Failed to write web error, because no metabase was provided");
    ExitOnNull(wzRoot, hr, E_INVALIDARG, "Failed to write web error, because no root was provided");

    // get the set of all valid custom errors from the metabase
    mr.dwMDIdentifier = MD_CUSTOM_ERROR_DESC;
    mr.dwMDAttributes = METADATA_INHERIT;
    mr.dwMDUserType = IIS_MD_UT_SERVER;
    mr.dwMDDataType = ALL_METADATA;
    mr.dwMDDataLen = cchData = 0;
    mr.pbMDData = NULL;

    hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC/Info", &mr);
    ExitOnFailure(hr, "Unable to get set of acceptable error codes for this server.");

    pwzAcceptableErrors = reinterpret_cast<LPWSTR>(mr.pbMDData);

    // Check if web errors already exist here
    mr.dwMDIdentifier = MD_CUSTOM_ERROR;
    mr.dwMDAttributes = METADATA_INHERIT;
    mr.dwMDUserType = IIS_MD_UT_SERVER;
    mr.dwMDDataType = ALL_METADATA;
    mr.dwMDDataLen = cchData = 0;
    mr.pbMDData = NULL;

    hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzRoot, &mr);
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || MD_ERROR_DATA_NOT_FOUND == hr)
    {
        //
        // If we don't have one already, find an appropriate one to start with
        //

        // we can walk up key by key and look for custom errors to inherit

        hr = StrAllocConcat(&pwzSearchKey, wzRoot, 0);
        ExitOnFailure1(hr, "Failed to copy root string: %ls", wzRoot);

        pwz = pwzSearchKey + lstrlenW(pwzSearchKey);

        while (NULL == pwzErrors)
        {
            // find the last slash
            while (*pwz != '/' && pwz != pwzSearchKey)
                pwz --;

            if (pwz == pwzSearchKey)
                break;

            *pwz = L'\0';

            // Try here.  If it's not found, keep walking up the path
            hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, pwzSearchKey, &mr);
            if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || MD_ERROR_DATA_NOT_FOUND == hr)
                hr = S_FALSE;
            ExitOnFailure1(hr, "failed to discover default error values to start with for web root: %ls while walking up the tree", wzRoot);

            if (S_OK == hr)
            {
                pwzErrors = reinterpret_cast<LPWSTR>(mr.pbMDData);
                break;
            }

            // Don't keep going if we're at the root
            if (0 == lstrcmpW(pwz + 1, L"W3SVC"))
                break;
        }
    }
    else
    {
        pwzErrors = reinterpret_cast<LPWSTR>(mr.pbMDData);
    }
    ExitOnFailure1(hr, "failed to discover default error values to start with for web root: %ls", wzRoot);

    // The above code should have come up with some value to start pwzErrors off with.  Make sure it did.
    if (NULL == pwzErrors)
    {
        ExitOnFailure1(hr = E_UNEXPECTED, "failed to discover default error values to start with for web root: %ls", wzRoot);
    }

    // Loop through the web errors
    for (SCA_WEB_ERROR* pswe = psweList; pswe; pswe = pswe->psweNext)
    {
        // Assume that we will have to replace 
        fOldValueFound = TRUE;

        // If the subcode is 0, that means "*" in MD_CUSTOM_ERROR (thus the special formatting logic)
        if (0 == pswe->iSubCode)
        {
            hr = StrAllocFormatted(&pwzCodeSubCode, L"%d,*", pswe->iErrorCode);
            ExitOnFailure(hr, "failed to create error code string while installing web error");
        }
        else
        {
            hr = StrAllocFormatted(&pwzCodeSubCode, L"%d,%d", pswe->iErrorCode, pswe->iSubCode);
            ExitOnFailure(hr, "failed to create error code,subcode string while installing web error");
        }

        hr = MultiSzFindSubstring(pwzErrors, pwzCodeSubCode, &dwFoundCodeSubCodeIndex, &wzFoundCodeSubCode);
        ExitOnFailure1(hr, "failed to find existing error code,subcode: %ls", pwzCodeSubCode);

        // If we didn't find this error code/sub code pair in the list already, make sure it's acceptable to add
        if (S_FALSE == hr)
        {
            //
            // Make sure this error code/sub code pair is in the "acceptable" list
            //

            // If the subcode is 0, that means "0" in MD_CUSTOM_ERROR_DESC (no special formatting logic needed)
            hr = StrAllocFormatted(&pwzAcceptableCodeSubCode, L"%d,%d", pswe->iErrorCode, pswe->iSubCode);
            ExitOnFailure(hr, "failed to create error code,subcode string while installing web error");

            // We don't care where it is, just whether it's there or not
            hr = MultiSzFindSubstring(pwzAcceptableErrors, pwzAcceptableCodeSubCode, NULL, NULL);
            ExitOnFailure1(hr, "failed to find whether or not error code, subcode: %ls is supported", pwzCodeSubCode);

            if (S_FALSE == hr)
            {
                WcaLog(LOGMSG_VERBOSE, "Skipping error code, subcode: %ls because it is not supported by the server.", pwzCodeSubCode);
                continue;
            }

            // If we didn't find it (and its an acceptable error) then we have nothing to replace
            fOldValueFound = FALSE;
        }

        // Set up the new error string if needed
        if (*(pswe->wzFile))
        {
            hr = StrAllocFormatted(&pwzNewError, L"%s,FILE,%s", pwzCodeSubCode, pswe->wzFile);
            ExitOnFailure2(hr, "failed to create new error code string with code,subcode: %ls, file: %ls", pwzCodeSubCode, pswe->wzFile);
        }
        else if (*(pswe->wzURL))
        {
            hr = StrAllocFormatted(&pwzNewError, L"%s,URL,%s", pwzCodeSubCode, pswe->wzURL);
            ExitOnFailure2(hr, "failed to create new error code string with code,subcode: %ls, file: %ls", pwzCodeSubCode, pswe->wzFile);
        }
        else if (fOldValueFound)
        {
            // If no File or URL was specified, they want a default error so remove the old value from the MULTISZ and move on
            hr = MultiSzRemoveString(&pwzErrors, dwFoundCodeSubCodeIndex);
            ExitOnFailure1(hr, "failed to remove string for error code sub code: %ls in order to make it 'default'", pwzCodeSubCode);
            continue;
        }

        // If we have something to replace, replace it, otherwise, put it at the beginning (order shouldn't matter)
        if (fOldValueFound)
        {
            hr = MultiSzReplaceString(&pwzErrors, dwFoundCodeSubCodeIndex, pwzNewError);
            ExitOnFailure1(hr, "failed to replace old error string with new error string for error code,subcode: %ls", pwzCodeSubCode);
        }
        else
        {
            hr = MultiSzPrepend(&pwzErrors, NULL, pwzNewError);
            ExitOnFailure1(hr, "failed to prepend new error string for error code,subcode: %ls", pwzCodeSubCode);
        }
    }

    // now write the CustomErrors to the metabase
    if (weptWeb == iParentType)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRoot, L"/Root", MD_CUSTOM_ERROR, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, pwzErrors);
        ExitOnFailure(hr, "Failed to write Web Error to /Root");
    }
    else
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRoot, NULL, MD_CUSTOM_ERROR, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, pwzErrors);
        ExitOnFailure(hr, "Failed to write Web Error");
    }

LExit:
    ReleaseStr(pwzErrors);
    ReleaseStr(pwzSearchKey);
    ReleaseStr(pwzCodeSubCode);
    ReleaseStr(pwzAcceptableCodeSubCode);
    ReleaseStr(pwzAcceptableErrors);

    return hr;
}

static HRESULT AddWebErrorToList(SCA_WEB_ERROR** ppsweList)
{
    HRESULT hr = S_OK;

    SCA_WEB_ERROR* pswe = static_cast<SCA_WEB_ERROR*>(MemAlloc(sizeof(SCA_WEB_ERROR), TRUE));
    ExitOnNull(pswe, hr, E_OUTOFMEMORY, "failed to allocate memory for new web error list element");

    pswe->psweNext = *ppsweList;
    *ppsweList = pswe;

LExit:
    return hr;
}

HRESULT ScaWebErrorCheckList(SCA_WEB_ERROR* psweList)
{
    if (!psweList)
    {
        return S_OK;
    }
    
    while (psweList)
    {
        WcaLog(LOGMSG_STANDARD, "WebError code: %d subcode: %d for parent: %ls not used!", psweList->iErrorCode, psweList->iSubCode, psweList->wzParentValue);
        psweList = psweList->psweNext;
    }

    return E_FAIL;
}

