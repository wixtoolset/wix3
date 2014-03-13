//-------------------------------------------------------------------------------------------------
// <copyright file="cppartrolesched.cpp" company="Outercurve Foundation">
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

#include "precomp.h"


// sql queries

LPCWSTR vcsPartitionRoleQuery =
    L"SELECT `PartitionRole`, `Partition_`, `Component_`, `Name` FROM `ComPlusPartitionRole`";
enum ePartitionRoleQuery { prqPartitionRole = 1, prqPartition, prqComponent, prqName };

LPCWSTR vcsUserInPartitionRoleQuery =
    L"SELECT `UserInPartitionRole`, `PartitionRole_`, `ComPlusUserInPartitionRole`.`Component_`, `Domain`, `Name` FROM `ComPlusUserInPartitionRole`, `User` WHERE `User_` = `User`";
LPCWSTR vcsGroupInPartitionRoleQuery =
    L"SELECT `GroupInPartitionRole`, `PartitionRole_`, `ComPlusGroupInPartitionRole`.`Component_`, `Domain`, `Name` FROM `ComPlusGroupInPartitionRole`, `Group` WHERE `Group_` = `Group`";
enum eTrusteeInPartitionRoleQuery { tiprqUserInPartitionRole = 1, tiprqPartitionRole, tiprqComponent, tiprqDomain, tiprqName };


// prototypes for private helper functions

static HRESULT TrusteesInPartitionRolesRead(
    LPCWSTR pwzQuery,
    CPI_PARTITION_ROLE_LIST* pPartRoleList,
    CPI_USER_IN_PARTITION_ROLE_LIST* pUsrInPartRoleList
    );
static void FreePartitionRole(
    CPI_PARTITION_ROLE* pItm
    );
static void FreeUserInPartitionRole(
    CPI_USER_IN_PARTITION_ROLE* pItm
    );
static HRESULT AddUserInPartitionRoleToActionData(
    CPI_USER_IN_PARTITION_ROLE* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );


// function definitions

void CpiPartitionRoleListFree(
    CPI_PARTITION_ROLE_LIST* pList
    )
{
    CPI_PARTITION_ROLE* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_PARTITION_ROLE* pDelete = pItm;
        pItm = pItm->pNext;
        FreePartitionRole(pDelete);
    }
}

HRESULT CpiPartitionRolesRead(
    CPI_PARTITION_LIST* pPartList,
    CPI_PARTITION_ROLE_LIST* pPartRoleList
    )
{
    HRESULT hr = S_OK;
    PMSIHANDLE hView, hRec;
    CPI_PARTITION_ROLE* pItm = NULL;
    LPWSTR pwzData = NULL;

    // loop through all application roles
    hr = WcaOpenExecuteView(vcsPartitionRoleQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ComPlusPartitionRole table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (CPI_PARTITION_ROLE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_PARTITION_ROLE));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, prqPartitionRole, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get partition
        hr = WcaGetRecordString(hRec, prqPartition, &pwzData);
        ExitOnFailure(hr, "Failed to get application");

        hr = CpiPartitionFindByKey(pPartList, pwzData, &pItm->pPartition);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        ExitOnFailure1(hr, "Failed to find partition, key: %S", pwzData);

        // get name
        hr = WcaGetRecordFormattedString(hRec, prqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // add entry
        if (pPartRoleList->pFirst)
            pItm->pNext = pPartRoleList->pFirst;
        pPartRoleList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreePartitionRole(pItm);

    ReleaseStr(pwzData);

    return hr;
}

HRESULT CpiPartitionRoleFindByKey(
    CPI_PARTITION_ROLE_LIST* pList,
    LPCWSTR pwzKey,
    CPI_PARTITION_ROLE** ppPartRole
    )
{
    for (CPI_PARTITION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pwzKey))
        {
            *ppPartRole = pItm;
            return S_OK;
        }
    }

    return E_FAIL;
}

void CpiUserInPartitionRoleListFree(
    CPI_USER_IN_PARTITION_ROLE_LIST* pList
    )
{
    CPI_USER_IN_PARTITION_ROLE* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_USER_IN_PARTITION_ROLE* pDelete = pItm;
        pItm = pItm->pNext;
        FreeUserInPartitionRole(pDelete);
    }
}

HRESULT CpiUsersInPartitionRolesRead(
    CPI_PARTITION_ROLE_LIST* pPartRoleList,
    CPI_USER_IN_PARTITION_ROLE_LIST* pUsrInPartRoleList
    )
{
    HRESULT hr = S_OK;

    // read users in partition roles
    if (CpiTableExists(cptComPlusUserInPartitionRole))
    {
        hr = TrusteesInPartitionRolesRead(vcsUserInPartitionRoleQuery, pPartRoleList, pUsrInPartRoleList);
        ExitOnFailure(hr, "Failed to read users in partition roles");
    }

    // read groups in partition roles
    if (CpiTableExists(cptComPlusGroupInPartitionRole))
    {
        hr = TrusteesInPartitionRolesRead(vcsGroupInPartitionRoleQuery, pPartRoleList, pUsrInPartRoleList);
        ExitOnFailure(hr, "Failed to read groups in partition roles");
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiUsersInPartitionRolesInstall(
    CPI_USER_IN_PARTITION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"AddUsersToComPlusPartitionRoles", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add roles to custom action data
    for (CPI_USER_IN_PARTITION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // roles that are being installed only
        if (!WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // action type
        if (rmRollback == iRunMode)
        {
            if (CpiIsInstalled(pItm->isInstalled))
                iActionType = atNoOp;
            else
                iActionType = atRemove;
        }
        else
            iActionType = atCreate;

        // add to action data
        hr = AddUserInPartitionRoleToActionData(pItm, iActionType, COST_USER_IN_APPLICATION_ROLE_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add user in partition role to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_USER_IN_APPLICATION_ROLE_CREATE * pList->iInstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiUsersInPartitionRolesUninstall(
    CPI_USER_IN_PARTITION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveUsersFromComPlusPartRoles", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add roles to custom action data
    for (CPI_USER_IN_PARTITION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // roles that are being uninstalled only
        if (!WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // action type
        if (rmRollback == iRunMode)
            iActionType = atCreate;
        else
            iActionType = atRemove;

        // add to action data
        hr = AddUserInPartitionRoleToActionData(pItm, iActionType, COST_USER_IN_APPLICATION_ROLE_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add user in partition role to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_USER_IN_APPLICATION_ROLE_DELETE * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}


// helper function definitions

static HRESULT TrusteesInPartitionRolesRead(
    LPCWSTR pwzQuery,
    CPI_PARTITION_ROLE_LIST* pPartRoleList,
    CPI_USER_IN_PARTITION_ROLE_LIST* pUsrInPartRoleList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    CPI_USER_IN_PARTITION_ROLE* pItm = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzName = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // loop through all application roles
    hr = WcaOpenExecuteView(pwzQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, tiprqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get component");

        // check if the component is our processor architecture
        hr = CpiVerifyComponentArchitecure(pwzData, &fMatchingArchitecture);
        ExitOnFailure(hr, "Failed to get component architecture.");

        if (!fMatchingArchitecture)
        {
            continue; // not the same architecture, ignore
        }

        // create entry
        pItm = (CPI_USER_IN_PARTITION_ROLE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_USER_IN_PARTITION_ROLE));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get key
        hr = WcaGetRecordString(hRec, tiprqUserInPartitionRole, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get partition role
        hr = WcaGetRecordString(hRec, tiprqPartitionRole, &pwzData);
        ExitOnFailure(hr, "Failed to get partition role");

        hr = CpiPartitionRoleFindByKey(pPartRoleList, pwzData, &pItm->pPartitionRole);
        ExitOnFailure1(hr, "Failed to find partition role, key: %S", pwzData);

        // get user domain
        hr = WcaGetRecordFormattedString(hRec, tiprqDomain, &pwzDomain);
        ExitOnFailure(hr, "Failed to get domain");

        // get user name
        hr = WcaGetRecordFormattedString(hRec, tiprqName, &pwzName);
        ExitOnFailure(hr, "Failed to get name");

        // build account name
        hr = CpiBuildAccountName(pwzDomain, pwzName, &pItm->pwzAccount);
        ExitOnFailure(hr, "Failed to build account name");

        // increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            pUsrInPartRoleList->iInstallCount++;
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            pUsrInPartRoleList->iUninstallCount++;

        // add entry
        if (pUsrInPartRoleList->pFirst)
            pItm->pNext = pUsrInPartRoleList->pFirst;
        pUsrInPartRoleList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreeUserInPartitionRole(pItm);

    ReleaseStr(pwzData);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzName);

    return hr;
}

static void FreePartitionRole(
    CPI_PARTITION_ROLE* pItm
    )
{
    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static void FreeUserInPartitionRole(
    CPI_USER_IN_PARTITION_ROLE* pItm
    )
{
    ReleaseStr(pItm->pwzAccount);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static HRESULT AddUserInPartitionRoleToActionData(
    CPI_USER_IN_PARTITION_ROLE* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add action information to custom action data
    hr = WcaWriteIntegerToCaData(iActionType, ppwzActionData);
    ExitOnFailure(hr, "Failed to add action type to custom action data");
    hr = WcaWriteIntegerToCaData(iActionCost, ppwzActionData);
    ExitOnFailure(hr, "Failed to add action cost to custom action data");

    // add application role information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add key to custom action data");
    hr = WcaWriteStringToCaData(pItm->pPartitionRole->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add role name to custom action data");
    hr = WcaWriteStringToCaData(pItm->pwzAccount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add user account to custom action data");

    // add partition information to custom action data
    hr = WcaWriteStringToCaData(pItm->pPartitionRole->pPartition->wzID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    hr = S_OK;

LExit:
    return hr;
}
