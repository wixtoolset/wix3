#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cppartrolesched.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ partition role functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// structs

struct CPI_PARTITION_ROLE
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];

    CPI_PARTITION* pPartition;

    ICatalogCollection* piUsersColl;

    CPI_PARTITION_ROLE* pNext;
};

struct CPI_PARTITION_ROLE_LIST
{
    CPI_PARTITION_ROLE* pFirst;
};

struct CPI_USER_IN_PARTITION_ROLE
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    LPWSTR pwzAccount;

    INSTALLSTATE isInstalled, isAction;

    CPI_PARTITION_ROLE* pPartitionRole;

    CPI_USER_IN_PARTITION_ROLE* pNext;
};

struct CPI_USER_IN_PARTITION_ROLE_LIST
{
    CPI_USER_IN_PARTITION_ROLE* pFirst;

    int iInstallCount;
    int iUninstallCount;
};


// function prototypes

void CpiPartitionRoleListFree(
    CPI_PARTITION_ROLE_LIST* pList
    );
HRESULT CpiPartitionRolesRead(
    CPI_PARTITION_LIST* pPartList,
    CPI_PARTITION_ROLE_LIST* pPartRoleList
    );
HRESULT CpiPartitionRoleFindByKey(
    CPI_PARTITION_ROLE_LIST* pList,
    LPCWSTR pwzKey,
    CPI_PARTITION_ROLE** ppPartRole
    );

void CpiUserInPartitionRoleListFree(
    CPI_USER_IN_PARTITION_ROLE_LIST* pList
    );
HRESULT CpiUsersInPartitionRolesRead(
    CPI_PARTITION_ROLE_LIST* pPartRoleList,
    CPI_USER_IN_PARTITION_ROLE_LIST* pUsrInPartRoleList
    );
HRESULT CpiUsersInPartitionRolesInstall(
    CPI_USER_IN_PARTITION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
HRESULT CpiUsersInPartitionRolesUninstall(
    CPI_USER_IN_PARTITION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    );
