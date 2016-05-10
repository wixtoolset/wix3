// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
static HRESULT AddVirtualDirToList(
    __in SCA_VDIR** psvdList
    );


HRESULT __stdcall ScaVirtualDirsRead(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in SCA_VDIR** ppsvdList,
    __in SCA_MIMEMAP** ppsmmList,
    __in SCA_HTTP_HEADER** ppshhList,
    __in SCA_WEB_ERROR** ppsweList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebBaseQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    Assert(piMetabase && ppsvdList);

    HRESULT hr = S_OK;
    MSIHANDLE hRec;
    INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
    INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

    SCA_VDIR* pvdir = NULL;
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
        // Get the Component first.  If there is a Component and it is not being modified during
        // this transaction, skip processing this whole record.
        hr = WcaGetRecordString(hRec, vdqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get IIsWebVirtualDir.Component");

        hr = WcaGetRecordInteger(hRec, vdqInstalled, (int *)&isInstalled);
        ExitOnFailure(hr, "Failed to get Component installed state for virtual dir");

        hr = WcaGetRecordInteger(hRec, vdqAction, (int *)&isAction);
        ExitOnFailure(hr, "Failed to get Component action state for virtual dir");

        if (!WcaIsInstalling(isInstalled, isAction) &&
            !WcaIsReInstalling(isInstalled, isAction) &&
            !WcaIsUninstalling(isInstalled, isAction))
        {
            continue; // skip this record.
        }

        hr = AddVirtualDirToList(ppsvdList);
        ExitOnFailure(hr, "failed to add virtual dir to list");

        pvdir = *ppsvdList;

        hr = ::StringCchCopyW(pvdir->wzComponent, countof(pvdir->wzComponent), pwzData);
        ExitOnFailure1(hr, "failed to copy component name: %ls", pwzData);

        pvdir->isInstalled = isInstalled;
        pvdir->isAction = isAction;

        // get the web key
        hr = WcaGetRecordString(hRec, vdqWeb, &pwzData);
        ExitOnFailure(hr, "Failed to get Web for VirtualDir");

        hr = ScaWebsGetBase(piMetabase, pswList, pwzData, pvdir->wzWebBase, countof(pvdir->wzWebBase), hWebBaseQuery);
        if (WcaIsUninstalling(isInstalled, isAction))
        {
            // If we're uninstalling, ignore any failure to find the existing web
            hr = S_OK;
        }
        ExitOnFailure1(hr, "Failed to get base of web: %ls for VirtualDir", pwzData);

        hr = WcaGetRecordString(hRec, vdqAlias, &pwzData);
        ExitOnFailure(hr, "Failed to get Alias for VirtualDir");

        if (0 != lstrlenW(pvdir->wzWebBase))
        {
            hr = ::StringCchPrintfW(pvdir->wzVDirRoot, countof(pvdir->wzVDirRoot), L"%s/Root/%s", pvdir->wzWebBase, pwzData);
            ExitOnFailure(hr, "Failed to set VDirRoot for VirtualDir");
        }

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


HRESULT ScaVirtualDirsInstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_VDIR* psvdList,
    __in SCA_APPPOOL * psapList
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;
    SCA_VDIR* psvd = psvdList;
    int i;

    while (psvd)
    {
        // On reinstall, we have to uninstall the old application, otherwise a duplicate will be created
        if (WcaIsReInstalling(psvd->isInstalled, psvd->isAction))
        {
            if (psvd->fHasApplication)
            {
                hr = ScaDeleteApp(piMetabase, psvd->wzVDirRoot);
                ExitOnFailure(hr, "Failed to remove application for WebVDir as part of a reinstall");
            }
        }

        if (WcaIsInstalling(psvd->isInstalled, psvd->isAction))
        {
            hr = ScaCreateMetabaseKey(piMetabase, psvd->wzVDirRoot, L"");
            ExitOnFailure(hr, "Failed to create key for VirtualDir");
            hr = ScaWriteMetabaseValue(piMetabase, psvd->wzVDirRoot, L"", MD_KEY_TYPE, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, STRING_METADATA, (LPVOID)L"IIsWebVirtualDir");
            ExitOnFailure(hr, "Failed to write key type for for VirtualDir");
            i = 0x4000003e; // 1073741886;	// default directory browsing rights
            hr = ScaWriteMetabaseValue(piMetabase, psvd->wzVDirRoot, L"", MD_DIRECTORY_BROWSING, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)i));
            ExitOnFailure(hr, "Failed to set directory browsing for VirtualDir");

            hr = ScaWriteMetabaseValue(piMetabase, psvd->wzVDirRoot, L"", MD_VR_PATH, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)psvd->wzDirectory);
            ExitOnFailure(hr, "Failed to write Directory for VirtualDir");

            if (psvd->fHasProperties)
            {
                ScaWriteWebDirProperties(piMetabase, psvd->wzVDirRoot, &psvd->swp);
                ExitOnFailure(hr, "Failed to write directory properties for VirtualDir");
            }

            if (psvd->fHasApplication)
            {
                hr = ScaWriteWebApplication(piMetabase, psvd->wzVDirRoot, &psvd->swapp, psapList);
                ExitOnFailure(hr, "Failed to write application for VirtualDir");
            }

            if (psvd->psmm)
            {
                hr = ScaWriteMimeMap(piMetabase, psvd->wzVDirRoot, psvd->psmm);
                ExitOnFailure(hr, "Failed to write mimemap for VirtualDir");
            }

            if (psvd->pshh)
            {
                hr = ScaWriteHttpHeader(piMetabase, psvd->wzVDirRoot, psvd->pshh);
                ExitOnFailure(hr, "Failed to write custom HTTP headers for VirtualDir");
            }

            if (psvd->pswe)
            {
                hr = ScaWriteWebError(piMetabase, weptVDir, psvd->wzVDirRoot, psvd->pswe);
                ExitOnFailure(hr, "Failed to write custom web errors for VirtualDir");
            }
        }

        psvd = psvd->psvdNext;
    }

LExit:
    return hr;
}


HRESULT ScaVirtualDirsUninstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_VDIR* psvdList
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;
    SCA_VDIR* psvd = psvdList;

    while (psvd)
    {
        if (WcaIsUninstalling(psvd->isInstalled, psvd->isAction))
        {
            // delete the application for this virtual directory
            if (psvd->fHasApplication)
            {
                hr = ScaDeleteApp(piMetabase, psvd->wzVDirRoot);
                ExitOnFailure(hr, "Failed to remove application for WebVDir");
            }

            if (0 != lstrlenW(psvd->wzVDirRoot))
            {
                hr = ScaDeleteMetabaseKey(piMetabase, psvd->wzVDirRoot, L"");
                ExitOnFailure1(hr, "Failed to remove VirtualDir '%ls' from metabase", psvd->wzKey);
            }
        }

        psvd = psvd->psvdNext;
    }

LExit:
    return hr;
}


void ScaVirtualDirsFreeList(
    __in SCA_VDIR* psvdList
    )
{
    SCA_VDIR* psvdDelete = psvdList;
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


static HRESULT AddVirtualDirToList(
    __in SCA_VDIR** ppsvdList
    )
{
    HRESULT hr = S_OK;
    SCA_VDIR* psvd = static_cast<SCA_VDIR*>(MemAlloc(sizeof(SCA_VDIR), TRUE));
    ExitOnNull(psvd, hr, E_OUTOFMEMORY, "failed to allocate memory for new vdir list element");

    psvd->psvdNext= *ppsvdList;
    *ppsvdList = psvd;

LExit:
    return hr;
}
