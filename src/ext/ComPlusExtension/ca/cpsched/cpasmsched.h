#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpasmsched.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ assembly functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// constants

enum eAssemblyAttributes
{
    aaEventClass     = (1 << 0),
    aaDotNetAssembly = (1 << 1),
    aaPathFromGAC    = (1 << 2),
    aaRunInCommit    = (1 << 3)
};


// structs

struct CPI_ROLE_ASSIGNMENT
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];

    INSTALLSTATE isInstalled, isAction;

    CPI_APPLICATION_ROLE* pApplicationRole;

    CPI_ROLE_ASSIGNMENT* pNext;
};

struct CPI_METHOD
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzIndex[11 + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];

    int iPropertyCount;
    CPI_PROPERTY* pProperties;

    int iRoleInstallCount;
    int iRoleUninstallCount;
    CPI_ROLE_ASSIGNMENT* pRoles;

    CPI_METHOD* pNext;
};

struct CPI_INTERFACE
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzIID[CPI_MAX_GUID + 1];

    int iPropertyCount;
    CPI_PROPERTY* pProperties;

    int iRoleInstallCount;
    int iRoleUninstallCount;
    CPI_ROLE_ASSIGNMENT* pRoles;

    int iMethodCount;
    CPI_METHOD* pMethods;

    CPI_INTERFACE* pNext;
};

struct CPI_COMPONENT
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzCLSID[CPI_MAX_GUID + 1];

    int iPropertyCount;
    CPI_PROPERTY* pProperties;

    int iRoleInstallCount;
    int iRoleUninstallCount;
    CPI_ROLE_ASSIGNMENT* pRoles;

    int iInterfaceCount;
    CPI_INTERFACE* pInterfaces;

    ICatalogCollection* piSubsColl;

    CPI_COMPONENT* pNext;
};

struct CPI_ASSEMBLY
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzModule[MAX_DARWIN_KEY + 1];
    LPWSTR pwzAssemblyName;
    LPWSTR pwzDllPath;
    LPWSTR pwzTlbPath;
    LPWSTR pwzPSDllPath;
    int iAttributes;

    int iComponentCount;
    CPI_COMPONENT* pComponents;

    BOOL fReferencedForInstall;
    BOOL fReferencedForUninstall;
    BOOL fIgnore;

    int iRoleAssignmentsInstallCount;
    int iRoleAssignmentsUninstallCount;

    INSTALLSTATE isInstalled, isAction;

    CPI_APPLICATION* pApplication;

    CPI_ASSEMBLY* pPrev;
    CPI_ASSEMBLY* pNext;
};

struct CPI_ASSEMBLY_LIST
{
    CPI_ASSEMBLY* pFirst;
    CPI_ASSEMBLY* pLast;

    int iInstallCount;
    int iCommitCount;
    int iUninstallCount;

    int iRoleInstallCount;
    int iRoleCommitCount;
    int iRoleUninstallCount;
};


// function prototypes

void CpiAssemblyListFree(
    CPI_ASSEMBLY_LIST* pList
    );
HRESULT CpiAssembliesRead(
    CPI_APPLICATION_LIST* pAppList,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY_LIST* pAsmList
    );
HRESULT CpiAssembliesVerifyInstall(
    CPI_ASSEMBLY_LIST* pList
    );
HRESULT CpiAssembliesVerifyUninstall(
    CPI_ASSEMBLY_LIST* pList
    );
HRESULT CpiAssembliesInstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiAssembliesUninstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiRoleAssignmentsInstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiRoleAssignmentsUninstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiGetSubscriptionsCollForComponent(
    CPI_ASSEMBLY* pAsm,
    CPI_COMPONENT* pComp,
    ICatalogCollection** ppiSubsColl
    );
