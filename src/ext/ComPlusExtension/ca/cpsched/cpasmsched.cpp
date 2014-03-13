//-------------------------------------------------------------------------------------------------
// <copyright file="cpasmsched.cpp" company="Outercurve Foundation">
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

#include "precomp.h"


// sql queries

LPCWSTR vcsMsiAssemblyNameQuery =
    L"SELECT `Name`, `Value` FROM `MsiAssemblyName` WHERE `Component_` = ?";
enum eMsiAssemblyNameQuery { manqName = 1, manqValue };

LPCWSTR vcsModuleQuery =
    L"SELECT `ModuleID` FROM `ModuleSignature`";
enum eModuleQuery { mqModule = 1 };

LPCWSTR vcsAssemblyQuery =
    L"SELECT `Assembly`, `Component_`, `Application_`, `AssemblyName`, `DllPath`, `TlbPath`, `PSDllPath`, `Attributes` FROM `ComPlusAssembly`";
enum eAssemblyQuery { aqAssembly = 1, aqComponent, aqApplication, aqAssemblyName, aqDllPath, aqTlbPath, aqPSDllPath, aqAttributes };

LPCWSTR vcsComponentQuery =
    L"SELECT `ComPlusComponent`, `CLSID` FROM `ComPlusComponent` WHERE `Assembly_` = ?";
enum eComponentQuery { cqComponent = 1, cqCLSID };

LPCWSTR vcsComponentPropertyQuery =
    L"SELECT `Name`, `Value` FROM `ComPlusComponentProperty` WHERE `ComPlusComponent_` = ?";

LPCWSTR vcsInterfaceQuery =
    L"SELECT `Interface`, `IID` FROM `ComPlusInterface` WHERE `ComPlusComponent_` = ?";
enum eInterfaceQuery { iqInterface = 1, iqIID };

LPCWSTR vcsInterfacePropertyQuery =
    L"SELECT `Name`, `Value` FROM `ComPlusInterfaceProperty` WHERE `Interface_` = ?";

LPCWSTR vcsMethodQuery =
    L"SELECT `Method`, `Index`, `Name` FROM `ComPlusMethod` WHERE `Interface_` = ?";
enum eMethodQuery { mqMethod = 1, mqIndex, mqName };

LPCWSTR vcsMethodPropertyQuery =
    L"SELECT `Name`, `Value` FROM `ComPlusMethodProperty` WHERE `Method_` = ?";

LPCWSTR vcsRoleForComponentQuery =
    L"SELECT `RoleForComponent`, `ApplicationRole_`, `Component_` FROM `ComPlusRoleForComponent` WHERE `ComPlusComponent_` = ?";
LPCWSTR vcsRoleForInterfaceQuery =
    L"SELECT `RoleForInterface`, `ApplicationRole_`, `Component_` FROM `ComPlusRoleForInterface` WHERE `Interface_` = ?";
LPCWSTR vcsRoleForMethodQuery =
    L"SELECT `RoleForMethod`, `ApplicationRole_`, `Component_` FROM `ComPlusRoleForMethod` WHERE `Method_` = ?";

enum eRoleAssignmentQuery { raqKey = 1, raqApplicationRole, raqComponent };

LPCWSTR vcsModuleComponentsQuery =
    L"SELECT `Component`, `ModuleID` FROM `ModuleComponents`";
LPCWSTR vcsModuleDependencyQuery =
    L"SELECT `ModuleID`, `RequiredID` FROM `ModuleDependency`";
LPCWSTR vcsAssemblyDependencyQuery =
    L"SELECT `Assembly_`, `RequiredAssembly_` FROM `ComPlusAssemblyDependency`";

enum eKeyPairQuery { kpqFirstKey = 1, kpqSecondKey };


// private structs

struct CPI_KEY_PAIR
{
    WCHAR wzFirstKey[MAX_DARWIN_KEY + 1];
    WCHAR wzSecondKey[MAX_DARWIN_KEY + 1];

    CPI_KEY_PAIR* pNext;
};

struct CPI_DEPENDENCY_CHAIN
{
    LPCWSTR pwzKey;

    CPI_DEPENDENCY_CHAIN* pPrev;
};

struct CPI_MODULE
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];

    CPI_MODULE* pPrev;
    CPI_MODULE* pNext;
};

struct CPI_MODULE_LIST
{
    CPI_MODULE* pFirst;
    CPI_MODULE* pLast;
};


// property definitions

CPI_PROPERTY_DEFINITION pdlComponentProperties[] =
{
    {L"AllowInprocSubscribers",             cpptBoolean, 500},
    {L"ComponentAccessChecksEnabled",       cpptBoolean, 500},
    {L"ComponentTransactionTimeout",        cpptInteger, 500},
    {L"ComponentTransactionTimeoutEnabled", cpptBoolean, 500},
    {L"COMTIIntrinsics",                    cpptBoolean, 500},
    {L"ConstructionEnabled",                cpptBoolean, 500},
    {L"ConstructorString",                  cpptString,  500},
    {L"CreationTimeout",                    cpptInteger, 500},
    {L"Description",                        cpptString,  500},
    {L"EventTrackingEnabled",               cpptBoolean, 500},
    {L"ExceptionClass",                     cpptString,  500},
    {L"FireInParallel",                     cpptBoolean, 500},
    {L"IISIntrinsics",                      cpptBoolean, 500},
    {L"InitializesServerApplication",       cpptBoolean, 500},
    {L"IsEnabled",                          cpptBoolean, 501},
    {L"IsPrivateComponent",                 cpptBoolean, 501},
    {L"JustInTimeActivation",               cpptBoolean, 500},
    {L"LoadBalancingSupported",             cpptBoolean, 500},
    {L"MaxPoolSize",                        cpptInteger, 500},
    {L"MinPoolSize",                        cpptInteger, 500},
    {L"MultiInterfacePublisherFilterCLSID", cpptString,  500},
    {L"MustRunInClientContext",             cpptBoolean, 500},
    {L"MustRunInDefaultContext",            cpptBoolean, 501},
    {L"ObjectPoolingEnabled",               cpptBoolean, 500},
    {L"PublisherID",                        cpptString,  500},
    {L"SoapAssemblyName",                   cpptString,  502},
    {L"SoapTypeName",                       cpptString,  502},
    {L"Synchronization",                    cpptInteger, 500},
    {L"Transaction",                        cpptInteger, 500},
    {L"TxIsolationLevel",                   cpptInteger, 501},
    {NULL,                                  cpptNone,    0}
};

CPI_PROPERTY_DEFINITION pdlInterfaceProperties[] =
{
    {L"Description",    cpptString,  500},
    {L"QueuingEnabled", cpptBoolean, 500},
    {NULL,              cpptNone,    0}
};

CPI_PROPERTY_DEFINITION pdlMethodProperties[] =
{
    {L"AutoComplete", cpptBoolean, 500},
    {L"Description",  cpptString,  500},
    {NULL,            cpptNone,    0}
};


// prototypes for private helper functions

static HRESULT GetAssemblyName(
    LPCWSTR pwzComponent,
    LPWSTR* ppwzAssemblyName
    );
static HRESULT KeyPairsRead(
    LPCWSTR pwzQuery,
    CPI_KEY_PAIR** ppKeyPairList
    );
static HRESULT ModulesRead(
    CPI_MODULE_LIST* pModList
    );
static HRESULT AssembliesRead(
    CPI_KEY_PAIR* pModCompList,
    CPI_APPLICATION_LIST* pAppList,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY_LIST* pAsmList
    );
static HRESULT ComponentsRead(
    LPCWSTR pwzAsmKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY* pAsm
    );
static HRESULT InterfacesRead(
    LPCWSTR pwzCompKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY* pAsm,
    CPI_COMPONENT* pComp
    );
static HRESULT MethodsRead(
    LPCWSTR pwzIntfKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY* pAsm,
    CPI_INTERFACE* pIntf
    );
static HRESULT RoleAssignmentsRead(
    LPCWSTR pwzQuery,
    LPCWSTR pwzKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ROLE_ASSIGNMENT** ppRoleList,
    int* piInstallCount,
    int* piUninstallCount
    );
static HRESULT TopSortModuleList(
    CPI_KEY_PAIR* pDepList,
    CPI_MODULE_LIST* pList
    );
static HRESULT SwapDependentModules(
    CPI_DEPENDENCY_CHAIN* pdcPrev,
    CPI_KEY_PAIR* pDepList,
    CPI_MODULE_LIST* pList,
    CPI_MODULE* pRoot,
    CPI_MODULE* pItm
    );
static HRESULT ModuleFindByKey(
    CPI_MODULE* pItm,
    LPCWSTR pwzKey,
    BOOL fReverse,
    CPI_MODULE** ppItm
    );
static void SortAssemblyListByModule(
    CPI_MODULE_LIST* pModList,
    CPI_ASSEMBLY_LIST* pAsmList
    );
static HRESULT TopSortAssemblyList(
    CPI_KEY_PAIR* pDepList,
    CPI_ASSEMBLY_LIST* pList
    );
static HRESULT SwapDependentAssemblies(
    CPI_DEPENDENCY_CHAIN* pdcPrev,
    CPI_KEY_PAIR* pDepList,
    CPI_ASSEMBLY_LIST* pList,
    CPI_ASSEMBLY* pRoot,
    CPI_ASSEMBLY* pItm
    );
static HRESULT AssemblyFindByKey(
    CPI_ASSEMBLY* pItm,
    LPCWSTR pwzKey,
    BOOL fReverse,
    CPI_ASSEMBLY** ppItm
    );
static HRESULT AddAssemblyToActionData(
    CPI_ASSEMBLY* pItm,
    BOOL fInstall,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );
static HRESULT AddRoleAssignmentsToActionData(
    CPI_ASSEMBLY* pItm,
    BOOL fInstall,
    int iActionType,
    int iActionCost,
    LPWSTR* ppwzActionData
    );
static HRESULT AddComponentToActionData(
    CPI_COMPONENT* pItm,
    BOOL fInstall,
    BOOL fProps,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    );
static HRESULT AddInterfaceToActionData(
    CPI_INTERFACE* pItm,
    BOOL fInstall,
    BOOL fProps,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    );
static HRESULT AddMethodToActionData(
    CPI_METHOD* pItm,
    BOOL fInstall,
    BOOL fProps,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    );
static HRESULT AddRolesToActionData(
    int iRoleInstallCount,
    int iRoleUninstallCount,
    CPI_ROLE_ASSIGNMENT* pRoleList,
    BOOL fInstall,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    );
static HRESULT KeyPairFindByFirstKey(
    CPI_KEY_PAIR* pList,
    LPCWSTR pwzKey,
    CPI_KEY_PAIR** ppItm
    );
static void AssemblyFree(
    CPI_ASSEMBLY* pItm
    );
static void KeyPairsFreeList(
    CPI_KEY_PAIR* pList
    );
void ModuleListFree(
    CPI_MODULE_LIST* pList
    );
static void ModuleFree(
    CPI_MODULE* pItm
    );
static void ComponentsFreeList(
    CPI_COMPONENT* pList
    );
static void InterfacesFreeList(
    CPI_INTERFACE* pList
    );
static void MethodsFreeList(
    CPI_METHOD* pList
    );
static void RoleAssignmentsFreeList(
    CPI_ROLE_ASSIGNMENT* pList
    );


// function definitions

void CpiAssemblyListFree(
    CPI_ASSEMBLY_LIST* pList
    )
{
    CPI_ASSEMBLY* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_ASSEMBLY* pDelete = pItm;
        pItm = pItm->pNext;
        AssemblyFree(pDelete);
    }
}

HRESULT CpiAssembliesRead(
    CPI_APPLICATION_LIST* pAppList,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY_LIST* pAsmList
    )
{
    HRESULT hr = S_OK;
    CPI_MODULE_LIST modList;
    CPI_KEY_PAIR* pModCompList = NULL;
    CPI_KEY_PAIR* pModDepList = NULL;
    CPI_KEY_PAIR* pAsmDepList = NULL;

    ::ZeroMemory(&modList, sizeof(CPI_MODULE_LIST));

    BOOL fModuleSignatureTable = (S_OK == WcaTableExists(L"ModuleSignature"));
    BOOL fModuleComponentsTable = (S_OK == WcaTableExists(L"ModuleComponents"));
    BOOL fModuleDependencyTable = (S_OK == WcaTableExists(L"ModuleDependency"));

    // read modules
    if (fModuleSignatureTable)
    {
        hr = ModulesRead(&modList);
        ExitOnFailure(hr, "Failed to read ModuleSignature table");
    }

    // read module components
    if (fModuleComponentsTable)
    {
        hr = KeyPairsRead(vcsModuleComponentsQuery, &pModCompList);
        ExitOnFailure(hr, "Failed to read ModuleComponents table");
    }

    // read module dependencies
    if (fModuleDependencyTable)
    {
        hr = KeyPairsRead(vcsModuleDependencyQuery, &pModDepList);
        ExitOnFailure(hr, "Failed to read ModuleDependency table");
    }

    // read assemblies
    hr = AssembliesRead(pModCompList, pAppList, pAppRoleList, pAsmList);
    ExitOnFailure(hr, "Failed to read ComPlusAssembly table");

    // read assembly dependencies
    if (CpiTableExists(cptComPlusAssemblyDependency))
    {
        hr = KeyPairsRead(vcsAssemblyDependencyQuery, &pAsmDepList);
        ExitOnFailure(hr, "Failed to read ComPlusAssemblyDependency table");
    }

    // sort modules
    if (modList.pFirst && pModDepList)
    {
        hr = TopSortModuleList(pModDepList, &modList);
        ExitOnFailure(hr, "Failed to sort modules");
    }

    // sort assemblies by module
    if (pAsmList->pFirst && modList.pFirst && pModDepList)
        SortAssemblyListByModule(&modList, pAsmList);

    // sort assemblies by dependency
    if (pAsmList->pFirst && pAsmDepList)
    {
        hr = TopSortAssemblyList(pAsmDepList, pAsmList);
        ExitOnFailure(hr, "Failed to sort assemblies");
    }

    hr = S_OK;

LExit:
    // clean up
    ModuleListFree(&modList);
    if (pModCompList)
        KeyPairsFreeList(pModCompList);
    if (pModDepList)
        KeyPairsFreeList(pModDepList);
    if (pAsmDepList)
        KeyPairsFreeList(pAsmDepList);

    return hr;
}

HRESULT CpiAssembliesVerifyInstall(
    CPI_ASSEMBLY_LIST* pList
    )
{
    HRESULT hr = S_OK;

    for (CPI_ASSEMBLY* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // assemblies that are being installed
        if (!pItm->fReferencedForInstall && !pItm->iRoleAssignmentsInstallCount && !WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // if the assembly is referensed, it must be installed
        if ((pItm->fReferencedForInstall || pItm->iRoleAssignmentsInstallCount) && !CpiWillBeInstalled(pItm->isInstalled, pItm->isAction))
            MessageExitOnFailure1(hr = E_FAIL, msierrComPlusAssemblyDependency, "An assembly is used by another entity being installed, but is not installed itself, key: %S", pItm->wzKey);
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiAssembliesVerifyUninstall(
    CPI_ASSEMBLY_LIST* pList
    )
{
    HRESULT hr = S_OK;

    for (CPI_ASSEMBLY* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // assemblies that are being uninstalled
        if (!pItm->fReferencedForUninstall && !pItm->iRoleAssignmentsUninstallCount && (!WcaIsUninstalling(pItm->isInstalled, pItm->isAction) && !WcaIsReInstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // if the application is not present, there is no need to remove the components
        if (pItm->pApplication && pItm->pApplication->fObjectNotFound)
        {
            pItm->fIgnore = TRUE;
            pList->iUninstallCount--; // elements with the fIgnore flag set will not be scheduled for uninstall
            pList->iRoleUninstallCount--;
        }
    }

    hr = S_OK;

//LExit:
    return hr;
}

HRESULT CpiAssembliesInstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;
    int iCount = 0;

    // add action text
    hr = CpiAddActionTextToActionData(L"RegisterComPlusAssemblies", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // assembly count
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

    // add assembly count to action data
    hr = WcaWriteIntegerToCaData(iCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add assemblies to custom action data in forward order
    for (CPI_ASSEMBLY* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // assemblies that are being installed, or contains roll assignments to install
        if (!WcaIsInstalling(pItm->isInstalled, pItm->isAction))
            continue;

        // assemblies that are being installed must be scheduled during the right type of action
        BOOL fRunInCommit = 0 != (pItm->iAttributes & aaRunInCommit);
        if (((rmCommit == iRunMode && !fRunInCommit) || (rmDeferred == iRunMode && fRunInCommit)))
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
        hr = AddAssemblyToActionData(pItm, TRUE, iActionType, COST_ASSEMBLY_REGISTER, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add assembly to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_ASSEMBLY_REGISTER * iCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiAssembliesUninstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"UnregisterComPlusAssemblies", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add assembly count to action data
    hr = WcaWriteIntegerToCaData(pList->iUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add assemblies to custom action data in reverse order
    for (CPI_ASSEMBLY* pItm = pList->pLast; pItm; pItm = pItm->pPrev)
    {
        // assemblies that are being uninstalled
        if (pItm->fIgnore || (!WcaIsUninstalling(pItm->isInstalled, pItm->isAction) && !WcaIsReInstalling(pItm->isInstalled, pItm->isAction)))
            continue;

        // action type
        if (rmRollback == iRunMode)
            iActionType = atCreate;
        else
            iActionType = atRemove;

        // add to action data
        hr = AddAssemblyToActionData(pItm, FALSE, iActionType, COST_ASSEMBLY_UNREGISTER, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add assembly to custom action data, key: %S", pItm->wzKey);
    }

    // add progress tics
    if (piProgress)
        *piProgress += COST_ASSEMBLY_UNREGISTER * pList->iUninstallCount;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiRoleAssignmentsInstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;
    int iCount = 0;

    // add action text
    hr = CpiAddActionTextToActionData(L"AddComPlusRoleAssignments", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // assembly count
    switch (iRunMode)
    {
    case rmDeferred:
        iCount = pList->iRoleInstallCount - pList->iRoleCommitCount;
        break;
    case rmCommit:
        iCount = pList->iRoleCommitCount;
        break;
    case rmRollback:
        iCount = pList->iRoleInstallCount;
        break;
    }

    // add assembly count to action data
    hr = WcaWriteIntegerToCaData(iCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add assemblies to custom action data in forward order
    for (CPI_ASSEMBLY* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // assemblies that are being installed, or contains roll assignments to install
        if (!pItm->iRoleAssignmentsInstallCount)
            continue;

        // assemblies that are being installed must be scheduled during the right type of action
        BOOL fRunInCommit = 0 != (pItm->iAttributes & aaRunInCommit);
        if (((rmCommit == iRunMode && !fRunInCommit) || (rmDeferred == iRunMode && fRunInCommit)))
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
        hr = AddRoleAssignmentsToActionData(pItm, TRUE, iActionType, COST_ROLLASSIGNMENT_CREATE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add assembly to custom action data, key: %S", pItm->wzKey);

        // add progress tics
        if (piProgress)
            *piProgress += COST_ROLLASSIGNMENT_CREATE * pItm->iRoleAssignmentsInstallCount;
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiRoleAssignmentsUninstall(
    CPI_ASSEMBLY_LIST* pList,
    int iRunMode,
    LPWSTR* ppwzActionData,
    int* piProgress
    )
{
    HRESULT hr = S_OK;

    int iActionType;

    // add action text
    hr = CpiAddActionTextToActionData(L"RemoveComPlusRoleAssignments", ppwzActionData);
    ExitOnFailure(hr, "Failed to add action text to custom action data");

    // add assembly count to action data
    hr = WcaWriteIntegerToCaData(pList->iRoleUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    // add assemblies to custom action data in reverse order
    for (CPI_ASSEMBLY* pItm = pList->pLast; pItm; pItm = pItm->pPrev)
    {
        // assemblies that are being uninstalled
        if (pItm->fIgnore || !pItm->iRoleAssignmentsUninstallCount)
            continue;

        // action type
        if (rmRollback == iRunMode)
            iActionType = atCreate;
        else
            iActionType = atRemove;

        // add to action data
        hr = AddRoleAssignmentsToActionData(pItm, FALSE, iActionType, COST_ROLLASSIGNMENT_DELETE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add assembly to custom action data, key: %S", pItm->wzKey);

        // add progress tics
        if (piProgress)
            *piProgress += COST_ROLLASSIGNMENT_DELETE * pItm->iRoleAssignmentsUninstallCount;
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetSubscriptionsCollForComponent(
    CPI_ASSEMBLY* pAsm,
    CPI_COMPONENT* pComp,
    ICatalogCollection** ppiSubsColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piCompColl = NULL;
    ICatalogObject* piCompObj = NULL;

    // get applications collection
    if (!pComp->piSubsColl)
    {
        // get components collection for application
        hr = CpiGetComponentsCollForApplication(pAsm->pApplication, &piCompColl);
        ExitOnFailure(hr, "Failed to get components collection for application");

        if (S_FALSE == hr)
            ExitFunction(); // exit with hr = S_FALSE

        // find component object
        hr = CpiFindCollectionObject(piCompColl, pComp->wzCLSID, NULL, &piCompObj);
        ExitOnFailure(hr, "Failed to find component object");

        if (S_FALSE == hr)
            ExitFunction(); // exit with hr = S_FALSE

        // get roles collection
        hr = CpiGetCatalogCollection(piCompColl, piCompObj, L"SubscriptionsForComponent", &pComp->piSubsColl);
        ExitOnFailure(hr, "Failed to get subscriptions collection");
    }

    // return value
    *ppiSubsColl = pComp->piSubsColl;
    (*ppiSubsColl)->AddRef();

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCompColl);
    ReleaseObject(piCompObj);

    return hr;
}


// helper function definitions

static HRESULT GetAssemblyName(
    LPCWSTR pwzComponent,
    LPWSTR* ppwzAssemblyName
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView, hRecKey, hRec;

    LPWSTR pwzKey = NULL;

    LPWSTR pwzName = NULL;
    LPWSTR pwzVersion = NULL;
    LPWSTR pwzCulture = NULL;
    LPWSTR pwzPublicKeyToken = NULL;

    // create parameter record
    hRecKey = ::MsiCreateRecord(1);
    ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
    hr = WcaSetRecordString(hRecKey, 1, pwzComponent);
    ExitOnFailure(hr, "Failed to set record string");

    // open view
    hr = WcaOpenView(vcsMsiAssemblyNameQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on MsiAssemblyName table");
    hr = WcaExecuteView(hView, hRecKey);
    ExitOnFailure(hr, "Failed to execute view on MsiAssemblyName table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // read key
        hr = WcaGetRecordString(hRec, manqName, &pwzKey);
        ExitOnFailure(hr, "Failed to get name");

        // read value
        if (0 == lstrcmpiW(L"name", pwzKey))
            hr = WcaGetRecordString(hRec, manqValue, &pwzName);
        else if (0 == lstrcmpiW(L"version", pwzKey))
            hr = WcaGetRecordString(hRec, manqValue, &pwzVersion);
        else if (0 == lstrcmpiW(L"culture", pwzKey))
            hr = WcaGetRecordString(hRec, manqValue, &pwzCulture);
        else if (0 == lstrcmpiW(L"publicKeyToken", pwzKey))
            hr = WcaGetRecordString(hRec, manqValue, &pwzPublicKeyToken);
        else
        {
            WcaLog(LOGMSG_VERBOSE, "Unknown name in MsiAssemblyName table: %S, %S", pwzComponent, pwzKey);
            hr = S_OK;
        }

        ExitOnFailure(hr, "Failed to get value");
    }

    if (E_NOMOREITEMS != hr)
        ExitOnFailure(hr, "Failed to fetch record");

    // verify
    if (!(pwzName && *pwzName) || !(pwzVersion && *pwzVersion))
        ExitOnFailure(hr = E_FAIL, "Incomplete assembly name");

    // build name string
    hr = StrAllocFormatted(ppwzAssemblyName, L"%s, Version=%s, Culture=%s, PublicKeyToken=%s",
        pwzName, pwzVersion,
        pwzCulture && *pwzCulture ? pwzCulture : L"Neutral",
        pwzPublicKeyToken && *pwzPublicKeyToken ? pwzPublicKeyToken : L"null");
    ExitOnFailure(hr, "Failed to build assembly name string");

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzKey);
    ReleaseStr(pwzName);
    ReleaseStr(pwzVersion);
    ReleaseStr(pwzCulture);
    ReleaseStr(pwzPublicKeyToken);

    return hr;
}

static HRESULT KeyPairsRead(
    LPCWSTR pwzQuery,
    CPI_KEY_PAIR** ppKeyPairList
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView, hRec;

    CPI_KEY_PAIR* pItm = NULL;
    LPWSTR pwzData = NULL;

    // loop through all dependencies
    hr = WcaOpenExecuteView(pwzQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (CPI_KEY_PAIR*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_KEY_PAIR));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, kpqFirstKey, &pwzData);
        ExitOnFailure(hr, "Failed to get first key");
        StringCchCopyW(pItm->wzFirstKey, countof(pItm->wzFirstKey), pwzData);

        // get key
        hr = WcaGetRecordString(hRec, kpqSecondKey, &pwzData);
        ExitOnFailure(hr, "Failed to get second key");
        StringCchCopyW(pItm->wzSecondKey, countof(pItm->wzSecondKey), pwzData);

        // add entry
        if (*ppKeyPairList)
            pItm->pNext = *ppKeyPairList;
        *ppKeyPairList = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        KeyPairsFreeList(pItm);

    ReleaseStr(pwzData);

    return hr;
}

static HRESULT ModulesRead(
    CPI_MODULE_LIST* pModList
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView, hRec;

    CPI_MODULE* pItm = NULL;
    LPWSTR pwzData = NULL;

    // loop through all modules
    hr = WcaOpenExecuteView(vcsModuleQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ModuleSignature table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (CPI_MODULE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_MODULE));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, mqModule, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // add entry
        if (pModList->pLast)
        {
            pModList->pLast->pNext = pItm;
            pItm->pPrev = pModList->pLast;
        }
        else
            pModList->pFirst = pItm;
        pModList->pLast = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        ModuleFree(pItm);

    ReleaseStr(pwzData);

    return hr;
}

static HRESULT AssembliesRead(
    CPI_KEY_PAIR* pModCompList,
    CPI_APPLICATION_LIST* pAppList,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY_LIST* pAsmList
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;

    CPI_ASSEMBLY* pItm = NULL;
    CPI_KEY_PAIR* pModComp;
    LPWSTR pwzData = NULL;
    LPWSTR pwzComponent = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // loop through all assemblies
    hr = WcaOpenExecuteView(vcsAssemblyQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view on ComPlusAssembly table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, aqComponent, &pwzComponent);
        ExitOnFailure(hr, "Failed to get component");

        // check if the component is our processor architecture
        hr = CpiVerifyComponentArchitecure(pwzComponent, &fMatchingArchitecture);
        ExitOnFailure(hr, "Failed to get component architecture.");

        if (!fMatchingArchitecture)
        {
            continue; // not the same architecture, ignore
        }

        // create entry
        pItm = (CPI_ASSEMBLY*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_ASSEMBLY));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzComponent, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get key
        hr = WcaGetRecordString(hRec, aqAssembly, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get attributes
        hr = WcaGetRecordInteger(hRec, aqAttributes, &pItm->iAttributes);
        ExitOnFailure(hr, "Failed to get attributes");

        // get assembly name
        hr = WcaGetRecordFormattedString(hRec, aqAssemblyName, &pItm->pwzAssemblyName);
        ExitOnFailure(hr, "Failed to get assembly name");

        if (!*pItm->pwzAssemblyName && (pItm->iAttributes & aaPathFromGAC))
        {
            // get assembly name for component
            hr = GetAssemblyName(pwzComponent, &pItm->pwzAssemblyName);
            ExitOnFailure(hr, "Failed to get assembly name for component");
        }

        // get dll path
        hr = WcaGetRecordFormattedString(hRec, aqDllPath, &pItm->pwzDllPath);
        ExitOnFailure(hr, "Failed to get assembly dll path");

        // get module
        // TODO: if there is a very large number of components belonging to modules, this search might be slow
        hr = KeyPairFindByFirstKey(pModCompList, pwzData, &pModComp);

        if (S_OK == hr)
            StringCchCopyW(pItm->wzModule, countof(pItm->wzModule), pModComp->wzSecondKey);

        // get application
        hr = WcaGetRecordString(hRec, aqApplication, &pwzData);
        ExitOnFailure(hr, "Failed to get application");

        if (pwzData && *pwzData)
        {
            hr = CpiApplicationFindByKey(pAppList, pwzData, &pItm->pApplication);
            if (S_FALSE == hr)
                hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
            ExitOnFailure1(hr, "Failed to find application, key: %S", pwzData);
        }

        // get tlb path
        hr = WcaGetRecordFormattedString(hRec, aqTlbPath, &pItm->pwzTlbPath);
        ExitOnFailure(hr, "Failed to get assembly tlb path");

        // get proxy-stub dll path
        hr = WcaGetRecordFormattedString(hRec, aqPSDllPath, &pItm->pwzPSDllPath);
        ExitOnFailure(hr, "Failed to get assembly proxy-stub DLL path");

        // read components
        if (CpiTableExists(cptComPlusComponent))
        {
            hr = ComponentsRead(pItm->wzKey, pAppRoleList, pItm);
            ExitOnFailure(hr, "Failed to read components for assembly");
        }

        // set references & increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
        {
            pAsmList->iInstallCount++;
            if (pItm->iAttributes & aaRunInCommit)
                pAsmList->iCommitCount++;
        }
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction) || WcaIsReInstalling(pItm->isInstalled, pItm->isAction))
            pAsmList->iUninstallCount++;

        if (pItm->iRoleAssignmentsInstallCount)
        {
            pAsmList->iRoleInstallCount++;
            if (pItm->iAttributes & aaRunInCommit)
                pAsmList->iRoleCommitCount++;
        }
        if (pItm->iRoleAssignmentsUninstallCount)
            pAsmList->iRoleUninstallCount++;

        if (pItm->pApplication)
        {
            if (pItm->iRoleAssignmentsInstallCount || WcaIsInstalling(pItm->isInstalled, pItm->isAction))
                CpiApplicationAddReferenceInstall(pItm->pApplication);
            if (pItm->iRoleAssignmentsUninstallCount || WcaIsUninstalling(pItm->isInstalled, pItm->isAction) || WcaIsReInstalling(pItm->isInstalled, pItm->isAction))
                CpiApplicationAddReferenceUninstall(pItm->pApplication);
        }

        // add entry
        if (pAsmList->pLast)
        {
            pAsmList->pLast->pNext = pItm;
            pItm->pPrev = pAsmList->pLast;
        }
        else
            pAsmList->pFirst = pItm;
        pAsmList->pLast = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        AssemblyFree(pItm);

    ReleaseStr(pwzData);
    ReleaseStr(pwzComponent);

    return hr;
}

static HRESULT TopSortModuleList(
    CPI_KEY_PAIR* pDepList,
    CPI_MODULE_LIST* pList
    )
{
    HRESULT hr = S_OK;

    // top sort list
    for (CPI_MODULE* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // append module
        hr = SwapDependentModules(NULL, pDepList, pList, pItm, pItm);
        ExitOnFailure(hr, "Failed to swap dependent modules");
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT SwapDependentModules(
    CPI_DEPENDENCY_CHAIN* pdcPrev, // list containing the entire dependency chain
    CPI_KEY_PAIR* pDepList,        // module dependency list
    CPI_MODULE_LIST* pList,        // module list being sorted
    CPI_MODULE* pRoot,             // first module in the chain
    CPI_MODULE* pItm               // current module to test for dependencies
    )
{
    HRESULT hr = S_OK;

    CPI_MODULE* pDepItm;

    // find dependencies
    for (CPI_KEY_PAIR* pDep = pDepList; pDep; pDep = pDep->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pDep->wzFirstKey))
        {
            CPI_DEPENDENCY_CHAIN dcItm;
            dcItm.pwzKey = pItm->wzKey;
            dcItm.pPrev = pdcPrev;

            // check for circular dependencies
            for (CPI_DEPENDENCY_CHAIN* pdcItm = &dcItm; pdcItm; pdcItm = pdcItm->pPrev)
            {
                if (0 == lstrcmpW(pdcItm->pwzKey, pDep->wzSecondKey))
                {
                    // circular dependency found
                    ExitOnFailure1(hr = E_FAIL, "Circular module dependency found, key: %S", pDep->wzSecondKey);
                }
            }

            // make sure the item is not already in the list
            hr = ModuleFindByKey(pRoot->pPrev, pDep->wzSecondKey, TRUE, &pDepItm); // find in reverse order

            if (S_OK == hr)
                continue; // item found, move on

            // find item in the list
            hr = ModuleFindByKey(pRoot->pNext, pDep->wzSecondKey, FALSE, &pDepItm); // find in forward order

            if (S_FALSE == hr)
            {
                // not found
                ExitOnFailure1(hr = E_FAIL, "Module dependency not found, key: %S", pDep->wzSecondKey);
            }

            // if this item in turn has dependencies, they have to be swaped first
            hr = SwapDependentModules(&dcItm, pDepList, pList, pRoot, pDepItm);
            ExitOnFailure(hr, "Failed to swap dependent module");

            // remove item from its current position
            pDepItm->pPrev->pNext = pDepItm->pNext; // pDepItm can never be the first item, no need to check pPrev
            if (pDepItm->pNext)
                pDepItm->pNext->pPrev = pDepItm->pPrev;
            else
            {
                pList->pLast = pDepItm->pPrev;
                pList->pLast->pNext = NULL;
            }

            // insert before the current item
            if (pRoot->pPrev)
                pRoot->pPrev->pNext = pDepItm;
            else
                pList->pFirst = pDepItm;
            pDepItm->pPrev = pRoot->pPrev;
            pRoot->pPrev = pDepItm;
            pDepItm->pNext = pRoot;
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT ModuleFindByKey(
    CPI_MODULE* pItm,
    LPCWSTR pwzKey,
    BOOL fReverse,
    CPI_MODULE** ppItm
    )
{
    for (; pItm; pItm = fReverse ? pItm->pPrev : pItm->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pwzKey))
        {
            *ppItm = pItm;
            return S_OK;
        }
    }

    return S_FALSE;
}

static void SortAssemblyListByModule(
    CPI_MODULE_LIST* pModList,
    CPI_ASSEMBLY_LIST* pAsmList
    )
{
    CPI_ASSEMBLY* pMoved = NULL; // first moved item

    // loop modules in reverse order
    for (CPI_MODULE* pMod = pModList->pLast; pMod; pMod = pMod->pPrev)
    {
        // loop assemblies in forward order, starting with the first unmoved item
        CPI_ASSEMBLY* pAsm = pMoved ? pMoved->pNext : pAsmList->pFirst;
        while (pAsm)
        {
            CPI_ASSEMBLY* pNext = pAsm->pNext;

            // check if assembly belongs to the current module
            if (0 == lstrcmpW(pMod->wzKey, pAsm->wzModule))
            {
                // if the item is not already first in the list
                if (pAsm->pPrev)
                {
                    // remove item from it's current position
                    pAsm->pPrev->pNext = pAsm->pNext;
                    if (pAsm->pNext)
                        pAsm->pNext->pPrev = pAsm->pPrev;
                    else
                        pAsmList->pLast = pAsm->pPrev;

                    // insert item first in the list
                    pAsmList->pFirst->pPrev = pAsm;
                    pAsm->pNext = pAsmList->pFirst;
                    pAsm->pPrev = NULL;
                    pAsmList->pFirst = pAsm;
                }

                // if we haven't moved any items yet, this is the first moved item
                if (!pMoved)
                    pMoved = pAsm;
            }

            pAsm = pNext;
        }
    }
}

static HRESULT TopSortAssemblyList(
    CPI_KEY_PAIR* pDepList,
    CPI_ASSEMBLY_LIST* pList
    )
{
    HRESULT hr = S_OK;

    // top sort list
    for (CPI_ASSEMBLY* pItm = pList->pFirst; pItm; pItm = pItm->pNext)
    {
        // append module
        hr = SwapDependentAssemblies(NULL, pDepList, pList, pItm, pItm);
        ExitOnFailure(hr, "Failed to swap dependent assemblies");
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT SwapDependentAssemblies(
    CPI_DEPENDENCY_CHAIN* pdcPrev, // list containing the entire dependency chain
    CPI_KEY_PAIR* pDepList,        // assembly dependency list
    CPI_ASSEMBLY_LIST* pList,      // assembly list being sorted
    CPI_ASSEMBLY* pRoot,           // first assembly in the chain
    CPI_ASSEMBLY* pItm             // current assembly to test for dependencies
    )
{
    HRESULT hr = S_OK;

    CPI_ASSEMBLY* pDepItm;

    // find dependencies
    for (CPI_KEY_PAIR* pDep = pDepList; pDep; pDep = pDep->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pDep->wzFirstKey))
        {
            CPI_DEPENDENCY_CHAIN dcItm;
            dcItm.pwzKey = pItm->wzKey;
            dcItm.pPrev = pdcPrev;

            // check for circular dependencies
            for (CPI_DEPENDENCY_CHAIN* pdcItm = &dcItm; pdcItm; pdcItm = pdcItm->pPrev)
            {
                if (0 == lstrcmpW(pdcItm->pwzKey, pDep->wzSecondKey))
                {
                    // circular dependency found
                    ExitOnFailure1(hr = E_FAIL, "Circular assembly dependency found, key: %S", pDep->wzSecondKey);
                }
            }

            // make sure the item is not already in the list
            hr = AssemblyFindByKey(pRoot->pPrev, pDep->wzSecondKey, TRUE, &pDepItm); // find in reverse order

            if (S_OK == hr)
                continue; // item found, move on

            // find item in the list
            hr = AssemblyFindByKey(pRoot->pNext, pDep->wzSecondKey, FALSE, &pDepItm); // find in forward order

            if (S_FALSE == hr)
            {
                // not found
                ExitOnFailure1(hr = E_FAIL, "Assembly dependency not found, key: %S", pDep->wzSecondKey);
            }

            // if the root item belongs to a module, this item must also belong to the same module
            if (*pItm->wzModule)
            {
                if (0 != lstrcmpW(pDepItm->wzModule, pItm->wzModule))
                    ExitOnFailure2(hr = E_FAIL, "An assembly dependency can only exist between two assemblies not belonging to modules, or belonging to the same module. assembly: %S, required assembly: %S", pItm->wzKey, pDepItm->wzKey);
            }

            // if this item in turn has dependencies, they have to be swaped first
            hr = SwapDependentAssemblies(&dcItm, pDepList, pList, pRoot, pDepItm);
            ExitOnFailure(hr, "Failed to swap dependent assemblies");

            // remove item from its current position
            pDepItm->pPrev->pNext = pDepItm->pNext; // pDepItm can never be the first item, no need to check pPrev
            if (pDepItm->pNext)
                pDepItm->pNext->pPrev = pDepItm->pPrev;
            else
            {
                pList->pLast = pDepItm->pPrev;
                pList->pLast->pNext = NULL;
            }

            // insert before the current item
            if (pRoot->pPrev)
                pRoot->pPrev->pNext = pDepItm;
            else
                pList->pFirst = pDepItm;
            pDepItm->pPrev = pRoot->pPrev;
            pRoot->pPrev = pDepItm;
            pDepItm->pNext = pRoot;
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AssemblyFindByKey(
    CPI_ASSEMBLY* pItm,
    LPCWSTR pwzKey,
    BOOL fReverse,
    CPI_ASSEMBLY** ppItm
    )
{
    for (; pItm; pItm = fReverse ? pItm->pPrev : pItm->pNext)
    {
        if (0 == lstrcmpW(pItm->wzKey, pwzKey))
        {
            *ppItm = pItm;
            return S_OK;
        }
    }

    return S_FALSE;
}

static HRESULT ComponentsRead(
    LPCWSTR pwzAsmKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY* pAsm
    )
{
    HRESULT hr = S_OK;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    PMSIHANDLE hRecKey;
    CPI_COMPONENT* pItm = NULL;
    LPWSTR pwzData = NULL;

    // create parameter record
    hRecKey = ::MsiCreateRecord(1);
    ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
    hr = WcaSetRecordString(hRecKey, 1, pwzAsmKey);
    ExitOnFailure(hr, "Failed to set record string");

    // open view
    hr = WcaOpenView(vcsComponentQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on ComPlusComponent table");
    hr = WcaExecuteView(hView, hRecKey);
    ExitOnFailure(hr, "Failed to execute view on ComPlusComponent table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (CPI_COMPONENT*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_COMPONENT));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, cqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get clsid
        hr = WcaGetRecordFormattedString(hRec, cqCLSID, &pwzData);
        ExitOnFailure(hr, "Failed to get clsid");
        StringCchCopyW(pItm->wzCLSID, countof(pItm->wzCLSID), pwzData);

        // read properties
        if (CpiTableExists(cptComPlusComponentProperty))
        {
            hr = CpiPropertiesRead(vcsComponentPropertyQuery, pItm->wzKey, pdlComponentProperties, &pItm->pProperties, &pItm->iPropertyCount);
            ExitOnFailure(hr, "Failed to get component properties");
        }

        // read roles
        if (CpiTableExists(cptComPlusRoleForComponent))
        {
            hr = RoleAssignmentsRead(vcsRoleForComponentQuery, pItm->wzKey, pAppRoleList, &pItm->pRoles, &pItm->iRoleInstallCount, &pItm->iRoleUninstallCount);
            ExitOnFailure(hr, "Failed to get roles for component");
        }

        if (pItm->iRoleInstallCount)
            pAsm->iRoleAssignmentsInstallCount++;
        if (pItm->iRoleUninstallCount)
            pAsm->iRoleAssignmentsUninstallCount++;

        // read interfaces
        if (CpiTableExists(cptComPlusInterface))
        {
            hr = InterfacesRead(pItm->wzKey, pAppRoleList, pAsm, pItm);
            ExitOnFailure(hr, "Failed to get interfaces for component");
        }

        // add entry
        pAsm->iComponentCount++;
        if (pAsm->pComponents)
            pItm->pNext = pAsm->pComponents;
        pAsm->pComponents = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        ComponentsFreeList(pItm);

    ReleaseStr(pwzData);

    return hr;
}

static HRESULT InterfacesRead(
    LPCWSTR pwzCompKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY* pAsm,
    CPI_COMPONENT* pComp
    )
{
    HRESULT hr = S_OK;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    PMSIHANDLE hRecKey;
    CPI_INTERFACE* pItm = NULL;
    LPWSTR pwzData = NULL;

    // create parameter record
    hRecKey = ::MsiCreateRecord(1);
    ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
    hr = WcaSetRecordString(hRecKey, 1, pwzCompKey);
    ExitOnFailure(hr, "Failed to set record string");

    // open view
    hr = WcaOpenView(vcsInterfaceQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on ComPlusInterface table");
    hr = WcaExecuteView(hView, hRecKey);
    ExitOnFailure(hr, "Failed to execute view on ComPlusInterface table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (CPI_INTERFACE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_INTERFACE));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, iqInterface, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get iid
        hr = WcaGetRecordFormattedString(hRec, iqIID, &pwzData);
        ExitOnFailure(hr, "Failed to get iid");
        StringCchCopyW(pItm->wzIID, countof(pItm->wzIID), pwzData);

        // read properties
        if (CpiTableExists(cptComPlusInterfaceProperty))
        {
            hr = CpiPropertiesRead(vcsInterfacePropertyQuery, pItm->wzKey, pdlInterfaceProperties, &pItm->pProperties, &pItm->iPropertyCount);
            ExitOnFailure(hr, "Failed to get interface properties");
        }

        // read roles
        if (CpiTableExists(cptComPlusRoleForInterface))
        {
            hr = RoleAssignmentsRead(vcsRoleForInterfaceQuery, pItm->wzKey, pAppRoleList, &pItm->pRoles, &pItm->iRoleInstallCount, &pItm->iRoleUninstallCount);
            ExitOnFailure(hr, "Failed to get roles for interface");
        }

        if (pItm->iRoleInstallCount)
            pAsm->iRoleAssignmentsInstallCount++;
        if (pItm->iRoleUninstallCount)
            pAsm->iRoleAssignmentsUninstallCount++;

        // read methods
        if (CpiTableExists(cptComPlusMethod))
        {
            hr = MethodsRead(pItm->wzKey, pAppRoleList, pAsm, pItm);
            ExitOnFailure(hr, "Failed to get methods for interface");
        }

        // add entry
        pComp->iInterfaceCount++;
        if (pComp->pInterfaces)
            pItm->pNext = pComp->pInterfaces;
        pComp->pInterfaces = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        InterfacesFreeList(pItm);

    ReleaseStr(pwzData);

    return hr;
}

static HRESULT MethodsRead(
    LPCWSTR pwzIntfKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ASSEMBLY* pAsm,
    CPI_INTERFACE* pIntf
    )
{
    HRESULT hr = S_OK;
    PMSIHANDLE hView, hRec, hRecKey;
    CPI_METHOD* pItm = NULL;
    LPWSTR pwzData = NULL;

    // create parameter record
    hRecKey = ::MsiCreateRecord(1);
    ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
    hr = WcaSetRecordString(hRecKey, 1, pwzIntfKey);
    ExitOnFailure(hr, "Failed to set record string");

    // open view
    hr = WcaOpenView(vcsMethodQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on ComPlusMethod table");
    hr = WcaExecuteView(hView, hRecKey);
    ExitOnFailure(hr, "Failed to execute view on ComPlusMethod table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (CPI_METHOD*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_METHOD));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get key
        hr = WcaGetRecordString(hRec, iqInterface, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get index
        hr = WcaGetRecordFormattedString(hRec, mqIndex, &pwzData);
        ExitOnFailure(hr, "Failed to get index");
        StringCchCopyW(pItm->wzIndex, countof(pItm->wzIndex), pwzData);

        // get name
        hr = WcaGetRecordFormattedString(hRec, mqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // either an index or a name must be provided
        if (!*pItm->wzIndex && !*pItm->wzName)
            ExitOnFailure1(hr = E_FAIL, "A method must have either an index or a name associated, key: %S", pItm->wzKey);

        // read properties
        if (CpiTableExists(cptComPlusMethodProperty))
        {
            hr = CpiPropertiesRead(vcsMethodPropertyQuery, pItm->wzKey, pdlMethodProperties, &pItm->pProperties, &pItm->iPropertyCount);
            ExitOnFailure(hr, "Failed to get method properties");
        }

        // read roles
        if (CpiTableExists(cptComPlusRoleForMethod))
        {
            hr = RoleAssignmentsRead(vcsRoleForMethodQuery, pItm->wzKey, pAppRoleList, &pItm->pRoles, &pItm->iRoleInstallCount, &pItm->iRoleUninstallCount);
            ExitOnFailure(hr, "Failed to get roles for method");
        }

        if (pItm->iRoleInstallCount)
            pAsm->iRoleAssignmentsInstallCount++;
        if (pItm->iRoleUninstallCount)
            pAsm->iRoleAssignmentsUninstallCount++;

        // add entry
        pIntf->iMethodCount++;
        if (pIntf->pMethods)
            pItm->pNext = pIntf->pMethods;
        pIntf->pMethods = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        MethodsFreeList(pItm);

    ReleaseStr(pwzData);

    return hr;
}

static HRESULT RoleAssignmentsRead(
    LPCWSTR pwzQuery,
    LPCWSTR pwzKey,
    CPI_APPLICATION_ROLE_LIST* pAppRoleList,
    CPI_ROLE_ASSIGNMENT** ppRoleList,
    int* piInstallCount,
    int* piUninstallCount
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec, hRecKey;

    CPI_ROLE_ASSIGNMENT* pItm = NULL;
    LPWSTR pwzData = NULL;
    BOOL fMatchingArchitecture = FALSE;

    // create parameter record
    hRecKey = ::MsiCreateRecord(1);
    ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
    hr = WcaSetRecordString(hRecKey, 1, pwzKey);
    ExitOnFailure(hr, "Failed to set record string");

    // open view
    hr = WcaOpenView(pwzQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on role assignment table");
    hr = WcaExecuteView(hView, hRecKey);
    ExitOnFailure(hr, "Failed to execute view on role assignment table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // get component
        hr = WcaGetRecordString(hRec, raqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get assembly component");

        // check if the component is our processor architecture
        hr = CpiVerifyComponentArchitecure(pwzData, &fMatchingArchitecture);
        ExitOnFailure(hr, "Failed to get component architecture.");

        if (!fMatchingArchitecture)
        {
            continue; // not the same architecture, ignore
        }

        // create entry
        pItm = (CPI_ROLE_ASSIGNMENT*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_ROLE_ASSIGNMENT));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &pItm->isInstalled, &pItm->isAction);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to get component state");

        // get key
        hr = WcaGetRecordString(hRec, raqKey, &pwzData);
        ExitOnFailure(hr, "Failed to get key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // get application role
        hr = WcaGetRecordString(hRec, raqApplicationRole, &pwzData);
        ExitOnFailure(hr, "Failed to get application role");

        hr = CpiApplicationRoleFindByKey(pAppRoleList, pwzData, &pItm->pApplicationRole);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        ExitOnFailure1(hr, "Failed to find application, key: %S", pwzData);

        // set references & increment counters
        if (WcaIsInstalling(pItm->isInstalled, pItm->isAction))
        {
            CpiApplicationRoleAddReferenceInstall(pItm->pApplicationRole);
            ++*piInstallCount;
        }
        if (WcaIsUninstalling(pItm->isInstalled, pItm->isAction))
        {
            CpiApplicationRoleAddReferenceUninstall(pItm->pApplicationRole);
            ++*piUninstallCount;
        }

        // add entry
        if (*ppRoleList)
            pItm->pNext = *ppRoleList;
        *ppRoleList = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        RoleAssignmentsFreeList(pItm);

    ReleaseStr(pwzData);

    return hr;
}

static HRESULT AddAssemblyToActionData(
    CPI_ASSEMBLY* pItm,
    BOOL fInstall,
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

    // add assembly information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly key to custom action data");
    hr = WcaWriteStringToCaData(pItm->pwzAssemblyName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly name to custom action data");
    hr = WcaWriteStringToCaData(pItm->pwzDllPath, ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly dll path to custom action data");
    hr = WcaWriteStringToCaData(atCreate == iActionType ? pItm->pwzTlbPath : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly tlb path to custom action data");
    hr = WcaWriteStringToCaData(atCreate == iActionType ? pItm->pwzPSDllPath : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly proxy-stub dll path to custom action data");
    hr = WcaWriteIntegerToCaData(pItm->iAttributes, ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly attributes to custom action data");

    // add application information to custom action data
    hr = WcaWriteStringToCaData(pItm->pApplication ? pItm->pApplication->wzID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add application id to custom action data");

    // add partition information to custom action data
    LPCWSTR pwzPartID = pItm->pApplication && pItm->pApplication->pPartition ? pItm->pApplication->pPartition->wzID : L"";
    hr = WcaWriteStringToCaData(pwzPartID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    // add components to custom action data
    //
    // components are needed acording to the following table:
    //
    //             Native    .NET
    // --------------------------------------------
    //  NoOp     |  No     |  No
    //  Create   |  Yes    |  Yes
    //  Remove   |  Yes    |  No
    //
    int iCompCount = (atCreate == iActionType || (atRemove == iActionType && 0 == (pItm->iAttributes & aaDotNetAssembly))) ? pItm->iComponentCount : 0;
    hr = WcaWriteIntegerToCaData(iCompCount, ppwzActionData);
    ExitOnFailure1(hr, "Failed to add component count to custom action data, key: %S", pItm->wzKey);

    if (iCompCount)
    {
        for (CPI_COMPONENT* pComp = pItm->pComponents; pComp; pComp = pComp->pNext)
        {
            hr = AddComponentToActionData(pComp, fInstall, atCreate == iActionType, FALSE, ppwzActionData);
            ExitOnFailure1(hr, "Failed to add component to custom action data, component: %S", pComp->wzKey);
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AddRoleAssignmentsToActionData(
    CPI_ASSEMBLY* pItm,
    BOOL fInstall,
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

    // add assembly information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzKey, ppwzActionData);
    ExitOnFailure(hr, "Failed to add assembly key to custom action data");
    hr = WcaWriteIntegerToCaData(fInstall ? pItm->iRoleAssignmentsInstallCount : pItm->iRoleAssignmentsUninstallCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add role assignments count to custom action data");

    // add application information to custom action data
    hr = WcaWriteStringToCaData(pItm->pApplication ? pItm->pApplication->wzID : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add application id to custom action data");

    // add partition information to custom action data
    LPCWSTR pwzPartID = pItm->pApplication && pItm->pApplication->pPartition ? pItm->pApplication->pPartition->wzID : L"";
    hr = WcaWriteStringToCaData(pwzPartID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add partition id to custom action data");

    // add components to custom action data
    hr = WcaWriteIntegerToCaData(pItm->iComponentCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add component count to custom action data");

    for (CPI_COMPONENT* pComp = pItm->pComponents; pComp; pComp = pComp->pNext)
    {
        hr = AddComponentToActionData(pComp, fInstall, FALSE, TRUE, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add component to custom action data, component: %S", pComp->wzKey);
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AddComponentToActionData(
    CPI_COMPONENT* pItm,
    BOOL fInstall,
    BOOL fProps,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add component information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzCLSID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add component CLSID to custom action data");

    // add properties to custom action data
    hr = CpiAddPropertiesToActionData(fProps ? pItm->iPropertyCount : 0, pItm->pProperties, ppwzActionData);
    ExitOnFailure(hr, "Failed to add properties to custom action data");

    // add roles to custom action data
    hr = AddRolesToActionData(pItm->iRoleInstallCount, pItm->iRoleUninstallCount, pItm->pRoles, fInstall, fRoles, ppwzActionData);
    ExitOnFailure(hr, "Failed to add roles to custom action data");

    // add interfaces to custom action data
    int iIntfCount = (fProps || fRoles) ? pItm->iInterfaceCount : 0;
    hr = WcaWriteIntegerToCaData(iIntfCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add interface count to custom action data");

    if (iIntfCount)
    {
        for (CPI_INTERFACE* pIntf = pItm->pInterfaces; pIntf; pIntf = pIntf->pNext)
        {
            hr = AddInterfaceToActionData(pIntf, fInstall, fProps, fRoles, ppwzActionData);
            ExitOnFailure1(hr, "Failed to add interface custom action data, interface: %S", pIntf->wzKey);
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AddInterfaceToActionData(
    CPI_INTERFACE* pItm,
    BOOL fInstall,
    BOOL fProps,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add interface information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzIID, ppwzActionData);
    ExitOnFailure(hr, "Failed to add interface IID to custom action data");

    // add properties to custom action data
    hr = CpiAddPropertiesToActionData(fProps ? pItm->iPropertyCount : 0, pItm->pProperties, ppwzActionData);
    ExitOnFailure(hr, "Failed to add properties to custom action data");

    // add roles to custom action data
    hr = AddRolesToActionData(pItm->iRoleInstallCount, pItm->iRoleUninstallCount, pItm->pRoles, fInstall, fRoles, ppwzActionData);
    ExitOnFailure(hr, "Failed to add roles to custom action data");

    // add methods to custom action data
    hr = WcaWriteIntegerToCaData(pItm->iMethodCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add method count to custom action data");

    for (CPI_METHOD* pMeth = pItm->pMethods; pMeth; pMeth = pMeth->pNext)
    {
        hr = AddMethodToActionData(pMeth, fInstall, fProps, fRoles, ppwzActionData);
        ExitOnFailure1(hr, "Failed to add method custom action data, method: %S", pMeth->wzKey);
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AddMethodToActionData(
    CPI_METHOD* pItm,
    BOOL fInstall,
    BOOL fProps,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    // add interface information to custom action data
    hr = WcaWriteStringToCaData(pItm->wzIndex, ppwzActionData);
    ExitOnFailure(hr, "Failed to add method index to custom action data");

    hr = WcaWriteStringToCaData(pItm->wzName, ppwzActionData);
    ExitOnFailure(hr, "Failed to add method name to custom action data");

    // add properties to custom action data
    hr = CpiAddPropertiesToActionData(fProps ? pItm->iPropertyCount : 0, pItm->pProperties, ppwzActionData);
    ExitOnFailure(hr, "Failed to add properties to custom action data");

    // add roles to custom action data
    hr = AddRolesToActionData(pItm->iRoleInstallCount, pItm->iRoleUninstallCount, pItm->pRoles, fInstall, fRoles, ppwzActionData);
    ExitOnFailure(hr, "Failed to add roles to custom action data");

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT AddRolesToActionData(
    int iRoleInstallCount,
    int iRoleUninstallCount,
    CPI_ROLE_ASSIGNMENT* pRoleList,
    BOOL fInstall,
    BOOL fRoles,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    int iRoleCount = fRoles ? (fInstall ? iRoleInstallCount : iRoleUninstallCount) : 0;
    hr = WcaWriteIntegerToCaData(iRoleCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add role count to custom action data");

    if (iRoleCount)
    {
        for (CPI_ROLE_ASSIGNMENT* pRole = pRoleList; pRole; pRole = pRole->pNext)
        {
            // make sure the install state matches the create flag
            if (fInstall ? !WcaIsInstalling(pRole->isInstalled, pRole->isAction) : !WcaIsUninstalling(pRole->isInstalled, pRole->isAction))
                continue;

            hr = WcaWriteStringToCaData(pRole->pApplicationRole->wzKey, ppwzActionData);
            ExitOnFailure1(hr, "Failed to add key to custom action data, role: %S", pRole->wzKey);

            hr = WcaWriteStringToCaData(pRole->pApplicationRole->wzName, ppwzActionData);
            ExitOnFailure1(hr, "Failed to add role name to custom action data, role: %S", pRole->wzKey);
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT KeyPairFindByFirstKey(
    CPI_KEY_PAIR* pList,
    LPCWSTR pwzKey,
    CPI_KEY_PAIR** ppItm
    )
{
    for (; pList; pList = pList->pNext)
    {
        if (0 == lstrcmpW(pList->wzFirstKey, pwzKey))
        {
            *ppItm = pList;
            return S_OK;
        }
    }

    return S_FALSE;
}

static void AssemblyFree(
    CPI_ASSEMBLY* pItm
    )
{
    ReleaseStr(pItm->pwzAssemblyName);
    ReleaseStr(pItm->pwzDllPath);
    ReleaseStr(pItm->pwzTlbPath);
    ReleaseStr(pItm->pwzPSDllPath);

    if (pItm->pComponents)
        ComponentsFreeList(pItm->pComponents);

    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static void KeyPairsFreeList(
    CPI_KEY_PAIR* pList
    )
{
    while (pList)
    {
        CPI_KEY_PAIR* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

void ModuleListFree(
    CPI_MODULE_LIST* pList
    )
{
    CPI_MODULE* pItm = pList->pFirst;

    while (pItm)
    {
        CPI_MODULE* pDelete = pItm;
        pItm = pItm->pNext;
        ModuleFree(pDelete);
    }
}

static void ModuleFree(
    CPI_MODULE* pItm
    )
{
    ::HeapFree(::GetProcessHeap(), 0, pItm);
}

static void ComponentsFreeList(
    CPI_COMPONENT* pList
    )
{
    while (pList)
    {
        if (pList->pProperties)
            CpiPropertiesFreeList(pList->pProperties);

        if (pList->pRoles)
            RoleAssignmentsFreeList(pList->pRoles);

        if (pList->pInterfaces)
            InterfacesFreeList(pList->pInterfaces);

        ReleaseObject(pList->piSubsColl);

        CPI_COMPONENT* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

static void InterfacesFreeList(
    CPI_INTERFACE* pList
    )
{
    while (pList)
    {
        if (pList->pProperties)
            CpiPropertiesFreeList(pList->pProperties);

        if (pList->pRoles)
            RoleAssignmentsFreeList(pList->pRoles);

        if (pList->pMethods)
            MethodsFreeList(pList->pMethods);

        CPI_INTERFACE* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

static void MethodsFreeList(
    CPI_METHOD* pList
    )
{
    while (pList)
    {
        if (pList->pProperties)
            CpiPropertiesFreeList(pList->pProperties);

        if (pList->pRoles)
            RoleAssignmentsFreeList(pList->pRoles);

        CPI_METHOD* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

static void RoleAssignmentsFreeList(
    CPI_ROLE_ASSIGNMENT* pList
    )
{
    while (pList)
    {
        CPI_ROLE_ASSIGNMENT* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}
