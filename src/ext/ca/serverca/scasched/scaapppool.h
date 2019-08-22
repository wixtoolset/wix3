#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "scauser.h"

// Identity
#define APATTR_NETSERVICE 0x0001 // Network Service
#define APATTR_LOCSERVICE 0x0002 // Local Service
#define APATTR_LOCSYSTEM 0x0004 // Local System
#define APATTR_OTHERUSER 0x0008 // Other User

struct SCA_APPPOOL
{
    // iis app pool configuation information
    WCHAR wzAppPool[MAX_DARWIN_KEY + 1];
    WCHAR wzName[METADATA_MAX_NAME_LEN + 1];
    WCHAR wzKey[METADATA_MAX_NAME_LEN + 1];
    WCHAR wzComponent[METADATA_MAX_NAME_LEN + 1];
    BOOL fHasComponent;
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;
    INT iAttributes;

    SCA_USER suUser;

    INT iRecycleRequests;
    INT iRecycleMinutes;
    WCHAR wzRecycleTimes[MAX_DARWIN_KEY + 1];
    INT iVirtualMemory;
    INT iPrivateMemory;

    INT iIdleTimeout;
    INT iQueueLimit;
    WCHAR wzCpuMon[MAX_DARWIN_KEY + 1];
    INT iMaxProcesses;
    WCHAR wzManagedPipelineMode[MAX_DARWIN_KEY + 1];
    WCHAR wzManagedRuntimeVersion[MAX_DARWIN_KEY + 1];

    int iCompAttributes;

    SCA_APPPOOL *psapNext;
};


// prototypes

HRESULT ScaAppPoolRead(
    __inout SCA_APPPOOL** ppsapList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __inout LPWSTR *ppwzCustomActionData
    );

void ScaAppPoolFreeList(
    __in SCA_APPPOOL* psapList
    );

HRESULT ScaFindAppPool(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzAppPool,
    __out_ecount(cchName) LPWSTR wzName,
    __in DWORD cchName,
    __in SCA_APPPOOL *psapList
    );

HRESULT ScaAppPoolInstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_APPPOOL* psapList
    );

HRESULT ScaAppPoolUninstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_APPPOOL* psapList
    );

HRESULT ScaWriteAppPool(
    __in IMSAdminBase* piMetabase,
    __in SCA_APPPOOL* psap
    );

HRESULT ScaRemoveAppPool(
    __in IMSAdminBase* piMetabase,
    __in SCA_APPPOOL* psap
    );

HRESULT AddAppPoolToList(
    __in SCA_APPPOOL** ppsapList
    );
