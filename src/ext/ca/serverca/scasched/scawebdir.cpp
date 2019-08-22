// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// sql queries
enum eWebDirQuery { wdqWeb = 1, wdqWebDir, wdqComponent , wdqPath, wdqProperties, wdqApplication, wdqInstalled, wdqAction };

// prototypes
static void AddWebDirToList(SCA_WEBDIR** ppswdList, SCA_WEBDIR *pswd);

static SCA_WEBDIR* NewWebDir();
static void FreeWebDir(SCA_WEBDIR *pswd);


UINT __stdcall ScaWebDirsRead(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebBaseQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData,
    __out SCA_WEBDIR** ppswdList
    )
{
    Assert(piMetabase && ppswdList);

    HRESULT hr = S_OK;
    MSIHANDLE hRec;

    LPWSTR pwzData = NULL;
    SCA_WEBDIR* pswd;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaWebDirsRead");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallWebDirs() because IIsWebDir table not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the web directories
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        pswd = NewWebDir();
        ExitOnNull(pswd, hr, E_OUTOFMEMORY, "Failed to allocate memory for web dir object in memory");

        // get component install state
        hr = WcaGetRecordString(hRec, wdqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Component for WebDirs");
        hr = ::StringCchCopyW(pswd->wzComponent, countof(pswd->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to webdir object");

        hr = WcaGetRecordInteger(hRec, wdqInstalled, (int *)&pswd->isInstalled);
        ExitOnFailure(hr, "Failed to get Component installed state for webdir");

        hr = WcaGetRecordInteger(hRec, wdqAction, (int *)&pswd->isAction);
        ExitOnFailure(hr, "Failed to get Component action state for webdir");

        // If this record has a component and no action is being taken for it, skip processing it entirely
        if (0 < lstrlenW(pswd->wzComponent) && !WcaIsInstalling(pswd->isInstalled, pswd->isAction)
            && !WcaIsUninstalling(pswd->isInstalled, pswd->isAction) && !WcaIsReInstalling(pswd->isInstalled, pswd->isAction))
        {
            FreeWebDir(pswd);
            pswd = NULL;
            continue;
        }

        hr = WcaGetRecordString(hRec, wdqWeb, &pwzData);
        ExitOnFailure(hr, "Failed to get Web for WebDir");

        hr = ScaWebsGetBase(piMetabase, pswList, pwzData, pswd->wzWebBase, countof(pswd->wzWebBase), hWebBaseQuery);
        if (WcaIsUninstalling(pswd->isInstalled, pswd->isAction))
        {
            // If we're uninstalling, ignore any failure to find the existing web
            hr = S_OK;
        }

        ExitOnFailure(hr, "Failed to get base of web for WebDir");

        hr = WcaGetRecordString(hRec, wdqPath, &pwzData);
        ExitOnFailure(hr, "Failed to get Path for WebDir");

        hr = ::StringCchPrintfW(pswd->wzWebDirRoot, countof(pswd->wzWebDirRoot), L"%s/Root/%s", pswd->wzWebBase, pwzData);
        ExitOnFailure(hr, "Failed to format webdir root string");

        // get the directory properties for this web
        hr = WcaGetRecordString(hRec, wdqProperties, &pwzData);
        ExitOnFailure(hr, "Failed to get security identifier for WebDir");
        if (*pwzData)
        {
            hr = ScaGetWebDirProperties(pwzData, hUserQuery, hWebDirPropQuery, &pswd->swp);
            ExitOnFailure(hr, "Failed to get properties for WebDir");

            pswd->fHasProperties = TRUE;
        }

        // get the application information for this web directory
        hr = WcaGetRecordString(hRec, wdqApplication, &pwzData);
        ExitOnFailure(hr, "Failed to get application identifier for WebDir");
        if (*pwzData)
        {
            hr = ScaGetWebApplication(NULL, pwzData, hWebAppQuery, hWebAppExtQuery, &pswd->swapp);
            ExitOnFailure(hr, "Failed to get application for WebDir");

            pswd->fHasApplication = TRUE;
        }

        AddWebDirToList(ppswdList, pswd);
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing WebDirs");

LExit:
    WcaFinishUnwrapQuery(hWrapQuery);

    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaWebDirsInstall(IMSAdminBase* piMetabase, SCA_WEBDIR* pswdList, SCA_APPPOOL * psapList)
{
    HRESULT hr = S_OK;
    SCA_WEBDIR* pswd = pswdList;
    int i;

    while (pswd)
    {
        // On reinstall, we have to uninstall the old application, otherwise a duplicate will be created
        if (WcaIsReInstalling(pswd->isInstalled, pswd->isAction))
        {
            if (pswd->fHasApplication)
            {
                hr = ScaDeleteApp(piMetabase, pswd->wzWebDirRoot);
                ExitOnFailure(hr, "Failed to remove application for WebDir as part of a reinstall");
            }
        }

        // if we are installing the web site
        if (WcaIsInstalling(pswd->isInstalled, pswd->isAction))
        {
            hr = ScaCreateMetabaseKey(piMetabase, pswd->wzWebDirRoot, L"");
            ExitOnFailure(hr, "Failed to create key for WebDir");
            hr = ScaWriteMetabaseValue(piMetabase, pswd->wzWebDirRoot, L"", MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsWebDirectory");
            ExitOnFailure(hr, "Failed to write key type for for WebDir");
            i = 0x4000003e; // 1073741886: default directory browsing rights
            hr = ScaWriteMetabaseValue(piMetabase, pswd->wzWebDirRoot, L"", MD_DIRECTORY_BROWSING, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)i));
            ExitOnFailure(hr, "Failed to set directory browsing for WebDir");

            // get the security information for this web
            if (pswd->fHasProperties)
            {
                ScaWriteWebDirProperties(piMetabase, pswd->wzWebDirRoot, &pswd->swp);
                ExitOnFailure(hr, "Failed to write properties for WebDir");
            }

            // get the application information for this web directory
            if (pswd->fHasApplication)
            {
                hr = ScaWriteWebApplication(piMetabase, pswd->wzWebDirRoot, &pswd->swapp, psapList);
                ExitOnFailure(hr, "Failed to write application for WebDir");
            }
        }

        pswd = pswd->pswdNext;
    }

LExit:
    return hr;
}


HRESULT ScaWebDirsUninstall(IMSAdminBase* piMetabase, SCA_WEBDIR* pswdList)
{
    Assert(piMetabase);

    HRESULT hr = S_OK;
    SCA_WEBDIR* pswd = pswdList;

    while (pswd)
    {
        if (WcaIsUninstalling(pswd->isInstalled, pswd->isAction))
        {
            // remove the application from this web directory
            if (pswd->fHasApplication)
            {
                hr = ScaDeleteApp(piMetabase, pswd->wzWebDirRoot);
                ExitOnFailure(hr, "Failed to remove application for WebDir");
            }

            hr = ScaDeleteMetabaseKey(piMetabase, pswd->wzWebDirRoot, L"");
            ExitOnFailure1(hr, "Failed to remove WebDir '%ls' from metabase", pswd->wzKey);
        }

        pswd = pswd->pswdNext;
    }

LExit:
    return hr;
}


static SCA_WEBDIR* NewWebDir()
{
    SCA_WEBDIR* pswd = static_cast<SCA_WEBDIR*>(MemAlloc(sizeof(SCA_WEBDIR), TRUE));
    Assert(pswd);
    return pswd;
}

static void FreeWebDir(SCA_WEBDIR *pswd)
{
    MemFree(pswd);
}

void ScaWebDirsFreeList(SCA_WEBDIR* pswdList)
{
    SCA_WEBDIR* pswdDelete = pswdList;
    while (pswdList)
    {
        pswdDelete = pswdList;
        pswdList = pswdList->pswdNext;

        FreeWebDir(pswdDelete);
    }
}


static void AddWebDirToList(SCA_WEBDIR** ppswdList, SCA_WEBDIR *pswd)
{
    pswd->pswdNext = *ppswdList;
    *ppswdList = pswd;
}
