//-------------------------------------------------------------------------------------------------
// <copyright file="cputilexec.cpp" company="Outercurve Foundation">
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


// private structs

struct CPI_WELLKNOWN_SID
{
    LPCWSTR pwzName;
    SID_IDENTIFIER_AUTHORITY iaIdentifierAuthority;
    BYTE nSubAuthorityCount;
    DWORD dwSubAuthority[8];
};


// well known SIDs

CPI_WELLKNOWN_SID wsWellKnownSids[] = {
    {L"\\Everyone",          SECURITY_WORLD_SID_AUTHORITY, 1, {SECURITY_WORLD_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\Administrators",    SECURITY_NT_AUTHORITY,        2, {SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0}},
    {L"\\LocalSystem",       SECURITY_NT_AUTHORITY,        1, {SECURITY_LOCAL_SYSTEM_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\LocalService",      SECURITY_NT_AUTHORITY,        1, {SECURITY_LOCAL_SERVICE_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\NetworkService",    SECURITY_NT_AUTHORITY,        1, {SECURITY_NETWORK_SERVICE_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\AuthenticatedUser", SECURITY_NT_AUTHORITY,        1, {SECURITY_AUTHENTICATED_USER_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\Guests",            SECURITY_NT_AUTHORITY,        2, {SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_GUESTS, 0, 0, 0, 0, 0, 0}},
    {L"\\Users",             SECURITY_NT_AUTHORITY,        2, {SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_USERS, 0, 0, 0, 0, 0, 0}},
    {L"\\CREATOR OWNER",     SECURITY_NT_AUTHORITY,        1, {SECURITY_CREATOR_OWNER_RID, 0, 0, 0, 0, 0, 0, 0}},
    {NULL,                   SECURITY_NULL_SID_AUTHORITY,  0, {0, 0, 0, 0, 0, 0, 0, 0}}
};


// prototypes for private helper functions

static HRESULT FindUserCollectionObjectIndex(
    ICatalogCollection* piColl,
    PSID pSid,
    int* pi
    );
static HRESULT CreateSidFromDomainRidPair(
    PSID pDomainSid,
    DWORD dwRid,
    PSID* ppSid
    );
static HRESULT InitLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr,
    LPCWSTR pwzStr,
    DWORD dwLen
    );
static void FreeLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr
    );
static HRESULT WriteFileAll(
    HANDLE hFile,
    PBYTE pbBuffer,
    DWORD dwBufferLength
    );
static HRESULT ReadFileAll(
    HANDLE hFile,
    PBYTE pbBuffer,
    DWORD dwBufferLength
    );


// variables

static ICOMAdminCatalog* gpiCatalog;


// function definitions

void CpiInitialize()
{
    // collections
    gpiCatalog = NULL;
}

void CpiFinalize()
{
    // collections
    ReleaseObject(gpiCatalog);
}

HRESULT CpiActionStartMessage(
    LPWSTR* ppwzActionData,
    BOOL fSuppress
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hRec;

    LPWSTR pwzData = NULL;

    // create record
    hRec = ::MsiCreateRecord(3);
    ExitOnNull(hRec, hr, E_OUTOFMEMORY, "Failed to create record");

    // action name
    hr = WcaReadStringFromCaData(ppwzActionData, &pwzData);
    ExitOnFailure(hr, "Failed to action name");

    er = ::MsiRecordSetStringW(hRec, 1, pwzData);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set action name");

    // description
    hr = WcaReadStringFromCaData(ppwzActionData, &pwzData);
    ExitOnFailure(hr, "Failed to description");

    er = ::MsiRecordSetStringW(hRec, 2, pwzData);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set description");

    // template
    hr = WcaReadStringFromCaData(ppwzActionData, &pwzData);
    ExitOnFailure(hr, "Failed to template");

    er = ::MsiRecordSetStringW(hRec, 3, pwzData);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set template");

    // message
    if (!fSuppress)
    {
        er = WcaProcessMessage(INSTALLMESSAGE_ACTIONSTART, hRec);
        if (0 == er || IDOK == er || IDYES == er)
        {
            hr = S_OK;
        }
        else if (IDABORT == er || IDCANCEL == er)
        {
            WcaSetReturnValue(ERROR_INSTALL_USEREXIT); // note that the user said exit
            hr = S_FALSE;
        }
        else
            hr = E_UNEXPECTED;
    }

LExit:
    // clean up
    ReleaseStr(pwzData);

    return hr;
}

HRESULT CpiActionDataMessage(
    DWORD cArgs,
    ...
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hRec;
    va_list args;

    // record
    hRec = ::MsiCreateRecord(cArgs);
    ExitOnNull(hRec, hr, E_OUTOFMEMORY, "Failed to create record");

    va_start(args, cArgs);
    for (DWORD i = 1; i <= cArgs; i++)
    {
        LPCWSTR pwzArg = va_arg(args, WCHAR*);
        if (pwzArg && *pwzArg)
        {
            er = ::MsiRecordSetStringW(hRec, i, pwzArg);
            ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set record field string");
        }
    }
    va_end(args);

    // message
    er = WcaProcessMessage(INSTALLMESSAGE_ACTIONDATA, hRec);
    if (0 == er || IDOK == er || IDYES == er)
    {
        hr = S_OK;
    }
    else if (IDABORT == er || IDCANCEL == er)
    {
        WcaSetReturnValue(ERROR_INSTALL_USEREXIT); // note that the user said exit
        hr = S_FALSE;
    }
    else
        hr = E_UNEXPECTED;

LExit:
    return hr;
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

HRESULT CpiLogCatalogErrorInfo()
{
    HRESULT hr = S_OK;

    ICOMAdminCatalog* piCatalog = NULL;
    ICatalogCollection* piErrColl = NULL;
    IDispatch* piDisp = NULL;
    ICatalogObject* piObj = NULL;

    LPWSTR pwzName = NULL;
    LPWSTR pwzErrorCode = NULL;
    LPWSTR pwzMajorRef = NULL;
    LPWSTR pwzMinorRef = NULL;

    // get catalog
    hr = CpiGetAdminCatalog(&piCatalog);
    ExitOnFailure(hr, "Failed to get COM+ admin catalog");

    // get error info collection
    hr = CpiGetCatalogCollection(L"ErrorInfo", &piErrColl);
    ExitOnFailure(hr, "Failed to get error info collection");

    // loop objects
    long lCnt;
    hr = piErrColl->get_Count(&lCnt);
    ExitOnFailure(hr, "Failed to get to number of items in collection");

    for (long i = 0; i < lCnt; i++)
    {
        // get ICatalogObject interface
        hr = piErrColl->get_Item(i, &piDisp);
        ExitOnFailure(hr, "Failed to get item from partitions collection");

        hr = piDisp->QueryInterface(IID_ICatalogObject, (void**)&piObj);
        ExitOnFailure(hr, "Failed to get IID_ICatalogObject interface");

        // get properties
        hr = CpiGetCollectionObjectValue(piObj, L"Name", &pwzName);
        ExitOnFailure(hr, "Failed to get name");
        hr = CpiGetCollectionObjectValue(piObj, L"ErrorCode", &pwzErrorCode);
        ExitOnFailure(hr, "Failed to get error code");
        hr = CpiGetCollectionObjectValue(piObj, L"MajorRef", &pwzMajorRef);
        ExitOnFailure(hr, "Failed to get major ref");
        hr = CpiGetCollectionObjectValue(piObj, L"MinorRef", &pwzMinorRef);
        ExitOnFailure(hr, "Failed to get minor ref");

        // write to log
        WcaLog(LOGMSG_STANDARD, "ErrorInfo: Name='%S', ErrorCode='%S', MajorRef='%S', MinorRef='%S'",
            pwzName, pwzErrorCode, pwzMajorRef, pwzMinorRef);

        // clean up
        ReleaseNullObject(piDisp);
        ReleaseNullObject(piObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCatalog);
    ReleaseObject(piErrColl);
    ReleaseObject(piDisp);
    ReleaseObject(piObj);

    ReleaseStr(pwzName);
    ReleaseStr(pwzErrorCode);
    ReleaseStr(pwzMajorRef);
    ReleaseStr(pwzMinorRef);

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
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
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
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
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

HRESULT CpiAddCollectionObject(
    ICatalogCollection* piColl,
    ICatalogObject** ppiObj
    )
{
    HRESULT hr = S_OK;

    IDispatch* piDisp = NULL;

    hr = piColl->Add(&piDisp);
    ExitOnFailure(hr, "Failed to add object to collection");

    hr = piDisp->QueryInterface(IID_ICatalogObject, (void**)ppiObj);
    ExitOnFailure(hr, "Failed to get IID_ICatalogObject interface");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piDisp);

    return hr;
}

HRESULT CpiPutCollectionObjectValue(
    ICatalogObject* piObj,
    LPCWSTR pwzPropName,
    LPCWSTR pwzValue
    )
{
    HRESULT hr = S_OK;

    BSTR bstrPropName = NULL;

    VARIANT vtVal;
    ::VariantInit(&vtVal);

    // allocate property name string
    bstrPropName = ::SysAllocString(pwzPropName);
    ExitOnNull(bstrPropName, hr, E_OUTOFMEMORY, "Failed to allocate property name string");

    // prepare value variant
    vtVal.vt = VT_BSTR;
    vtVal.bstrVal = ::SysAllocString(pwzValue);
    ExitOnNull(vtVal.bstrVal, hr, E_OUTOFMEMORY, "Failed to allocate property value string");

    // put value
    hr = piObj->put_Value(bstrPropName, vtVal);
    ExitOnFailure(hr, "Failed to put property value");

    hr = S_OK;

LExit:
    // clean up
    ReleaseBSTR(bstrPropName);
    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiPutCollectionObjectValues(
    ICatalogObject* piObj,
    CPI_PROPERTY* pPropList
    )
{
    HRESULT hr = S_OK;

    for (CPI_PROPERTY* pItm = pPropList; pItm; pItm = pItm->pNext)
    {
        // set property
        hr = CpiPutCollectionObjectValue(piObj, pItm->wzName, pItm->pwzValue);
        ExitOnFailure1(hr, "Failed to set object property value, name: %S", pItm->wzName);
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetCollectionObjectValue(
    ICatalogObject* piObj,
    LPCWSTR szPropName,
    LPWSTR* ppwzValue
    )
{
    HRESULT hr = S_OK;

    BSTR bstrPropName = NULL;

    VARIANT vtVal;
    ::VariantInit(&vtVal);

    // allocate property name string
    bstrPropName = ::SysAllocString(szPropName);
    ExitOnNull(bstrPropName, hr, E_OUTOFMEMORY, "Failed to allocate property name string");

    // get value
    hr = piObj->get_Value(bstrPropName, &vtVal);
    ExitOnFailure(hr, "Failed to get property value");

    hr = ::VariantChangeType(&vtVal, &vtVal, 0, VT_BSTR);
    ExitOnFailure(hr, "Failed to change variant type");

    hr = StrAllocString(ppwzValue, vtVal.bstrVal, ::SysStringLen(vtVal.bstrVal));
    ExitOnFailure(hr, "Failed to allocate memory for value string");

    hr = S_OK;

LExit:
    // clean up
    ReleaseBSTR(bstrPropName);
    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiResetObjectProperty(
    ICatalogCollection* piColl,
    ICatalogObject* piObj,
    LPCWSTR pwzPropName
    )
{
    HRESULT hr = S_OK;

    BSTR bstrPropName = NULL;

    long lChanges = 0;

    VARIANT vtVal;
    ::VariantInit(&vtVal);

    // allocate property name string
    bstrPropName = ::SysAllocString(pwzPropName);
    ExitOnNull(bstrPropName, hr, E_OUTOFMEMORY, "Failed to allocate deleteable property name string");

    // get value
    hr = piObj->get_Value(bstrPropName, &vtVal);
    ExitOnFailure(hr, "Failed to get deleteable property value");

    hr = ::VariantChangeType(&vtVal, &vtVal, 0, VT_BOOL);
    ExitOnFailure(hr, "Failed to change variant type");

    // if the deleteable property is set
    if (VARIANT_FALSE == vtVal.boolVal)
    {
        // clear property
        vtVal.boolVal = VARIANT_TRUE;

        hr = piObj->put_Value(bstrPropName, vtVal);
        ExitOnFailure(hr, "Failed to get property value");

        // save changes
        hr = piColl->SaveChanges(&lChanges);
        if (COMADMIN_E_OBJECTERRORS == hr)
            CpiLogCatalogErrorInfo();
        ExitOnFailure(hr, "Failed to save changes");
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseBSTR(bstrPropName);
    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiRemoveCollectionObject(
    ICatalogCollection* piColl,
    LPCWSTR pwzID,
    LPCWSTR pwzName,
    BOOL fResetDeleteable
    )
{
    HRESULT hr = S_OK;

    IDispatch* piDisp = NULL;
    ICatalogObject* piObj = NULL;

    BOOL fMatch = FALSE;

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
                fMatch = TRUE;

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
                fMatch = TRUE;

            ::VariantClear(&vtVal);
        }

        // if it's a match, remove it
        if (fMatch)
        {
            if (fResetDeleteable)
            {
                // reset deleteable property, if set
                hr = CpiResetObjectProperty(piColl, piObj, L"Deleteable");
                ExitOnFailure(hr, "Failed to reset deleteable property");
            }

            hr = piColl->Remove(i);
            ExitOnFailure(hr, "Failed to remove item from collection");
            break;
        }

        // release interface pointers
        ReleaseNullObject(piDisp);
        ReleaseNullObject(piObj);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piDisp);
    ReleaseObject(piObj);

    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiRemoveUserCollectionObject(
    ICatalogCollection* piColl,
    PSID pSid
    )
{
    HRESULT hr = S_OK;

    int i = 0;

    // find index
    hr = FindUserCollectionObjectIndex(piColl, pSid, &i);
    ExitOnFailure(hr, "Failed to find user collection index");

    if (S_FALSE == hr)
        ExitFunction(); // not found, exit with hr = S_FALSE

    // remove object
    hr = piColl->Remove(i);
    ExitOnFailure(hr, "Failed to remove object from collection");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiFindCollectionObjectByStringKey(
    ICatalogCollection* piColl,
    LPCWSTR pwzKey,
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

        // compare key
        hr = piObj->get_Key(&vtVal);
        ExitOnFailure(hr, "Failed to get key");

        hr = ::VariantChangeType(&vtVal, &vtVal, 0, VT_BSTR);
        ExitOnFailure(hr, "Failed to change variant type");

        if (0 == lstrcmpiW(vtVal.bstrVal, pwzKey))
        {
            if (ppiObj)
            {
                *ppiObj = piObj;
                piObj = NULL;
            }
            ExitFunction1(hr = S_OK);
        }

        // clean up
        ReleaseNullObject(piDisp);
        ReleaseNullObject(piObj);

        ::VariantClear(&vtVal);
    }

    hr = S_FALSE;

LExit:
    // clean up
    ReleaseObject(piDisp);
    ReleaseObject(piObj);

    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiFindCollectionObjectByIntegerKey(
    ICatalogCollection* piColl,
    long lKey,
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

        // compare key
        hr = piObj->get_Key(&vtVal);
        ExitOnFailure(hr, "Failed to get key");

        hr = ::VariantChangeType(&vtVal, &vtVal, 0, VT_I4);
        ExitOnFailure(hr, "Failed to change variant type");

        if (vtVal.lVal == lKey)
        {
            if (ppiObj)
            {
                *ppiObj = piObj;
                piObj = NULL;
            }
            ExitFunction1(hr = S_OK);
        }

        // clean up
        ReleaseNullObject(piDisp);
        ReleaseNullObject(piObj);

        ::VariantClear(&vtVal);
    }

    hr = S_FALSE;

LExit:
    // clean up
    ReleaseObject(piDisp);
    ReleaseObject(piObj);

    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiFindCollectionObjectByName(
    ICatalogCollection* piColl,
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

        // compare key
        hr = piObj->get_Name(&vtVal);
        ExitOnFailure(hr, "Failed to get key");

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

        // clean up
        ReleaseNullObject(piDisp);
        ReleaseNullObject(piObj);

        ::VariantClear(&vtVal);
    }

    hr = S_FALSE;

LExit:
    // clean up
    ReleaseObject(piDisp);
    ReleaseObject(piObj);

    ::VariantClear(&vtVal);

    return hr;
}

HRESULT CpiFindUserCollectionObject(
    ICatalogCollection* piColl,
    PSID pSid,
    ICatalogObject** ppiObj
    )
{
    HRESULT hr = S_OK;

    int i = 0;

    IDispatch* piDisp = NULL;

    // find index
    hr = FindUserCollectionObjectIndex(piColl, pSid, &i);
    ExitOnFailure(hr, "Failed to find user collection index");

    if (S_FALSE == hr)
        ExitFunction(); // not found, exit with hr = S_FALSE

    // get object
    if (ppiObj)
    {
        hr = piColl->get_Item(i, &piDisp);
        ExitOnFailure(hr, "Failed to get object from collection");

        hr = piDisp->QueryInterface(IID_ICatalogObject, (void**)ppiObj);
        ExitOnFailure(hr, "Failed to get IID_ICatalogObject interface");
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piDisp);

    return hr;
}

HRESULT CpiGetPartitionsCollection(
    ICatalogCollection** ppiPartColl
    )
{
    HRESULT hr = S_OK;

    // get collection
    hr = CpiGetCatalogCollection(L"Partitions", ppiPartColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetPartitionRolesCollection(
    LPCWSTR pwzPartID,
    ICatalogCollection** ppiRolesColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;

    // get partitions collection
    hr = CpiGetPartitionsCollection(&piPartColl);
    ExitOnFailure(hr, "Failed to get partitions collection");

    if (S_FALSE == hr)
        ExitFunction(); // partitions collection not found, exit with hr = S_FALSE

    // find object
    hr = CpiFindCollectionObjectByStringKey(piPartColl, pwzPartID, &piPartObj);
    ExitOnFailure(hr, "Failed to find collection object");

    if (S_FALSE == hr)
        ExitFunction(); // partition not found, exit with hr = S_FALSE

    // get roles collection
    hr = CpiGetCatalogCollection(piPartColl, piPartObj, L"RolesForPartition", ppiRolesColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);

    return hr;
}

HRESULT CpiGetUsersInPartitionRoleCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzRoleName,
    ICatalogCollection** ppiUsrInRoleColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piRoleColl = NULL;
    ICatalogObject* piRoleObj = NULL;

    // get roles collection
    hr = CpiGetPartitionRolesCollection(pwzPartID, &piRoleColl);
    ExitOnFailure(hr, "Failed to get roles collection");

    if (S_FALSE == hr)
        ExitFunction(); // partition roles collection not found, exit with hr = S_FALSE

    // find object
    hr = CpiFindCollectionObjectByName(piRoleColl, pwzRoleName, &piRoleObj);
    ExitOnFailure(hr, "Failed to find collection object");

    if (S_FALSE == hr)
        ExitFunction(); // user not found, exit with hr = S_FALSE

    // get roles collection
    hr = CpiGetCatalogCollection(piRoleColl, piRoleObj, L"UsersInPartitionRole", ppiUsrInRoleColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piRoleColl);
    ReleaseObject(piRoleObj);

    return hr;
}

HRESULT CpiGetPartitionUsersCollection(
    ICatalogCollection** ppiUserColl
    )
{
    HRESULT hr = S_OK;

    // get roles collection
    hr = CpiGetCatalogCollection(L"PartitionUsers", ppiUserColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetApplicationsCollection(
    LPCWSTR pwzPartID,
    ICatalogCollection** ppiAppColl
    )
{
    HRESULT hr = S_OK;

    ICOMAdminCatalog* piCatalog = NULL;
    ICOMAdminCatalog2* piCatalog2 = NULL;
    BSTR bstrGlobPartID = NULL;

    ICatalogCollection* piPartColl = NULL;
    ICatalogObject* piPartObj = NULL;

    // get catalog
    hr = CpiGetAdminCatalog(&piCatalog);
    ExitOnFailure(hr, "Failed to get COM+ admin catalog");

    // get ICOMAdminCatalog2 interface
    hr = piCatalog->QueryInterface(IID_ICOMAdminCatalog2, (void**)&piCatalog2);

    // COM+ 1.5 or later
    if (E_NOINTERFACE != hr)
    {
        ExitOnFailure(hr, "Failed to get IID_ICOMAdminCatalog2 interface");

        // partition id
        if (!pwzPartID || !*pwzPartID)
        {
            // get global partition id
            hr = piCatalog2->get_GlobalPartitionID(&bstrGlobPartID);
            ExitOnFailure(hr, "Failed to get global partition id");
        }

        // get partitions collection
        hr = CpiGetPartitionsCollection(&piPartColl);
        ExitOnFailure(hr, "Failed to get partitions collection");

        // find object
        hr = CpiFindCollectionObjectByStringKey(piPartColl, bstrGlobPartID ? bstrGlobPartID : pwzPartID, &piPartObj);
        ExitOnFailure(hr, "Failed to find collection object");

        if (S_FALSE == hr)
            ExitFunction(); // partition not found, exit with hr = S_FALSE

        // get applications collection
        hr = CpiGetCatalogCollection(piPartColl, piPartObj, L"Applications", ppiAppColl);
        ExitOnFailure(hr, "Failed to get catalog collection for partition");
    }

    // COM+ pre 1.5
    else
    {
        // this version of COM+ does not support partitions, make sure a partition was not specified
        if (pwzPartID && *pwzPartID)
            ExitOnFailure(hr = E_FAIL, "Partitions are not supported by this version of COM+");

        // get applications collection
        hr = CpiGetCatalogCollection(L"Applications", ppiAppColl);
        ExitOnFailure(hr, "Failed to get catalog collection");
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCatalog);
    ReleaseObject(piCatalog2);
    ReleaseBSTR(bstrGlobPartID);

    ReleaseObject(piPartColl);
    ReleaseObject(piPartObj);

    return hr;
}

HRESULT CpiGetRolesCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    ICatalogCollection** ppiRolesColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piAppColl = NULL;
    ICatalogObject* piAppObj = NULL;

    // get applications collection
    hr = CpiGetApplicationsCollection(pwzPartID, &piAppColl);
    ExitOnFailure(hr, "Failed to get applications collection");

    if (S_FALSE == hr)
        ExitFunction(); // applications collection not found, exit with hr = S_FALSE

    // find object
    hr = CpiFindCollectionObjectByStringKey(piAppColl, pwzAppID, &piAppObj);
    ExitOnFailure(hr, "Failed to find collection object");

    if (S_FALSE == hr)
        ExitFunction(); // application not found, exit with hr = S_FALSE

    // get roles collection
    hr = CpiGetCatalogCollection(piAppColl, piAppObj, L"Roles", ppiRolesColl);
    ExitOnFailure(hr, "Failed to catalog collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppColl);
    ReleaseObject(piAppObj);

    return hr;
}

HRESULT CpiGetUsersInRoleCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    LPCWSTR pwzRoleName,
    ICatalogCollection** ppiUsrInRoleColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piRoleColl = NULL;
    ICatalogObject* piRoleObj = NULL;

    // get roles collection
    hr = CpiGetRolesCollection(pwzPartID, pwzAppID, &piRoleColl);
    ExitOnFailure(hr, "Failed to get roles collection");

    if (S_FALSE == hr)
        ExitFunction(); // roles collection not found, exit with hr = S_FALSE

    // find object
    hr = CpiFindCollectionObjectByName(piRoleColl, pwzRoleName, &piRoleObj);
    ExitOnFailure(hr, "Failed to find collection object");

    if (S_FALSE == hr)
        ExitFunction(); // role not found, exit with hr = S_FALSE

    // get roles collection
    hr = CpiGetCatalogCollection(piRoleColl, piRoleObj, L"UsersInRole", ppiUsrInRoleColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piRoleColl);
    ReleaseObject(piRoleObj);

    return hr;
}

HRESULT CpiGetComponentsCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    ICatalogCollection** ppiCompsColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piAppColl = NULL;
    ICatalogObject* piAppObj = NULL;

    // get applications collection
    hr = CpiGetApplicationsCollection(pwzPartID, &piAppColl);
    ExitOnFailure(hr, "Failed to get applications collection");

    if (S_FALSE == hr)
        ExitFunction(); // applications collection not found, exit with hr = S_FALSE

    // find object
    hr = CpiFindCollectionObjectByStringKey(piAppColl, pwzAppID, &piAppObj);
    ExitOnFailure(hr, "Failed to find collection object");

    if (S_FALSE == hr)
        ExitFunction(); // application not found, exit with hr = S_FALSE

    // get components collection
    hr = CpiGetCatalogCollection(piAppColl, piAppObj, L"Components", ppiCompsColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piAppColl);
    ReleaseObject(piAppObj);

    return hr;
}

HRESULT CpiGetInterfacesCollection(
    ICatalogCollection* piCompColl,
    ICatalogObject* piCompObj,
    ICatalogCollection** ppiIntfColl
    )
{
    HRESULT hr = S_OK;

    // get interfaces collection
    hr = CpiGetCatalogCollection(piCompColl, piCompObj, L"InterfacesForComponent", ppiIntfColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetMethodsCollection(
    ICatalogCollection* piIntfColl,
    ICatalogObject* piIntfObj,
    ICatalogCollection** ppiMethColl
    )
{
    HRESULT hr = S_OK;

    // get interfaces collection
    hr = CpiGetCatalogCollection(piIntfColl, piIntfObj, L"MethodsForInterface", ppiMethColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiGetSubscriptionsCollection(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    LPCWSTR pwzCompCLSID,
    ICatalogCollection** ppiSubsColl
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piCompColl = NULL;
    ICatalogObject* piCompObj = NULL;

    // get components collection
    hr = CpiGetComponentsCollection(pwzPartID, pwzAppID, &piCompColl);
    ExitOnFailure(hr, "Failed to get components collection");

    if (S_FALSE == hr)
        ExitFunction(); // components collection not found, exit with hr = S_FALSE

    // find object
    hr = CpiFindCollectionObjectByStringKey(piCompColl, pwzCompCLSID, &piCompObj);
    ExitOnFailure(hr, "Failed to find collection object");

    if (S_FALSE == hr)
        ExitFunction(); // component not found, exit with hr = S_FALSE

    // get subscriptions collection
    hr = CpiGetCatalogCollection(piCompColl, piCompObj, L"SubscriptionsForComponent", ppiSubsColl);
    ExitOnFailure(hr, "Failed to get catalog collection");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCompColl);
    ReleaseObject(piCompObj);

    return hr;
}

HRESULT CpiReadPropertyList(
    LPWSTR* ppwzData,
    CPI_PROPERTY** ppPropList
    )
{
    HRESULT hr = S_OK;

    CPI_PROPERTY* pItm = NULL;
    LPWSTR pwzName = NULL;

    // clear list if it already contains items
    if (*ppPropList)
        CpiFreePropertyList(*ppPropList);
    *ppPropList = NULL;

    // read property count
    int iPropCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iPropCnt);
    ExitOnFailure(hr, "Failed to read property count");

    for (int i = 0; i < iPropCnt; i++)
    {
        // allocate new element
        pItm = (CPI_PROPERTY*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_PROPERTY));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // Name
        hr = WcaReadStringFromCaData(ppwzData, &pwzName);
        ExitOnFailure(hr, "Failed to read name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzName);

        // Value
        hr = WcaReadStringFromCaData(ppwzData, &pItm->pwzValue);
        ExitOnFailure(hr, "Failed to read property value");

        // add to list
        if (*ppPropList)
            pItm->pNext = *ppPropList;
        *ppPropList = pItm;
        pItm = NULL;
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzName);

    if (pItm)
        CpiFreePropertyList(pItm);

    return hr;
}

void CpiFreePropertyList(
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

HRESULT CpiWriteKeyToRollbackFile(
    HANDLE hFile,
    LPCWSTR pwzKey
    )
{
    HRESULT hr = S_OK;

    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    ::ZeroMemory(wzKey, sizeof(wzKey));
    hr = StringCchCopyW(wzKey, countof(wzKey), pwzKey);
    ExitOnFailure(hr, "Failed to copy key");

    hr = WriteFileAll(hFile, (PBYTE)wzKey, MAX_DARWIN_KEY * sizeof(WCHAR));
    ExitOnFailure(hr, "Failed to write buffer");

    FlushFileBuffers(hFile);

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiWriteIntegerToRollbackFile(
    HANDLE hFile,
    int i
    )
{
    HRESULT hr = S_OK;

    hr = WriteFileAll(hFile, (PBYTE)&i, sizeof(int));
    ExitOnFailure(hr, "Failed to write buffer");

    FlushFileBuffers(hFile);

    hr = S_OK;

LExit:
    return hr;
}

HRESULT CpiReadRollbackDataList(
    HANDLE hFile,
    CPI_ROLLBACK_DATA** pprdList
    )
{
    HRESULT hr = S_OK;

    int iCount;

    CPI_ROLLBACK_DATA* pItm = NULL;

    // read count
    hr = ReadFileAll(hFile, (PBYTE)&iCount, sizeof(int));
    if (HRESULT_FROM_WIN32(ERROR_HANDLE_EOF) == hr)
        ExitFunction1(hr = S_OK); // EOF reached, nothing left to read
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCount; i++)
    {
        // allocate new element
        pItm = (CPI_ROLLBACK_DATA*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_ROLLBACK_DATA));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // read from file
        hr = ReadFileAll(hFile, (PBYTE)pItm->wzKey, MAX_DARWIN_KEY * sizeof(WCHAR));
        if (HRESULT_FROM_WIN32(ERROR_HANDLE_EOF) == hr)
            break; // EOF reached, nothing left to read
        ExitOnFailure(hr, "Failed to read key");

        hr = ReadFileAll(hFile, (PBYTE)&pItm->iStatus, sizeof(int));
        if (HRESULT_FROM_WIN32(ERROR_HANDLE_EOF) == hr)
            pItm->iStatus = 0; // EOF reached, the operation was interupted; set status to zero
        else
            ExitOnFailure(hr, "Failed to read status");

        // add to list
        if (*pprdList)
            pItm->pNext = *pprdList;
        *pprdList = pItm;
        pItm = NULL;
    }

    hr = S_OK;

LExit:
    // clean up
    if (pItm)
        CpiFreeRollbackDataList(pItm);

    return hr;
}

void CpiFreeRollbackDataList(
    CPI_ROLLBACK_DATA* pList
    )
{
    while (pList)
    {
        CPI_ROLLBACK_DATA* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

HRESULT CpiFindRollbackStatus(
    CPI_ROLLBACK_DATA* pList,
    LPCWSTR pwzKey,
    int* piStatus
    )
{
    HRESULT hr = S_OK;

    for (; pList; pList = pList->pNext)
    {
        if (0 == lstrcmpW(pList->wzKey, pwzKey))
        {
            *piStatus = pList->iStatus;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = S_FALSE;

LExit:
    return hr;
}

HRESULT CpiAccountNameToSid(
    LPCWSTR pwzAccountName,
    PSID* ppSid
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    NTSTATUS st = 0;

    PSID pSid = NULL;
    LSA_OBJECT_ATTRIBUTES loaAttributes;
    LSA_HANDLE lsahPolicy = NULL;
    LSA_UNICODE_STRING lusName;
    PLSA_REFERENCED_DOMAIN_LIST plrdsDomains = NULL;
    PLSA_TRANSLATED_SID pltsSid = NULL;

    ::ZeroMemory(&loaAttributes, sizeof(loaAttributes));
    ::ZeroMemory(&lusName, sizeof(lusName));

    // identify well known SIDs
    for (CPI_WELLKNOWN_SID* pWS = wsWellKnownSids; pWS->pwzName; pWS++)
    {
        if (0 == lstrcmpiW(pwzAccountName, pWS->pwzName))
        {
            // allocate SID buffer
            pSid = (PSID)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, ::GetSidLengthRequired(pWS->nSubAuthorityCount));
            ExitOnNull(pSid, hr, E_OUTOFMEMORY, "Failed to allocate buffer for SID");

            // initialize SID
            ::InitializeSid(pSid, &pWS->iaIdentifierAuthority, pWS->nSubAuthorityCount);

            // copy sub autorities
            for (DWORD i = 0; i < pWS->nSubAuthorityCount; i++)
                *::GetSidSubAuthority(pSid, i) = pWS->dwSubAuthority[i];

            break;
        }
    }

    // lookup name
    if (!pSid)
    {
        // open policy handle
        st = ::LsaOpenPolicy(NULL, &loaAttributes, POLICY_ALL_ACCESS, &lsahPolicy);
        er = ::LsaNtStatusToWinError(st);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to open policy handle");

        // create account name lsa unicode string
        hr = InitLsaUnicodeString(&lusName, pwzAccountName, (DWORD)wcslen(pwzAccountName));
        ExitOnFailure(hr, "Failed to initialize account name string");

        // lookup name
        st = ::LsaLookupNames(lsahPolicy, 1, &lusName, &plrdsDomains, &pltsSid);
        er = ::LsaNtStatusToWinError(st);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to lookup account names");

        if (SidTypeDomain == pltsSid->Use)
            ExitOnFailure(hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED), "Domain SIDs not supported");

        // convert sid
        hr = CreateSidFromDomainRidPair(plrdsDomains->Domains[pltsSid->DomainIndex].Sid, pltsSid->RelativeId, &pSid);
        ExitOnFailure(hr, "Failed to convert SID");
    }

    *ppSid = pSid;
    pSid = NULL;

    hr = S_OK;

LExit:
    // clean up
    if (pSid)
        ::HeapFree(::GetProcessHeap(), 0, pSid);
    if (lsahPolicy)
        ::LsaClose(lsahPolicy);
    if (plrdsDomains)
        ::LsaFreeMemory(plrdsDomains);
    if (pltsSid)
        ::LsaFreeMemory(pltsSid);
    FreeLsaUnicodeString(&lusName);

    return hr;
}

HRESULT CpiSidToAccountName(
    PSID pSid,
    LPWSTR* ppwzAccountName
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    NTSTATUS st = 0;

    LSA_OBJECT_ATTRIBUTES loaAttributes;
    LSA_HANDLE lsahPolicy = NULL;
    PLSA_REFERENCED_DOMAIN_LIST plrdsDomains = NULL;
    PLSA_TRANSLATED_NAME pltnName = NULL;

    LPWSTR pwzDomain = NULL;
    LPWSTR pwzName = NULL;

    ::ZeroMemory(&loaAttributes, sizeof(loaAttributes));

    // open policy handle
    st = ::LsaOpenPolicy(NULL, &loaAttributes, POLICY_ALL_ACCESS, &lsahPolicy);
    er = ::LsaNtStatusToWinError(st);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to open policy handle");

    // lookup SID
    st = ::LsaLookupSids(lsahPolicy, 1, &pSid, &plrdsDomains, &pltnName);
    er = ::LsaNtStatusToWinError(st);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed lookup SID");

    if (SidTypeDomain == pltnName->Use)
        ExitOnFailure(hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED), "Domain SIDs not supported");

    // format account name string
    if (SidTypeWellKnownGroup != pltnName->Use)
    {
        PLSA_UNICODE_STRING plusDomain = &plrdsDomains->Domains[pltnName->DomainIndex].Name;
        hr = StrAllocString(&pwzDomain, plusDomain->Buffer, plusDomain->Length / sizeof(WCHAR));
        ExitOnFailure(hr, "Failed to allocate name string");
    }

    hr = StrAllocString(&pwzName, pltnName->Name.Buffer, pltnName->Name.Length / sizeof(WCHAR));
    ExitOnFailure(hr, "Failed to allocate domain string");

    hr = StrAllocFormatted(ppwzAccountName, L"%s\\%s", pwzDomain ? pwzDomain : L"", pwzName);
    ExitOnFailure(hr, "Failed to format account name string");

    hr = S_OK;

LExit:
    // clean up
    if (lsahPolicy)
        ::LsaClose(lsahPolicy);
    if (plrdsDomains)
        ::LsaFreeMemory(plrdsDomains);
    if (pltnName)
        ::LsaFreeMemory(pltnName);

    ReleaseStr(pwzDomain);
    ReleaseStr(pwzName);

    return hr;
}

// helper function definitions

static HRESULT FindUserCollectionObjectIndex(
    ICatalogCollection* piColl,
    PSID pSid,
    int* pi
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    NTSTATUS st = 0;

    long i = 0;
    long lCollCnt = 0;

    LSA_OBJECT_ATTRIBUTES loaAttributes;
    LSA_HANDLE lsahPolicy = NULL;
    PLSA_UNICODE_STRING plusNames = NULL;
    PLSA_REFERENCED_DOMAIN_LIST plrdsDomains = NULL;
    PLSA_TRANSLATED_SID pltsSids = NULL;

    IDispatch* piDisp = NULL;
    ICatalogObject* piObj = NULL;
    VARIANT vtVal;

    PSID pTmpSid = NULL;

    PLSA_TRANSLATED_SID pltsSid;

    ::VariantInit(&vtVal);
    ::ZeroMemory(&loaAttributes, sizeof(loaAttributes));

    // open policy handle
    st = ::LsaOpenPolicy(NULL, &loaAttributes, POLICY_ALL_ACCESS, &lsahPolicy);
    er = ::LsaNtStatusToWinError(st);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to open policy handle");

    // get number of elements in collection
    hr = piColl->get_Count(&lCollCnt);
    ExitOnFailure(hr, "Failed to get to number of objects in collection");

    if (0 == lCollCnt)
        ExitFunction1(hr = S_FALSE); // not found

    // allocate name buffer
    plusNames = (PLSA_UNICODE_STRING)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(LSA_UNICODE_STRING) * lCollCnt);
    ExitOnNull(plusNames, hr, E_OUTOFMEMORY, "Failed to allocate names buffer");

    // get accounts in collection
    for (i = 0; i < lCollCnt; i++)
    {
        // get ICatalogObject interface
        hr = piColl->get_Item(i, &piDisp);
        ExitOnFailure(hr, "Failed to get object from collection");

        hr = piDisp->QueryInterface(IID_ICatalogObject, (void**)&piObj);
        ExitOnFailure(hr, "Failed to get IID_ICatalogObject interface");

        // get value
        hr = piObj->get_Key(&vtVal);
        ExitOnFailure(hr, "Failed to get key");

        hr = ::VariantChangeType(&vtVal, &vtVal, 0, VT_BSTR);
        ExitOnFailure(hr, "Failed to change variant type");

        // copy account name string
        hr = InitLsaUnicodeString(&plusNames[i], vtVal.bstrVal, ::SysStringLen(vtVal.bstrVal));
        ExitOnFailure(hr, "Failed to initialize account name string");

        // clean up
        ReleaseNullObject(piDisp);
        ReleaseNullObject(piObj);
        ::VariantClear(&vtVal);
    }

    // lookup names
    st = ::LsaLookupNames(lsahPolicy, lCollCnt, plusNames, &plrdsDomains, &pltsSids);
    er = ::LsaNtStatusToWinError(st);
    if (ERROR_NONE_MAPPED != er && ERROR_SOME_NOT_MAPPED != er)
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to lookup account names");

    // compare SIDs
    for (i = 0; i < lCollCnt; i++)
    {
        // get SID
        pltsSid = &pltsSids[i];
        if (SidTypeDomain == pltsSid->Use || SidTypeInvalid == pltsSid->Use || SidTypeUnknown == pltsSid->Use)
            continue; // ignore...

        hr = CreateSidFromDomainRidPair(plrdsDomains->Domains[pltsSid->DomainIndex].Sid, pltsSid->RelativeId, &pTmpSid);
        ExitOnFailure(hr, "Failed to convert SID");

        // compare SIDs
        if (::EqualSid(pSid, pTmpSid))
        {
            *pi = i;
            ExitFunction1(hr = S_OK);
        }
    }

    if (ERROR_NONE_MAPPED == er || ERROR_SOME_NOT_MAPPED == er)
        hr = HRESULT_FROM_WIN32(er);
    else
        hr = S_FALSE; // not found

LExit:
    // clean up
    ReleaseObject(piDisp);
    ReleaseObject(piObj);
    ::VariantClear(&vtVal);

    if (plusNames)
    {
        for (i = 0; i < lCollCnt; i++)
            FreeLsaUnicodeString(&plusNames[i]);
        ::HeapFree(::GetProcessHeap(), 0, plusNames);
    }

    if (lsahPolicy)
        ::LsaClose(lsahPolicy);
    if (plrdsDomains)
        ::LsaFreeMemory(plrdsDomains);
    if (pltsSids)
        ::LsaFreeMemory(pltsSids);

    if (pTmpSid)
        ::HeapFree(::GetProcessHeap(), 0, pTmpSid);

    return hr;
}

static HRESULT CreateSidFromDomainRidPair(
    PSID pDomainSid,
    DWORD dwRid,
    PSID* ppSid
    )
{
    HRESULT hr = S_OK;
    PSID pSid = NULL;

    // get domain SID sub authority count
    UCHAR ucSubAuthorityCount = *::GetSidSubAuthorityCount(pDomainSid);

    // allocate SID buffer
    DWORD dwLengthRequired = ::GetSidLengthRequired(ucSubAuthorityCount + (UCHAR)1);
    if (*ppSid)
    {
        SIZE_T ccb = ::HeapSize(::GetProcessHeap(), 0, *ppSid);
        if (-1 == ccb)
            ExitOnFailure(hr = E_FAIL, "Failed to get size of SID buffer");

        if (ccb < dwLengthRequired)
        {
            pSid = (PSID)::HeapReAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, *ppSid, dwLengthRequired);
            ExitOnNull1(pSid, hr, E_OUTOFMEMORY, "Failed to reallocate buffer for SID, len: %d", dwLengthRequired);
            *ppSid = pSid;
        }
    }
    else
    {
        *ppSid = (PSID)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, dwLengthRequired);
        ExitOnNull1(*ppSid, hr, E_OUTOFMEMORY, "Failed to allocate buffer for SID, len: %d", dwLengthRequired);
    }

    ::InitializeSid(*ppSid, ::GetSidIdentifierAuthority(pDomainSid), ucSubAuthorityCount + (UCHAR)1);

    // copy sub autorities
    DWORD i = 0;
    for (; i < ucSubAuthorityCount; i++)
        *::GetSidSubAuthority(*ppSid, i) = *::GetSidSubAuthority(pDomainSid, i);
    *::GetSidSubAuthority(*ppSid, i) = dwRid;

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT InitLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr,
    LPCWSTR pwzStr,
    DWORD dwLen
    )
{
    HRESULT hr = S_OK;

    plusStr->Length = (USHORT)dwLen * sizeof(WCHAR);
    plusStr->MaximumLength = (USHORT)(dwLen + 1) * sizeof(WCHAR);

    plusStr->Buffer = (WCHAR*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(WCHAR) * (dwLen + 1));
    ExitOnNull(plusStr->Buffer, hr, E_OUTOFMEMORY, "Failed to allocate account name string");

    hr = StringCchCopyW(plusStr->Buffer, dwLen + 1, pwzStr);
    ExitOnFailure(hr, "Failed to copy buffer");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr
    )
{
    if (plusStr->Buffer)
        ::HeapFree(::GetProcessHeap(), 0, plusStr->Buffer);
}

static HRESULT WriteFileAll(
    HANDLE hFile,
    PBYTE pbBuffer,
    DWORD dwBufferLength
    )
{
    HRESULT hr = S_OK;

    DWORD dwBytesWritten;

    while (dwBufferLength)
    {
        if (!::WriteFile(hFile, pbBuffer, dwBufferLength, &dwBytesWritten, NULL))
            ExitFunction1(hr = HRESULT_FROM_WIN32(::GetLastError()));

        dwBufferLength -= dwBytesWritten;
        pbBuffer += dwBytesWritten;
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT ReadFileAll(
    HANDLE hFile,
    PBYTE pbBuffer,
    DWORD dwBufferLength
    )
{
    HRESULT hr = S_OK;

    DWORD dwBytesRead;

    while (dwBufferLength)
    {
        if (!::ReadFile(hFile, pbBuffer, dwBufferLength, &dwBytesRead, NULL))
            ExitFunction1(hr = HRESULT_FROM_WIN32(::GetLastError()));

        if (0 == dwBytesRead)
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_HANDLE_EOF));

        dwBufferLength -= dwBytesRead;
        pbBuffer += dwBytesRead;
    }

    hr = S_OK;

LExit:
    return hr;
}
