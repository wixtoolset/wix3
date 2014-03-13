//-------------------------------------------------------------------------------------------------
// <copyright file="scawebappext.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Web Application Extension functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

enum eWebAppExtensionQuery { wappextqExtension = 1, wappextqVerbs, wappextqExecutable, wappextqAttributes, wappextqApplication };

// prototypes for private helper functions
static HRESULT NewAppExt(
    __out SCA_WEB_APPLICATION_EXTENSION** ppswappext
    );
static SCA_WEB_APPLICATION_EXTENSION* AddAppExtToList(
    __in SCA_WEB_APPLICATION_EXTENSION* pswappextList,
    __in SCA_WEB_APPLICATION_EXTENSION* pswappext
    );



HRESULT ScaWebAppExtensionsRead(
    __in LPCWSTR wzApplication,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout SCA_WEB_APPLICATION_EXTENSION** ppswappextList
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec;

    SCA_WEB_APPLICATION_EXTENSION* pswappext = NULL;
    LPWSTR pwzData = NULL;

    // Reset back to the first record
    WcaFetchWrappedReset(hWebAppExtQuery);

    // get the application extension information
    while (S_OK == (hr = WcaFetchWrappedRecordWhereString(hWebAppExtQuery, wappextqApplication, wzApplication, &hRec)))
    {
        hr = NewAppExt(&pswappext);
        ExitOnFailure(hr, "failed to create new web app extension");

        // get the extension
        hr = WcaGetRecordString(hRec, wappextqExtension, &pwzData);
        ExitOnFailure(hr, "Failed to get Web Application Extension");
        hr = ::StringCchCopyW(pswappext->wzExtension, countof(pswappext->wzExtension), pwzData);
        ExitOnFailure(hr, "Failed to copy extension string to webappext object");

        // application extension verbs
        hr = WcaGetRecordString(hRec, wappextqVerbs, &pwzData);
        ExitOnFailure1(hr, "Failed to get Verbs for Application: '%ls'", wzApplication);
        hr = ::StringCchCopyW(pswappext->wzVerbs, countof(pswappext->wzVerbs), pwzData);
        ExitOnFailure(hr, "Failed to copy verbs string to webappext object");

        // extension executeable
        hr = WcaGetRecordString(hRec, wappextqExecutable, &pwzData);
        ExitOnFailure1(hr, "Failed to get Executable for Application: '%ls'", wzApplication);
        hr = ::StringCchCopyW(pswappext->wzExecutable, countof(pswappext->wzExecutable), pwzData);
        ExitOnFailure(hr, "Failed to copy executable string to webappext object");

        hr = WcaGetRecordInteger(hRec, wappextqAttributes, &pswappext->iAttributes);
        if (S_FALSE == hr)
        {
            pswappext->iAttributes = 0;
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to get App isolation");

        *ppswappextList = AddAppExtToList(*ppswappextList, pswappext);
        pswappext = NULL; // set the appext NULL so it doesn't accidentally get freed below
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }

LExit:
    // if anything was left over after an error clean it all up
    if (pswappext)
    {
        ScaWebAppExtensionsFreeList(pswappext);
    }

    ReleaseStr(pwzData);

    return hr;
}



HRESULT ScaWebAppExtensionsWrite(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzRootOfWeb,
    __in SCA_WEB_APPLICATION_EXTENSION* pswappextList
    )
{
    HRESULT hr = S_OK;

    LPWSTR wzAppExt = NULL;
    DWORD cchAppExt;
    WCHAR wzAppExtension[1024];
    WCHAR wzAppExtensions[65536];
    SCA_WEB_APPLICATION_EXTENSION* pswappext = NULL;

    if (!pswappextList)
    {
        ExitFunction();
    }

    ::ZeroMemory(wzAppExtensions, sizeof(wzAppExtensions));
    wzAppExt = wzAppExtensions;
    cchAppExt = countof(wzAppExtensions);
    pswappext = pswappextList;

    while (pswappext)
    {
        // if all (represented by "*" or blank)
        if (0 == lstrcmpW(pswappext->wzExtension, L"*") || 0 == lstrlenW(pswappext->wzExtension))
        {
            hr = ::StringCchPrintfW(wzAppExtension, countof(wzAppExtension), L"*,%s,%d", pswappext->wzExecutable, pswappext->iAttributes);
            ExitOnFailure(hr, "Failed to format *,executable,attributes string");
        }
        else
        {
            hr = ::StringCchPrintfW(wzAppExtension, countof(wzAppExtension), L".%s,%s,%d", pswappext->wzExtension, pswappext->wzExecutable, pswappext->iAttributes);
            ExitOnFailure(hr, "Failed to format extension,executable,attributes string");
        }

        // if verbs were specified and not the keyword "all"
        if (pswappext->wzVerbs[0] && CSTR_EQUAL != CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pswappext->wzVerbs, -1, L"all", -1))
        {
            hr = ::StringCchCatW(wzAppExtension, countof(wzAppExtension), L",");
            ExitOnFailure(hr, "Failed to concatenate comma to app extension string");
            hr = ::StringCchCatW(wzAppExtension, countof(wzAppExtension), pswappext->wzVerbs);
            ExitOnFailure(hr, "Failed to concatenate verb to app extension string");
        }

        hr = ::StringCchCopyW(wzAppExt, cchAppExt, wzAppExtension);
        ExitOnFailure(hr, "Failed to copy app extension string");
        wzAppExt += lstrlenW(wzAppExtension) + 1;
        cchAppExt -= lstrlenW(wzAppExtension) + 1;
        pswappext = pswappext->pswappextNext;
    }

    if (*wzAppExtensions)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_SCRIPT_MAPS, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, wzAppExtensions);
        ExitOnFailure1(hr, "Failed to write AppExtension: '%ls'", wzAppExtension);
    }

LExit:
    return hr;
}


void ScaWebAppExtensionsFreeList(
    __in SCA_WEB_APPLICATION_EXTENSION* pswappextList
    )
{
    SCA_WEB_APPLICATION_EXTENSION* pswappextDelete = pswappextList;
    while (pswappextList)
    {
        pswappextDelete = pswappextList;
        pswappextList = pswappextList->pswappextNext;

        MemFree(pswappextDelete);
    }
}



// private helper functions

static HRESULT NewAppExt(
    __out SCA_WEB_APPLICATION_EXTENSION** ppswappext
    )
{
    HRESULT hr = S_OK;
    SCA_WEB_APPLICATION_EXTENSION* pswappext = static_cast<SCA_WEB_APPLICATION_EXTENSION*>(MemAlloc(sizeof(SCA_WEB_APPLICATION_EXTENSION), TRUE));
    ExitOnNull(pswappext, hr, E_OUTOFMEMORY, "failed to allocate memory for new web app ext element");

    *ppswappext = pswappext;

LExit:
    return hr;
}


static SCA_WEB_APPLICATION_EXTENSION* AddAppExtToList(
    __in SCA_WEB_APPLICATION_EXTENSION* pswappextList,
    __in SCA_WEB_APPLICATION_EXTENSION* pswappext
    )
{
    if (pswappextList)
    {
        SCA_WEB_APPLICATION_EXTENSION* pswappextT = pswappextList;
        while (pswappextT->pswappextNext)
        {
            pswappextT = pswappextT->pswappextNext;
        }

        pswappextT->pswappextNext = pswappext;
    }
    else
    {
        pswappextList = pswappext;
    }

    return pswappextList;
}
