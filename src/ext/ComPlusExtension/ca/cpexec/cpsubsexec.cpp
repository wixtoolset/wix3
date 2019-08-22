// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// private structs

struct CPI_SUBSCRIPTION_ATTRIBUTES
{
    int iActionType;
    int iActionCost;
    LPWSTR pwzKey;
    LPWSTR pwzID;
    LPWSTR pwzName;
    LPWSTR pwzEventCLSID;
    LPWSTR pwzPublisherID;
    LPWSTR pwzCompCLSID;
    LPWSTR pwzAppID;
    LPWSTR pwzPartID;
    CPI_PROPERTY* pPropList;
};


// prototypes for private helper functions

static HRESULT ReadSubscriptionAttributes(
    LPWSTR* ppwzData,
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
    );
static void FreeSubscriptionAttributes(
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
    );
static HRESULT CreateSubscription(
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
    );
static HRESULT RemoveSubscription(
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
    );


// function definitions

HRESULT CpiConfigureSubscriptions(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    )
{
    HRESULT hr = S_OK;

    CPI_SUBSCRIPTION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, FALSE);
    ExitOnFailure(hr, "Failed to send action start message");

    // ger count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // write count to rollback file
    hr = CpiWriteIntegerToRollbackFile(hRollbackFile, iCnt);
    ExitOnFailure(hr, "Failed to write count to rollback file");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadSubscriptionAttributes(ppwzData, &attrs);
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
            hr = CreateSubscription(&attrs);
            ExitOnFailure1(hr, "Failed to create subscription, key: %S", attrs.pwzKey);
            break;
        case atRemove:
            hr = RemoveSubscription(&attrs);
            ExitOnFailure1(hr, "Failed to remove subscription, key: %S", attrs.pwzKey);
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
    FreeSubscriptionAttributes(&attrs);

    return hr;
}

HRESULT CpiRollbackConfigureSubscriptions(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    )
{
    HRESULT hr = S_OK;

    int iRollbackStatus;

    CPI_SUBSCRIPTION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, NULL == pRollbackDataList);
    ExitOnFailure(hr, "Failed to send action start message");

    // ger count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadSubscriptionAttributes(ppwzData, &attrs);
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
            hr = CreateSubscription(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to create subscription, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            break;
        case atRemove:
            hr = RemoveSubscription(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to remove subscription, hr: 0x%x, key: %S", hr, attrs.pwzKey);
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
    FreeSubscriptionAttributes(&attrs);

    return hr;
}


// helper function definitions

static HRESULT ReadSubscriptionAttributes(
    LPWSTR* ppwzData,
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
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
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzEventCLSID);
    ExitOnFailure(hr, "Failed to read event clsid");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPublisherID);
    ExitOnFailure(hr, "Failed to read publisher id");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzCompCLSID);
    ExitOnFailure(hr, "Failed to read component clsid");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzAppID);
    ExitOnFailure(hr, "Failed to read application id");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPartID);
    ExitOnFailure(hr, "Failed to read partition id");

    hr = CpiReadPropertyList(ppwzData, &pAttrs->pPropList);
    ExitOnFailure(hr, "Failed to read properties");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeSubscriptionAttributes(
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzID);
    ReleaseStr(pAttrs->pwzName);
    ReleaseStr(pAttrs->pwzEventCLSID);
    ReleaseStr(pAttrs->pwzPublisherID);
    ReleaseStr(pAttrs->pwzCompCLSID);
    ReleaseStr(pAttrs->pwzAppID);
    ReleaseStr(pAttrs->pwzPartID);

    if (pAttrs->pPropList)
        CpiFreePropertyList(pAttrs->pPropList);
}

static HRESULT CreateSubscription(
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ICatalogCollection* piSubsColl = NULL;
    ICatalogObject* piSubsObj = NULL;

    PSID pSid = NULL;
    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Creating subscription, key: %S", pAttrs->pwzKey);

    // get subscriptions collection
    hr = CpiGetSubscriptionsCollection(pAttrs->pwzPartID, pAttrs->pwzAppID, pAttrs->pwzCompCLSID, &piSubsColl);
    if (S_FALSE == hr)
        hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
    ExitOnFailure(hr, "Failed to get subscriptions collection");

    // check if subscription exists
    hr = CpiFindCollectionObjectByStringKey(piSubsColl, pAttrs->pwzID, &piSubsObj);
    ExitOnFailure(hr, "Failed to find subscription");

    if (S_FALSE == hr)
    {
        // create subscription
        hr = CpiAddCollectionObject(piSubsColl, &piSubsObj);
        ExitOnFailure(hr, "Failed to add subscription to collection");

        hr = CpiPutCollectionObjectValue(piSubsObj, L"ID", pAttrs->pwzID);
        ExitOnFailure(hr, "Failed to set subscription id property");

        hr = CpiPutCollectionObjectValue(piSubsObj, L"Name", pAttrs->pwzName);
        ExitOnFailure(hr, "Failed to set subscription name property");

        if (pAttrs->pwzEventCLSID && *pAttrs->pwzEventCLSID)
        {
            hr = CpiPutCollectionObjectValue(piSubsObj, L"EventCLSID", pAttrs->pwzEventCLSID);
            ExitOnFailure(hr, "Failed to set role event clsid property");
        }

        if (pAttrs->pwzPublisherID && *pAttrs->pwzPublisherID)
        {
            hr = CpiPutCollectionObjectValue(piSubsObj, L"PublisherID", pAttrs->pwzPublisherID);
            ExitOnFailure(hr, "Failed to set role publisher id property");
        }
    }

    // properties
    for (CPI_PROPERTY* pItm = pAttrs->pPropList; pItm; pItm = pItm->pNext)
    {
        // UserName property
        if (0 == lstrcmpW(pItm->wzName, L"UserName"))
        {
            // get SID for account
            do {
                er = ERROR_SUCCESS;
                hr = CpiAccountNameToSid(pItm->pwzValue, &pSid);
                if (!::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_ROLLBACK))
                {
                    if (HRESULT_FROM_WIN32(ERROR_NONE_MAPPED) == hr)
                    {
                        WcaLog(LOGMSG_STANDARD, "Failed to lookup account name, hr: 0x%x, account: '%S'", hr, pItm->pwzValue);
                        er = WcaErrorMessage(msierrComPlusFailedLookupNames, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                        switch (er)
                        {
                        case IDABORT:
                            ExitFunction(); // exit with error code from CpiAccountNameToSid()
                        case IDRETRY:
                            break;
                        case IDIGNORE:
                        default:
                            hr = S_FALSE;
                        }
                    }
                    else
                        ExitOnFailure1(hr, "Failed to get SID for account, account: '%S'", pItm->pwzValue);
                }
                else if (FAILED(hr))
                {
                    WcaLog(LOGMSG_STANDARD, "Failed to get SID for account, hr: 0x%x, account: '%S'", hr, pItm->pwzValue);
                    hr = S_FALSE;
                }
            } while (IDRETRY == er);

            if (S_FALSE == hr)
                continue;

            // convert SID back to account name
            hr = CpiSidToAccountName(pSid, &pItm->pwzValue);
            ExitOnFailure(hr, "Failed to convert SID to account name");
        }

        // set property
        hr = CpiPutCollectionObjectValue(piSubsObj, pItm->wzName, pItm->pwzValue);
        ExitOnFailure1(hr, "Failed to set object property value, name: %S", pItm->wzName);
    }

    // save changes
    hr = piSubsColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piSubsColl);
    ReleaseObject(piSubsObj);

    if (pSid)
        ::HeapFree(::GetProcessHeap(), 0, pSid);

    return hr;
}

static HRESULT RemoveSubscription(
    CPI_SUBSCRIPTION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piSubsColl = NULL;

    long lChanges = 0;

    // log
    WcaLog(LOGMSG_VERBOSE, "Removing subscription, key: %S", pAttrs->pwzKey);

    // get subscriptions collection
    hr = CpiGetSubscriptionsCollection(pAttrs->pwzPartID, pAttrs->pwzAppID, pAttrs->pwzCompCLSID, &piSubsColl);
    ExitOnFailure(hr, "Failed to get subscriptions collection");

    if (S_FALSE == hr)
    {
        // subscription not found
        WcaLog(LOGMSG_VERBOSE, "Unable to retrieve subscriptions collection, nothing to delete, key: %S", pAttrs->pwzKey);
        ExitFunction1(hr = S_OK);
    }

    // remove
    hr = CpiRemoveCollectionObject(piSubsColl, pAttrs->pwzID, NULL, FALSE);
    ExitOnFailure(hr, "Failed to remove subscriptions");

    // save changes
    hr = piSubsColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    // log
    WcaLog(LOGMSG_VERBOSE, "%d changes saved to catalog, key: %S", lChanges, pAttrs->pwzKey);

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piSubsColl);

    return hr;
}
