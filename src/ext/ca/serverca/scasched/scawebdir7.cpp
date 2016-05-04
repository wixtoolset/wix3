// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// sql queries
static LPCWSTR vcsWebDirQuery7 = L"SELECT `Web_`, `WebDir`, `Component_`, `Path`, `DirProperties_`, `Application_`"
                                       L"FROM `IIsWebDir`";

enum eWebDirQuery { wdqWeb = 1, wdqWebDir, wdqComponent , wdqPath, wdqProperties, wdqApplication, wdqInstalled, wdqAction };

// prototypes
static HRESULT AddWebDirToList(SCA_WEBDIR7** ppswdList);


UINT __stdcall ScaWebDirsRead7(
    __in SCA_WEB7* pswList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE /*hWebBaseQuery*/,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData,
    __out SCA_WEBDIR7** ppswdList
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec;

    LPWSTR pwzData = NULL;
    SCA_WEBDIR7* pswd;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaWebDirsRead7");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallWebDirs7() because IIsWebDir table not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the web directories
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        hr = AddWebDirToList(ppswdList);
        ExitOnFailure(hr, "failed to add web dir to list");

        pswd = *ppswdList;
        ExitOnNull(pswd, hr, E_INVALIDARG, "No web dir provided");

        // get component install state
        hr = WcaGetRecordString(hRec, wdqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Component for WebDirs");
        hr = ::StringCchCopyW(pswd->wzComponent, countof(pswd->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to webdir object");

        hr = WcaGetRecordInteger(hRec, wdqInstalled, (int *)&pswd->isInstalled);
        ExitOnFailure(hr, "Failed to get Component installed state for webdir");

        hr = WcaGetRecordInteger(hRec, wdqAction, (int *)&pswd->isAction);
        ExitOnFailure(hr, "Failed to get Component action state for webdir");

        hr = WcaGetRecordString(hRec, wdqWeb, &pwzData);
        ExitOnFailure(hr, "Failed to get Web for WebDir");

        // get the web key
        hr = ScaWebsGetBase7(pswList, pwzData, pswd->wzWebSite, countof(pswd->wzWebSite));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
            ExitOnFailure(hr, "Failed to get base of web for WebDir");
        }
        ExitOnFailure(hr, "Failed to format webdir root string");

        hr = WcaGetRecordString(hRec, wdqPath, &pwzData);
        ExitOnFailure(hr, "Failed to get Path for WebDir");

        hr = ::StringCchCopyW(pswd->wzPath, countof(pswd->wzPath), pwzData);
        ExitOnFailure(hr, "Failed to copy path for WebDir");

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


HRESULT ScaWebDirsInstall7(SCA_WEBDIR7* pswdList, SCA_APPPOOL * psapList)
{
    HRESULT hr = S_OK;
    SCA_WEBDIR7* pswd = pswdList;

    while (pswd)
    {
        // if we are installing the web site
        if (WcaIsInstalling(pswd->isInstalled, pswd->isAction))
        {
            hr = ScaWriteConfigID(IIS_WEBDIR);
            ExitOnFailure(hr, "Failed to write WebDir ID");

            hr = ScaWriteConfigID(IIS_CREATE);
            ExitOnFailure(hr, "Failed to write WebDir action ID");

            hr = ScaWriteConfigString(pswd->wzWebSite);
            ExitOnFailure(hr, "Failed to write WebDir site");

            hr = ScaWriteConfigString(pswd->wzPath);
            ExitOnFailure(hr, "Failed to write WebDir path");

            // get the security information for this web
            if (pswd->fHasProperties)
            {
                ScaWriteWebDirProperties7(pswd->wzWebSite, pswd->wzPath, &pswd->swp);
                ExitOnFailure(hr, "Failed to write properties for WebDir");
            }

            // get the application information for this web directory
            if (pswd->fHasApplication)
            {
                hr = ScaWriteWebApplication7(pswd->wzWebSite, pswd->wzPath, &pswd->swapp, psapList);
                ExitOnFailure(hr, "Failed to write application for WebDir");
            }
        }

        pswd = pswd->pswdNext;
    }

LExit:
    return hr;
}


HRESULT ScaWebDirsUninstall7(SCA_WEBDIR7* pswdList)
{
    HRESULT hr = S_OK;
    SCA_WEBDIR7* pswd = pswdList;

    while (pswd)
    {
        if (WcaIsUninstalling(pswd->isInstalled, pswd->isAction))
        {
            hr = ScaWriteConfigID(IIS_WEBDIR);
            ExitOnFailure(hr, "Failed to write WebDir ID");

            hr = ScaWriteConfigID(IIS_DELETE);
            ExitOnFailure(hr, "Failed to write WebDir action ID");

            hr = ScaWriteConfigString(pswd->wzWebSite);
            ExitOnFailure(hr, "Failed to write WebDir site");

            hr = ScaWriteConfigString(pswd->wzPath);
            ExitOnFailure(hr, "Failed to write WebDir path");
        }

        pswd = pswd->pswdNext;
    }

LExit:
    return hr;
}


void ScaWebDirsFreeList7(SCA_WEBDIR7* pswdList)
{
    SCA_WEBDIR7* pswdDelete = pswdList;
    while (pswdList)
    {
        pswdDelete = pswdList;
        pswdList = pswdList->pswdNext;

        MemFree(pswdDelete);
    }
}


static HRESULT AddWebDirToList(SCA_WEBDIR7** ppswdList)
{
    HRESULT hr = S_OK;

    SCA_WEBDIR7* pswd = static_cast<SCA_WEBDIR7*>(MemAlloc(sizeof(SCA_WEBDIR7), TRUE));
    ExitOnNull(pswd, hr, E_OUTOFMEMORY, "failed to allocate element for web dir list");

    pswd->pswdNext = *ppswdList;
    *ppswdList = pswd;

LExit:
    return hr;
}
