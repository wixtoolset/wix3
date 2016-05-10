// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// sql queries

LPCWSTR vcsPartitionQuery =
    L"SELECT `Partition`, `Component_`, `Id`, `Name` FROM `ComPlusPartition`";
enum ePartitionQuery { pqPartition = 1, pqComponent, pqID, pqName };

LPCWSTR vcsPartitionPropertyQuery =
    L"SELECT `Name`, `Value` FROM `ComPlusPartitionProperty` WHERE `Partition_` = ?";

LPCWSTR vcsPartitionUserQuery =
    L"SELECT `PartitionUser`, `Partition_`, `ComPlusPartitionUser`.`Component_`, `Domain`, `Name` FROM `ComPlusPartitionUser`, `User` WHERE `User_` = `User`";
enum ePartitionUserQuery { puqPartitionUser = 1, puqPartition, puqComponent, puqDomain, puqName };


// property definitions

CPI_PROPERTY_DEFINITION pdlPartitionProperties[] =
{
    {L"Changeable",  cpptBoolean, 502},
    {L"Deleteable",  cpptBoolean, 502},
    {L"Description", cpptString,  502},
    {NULL,           cpptNone,    0}
};


// prototypes for private helper functions

static void FreePartition(
    CPI_PARTITION* pItm
    );
static void FreePartitionUser(
    CPI_PARTITION_USER* pItm
    );
static HRESULT AddPartitionToActionData(
    CPI_PARTITION* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );
static HRESULT AddPartitionUserToActionData(
    CPI_PARTITION_USER* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );


// function definitions

void CpiPartitionListFree(
    CPI_PARTITION_LIST* pList
    )
{
    CPI_PARTITION* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_PARTITION* pDelete = pItm;
        pItm = pItm->pNext;
        FreePartition(pDelete);
    }
}

HRESULT CpiPartitionsRead(
    CPI_PARTITION_LIST* pPartList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    CPI_PARTITION* pItm = NULL;
    LPWSTR pwzData = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // loop through all partitions
    hr = WcaOpenExecuteView(vcsPartitionQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ComPlusPartition table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, pqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get component");

        // check if the component is our processor architecture
        if (pwzData && *pwzData)
        {
            hr = CpiVerifyComponentArchitecure(pwzData, &fMatchingArchitecture);
            ExitOnFailure(hr, "Failed to get component architecture.");

            if (!fMatchingArchitecture)
            {
                continue; // not the same architecture, ignore
            }
        }

        // create entry
        pItm = (CPI_PARTITION*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_PARTITION));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get component install state
        if (pwzData && *pwzData)
        {
            pItm->fHasComponent = TRUE;

            er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
            ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");
        }

        // get key
        hr = WcaGetRecordString(hRec, pqPartition, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get id
        hr = WcaGetRecordFormattedString(hRec, pqID, &pwzData);
        ExitOnFailure(hr, "Failed to get id");

        if (pwzData && *pwzData)
        {
            hr = PcaGuidToRegFormat(pwzData, pItm->wzID, countof(pItm->wzID));
            ExitOnFailure2(hr, "Failed to parse id guid value, key: %S, value: '%S'", pItm->wzKey, pwzData);
        }

        // get name
        hr = WcaGetRecordFormattedString(hRec, pqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // if partition is a locater, either an id or a name must be provided
        if (!pItm->fHasComponent && !*pItm->wzID && !*pItm->wzName)
            ExitOnFailure1(hr = E_FAIL, "A partition locater must have either an id or a name associated, key: %S", pItm->wzKey);

        // if partition is not a locater, an name must be provided
        if (pItm->fHasComponent && !*pItm->wzName)
            ExitOnFailure1(hr = E_FAIL, "A partition must have a name associated, key: %S", pItm->wzKey);

        // get properties
        if (CpiTableExists(cptComPlusPartitionProperty) && pItm->fHasComponent)
        {
            hr = CpiPropertiesRead(vcsPartitionPropertyQuery, pItm->wzKey, pdlPartitionProperties, &pItm->pProperties, &pItm->iPropertyCount);
            ExitOnFailure(hr, "Failed to get properties");
        }

        // increment counters
        if (pItm->fHasComponent && WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            pPartList->iInstallCount++;
        if (pItm->fHasComponent && WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            pPartList->iUninstallCount++;

        // add entry
        if (pPartList->pFirst)
            pItm->pNext = pPartList->pFirst;
        pPartList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreePartition(pItm);

    ReleaseStr(pwzData);

    return hr;
}

HRESULT CpiPartitionsVerifyInstall(
    CPI_PARTITION_LIST* pList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;

    for (CPI_PARTITION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // referenced locaters or partitions that are being installed
        if (!pItm->fReferencedForInstall && !(pItm->fHasComponent && WcaIsInstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // if the partition is referensed and is not a locater, it must be installed
        if (pItm->fReferencedForInstall && pItm->fHasComponent && !CpiWillBeInstalled(pItm->isInstalled, pItm->isAction))
            MessageExitOnFailure1(hr = E_FAIL, msierrComPlusPartitionDependency, "A partition is used by another entity being installed, but is not installed itself, key: %S", pItm->wzKey);

        // get partitions collection
        if (!piPartColl)
        {
            hr = CpiGetPartitionsCollection(&piPartColl);
            ExitOnFailure(hr, "Failed to get partitions collection");
        }

        // partition is supposed to exist
        if (!pItm->fHasComponent || CpiIsInstalled(pItm->isInstalled))
        {
            // get collection object for partition
            hr = CpiFindCollectionObject(piPartColl, pItm->wzID, *pItm->wzID ? NULL : pItm->wzName, &piPartObj);
            ExitOnFailure(hr, "Failed to find collection object for partition");

            // if the partition was found
            if (S_OK == hr)
            {
                // if we don't have an id, copy id from object
                if (!*pItm->wzID)
                {
                    hr = CpiGetKeyForObject(piPartObj, pItm->wzID, countof(pItm->wzID));
                    ExitOnFailure(hr, "Failed to get id");
                }
            }

            // if the partition was not found
            else
            {
                // if the application is a locater, this is an error
                if (!pItm->fHasComponent)
                    MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND), msierrComPlusPartitionNotFound, "A partition required by this installation was not found, key: %S", pItm->wzKey);

                // create a new id if one is missing
                if (!*pItm->wzID)
                {
                    hr = CpiCreateId(pItm->wzID, countof(pItm->wzID));
                    ExitOnFailure(hr, "Failed to create id");
                }
            }
        }

        // partition is supposed to be created
        else
        {
            // check for conflicts
            do {
                if (*pItm->wzID)
                {
                    // find partitions with conflicting id
                    hr = CpiFindCollectionObject(piPartColl, pItm->wzID, NULL, &piPartObj);
                    ExitOnFailure(hr, "Failed to find collection object for partition");

                    if (S_FALSE == hr)
                    {
                        // find partitions with conflicting name
                        hr = CpiFindCollectionObject(piPartColl, NULL, pItm->wzName, &piPartObj);
                        ExitOnFailure(hr, "Failed to find collection object for partition");

                        if (S_OK == hr)
                            // "A partition with a conflictiong name exists. retry cancel"
                            er = WcaErrorMessage(msierrComPlusPartitionNameConflict, hr, INSTALLMESSAGE_ERROR | MB_RETRYCANCEL, 0);
                        else
                            break; // no conflicting entry found, break loop
                    }
                    else
                        // "A partition with a conflicting id exists. abort retry ignore"
                        er = WcaErrorMessage(msierrComPlusPartitionIdConflict, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                }
                else
                {
                    // find partitions with conflicting name
                    hr = CpiFindCollectionObject(piPartColl, NULL, pItm->wzName, &piPartObj);
                    ExitOnFailure(hr, "Failed to find collection object for partition");

                    if (S_OK == hr)
                        // "A partition with a conflictiong name exists. abort retry ignore"
                        er = WcaErrorMessage(msierrComPlusPartitionNameConflict, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                    else
                        break; // no conflicting entry found, break loop
                }

                switch (er)
                {
                case IDCANCEL:
                case IDABORT:
                    ExitOnFailure1(hr = E_FAIL, "A partition with a conflictiong name or id exists, key: %S", pItm->wzKey);
                    break;
                case IDRETRY:
                    break;
                case IDIGNORE:
                default:
                    // if we don't have an id, copy id from object
                    if (!*pItm->wzID)
                    {
                        hr = CpiGetKeyForObject(piPartObj, pItm->wzID, countof(pItm->wzID));
                        ExitOnFailure(hr, "Failed to get id");
                    }
                    hr = S_FALSE; // indicate that this is not a conflict
                }
            } while (S_OK == hr); // hr = S_FALSE if we don't have any conflicts

            // create a new id if one is missing
            if (!*pItm->wzID)
            {
                hr = CpiCreateId(pItm->wzID, countof(pItm->wzID));
                ExitOnFailure(hr, "Failed to create id");
            }
        }

        // clean up
        ReleaseNullObject(piPartObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);

    return hr;
}

HRESULT CpiPartitionsVerifyUninstall(
    CPI_PARTITION_LIST* pList
    )
{
    HRESULT hr = S_OK;
    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;

    for (CPI_PARTITION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // referenced locaters or partitions that are being uninstalled
        if (!pItm->fReferencedForUninstall && !(pItm->fHasComponent && WcaIsUninstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // get partitions collection
        if (!piPartColl)
        {
            hr = CpiGetPartitionsCollection(&piPartColl);
            ExitOnFailure(hr, "Failed to get partitions collection");
        }

        // get collection object for partition
        hr = CpiFindCollectionObject(piPartColl, pItm->wzID, *pItm->wzID ? NULL : pItm->wzName, &piPartObj);
        ExitOnFailure(hr, "Failed to find collection object for partition");

        // if the partition was found
        if (S_OK == hr)
        {
            // if we don't have an id, copy id from object
            if (!*pItm->wzID)
            {
                hr = CpiGetKeyForObject(piPartObj, pItm->wzID, countof(pItm->wzID));
                ExitOnFailure(hr, "Failed to get id");
            }
        }

        // if the partition was not found
        else
        {
            pItm->fObjectNotFound = TRUE;
            if (pItm->fHasComponent)
                pList->iUninstallCount--; // elements with the fObjectNotFound flag set will not be scheduled for uninstall
        }

        // clean up
        ReleaseNullObject(piPartObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);

    return hr;
}

void CpiPartitionAddReferenceInstall(
    CPI_PARTITION* pItm
    )
{
    pItm->fReferencedForInstall = TRUE;
}

void CpiPartitionAddReferenceUninstall(
    CPI_PARTITION* pItm
    )
{
    pItm->fReferencedForUninstall = TRUE;
}

HRESULT CpiPartitionsInstall(
    CPI_PARTITION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"CreateComPlusPartitions", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add partition count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add applications to custom action data
    for (CPI_PARTITION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // partitions that are being installed only
        if (!pItm->fHasComponent || !WcaIsInstalling(pItm->isInstalled, pItm->isAction))
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
        hr = AddPartitionToActionData(pItm, iActionType, COST_PARTITION_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add partition to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_PARTITION_CREATE * pList->iInstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiPartitionsUninstall(
    CPI_PARTITION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveComPlusPartitions", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add partition count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add partitions to custom action data
    for (CPI_PARTITION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // partitions that are being uninstalled only
        if (!pItm->fHasComponent || pItm->fObjectNotFound || !WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // action type
        if (rmRollback == iRunMode)
            iActionType = atCreate;
        else
            iActionType = atRemove;

        // add to action data
        hr = AddPartitionToActionData(pItm, iActionType, COST_PARTITION_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add partition to custom action data, key:", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_PARTITION_DELETE * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiPartitionFindByKey(
    CPI_PARTITION_LIST* pList,
    LPCWSTR wzKey,
    CPI_PARTITION** ppItm
    )
{
    for (CPI_PARTITION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, wzKey))
        {
            *ppItm = pItm;
            return S_OK;
        }
    }

    return S_FALSE;
}

HRESULT CpiGetApplicationsCollForPartition(
    CPI_PARTITION* pPart,
    ICatalogCollection** ppiAppColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;

    // if a previous attempt to locate the collection object failed
    if (pPart->fObjectNotFound)
        ExitFunction1(hr = S_FALSE);

    // get applications collection
    if (!pPart->piApplicationsColl)
    {
        // get partitions collection from catalog
        hr = CpiGetPartitionsCollection(&piPartColl);
        ExitOnFailure(hr, "Failed to get partitions collection");

        // find application object
        hr = CpiFindCollectionObject(piPartColl, pPart->wzID, *pPart->wzID ? NULL : pPart->wzName, &piPartObj);
        ExitOnFailure(hr, "Failed to find partition object");

        if (S_FALSE == hr)
        {
            pPart->fObjectNotFound = TRUE;
            ExitFunction(); // exit with hr = S_FALSE
        }

        // get roles collection
        hr = CpiGetCatalogCollection(piPartColl, piPartObj, L"Applications", &pPart->piApplicationsColl);
        ExitOnFailure(hr, "Failed to get applications collection");
    }

    // return value
    *ppiAppColl = pPart->piApplicationsColl;
    (*ppiAppColl)->AddRef();

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);

    return hr;
}

HRESULT CpiGetRolesCollForPartition(
    CPI_PARTITION* pPart,
    ICatalogCollection** ppiRolesColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;

    // if a previous attempt to locate the collection object failed
    if (pPart->fObjectNotFound)
        ExitFunction1(hr = S_FALSE);

    // get applications collection
    if (!pPart->piRolesColl)
    {
        // get partitions collection from catalog
        hr = CpiGetPartitionsCollection(&piPartColl);
        ExitOnFailure(hr, "Failed to get partitions collection");

        // find partition object
        hr = CpiFindCollectionObject(piPartColl, pPart->wzID, *pPart->wzID ? NULL : pPart->wzName, &piPartObj);
        ExitOnFailure(hr, "Failed to find partition object");

        if (S_FALSE == hr)
            ExitFunction(); // exit with hr = S_FALSE

        // get roles collection
        hr = CpiGetCatalogCollection(piPartColl, piPartObj, L"RolesForPartition", &pPart->piRolesColl);
        ExitOnFailure(hr, "Failed to get roles collection");
    }

    // return value
    *ppiRolesColl = pPart->piRolesColl;
    (*ppiRolesColl)->AddRef();

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);

    return hr;
}

void CpiPartitionUserListFree(
    CPI_PARTITION_USER_LIST* pList
    )
{
    CPI_PARTITION_USER* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_PARTITION_USER* pDelete = pItm;
        pItm = pItm->pNext;
        FreePartitionUser(pDelete);
    }
}

HRESULT CpiPartitionUsersRead(
    CPI_PARTITION_LIST* pPartList,
    CPI_PARTITION_USER_LIST* pPartUsrList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    CPI_PARTITION_USER* pItm = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzName = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // loop through all partition users
    hr = WcaOpenExecuteView(vcsPartitionUserQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ComPlusPartitionUser table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, puqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get component");

        // check if the component is our processor architecture
        hr = CpiVerifyComponentArchitecure(pwzData, &fMatchingArchitecture);
        ExitOnFailure(hr, "Failed to get component architecture.");

        if (!fMatchingArchitecture)
        {
            continue; // not the same architecture, ignore
        }

        // create entry
        pItm = (CPI_PARTITION_USER*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_PARTITION_USER));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get key
        hr = WcaGetRecordString(hRec, puqPartitionUser, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get partition
        hr = WcaGetRecordString(hRec, puqPartition, &pwzData);
        ExitOnFailure(hr, "Failed to get partition");

        hr = CpiPartitionFindByKey(pPartList, pwzData, &pItm->pPartition);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        ExitOnFailure1(hr, "Failed to find partition, key: %S", pwzData);

        // get user domain
        hr = WcaGetRecordFormattedString(hRec, puqDomain, &pwzDomain);
        ExitOnFailure(hr, "Failed to get user domain");

        // get user name
        hr = WcaGetRecordFormattedString(hRec, puqName, &pwzName);
        ExitOnFailure(hr, "Failed to get user name");

        // build account name
        hr = CpiBuildAccountName(pwzDomain, pwzName, &pItm->pwzAccount);
        ExitOnFailure(hr, "Failed to build account name");

        // set references & increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
        {
            pItm->pPartition->fReferencedForInstall = TRUE;
            pPartUsrList->iInstallCount++;
        }
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
        {
            pItm->pPartition->fReferencedForUninstall = TRUE;
            pPartUsrList->iUninstallCount++;
        }

        // add entry
        if (pPartUsrList->pFirst)
            pItm->pNext = pPartUsrList->pFirst;
        pPartUsrList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreePartitionUser(pItm);

    ReleaseStr(pwzData);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzName);

    return hr;
}

HRESULT CpiPartitionUsersInstall(
    CPI_PARTITION_USER_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"AddComPlusPartitionUsers", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add partition count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add applications to custom action data
    for (CPI_PARTITION_USER* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // partitions that are being installed only
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
        hr = AddPartitionUserToActionData(pItm, iActionType, COST_PARTITION_USER_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add partition user to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_PARTITION_USER_CREATE * pList->iInstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiPartitionUsersUninstall(
    CPI_PARTITION_USER_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveComPlusPartitionUsers", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add partition count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add partitions to custom action data
    for (CPI_PARTITION_USER* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // partitions that are being uninstalled only
        if (!WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // action type
        if (rmRollback == iRunMode)
            iActionType = atCreate;
        else
            iActionType = atRemove;

        // add to action data
        hr = AddPartitionUserToActionData(pItm, iActionType, COST_PARTITION_USER_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add partition user to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_PARTITION_USER_DELETE * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}


// helper function definitions

static void FreePartition(
    CPI_PARTITION* pItm
    )
{
    if (pItm->pProperties)
        CpiPropertiesFreeList(pItm->pProperties);

    ReleaseObject(pItm->piApplicationsColl);
    ReleaseObject(pItm->piRolesColl);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static void FreePartitionUser(
    CPI_PARTITION_USER* pItm
    )
{
    ReleaseStr(pItm->pwzAccount);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static HRESULT AddPartitionToActionData(
    CPI_PARTITION* pItm,
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

    // add partition information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition key to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition name to custom action data");

    // add properties to custom action data
    hr = CpiAddPropertiesToActionData(atCreate == iActionType ? pItm->iPropertyCount : 0, pItm->pProperties, ppwzActionData);
    ExitOnFailure(hr, "Failed to add properties to custom action data");

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AddPartitionUserToActionData(
    CPI_PARTITION_USER* pItm,
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

    // add partition user information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition user key to custom action data");
    hr = WcaWriteStringToCaData(pItm->pwzAccount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add user account to custom action data");

    // add partition information to custom action data
    hr = WcaWriteStringToCaData(atCreate == iActionType ? pItm->pPartition->wzID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    hr = S_OK;

LExit:
    return hr;
}
