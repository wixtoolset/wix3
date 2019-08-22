// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT WriteFilter(const SCA_FILTER* psf);

UINT __stdcall ScaFiltersRead7(
    __in SCA_WEB7* pswList,
    __in WCA_WRAPQUERY_HANDLE /*hWebBaseQuery*/,
    __inout SCA_FILTER** ppsfList,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec;
    INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
    INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;
    SCA_FILTER* psf;

    LPWSTR pwzData = NULL;
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
        // get the darwin information
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
            hr = ScaWebsGetBase7(pswList, pwzData, psf->wzFilterRoot, countof(psf->wzFilterRoot));
            if (FAILED(hr))
            {
                WcaLog(LOGMSG_VERBOSE, "Could not find site for filter: %ls. Result 0x%x ", psf->wzFilterRoot, hr);
                hr = S_OK;
            }
        }
        else
        {
            hr = ::StringCchCopyW(psf->wzFilterRoot, countof(psf->wzFilterRoot), L"/");
            ExitOnFailure(hr, "Failed to allocate global filter base string");
        }

        // filter Name key
        hr = WcaGetRecordString(hRec, fqFilter, &pwzData);
        ExitOnFailure(hr, "Failed to get Filter.Filter");
        hr = ::StringCchCopyW(psf->wzKey, countof(psf->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to filter object");

        // filter path
        hr = WcaGetRecordString(hRec, fqPath, &pwzData);
        ExitOnFailure(hr, "Failed to get Filter.Path");
        hr = ::StringCchCopyW(psf->wzPath, countof(psf->wzPath), pwzData);
        ExitOnFailure(hr, "Failed to copy path string to filter object");

        // filter description -- not supported in iis 7
        hr = WcaGetRecordString(hRec, fqDescription, &pwzData);
        ExitOnFailure(hr, "Failed to get Filter.Description");
        hr = ::StringCchCopyW(psf->wzDescription, countof(psf->wzDescription), pwzData);
        ExitOnFailure(hr, "Failed to copy description string to filter object");

        // filter flags
        //What are these
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


HRESULT ScaFiltersInstall7(
    __in SCA_FILTER* psfList
    )
{
    HRESULT hr = S_OK;
    SCA_FILTER* psf = psfList;

    if (!psf)
    {
        ExitFunction();
    }
    //write global filters
    hr = ScaWriteConfigID(IIS_FILTER_GLOBAL_BEGIN);
    ExitOnFailure(hr, "Failed to write filter begin ID");
    while (psf)
    {
        if (WcaIsInstalling(psf->isInstalled, psf->isAction))
        {
            if (0 == wcscmp(psf->wzFilterRoot, L"/"))
            {
                hr = WriteFilter(psf);
            }
        }
        psf = psf->psfNext;
    }
    hr = ScaWriteConfigID(IIS_FILTER_END);
    ExitOnFailure(hr, "Failed to write filter ID");

    psf = psfList;

    //Write Web Site Filters
    hr = ScaWriteConfigID(IIS_FILTER_BEGIN);
    ExitOnFailure(hr, "Failed to write filter begin ID");
    while (psf)
    {
        if (WcaIsInstalling(psf->isInstalled, psf->isAction))
        {
            if (0 != wcscmp(psf->wzFilterRoot, L"/"))
            {
                hr = WriteFilter(psf);
            }
        }
        psf = psf->psfNext;
    }
    hr = ScaWriteConfigID(IIS_FILTER_END);
    ExitOnFailure(hr, "Failed to write filter ID");

LExit:

    return hr;
}
static HRESULT WriteFilter(const SCA_FILTER* psf)
{
    HRESULT hr = S_OK;

    hr = ScaWriteConfigID(IIS_FILTER);
    ExitOnFailure(hr, "Failed to write filter begin ID");

    hr = ScaWriteConfigID(IIS_CREATE);
    ExitOnFailure(hr, "Failed to write filter create ID");

    //filter Name key
    hr = ScaWriteConfigString(psf->wzKey);
    ExitOnFailure1(hr, "Failed to write key name for filter '%ls'", psf->wzKey);

    //web site name
    hr = ScaWriteConfigString(psf->wzFilterRoot);
    ExitOnFailure(hr, "Failed to write filter web root ");

    // filter path
    hr = ScaWriteConfigString(psf->wzPath);
    ExitOnFailure1(hr, "Failed to write Path for filter '%ls'", psf->wzKey);

    //filter load order
    hr = ScaWriteConfigInteger(psf->iLoadOrder);
    ExitOnFailure1(hr, "Failed to write load order for filter '%ls'", psf->wzKey);

LExit:
    return hr;
}


HRESULT ScaFiltersUninstall7(
    __in SCA_FILTER* psfList
    )
{
    HRESULT hr = S_OK;
    SCA_FILTER* psf = psfList;

    if (!psf)
    {
        ExitFunction1(hr = S_OK);
    }

    //Uninstall global filters
    hr = ScaWriteConfigID(IIS_FILTER_GLOBAL_BEGIN);
    ExitOnFailure(hr, "Failed to write filter begin ID");

    while (psf)
    {
        if (WcaIsUninstalling(psf->isInstalled, psf->isAction))
        {
            if (0 == wcscmp(psf->wzFilterRoot, L"/"))
            {
                hr = ScaWriteConfigID(IIS_FILTER);
                ExitOnFailure(hr, "Failed to write filter begin ID");

                hr = ScaWriteConfigID(IIS_DELETE);
                ExitOnFailure(hr, "Failed to write filter create ID");

                //filter Name key
                hr = ScaWriteConfigString(psf->wzKey);
                ExitOnFailure1(hr, "Failed to write key name for filter '%ls'", psf->wzKey);

                //web site name
                hr = ScaWriteConfigString(psf->wzFilterRoot);
                ExitOnFailure(hr, "Failed to write filter web root ");

            }
        }
        psf = psf->psfNext;
    }

    hr = ScaWriteConfigID(IIS_FILTER_END);
    ExitOnFailure(hr, "Failed to write filter ID");

    psf = psfList;

    //Uninstall website filters
    hr = ScaWriteConfigID(IIS_FILTER_BEGIN);
    ExitOnFailure(hr, "Failed to write filter begin ID");
    while (psf)
    {
        if (WcaIsUninstalling(psf->isInstalled, psf->isAction))
        {
            if (0 != wcscmp(psf->wzFilterRoot, L"/"))
            {
                hr = ScaWriteConfigID(IIS_FILTER);
                ExitOnFailure(hr, "Failed to write filter begin ID");

                hr = ScaWriteConfigID(IIS_DELETE);
                ExitOnFailure(hr, "Failed to write filter create ID");

                //filter Name key
                hr = ScaWriteConfigString(psf->wzKey);
                ExitOnFailure1(hr, "Failed to write key name for filter '%ls'", psf->wzKey);

                //web site name
                hr = ScaWriteConfigString(psf->wzFilterRoot);
                ExitOnFailure(hr, "Failed to write filter web root ");
            }
        }
        psf = psf->psfNext;
    }
    hr = ScaWriteConfigID(IIS_FILTER_END);
    ExitOnFailure(hr, "Failed to write filter ID");

LExit:
    return hr;
}
