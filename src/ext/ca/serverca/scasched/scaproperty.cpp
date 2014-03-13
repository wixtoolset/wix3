//-------------------------------------------------------------------------------------------------
// <copyright file="scaproperty.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Property functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/*------------------------------------------------------------------
IIsProperty table:

Property  Component_  Attributes  Value
s72      s72         i4          s255
------------------------------------------------------------------*/

// sql queries
enum ePropertyQuery { pqProperty = 1, pqComponent, pqAttributes, pqValue, pqInstalled, pqAction };


// prototypes
static HRESULT AddPropertyToList(
    SCA_PROPERTY** ppspList
    );


// functions
void ScaPropertyFreeList(
    SCA_PROPERTY* pspList
    )
{
    SCA_PROPERTY* pspDelete = pspList;
    while (pspList)
    {
        pspDelete = pspList;
        pspList = pspList->pspNext;

        MemFree(pspDelete);
    }
}


HRESULT ScaPropertyRead(
    SCA_PROPERTY** ppspList,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec;

    LPWSTR pwzData = NULL;
    SCA_PROPERTY* pss;

    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    ExitOnNull(ppspList, hr, E_INVALIDARG, "Failed to read property, because no property to read was provided");

    hr = WcaBeginUnwrapQuery(&hWrapQuery, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaAppPoolRead");

    if (0 == WcaGetQueryRecords(hWrapQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaInstallProperty() - required table not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the Settings
    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        hr = AddPropertyToList(ppspList);
        ExitOnFailure(hr, "failed to add property to list");

        pss = *ppspList;

        hr = WcaGetRecordString(hRec, pqProperty, &pwzData);
        ExitOnFailure(hr, "failed to get IIsProperty.Property");
        hr = ::StringCchCopyW(pss->wzProperty, countof(pss->wzProperty), pwzData);
        ExitOnFailure1(hr, "failed to copy Property name: %ls", pwzData);

        hr = WcaGetRecordString(hRec, pqValue, &pwzData);
        ExitOnFailure(hr, "failed to get IIsProperty.Value");
        hr = ::StringCchCopyW(pss->wzValue, countof(pss->wzValue), pwzData);
        ExitOnFailure1(hr, "failed to copy Property value: %ls", pwzData);

        hr = WcaGetRecordInteger(hRec, pqAttributes, &pss->iAttributes);
        ExitOnFailure(hr, "failed to get IIsProperty.Attributes");

        hr = WcaGetRecordString(hRec, pqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get IIsProperty.Component");
        hr = ::StringCchCopyW(pss->wzComponent, countof(pss->wzComponent), pwzData);
        ExitOnFailure1(hr, "failed to copy component name: %ls", pwzData);

        hr = WcaGetRecordInteger(hRec, pqInstalled, (int *)&pss->isInstalled);
        ExitOnFailure(hr, "Failed to get Component installed state for filter");

        hr = WcaGetRecordInteger(hRec, pqAction, (int *)&pss->isAction);
        ExitOnFailure(hr, "Failed to get Component action state for filter");
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failure while processing IIsProperty table");

LExit:
    WcaFinishUnwrapQuery(hWrapQuery);

    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaPropertyInstall(
    IMSAdminBase* piMetabase, 
    SCA_PROPERTY* pspList
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;

    for (SCA_PROPERTY* psp = pspList; psp; psp = psp->pspNext)
    {
        // if we are installing the web site
        if (WcaIsInstalling(psp->isInstalled, psp->isAction))
        {
            hr = ScaWriteProperty(piMetabase, psp);
            ExitOnFailure1(hr, "failed to write Property '%ls' to metabase", psp->wzProperty);
        }
    }

LExit:
    return hr;
}


HRESULT ScaPropertyUninstall(
    IMSAdminBase* piMetabase, 
    SCA_PROPERTY* pspList
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;

    for (SCA_PROPERTY* psp = pspList; psp; psp = psp->pspNext)
    {
        // if we are uninstalling the web site
        if (WcaIsUninstalling(psp->isInstalled, psp->isAction))
        {
            hr = ScaRemoveProperty(piMetabase, psp);
            ExitOnFailure1(hr, "Failed to remove Property '%ls' from metabase", psp->wzProperty);
        }
    }

LExit:
    return hr;
}


HRESULT ScaWriteProperty(
    IMSAdminBase* piMetabase, 
    SCA_PROPERTY* psp
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;
    DWORD dwValue;
    LPWSTR wz = NULL;

    ExitOnNull(psp, hr, E_INVALIDARG, "Failed to write property because no property to write was given");

    //
    // Figure out what setting we're writing and write it
    //
    if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_IIS5_ISOLATION_MODE))
    {
        dwValue = 1;
        hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_STANDARD_APP_MODE_ENABLED, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
        ExitOnFailure(hr, "failed to set IIs5IsolationMode");
    }
    else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH))
    {
        dwValue = wcstoul(psp->wzValue, &wz, 10) * 1024; // remember, the value shown is in kilobytes, the value saved is in bytes
        hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_MAX_GLOBAL_BANDWIDTH, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
        ExitOnFailure(hr, "failed to set MaxGlobalBandwidth");
    }
    else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_LOG_IN_UTF8))
    {
        dwValue = 1;
        hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_LOG_IN_UTF_8, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
        ExitOnFailure(hr, "failed to set LogInUTF8");
    }
    else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_ETAG_CHANGENUMBER))
    {
        dwValue = wcstoul(psp->wzValue, &wz, 10);
        hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, /*MD_ETAG_CHANGENUMBER*/ 2039, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
        ExitOnFailure(hr, "failed to set EtagChangenumber");
    }
LExit:
    return hr;
}


HRESULT ScaRemoveProperty(
    IMSAdminBase* piMetabase, 
    SCA_PROPERTY* psp
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;
    DWORD dwValue;

    ExitOnNull(psp, hr, E_INVALIDARG, "Failed to remove property because no property to remove was given");

    if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_IIS5_ISOLATION_MODE))
    {
        dwValue = 0;
        hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_STANDARD_APP_MODE_ENABLED, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
        ExitOnFailure(hr, "failed to clear IIs5IsolationMode");
    }
    else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH))
    {
        dwValue = 0xFFFFFFFF; // This unchecks the box
        hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_MAX_GLOBAL_BANDWIDTH, METADATA_NO_ATTRIBUTES , IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
        ExitOnFailure(hr, "failed to clear MaxGlobalBandwidth");
    }
    else if (0 == lstrcmpW(psp->wzProperty, wzIISPROPERTY_LOG_IN_UTF8))
    {
        dwValue = 0;
        hr = ScaWriteMetabaseValue(piMetabase, L"/LM/W3SVC", NULL, MD_GLOBAL_LOG_IN_UTF_8, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)dwValue));
        ExitOnFailure(hr, "failed to clear LogInUTF8");
    }

LExit:
    return hr;
}


static HRESULT AddPropertyToList(
    SCA_PROPERTY** ppspList
    )
{
    HRESULT hr = S_OK;
    SCA_PROPERTY* psp = static_cast<SCA_PROPERTY*>(MemAlloc(sizeof(SCA_PROPERTY), TRUE));
    ExitOnNull(psp, hr, E_OUTOFMEMORY, "failed to allocate memory for new property list element");
    
    psp->pspNext = *ppspList;
    *ppspList = psp;
    
LExit:
    return hr;
}
