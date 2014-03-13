#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="mqqueuesched.h" company="Outercurve Foundation">
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


// structs

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
