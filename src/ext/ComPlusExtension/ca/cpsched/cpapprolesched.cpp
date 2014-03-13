//-------------------------------------------------------------------------------------------------
// <copyright file="cpapprolesched.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ application role functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// sql queries

LPCWSTR vcsApplicationRoleQuery =
    L"SELECT `ApplicationRole`, `Application_`, `Component_`, `Name` FROM `ComPlusApplicationRole`";
enum eApplicationRoleQuery { arqApplicationRole = 1, arqApplication, arqComponent, arqName };

LPCWSTR vcsUserInApplicationRoleQuery =
    L"SELECT `UserInApplicationRole`, `ApplicationRole_`, `ComPlusUserInApplicationRole`.`Component_`, `Domain`, `Name` FROM `ComPlusUserInApplicationRole`, `User` WHERE `User_` = `User`";
LPCWSTR vcsGroupInApplicationRoleQuery =
    L"SELECT `GroupInApplicationRole`, `ApplicationRole_`, `ComPlusGroupInApplicationRole`.`Component_`, `Domain`, `Name` FROM `ComPlusGroupInApplicationRole`, `Group` WHERE `Group_` = `Group`";
enum eTrusteeInApplicationRoleQuery { tiarqUserInApplicationRole = 1, tiarqApplicationRole, tiarqComponent, tiarqDomain, tiarqName };

LPCWSTR vcsApplicationRolePropertyQuery =
    L"SELECT `Name`, `Value` FROM `ComPlusApplicationRoleProperty` WHERE `ApplicationRole_` = ?";


// property definitions

CPI_PROPERTY_DEFINITION pdlApplicationRoleProperties[] =
{
    {L"Description", cpptString, 500},
    {NULL,           cpptNone,   0}
};


// prototypes for private helper functions

static HRESULT TrusteesInApplicationRolesRead(
    LPCWSTR pwzQuery,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_USER_IN_APPLICATION_ROLE_LIST* pUsrInAppRoleList
    );
static void FreeApplicationRole(
    CPI_APPLICATION_ROLE* pItm
    );
static void FreeUserInApplicationRole(
    CPI_USER_IN_APPLICATION_ROLE* pItm
    );
//static HRESULT GetUsersCollForApplicationRole(
//    CPI_APPLICATION_ROLE* pAppRole,
//    ICatalogCollection** ppiUsersColl
//    );
static HRESULT FindObjectForApplicationRole(
    CPI_APPLICATION_ROLE* pItm,
    ICatalogObject** ppiRoleObj
    );
static HRESULT AddApplicationRoleToActionData(
    CPI_APPLICATION_ROLE* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );
static HRESULT AddUserInApplicationRoleToActionData(
    CPI_USER_IN_APPLICATION_ROLE* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );


// function definitions

void CpiApplicationRoleListFree(
    CPI_APPLICATION_ROLE_LIST* pList
    )
{
    CPI_APPLICATION_ROLE* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_APPLICATION_ROLE* pDelete = pItm;
        pItm = pItm->pNext;
        FreeApplicationRole(pDelete);
    }
}

HRESULT CpiApplicationRolesRead(
    CPI_APPLICATION_LIST* pAppList,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    CPI_APPLICATION_ROLE* pItm = NULL;
    LPWSTR pwzData = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // loop through all application roles
    hr = WcaOpenExecuteView(vcsApplicationRoleQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ComPlusApplicationRole table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, arqComponent, &pwzData);
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
        pItm = (CPI_APPLICATION_ROLE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_APPLICATION_ROLE));
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
        hr = WcaGetRecordString(hRec, arqApplicationRole, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get application
        hr = WcaGetRecordString(hRec, arqApplication, &pwzData);
        ExitOnFailure(hr, "Failed to get application");

        hr = CpiApplicationFindByKey(pAppList, pwzData, &pItm->pApplication);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        ExitOnFailure1(hr, "Failed to find application, key: %S", pwzData);

        // get name
        hr = WcaGetRecordFormattedString(hRec, arqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // get properties
        if (CpiTableExists(cptComPlusApplicationRoleProperty))
        {
            hr = CpiPropertiesRead(vcsApplicationRolePropertyQuery, pItm->wzKey, pdlApplicationRoleProperties, &pItm->pProperties, &pItm->iPropertyCount);
            ExitOnFailure(hr, "Failed to get properties");
        }

        // set references & increment counters
        if (pItm->fHasComponent)
        {
            if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            {
                CpiApplicationAddReferenceInstall(pItm->pApplication);
                pAppRoleList->iInstallCount++;
            }
            if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            {
                CpiApplicationAddReferenceUninstall(pItm->pApplication);
                pAppRoleList->iUninstallCount++;
            }
        }

        // add entry
        if (pAppRoleList->pFirst)
            pItm->pNext = pAppRoleList->pFirst;
        pAppRoleList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreeApplicationRole(pItm);

    ReleaseStr(pwzData);

    return hr;
}

HRESULT CpiApplicationRolesVerifyInstall(
    CPI_APPLICATION_ROLE_LIST* pList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ICatalogObject* piRoleObj = NULL;

    for (CPI_APPLICATION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // referenced locaters or roles that are being installed
        if (!pItm->fReferencedForInstall && !(pItm->fHasComponent && WcaIsInstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // if the role is referensed and is not a locater, it must be installed
        if (pItm->fReferencedForInstall && pItm->fHasComponent && !CpiWillBeInstalled(pItm->isInstalled, pItm->isAction))
            MessageExitOnFailure1(hr = E_FAIL, msierrComPlusApplicationRoleDependency, "An application role is used by another entity being installed, but is not installed itself, key: %S", pItm->wzKey);

        // role is a locater
        if (!pItm->fHasComponent)
        {
            // get collection object for role
            hr = FindObjectForApplicationRole(pItm, &piRoleObj);
            ExitOnFailure(hr, "Failed to find collection object for role");

            // if the role was not found
            if (S_FALSE == hr)
                MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND), msierrComPlusApplicationRoleNotFound, "An application role required by this installation was not found, key: %S", pItm->wzKey);
        }

        // role is supposed to be created
        else if (!CpiIsInstalled(pItm->isInstalled))
        {
            do {
                // find roles with conflicting name or id
                hr = FindObjectForApplicationRole(pItm, NULL);
                ExitOnFailure(hr, "Failed to find collection object for role");

                if (S_OK == hr)
                {
                    er = WcaErrorMessage(msierrComPlusApplicationRoleConflict, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                    switch (er)
                    {
                    case IDABORT:
                        ExitOnFailure1(hr = E_FAIL, "An application with a conflictiong name exists, key: %S", pItm->wzKey);
                        break;
                    case IDRETRY:
                        break;
                    case IDIGNORE:
                    default:
                        hr = S_FALSE; // indicate that this is not a conflict
                    }
                }
            } while (S_OK == hr); // hr = S_FALSE if we don't have any conflicts
        }

        // clean up
        ReleaseNullObject(piRoleObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piRoleObj);

    return hr;
}

HRESULT CpiApplicationRolesVerifyUninstall(
    CPI_APPLICATION_ROLE_LIST* pList
    )
{
    HRESULT hr = S_OK;

    for (CPI_APPLICATION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // referenced locaters or roles that are being installed
        if (!pItm->fReferencedForUninstall && !(pItm->fHasComponent && WcaIsUninstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // get collection object for role
        hr = FindObjectForApplicationRole(pItm, NULL);
        ExitOnFailure(hr, "Failed to find collection object for role");

        // if the role was not found
        if (S_FALSE == hr)
        {
            pItm->fObjectNotFound = TRUE;
            if (pItm->fHasComponent)
                pList->iUninstallCount--; // elements with the fObjectNotFound flag set will not be scheduled for uninstall
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

void CpiApplicationRoleAddReferenceInstall(
    CPI_APPLICATION_ROLE* pItm
    )
{
    pItm->fReferencedForInstall = TRUE;
    CpiApplicationAddReferenceInstall(pItm->pApplication);
}

void CpiApplicationRoleAddReferenceUninstall(
    CPI_APPLICATION_ROLE* pItm
    )
{
    pItm->fReferencedForUninstall = TRUE;
    CpiApplicationAddReferenceUninstall(pItm->pApplication);
}

HRESULT CpiApplicationRolesInstall(
    CPI_APPLICATION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"CreateComPlusApplicationRoles", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add roles to custom action data
    for (CPI_APPLICATION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
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
        hr = AddApplicationRoleToActionData(pItm, iActionType, COST_APPLICATION_ROLE_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add application role to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_APPLICATION_ROLE_CREATE * pList->iInstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiApplicationRolesUninstall(
    CPI_APPLICATION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveComPlusApplicationRoles", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add roles to custom action data
    for (CPI_APPLICATION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // roles that are being uninstalled only
        if (pItm->fObjectNotFound || !WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // action type
        if (rmRollback == iRunMode)
            iActionType = atCreate;
        else
            iActionType = atRemove;

        // add to action data
        hr = AddApplicationRoleToActionData(pItm, iActionType, COST_APPLICATION_ROLE_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add application role to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_APPLICATION_ROLE_DELETE * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiApplicationRoleFindByKey(
    CPI_APPLICATION_ROLE_LIST* pList,
    LPCWSTR pwzKey,
    CPI_APPLICATION_ROLE** ppAppRole
    )
{
    for (CPI_APPLICATION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pwzKey))
        {
            *ppAppRole = pItm;
            return S_OK;
        }
    }

    return S_FALSE;
}

void CpiUserInApplicationRoleListFree(
    CPI_USER_IN_APPLICATION_ROLE_LIST* pList
    )
{
    CPI_USER_IN_APPLICATION_ROLE* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_USER_IN_APPLICATION_ROLE* pDelete = pItm;
        pItm = pItm->pNext;
        FreeUserInApplicationRole(pDelete);
    }
}

HRESULT CpiUsersInApplicationRolesRead(
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_USER_IN_APPLICATION_ROLE_LIST* pUsrInAppRoleList
    )
{
    HRESULT hr = S_OK;

    // read users in application roles
    if (CpiTableExists(cptComPlusUserInApplicationRole))
    {
        hr = TrusteesInApplicationRolesRead(vcsUserInApplicationRoleQuery, pAppRoleList, pUsrInAppRoleList);
        ExitOnFailure(hr, "Failed to read users in application roles");
    }

    // read groups in application roles
    if (CpiTableExists(cptComPlusGroupInApplicationRole))
    {
        hr = TrusteesInApplicationRolesRead(vcsGroupInApplicationRoleQuery, pAppRoleList, pUsrInAppRoleList);
        ExitOnFailure(hr, "Failed to read groups in application roles");
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiUsersInApplicationRolesInstall(
    CPI_USER_IN_APPLICATION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"AddUsersToComPlusApplicationRoles", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add roles to custom action data
    for (CPI_USER_IN_APPLICATION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
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
        hr = AddUserInApplicationRoleToActionData(pItm, iActionType, COST_USER_IN_APPLICATION_ROLE_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add user in application role to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_USER_IN_APPLICATION_ROLE_CREATE * pList->iInstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiUsersInApplicationRolesUninstall(
    CPI_USER_IN_APPLICATION_ROLE_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveUsersFromComPlusAppRoles", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add roles to custom action data
    for (CPI_USER_IN_APPLICATION_ROLE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
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
        hr = AddUserInApplicationRoleToActionData(pItm, iActionType, COST_USER_IN_APPLICATION_ROLE_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add user in application role to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_USER_IN_APPLICATION_ROLE_DELETE * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}


// helper function definitions

static HRESULT TrusteesInApplicationRolesRead(
    LPCWSTR pwzQuery,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_USER_IN_APPLICATION_ROLE_LIST* pUsrInAppRoleList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    CPI_USER_IN_APPLICATION_ROLE* pItm = NULL;
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
        hr = WcaGetRecordString(hRec, tiarqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get component");

        // check if the component is our processor architecture
        hr = CpiVerifyComponentArchitecure(pwzData, &fMatchingArchitecture);
        ExitOnFailure(hr, "Failed to get component architecture.");

        if (!fMatchingArchitecture)
        {
            continue; // not the same architecture, ignore
        }

        // create entry
        pItm = (CPI_USER_IN_APPLICATION_ROLE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_USER_IN_APPLICATION_ROLE));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get key
        hr = WcaGetRecordString(hRec, tiarqUserInApplicationRole, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get application role
        hr = WcaGetRecordString(hRec, tiarqApplicationRole, &pwzData);
        ExitOnFailure(hr, "Failed to get application role");

        hr = CpiApplicationRoleFindByKey(pAppRoleList, pwzData, &pItm->pApplicationRole);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        ExitOnFailure1(hr, "Failed to find application role, key: %S", pwzData);

        // get user domain
        hr = WcaGetRecordFormattedString(hRec, tiarqDomain, &pwzDomain);
        ExitOnFailure(hr, "Failed to get domain");

        // get user name
        hr = WcaGetRecordFormattedString(hRec, tiarqName, &pwzName);
        ExitOnFailure(hr, "Failed to get name");

        // build account name
        hr = CpiBuildAccountName(pwzDomain, pwzName, &pItm->pwzAccount);
        ExitOnFailure(hr, "Failed to build account name");

        // set references & increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
        {
            CpiApplicationRoleAddReferenceInstall(pItm->pApplicationRole);
            pUsrInAppRoleList->iInstallCount++;
        }
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
        {
            CpiApplicationRoleAddReferenceUninstall(pItm->pApplicationRole);
            pUsrInAppRoleList->iUninstallCount++;
        }

        // add entry
        if (pUsrInAppRoleList->pFirst)
            pItm->pNext = pUsrInAppRoleList->pFirst;
        pUsrInAppRoleList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreeUserInApplicationRole(pItm);

    ReleaseStr(pwzData);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzName);

    return hr;
}

static void FreeApplicationRole(
    CPI_APPLICATION_ROLE* pItm
    )
{
    if (pItm->pProperties)
        CpiPropertiesFreeList(pItm->pProperties);

    ReleaseObject(pItm->piUsersColl);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static void FreeUserInApplicationRole(
    CPI_USER_IN_APPLICATION_ROLE* pItm
    )
{
    ReleaseStr(pItm->pwzAccount);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

//static HRESULT GetUsersCollForApplicationRole(
//    CPI_APPLICATION_ROLE* pAppRole,
//    ICatalogCollection** ppiUsersColl
//    )
//{
//    HRESULT hr = S_OK;
//
//    ICatalogCollection* piRoleColl = NULL;
//    ICatalogObject* piRoleObj = NULL;
//
//    // if a previous attempt to locate the collection object failed
//    if (pAppRole->fObjectNotFound)
//        ExitFunction1(hr = S_FALSE);
//
//    // get applications collection
//    if (!pAppRole->piUsersColl)
//    {
//        // get collection object for role
//        hr = FindObjectForApplicationRole(pAppRole, &piRoleObj);
//        ExitOnFailure(hr, "Failed to find collection object for role");
//
//        if (S_FALSE == hr)
//            ExitFunction(); // exit with hr = S_FALSE
//
//        // get users collection
//        hr = CpiGetCatalogCollection(piRoleColl, piRoleObj, L"UsersInRole", &pAppRole->piUsersColl);
//        ExitOnFailure(hr, "Failed to get users in role collection");
//    }
//
//    // return value
//    *ppiUsersColl = pAppRole->piUsersColl;
//    (*ppiUsersColl)->AddRef();
//
//    hr = S_OK;
//
//LExit:
//    // clean up
//    ReleaseObject(piRoleColl);
//    ReleaseObject(piRoleObj);
//
//    return hr;
//}

static HRESULT FindObjectForApplicationRole(
    CPI_APPLICATION_ROLE* pItm,
    ICatalogObject** ppiRoleObj
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piRoleColl = NULL;

    // get roles collection
    hr = CpiGetRolesCollForApplication(pItm->pApplication, &piRoleColl);
    ExitOnFailure(hr, "Failed to get collection");

    if (S_FALSE == hr)
        ExitFunction(); // exit with hr = S_FALSE

    // find role object
    hr = CpiFindCollectionObject(piRoleColl, NULL, pItm->wzName, ppiRoleObj);
    ExitOnFailure(hr, "Failed to find object");

    // exit with hr from CpiFindCollectionObject()

LExit:
    // clean up
    ReleaseObject(piRoleColl);

    return hr;
}

static HRESULT AddApplicationRoleToActionData(
    CPI_APPLICATION_ROLE* pItm,
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
    ExitOnFailure(hr, "Failed to add application role key to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application role name to custom action data");

    // add application information to custom action data
    hr = WcaWriteStringToCaData(pItm->pApplication->wzID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application id to custom action data");

    // add partition information to custom action data
    hr = WcaWriteStringToCaData(pItm->pApplication->pPartition ? pItm->pApplication->pPartition->wzID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    // add properties to custom action data
    hr = CpiAddPropertiesToActionData(atCreate == iActionType ? pItm->iPropertyCount : 0, pItm->pProperties, ppwzActionData);
    ExitOnFailure(hr, "Failed to add properties to custom action data");

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AddUserInApplicationRoleToActionData(
    CPI_USER_IN_APPLICATION_ROLE* pItm,
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
    hr = WcaWriteStringToCaData(pItm->pApplicationRole->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add role name to custom action data");
    hr = WcaWriteStringToCaData(pItm->pwzAccount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add user account to custom action data");

    // add application information to custom action data
    CPI_APPLICATION* pApplication = pItm->pApplicationRole->pApplication;
    hr = WcaWriteStringToCaData(pApplication->wzID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application id to custom action data");

    // add partition information to custom action data
    hr = WcaWriteStringToCaData(pApplication->pPartition ? pApplication->pPartition->wzID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    hr = S_OK;

LExit:
    return hr;
}
