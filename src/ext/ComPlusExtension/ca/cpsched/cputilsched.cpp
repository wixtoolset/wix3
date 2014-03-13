//-------------------------------------------------------------------------------------------------
// <copyright file="cputilsched.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ related utility functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// sql queries

LPCWSTR vcsActionTextQuery =
    L"SELECT `Description`, `Template` FROM `ActionText` WHERE `Action` = ?";
enum eActionTextQuery { atqDescription = 1, atqTemplate };

LPCWSTR vcsComponentAttributesQuery =
    L"SELECT `Attributes` FROM `Component` WHERE `Component` = ?";
enum eComponentAttributesQuery { caqAttributes = 1 };

LPCWSTR vcsUserQuery = L"SELECT `Domain`, `Name` FROM `User` WHERE `User` = ?";
enum eUserQuery { uqDomain = 1, uqName };

enum ePropertyQuery { pqName = 1, pqValue };


// prototypes for private helper functions

static HRESULT FindPropertyDefinition(
    CPI_PROPERTY_DEFINITION* pPropDefList,
    LPCWSTR pwzName,
    CPI_PROPERTY_DEFINITION** ppPropDef
    );
static HRESULT GetUserAccountName(
    LPCWSTR pwzKey,
    LPWSTR* ppwzAccount
    );


// variables

static ICOMAdminCatalog* gpiCatalog;
static ICatalogCollection* gpiPartColl;
static ICatalogCollection* gpiAppColl;

static int giTables;


// function definitions

void CpiInitialize()
{
    // collections
    gpiCatalog = NULL;
    gpiPartColl = NULL;
    gpiAppColl = NULL;

    // tables
    giTables = 0;

    if (S_OK == WcaTableExists(L"ComPlusPartition"))               giTables |= cptComPlusPartition;
    if (S_OK == WcaTableExists(L"ComPlusPartitionProperty"))       giTables |= cptComPlusPartitionProperty;
    if (S_OK == WcaTableExists(L"ComPlusPartitionRole"))           giTables |= cptComPlusPartitionRole;
    if (S_OK == WcaTableExists(L"ComPlusUserInPartitionRole"))     giTables |= cptComPlusUserInPartitionRole;
    if (S_OK == WcaTableExists(L"ComPlusGroupInPartitionRole"))    giTables |= cptComPlusGroupInPartitionRole;
    if (S_OK == WcaTableExists(L"ComPlusPartitionUser"))           giTables |= cptComPlusPartitionUser;
    if (S_OK == WcaTableExists(L"ComPlusApplication"))             giTables |= cptComPlusApplication;
    if (S_OK == WcaTableExists(L"ComPlusApplicationProperty"))     giTables |= cptComPlusApplicationProperty;
    if (S_OK == WcaTableExists(L"ComPlusApplicationRole"))         giTables |= cptComPlusApplicationRole;
    if (S_OK == WcaTableExists(L"ComPlusApplicationRoleProperty")) giTables |= cptComPlusApplicationRoleProperty;
    if (S_OK == WcaTableExists(L"ComPlusUserInApplicationRole"))   giTables |= cptComPlusUserInApplicationRole;
    if (S_OK == WcaTableExists(L"ComPlusGroupInApplicationRole"))  giTables |= cptComPlusGroupInApplicationRole;
    if (S_OK == WcaTableExists(L"ComPlusAssembly"))                giTables |= cptComPlusAssembly;
    if (S_OK == WcaTableExists(L"ComPlusAssemblyDependency"))      giTables |= cptComPlusAssemblyDependency;
    if (S_OK == WcaTableExists(L"ComPlusComponent"))               giTables |= cptComPlusComponent;
    if (S_OK == WcaTableExists(L"ComPlusComponentProperty"))       giTables |= cptComPlusComponentProperty;
    if (S_OK == WcaTableExists(L"ComPlusRoleForComponent"))        giTables |= cptComPlusRoleForComponent;
    if (S_OK == WcaTableExists(L"ComPlusInterface"))               giTables |= cptComPlusInterface;
    if (S_OK == WcaTableExists(L"ComPlusInterfaceProperty"))       giTables |= cptComPlusInterfaceProperty;
    if (S_OK == WcaTableExists(L"ComPlusRoleForInterface"))        giTables |= cptComPlusRoleForInterface;
    if (S_OK == WcaTableExists(L"ComPlusMethod"))                  giTables |= cptComPlusMethod;
    if (S_OK == WcaTableExists(L"ComPlusMethodProperty"))          giTables |= cptComPlusMethodProperty;
    if (S_OK == WcaTableExists(L"ComPlusRoleForMethod"))           giTables |= cptComPlusRoleForMethod;
    if (S_OK == WcaTableExists(L"ComPlusSubscription"))            giTables |= cptComPlusSubscription;
    if (S_OK == WcaTableExists(L"ComPlusSubscriptionProperty"))    giTables |= cptComPlusSubscriptionProperty;
}

void CpiFinalize()
{
    // collections
    ReleaseObject(gpiCatalog);
    ReleaseObject(gpiPartColl);
    ReleaseObject(gpiAppColl);
}

BOOL CpiTableExists(
    int iTable
    )
{
    return (giTables & iTable) == iTable;
}

HRESULT CpiGetAdminCatalog(
    ICOMAdminCatalog** ppiCatalog
    )
{
    HRESULT hr = S_OK;

    if (!gpiCatalog)
    {
        // get collection
        hr = ::CoCreateInstance(CLSID_COMAdminCatalog, NULL, CLSCTX_ALL, IID_ICOMAdminCatalog, (void**)&gpiCatalog); 
        ExitOnFailure(hr, "Failed to create COM+ admin catalog object");
    }

    // return value
    gpiCatalog->AddRef();
    *ppiCatalog = gpiCatalog;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetCatalogCollection(
    LPCWSTR pwzName,
    ICatalogCollection** ppiColl
    )
{
    HRESULT hr = S_OK;

    ICOMAdminCatalog* piCatalog = NULL;
    IDispatch* piDisp = NULL;
    BSTR bstrName = NULL;

    // copy name string
    bstrName = ::SysAllocString(pwzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for collection name");

    // get catalog
    hr = CpiGetAdminCatalog(&piCatalog);
    ExitOnFailure(hr, "Failed to get COM+ admin catalog");

    // get collecton from catalog
    hr = piCatalog->GetCollection(bstrName, &piDisp);
    ExitOnFailure(hr, "Failed to get collection");

    hr = piDisp->QueryInterface(IID_ICatalogCollection, (void**)ppiColl);
    ExitOnFailure(hr, "Failed to get IID_ICatalogCollection interface");

    // populate collection
    hr = (*ppiColl)->Populate();
    ExitOnFailure(hr, "Failed to populate collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCatalog);
    ReleaseObject(piDisp);
    ReleaseBSTR(bstrName);

    return hr;
}

HRESULT CpiGetCatalogCollection(
    ICatalogCollection* piColl,
    ICatalogObject* piObj,
    LPCWSTR pwzName,
    ICatalogCollection** ppiColl
    )
{
    HRESULT hr = S_OK;

    ICOMAdminCatalog* piCatalog = NULL;
    IDispatch* piDisp = NULL;
    BSTR bstrName = NULL;

    VARIANT vtKey;
    ::VariantInit(&vtKey);

    // copy name string
    bstrName = ::SysAllocString(pwzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for collection name");

    // get catalog
    hr = CpiGetAdminCatalog(&piCatalog);
    ExitOnFailure(hr, "Failed to get COM+ admin catalog");

    // get key
    hr = piObj->get_Key(&vtKey);
    ExitOnFailure(hr, "Failed to get object key");

    // get collecton from catalog
    hr = piColl->GetCollection(bstrName, vtKey, &piDisp);
    ExitOnFailure(hr, "Failed to get collection");

    hr = piDisp->QueryInterface(IID_ICatalogCollection, (void**)ppiColl);
    ExitOnFailure(hr, "Failed to get IID_ICatalogCollection interface");

    // populate collection
    hr = (*ppiColl)->Populate();
    ExitOnFailure(hr, "Failed to populate collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCatalog);
    ReleaseObject(piDisp);
    ReleaseBSTR(bstrName);
    ::VariantClear(&vtKey);

    return hr;
}

HRESULT CpiGetKeyForObject(
    ICatalogObject* piObj,
    LPWSTR pwzKey,
    SIZE_T cchKey
    )
{
    HRESULT hr = S_OK;

    VARIANT vtKey;
    ::VariantInit(&vtKey);

    // get key
    hr = piObj->get_Key(&vtKey);
    ExitOnFailure(hr, "Failed to get key");

    // change variant type
    hr = ::VariantChangeType(&vtKey, &vtKey, 0, VT_BSTR);
    ExitOnFailure(hr, "Failed to change variant type");

    // copy key
    hr = StringCchCopyW(pwzKey, cchKey, vtKey.bstrVal);
    ExitOnFailure(hr, "Failed to copy key");

    hr = S_OK;

LExit:
    // clean up
    ::VariantClear(&vtKey);

    return hr;
}

HRESULT CpiFindCollectionObject(
    ICatalogCollection* piColl,
    LPCWSTR pwzID,
    LPCWSTR pwzName,
    ICatalogObject** ppiObj
    )
{
    HRESULT hr = S_OK;

    IDispatch* piDisp = NULL;
    ICatalogObject* piObj = NULL;

    VARIANT vtVal;
    ::VariantInit(&vtVal);

    long lCnt;
    hr = piColl->get_Count(&lCnt);
    ExitOnFailure(hr, "Failed to get to number of items in collection");

    for (long i = 0; i < lCnt; i++)
    {
        // get ICatalogObject interface
        hr = piColl->get_Item(i, &piDisp);
        ExitOnFailure(hr, "Failed to get object from collection");

        hr = piDisp->QueryInterface(IID_ICatalogObject, (void**)&piObj);
        ExitOnFailure(hr, "Failed to get IID_ICatalogObject interface");

        // compare id
        if (pwzID && *pwzID)
        {
            hr = piObj->get_Key(&vtVal);
            ExitOnFailure(hr, "Failed to get key");

            hr = ::VariantChangeType(&vtVal, &vtVal, 0, VT_BSTR);
            ExitOnFailure(hr, "Failed to change variant type");

            if (0 == lstrcmpiW(vtVal.bstrVal, pwzID))
            {
                if (ppiObj)
                {
                    *ppiObj = piObj;
                    piObj = NULL;
                }
                ExitFunction1(hr = S_OK);
            }

            ::VariantClear(&vtVal);
        }

        // compare name
        if (pwzName && *pwzName)
        {
            hr = piObj->get_Name(&vtVal);
            ExitOnFailure(hr, "Failed to get name");

            hr = ::VariantChangeType(&vtVal, &vtVal, 0, VT_BSTR);
            ExitOnFailure(hr, "Failed to change variant type");

            if (0 == lstrcmpW(vtVal.bstrVal, pwzName))
            {
                if (ppiObj)
                {
                    *ppiObj = piObj;
                    piObj = NULL;
                }
                ExitFunction1(hr = S_OK);
            }

            ::VariantClear(&vtVal);
        }

        // release interface pointers
        ReleaseNullObject(piDisp);
        ReleaseNullObject(piObj);
    }

    hr = S_FALSE;

LExit:
    // clean up
    ReleaseObject(piDisp);
    ReleaseObject(piObj);

    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiGetPartitionsCollection(
    ICatalogCollection** ppiPartColl
    )
{
    HRESULT hr = S_OK;

    if (!gpiPartColl)
    {
        // get collection
        hr = CpiGetCatalogCollection(L"Partitions", &gpiPartColl);
        ExitOnFailure(hr, "Failed to get partitions collection");
    }

    // return value
    gpiPartColl->AddRef();
    *ppiPartColl = gpiPartColl;

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetApplicationsCollection(
    ICatalogCollection** ppiAppColl
    )
{
    HRESULT hr = S_OK;

    ICOMAdminCatalog* piCatalog = NULL;
    ICOMAdminCatalog2* piCatalog2 = NULL;
    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;
    BSTR bstrGlobPartID = NULL;

    if (!gpiAppColl)
    {
        // get catalog
        hr = CpiGetAdminCatalog(&piCatalog);
        ExitOnFailure(hr, "Failed to get COM+ admin catalog");

        // get ICOMAdminCatalog2 interface
        hr = piCatalog->QueryInterface(IID_ICOMAdminCatalog2, (void**)&piCatalog2);

        // COM+ 1.5 or later
        if (E_NOINTERFACE != hr)
        {
            ExitOnFailure(hr, "Failed to get IID_ICOMAdminCatalog2 interface");

            // get global partition id
            hr = piCatalog2->get_GlobalPartitionID(&bstrGlobPartID);
            ExitOnFailure(hr, "Failed to get global partition id");

            // get partitions collection
            hr = CpiGetPartitionsCollection(&piPartColl);
            ExitOnFailure(hr, "Failed to get partitions collection");

            // find object
            hr = CpiFindCollectionObject(piPartColl, bstrGlobPartID, NULL, &piPartObj);
            ExitOnFailure(hr, "Failed to find collection object");

            if (S_FALSE == hr)
                ExitFunction(); // partition not found, exit with hr = S_FALSE

            // get applications collection
            hr = CpiGetCatalogCollection(piPartColl, piPartObj, L"Applications", &gpiAppColl);
            ExitOnFailure(hr, "Failed to get applications collection");
        }

        // COM+ pre 1.5
        else
        {
            // get applications collection
            hr = CpiGetCatalogCollection(L"Applications", &gpiAppColl);
            ExitOnFailure(hr, "Failed to get applications collection");
        }
    }

    // return value
    gpiAppColl->AddRef();
    *ppiAppColl = gpiAppColl;

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCatalog);
    ReleaseObject(piCatalog2);
    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);
    ReleaseBSTR(bstrGlobPartID);

    return hr;
}

HRESULT CpiAddActionTextToActionData(
    LPCWSTR pwzAction,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView, hRecKey, hRec;

    LPWSTR pwzDescription = NULL;
    LPWSTR pwzTemplate = NULL;

    if (S_OK == WcaTableExists(L"ActionText"))
    {
        // create parameter record
        hRecKey = ::MsiCreateRecord(1);
        ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
        hr = WcaSetRecordString(hRecKey, 1, pwzAction);
        ExitOnFailure(hr, "Failed to set record string");

        // open view
        hr = WcaOpenView(vcsActionTextQuery, &hView);
        ExitOnFailure(hr, "Failed to open view on ActionText table");
        hr = WcaExecuteView(hView, hRecKey);
        ExitOnFailure(hr, "Failed to execute view on ActionText table");

        // fetch record
        hr = WcaFetchSingleRecord(hView, &hRec);
        if (S_FALSE != hr)
        {
            ExitOnFailure(hr, "Failed to fetch action text record");

            // get description
            hr = WcaGetRecordString(hRec, atqDescription, &pwzDescription);
            ExitOnFailure(hr, "Failed to get description");

            // get template
            hr = WcaGetRecordString(hRec, atqTemplate, &pwzTemplate);
            ExitOnFailure(hr, "Failed to get template");
        }
    }

    // add action name to action data
    hr = WcaWriteStringToCaData(pwzAction, ppwzActionData);
    ExitOnFailure(hr, "Failed to add action name to custom action data");

    // add description to action data
    hr = WcaWriteStringToCaData(pwzDescription ? pwzDescription : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add description to custom action data");

    // add template to action data
    hr = WcaWriteStringToCaData(pwzTemplate ? pwzTemplate : L"", ppwzActionData);
    ExitOnFailure(hr, "Failed to add template to custom action data");

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzDescription);
    ReleaseStr(pwzTemplate);

    return hr;
}

HRESULT CpiVerifyComponentArchitecure(
    LPCWSTR pwzComponent,
    BOOL* pfMatchingArchitecture
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView, hRecKey, hRec;

    int iAttributes = 0;

    if (S_OK == WcaTableExists(L"Component"))
    {
        // create parameter record
        hRecKey = ::MsiCreateRecord(1);
        ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
        hr = WcaSetRecordString(hRecKey, 1, pwzComponent);
        ExitOnFailure(hr, "Failed to set record string");

        // open view
        hr = WcaOpenView(vcsComponentAttributesQuery, &hView);
        ExitOnFailure(hr, "Failed to open view on ActionText table");
        hr = WcaExecuteView(hView, hRecKey);
        ExitOnFailure(hr, "Failed to execute view on ActionText table");

        // fetch record
        hr = WcaFetchSingleRecord(hView, &hRec);
        if (S_FALSE != hr)
        {
            ExitOnFailure(hr, "Failed to fetch component record");

            hr = WcaGetRecordInteger(hRec, caqAttributes, &iAttributes);
            ExitOnFailure(hr, "Failed to get component attributes");
        }
    }

    // return values
#ifdef _WIN64
    *pfMatchingArchitecture = 256 == (iAttributes & 256);
#else
    *pfMatchingArchitecture = 256 != (iAttributes & 256);
#endif

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiPropertiesRead(
    LPCWSTR pwzQuery,
    LPCWSTR pwzKey,
    CPI_PROPERTY_DEFINITION* pPropDefList,
    CPI_PROPERTY** ppPropList,
    int* piCount
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView, hRecKey, hRec;

    CPI_PROPERTY* pItm = NULL;
    LPWSTR pwzData = NULL;

    int iVersionNT = 0;

    CPI_PROPERTY_DEFINITION* pPropDef;

    *piCount = 0;

    // get NT version
    hr = WcaGetIntProperty(L"VersionNT", &iVersionNT);
    ExitOnFailure(hr, "Failed to set record string");

    // create parameter record
    hRecKey = ::MsiCreateRecord(1);
    ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
    hr = WcaSetRecordString(hRecKey, 1, pwzKey);
    ExitOnFailure(hr, "Failed to set record string");

    // open view
    hr = WcaOpenView(pwzQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on property table");
    hr = WcaExecuteView(hView, hRecKey);
    ExitOnFailure(hr, "Failed to execute view on property table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // create entry
        pItm = (CPI_PROPERTY*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_PROPERTY));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // get name
        hr = WcaGetRecordString(hRec, pqName, &pwzData);
        ExitOnFailure(hr, "Failed to get name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // get value
        hr = WcaGetRecordFormattedString(hRec, pqValue, &pItm->pwzValue);
        ExitOnFailure(hr, "Failed to get value");

        // find property definition
        hr = FindPropertyDefinition(pPropDefList, pItm->wzName, &pPropDef);
        ExitOnFailure(hr, "Failed to find property definition");

        if (S_FALSE == hr)
            ExitOnFailure2(hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "Unknown property, key: %S, property: %S", pwzKey, pItm->wzName);

        // check version, ignore if catalog version is too low
        if (iVersionNT < pPropDef->iMinVersionNT)
        {
            WcaLog(LOGMSG_VERBOSE, "Skipping property since NT version is too low, key: %S, property: %S", pwzKey, pItm->wzName);
            CpiPropertiesFreeList(pItm);
            pItm = NULL;
            continue;
        }

        // if the property is a user, replace the User table key with a user account name
        if (cpptUser == pPropDef->iType)
        {
            hr = GetUserAccountName(pItm->pwzValue, &pItm->pwzValue);
            ExitOnFailure(hr, "Failed to get user account name");
        }

        // add entry
        ++*piCount;
        if (*ppPropList)
            pItm->pNext = *ppPropList;
        *ppPropList = pItm;
        pItm = NULL;
    }

    if (E_NOMOREITEMS == hr)
        hr = S_OK;

LExit:
    // clean up
    if (pItm)
        CpiPropertiesFreeList(pItm);

    ReleaseStr(pwzData);

    return hr;
}

void CpiPropertiesFreeList(
    CPI_PROPERTY* pList
    )
{
    while (pList)
    {
        ReleaseStr(pList->pwzValue);

        CPI_PROPERTY* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

HRESULT CpiAddPropertiesToActionData(
    int iPropCount,
    CPI_PROPERTY* pPropList,
    LPWSTR* ppwzActionData
    )
{
    HRESULT hr = S_OK;

    hr = WcaWriteIntegerToCaData(iPropCount, ppwzActionData);
    ExitOnFailure(hr, "Failed to add count to custom action data");

    if (iPropCount) // count might be 0 event thought there are elements in the list
    {
        for (CPI_PROPERTY* pProp = pPropList; pProp; pProp = pProp->pNext)
        {
            hr = WcaWriteStringToCaData(pProp->wzName, ppwzActionData);
            ExitOnFailure1(hr, "Failed to add property name to custom action data, name: %S", pProp->wzName);

            hr = WcaWriteStringToCaData(pProp->pwzValue, ppwzActionData);
            ExitOnFailure1(hr, "Failed to add property value to custom action data, name: %S", pProp->wzName);
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiBuildAccountName(
    LPCWSTR pwzDomain,
    LPCWSTR pwzName,
    LPWSTR* ppwzAccount
    )
{
    HRESULT hr = S_OK;

    WCHAR wzComputerName[MAX_COMPUTERNAME_LENGTH + 1];
    ::ZeroMemory(wzComputerName, sizeof(wzComputerName));

    // if domain is '.', get computer name
    if (0 == lstrcmpW(pwzDomain, L"."))
    {
        DWORD dwSize = countof(wzComputerName);
        if (!::GetComputerNameW(wzComputerName, &dwSize))
            ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to get computer name");
    }

    // build account name
    hr = StrAllocFormatted(ppwzAccount, L"%s\\%s", *wzComputerName ? wzComputerName : pwzDomain, pwzName);
    ExitOnFailure(hr, "Failed to build domain user name");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetTempFileName(
    LPWSTR* ppwzTempFile
    )
{
    HRESULT hr = S_OK;

    // get temp path
    WCHAR wzTempPath[MAX_PATH];
    DWORD dw = ::GetTempPathW(countof(wzTempPath), wzTempPath);
    if (countof(wzTempPath) <= dw)
        ExitOnFailure(hr = E_FAIL, "TEMP directory path too long");

    // get unique number
    LARGE_INTEGER liCount;
    if (!::QueryPerformanceCounter(&liCount))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to query performance counter");

    // create temp file name
    hr = StrAllocFormatted(ppwzTempFile, L"%sCPI%I64X.tmp", wzTempPath, liCount.QuadPart);
    ExitOnFailure(hr, "Failed to create temp file name string");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiCreateId(
    LPWSTR pwzDest,
    SIZE_T cchDest
    )
{
    HRESULT hr = S_OK;

    GUID guid;

    // create new guid
    hr = ::CoCreateGuid(&guid);
    ExitOnFailure(hr, "Failed to create new guid");

    // convert guid to string
    if (0 == ::StringFromGUID2(guid, pwzDest, (int)cchDest))
        ExitOnFailure(hr = E_FAIL, "Failed to convert guid to string");

    hr = S_OK;

LExit:
    return hr;
}

BOOL CpiIsInstalled(
    INSTALLSTATE isInstalled
    )
{
    return INSTALLSTATE_LOCAL == isInstalled || INSTALLSTATE_SOURCE == isInstalled;
}

BOOL CpiWillBeInstalled(
    INSTALLSTATE isInstalled,
    INSTALLSTATE isAction
    )
{
    return WcaIsInstalling(isInstalled, isAction) ||
        (CpiIsInstalled(isInstalled) && !WcaIsUninstalling(isInstalled, isAction));
}

HRESULT PcaGuidToRegFormat(
    LPWSTR pwzGuid,
    LPWSTR pwzDest,
    SIZE_T cchDest
    )
{
    HRESULT hr = S_OK;

    GUID guid = GUID_NULL;
    int cch = 0;

    WCHAR wz[39];
    ::ZeroMemory(wz, sizeof(wz));

    cch = lstrlenW(pwzGuid);

    if (38 == cch && L'{' == pwzGuid[0] && L'}' == pwzGuid[37])
        StringCchCopyW(wz, countof(wz), pwzGuid);
    else if (36 == cch)
        StringCchPrintfW(wz, countof(wz), L"{%s}", pwzGuid);
    else
        ExitFunction1(hr = E_INVALIDARG);

    // convert string to guid
    hr = ::CLSIDFromString(wz, &guid);
    ExitOnFailure(hr, "Failed to parse guid string");

    // convert guid to string
    if (0 == ::StringFromGUID2(guid, pwzDest, (int)cchDest))
        ExitOnFailure(hr = E_FAIL, "Failed to convert guid to string");

    hr = S_OK;

LExit:
    return hr;
}


// helper function definitions

static HRESULT FindPropertyDefinition(
    CPI_PROPERTY_DEFINITION* pPropDefList,
    LPCWSTR pwzName,
    CPI_PROPERTY_DEFINITION** ppPropDef
    )
{
    for (CPI_PROPERTY_DEFINITION* pItm = pPropDefList; pItm->pwzName; pItm++)
    {
        if (0 == lstrcmpW(pItm->pwzName, pwzName))
        {
            *ppPropDef = pItm;
            return S_OK;
        }
    }

    return S_FALSE;
}

static HRESULT GetUserAccountName(
    LPCWSTR pwzKey,
    LPWSTR* ppwzAccount
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView, hRecKey, hRec;

    LPWSTR pwzDomain = NULL;
    LPWSTR pwzName = NULL;

    // create parameter record
    hRecKey = ::MsiCreateRecord(1);
    ExitOnNull(hRecKey, hr, E_OUTOFMEMORY, "Failed to create record");
    hr = WcaSetRecordString(hRecKey, 1, pwzKey);
    ExitOnFailure(hr, "Failed to set record string");

    // open view
    hr = WcaOpenView(vcsUserQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on User table");
    hr = WcaExecuteView(hView, hRecKey);
    ExitOnFailure(hr, "Failed to execute view on User table");

    // fetch record
    hr = WcaFetchSingleRecord(hView, &hRec);
    if (S_FALSE == hr)
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "User not found, key: %S", pwzKey);
    ExitOnFailure(hr, "Failed to fetch user record");

    // get user domain
    hr = WcaGetRecordFormattedString(hRec, uqDomain, &pwzDomain);
    ExitOnFailure(hr, "Failed to get domain");

    // get user name
    hr = WcaGetRecordFormattedString(hRec, uqName, &pwzName);
    ExitOnFailure(hr, "Failed to get name");

    // build account name
    hr = CpiBuildAccountName(pwzDomain, pwzName, ppwzAccount);
    ExitOnFailure(hr, "Failed to build account name");

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzName);

    return hr;
}
