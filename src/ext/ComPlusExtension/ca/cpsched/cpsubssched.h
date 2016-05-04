#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct CPI_SUBSCRIPTION
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzID[CPI_MAX_GUID + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];
    WCHAR wzEventCLSID[CPI_MAX_GUID + 1];
    WCHAR wzPublisherID[CPI_MAX_GUID + 1];

    BOOL fObjectNotFound;

    int iPropertyCount;
    CPI_PROPERTY* pProperties;

    INSTALLSTATE isInstalled, isAction;

    CPI_ASSEMBLY* pAssembly;
    CPI_COMPONENT* pComponent;

    CPI_SUBSCRIPTION* pNext;
};

struct CPI_SUBSCRIPTION_LIST
{
    CPI_SUBSCRIPTION* pFirst;

    int iInstallCount;
    int iCommitCount;
    int iUninstallCount;
};


// function prototypes

void CpiSubscriptionListFree(
    CPI_SUBSCRIPTION_LIST* pList
    );
HRESULT CpiSubscriptionsRead(
    CPI_ASSEMBLY_LIST* pAsmList,
    CPI_SUBSCRIPTION_LIST* pSubList
    );
HRESULT CpiSubscriptionsVerifyInstall(
    CPI_SUBSCRIPTION_LIST* pList
    );
HRESULT CpiSubscriptionsVerifyUninstall(
    CPI_SUBSCRIPTION_LIST* pList
    );
HRESULT CpiSubscriptionsInstall(
    CPI_SUBSCRIPTION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiSubscriptionsUninstall(
    CPI_SUBSCRIPTION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
