//-------------------------------------------------------------------------------------------------
// <copyright file="cpsubssched.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ subscription functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// sql queries

LPCWSTR vcsSubscriptionQuery =
    L"SELECT `Subscription`, `ComPlusComponent_`, `Component_`, `Id`, `Name`, `EventCLSID`, `PublisherID` FROM `ComPlusSubscription`";
enum eSubscriptionQuery { sqSubscription = 1, sqComPlusComponent, sqComponent, sqID, sqName, sqEventCLSID, sqPublisherID };

LPCWSTR vcsSubscriptionPropertyQuery =
    L"SELECT `Name`, `Value` FROM `ComPlusSubscriptionProperty` WHERE `Subscription_` = ?";


// property definitions

CPI_PROPERTY_DEFINITION pdlSubscriptionProperties[] =
{
    {L"Description",           cpptString,  500},
    {L"Enabled",               cpptBoolean, 500},
    {L"EventClassPartitionID", cpptString,  502},
    {L"FilterCriteria",        cpptString,  500},
    {L"InterfaceID",           cpptString,  500},
    {L"MachineName",           cpptString,  500},
    {L"MethodName",            cpptString,  500},
    {L"PerUser",               cpptBoolean, 500},
    {L"Queued",                cpptBoolean, 500},
    {L"SubscriberMoniker",     cpptString,  500},
    {L"UserName",              cpptUser,    500},
    {NULL,                     cpptNone,    0}
};


// prototypes for private helper functions

static void FreeSubscription(
    CPI_SUBSCRIPTION* pItm
    );
static HRESULT FindObjectForSubscription(
    CPI_SUBSCRIPTION* pItm,
    BOOL fFindId,
    BOOL fFindName,
    ICatalogObject** ppiSubsObj
    );
static HRESULT AddSubscriptionToActionData(
    CPI_SUBSCRIPTION* pItm,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );
static HRESULT ComponentFindByKey(
    CPI_ASSEMBLY_LIST* pAsmList,
    LPCWSTR pwzKey,
    CPI_ASSEMBLY** ppAsmItm,
    CPI_COMPONENT** ppCompItm
    );


// function definitions

void CpiSubscriptionListFree(
    CPI_SUBSCRIPTION_LIST* pList
    )
{
    CPI_SUBSCRIPTION* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_SUBSCRIPTION* pDelete = pItm;
        pItm = pItm->pNext;
        FreeSubscription(pDelete);
    }
}

HRESULT CpiSubscriptionsRead(
    CPI_ASSEMBLY_LIST* pAsmList,
    CPI_SUBSCRIPTION_LIST* pSubList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    CPI_SUBSCRIPTION* pItm = NULL;
    LPWSTR pwzData = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // loop through all applications
    hr = WcaOpenExecuteView(vcsSubscriptionQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ComPlusSubscription table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, sqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get component");

        // check if the component is our processor architecture
        hr = CpiVerifyComponentArchitecure(pwzData, &fMatchingArchitecture);
        ExitOnFailure(hr, "Failed to get component architecture.");

        if (!fMatchingArchitecture)
        {
            continue; // not the same architecture, ignore
        }

        // create entry
        pItm = (CPI_SUBSCRIPTION*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_SUBSCRIPTION));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get key
        hr = WcaGetRecordString(hRec, sqSubscription, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get com+ component
        hr = WcaGetRecordString(hRec, sqComPlusComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get COM+ component");

        hr = ComponentFindByKey(pAsmList, pwzData, &pItm->pAssembly, &pItm->pComponent);

        if (S_FALSE == hr)
        {
            // component not found
            ExitOnFailure1(hr = E_FAIL, "Failed to find component, key: %S", pwzData);
        }

        // get id
        hr = WcaGetRecordFormattedString(hRec, sqID, &pwzData);
        ExitOnFailure(hr, "Failed to get id");

        if (pwzData && *pwzData)
        {
            hr = PcaGuidToRegFormat(pwzData, pItm->wzID, countof(pItm->wzID));
            ExitOnFailure2(hr, "Failed to parse id guid value, key: %S, value: '%S'", pItm->wzKey, pwzData);
        }

        // get name
        hr = WcaGetRecordFormattedString(hRec, sqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // get event clsid
        hr = WcaGetRecordFormattedString(hRec, sqEventCLSID, &pwzData);
        ExitOnFailure(hr, "Failed to get event clsid");
        StringCchCopyW(pItm->wzEventCLSID, countof(pItm->wzEventCLSID), pwzData);

        // get publisher id
        hr = WcaGetRecordFormattedString(hRec, sqPublisherID, &pwzData);
        ExitOnFailure(hr, "Failed to get publisher id");
        StringCchCopyW(pItm->wzPublisherID, countof(pItm->wzPublisherID), pwzData);

        // get properties
        if (CpiTableExists(cptComPlusSubscriptionProperty))
        {
            hr = CpiPropertiesRead(vcsSubscriptionPropertyQuery, pItm->wzKey, pdlSubscriptionProperties, &pItm->pProperties, &pItm->iPropertyCount);
            ExitOnFailure(hr, "Failed to get subscription properties");
        }

        // set references & increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
        {
            CpiApplicationAddReferenceInstall(pItm->pAssembly->pApplication);
            pItm->pAssembly->fReferencedForInstall = TRUE;
            pSubList->iInstallCount++;
            if (pItm->pAssembly->iAttributes & aaRunInCommit)
                pSubList->iCommitCount++;
        }
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
        {
            CpiApplicationAddReferenceUninstall(pItm->pAssembly->pApplication);
            pItm->pAssembly->fReferencedForUninstall = TRUE;
            pSubList->iUninstallCount++;
        }

        // add entry
        if (pSubList->pFirst)
            pItm->pNext = pSubList->pFirst;
        pSubList->pFirst = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        FreeSubscription(pItm);

    ReleaseStr(pwzData);

    return hr;
}

HRESULT CpiSubscriptionsVerifyInstall(
    CPI_SUBSCRIPTION_LIST* pList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ICatalogObject* piSubsObj = NULL;

    for (CPI_SUBSCRIPTION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // subscriptions that are being installed
        if (!WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // subscription is supposed to exist
        if (CpiIsInstalled(pItm->isInstalled))
        {
            // if we don't have an id
            if (!*pItm->wzID)
            {
                // find subscriptions with conflicting name
                hr = FindObjectForSubscription(pItm, FALSE, TRUE, &piSubsObj);
                ExitOnFailure(hr, "Failed to find collection object for subscription");

                // if the subscription was found
                if (S_OK == hr)
                {
                    // get id from subscription object
                    hr = CpiGetKeyForObject(piSubsObj, pItm->wzID, countof(pItm->wzID));
                    ExitOnFailure(hr, "Failed to get id");
                }

                // if the subscription was not found
                else
                {
                    // create a new id
                    hr = CpiCreateId(pItm->wzID, countof(pItm->wzID));
                    ExitOnFailure(hr, "Failed to create id");
                }
            }
        }

        // subscription is supposed to be created
        else
        {
            // check for conflicts
            do {
                if (*pItm->wzID)
                {
                    // find subscriptions with conflicting id
                    hr = FindObjectForSubscription(pItm, TRUE, FALSE, &piSubsObj);
                    ExitOnFailure(hr, "Failed to find collection object for subscription");

                    if (S_FALSE == hr)
                    {
                        // find subscriptions with conflicting name
                        hr = FindObjectForSubscription(pItm, FALSE, TRUE, &piSubsObj);
                        ExitOnFailure(hr, "Failed to find collection object for subscription");

                        if (S_OK == hr)
                            // "A subscription with a conflictiong name exists. retry cancel"
                            er = WcaErrorMessage(msierrComPlusSubscriptionNameConflict, hr, INSTALLMESSAGE_ERROR | MB_RETRYCANCEL, 0);
                        else
                            break; // no conflicting entry found, break loop
                    }
                    else
                        // "A subscription with a conflicting id exists. abort retry ignore"
                        er = WcaErrorMessage(msierrComPlusSubscriptionIdConflict, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                }
                else
                {
                    // find subscriptions with conflicting name
                    hr = FindObjectForSubscription(pItm, FALSE, TRUE, &piSubsObj);
                    ExitOnFailure(hr, "Failed to find collection object for subscription");

                    if (S_OK == hr)
                        // "A subscription with a conflictiong name exists. abort retry ignore"
                        er = WcaErrorMessage(msierrComPlusSubscriptionNameConflict, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
                    else
                        break; // no conflicting entry found, break loop
                }

                switch (er)
                {
                case IDCANCEL:
                case IDABORT:
                    ExitOnFailure1(hr = E_FAIL, "A subscription with a conflictiong name or id exists, key: %S", pItm->wzKey);
                    break;
                case IDRETRY:
                    break;
                case IDIGNORE:
                default:
                    // if we don't have an id, copy id from object
                    if (!*pItm->wzID)
                    {
                        hr = CpiGetKeyForObject(piSubsObj, pItm->wzID, countof(pItm->wzID));
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
        ReleaseNullObject(piSubsObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piSubsObj);

    return hr;
}

HRESULT CpiSubscriptionsVerifyUninstall(
    CPI_SUBSCRIPTION_LIST* pList
    )
{
    HRESULT hr = S_OK;
    ICatalogObject* piSubsObj = NULL;

    for (CPI_SUBSCRIPTION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // subscriptions that are being installed
        if (!WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // find subscriptions with conflicting name
        hr = FindObjectForSubscription(pItm, 0 != *pItm->wzID, 0 == *pItm->wzID, &piSubsObj);
        ExitOnFailure(hr, "Failed to find collection object for subscription");

        // if the subscription was found
        if (S_OK == hr)
        {
            // if we don't have an id, copy id from object
            if (!*pItm->wzID)
            {
                hr = CpiGetKeyForObject(piSubsObj, pItm->wzID, countof(pItm->wzID));
                ExitOnFailure(hr, "Failed to get id");
            }
        }

        // if the subscription was not found
        else
        {
            pItm->fObjectNotFound = TRUE;
            pList->iUninstallCount--; // elements with the fObjectNotFound flag set will not be scheduled for uninstall
        }

        // clean up
        ReleaseNullObject(piSubsObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piSubsObj);

    return hr;
}

HRESULT CpiSubscriptionsInstall(
    CPI_SUBSCRIPTION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;
    int iCount = 0;

    // add action text
    hr = CpiAddActionTextToActionData(L"CreateSubscrComPlusComponents", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // subscription count
    switch (iRunMode)
    {
    case rmDeferred:
        iCount = pList->iInstallCount - pList->iCommitCount;
        break;
    case rmCommit:
        iCount = pList->iCommitCount;
        break;
    case rmRollback:
        iCount = pList->iInstallCount;
        break;
    }

    // add subscription count to action data
    hr = WcaWriteIntegerToCaData(iCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add assemblies to custom action data in forward order
    for (CPI_SUBSCRIPTION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // roles that are being installed only
        if ((rmCommit == iRunMode && !(pItm->pAssembly->iAttributes & aaRunInCommit)) ||
            (rmDeferred == iRunMode && (pItm->pAssembly->iAttributes & aaRunInCommit)) ||
            !WcaIsInstalling(pItm->isInstalled, pItm->isAction))
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
        hr = AddSubscriptionToActionData(pItm, iActionType, COST_SUBSCRIPTION_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add subscription to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_SUBSCRIPTION_CREATE * pList->iInstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiSubscriptionsUninstall(
    CPI_SUBSCRIPTION_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveSubscrComPlusComponents", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add subscription count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add assemblies to custom action data in reverse order
    for (CPI_SUBSCRIPTION* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
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
        hr = AddSubscriptionToActionData(pItm, iActionType, COST_SUBSCRIPTION_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add subscription to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_SUBSCRIPTION_DELETE * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}


// helper function definitions

static void FreeSubscription(
    CPI_SUBSCRIPTION* pItm
    )
{
    if (pItm->pProperties)
        CpiPropertiesFreeList(pItm->pProperties);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static HRESULT FindObjectForSubscription(
    CPI_SUBSCRIPTION* pItm,
    BOOL fFindId,
    BOOL fFindName,
    ICatalogObject** ppiSubsObj
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piSubsColl = NULL;

    // get applications collection
    hr = CpiGetSubscriptionsCollForComponent(pItm->pAssembly, pItm->pComponent, &piSubsColl);
    ExitOnFailure(hr, "Failed to get collection");

    if (S_FALSE == hr)
        ExitFunction(); // exit with hr = S_FALSE

    // find application object
    hr = CpiFindCollectionObject(piSubsColl, fFindId ? pItm->wzID : NULL, fFindName ? pItm->wzName : NULL, ppiSubsObj);
    ExitOnFailure(hr, "Failed to find object");

    // exit with hr from CpiFindCollectionObject()

LExit:
    // clean up
    ReleaseObject(piSubsColl);

    return hr;
}

static HRESULT AddSubscriptionToActionData(
    CPI_SUBSCRIPTION* pItm,
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
    ExitOnFailure(hr, "Failed to add subscription key to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add subscription id to custom action data");
    hr = WcaWriteStringToCaData(pItm->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add subscription name to custom action data");
    hr = WcaWriteStringToCaData(atCreate == iActionType ? pItm->wzEventCLSID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly tlb path to custom action data");
    hr = WcaWriteStringToCaData(atCreate == iActionType ? pItm->wzPublisherID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly proxy-stub dll path to custom action data");

    // add component information to custom action data
    hr = WcaWriteStringToCaData(pItm->pComponent->wzCLSID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application id to custom action data");

    // add application information to custom action data
    hr = WcaWriteStringToCaData(pItm->pAssembly->pApplication->wzID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add application id to custom action data");

    // add partition information to custom action data
    LPCWSTR pwzPartID = pItm->pAssembly->pApplication->pPartition ? pItm->pAssembly->pApplication->pPartition->wzID : L"";
    hr = WcaWriteStringToCaData(pwzPartID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    // add properties to custom action data
    hr = CpiAddPropertiesToActionData(atCreate == iActionType ? pItm->iPropertyCount : 0, pItm->pProperties, ppwzActionData);
    ExitOnFailure(hr, "Failed to add properties to custom action data");

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT ComponentFindByKey(
    CPI_ASSEMBLY_LIST* pAsmList,
    LPCWSTR pwzKey,
    CPI_ASSEMBLY** ppAsmItm,
    CPI_COMPONENT** ppCompItm
    )
{
    for (CPI_ASSEMBLY* pAsmItm = pAsmList->pFirst; pAsmItm; pAsmItm = pAsmItm->pNext)
    {
        for (CPI_COMPONENT* pCompItm = pAsmItm->pComponents; pCompItm; pCompItm = pCompItm->pNext)
        {
            if (0 == lstrcmpW(pCompItm->wzKey, pwzKey))
            {
                *ppAsmItm = pAsmItm;
                *ppCompItm = pCompItm;
                return S_OK;
            }
        }
    }

    return S_FALSE;
}
