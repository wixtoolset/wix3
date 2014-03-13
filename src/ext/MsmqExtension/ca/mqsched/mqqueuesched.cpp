//-------------------------------------------------------------------------------------------------
// <copyright file="mqqueuesched.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    MSMQ functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// sql queries

LPCWSTR vcsMessageQueueQuery =
    L"SELECT `MessageQueue`, `Component_`, `BasePriority`, `JournalQuota`, `Label`, `MulticastAddress`, `PathName`, `PrivLevel`, `Quota`, `ServiceTypeGuid`, `Attributes` FROM `MessageQueue`";
enum eMessageQueueQuery { mqqMessageQueue = 1, mqqComponent,  mqqBasePriority, mqqJournalQuota, mqqLabel, mqqMulticastAddress, mqqPathName, mqqPrivLevel, mqqQuota, mqqServiceTypeGuid, mqqAttributes };

LPCWSTR vcsMessageQueueUserPermissionQuery =
    L"SELECT `MessageQueueUserPermission`, `MessageQueue_`, `MessageQueueUserPermission`.`Component_`, `Domain`, `Name`, `Permissions` FROM `MessageQueueUserPermission`, `User` WHERE `User_` = `User`";
LPCWSTR vcsMessageQueueGroupPermissionQuery =
    L"SELECT `MessageQueueGroupPermission`, `MessageQueue_`, `MessageQueueGroupPermission`.`Component_`, `Domain`, `Name`, `Permissions` FROM `MessageQueueGroupPermission`, `Group` WHERE `Group_` = `Group`";
enum eMessageQueuePermissionQuery { mqpqMessageQueuePermission = 1, mqpqMessageQueue, mqpqComponent, mqpqDomain, mqpqName, mqpqPermissions };


// prototypes for private helper functions

static HRESULT MqiMessageQueueFindByKey(
    MQI_MESSAGE_QUEUE_LIST* pList,
    LPCWSTR pwzKey,
    MQI_MESSAGE_QUEUE** ppItm
    );
static HRESULT AddMessageQueueToActionData(
    MQI_MESSAGE_QUEUE* pItm,
    LPWSTR* ppwzActionData
    );
static HRESULT MessageQueueTrusteePermissionsRead(
    LPCWSTR pwzQuery,
    MQI_MESSAGE_QUEUE_LIST* pMessageQueueList,
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
    );
static HRESULT AddMessageQueuePermissionToActionData(
    MQI_MESSAGE_QUEUE_PERMISSION* pItm,
    LPWSTR* ppwzActionData
    );


// private typedefs

typedef HRESULT (__stdcall *MQPathNameToFormatNameFunc)(LPCWSTR, LPWSTR, LPDWORD);


// private variables

static HMODULE ghMQRT;
static MQPathNameToFormatNameFunc gpfnMQPathNameToFormatName;


// function definitions

HRESULT MqiInitialize()
{
    HRESULT hr = S_OK;

    // load mqrt.dll
    ghMQRT = ::LoadLibraryW(L"mqrt.dll");
    if (!ghMQRT)
    {
        ExitFunction1(hr = S_FALSE);
    }

    // get MQPathNameToFormatName function address
    gpfnMQPathNameToFormatName = (MQPathNameToFormatNameFunc)::GetProcAddress(ghMQRT, "MQPathNameToFormatName");
    ExitOnNullWithLastError(gpfnMQPathNameToFormatName, hr, "Failed get address for MQPathNameToFormatName() function");

    hr = S_OK;

LExit:
    return hr;
}

void MqiUninitialize()
{
    if (ghMQRT)
    {
        ::FreeLibrary(ghMQRT);
    }
}

HRESULT MqiMessageQueueRead(
    MQI_MESSAGE_QUEUE_LIST* pList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    MQI_MESSAGE_QUEUE* pItm = NULL;
    LPWSTR pwzData = NULL;

    // loop through all partitions
    hr = WcaOpenExecuteView(vcsMessageQueueQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on MessageQueue table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (MQI_MESSAGE_QUEUE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(MQI_MESSAGE_QUEUE));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, mqqMessageQueue, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get component install state
        hr = WcaGetRecordString(hRec, mqqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get component");

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get base priority
        hr = WcaGetRecordInteger(hRec, mqqBasePriority, &pItm->iBasePriority);
        ExitOnFailure(hr, "Failed to get base priority");

        // get journal quota
        hr = WcaGetRecordInteger(hRec, mqqJournalQuota, &pItm->iJournalQuota);
        ExitOnFailure(hr, "Failed to get journal quota");

        // get label
        hr = WcaGetRecordFormattedString(hRec, mqqLabel, &pwzData);
        ExitOnFailure(hr, "Failed to get label");
        StringCchCopyW(pItm->wzLabel, countof(pItm->wzLabel), pwzData);

        // get multicast address
        hr = WcaGetRecordFormattedString(hRec, mqqMulticastAddress, &pwzData);
        ExitOnFailure(hr, "Failed to get multicast address");
        StringCchCopyW(pItm->wzMulticastAddress, countof(pItm->wzMulticastAddress), pwzData);

        // get path name
        hr = WcaGetRecordFormattedString(hRec, mqqPathName, &pwzData);
        ExitOnFailure(hr, "Failed to get path name");
        StringCchCopyW(pItm->wzPathName, countof(pItm->wzPathName), pwzData);

        // get privacy level
        hr = WcaGetRecordInteger(hRec, mqqPrivLevel, &pItm->iPrivLevel);
        ExitOnFailure(hr, "Failed to get privacy level");

        // get quota
        hr = WcaGetRecordInteger(hRec, mqqQuota, &pItm->iQuota);
        ExitOnFailure(hr, "Failed to get quota");

        // get service type guid
        hr = WcaGetRecordFormattedString(hRec, mqqServiceTypeGuid, &pwzData);
        ExitOnFailure(hr, "Failed to get service type guid");
        StringCchCopyW(pItm->wzServiceTypeGuid, countof(pItm->wzServiceTypeGuid), pwzData);

        // get attributes
        hr = WcaGetRecordInteger(hRec, mqqAttributes, &pItm->iAttributes);
        ExitOnFailure(hr, "Failed to get attributes");

        // increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            pList->iInstallCount++;
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            pList->iUninstallCount++;

        // add entry
        pItm->pNext = pList->pFirst;
        pList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        ::HeapFree(::GetProcessHeap(), 0, pItm);

    ReleaseStr(pwzData);

    return hr;
}

HRESULT MqiMessageQueueVerify(
    MQI_MESSAGE_QUEUE_LIST* pList
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzFormatName = NULL;
    DWORD dwCount = 128;

    for (MQI_MESSAGE_QUEUE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // queues that are being installed only
        if (!WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // get format name
        hr = StrAlloc(&pwzFormatName, dwCount);
        ExitOnFailure(hr, "Failed to allocate format name string");
        do {
            hr = gpfnMQPathNameToFormatName(pItm->wzPathName, pwzFormatName, &dwCount);
            switch (hr)
            {
            case MQ_ERROR_QUEUE_NOT_FOUND:
                break; // break
            case MQ_ERROR_FORMATNAME_BUFFER_TOO_SMALL:
                hr = StrAlloc(&pwzFormatName, dwCount);
                ExitOnFailure(hr, "Failed to reallocate format name string");
                hr = S_FALSE; // retry
                break;
            default:
                ExitOnFailure(hr, "Failed to get format name");
                hr = S_OK;
            }
        } while (S_FALSE == hr);

        if (MQ_ERROR_QUEUE_NOT_FOUND == hr)
        {
            continue;
        }
        pItm->fExists = TRUE;
        pList->iInstallCount--;

        // clean up
        ReleaseNullStr(pwzFormatName);
    }

    hr = S_OK;

LExit:
    ReleaseStr(pwzFormatName);
    return hr;
}

HRESULT MqiMessageQueueInstall(
    MQI_MESSAGE_QUEUE_LIST* pList,
    BOOL fRollback,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    for (MQI_MESSAGE_QUEUE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // queues that are being installed only
        if (!WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // if the queue exists we should not try to create it
        if (pItm->fExists && !fRollback)
        {
            continue;
        }

        // add message queue to action data
        hr = AddMessageQueueToActionData(pItm, ppwzActionData);
        ExitOnFailure(hr, "Failed to add message queue to action data");
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT MqiMessageQueueUninstall(
    MQI_MESSAGE_QUEUE_LIST* pList,
    BOOL fRollback,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    for (MQI_MESSAGE_QUEUE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // queues that are being uninstalled only
        if (!WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // if we did not create the queue we should not try to delete it
        if (pItm->fExists && fRollback)
        {
            continue;
        }

        // add message queue to action data
        hr = AddMessageQueueToActionData(pItm, ppwzActionData);
        ExitOnFailure(hr, "Failed to add message queue to action data");
    }

    hr = S_OK;

LExit:
    return hr;
}

void MqiMessageQueueFreeList(
    MQI_MESSAGE_QUEUE_LIST* pList
    )
{
    MQI_MESSAGE_QUEUE* pItm = pList->pFirst;
    while (pItm)
    {
        MQI_MESSAGE_QUEUE* pDelete = pItm;
        pItm = pItm->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

HRESULT MqiMessageQueuePermissionRead(
    MQI_MESSAGE_QUEUE_LIST* pMessageQueueList,
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
    )
{
    HRESULT hr = S_OK;

    // read message queue user permissions
    if (S_OK == WcaTableExists(L"MessageQueueUserPermission"))
    {
        hr = MessageQueueTrusteePermissionsRead(vcsMessageQueueUserPermissionQuery, pMessageQueueList, pList);
        ExitOnFailure(hr, "Failed to read message queue user permissions");
    }

    // read message queue group permissions
    if (S_OK == WcaTableExists(L"MessageQueueGroupPermission"))
    {
        hr = MessageQueueTrusteePermissionsRead(vcsMessageQueueGroupPermissionQuery, pMessageQueueList, pList);
        ExitOnFailure(hr, "Failed to read message queue group permissions");
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT MqiMessageQueuePermissionInstall(
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    for (MQI_MESSAGE_QUEUE_PERMISSION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // queue permissions that are being installed only
        if (!WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // add message queue permission to action data
        hr = AddMessageQueuePermissionToActionData(pItm, ppwzActionData);
        ExitOnFailure(hr, "Failed to add message queue permission to action data");
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT MqiMessageQueuePermissionUninstall(
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    for (MQI_MESSAGE_QUEUE_PERMISSION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // queue permissions that are being uninstalled only
        if (!WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // add message queue permission to action data
        hr = AddMessageQueuePermissionToActionData(pItm, ppwzActionData);
        ExitOnFailure(hr, "Failed to add message queue permission to action data");
    }

    hr = S_OK;

LExit:
    return hr;
}

void MqiMessageQueuePermissionFreeList(
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
    )
{
    MQI_MESSAGE_QUEUE_PERMISSION* pItm = pList->pFirst;
    while (pItm)
    {
        MQI_MESSAGE_QUEUE_PERMISSION* pDelete = pItm;
        pItm = pItm->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}


// helper function definitions

static HRESULT MqiMessageQueueFindByKey(
    MQI_MESSAGE_QUEUE_LIST* pList,
    LPCWSTR pwzKey,
    MQI_MESSAGE_QUEUE** ppItm
    )
{
    for (MQI_MESSAGE_QUEUE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pwzKey))
        {
            *ppItm = pItm;
            return S_OK;
        }
    }

    return S_FALSE;
}

static HRESULT AddMessageQueueToActionData(
    MQI_MESSAGE_QUEUE* pItm,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add message queue information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add key to custom action data");
    hr = WcaWriteIntegerToCaData(pItm->iBasePriority, ppwzActionData);
    ExitOnFailure(hr, "Failed to add base priority to custom action data");
    hr = WcaWriteIntegerToCaData(pItm->iJournalQuota, ppwzActionData);
    ExitOnFailure(hr, "Failed to add journal quota to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzLabel, ppwzActionData);
    ExitOnFailure(hr, "Failed to add label to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzMulticastAddress, ppwzActionData);
    ExitOnFailure(hr, "Failed to add multicast address to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzPathName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add path name to custom action data");
    hr = WcaWriteIntegerToCaData(pItm->iPrivLevel, ppwzActionData);
    ExitOnFailure(hr, "Failed to add privacy level to custom action data");
    hr = WcaWriteIntegerToCaData(pItm->iQuota, ppwzActionData);
    ExitOnFailure(hr, "Failed to add quota to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzServiceTypeGuid, ppwzActionData);
    ExitOnFailure(hr, "Failed to add service type guid to custom action data");
    hr = WcaWriteIntegerToCaData(pItm->iAttributes, ppwzActionData);
    ExitOnFailure(hr, "Failed to add attributes to custom action data");

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT MessageQueueTrusteePermissionsRead(
    LPCWSTR pwzQuery,
    MQI_MESSAGE_QUEUE_LIST* pMessageQueueList,
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    LPWSTR pwzData = NULL;

    MQI_MESSAGE_QUEUE_PERMISSION* pItm = NULL;

    // loop through all application roles
    hr = WcaOpenExecuteView(pwzQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (MQI_MESSAGE_QUEUE_PERMISSION*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(MQI_MESSAGE_QUEUE_PERMISSION));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, mqpqMessageQueuePermission, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get component
        hr = WcaGetRecordString(hRec, mqpqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get component");

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get message queue
        hr = WcaGetRecordString(hRec, mqpqMessageQueue, &pwzData);
        ExitOnFailure(hr, "Failed to get application role");

        hr = MqiMessageQueueFindByKey(pMessageQueueList, pwzData, &pItm->pMessageQueue);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        ExitOnFailure1(hr, "Failed to find message queue, key: %S", pwzData);

        // get user domain
        hr = WcaGetRecordFormattedString(hRec, mqpqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get domain");
        StringCchCopyW(pItm->wzDomain, countof(pItm->wzDomain), pwzData);

        // get user name
        hr = WcaGetRecordFormattedString(hRec, mqpqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // get permissions
        hr = WcaGetRecordInteger(hRec, mqpqPermissions, &pItm->iPermissions);
        ExitOnFailure(hr, "Failed to get permissions");

        // set references & increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            pList->iInstallCount++;
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            pList->iUninstallCount++;

        // add entry
        if (pList->pFirst)
            pItm->pNext = pList->pFirst;
        pList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzData);

    if (pItm)
        ::HeapFree(::GetProcessHeap(), 0, pItm);

    return hr;
}

static HRESULT AddMessageQueuePermissionToActionData(
    MQI_MESSAGE_QUEUE_PERMISSION* pItm,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add message queue information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add key to custom action data");
    hr = WcaWriteStringToCaData(pItm->pMessageQueue->wzPathName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add path name to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzDomain, ppwzActionData);
    ExitOnFailure(hr, "Failed to add domain to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add name to custom action data");
    hr = WcaWriteIntegerToCaData(pItm->iPermissions, ppwzActionData);
    ExitOnFailure(hr, "Failed to add permissions to custom action data");

    hr = S_OK;

LExit:
    return hr;
}
