//-------------------------------------------------------------------------------------------------
// <copyright file="scamimemap.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Mime Map functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

enum eMimeMapQuery { mmqMimeMap = 1, mmqParentType, mmqParentValue, 
                        mmqMimeType, mmqExtension};

// prototypes
static HRESULT AddMimeMapToList(SCA_MIMEMAP** ppsmmList);


void ScaMimeMapFreeList(SCA_MIMEMAP* psmmList)
{
    SCA_MIMEMAP* psmmDelete = psmmList;
    while (psmmList)
    {
        psmmDelete = psmmList;
        psmmList = psmmList->psmmNext;

        MemFree(psmmDelete);
    }
}


HRESULT __stdcall ScaMimeMapRead(
    SCA_MIMEMAP** ppsmmList,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec;
    LPWSTR pwzData = NULL;
    SCA_MIMEMAP* psmm;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaMimeMapRead");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaMimeMapRead() - required table not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the mimemappings
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        hr = AddMimeMapToList(ppsmmList);
        ExitOnFailure(hr, "failed to add mime map to list");

        psmm = *ppsmmList;

        hr = WcaGetRecordString(hRec, mmqMimeMap, &pwzData);
        ExitOnFailure(hr, "Failed to get MimeMap.MimeMap");
        hr = ::StringCchCopyW(psmm->wzMimeMap, countof(psmm->wzMimeMap), pwzData);
        ExitOnFailure(hr, "Failed to copy mimemap string to mimemap object");

        hr = WcaGetRecordInteger(hRec, mmqParentType, &psmm->iParentType);
        ExitOnFailure(hr, "Failed to get MimeMap.iParentType");
    
        hr = WcaGetRecordString(hRec, mmqParentValue, &pwzData);
        ExitOnFailure(hr, "Failed to get MimeMap.ParentValue");
        hr = ::StringCchCopyW(psmm->wzParentValue, countof(psmm->wzParentValue), pwzData);
        ExitOnFailure(hr, "Failed to copy parent value string to mimemap object");

        hr = WcaGetRecordString(hRec, mmqExtension, &pwzData);
        ExitOnFailure(hr, "Failed to get MimeMap.Extension");
        hr = ::StringCchCopyW(psmm->wzExtension, countof(psmm->wzExtension), pwzData);
        ExitOnFailure(hr, "Failed to copy extension string to mimemap object");

        hr = WcaGetRecordString(hRec, mmqMimeType, &pwzData);
        ExitOnFailure(hr, "Failed to get MimeMap.MimeType");
        hr = ::StringCchCopyW(psmm->wzMimeType, countof(psmm->wzMimeType), pwzData);
        ExitOnFailure(hr, "Failed to copy mimetype string to mimemap object");
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;
    ExitOnFailure(hr, "Failure while processing mimemappings");

LExit:
    WcaFinishUnwrapQuery(hWrapQuery);

    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaGetMimeMap(int iParentType, LPCWSTR wzParentValue, SCA_MIMEMAP **ppsmmList, SCA_MIMEMAP **ppsmmOut)
{
    HRESULT hr = S_OK;
    SCA_MIMEMAP* psmmAdd = NULL;
    SCA_MIMEMAP* psmmLast = NULL;

    *ppsmmOut = NULL;
    
    if (!*ppsmmList)
        return hr;

    SCA_MIMEMAP* psmm = *ppsmmList;
    while (psmm)
    {
        if (iParentType == psmm->iParentType && 0 == lstrcmpW(wzParentValue, psmm->wzParentValue))
        {
            // Found a match, take this one out of the list and add it to the matched out list
            psmmAdd = psmm;

            if (psmmLast)
            {
                // If we're not at the beginning of the list tell the last node about it's new next (since we're taking away it's current next)
                psmmLast->psmmNext = psmmAdd->psmmNext;
            }
            else
            {
                // If we are at the beginning (no psmmLast) update the beginning (since we're taking it)
                *ppsmmList = psmm->psmmNext;
            }
            psmm = psmm->psmmNext; // move on

            // Add the one we've removed to the beginning of the out list
            psmmAdd->psmmNext = *ppsmmOut;
            *ppsmmOut = psmmAdd;
        }
        else
        {
            psmmLast = psmm; // remember the last we that didn't match
            psmm = psmm->psmmNext; // move on
        }
    }

    return hr;
}

HRESULT ScaMimeMapCheckList(SCA_MIMEMAP* psmmList)
{
    if (!psmmList)
        return S_OK;
    
    while (psmmList)
    {
        WcaLog(LOGMSG_STANDARD, "MimeMapping of %ls with ParentType=%d and ParentValue=%ls not used!", psmmList->wzMimeMap, psmmList->iParentType, psmmList->wzParentValue);
        psmmList = psmmList->psmmNext;
    }

    return E_FAIL;
}


HRESULT ScaWriteMimeMap(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb, 
                               SCA_MIMEMAP* psmmList)
{
    HRESULT hr = S_OK;

    WCHAR wzMimeMap[8192];
    WCHAR *pwzNext = wzMimeMap;
    const WCHAR *pwzMac = wzMimeMap + countof(wzMimeMap); // used to properly create the MULTI_SZ

    // fill the MULTI_SZ wzMimeMap buffer for the MimeMap attribute
    ::ZeroMemory(wzMimeMap, sizeof(wzMimeMap));

    for (SCA_MIMEMAP* psmm = psmmList; psmm; psmm = psmm->psmmNext)
    {
        hr = ::StringCchPrintfW(pwzNext, max(0, pwzMac - pwzNext), L"%s,%s", psmm->wzExtension, psmm->wzMimeType);
        ExitOnFailure(hr, "Failed to set MimeMap string");

        pwzNext += lstrlenW(pwzNext) + 1; // reserve space for null
        Assert(pwzNext <= pwzMac);
    }

    if (pwzNext != wzMimeMap)
    {
        // now write the CustomErrors to the metabase
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_MIME_MAP, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, wzMimeMap);
        ExitOnFailure(hr, "Failed to write MimeMap");
    }
    else
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaWriteMimeMap() - no mappings found.");
        ExitFunction1(hr = S_FALSE);
    }

LExit:
    return hr;
}


static HRESULT AddMimeMapToList(SCA_MIMEMAP** ppsmmList)
{
    HRESULT hr = S_OK;

    SCA_MIMEMAP* psmm = static_cast<SCA_MIMEMAP*>(MemAlloc(sizeof(SCA_MIMEMAP), TRUE));
    ExitOnNull(psmm, hr, E_OUTOFMEMORY, "failed to allocate memory for new mime map list element");

    psmm->psmmNext = *ppsmmList;
    *ppsmmList = psmm;
    
LExit:
    return hr;
}
