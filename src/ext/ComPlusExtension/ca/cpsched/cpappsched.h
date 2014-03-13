#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpappsched.h" company="Outercurve Foundation">
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


// structs

struct CPI_APPLICATION
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzID[CPI_MAX_GUID + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];

    int iPropertyCount;
    CPI_PROPERTY* pProperties;

    BOOL fHasComponent;
    BOOL fReferencedForInstall;
    BOOL fReferencedForUninstall;
    BOOL fObjectNotFound;

    INSTALLSTATE isInstalled, isAction;

    CPI_PARTITION* pPartition;

    ICatalogCollection* piRolesColl;
    ICatalogCollection* piCompsColl;

    CPI_APPLICATION* pNext;
};

struct CPI_APPLICATION_LIST
{
    CPI_APPLICATION* pFirst;

    int iInstallCount;
    int iUninstallCount;
};


// function prototypes

void CpiApplicationListFree(
    CPI_APPLICATION_LIST* pList
    );
HRESULT CpiApplicationsRead(
    CPI_PARTITION_LIST* pPartList,
    CPI_APPLICATION_LIST* pAppList
    );
HRESULT CpiApplicationsVerifyInstall(
    CPI_APPLICATION_LIST* pList
    );
HRESULT CpiApplicationsVerifyUninstall(
    CPI_APPLICATION_LIST* pList
    );
void CpiApplicationAddReferenceInstall(
    CPI_APPLICATION* pItm
    );
void CpiApplicationAddReferenceUninstall(
    CPI_APPLICATION* pItm
    );
HRESULT CpiApplicationsInstall(
    CPI_APPLICATION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiApplicationsUninstall(
    CPI_APPLICATION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiApplicationFindByKey(
    CPI_APPLICATION_LIST* pList,
    LPCWSTR pwzKey,
    CPI_APPLICATION** ppApp
    );
HRESULT CpiGetRolesCollForApplication(
    CPI_APPLICATION* pApp,
    ICatalogCollection** ppiRolesColl
    );
HRESULT CpiGetComponentsCollForApplication(
    CPI_APPLICATION* pApp,
    ICatalogCollection** ppiCompsColl
    );
