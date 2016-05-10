#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct MQI_MESSAGE_QUEUE
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    int iBasePriority;
    int iJournalQuota;
    WCHAR wzLabel[MAX_DARWIN_COLUMN + 1];
    WCHAR wzMulticastAddress[MAX_DARWIN_COLUMN + 1];
    WCHAR wzPathName[MAX_DARWIN_COLUMN + 1];
    int iPrivLevel;
    int iQuota;
    WCHAR wzServiceTypeGuid[MAX_DARWIN_COLUMN + 1];
    int iAttributes;

    INSTALLSTATE isInstalled, isAction;
    BOOL fExists;

    MQI_MESSAGE_QUEUE* pNext;
};

struct MQI_MESSAGE_QUEUE_LIST
{
    MQI_MESSAGE_QUEUE* pFirst;

    int iInstallCount;
    int iUninstallCount;
};

struct MQI_MESSAGE_QUEUE_PERMISSION
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzDomain[MAX_DARWIN_COLUMN + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];
    int iPermissions;

    MQI_MESSAGE_QUEUE* pMessageQueue;

    INSTALLSTATE isInstalled, isAction;

    MQI_MESSAGE_QUEUE_PERMISSION* pNext;
};

struct MQI_MESSAGE_QUEUE_PERMISSION_LIST
{
    MQI_MESSAGE_QUEUE_PERMISSION* pFirst;

    int iInstallCount;
    int iUninstallCount;
};


// function prototypes

HRESULT MqiInitialize();
void MqiUninitialize();
HRESULT MqiMessageQueueRead(
    MQI_MESSAGE_QUEUE_LIST* pList
    );
HRESULT MqiMessageQueueVerify(
    MQI_MESSAGE_QUEUE_LIST* pList
    );
HRESULT MqiMessageQueueInstall(
    MQI_MESSAGE_QUEUE_LIST* pList,
    BOOL fRollback,
    LPWSTR* ppwzActionData
    );
HRESULT MqiMessageQueueUninstall(
    MQI_MESSAGE_QUEUE_LIST* pList,
    BOOL fRollback,
    LPWSTR* ppwzActionData
    );
void MqiMessageQueueFreeList(
    MQI_MESSAGE_QUEUE_LIST* pList
    );
HRESULT MqiMessageQueuePermissionRead(
    MQI_MESSAGE_QUEUE_LIST* pMessageQueueList,
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
    );
HRESULT MqiMessageQueuePermissionInstall(
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList,
    LPWSTR* ppwzActionData
    );
HRESULT MqiMessageQueuePermissionUninstall(
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList,
    LPWSTR* ppwzActionData
    );
void MqiMessageQueuePermissionFreeList(
    MQI_MESSAGE_QUEUE_PERMISSION_LIST* pList
    );
