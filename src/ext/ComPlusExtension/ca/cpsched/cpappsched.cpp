//-------------------------------------------------------------------------------------------------
// <copyright file="cpappsched.cpp" company="Outercurve Foundation">
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

#include "precomp.h"


// sql queries

LPCWSTR vcsApplicationQuery =
    L"SELECT `Application`, `Component_`, `Partition_`, `Id`, `Name` FROM `ComPlusApplication`";
enum eApplicationQuery { aqApplication = 1, aqComponent, aqPartition, aqID, aqName };

LPCWSTR vcsApplicationPropertyQuery =
    L"SELECT `Name`, `Value` FROM `ComPlusApplicationProperty` WHERE `Application_` = ?";


// property definitions

CPI_PROPERTY_DEFINITION pdlApplicationProperties[] =
{
    {L"3GigSupportEnabled",             cpptBoolean, 500},
    {L"AccessChecksLevel",              cpptInteger, 500},
    {L"Activation",                     cpptInteger, 500},
    {L"ApplicationAccessChecksEnabled", cpptBoolean, 500},
    {L"ApplicationDirectory",           cpptString,  501},
    {L"Authentication",                 cpptInteger, 500},
    {L"AuthenticationCapability",       cpptInteger, 500},
    {L"Changeable",                     cpptBoolean, 500},
    {L"CommandLine",                    cpptString,  500},
    {L"ConcurrentApps",                 cpptInteger, 501},
    {L"CreatedBy",                      cpptString,  500},
    {L"CRMEnabled",                     cpptBoolean, 500},
    {L"CRMLogFile",                     cpptString,  500},
    {L"Deleteable",                     cpptBoolean, 500},
    {L"Description",                    cpptString,  500},
    {L"DumpEnabled",                    cpptBoolean, 501},
    {L"DumpOnException",                cpptBoolean, 501},
    {L"DumpOnFailfast",                 cpptBoolean, 501},
    {L"DumpPath",                       cpptString,  501},
    {L"EventsEnabled",                  cpptBoolean, 500},
    {L"Identity",                       cpptString,  500},
    {L"ImpersonationLevel",             cpptInteger, 500},
    {L"IsEnabled",                      cpptBoolean, 501},
    {L"MaxDumpCount",                   cpptInteger, 501},
    {L"Password",                       cpptString,  500},
    {L"QCAuthenticateMsgs",             cpptInteger, 501},
    {L"QCListenerMaxThreads",           cpptInteger, 501},
    {L"QueueListenerEnabled",           cpptBoolean, 500},
    {L"QueuingEnabled",                 cpptBoolean, 500},
    {L"RecycleActivationLimit",         cpptInteger, 501},
    {L"RecycleCallLimit",               cpptInteger, 501},
    {L"RecycleExpirationTimeout",       cpptInteger, 501},
    {L"RecycleLifetimeLimit",           cpptInteger, 501},
    {L"RecycleMemoryLimit",             cpptInteger, 501},
    {L"Replicable",                     cpptBoolean, 501},
    {L"RunForever",                     cpptBoolean, 500},
    {L"ShutdownAfter",                  cpptInteger, 500},
    {L"SoapActivated",                  cpptBoolean, 502},
    {L"SoapBaseUrl",                    cpptString,  502},
    {L"SoapMailTo",                     cpptString,  502},
    {L"SoapVRoot",                      cpptString,  502},
    {L"SRPEnabled",                     cpptBoolean, 501},
    {L"SRPTrustLevel",                  cpptInteger, 501},
    {NULL,                              cpptNone,    0}
};


// prototypes for private helper functions

static void FreeApplication(
    CPI_APPLICATION* pItm
    );
static HRESULT FindObjectForApplication(
    CPI_APPLICATION* pItm,
    BOOL fFindId,
    BOOL fFindName,
    ICatalogObject** ppiAppObj
    );
static HRESULT AddApplicationToActionData(
    CPI_APPLICATION* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );


// function definitions

void CpiApplicationListFree(
    CPI_APPLICATION_LIST* pList
    )
{
    CPI_APPLICATION* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_APPLICATION* pDelete = pItm;
        pItm = pItm->pNext;
        FreeApplication(pDelete);
    }
}

HRESULT CpiApplicationsRead(
    CPI_PARTITION_LIST* pPartList,
    CPI_APPLICATION_LIST* pAppList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    int iVersionNT = 0;

    PMSIHANDLE hView, hRec;

    CPI_APPLICATION* pItm = NULL;
    LPWSTR pwzData = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // get NT version
    hr = WcaGetIntProperty(L"VersionNT", &iVersionNT);
    ExitOnFailure(hr, "Failed to get VersionNT property");

    // loop through all applications
    hr = WcaOpenExecuteView(vcsApplicationQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ComPlusApplication table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, aqComponent, &pwzData);
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
        pItm = (CPI_APPLICATION*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_APPLICATION));
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
        hr = WcaGetRecordString(hRec, aqApplication, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get partition
        if (502 <= iVersionNT)
        {
            hr = WcaGetRecordString(hRec, aqPartition, &pwzData);
            ExitOnFailure(hr, "Failed to get partition");

            if (pwzData && *pwzData)
            {
                hr = CpiPartitionFindByKey(pPartList, pwzData, &pItm->pPartition);
                ExitOnFailure1(hr, "Failed to find partition, key: %S", pwzData);
            }
        }

        // get id
        hr = WcaGetRecordFormattedString(hRec, aqID, &pwzData);
        ExitOnFailure(hr, "Failed to get id");

        if (pwzData && *pwzData)
        {
            hr = PcaGuidToRegFormat(pwzData, pItm->wzID, countof(pItm->wzID));
            ExitOnFailure2(hr, "Failed to parse id guid value, key: %S, value: '%S'", pItm->wzKey, pwzData);
        }

        // get name
        hr = WcaGetRecordFormattedString(hRec, aqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // if application is a locater, either an id or a name must be provided
        if (!pItm->fHasComponent && !*pItm->wzID && !*pItm->wzName)
            ExitOnFailure1(hr = E_FAIL, "An application locater must have either an id or a name associated, key: %S", pItm->wzKey);

        // if application is not a locater, an name must be provided
        if (pItm->fHasComponent && !*pItm->wzName)
            ExitOnFailure1(hr = E_FAIL, "An application must have a name associated, key: %S", pItm->wzKey);

        // get properties
        if (CpiTableExists(cptComPlusApplicationProperty) && pItm->fHasComponent)
        {
            hr = CpiPropertiesRead(vcsApplicationPropertyQuery, pItm->wzKey, pdlApplicationProperties, &pItm->pProperties, &pItm->iPropertyCount);
            ExitOnFailure(hr, "Failed to get properties");
        }

        // set references & increment counters
        if (pItm->fHasComponent)
        {
            if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            {
                if (pItm->pPartition)
                    CpiPartitionAddReferenceInstall(pItm->pPartition);
                pAppList->iInstallCount++;
            }
            if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            {
                if (pItm->pPartition)
                    CpiPartitionAddReferenceUninstall(pItm->pPartition);
                pAppList->iUninstallCount++;
            }
        }

        // add entry
        if (pAppList->pFirst)
            pItm->pNext = pAppList->pFirst;
        pAppList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreeApplication(pItm);

    ReleaseStr(pwzData);

    return hr;
}

HRESULT CpiApplicationsVerifyInstall(
    CPI_APPLICATION_LIST* pList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ICatalogObject* piAppObj = NULL;

    for (CPI_APPLICATION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // referenced locaters or applications that are being installed
        if (!pItm->fReferencedForInstall && !(pItm->fHasComponent && WcaIsInstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // if the application is referensed and is not a locater, it must be installed
        if (pItm->fReferencedForInstall && pItm->fHasComponent && !CpiWillBeInstalled(pItm->isInstalled, pItm->isAction))
            MessageExitOnFailure1(hr = E_FAIL, msierrComPlusApplicationDependency, "An application is used by another entity being installed, but is not installed itself, key: %S", pItm->wzKey);

        // application is supposed to exist
        if (!pItm->fHasComponent || CpiIsInstalled(pItm->isInstalled))
        {
            // get collection object for application
            hr = FindObjectForApplication(pItm, 0 != *pItm->wzID, 0 == *pItm->wzID, &piAppObj);
            ExitOnFailure(hr, "Failed to find collection object for application");

            // if the application was found
            if (S_OK == hr)
            {
                // if we don't have an id, copy id from object
                if (!*pItm->wzID)
                {
                    hr = CpiGetKeyForObject(piAppObj, pItm->wzID, countof(pItm->wzID));
                    ExitOnFailure(hr, "Failed to get id");
                }
            }

            // if the application was not found
            else
            {
                // if the application is a locater, this is an error
                if (!pItm->fHasComponent)
                    MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND), msierrComPlusApplicationNotFound, "An application required by this installation was not found, key: %S", pItm->wzKey);

                // create a new id if one is missing
                if (!*pItm->wzID)
                {
                    hr = CpiCreateId(pItm->wzID, countof(pItm->wzID));
                    ExitOnFailure(hr, "Failed to create id");
                }
            }
        }

        // application is supposed to be created
        else
        {
            // check for conflicts
            do {
                if (*pItm->wzID)
                {
                    // find applications with conflicting id
                    hr = FindObjectForApplication(pItm, TRUE, FALSE, &piAppObj);
                    ExitOnFailure(hr, "Failed to find collection object for application");

                    if (S_FALSE == hr)
                    {
                        // find applications with conflicting name
                        hr = FindObjectForApplication(pItm, FALSE, TRUE, &piAppObj);
                        ExitOnFailure(hr, "Failed to find collection object for application");

                        if (S_OK == hr)
                            // "A application with a conflictiong name exists. retry cancel"
                            er = WcaErrorMessage(msierrComPlusApplicationNameConflict, hr, INSTALLMESSAGE_ERROR | MB_RETRYCANCEL, 0);
                        else
                            break; // no conflicting entry found, break loop
                    }
                    else
                        // "A application with a conflicting id exists. abort retry ignore"
                        er = WcaErrorMessage(msierrComPlusApplicationIdConflict, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                }
                else
                {
                    // find applications with conflicting name
                    hr = FindObjectForApplication(pItm, FALSE, TRUE, &piAppObj);
                    ExitOnFailure(hr, "Failed to find collection object for application");

                    if (S_OK == hr)
                        // "A subscription with a conflictiong name exists. abort retry ignore"
                        er = WcaErrorMessage(msierrComPlusApplicationNameConflict, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                    else
                        break; // no conflicting entry found, break loop
                }

                switch (er)
                {
                case IDCANCEL:
                case IDABORT:
                    ExitOnFailure1(hr = E_FAIL, "An application with a conflictiong name or id exists, key: %S", pItm->wzKey);
                    break;
                case IDRETRY:
                    break;
                case IDIGNORE:
                default:
                    // if we don't have an id, copy id from object
                    if (!*pItm->wzID)
                    {
                        hr = CpiGetKeyForObject(piAppObj, pItm->wzID, countof(pItm->wzID));
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
        ReleaseNullObject(piAppObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppObj);

    return hr;
}

HRESULT CpiApplicationsVerifyUninstall(
    CPI_APPLICATION_LIST* pList
    )
{
    HRESULT hr = S_OK;
    ICatalogObject* piAppObj = NULL;

    for (CPI_APPLICATION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // referenced locaters or applications that are being installed
        if (!pItm->fReferencedForUninstall && !(pItm->fHasComponent && WcaIsUninstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // get collection object for application
        hr = FindObjectForApplication(pItm, 0 != *pItm->wzID, 0 == *pItm->wzID, &piAppObj);
        ExitOnFailure(hr, "Failed to find collection object for application");

        // if the application was found
        if (S_OK == hr)
        {
            // if we don't have an id, copy id from object
            if (!*pItm->wzID)
            {
                hr = CpiGetKeyForObject(piAppObj, pItm->wzID, countof(pItm->wzID));
                ExitOnFailure(hr, "Failed to get id");
            }
        }

        // if the application was not found
        else
        {
            pItm->fObjectNotFound = TRUE;
            if (pItm->fHasComponent)
                pList->iUninstallCount--; // elements with the fObjectNotFound flag set will not be scheduled for uninstall
        }

        // clean up
        ReleaseNullObject(piAppObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppObj);

    return hr;
}

void CpiApplicationAddReferenceInstall(
    CPI_APPLICATION* pItm
    )
{
    pItm->fReferencedForInstall = TRUE;
    if (pItm->pPartition)
        CpiPartitionAddReferenceInstall(pItm->pPartition);
}

void CpiApplicationAddReferenceUninstall(
    CPI_APPLICATION* pItm
    )
{
    pItm->fReferencedForUninstall = TRUE;
    if (pItm->pPartition)
        CpiPartitionAddReferenceUninstall(pItm->pPartition);
}

HRESULT CpiApplicationsInstall(
    CPI_APPLICATION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"CreateComPlusApplications", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add applicaton count to action data
    hr = WcaWriteIntegerToCaData(pList->iInstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add applications to custom action data
    for (CPI_APPLICATION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // applications that are being installed only
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
        hr = AddApplicationToActionData(pItm, iActionType, COST_APPLICATION_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add applicaton to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_APPLICATION_CREATE * pList->iInstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiApplicationsUninstall(
    CPI_APPLICATION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveComPlusApplications", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add applicaton count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add applications to custom action data
    for (CPI_APPLICATION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // applications that are being uninstalled only
        if (!pItm->fHasComponent || !WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // action type
        if (rmRollback == iRunMode)
            iActionType = atCreate;
        else
            iActionType = atRemove;

        // add to action data
        hr = AddApplicationToActionData(pItm, iActionType, COST_APPLICATION_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add applicaton to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_APPLICATION_DELETE * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiApplicationFindByKey(
    CPI_APPLICATION_LIST* pList,
    LPCWSTR pwzKey,
    CPI_APPLICATION** ppApp
    )
{
    for (CPI_APPLICATION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pwzKey))
        {
            *ppApp = pItm;
            return S_OK;
        }
    }

    return S_FALSE;
}

HRESULT CpiGetRolesCollForApplication(
    CPI_APPLICATION* pApp,
    ICatalogCollection** ppiRolesColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piAppColl = NULL;
    ICatalogObject* piAppObj = NULL;

    // if a previous attempt to locate the collection object failed
    if (pApp->fObjectNotFound)
        ExitFunction1(hr = S_FALSE);

    // get applications collection
    if (!pApp->piRolesColl)
    {
        // get applications collection
        if (pApp->pPartition)
            hr = CpiGetApplicationsCollForPartition(pApp->pPartition, &piAppColl);
        else
            hr = CpiGetApplicationsCollection(&piAppColl);
        ExitOnFailure(hr, "Failed to get applications collection");

        if (S_FALSE == hr)
            ExitFunction(); // exit with hr = S_FALSE

        // find application object
        hr = CpiFindCollectionObject(piAppColl, pApp->wzID, *pApp->wzID ? NULL : pApp->wzName, &piAppObj);
        ExitOnFailure(hr, "Failed to find application object");

        if (S_FALSE == hr)
            ExitFunction(); // exit with hr = S_FALSE

        // get roles collection
        hr = CpiGetCatalogCollection(piAppColl, piAppObj, L"Roles", &pApp->piRolesColl);
        ExitOnFailure(hr, "Failed to get roles collection");
    }

    // return value
    *ppiRolesColl = pApp->piRolesColl;
    (*ppiRolesColl)->AddRef();

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppColl);
    ReleaseObject(piAppObj);

    return hr;
}

HRESULT CpiGetComponentsCollForApplication(
    CPI_APPLICATION* pApp,
    ICatalogCollection** ppiCompsColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piAppColl = NULL;
    ICatalogObject* piAppObj = NULL;

    // if a previous attempt to locate the collection object failed
    if (pApp->fObjectNotFound)
        ExitFunction1(hr = S_FALSE);

    // get applications collection
    if (!pApp->piCompsColl)
    {
        // get applications collection
        if (pApp->pPartition)
            hr = CpiGetApplicationsCollForPartition(pApp->pPartition, &piAppColl);
        else
            hr = CpiGetApplicationsCollection(&piAppColl);
        ExitOnFailure(hr, "Failed to get applications collection");

        if (S_FALSE == hr)
            ExitFunction(); // exit with hr = S_FALSE

        // find application object
        hr = CpiFindCollectionObject(piAppColl, pApp->wzID, *pApp->wzID ? NULL : pApp->wzName, &piAppObj);
        ExitOnFailure(hr, "Failed to find application object");

        if (S_FALSE == hr)
            ExitFunction(); // exit with hr = S_FALSE

        // get roles collection
        hr = CpiGetCatalogCollection(piAppColl, piAppObj, L"Components", &pApp->piCompsColl);
        ExitOnFailure(hr, "Failed to get components collection");
    }

    // return value
    *ppiCompsColl = pApp->piCompsColl;
    (*ppiCompsColl)->AddRef();

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppColl);
    ReleaseObject(piAppObj);

    return hr;
}


// helper function definitions

static void FreeApplication(
    CPI_APPLICATION* pItm
    )
{
    if (pItm->pProperties)
        CpiPropertiesFreeList(pItm->pProperties);

    ReleaseObject(pItm->piRolesColl);
    ReleaseObject(pItm->piCompsColl);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static HRESULT FindObjectForApplication(
    CPI_APPLICATION* pItm,
    BOOL fFindId,
    BOOL fFindName,
    ICatalogObject** ppiAppObj
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piAppColl = NULL;

    // get applications collection
    if (pItm->pPartition)
        hr = CpiGetApplicationsCollForPartition(pItm->pPartition, &piAppColl);
    else
        hr = CpiGetApplicationsCollection(&piAppColl);
    ExitOnFailure(hr, "Failed to get applications collection");

    if (S_FALSE == hr)
        ExitFunction(); // exit with hr = S_FALSE

    // find application object
    hr = CpiFindCollectionObject(piAppColl, fFindId ? pItm->wzID : NULL, fFindName ? pItm->wzName : NULL, ppiAppObj);
        ExitOnFailure(hr, "Failed to find application object");

    // exit with hr from CpiFindCollectionObject()

LExit:
    // clean up
    ReleaseObject(piAppColl);

    return hr;
}

static HRESULT AddApplicationToActionData(
    CPI_APPLICATION* pItm,
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

    // add application information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application key to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application id to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application name to custom action data");

    // add partition information to custom action data
    hr = WcaWriteStringToCaData(pItm->pPartition ? pItm->pPartition->wzID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    // add properties to custom action data
    hr = CpiAddPropertiesToActionData(atCreate == iActionType ? pItm->iPropertyCount : 0, pItm->pProperties, ppwzActionData);
    ExitOnFailure(hr, "Failed to add properties to custom action data");

    hr = S_OK;

LExit:
    return hr;
}
