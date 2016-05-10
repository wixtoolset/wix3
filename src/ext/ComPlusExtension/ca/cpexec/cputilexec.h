#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#define CPI_MAX_GUID 38

enum eActionType { atNoOp = 0, atCreate, atRemove };


// structs

struct CPI_PROPERTY
{
    WCHAR wzName[MAX_DARWIN_KEY + 1];
    LPWSTR pwzValue;

    CPI_PROPERTY* pNext;
};

struct CPI_ROLLBACK_DATA
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    int iStatus;

    CPI_ROLLBACK_DATA* pNext;
};


// function prototypes

void CpiInitialize();
void CpiFinalize();
HRESULT CpiActionStartMessage(
    LPWSTR* ppwzActionData,
    BOOL fSuppress
    );
HRESULT CpiActionDataMessage(
    DWORD cArgs,
    ...
    );
HRESULT CpiGetAdminCatalog(
    ICOMAdminCatalog** ppiCatalog
    );
HRESULT CpiLogCatalogErrorInfo();
HRESULT CpiGetCatalogCollection(
    LPCWSTR pwzName,
    ICatalogCollection** ppiColl
    );
HRESULT CpiGetCatalogCollection(
    ICatalogCollection* piColl,
    ICatalogObject* piObj,
    LPCWSTR pwzName,
    ICatalogCollection** ppiColl
    );
HRESULT CpiAddCollectionObject(
    ICatalogCollection* piColl,
    ICatalogObject** ppiObj
    );
HRESULT CpiPutCollectionObjectValue(
    ICatalogObject* piObj,
    LPCWSTR pwzPropName,
    LPCWSTR pwzValue
    );
HRESULT CpiPutCollectionObjectValues(
    ICatalogObject* piObj,
    CPI_PROPERTY* pPropList
    );
HRESULT CpiGetCollectionObjectValue(
    ICatalogObject* piObj,
    LPCWSTR szPropName,
    LPWSTR* ppwzValue
    );
HRESULT CpiResetObjectProperty(
    ICatalogCollection* piColl,
    ICatalogObject* piObj,
    LPCWSTR pwzPropName
    );
HRESULT CpiRemoveCollectionObject(
    ICatalogCollection* piColl,
    LPCWSTR pwzID,
    LPCWSTR pwzName,
    BOOL fResetDeleteable
    );
HRESULT CpiRemoveUserCollectionObject(
    ICatalogCollection* piColl,
    PSID pSid
    );
HRESULT CpiFindCollectionObjectByStringKey(
    ICatalogCollection* piColl,
    LPCWSTR pwzKey,
    ICatalogObject** ppiObj
    );
HRESULT CpiFindCollectionObjectByIntegerKey(
    ICatalogCollection* piColl,
    long lKey,
    ICatalogObject** ppiObj
    );
HRESULT CpiFindCollectionObjectByName(
    ICatalogCollection* piColl,
    LPCWSTR pwzName,
    ICatalogObject** ppiObj
    );
HRESULT CpiFindUserCollectionObject(
    ICatalogCollection* piColl,
    PSID pSid,
    ICatalogObject** ppiObj
    );
HRESULT CpiGetPartitionsCollection(
    ICatalogCollection** ppiPartColl
    );
HRESULT CpiGetPartitionRolesCollection(
    LPCWSTR pwzPartID,
    ICatalogCollection** ppiRolesColl
    );
HRESULT CpiGetUsersInPartitionRoleCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzRoleName,
    ICatalogCollection** ppiUsrInRoleColl
    );
HRESULT CpiGetPartitionUsersCollection(
    ICatalogCollection** ppiUserColl
    );
HRESULT CpiGetApplicationsCollection(
    LPCWSTR pwzPartID,
    ICatalogCollection** ppiAppColl
    );
HRESULT CpiGetRolesCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    ICatalogCollection** ppiRolesColl
    );
HRESULT CpiGetUsersInRoleCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    LPCWSTR pwzRoleName,
    ICatalogCollection** ppiUsrInRoleColl
    );
HRESULT CpiGetComponentsCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    ICatalogCollection** ppiCompsColl
    );
HRESULT CpiGetInterfacesCollection(
    ICatalogCollection* piCompColl,
    ICatalogObject* piCompObj,
    ICatalogCollection** ppiIntfColl
    );
HRESULT CpiGetMethodsCollection(
    ICatalogCollection* piIntfColl,
    ICatalogObject* piIntfObj,
    ICatalogCollection** ppiMethColl
    );
HRESULT CpiGetSubscriptionsCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    LPCWSTR pwzCompCLSID,
    ICatalogCollection** ppiCompsColl
    );
HRESULT CpiReadPropertyList(
    LPWSTR* ppwzData,
    CPI_PROPERTY** ppPropList
    );
void CpiFreePropertyList(
    CPI_PROPERTY* pList
    );
HRESULT CpiWriteKeyToRollbackFile(
    HANDLE hFile,
    LPCWSTR pwzKey
    );
HRESULT CpiWriteIntegerToRollbackFile(
    HANDLE hFile,
    int i
    );
HRESULT CpiReadRollbackDataList(
    HANDLE hFile,
    CPI_ROLLBACK_DATA** pprdList
    );
void CpiFreeRollbackDataList(
    CPI_ROLLBACK_DATA* pList
    );
HRESULT CpiFindRollbackStatus(
    CPI_ROLLBACK_DATA* pList,
    LPCWSTR pwzKey,
    int* piStatus
    );
HRESULT CpiAccountNameToSid(
    LPCWSTR pwzAccountName,
    PSID* ppSid
    );
HRESULT CpiSidToAccountName(
    PSID pSid,
    LPWSTR* ppwzAccountName
    );
