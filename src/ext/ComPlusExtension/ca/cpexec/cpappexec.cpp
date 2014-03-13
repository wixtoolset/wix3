//-------------------------------------------------------------------------------------------------
// <copyright file="cpappexec.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ application functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// private structs

struct CPI_APPLICATION_ATTRIBUTES
{
    int iActionType;
    int iActionCost;
    LPWSTR pwzKey;
    LPWSTR pwzID;
    LPWSTR pwzName;
    LPWSTR pwzPartID;
    CPI_PROPERTY* pPropList;
};


// prototypes for private helper functions

static HRESULT ReadApplicationAttributes(
    LPWSTR* ppwzData,
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    );
static void FreeApplicationAttributes(
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    );
static HRESULT CreateApplication(
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    );
static HRESULT RemoveApplication(
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    );


// function definitions

HRESULT CpiConfigureApplications(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    )
{
    HRESULT hr = S_OK;

    CPI_APPLICATION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, FALSE);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // write count to rollback file
    hr = CpiWriteIntegerToRollbackFile(hRollbackFile, iCnt);
    ExitOnFailure(hr, "Failed to write count to rollback file");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadApplicationAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // progress message
        hr = CpiActionDataMessage(1, attrs.pwzName);
        ExitOnFailure(hr, "Failed to send progress messages");

        if (S_FALSE == hr)
            ExitFunction();

        // write key to rollback file
        hr = CpiWriteKeyToRollbackFile(hRollbackFile, attrs.pwzKey);
        ExitOnFailure(hr, "Failed to write key to rollback file");

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = CreateApplication(&attrs);
            ExitOnFailure1(hr, "Failed to create application, key: %S", attrs.pwzKey);
            break;
        case atRemove:
            hr = RemoveApplication(&attrs);
            ExitOnFailure1(hr, "Failed to remove application, key: %S", attrs.pwzKey);
            break;
        }

        // write completion status to rollback file
        hr = CpiWriteIntegerToRollbackFile(hRollbackFile, 1);
        ExitOnFailure(hr, "Failed to write completion status to rollback file");

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeApplicationAttributes(&attrs);

    return hr;
}

HRESULT CpiRollbackConfigureApplications(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    )
{
    HRESULT hr = S_OK;

    int iRollbackStatus;

    CPI_APPLICATION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, NULL == pRollbackDataList);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadApplicationAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // rollback status
        hr = CpiFindRollbackStatus(pRollbackDataList, attrs.pwzKey, &iRollbackStatus);

        if (S_FALSE == hr)
            continue; // not found, nothing to rollback

        // progress message
        hr = CpiActionDataMessage(1, attrs.pwzName);
        ExitOnFailure(hr, "Failed to send progress messages");

        if (S_FALSE == hr)
            ExitFunction();

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = CreateApplication(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to create application, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            break;
        case atRemove:
            hr = RemoveApplication(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to remove application, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            break;
        }

        // check rollback status
        if (0 == iRollbackStatus)
            continue; // operation did not complete, skip progress

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeApplicationAttributes(&attrs);

    return hr;
}


// helper function definitions

static HRESULT ReadApplicationAttributes(
    LPWSTR* ppwzData,
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionType);
    ExitOnFailure(hr, "Failed to read action type");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionCost);
    ExitOnFailure(hr, "Failed to read action cost");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzKey);
    ExitOnFailure(hr, "Failed to read key");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzID);
    ExitOnFailure(hr, "Failed to read id");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzName);
    ExitOnFailure(hr, "Failed to read name");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPartID);
    ExitOnFailure(hr, "Failed to read partition id");
    hr = CpiReadPropertyList(ppwzData, &pAttrs->pPropList);
    ExitOnFailure(hr, "Failed to read properties");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeApplicationAttributes(
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzID);
    ReleaseStr(pAttrs->pwzName);
    ReleaseStr(pAttrs->pwzPartID);

    if (pAttrs->pPropList)
        CpiFreePropertyList(pAttrs->pPropList);
}

static HRESULT CreateApplication(
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piAppColl = NULL;
    ICatalogObject* piAppObj = NULL;

    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Creating application, key: %S", pAttrs->pwzKey);

    // get applications collection
    hr = CpiGetApplicationsCollection(pAttrs->pwzPartID, &piAppColl);
    if (S_FALSE == hr)
        hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
    ExitOnFailure(hr, "Failed to get applications collection");

    // check if application exists
    hr = CpiFindCollectionObjectByStringKey(piAppColl, pAttrs->pwzID, &piAppObj);
    ExitOnFailure(hr, "Failed to find application");

    if (S_FALSE == hr)
    {
        // create application
        hr = CpiAddCollectionObject(piAppColl, &piAppObj);
        ExitOnFailure(hr, "Failed to add application to collection");

        hr = CpiPutCollectionObjectValue(piAppObj, L"ID", pAttrs->pwzID);
        ExitOnFailure(hr, "Failed to set application id property");

        hr = CpiPutCollectionObjectValue(piAppObj, L"Name", pAttrs->pwzName);
        ExitOnFailure(hr, "Failed to set application name property");

        // save changes
        hr = piAppColl->SaveChanges(&lChanges);
        if (COMADMIN_E_OBJECTERRORS == hr)
            CpiLogCatalogErrorInfo();
        ExitOnFailure(hr, "Failed to add application");
    }

    // properties
    hr = CpiPutCollectionObjectValues(piAppObj, pAttrs->pPropList);
    ExitOnFailure(hr, "Failed to write properties");

    // save changes
    hr = piAppColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppColl);
    ReleaseObject(piAppObj);

    return hr;
}

static HRESULT RemoveApplication(
    CPI_APPLICATION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piAppColl = NULL;

    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Removing application, key: %S", pAttrs->pwzKey);

    // get applications collection
    hr = CpiGetApplicationsCollection(pAttrs->pwzPartID, &piAppColl);
    ExitOnFailure(hr, "Failed to get applications collection");

    if (S_FALSE == hr)
    {
        // applications collection not found
        WcaLog(LOGMSG_VERBOSE, "Unable to retrieve applications collection, nothing to delete, key: %S", pAttrs->pwzKey);
        ExitFunction1(hr = S_OK);
    }

    // remove
    hr = CpiRemoveCollectionObject(piAppColl, pAttrs->pwzID, NULL, TRUE);
    ExitOnFailure(hr, "Failed to remove application");

    if (S_FALSE == hr)
    {
        // application not found
        WcaLog(LOGMSG_VERBOSE, "Application not found, nothing to delete, key: %S", pAttrs->pwzKey);
        ExitFunction1(hr = S_OK);
    }

    // save changes
    hr = piAppColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppColl);

    return hr;
}
