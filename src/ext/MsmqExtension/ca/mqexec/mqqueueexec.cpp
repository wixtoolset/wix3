// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// private typedefs

typedef HRESULT (__stdcall *MQCreateQueueFunc)(PSECURITY_DESCRIPTOR, MQQUEUEPROPS*, LPWSTR, LPDWORD);
typedef HRESULT (__stdcall *MQDeleteQueueFunc)(LPCWSTR);
typedef HRESULT (__stdcall *MQPathNameToFormatNameFunc)(LPCWSTR, LPWSTR, LPDWORD);
typedef HRESULT (__stdcall *MQGetQueueSecurityFunc)(LPCWSTR, SECURITY_INFORMATION, PSECURITY_DESCRIPTOR, DWORD, LPDWORD);
typedef HRESULT (__stdcall *MQSetQueueSecurityFunc)(LPCWSTR, SECURITY_INFORMATION, PSECURITY_DESCRIPTOR);


// private enums

enum eMessageQueueAttributes
{
    mqaAuthenticate  = (1 << 0),
    mqaJournal       = (1 << 1),
    mqaTransactional = (1 << 2)
};

enum eMessageQueuePrivacyLevel
{
    mqplNone     = 0,
    mqplOptional = 1,
    mqplBody     = 2
};

enum eMessageQueuePermission
{
    mqpDeleteMessage          = (1 << 0),
    mqpPeekMessage            = (1 << 1),
    mqpWriteMessage           = (1 << 2),
    mqpDeleteJournalMessage   = (1 << 3),
    mqpSetQueueProperties     = (1 << 4),
    mqpGetQueueProperties     = (1 << 5),
    mqpDeleteQueue            = (1 << 6),
    mqpGetQueuePermissions    = (1 << 7),
    mqpChangeQueuePermissions = (1 << 8),
    mqpTakeQueueOwnership     = (1 << 9),
    mqpReceiveMessage         = (1 << 10),
    mqpReceiveJournalMessage  = (1 << 11),
    mqpQueueGenericRead       = (1 << 12),
    mqpQueueGenericWrite      = (1 << 13),
    mqpQueueGenericExecute    = (1 << 14),
    mqpQueueGenericAll        = (1 << 15)
};


// private structs

struct MQI_MESSAGE_QUEUE_ATTRIBUTES
{
    LPWSTR pwzKey;
    int iBasePriority;
    int iJournalQuota;
    LPWSTR pwzLabel;
    LPWSTR pwzMulticastAddress;
    LPWSTR pwzPathName;
    int iPrivLevel;
    int iQuota;
    LPWSTR pwzServiceTypeGuid;
    int iAttributes;
};

struct MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES
{
    LPWSTR pwzKey;
    LPWSTR pwzPathName;
    LPWSTR pwzDomain;
    LPWSTR pwzName;
    int iPermissions;
};


// prototypes for private helper functions

static HRESULT ReadMessageQueueAttributes(
    LPWSTR* ppwzData,
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    );
static void FreeMessageQueueAttributes(
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    );
static HRESULT ReadMessageQueuePermissionAttributes(
    LPWSTR* ppwzData,
    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES* pAttrs
    );
static void FreeMessageQueuePermissionAttributes(
    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES* pAttrs
    );
static HRESULT CreateMessageQueue(
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    );
static HRESULT DeleteMessageQueue(
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    );
static HRESULT SetMessageQueuePermissions(
    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES* pAttrs,
    BOOL fRevoke
    );
static void SetAccessPermissions(
    int iPermissions,
    LPDWORD pgrfAccessPermissions
    );


// private variables

static HMODULE ghMQRT;
static MQCreateQueueFunc gpfnMQCreateQueue;
static MQDeleteQueueFunc gpfnMQDeleteQueue;
static MQPathNameToFormatNameFunc gpfnMQPathNameToFormatName;
static MQGetQueueSecurityFunc gpfnMQGetQueueSecurity;
static MQSetQueueSecurityFunc gpfnMQSetQueueSecurity;


// function definitions

HRESULT MqiInitialize()
{
    HRESULT hr = S_OK;

    // load mqrt.dll
    ghMQRT = ::LoadLibraryW(L"mqrt.dll");
    ExitOnNull(ghMQRT, hr, E_FAIL, "Failed to load mqrt.dll");

    // get MQCreateQueue function address
    gpfnMQCreateQueue = (MQCreateQueueFunc)::GetProcAddress(ghMQRT, "MQCreateQueue");
    ExitOnNull(gpfnMQCreateQueue, hr, HRESULT_FROM_WIN32(::GetLastError()), "Failed get address for MQCreateQueue() function");

    // get MQDeleteQueue function address
    gpfnMQDeleteQueue = (MQDeleteQueueFunc)::GetProcAddress(ghMQRT, "MQDeleteQueue");
    ExitOnNull(gpfnMQDeleteQueue, hr, HRESULT_FROM_WIN32(::GetLastError()), "Failed get address for MQDeleteQueue() function");

    // get MQPathNameToFormatName function address
    gpfnMQPathNameToFormatName = (MQPathNameToFormatNameFunc)::GetProcAddress(ghMQRT, "MQPathNameToFormatName");
    ExitOnNull(gpfnMQPathNameToFormatName, hr, HRESULT_FROM_WIN32(::GetLastError()), "Failed get address for MQPathNameToFormatName() function");

    // get MQGetQueueSecurity function address
    gpfnMQGetQueueSecurity = (MQGetQueueSecurityFunc)::GetProcAddress(ghMQRT, "MQGetQueueSecurity");
    ExitOnNull(gpfnMQGetQueueSecurity, hr, HRESULT_FROM_WIN32(::GetLastError()), "Failed get address for MQGetQueueSecurity() function");

    // get MQSetQueueSecurity function address
    gpfnMQSetQueueSecurity = (MQSetQueueSecurityFunc)::GetProcAddress(ghMQRT, "MQSetQueueSecurity");
    ExitOnNull(gpfnMQSetQueueSecurity, hr, HRESULT_FROM_WIN32(::GetLastError()), "Failed get address for MQSetQueueSecurity() function");

    hr = S_OK;

LExit:
    return hr;
}

void MqiUninitialize()
{
    if (ghMQRT)
        ::FreeLibrary(ghMQRT);
}

HRESULT MqiCreateMessageQueues(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueueAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // progress message
        hr = PcaActionDataMessage(1, attrs.pwzPathName);
        ExitOnFailure1(hr, "Failed to send progress messages, key: %S", attrs.pwzKey);

        // create message queue
        hr = CreateMessageQueue(&attrs);
        ExitOnFailure1(hr, "Failed to create message queue, key: %S", attrs.pwzKey);

        // progress tics
        hr = WcaProgressMessage(COST_MESSAGE_QUEUE_CREATE, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueueAttributes(&attrs);

    return hr;
}

HRESULT MqiRollbackCreateMessageQueues(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueueAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // create message queue
        hr = DeleteMessageQueue(&attrs);
        if (FAILED(hr))
            WcaLog(LOGMSG_STANDARD, "Failed to delete message queue, hr: 0x%x, key: %S", hr, attrs.pwzKey);
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueueAttributes(&attrs);

    return hr;
}

HRESULT MqiDeleteMessageQueues(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueueAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // progress message
        hr = PcaActionDataMessage(1, attrs.pwzPathName);
        ExitOnFailure1(hr, "Failed to send progress messages, key: %S", attrs.pwzKey);

        // create message queue
        hr = DeleteMessageQueue(&attrs);
        if (FAILED(hr))
        {
            WcaLog(LOGMSG_STANDARD, "Failed to delete queue, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            continue;
        }

        // progress tics
        hr = WcaProgressMessage(COST_MESSAGE_QUEUE_DELETE, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueueAttributes(&attrs);

    return hr;
}

HRESULT MqiRollbackDeleteMessageQueues(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueueAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // create message queue
        hr = CreateMessageQueue(&attrs);
        if (FAILED(hr))
            WcaLog(LOGMSG_STANDARD, "Failed to create message queue, hr: 0x%x, key: %S", hr, attrs.pwzKey);
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueueAttributes(&attrs);

    return hr;
}

HRESULT MqiAddMessageQueuePermissions(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueuePermissionAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // progress message
        hr = PcaActionDataMessage(1, attrs.pwzPathName);
        ExitOnFailure(hr, "Failed to send progress messages");

        // add message queue permission
        hr = SetMessageQueuePermissions(&attrs, FALSE);
        ExitOnFailure(hr, "Failed to add message queue permission");

        // progress tics
        hr = WcaProgressMessage(COST_MESSAGE_QUEUE_PERMISSION_ADD, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueuePermissionAttributes(&attrs);

    return hr;
}

HRESULT MqiRollbackAddMessageQueuePermissions(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueuePermissionAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // add message queue permission
        hr = SetMessageQueuePermissions(&attrs, TRUE);
        if (FAILED(hr))
            WcaLog(LOGMSG_STANDARD, "Failed to rollback add message queue permission, hr: 0x%x, key: %S", hr, attrs.pwzKey);
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueuePermissionAttributes(&attrs);

    return hr;
}

HRESULT MqiRemoveMessageQueuePermissions(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueuePermissionAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // progress message
        hr = PcaActionDataMessage(1, attrs.pwzPathName);
        ExitOnFailure(hr, "Failed to send progress messages");

        // add message queue permission
        hr = SetMessageQueuePermissions(&attrs, TRUE);
        ExitOnFailure(hr, "Failed to remove message queue permission");

        // progress tics
        hr = WcaProgressMessage(COST_MESSAGE_QUEUE_PERMISSION_ADD, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueuePermissionAttributes(&attrs);

    return hr;
}

HRESULT MqiRollbackRemoveMessageQueuePermissions(
    LPWSTR* ppwzData
    )
{
    HRESULT hr = S_OK;

    int iCnt = 0;

    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // ger count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadMessageQueuePermissionAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read attributes");

        // add message queue permission
        hr = SetMessageQueuePermissions(&attrs, FALSE);
        if (FAILED(hr))
            WcaLog(LOGMSG_STANDARD, "Failed to rollback remove message queue permission, hr: 0x%x, key: %S", hr, attrs.pwzKey);
    }

    hr = S_OK;

LExit:
    // clean up
    FreeMessageQueuePermissionAttributes(&attrs);

    return hr;
}


// helper function definitions

static HRESULT ReadMessageQueueAttributes(
    LPWSTR* ppwzData,
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    // read message queue information from custom action data
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzKey);
    ExitOnFailure(hr, "Failed to read key from custom action data");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iBasePriority);
    ExitOnFailure(hr, "Failed to read base priority from custom action data");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iJournalQuota);
    ExitOnFailure(hr, "Failed to read journal quota from custom action data");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzLabel);
    ExitOnFailure(hr, "Failed to read label from custom action data");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzMulticastAddress);
    ExitOnFailure(hr, "Failed to read multicast address from custom action data");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPathName);
    ExitOnFailure(hr, "Failed to read path name from custom action data");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iPrivLevel);
    ExitOnFailure(hr, "Failed to read privacy level from custom action data");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iQuota);
    ExitOnFailure(hr, "Failed to read quota from custom action data");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzServiceTypeGuid);
    ExitOnFailure(hr, "Failed to read service type guid from custom action data");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iAttributes);
    ExitOnFailure(hr, "Failed to read attributes from custom action data");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeMessageQueueAttributes(
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzLabel);
    ReleaseStr(pAttrs->pwzMulticastAddress);
    ReleaseStr(pAttrs->pwzPathName);
    ReleaseStr(pAttrs->pwzServiceTypeGuid);
}

static HRESULT ReadMessageQueuePermissionAttributes(
    LPWSTR* ppwzData,
    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    // read message queue permission information from custom action data
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzKey);
    ExitOnFailure(hr, "Failed to read key from custom action data");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPathName);
    ExitOnFailure(hr, "Failed to read path name from custom action data");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzDomain);
    ExitOnFailure(hr, "Failed to read domain from custom action data");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzName);
    ExitOnFailure(hr, "Failed to read name from custom action data");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iPermissions);
    ExitOnFailure(hr, "Failed to read permissions from custom action data");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeMessageQueuePermissionAttributes(
    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzPathName);
    ReleaseStr(pAttrs->pwzDomain);
    ReleaseStr(pAttrs->pwzName);
}

static HRESULT CreateMessageQueue(
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    SECURITY_DESCRIPTOR sd;
    PSID pOwner = NULL;
    DWORD cbDacl = 0;
    PACL pDacl = NULL;
    QUEUEPROPID aPropID[11];
    MQPROPVARIANT aPropVar[11];
    MQQUEUEPROPS props;

    GUID guidType;

    DWORD dwFormatNameLength = 0;

    ::ZeroMemory(&sd, sizeof(sd));
    ::ZeroMemory(aPropID, sizeof(aPropID));
    ::ZeroMemory(aPropVar, sizeof(aPropVar));
    ::ZeroMemory(&props, sizeof(props));
    ::ZeroMemory(&guidType, sizeof(guidType));

    // initialize security descriptor
    if (!::InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to initialize security descriptor");

    // set security descriptor owner
    hr = PcaAccountNameToSid(L"\\Administrators", &pOwner);
    ExitOnFailure(hr, "Failed to get sid for account name");

    if (!::SetSecurityDescriptorOwner(&sd, pOwner, FALSE))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to set security descriptor owner");

    // set security descriptor DACL
    cbDacl = sizeof(ACL) + (sizeof(ACCESS_ALLOWED_ACE) - sizeof(DWORD)) + ::GetLengthSid(pOwner);
    pDacl = (PACL)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, cbDacl);
    ExitOnNull(pDacl, hr, E_OUTOFMEMORY, "Failed to allocate buffer for DACL");

    if (!::InitializeAcl(pDacl, cbDacl, ACL_REVISION))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to initialize DACL");

    if (!::AddAccessAllowedAce(pDacl, ACL_REVISION, MQSEC_QUEUE_GENERIC_ALL, pOwner))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to add ACE to DACL");

    if (!::SetSecurityDescriptorDacl(&sd, TRUE, pDacl, FALSE))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to set security descriptor DACL");

    // set property values
    props.aPropID = aPropID;
    props.aPropVar = aPropVar;

    aPropID[0] = PROPID_Q_LABEL;
    aPropVar[0].vt = VT_LPWSTR;
    aPropVar[0].pwszVal = pAttrs->pwzLabel;

    aPropID[1] = PROPID_Q_PATHNAME;
    aPropVar[1].vt = VT_LPWSTR;
    aPropVar[1].pwszVal = pAttrs->pwzPathName;

    aPropID[2] = PROPID_Q_AUTHENTICATE;
    aPropVar[2].vt = VT_UI1;
    aPropVar[2].bVal = mqaAuthenticate == (pAttrs->iAttributes & mqaAuthenticate);

    aPropID[3] = PROPID_Q_JOURNAL;
    aPropVar[3].vt = VT_UI1;
    aPropVar[3].bVal = mqaJournal == (pAttrs->iAttributes & mqaJournal);

    aPropID[4] = PROPID_Q_TRANSACTION;
    aPropVar[4].vt = VT_UI1;
    aPropVar[4].bVal = mqaTransactional == (pAttrs->iAttributes & mqaTransactional);

    props.cProp = 5;

    if (MSI_NULL_INTEGER != pAttrs->iBasePriority)
    {
        aPropID[props.cProp] = PROPID_Q_BASEPRIORITY;
        aPropVar[props.cProp].vt = VT_I2;
        aPropVar[props.cProp].iVal = (SHORT)pAttrs->iBasePriority;
        props.cProp++;
    }

    if (MSI_NULL_INTEGER != pAttrs->iJournalQuota)
    {
        aPropID[props.cProp] = PROPID_Q_JOURNAL_QUOTA;
        aPropVar[props.cProp].vt = VT_UI4;
        aPropVar[props.cProp].ulVal = (ULONG)pAttrs->iJournalQuota;
        props.cProp++;
    }

    if (*pAttrs->pwzMulticastAddress)
    {
        aPropID[props.cProp] = PROPID_Q_MULTICAST_ADDRESS;
        aPropVar[props.cProp].vt = VT_LPWSTR;
        aPropVar[props.cProp].pwszVal = pAttrs->pwzMulticastAddress;
        props.cProp++;
    }

    if (MSI_NULL_INTEGER != pAttrs->iPrivLevel)
    {
        aPropID[props.cProp] = PROPID_Q_PRIV_LEVEL;
        aPropVar[props.cProp].vt = VT_UI4;
        switch (pAttrs->iPrivLevel)
        {
        case mqplNone:
            aPropVar[props.cProp].ulVal = MQ_PRIV_LEVEL_NONE;
            break;
        case mqplBody:
            aPropVar[props.cProp].ulVal = MQ_PRIV_LEVEL_BODY;
            break;
        case mqplOptional:
            aPropVar[props.cProp].ulVal = MQ_PRIV_LEVEL_OPTIONAL;
            break;
        }
        props.cProp++;
    }

    if (MSI_NULL_INTEGER != pAttrs->iQuota)
    {
        aPropID[props.cProp] = PROPID_Q_QUOTA;
        aPropVar[props.cProp].vt = VT_UI4;
        aPropVar[props.cProp].ulVal = (ULONG)pAttrs->iQuota;
        props.cProp++;
    }

    if (*pAttrs->pwzServiceTypeGuid)
    {
        // parse guid string
        hr = PcaGuidFromString(pAttrs->pwzServiceTypeGuid, &guidType);
        ExitOnFailure(hr, "Failed to parse service type GUID string");

        aPropID[props.cProp] = PROPID_Q_TYPE;
        aPropVar[props.cProp].vt = VT_CLSID;
        aPropVar[props.cProp].puuid = &guidType;
        props.cProp++;
    }

    // create message queue
    hr = gpfnMQCreateQueue(&sd, &props, NULL, &dwFormatNameLength);
    ExitOnFailure(hr, "Failed to create message queue");

    // log
    WcaLog(LOGMSG_VERBOSE, "Message queue created, key: %S, PathName: '%S'", pAttrs->pwzKey, pAttrs->pwzPathName);

    hr = S_OK;

LExit:
    // clean up
    if (pOwner)
        ::HeapFree(::GetProcessHeap(), 0, pOwner);
    if (pDacl)
        ::HeapFree(::GetProcessHeap(), 0, pDacl);

    return hr;
}

static HRESULT DeleteMessageQueue(
    MQI_MESSAGE_QUEUE_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    LPWSTR pwzFormatName = NULL;
    DWORD dwCount = 128;

    // get format name
    hr = StrAlloc(&pwzFormatName, dwCount);
    ExitOnFailure(hr, "Failed to allocate format name string");
    do {
        hr = gpfnMQPathNameToFormatName(pAttrs->pwzPathName, pwzFormatName, &dwCount);
        switch (hr)
        {
        case MQ_ERROR_QUEUE_NOT_FOUND:
            ExitFunction1(hr = S_OK); // nothing to delete
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

    // delete queue
    hr = gpfnMQDeleteQueue(pwzFormatName);
    ExitOnFailure(hr, "Failed to delete queue");

    // log
    WcaLog(LOGMSG_VERBOSE, "Message queue deleted, key: %S, PathName: '%S'", pAttrs->pwzKey, pAttrs->pwzPathName);

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzFormatName);

    return hr;
}

static HRESULT SetMessageQueuePermissions(
    MQI_MESSAGE_QUEUE_PERMISSION_ATTRIBUTES* pAttrs,
    BOOL fRevoke
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    DWORD dw = 0;

    LPWSTR pwzAccount = NULL;
    LPWSTR pwzFormatName = NULL;

    PSECURITY_DESCRIPTOR psd = NULL;
    PSECURITY_DESCRIPTOR ptsd = NULL;

    PACL pAclExisting = NULL;
    PACL pAclNew = NULL;
    BOOL fDaclPresent = FALSE;
    BOOL fDaclDefaulted = FALSE;

    PSID psid = NULL;

    EXPLICIT_ACCESSW ea;
    SECURITY_DESCRIPTOR sdNew;

    ::ZeroMemory(&ea, sizeof(ea));
    ::ZeroMemory(&sdNew, sizeof(sdNew));

    // get format name
    dw = 128;
    hr = StrAlloc(&pwzFormatName, dw);
    ExitOnFailure(hr, "Failed to allocate format name string");
    do {
        hr = gpfnMQPathNameToFormatName(pAttrs->pwzPathName, pwzFormatName, &dw);
        if (MQ_ERROR_FORMATNAME_BUFFER_TOO_SMALL == hr)
        {
            hr = StrAlloc(&pwzFormatName, dw);
            ExitOnFailure(hr, "Failed to reallocate format name string");
            hr = S_FALSE; // retry
        }
        else
        {
            ExitOnFailure(hr, "Failed to get format name");
            hr = S_OK;
        }
    } while (S_FALSE == hr);

    // get queue security information
    dw = 256;
    psd = (PSECURITY_DESCRIPTOR)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, dw);
    ExitOnNull(psd, hr, E_OUTOFMEMORY, "Failed to allocate buffer for security descriptor");
    do {
        hr = gpfnMQGetQueueSecurity(pwzFormatName, DACL_SECURITY_INFORMATION, psd, dw, &dw);
        if (MQ_ERROR_SECURITY_DESCRIPTOR_TOO_SMALL == hr)
        {
            ptsd = (PSECURITY_DESCRIPTOR)::HeapReAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, psd, dw);
            ExitOnNull(ptsd, hr, E_OUTOFMEMORY, "Failed to reallocate buffer for security descriptor");
            psd = ptsd;
            hr = S_FALSE; // retry
        }
        else
        {
            ExitOnFailure(hr, "Failed to get queue security information");
            hr = S_OK;
        }
    } while (S_FALSE == hr);

    // get dacl
    if (!::GetSecurityDescriptorDacl(psd, &fDaclPresent, &pAclExisting, &fDaclDefaulted))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to get DACL for security descriptor");
    if (!fDaclPresent || !pAclExisting)
        ExitOnFailure(hr = E_ACCESSDENIED, "Failed to get DACL for security descriptor, access denied");

    // build account name string
    hr = PcaBuildAccountName(pAttrs->pwzDomain, pAttrs->pwzName, &pwzAccount);
    ExitOnFailure(hr, "Failed to build account name string");

    // get sid for account name
    hr = PcaAccountNameToSid(pwzAccount, &psid);
    ExitOnFailure(hr, "Failed to get SID for account name");

    // set acl entry
    SetAccessPermissions(pAttrs->iPermissions, &ea.grfAccessPermissions);
    ea.grfAccessMode = fRevoke ? REVOKE_ACCESS : SET_ACCESS;
    ea.grfInheritance = NO_INHERITANCE;
    ::BuildTrusteeWithSidW(&ea.Trustee, psid);

    er = ::SetEntriesInAclW(1, &ea, pAclExisting, &pAclNew);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set ACL entry");

    // create new security descriptor
    if (!::InitializeSecurityDescriptor(&sdNew, SECURITY_DESCRIPTOR_REVISION))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to initialize security descriptor");

    if (!::SetSecurityDescriptorDacl(&sdNew, TRUE, pAclNew, FALSE))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to set DACL for security descriptor");

    // set queue security information
    hr = gpfnMQSetQueueSecurity(pwzFormatName, DACL_SECURITY_INFORMATION, &sdNew);
    ExitOnFailure(hr, "Failed to set queue security information");

    // log
    WcaLog(LOGMSG_VERBOSE, "Permission set for message queue, key: %S, PathName: '%S'", pAttrs->pwzKey, pAttrs->pwzPathName);

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzFormatName);
    ReleaseStr(pwzAccount);

    if (psd)
        ::HeapFree(::GetProcessHeap(), 0, psd);
    if (psid)
        ::HeapFree(::GetProcessHeap(), 0, psid);
    if (pAclNew)
        ::LocalFree(pAclNew);

    return hr;
}

static void SetAccessPermissions(
    int iPermissions,
    LPDWORD pgrfAccessPermissions
    )
{
    if (iPermissions & mqpDeleteMessage)
        *pgrfAccessPermissions |= MQSEC_DELETE_MESSAGE;
    if (iPermissions & mqpPeekMessage)
        *pgrfAccessPermissions |= MQSEC_PEEK_MESSAGE;
    if (iPermissions & mqpWriteMessage)
        *pgrfAccessPermissions |= MQSEC_WRITE_MESSAGE;
    if (iPermissions & mqpDeleteJournalMessage)
        *pgrfAccessPermissions |= MQSEC_DELETE_JOURNAL_MESSAGE;
    if (iPermissions & mqpSetQueueProperties)
        *pgrfAccessPermissions |= MQSEC_SET_QUEUE_PROPERTIES;
    if (iPermissions & mqpGetQueueProperties)
        *pgrfAccessPermissions |= MQSEC_GET_QUEUE_PROPERTIES;
    if (iPermissions & mqpDeleteQueue)
        *pgrfAccessPermissions |= MQSEC_DELETE_QUEUE;
    if (iPermissions & mqpGetQueuePermissions)
        *pgrfAccessPermissions |= MQSEC_GET_QUEUE_PERMISSIONS;
    if (iPermissions & mqpChangeQueuePermissions)
        *pgrfAccessPermissions |= MQSEC_CHANGE_QUEUE_PERMISSIONS;
    if (iPermissions & mqpTakeQueueOwnership)
        *pgrfAccessPermissions |= MQSEC_TAKE_QUEUE_OWNERSHIP;
    if (iPermissions & mqpReceiveMessage)
        *pgrfAccessPermissions |= MQSEC_RECEIVE_MESSAGE;
    if (iPermissions & mqpReceiveJournalMessage)
        *pgrfAccessPermissions |= MQSEC_RECEIVE_JOURNAL_MESSAGE;
    if (iPermissions & mqpQueueGenericRead)
        *pgrfAccessPermissions |= MQSEC_QUEUE_GENERIC_READ;
    if (iPermissions & mqpQueueGenericWrite)
        *pgrfAccessPermissions |= MQSEC_QUEUE_GENERIC_WRITE;
    if (iPermissions & mqpQueueGenericExecute)
        *pgrfAccessPermissions |= MQSEC_QUEUE_GENERIC_EXECUTE;
    if (iPermissions & mqpQueueGenericAll)
        *pgrfAccessPermissions |= MQSEC_QUEUE_GENERIC_ALL;
}
