// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
static HRESULT AddVirtualDirToList7(
    __in SCA_VDIR7** psvdList
    );


HRESULT __stdcall ScaVirtualDirsRead7(
    __in SCA_WEB7* pswList,
    __in SCA_VDIR7** ppsvdList,
    __in SCA_MIMEMAP** ppsmmList,
    __in SCA_HTTP_HEADER** ppshhList,
    __in SCA_WEB_ERROR** ppsweList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE /*hWebBaseQuery*/,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    Assert(ppsvdList);

    HRESULT hr = S_OK;
    MSIHANDLE hRec;

    SCA_VDIR7* pvdir = NULL;
    LPWSTR pwzData = NULL;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaAppPoolRead");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaVirtualDirsRead() because IIsWebVirtualDir table not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the vdirs
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        // Add this record's information into the list of things to process.
        hr = AddVirtualDirToList7(ppsvdList);
        ExitOnFailure(hr, "failed to add vdir to vdir list");

        pvdir = *ppsvdList;

        // get the darwin information
        hr = WcaGetRecordString(hRec, vdqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get IIsWebVirtualDir.Component");

        hr = WcaGetRecordInteger(hRec, vdqInstalled, (int *)&pvdir->isInstalled);
        ExitOnFailure(hr, "Failed to get Component installed state for virtual dir");

        hr = WcaGetRecordInteger(hRec, vdqAction, (int *)&pvdir->isAction);
        ExitOnFailure(hr, "Failed to get Component action state for virtual dir");

        // get vdir properties
        hr = ::StringCchCopyW(pvdir->wzComponent, countof(pvdir->wzComponent), pwzData);
        ExitOnFailure1(hr, "failed to copy vdir component name: %ls", pwzData);

        hr = WcaGetRecordString(hRec, vdqWeb, &pwzData);
        ExitOnFailure(hr, "Failed to get Web for VirtualDir");

        hr = ScaWebsGetBase7(pswList, pwzData, pvdir->wzWebName , countof(pvdir->wzWebName));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
            ExitOnFailure(hr, "Failed to get Web Base for VirtualDir");
        }
        if (WcaIsUninstalling(pvdir->isInstalled, pvdir->isAction))
        {
            // If we're uninstalling, ignore any failure to find the existing web
            hr = S_OK;
        }

        hr = WcaGetRecordString(hRec, vdqAlias, &pwzData);
        ExitOnFailure(hr, "Failed to get Alias for VirtualDir");

        hr = ::StringCchCopyW(pvdir->wzVDirRoot, countof(pvdir->wzVDirRoot), pwzData);
        ExitOnFailure(hr, "Failed to set VDirRoot for VirtualDir");

        // get the vdir's directory
        hr = WcaGetRecordString(hRec, vdqDirectory, &pwzData);
        ExitOnFailure(hr, "Failed to get Directory for VirtualDir");

        // get the web's directory
        if (INSTALLSTATE_SOURCE == pvdir->isAction)
        {
            hr = WcaGetRecordString(hRec, vdqSourcePath, &pwzData);
        }
        else
        {
            hr = WcaGetRecordString(hRec, vdqTargetPath, &pwzData);
        }
        ExitOnFailure(hr, "Failed to get Source/TargetPath for Directory");

        // remove trailing backslash(es)
        while (lstrlenW(pwzData) > 0 && pwzData[lstrlenW(pwzData)-1] == L'\\')
        {
            pwzData[lstrlenW(pwzData)-1] = 0;
        }
        hr = ::StringCchCopyW(pvdir->wzDirectory, countof(pvdir->wzDirectory), pwzData);
        ExitOnFailure(hr, "Failed to copy directory string to vdir object");

        // get the security information for this web
        hr = WcaGetRecordString(hRec, vdqProperties, &pwzData);
        ExitOnFailure(hr, "Failed to get web directory identifier for VirtualDir");
        if (*pwzData)
        {
            hr = ScaGetWebDirProperties(pwzData, hUserQuery, hWebDirPropQuery, &pvdir->swp);
            ExitOnFailure(hr, "Failed to get web directory for VirtualDir");

            pvdir->fHasProperties = TRUE;
        }

        // get the application information for this web
        hr = WcaGetRecordString(hRec, vdqApplication, &pwzData);
        ExitOnFailure(hr, "Failed to get application identifier for VirtualDir");
        if (*pwzData)
        {
            hr = ScaGetWebApplication(NULL, pwzData, hWebAppQuery, hWebAppExtQuery, &pvdir->swapp);
            ExitOnFailure(hr, "Failed to get application for VirtualDir");

            pvdir->fHasApplication = TRUE;
        }

        hr = WcaGetRecordString(hRec, vdqVDir, &pwzData);
        ExitOnFailure(hr, "Failed to get VDir for VirtualDir");

        if (*pwzData && *ppsmmList)
        {
            hr = ScaGetMimeMap(mmptVDir, pwzData, ppsmmList, &pvdir->psmm);
            ExitOnFailure(hr, "Failed to get mimemap for VirtualDir");
        }

        if (*pwzData && *ppshhList)
        {
            hr = ScaGetHttpHeader(hhptVDir, pwzData, ppshhList, &pvdir->pshh);
            ExitOnFailure1(hr, "Failed to get custom HTTP headers for VirtualDir: %ls", pwzData);
        }

        if (*pwzData && *ppsweList)
        {
            hr = ScaGetWebError(weptVDir, pwzData, ppsweList, &pvdir->pswe);
            ExitOnFailure1(hr, "Failed to get custom web errors for VirtualDir: %ls", pwzData);
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing VirtualDirs");

LExit:
    WcaFinishUnwrapQuery(hWrapQuery);

    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaVirtualDirsInstall7(
    __in SCA_VDIR7* psvdList,
    __in SCA_APPPOOL * psapList
    )
{
    HRESULT hr = S_OK;
    SCA_VDIR7* psvd = psvdList;
    LPWSTR wzPath = NULL;
    WCHAR wzAppPoolName[MAX_PATH];
    while (psvd)
    {
        if (WcaIsInstalling(psvd->isInstalled, psvd->isAction))
        {
            // First write all applications, this is necessary since vdirs must be nested under the applications.
            if (psvd->fHasApplication)
            {
                //create the application for this vdir application
                hr = ScaWriteConfigID(IIS_APPLICATION);
                ExitOnFailure(hr, "Failed to write app ID");
                hr = ScaWriteConfigID(IIS_CREATE);
                ExitOnFailure(hr, "Failed to write app action");
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
                hr = ScaWriteConfigString(psvd->wzWebName);           //site name key
                ExitOnFailure(hr, "Failed to write app web key");
                hr = StrAllocFormatted(&wzPath, L"/%s", psvd->wzVDirRoot);
                ExitOnFailure(hr, "Failed to create app path");
                hr = ScaWriteConfigString(wzPath);                    //  App Path
                ExitOnFailure(hr, "Failed to write app path root ");

                if (!*psvd->swapp.wzAppPool)
                {
                    //This Application goes in default appPool
                    hr = ScaWriteConfigString(L"");                   //  App Pool
                }
                else
                {
                    //get apppool from WebApplication
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
                    hr = ScaFindAppPool7(psvd->swapp.wzAppPool, wzAppPoolName, countof(wzAppPoolName), psapList);
                    ExitOnFailure(hr, "Failed to read app pool from application");
                    hr = ScaWriteConfigString(wzAppPoolName);           //  App Pool
                    ExitOnFailure(hr, "Failed to write appPool for vdir");

                }
            }
        }

        psvd = psvd->psvdNext;
    }

    // Reset our linked list and write all the VDirs
    psvd = psvdList;
    while (psvd)
    {
        if (WcaIsInstalling(psvd->isInstalled, psvd->isAction))
        {
            //create the Vdir
            hr = ScaWriteConfigID(IIS_VDIR);
            ExitOnFailure(hr, "Failed write VirDir ID")
            hr = ScaWriteConfigID(IIS_CREATE);
            ExitOnFailure(hr, "Failed write VirDir action")
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
            hr = ScaWriteConfigString(psvd->wzWebName);         //site name key
            ExitOnFailure(hr, "Failed write VirDir web name");
            hr = StrAllocFormatted(&wzPath, L"/%s", psvd->wzVDirRoot);
            ExitOnFailure(hr, "Failed to create vdir path");
            hr = ScaWriteConfigString(wzPath);                  //vdir path
            ExitOnFailure(hr, "Failed write VirDir path")
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
            hr = ScaWriteConfigString(psvd->wzDirectory);       //physical dir
            ExitOnFailure(hr, "Failed write VirDir dir");

            if (psvd->fHasProperties)
            {
                ScaWriteWebDirProperties7(psvd->wzWebName, psvd->wzVDirRoot, &psvd->swp);
                ExitOnFailure(hr, "Failed to write directory properties for VirtualDir");
            }

            if (psvd->fHasApplication)
            {
                hr = ScaWriteWebApplication7(psvd->wzWebName, psvd->wzVDirRoot, &psvd->swapp, psapList);
                ExitOnFailure(hr, "Failed to write application for VirtualDir");
            }

            if (psvd->psmm)
            {
                hr = ScaWriteMimeMap7(psvd->wzWebName, psvd->wzVDirRoot, psvd->psmm);
                ExitOnFailure(hr, "Failed to write mimemap for VirtualDir");
            }

            if (psvd->pshh)
            {
                hr = ScaWriteHttpHeader7(psvd->wzWebName, psvd->wzVDirRoot, psvd->pshh);
                ExitOnFailure(hr, "Failed to write custom HTTP headers for VirtualDir");
            }

            if (psvd->pswe)
            {
                hr = ScaWriteWebError7(psvd->wzWebName, psvd->wzVDirRoot, psvd->pswe);
                ExitOnFailure(hr, "Failed to write custom web errors for VirtualDir");
            }
        }

        psvd = psvd->psvdNext;
    }

LExit:
    ReleaseStr(wzPath);
    return hr;
}


HRESULT ScaVirtualDirsUninstall7(
                                 __in SCA_VDIR7* psvdList
                                 )
{

    HRESULT hr = S_OK;
    SCA_VDIR7* psvd = psvdList;
    LPWSTR wzPath = NULL;

    while (psvd)
    {
        if (WcaIsUninstalling(psvd->isInstalled, psvd->isAction))
        {
            //init path
            hr = StrAllocFormatted(&wzPath, L"/%s", psvd->wzVDirRoot);
            ExitOnFailure(hr, "Failed to create vdir path");

            if (psvd->fHasApplication)
            {
                //delete Application
                hr = ScaWriteConfigID(IIS_APPLICATION);
                ExitOnFailure(hr, "Failed to write app ID ");
                hr = ScaWriteConfigID(IIS_DELETE);
                ExitOnFailure(hr, "Failed to write delete app ID ");
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
                hr = ScaWriteConfigString(psvd->wzWebName);        //site name key
                ExitOnFailure(hr, "Failed to write App site Name");
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
                hr = ScaWriteConfigString(wzPath);                 //  App Path
                ExitOnFailure(hr, "Failed to write app path root ");
                hr = ScaWriteConfigString(L"NOP");                 //  App pool
                ExitOnFailure(hr, "Failed to write app path app pool ");
            }
            else
            {
                //delete VDir
                hr = ScaWriteConfigID(IIS_VDIR);
                ExitOnFailure(hr, "Failed to write vDir ID ");
                hr = ScaWriteConfigID(IIS_DELETE);
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
                hr = ScaWriteConfigString(psvd->wzWebName);        //site name key
                ExitOnFailure(hr, "Failed to write App site Name");
                hr = ScaWriteConfigString(wzPath);                 //  Vdir Path
                ExitOnFailure(hr, "Failed to write app vdir ");
                hr = ScaWriteConfigString(L"NOP");                 //  Phy Path
                ExitOnFailure(hr, "Failed to write vdir path");
            }

            ExitOnFailure1(hr, "Failed to remove VirtualDir '%ls' from config", psvd->wzKey);
        }

        psvd = psvd->psvdNext;
    }

LExit:
    ReleaseStr(wzPath);
    return hr;
}


void ScaVirtualDirsFreeList7(
    __in SCA_VDIR7* psvdList
    )
{
    SCA_VDIR7* psvdDelete = psvdList;
    while (psvdList)
    {
        psvdDelete = psvdList;
        psvdList = psvdList->psvdNext;

        if (psvdDelete->psmm)
        {
            ScaMimeMapFreeList(psvdDelete->psmm);
        }

        if (psvdDelete->pswe)
        {
            ScaWebErrorFreeList(psvdDelete->pswe);
        }

        MemFree(psvdDelete);
    }
}


static HRESULT AddVirtualDirToList7(
    __in SCA_VDIR7** ppsvdList
    )
{
    HRESULT hr = S_OK;

    SCA_VDIR7* psvd = static_cast<SCA_VDIR7*>(MemAlloc(sizeof(SCA_VDIR7), TRUE));
    ExitOnNull(psvd, hr, E_OUTOFMEMORY, "failed to allocate memory for new vdir list element");

    psvd->psvdNext= *ppsvdList;
    *ppsvdList = psvd;

LExit:
    return hr;
}
