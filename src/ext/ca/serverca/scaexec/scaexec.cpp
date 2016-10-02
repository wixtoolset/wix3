// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


/********************************************************************
 DllMain - standard entry point for all WiX CustomActions

********************************************************************/
extern "C" BOOL WINAPI DllMain(
    __in HINSTANCE hInst,
    __in ULONG ulReason,
    __in LPVOID)
{
    switch (ulReason)
    {
    case DLL_PROCESS_ATTACH:
        WcaGlobalInitialize(hInst);
        break;

    case DLL_PROCESS_DETACH:
        WcaGlobalFinalize();
        break;
    }

    return TRUE;
}


/********************************************************************
 StartMetabaseTransaction - CUSTOM ACTION ENTRY POINT for backing up metabase

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall StartMetabaseTransaction(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug StartMetabaseTransaction here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    IMSAdminBase* piMetabase = NULL;
    LPWSTR pwzData = NULL;
    BOOL fInitializedCom = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "StartMetabaseTransaction");
    ExitOnFailure(hr, "failed to initialize StartMetabaseTransaction");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;
    hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
    if (hr == REGDB_E_CLASSNOTREG)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to get IIMSAdminBase to backup - continuing");
        hr = S_OK;
    }
    else
    {
        MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");

        hr = WcaGetProperty(L"CustomActionData", &pwzData);
        ExitOnFailure(hr, "failed to get CustomActionData");

        // back up the metabase
        Assert(lstrlenW(pwzData) < MD_BACKUP_MAX_LEN);

        // MD_BACKUP_OVERWRITE = Overwrite if a backup of the same name and version exists in the backup location
        hr = piMetabase->Backup(pwzData, MD_BACKUP_NEXT_VERSION, MD_BACKUP_OVERWRITE | MD_BACKUP_FORCE_BACKUP | MD_BACKUP_SAVE_FIRST);
        if (MD_WARNING_SAVE_FAILED == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to save metabase before backing up - continuing");
            hr = S_OK;
        }
        MessageExitOnFailure1(hr, msierrIISFailedStartTransaction, "failed to begin metabase transaction: '%ls'", pwzData);
    }
    hr = WcaProgressMessage(COST_IIS_TRANSACTIONS, FALSE);
LExit:
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 RollbackMetabaseTransaction - CUSTOM ACTION ENTRY POINT for unbacking up metabase

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall RollbackMetabaseTransaction(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug RollbackMetabaseTransaction here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    IMSAdminBase* piMetabase = NULL;
    LPWSTR pwzData = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "RollbackMetabaseTransaction");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;
    hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
    if (hr == REGDB_E_CLASSNOTREG)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to get IIMSAdminBase to rollback - continuing");
        hr = S_OK;
        ExitFunction();
    }
    ExitOnFailure(hr, "failed to get IID_IIMSAdminBase object");


    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    hr = piMetabase->Restore(pwzData, MD_BACKUP_HIGHEST_VERSION, 0);
    ExitOnFailure1(hr, "failed to rollback metabase transaction: '%ls'", pwzData);

    hr = piMetabase->DeleteBackup(pwzData, MD_BACKUP_HIGHEST_VERSION);
    ExitOnFailure1(hr, "failed to cleanup metabase transaction '%ls', continuing", pwzData);

LExit:
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 CommitMetabaseTransaction - CUSTOM ACTION ENTRY POINT for unbacking up metabase

  Input:  deferred CustomActionData - BackupName
 * *****************************************************************/
extern "C" UINT __stdcall CommitMetabaseTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    IMSAdminBase* piMetabase = NULL;
    LPWSTR pwzData = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "CommitMetabaseTransaction");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;
    hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
    if (hr == REGDB_E_CLASSNOTREG)
    {
        WcaLog(LOGMSG_STANDARD, "Failed to get IIMSAdminBase to commit - continuing");
        hr = S_OK;
        ExitFunction();
    }
    ExitOnFailure(hr, "failed to get IID_IIMSAdminBase object");

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    hr = piMetabase->DeleteBackup(pwzData, MD_BACKUP_HIGHEST_VERSION);
    ExitOnFailure1(hr, "failed to cleanup metabase transaction: '%ls'", pwzData);

LExit:
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 CreateMetabaseKey - Installs metabase keys

  Input:  deferred CustomActionData - Key
 * *****************************************************************/
static HRESULT CreateMetabaseKey(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
//AssertSz(FALSE, "debug CreateMetabaseKey here");
    HRESULT hr = S_OK;
    METADATA_HANDLE mhRoot = 0;
    LPWSTR pwzData = NULL;
    LPCWSTR pwzKey;

    int i;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzData);
    ExitOnFailure(hr, "failed to read key from custom action data");

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open root key, retrying %d time(s)...", i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    }
    MessageExitOnFailure1(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", L"/LM");

    pwzKey = pwzData + 3;

    WcaLog(LOGMSG_VERBOSE, "Creating Metabase Key: %ls", pwzKey);

    hr = piMetabase->AddKey(mhRoot, pwzKey);
    if (HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS) == hr)
    {
        WcaLog(LOGMSG_VERBOSE, "Key `%ls` already existed, continuing.", pwzData);
        hr = S_OK;
    }
    MessageExitOnFailure1(hr, msierrIISFailedCreateKey, "failed to create metabase key: %ls", pwzKey);

    hr = WcaProgressMessage(COST_IIS_CREATEKEY, FALSE);

LExit:
    if (mhRoot)
    {
        piMetabase->CloseKey(mhRoot);
    }

    return hr;
}


/********************************************************************
 WriteMetabaseValue -Installs metabase values

  Input:  deferred CustomActionData - Key\tIdentifier\tAttributes\tUserType\tDataType\tData
 * *****************************************************************/
static HRESULT WriteMetabaseValue(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
    //AssertSz(FALSE, "debug WriteMetabaseValue here");
    HRESULT hr = S_OK;

    METADATA_HANDLE mhKey = 0;

    LPWSTR pwzKey = NULL;
    LPWSTR pwzTemp = NULL;
    DWORD dwData = 0;
    DWORD dwTemp = 0;
    BOOL fFreeData = FALSE;
    METADATA_RECORD mr;
    ::ZeroMemory((LPVOID)&mr, sizeof(mr));
    METADATA_RECORD mrGet;
    ::ZeroMemory((LPVOID)&mrGet, sizeof(mrGet));

    int i;

    // get the key first
    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzKey);
    ExitOnFailure(hr, "failed to read key");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDIdentifier));
    ExitOnFailure(hr, "failed to read identifier");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDAttributes));
    ExitOnFailure(hr, "failed to read attributes");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDUserType));
    ExitOnFailure(hr, "failed to read user type");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&mr.dwMDDataType));
    ExitOnFailure(hr, "failed to read data type");

    switch (mr.dwMDDataType) // data
    {
    case DWORD_METADATA:
        hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&dwData));
        mr.dwMDDataLen = sizeof(dwData);
        mr.pbMDData = reinterpret_cast<BYTE*>(&dwData);
        break;
    case STRING_METADATA:
        hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzTemp);
        mr.dwMDDataLen = (lstrlenW(pwzTemp) + 1) * sizeof(WCHAR);
        mr.pbMDData = reinterpret_cast<BYTE*>(pwzTemp);
        break;
    case MULTISZ_METADATA:
        {
        hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzTemp);
        mr.dwMDDataLen = (lstrlenW(pwzTemp) + 1) * sizeof(WCHAR);
        for (LPWSTR pwzT = pwzTemp; *pwzT; ++pwzT)
        {
            if (MAGIC_MULTISZ_CHAR == *pwzT)
            {
                *pwzT = L'\0';
            }
        }
        mr.pbMDData = reinterpret_cast<BYTE*>(pwzTemp);
        }
        break;
    case BINARY_METADATA:
        hr = WcaReadStreamFromCaData(ppwzCustomActionData, &mr.pbMDData, reinterpret_cast<DWORD_PTR *>(&mr.dwMDDataLen));
        fFreeData = TRUE;
        break;
    default:
        hr = E_UNEXPECTED;
        break;
    }
    ExitOnFailure(hr, "failed to parse CustomActionData into metabase record");

    WcaLog(LOGMSG_VERBOSE, "Writing Metabase Value Under Key: %ls ID: %d", pwzKey, mr.dwMDIdentifier);

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open '%ls' key, retrying %d time(s)...", pwzKey, i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    }
    MessageExitOnFailure1(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", pwzKey);

    if (lstrlenW(pwzKey) < 3)
    {
        ExitOnFailure1(hr = E_INVALIDARG, "Key didn't begin with \"/LM\" as expected - key value: %ls", pwzKey);
    }

    hr = piMetabase->SetData(mhKey, pwzKey + 3, &mr); // pwzKey + 3 to skip "/LM" that was used to open the key.

    // This means we're trying to write to a secure key without the secure flag set - let's try again with the secure flag set
    if (MD_ERROR_CANNOT_REMOVE_SECURE_ATTRIBUTE == hr)
    {
        mr.dwMDAttributes |= METADATA_SECURE;
        hr = piMetabase->SetData(mhKey, pwzKey + 3, &mr);
    }

    // If IIS6 returned failure, let's check if the correct value exists in the metabase before actually failing the CA
    if (FAILED(hr))
    {
        // Backup the original failure error, so we can log it below if necessary
        HRESULT hrOldValue = hr;

        mrGet.dwMDIdentifier = mr.dwMDIdentifier;
        mrGet.dwMDAttributes = METADATA_NO_ATTRIBUTES;
        mrGet.dwMDUserType = mr.dwMDUserType;
        mrGet.dwMDDataType = mr.dwMDDataType;
        mrGet.dwMDDataLen = mr.dwMDDataLen;
        mrGet.pbMDData = static_cast<BYTE*>(MemAlloc(mr.dwMDDataLen, TRUE));

        hr = piMetabase->GetData(mhKey, pwzKey + 3, &mrGet, &dwTemp);
        if (SUCCEEDED(hr))
        {
            if (mrGet.dwMDDataType == mr.dwMDDataType && mrGet.dwMDDataLen == mr.dwMDDataLen && 0 == memcmp(mrGet.pbMDData, mr.pbMDData, mr.dwMDDataLen))
            {
                WcaLog(LOGMSG_VERBOSE, "Received error while writing metabase value under key: %ls ID: %d with error 0x%x, but the correct value is in the metabase - continuing", pwzKey, mr.dwMDIdentifier, hrOldValue);
                hr = S_OK;
            }
            else
            {
                WcaLog(LOGMSG_VERBOSE, "Succeeded in checking metabase value after write value, but the values didn't match");
                hr = hrOldValue;
            }
        }
        else
        {
            WcaLog(LOGMSG_VERBOSE, "Failed to check value after metabase write failure (error code 0x%x)", hr);
            hr = hrOldValue;
        }
    }
    MessageExitOnFailure1(hr, msierrIISFailedWriteData, "failed to write data to metabase key: %ls", pwzKey);

    hr = WcaProgressMessage(COST_IIS_WRITEVALUE, FALSE);

LExit:
    ReleaseStr(pwzTemp);
    ReleaseStr(pwzKey);

    if (mhKey)
    {
        piMetabase->CloseKey(mhKey);
    }

    if (fFreeData && mr.pbMDData)
    {
        MemFree(mr.pbMDData);
    }

    return hr;
}


/********************************************************************
 DeleteMetabaseValue -Installs metabase values

  Input:  deferred CustomActionData - Key\tIdentifier\tAttributes\tUserType\tDataType\tData
 * *****************************************************************/
static HRESULT DeleteMetabaseValue(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
    //AssertSz(FALSE, "debug DeleteMetabaseValue here");
    HRESULT hr = S_OK;

    METADATA_HANDLE mhKey = 0;

    LPWSTR pwzKey = NULL;
    DWORD dwIdentifier = 0;
    DWORD dwDataType = 0;

    int i;

    // get the key first
    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzKey);
    ExitOnFailure(hr, "failed to read key");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&dwIdentifier));
    ExitOnFailure(hr, "failed to read identifier");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&dwDataType));
    ExitOnFailure(hr, "failed to read data type");

    WcaLog(LOGMSG_VERBOSE, "Deleting Metabase Value Under Key: %ls ID: %d", pwzKey, dwIdentifier);

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open '%ls' key, retrying %d time(s)...", pwzKey, i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhKey);
    }
    MessageExitOnFailure1(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", pwzKey);

    if (lstrlenW(pwzKey) < 3)
    {
        ExitOnFailure1(hr = E_INVALIDARG, "Key didn't begin with \"/LM\" as expected - key value: %ls", pwzKey);
    }

    hr = piMetabase->DeleteData(mhKey, pwzKey + 3, dwIdentifier, dwDataType); // pwzKey + 3 to skip "/LM" that was used to open the key.
    if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        hr = S_OK;
    }
    MessageExitOnFailure2(hr, msierrIISFailedDeleteValue, "failed to delete value %d from metabase key: %ls", dwIdentifier, pwzKey);

    hr = WcaProgressMessage(COST_IIS_DELETEVALUE, FALSE);
LExit:
    ReleaseStr(pwzKey);

    if (mhKey)
        piMetabase->CloseKey(mhKey);

    return hr;
}


/********************************************************************
 DeleteAspApp - Deletes applications in IIS

  Input:  deferred CustomActionData - MetabaseRoot\tRecursive
 * *****************************************************************/
static HRESULT DeleteAspApp(__in LPWSTR* ppwzCustomActionData, __in IMSAdminBase* piMetabase, __in ICatalogCollection* pApplicationCollection, __in IWamAdmin* piWam)
{
    const int BUFFER_BYTES = 512;
    const BSTR bstrPropName = SysAllocString(L"Deleteable");

    HRESULT hr = S_OK;
    ICatalogObject* pApplication = NULL;

    LPWSTR pwzRoot = NULL;
    DWORD dwActualBufferSize = 0;
    long lSize = 0;
    long lIndex = 0;
    long lChanges = 0;

    VARIANT keyValue;
    ::VariantInit(&keyValue);
    VARIANT propValue;
    propValue.vt = VT_BOOL;
    propValue.boolVal = TRUE;

    METADATA_RECORD mr;
    // Get current set of web service extensions.
    ::ZeroMemory(&mr, sizeof(mr));
    mr.dwMDIdentifier = MD_APP_PACKAGE_ID;
    mr.dwMDAttributes = 0;
    mr.dwMDUserType  = ASP_MD_UT_APP;
    mr.dwMDDataType = STRING_METADATA;
    mr.pbMDData = new unsigned char[BUFFER_BYTES];
    mr.dwMDDataLen = BUFFER_BYTES;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzRoot); // MetabaseRoot
    ExitOnFailure(hr, "failed to get metabase root");

    hr = piMetabase->GetData(METADATA_MASTER_ROOT_HANDLE, pwzRoot, &mr, &dwActualBufferSize);
    if (HRESULT_FROM_WIN32(MD_ERROR_DATA_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        // This one doesn't have an independent app GUID associated with it - it may have been already partially deleted, or a low isolation app
        WcaLog(LOGMSG_VERBOSE, "No independent COM+ application found associated with %ls. It either doesn't exist, or was already removed - continuing", pwzRoot);
        ExitFunction1(hr = S_OK);
    }
    MessageExitOnFailure1(hr, msierrIISFailedDeleteApp, "failed to get GUID for application at path: %ls", pwzRoot);

    WcaLog(LOGMSG_VERBOSE, "Deleting ASP App (used query: %ls) with GUID: %ls", pwzRoot, (LPWSTR)(mr.pbMDData));

    // Delete the application association from IIS's point of view before it's obliterated from the application collection
    hr = piWam->AppDelete(pwzRoot, FALSE);
    if (FAILED(hr))
    {
        // This isn't necessarily an error if we fail here, but let's log a failure if it happens
        WcaLog(LOGMSG_VERBOSE, "error 0x%x: failed to call IWamAdmin::AppDelete() while removing web application - continuing");
        hr = S_OK;
    }

    if (!pApplicationCollection)
    {
        WcaLog(LOGMSG_STANDARD, "Could not remove application with GUID %ls because the application collection could not be found", (LPWSTR)(mr.pbMDData));
        ExitFunction1(hr = S_OK);
    }

    hr = pApplicationCollection->Populate();
    MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to populate Application collection");

    hr = pApplicationCollection->get_Count(&lSize);
    MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to get size of Application collection");
    WcaLog(LOGMSG_TRACEONLY, "Found %u items in application collection", lSize);

    // No need to check this too early, as we may not even need this to have successfully allocated
    ExitOnNull(bstrPropName, hr, E_OUTOFMEMORY, "failed to allocate memory for \"Deleteable\" string");

    for (lIndex = 0; lIndex < lSize; ++lIndex)
    {
        hr = pApplicationCollection->get_Item(lIndex, reinterpret_cast<IDispatch**>(&pApplication));
        MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to get COM+ application while enumerating through COM+ applications");

        hr = pApplication->get_Key(&keyValue);
        MessageExitOnFailure(hr, msierrIISFailedDeleteApp, "failed to get key of COM+ application while enumerating through COM+ applications");

        WcaLog(LOGMSG_TRACEONLY, "While enumerating through COM+ applications, found an application with GUID: %ls", (LPWSTR)keyValue.bstrVal);

        if (VT_BSTR == keyValue.vt && 0 == lstrcmpW((LPWSTR)keyValue.bstrVal, (LPWSTR)(mr.pbMDData)))
        {
            hr = pApplication->put_Value(bstrPropName, propValue);
            if (FAILED(hr))
            {
                // This isn't necessarily a critical error unless we fail to actually delete it in the next step
                WcaLog(LOGMSG_VERBOSE, "error 0x%x: failed to ensure COM+ application with guid %ls is deletable - continuing", hr, (LPWSTR)(mr.pbMDData));
            }

            hr = pApplicationCollection->SaveChanges(&lChanges);
            if (FAILED(hr))
            {
                // This isn't necessarily a critical error unless we fail to actually delete it in the next step
                WcaLog(LOGMSG_VERBOSE, "error 0x%x: failed to save changes while ensuring COM+ application with guid %ls is deletable - continuing", hr, (LPWSTR)(mr.pbMDData));
            }

            hr = pApplicationCollection->Remove(lIndex);
            if (FAILED(hr))
            {
                WcaLog(LOGMSG_STANDARD, "error 0x%x: failed to remove COM+ application with guid %ls. The COM application will not be removed", hr, (LPWSTR)(mr.pbMDData));
            }
            else
            {
                hr = pApplicationCollection->SaveChanges(&lChanges);
                if (FAILED(hr))
                {
                    WcaLog(LOGMSG_STANDARD, "error 0x%x: failed to save changes when removing COM+ application with guid %ls. The COM application will not be removed - continuing", hr, (LPWSTR)(mr.pbMDData));
                }
                else
                {
                    WcaLog(LOGMSG_VERBOSE, "Found and removed application with GUID %ls", (LPWSTR)(mr.pbMDData));
                }
            }

            // We've found the right key and successfully deleted the app - let's exit the loop now
            lIndex = lSize;
        }
    }
    // If we didn't find it, it isn't an error, because the application we want to delete doesn't seem to exist!

    hr = WcaProgressMessage(COST_IIS_DELETEAPP, FALSE);
LExit:
    ReleaseBSTR(bstrPropName);

    ReleaseStr(pwzRoot);
    // Don't release pApplication, because it points to an object within the collection

    delete [] mr.pbMDData;

    return hr;
}


/********************************************************************
 CreateAspApp - Creates applications in IIS

  Input:  deferred CustomActionData - MetabaseRoot\tInProc
 * ****************************************************************/
static HRESULT CreateAspApp(__in LPWSTR* ppwzCustomActionData, __in IWamAdmin* piWam)
{
    HRESULT hr = S_OK;

    LPWSTR pwzRoot = NULL;
    BOOL fInProc;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzRoot); // MetabaseRoot
    ExitOnFailure(hr, "failed to get metabase root");
    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, reinterpret_cast<int *>(&fInProc)); // InProc
    ExitOnFailure(hr, "failed to get in proc flag");

    WcaLog(LOGMSG_VERBOSE, "Creating ASP App: %ls", pwzRoot);

    hr = piWam->AppCreate(pwzRoot, fInProc);
    MessageExitOnFailure1(hr, msierrIISFailedCreateApp, "failed to create web application: %ls", pwzRoot);

    hr = WcaProgressMessage(COST_IIS_CREATEAPP, FALSE);
LExit:
    return hr;
}


/********************************************************************
 DeleteMetabaseKey - Deletes metabase keys

  Input:  deferred CustomActionData - Key
 ******************************************************************/
static HRESULT DeleteMetabaseKey(__in LPWSTR *ppwzCustomActionData, __in IMSAdminBase* piMetabase)
{
    HRESULT hr = S_OK;

    METADATA_HANDLE mhRoot = 0;

    LPWSTR pwzData = NULL;

    LPCWSTR pwzKey;
    int i;

    hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzData);
    ExitOnFailure(hr, "failed to read key to be deleted");

    hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    for (i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
    {
        ::Sleep(1000);
        WcaLog(LOGMSG_STANDARD, "failed to open root key, retrying %d time(s)...", i);
        hr = piMetabase->OpenKey(METADATA_MASTER_ROOT_HANDLE, L"/LM", METADATA_PERMISSION_WRITE, 10, &mhRoot);
    }
    MessageExitOnFailure1(hr, msierrIISFailedOpenKey, "failed to open metabase key: %ls", L"/LM");

    pwzKey = pwzData + 3;

    WcaLog(LOGMSG_VERBOSE, "Deleting Metabase Key: %ls", pwzKey);

    hr = piMetabase->DeleteKey(mhRoot, pwzKey);
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Key `%ls` did not exist, continuing.", pwzData);
        hr = S_OK;
    }
    MessageExitOnFailure1(hr, msierrIISFailedDeleteKey, "failed to delete metabase key: %ls", pwzData);

    hr = WcaProgressMessage(COST_IIS_DELETEKEY, FALSE);
LExit:
    if (mhRoot)
    {
        piMetabase->CloseKey(mhRoot);
    }

    return hr;
}


/********************************************************************
 WriteMetabaseChanges - CUSTOM ACTION ENTRY POINT for IIS Metabase changes

 *******************************************************************/
extern "C" UINT __stdcall WriteMetabaseChanges(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug WriteMetabaseChanges here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    IMSAdminBase* piMetabase = NULL;
    IWamAdmin* piWam = NULL;
    ICOMAdminCatalog* pCatalog = NULL;
    ICatalogCollection* pApplicationCollection = NULL;
    WCA_CASCRIPT_HANDLE hWriteMetabaseScript = NULL;
    BSTR bstrApplications = SysAllocString(L"Applications");
    BOOL fInitializedCom = FALSE;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzScriptKey = NULL;
    METABASE_ACTION maAction = MBA_UNKNOWNACTION;

    hr = WcaInitialize(hInstall, "WriteMetabaseChanges");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    // Get the CaScript key
    hr = WcaReadStringFromCaData(&pwzData, &pwzScriptKey);
    ExitOnFailure(hr, "Failed to get CaScript key from custom action data");

    hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_SCHEDULED, FALSE, pwzScriptKey, &hWriteMetabaseScript);
    ExitOnFailure(hr, "Failed to open CaScript file");

    // The rest of our existing custom action data string should be empty - go ahead and overwrite it
    ReleaseNullStr(pwzData);
    hr = WcaCaScriptReadAsCustomActionData(hWriteMetabaseScript, &pwzData);
    ExitOnFailure(hr, "Failed to read script into CustomAction data.");

    pwz = pwzData;

    while (S_OK == (hr = WcaReadIntegerFromCaData(&pwz, (int *)&maAction)))
    {
        switch (maAction)
        {
        case MBA_CREATEAPP:
            if (NULL == piWam)
            {
                hr = ::CoCreateInstance(CLSID_WamAdmin, NULL, CLSCTX_ALL, IID_IWamAdmin, reinterpret_cast<void**>(&piWam));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IWamAdmin object");
            }

            hr = CreateAspApp(&pwz, piWam);
            ExitOnFailure(hr, "failed to create ASP App");
            break;
        case MBA_DELETEAPP:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            if (NULL == pCatalog)
            {
                hr = CoCreateInstance(CLSID_COMAdminCatalog, NULL, CLSCTX_INPROC_SERVER, IID_IUnknown, (void**)&pCatalog);
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_ICOMAdmin object");

                hr = pCatalog->GetCollection(bstrApplications, reinterpret_cast<IDispatch**>(&pApplicationCollection));
                if (FAILED(hr))
                {
                    hr = S_OK;
                    WcaLog(LOGMSG_STANDARD, "error 0x%x: failed to get ApplicationCollection object for list of COM+ applications - COM+ applications will not be able to be uninstalled - continuing", hr);
                }
            }

            if (NULL == piWam)
            {
                hr = ::CoCreateInstance(CLSID_WamAdmin, NULL, CLSCTX_ALL, IID_IWamAdmin, reinterpret_cast<void**>(&piWam));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IWamAdmin object");
            }

            hr = DeleteAspApp(&pwz, piMetabase, pApplicationCollection, piWam);
            ExitOnFailure(hr, "failed to delete ASP App");
            break;
        case MBA_CREATEKEY:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = CreateMetabaseKey(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to create metabase key");
            break;
        case MBA_DELETEKEY:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = DeleteMetabaseKey(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to delete metabase key");
            break;
        case MBA_WRITEVALUE:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = WriteMetabaseValue(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to write metabase value");
            break;
        case MBA_DELETEVALUE:
            if (NULL == piMetabase)
            {
                hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, reinterpret_cast<void**>(&piMetabase));
                MessageExitOnFailure(hr, msierrIISCannotConnect, "failed to get IID_IIMSAdminBase object");
            }

            hr = DeleteMetabaseValue(&pwz, piMetabase);
            ExitOnFailure(hr, "failed to delete metabase value");
            break;
        default:
            ExitOnFailure1(hr = E_UNEXPECTED, "Unexpected metabase action specified: %d", maAction);
            break;
        }
    }
    if (E_NOMOREITEMS == hr) // If there are no more items, all is well
    {
        if (NULL != piMetabase)
        {
            hr = piMetabase->SaveData();
            for (int i = 30; i > 0 && HRESULT_FROM_WIN32(ERROR_PATH_BUSY) == hr; i--)
            {
                ::Sleep(1000);
                WcaLog(LOGMSG_VERBOSE, "Failed to force save of metabase data, retrying %d time(s)...", i);
                hr = piMetabase->SaveData();
            }
            if (FAILED(hr))
            {
                WcaLog(LOGMSG_VERBOSE, "Failed to force save of metabase data: 0x%x - continuing", hr);
            }
            hr = S_OK;
        }
        else
        {
            hr = S_OK;
        }
    }

LExit:
    WcaCaScriptClose(hWriteMetabaseScript, WCA_CASCRIPT_CLOSE_DELETE);

    ReleaseBSTR(bstrApplications);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzData);
    ReleaseObject(piMetabase);
    ReleaseObject(piWam);
    ReleaseObject(pCatalog);
    ReleaseObject(pApplicationCollection);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 * CreateDatabase - CUSTOM ACTION ENTRY POINT for creating databases
 *
 *  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword
 * ****************************************************************/
extern "C" UINT __stdcall CreateDatabase(MSIHANDLE hInstall)
{
//AssertSz(FALSE, "debug CreateDatabase here");
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    IDBCreateSession* pidbSession = NULL;
    BSTR bstrErrorDescription = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzDatabaseKey = NULL;
    LPWSTR pwzServer = NULL;
    LPWSTR pwzInstance = NULL;
    LPWSTR pwzDatabase = NULL;
    LPWSTR pwzTemp = NULL;
    int iAttributes;
    BOOL fIntegratedAuth;
    LPWSTR pwzUser = NULL;
    LPWSTR pwzPassword = NULL;
    BOOL fHaveDbFileSpec = FALSE;
    SQL_FILESPEC sfDb;
    BOOL fHaveLogFileSpec = FALSE;
    SQL_FILESPEC sfLog;
    BOOL fInitializedCom = FALSE;

    memset(&sfDb, 0, sizeof(sfDb));
    memset(&sfLog, 0, sizeof(sfLog));

    hr = WcaInitialize(hInstall, "CreateDatabase");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to intialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey); // SQL Server
    ExitOnFailure1(hr, "failed to read database key from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzServer); // SQL Server
    ExitOnFailure1(hr, "failed to read server from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzInstance); // SQL Server Instance
    ExitOnFailure1(hr, "failed to read server instance from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabase); // SQL Database
    ExitOnFailure1(hr, "failed to read server instance from custom action data: %ls", pwz);
    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure1(hr, "failed to read attributes from custom action data: %ls", pwz);
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth)); // Integrated Windows Authentication?
    ExitOnFailure1(hr, "failed to read integrated auth flag from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzUser); // SQL User
    ExitOnFailure1(hr, "failed to read user from custom action data: %ls", pwz);
    hr = WcaReadStringFromCaData(&pwz, &pwzPassword); // SQL User Password
    ExitOnFailure1(hr, "failed to read user from custom action data: %ls", pwz);

    // db file spec
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fHaveDbFileSpec));
    ExitOnFailure1(hr, "failed to read db file spec from custom action data: %ls", pwz);

    if (fHaveDbFileSpec)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read db file spec name from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzName, countof(sfDb.wzName), pwzTemp);
        ExitOnFailure1(hr, "failed to copy db file spec name: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read db file spec filename from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzFilename, countof(sfDb.wzFilename), pwzTemp);
        ExitOnFailure1(hr, "failed to copy db file spec filename: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read db file spec size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzSize, countof(sfDb.wzSize), pwzTemp);
        ExitOnFailure1(hr, "failed to copy db file spec size value: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read db file spec max size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzMaxSize, countof(sfDb.wzMaxSize), pwzTemp);
        ExitOnFailure1(hr, "failed to copy db file spec max size: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read db file spec grow from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfDb.wzGrow, countof(sfDb.wzGrow), pwzTemp);
        ExitOnFailure1(hr, "failed to copy db file spec grow value: %ls", pwzTemp);
    }

    // log file spec
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fHaveLogFileSpec));
    ExitOnFailure1(hr, "failed to read log file spec from custom action data: %ls", pwz);
    if (fHaveLogFileSpec)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read log file spec name from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzName, countof(sfDb.wzName), pwzTemp);
        ExitOnFailure1(hr, "failed to copy log file spec name: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read log file spec filename from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzFilename, countof(sfDb.wzFilename), pwzTemp);
        ExitOnFailure1(hr, "failed to copy log file spec filename: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read log file spec size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzSize, countof(sfDb.wzSize), pwzTemp);
        ExitOnFailure1(hr, "failed to copy log file spec size value: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read log file spec max size from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzMaxSize, countof(sfDb.wzMaxSize), pwzTemp);
        ExitOnFailure1(hr, "failed to copy log file spec max size: %ls", pwzTemp);

        hr = WcaReadStringFromCaData(&pwz, &pwzTemp);
        ExitOnFailure1(hr, "failed to read log file spec grow from custom action data: %ls", pwz);
        hr = ::StringCchCopyW(sfLog.wzGrow, countof(sfDb.wzGrow), pwzTemp);
        ExitOnFailure1(hr, "failed to copy log file spec grow value: %ls", pwzTemp);
    }

    if (iAttributes & SCADB_CONFIRM_OVERWRITE)
    {
        // Check if the database already exists
        hr = SqlDatabaseExists(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &bstrErrorDescription);
        MessageExitOnFailure2(hr, msierrSQLFailedCreateDatabase, "failed to check if database exists: '%ls', error: %ls", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

        if (S_OK == hr) // found an existing database, confirm that they don't want to stop before it gets trampled, in no UI case just continue anyways
        {
            hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            if (IDNO == WcaErrorMessage(msierrSQLDatabaseAlreadyExists, hr, MB_YESNO, 1, pwzDatabase))
                ExitOnFailure(hr, "failed to initialize");
        }
    }

    hr = SqlDatabaseEnsureExists(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, fHaveDbFileSpec ? &sfDb : NULL, fHaveLogFileSpec ? &sfLog : NULL, &bstrErrorDescription);
    if ((iAttributes & SCADB_CONTINUE_ON_ERROR) && FAILED(hr))
    {
        WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to create SQL database but continuing, error: %ls, Database: %ls", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzDatabase);
        hr = S_OK;
    }
    MessageExitOnFailure2(hr, msierrSQLFailedCreateDatabase, "failed to create to database: '%ls', error: %ls", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

    hr = WcaProgressMessage(COST_SQL_CONNECTDB, FALSE);
LExit:
    ReleaseStr(pwzDatabaseKey);
    ReleaseStr(pwzServer);
    ReleaseStr(pwzInstance);
    ReleaseStr(pwzDatabase);
    ReleaseStr(pwzUser);
    ReleaseStr(pwzPassword);
    ReleaseObject(pidbSession);
    ReleaseBSTR(bstrErrorDescription);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 DropDatabase - CUSTOM ACTION ENTRY POINT for removing databases

  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword
 * ****************************************************************/
extern "C" UINT __stdcall DropDatabase(MSIHANDLE hInstall)
{
//Assert(FALSE);
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    IDBCreateSession* pidbSession = NULL;
    BSTR bstrErrorDescription = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzDatabaseKey = NULL;
    LPWSTR pwzServer = NULL;
    LPWSTR pwzInstance = NULL;
    LPWSTR pwzDatabase = NULL;
    long lAttributes;
    BOOL fIntegratedAuth;
    LPWSTR pwzUser = NULL;
    LPWSTR pwzPassword = NULL;
    BOOL fInitializedCom = TRUE;

    hr = WcaInitialize(hInstall, "DropDatabase");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to intialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey);
    ExitOnFailure(hr, "failed to read database key");
    hr = WcaReadStringFromCaData(&pwz, &pwzServer);
    ExitOnFailure(hr, "failed to read server");
    hr = WcaReadStringFromCaData(&pwz, &pwzInstance);
    ExitOnFailure(hr, "failed to read instance");
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabase);
    ExitOnFailure(hr, "failed to read database");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&lAttributes));
    ExitOnFailure(hr, "failed to read attributes");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth)); // Integrated Windows Authentication?
    ExitOnFailure(hr, "failed to read integrated auth flag");
    hr = WcaReadStringFromCaData(&pwz, &pwzUser);
    ExitOnFailure(hr, "failed to read user");
    hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
    ExitOnFailure(hr, "failed to read password");

    hr = SqlDropDatabase(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &bstrErrorDescription);
    if ((lAttributes & SCADB_CONTINUE_ON_ERROR) && FAILED(hr))
    {
        WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to drop SQL database but continuing, error: %ls, Database: %ls", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzDatabase);
        hr = S_OK;
    }
    MessageExitOnFailure2(hr, msierrSQLFailedDropDatabase, "failed to drop to database: '%ls', error: %ls", pwzDatabase, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription);

    hr = WcaProgressMessage(COST_SQL_CONNECTDB, FALSE);

LExit:
    ReleaseStr(pwzDatabaseKey);
    ReleaseStr(pwzServer);
    ReleaseStr(pwzInstance);
    ReleaseStr(pwzDatabase);
    ReleaseStr(pwzUser);
    ReleaseStr(pwzPassword);
    ReleaseStr(pwzData);
    ReleaseObject(pidbSession);
    ReleaseBSTR(bstrErrorDescription);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 ExecuteSqlStrings - CUSTOM ACTION ENTRY POINT for running SQL strings

  Input:  deferred CustomActionData - DbKey\tServer\tInstance\tDatabase\tAttributes\tIntegratedAuth\tUser\tPassword\tSQLKey1\tSQLString1\tSQLKey2\tSQLString2\tSQLKey3\tSQLString3\t...
          rollback CustomActionData - same as above
 * ****************************************************************/
extern "C" UINT __stdcall ExecuteSqlStrings(MSIHANDLE hInstall)
{
//Assert(FALSE);
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;
    HRESULT hrDB = S_OK;

    LPWSTR pwzData = NULL;
    IDBCreateSession* pidbSession = NULL;
    BSTR bstrErrorDescription = NULL;

    LPWSTR pwz = NULL;
    LPWSTR pwzDatabaseKey = NULL;
    LPWSTR pwzServer = NULL;
    LPWSTR pwzInstance = NULL;
    LPWSTR pwzDatabase = NULL;
    int iAttributesDB;
    int iAttributesSQL;
    BOOL fIntegratedAuth;
    LPWSTR pwzUser = NULL;
    LPWSTR pwzPassword = NULL;
    LPWSTR pwzSqlKey = NULL;
    LPWSTR pwzSql = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "ExecuteSqlStrings");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to intialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabaseKey);
    ExitOnFailure(hr, "failed to read database key");
    hr = WcaReadStringFromCaData(&pwz, &pwzServer);
    ExitOnFailure(hr, "failed to read server");
    hr = WcaReadStringFromCaData(&pwz, &pwzInstance);
    ExitOnFailure(hr, "failed to read instance");
    hr = WcaReadStringFromCaData(&pwz, &pwzDatabase);
    ExitOnFailure(hr, "failed to read database");
    hr = WcaReadIntegerFromCaData(&pwz, &iAttributesDB);
    ExitOnFailure(hr, "failed to read attributes");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth)); // Integrated Windows Authentication?
    ExitOnFailure(hr, "failed to read integrated auth flag");
    hr = WcaReadStringFromCaData(&pwz, &pwzUser);
    ExitOnFailure(hr, "failed to read user");
    hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
    ExitOnFailure(hr, "failed to read password");

    // Store off the result of the connect, only exit if we don't care if the database connection succeeds
    // Wait to fail until later to see if we actually have work to do that is not set to continue on error
    hrDB = SqlConnectDatabase(pwzServer, pwzInstance, pwzDatabase, fIntegratedAuth, pwzUser, pwzPassword, &pidbSession);
    if ((iAttributesDB & SCADB_CONTINUE_ON_ERROR) && FAILED(hrDB))
    {
        WcaLog(LOGMSG_STANDARD, "Error 0x%x: continuing after failure to connect to database: %ls", hrDB, pwzDatabase);
        ExitFunction1(hr = S_OK);
    }

    while (S_OK == hr && S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzSqlKey)))
    {
        hr = WcaReadIntegerFromCaData(&pwz, &iAttributesSQL);
        ExitOnFailure1(hr, "failed to read attributes for SQL string: %ls", pwzSqlKey);

        hr = WcaReadStringFromCaData(&pwz, &pwzSql);
        ExitOnFailure1(hr, "failed to read SQL string for key: %ls", pwzSqlKey);

        // If the SqlString row is set to continue on error and the DB connection failed, skip attempting to execute
        if ((iAttributesSQL & SCASQL_CONTINUE_ON_ERROR) && FAILED(hrDB))
        {
            WcaLog(LOGMSG_STANDARD, "Error 0x%x: continuing after failure to connect to database: %ls", hrDB, pwzDatabase);
            continue;
        }

        // Now check if the DB connection succeeded
        MessageExitOnFailure1(hr = hrDB, msierrSQLFailedConnectDatabase, "failed to connect to database: '%ls'", pwzDatabase);

        WcaLog(LOGMSG_VERBOSE, "Executing SQL string: %ls", pwzSql);
        hr = SqlSessionExecuteQuery(pidbSession, pwzSql, NULL, NULL, &bstrErrorDescription);
        if ((iAttributesSQL & SCASQL_CONTINUE_ON_ERROR) && FAILED(hr))
        {
            WcaLog(LOGMSG_STANDARD, "Error 0x%x: failed to execute SQL string but continuing, error: %ls, SQL key: %ls SQL string: %ls", hr, NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzSqlKey, pwzSql);
            hr = S_OK;
        }
        MessageExitOnFailure3(hr, msierrSQLFailedExecString, "failed to execute SQL string, error: %ls, SQL key: %ls SQL string: %ls", NULL == bstrErrorDescription ? L"unknown error" : bstrErrorDescription, pwzSqlKey, pwzSql);

        WcaProgressMessage(COST_SQL_STRING, FALSE);
    }
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseStr(pwzDatabaseKey);
    ReleaseStr(pwzServer);
    ReleaseStr(pwzInstance);
    ReleaseStr(pwzDatabase);
    ReleaseStr(pwzUser);
    ReleaseStr(pwzPassword);
    ReleaseStr(pwzData);

    ReleaseBSTR(bstrErrorDescription);
    ReleaseObject(pidbSession);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
 * CreateSmb - CUSTOM ACTION ENTRY POINT for creating fileshares
 *
 * Input:  deferred CustomActionData -
 *    wzFsKey\twzShareDesc\twzFullPath\tfIntegratedAuth\twzUserName\tnPermissions\twzUserName\tnPermissions...
 *
 * ****************************************************************/
extern "C" UINT __stdcall CreateSmb(MSIHANDLE hInstall)
{
//AssertSz(0, "debug CreateSmb");
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzFsKey = NULL;
    LPWSTR pwzShareDesc = NULL;
    LPWSTR pwzDirectory = NULL;
    int iAccessMode = 0;
    DWORD nExPermissions = 0;
    BOOL fIntegratedAuth;
    LPWSTR pwzExUser = NULL;
    SCA_SMBP ssp = {0};
    DWORD dwExUserPerms = 0;
    DWORD dwCounter = 0;
    SCA_SMBP_USER_PERMS* pUserPermsList = NULL;

    hr = WcaInitialize(hInstall, "CreateSmb");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzFsKey); // share name
    ExitOnFailure(hr, "failed to read share name");
    hr = WcaReadStringFromCaData(&pwz, &pwzShareDesc); // share description
    ExitOnFailure(hr, "failed to read share name");
    hr = WcaReadStringFromCaData(&pwz, &pwzDirectory); // full path to share
    ExitOnFailure(hr, "failed to read share name");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth));
    ExitOnFailure(hr, "failed to read integrated authentication");

    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&dwExUserPerms));
    ExitOnFailure(hr, "failed to read count of permissions to set");
    if(dwExUserPerms > 0)
    {
        pUserPermsList = static_cast<SCA_SMBP_USER_PERMS*>(MemAlloc(sizeof(SCA_SMBP_USER_PERMS)*dwExUserPerms, TRUE));
        ExitOnNull(pUserPermsList, hr, E_OUTOFMEMORY, "failed to allocate memory for permissions structure");

        //Pull out all of the ExUserPerm strings
        for (dwCounter = 0; dwCounter < dwExUserPerms; ++dwCounter)
        {
            hr = WcaReadStringFromCaData(&pwz, &pwzExUser); // user account
            ExitOnFailure(hr, "failed to read user account");
            pUserPermsList[dwCounter].wzUser = pwzExUser;
            pwzExUser = NULL;

            hr = WcaReadIntegerFromCaData(&pwz, &iAccessMode);
            ExitOnFailure(hr, "failed to read access mode");
            pUserPermsList[dwCounter].accessMode = (ACCESS_MODE)iAccessMode;
            iAccessMode = 0;

            hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&nExPermissions));
            ExitOnFailure(hr, "failed to read count of permissions");
            pUserPermsList[dwCounter].nPermissions = nExPermissions;
            nExPermissions = 0;
        }
    }

    ssp.wzKey = pwzFsKey;
    ssp.wzDescription = pwzShareDesc;
    ssp.wzDirectory = pwzDirectory;
    ssp.fUseIntegratedAuth = fIntegratedAuth;
    ssp.dwUserPermissionCount = dwExUserPerms;
    ssp.pUserPerms = pUserPermsList;

    hr = ScaEnsureSmbExists(&ssp);
    MessageExitOnFailure1(hr, msierrSMBFailedCreate, "failed to create share: '%ls'", pwzFsKey);

    hr = WcaProgressMessage(COST_SMB_CREATESMB, FALSE);

LExit:
    ReleaseStr(pwzFsKey);
    ReleaseStr(pwzShareDesc);
    ReleaseStr(pwzDirectory);
    ReleaseStr(pwzData);

    if (pUserPermsList)
    {
        MemFree(pUserPermsList);
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}



/********************************************************************
 DropSmb - CUSTOM ACTION ENTRY POINT for creating fileshares

 Input:  deferred CustomActionData - wzFsKey\twzShareDesc\twzFullPath\tnPermissions\tfIntegratedAuth\twzUserName\twzPassword

 * ****************************************************************/
extern "C" UINT __stdcall DropSmb(MSIHANDLE hInstall)
{
    //AssertSz(0, "debug DropSmb");
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzFsKey = NULL;
    SCA_SMBP ssp = {0};

    hr = WcaInitialize(hInstall, "DropSmb");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzFsKey); // share name
    ExitOnFailure(hr, "failed to read share name");

    ssp.wzKey = pwzFsKey;

    hr = ScaDropSmb(&ssp);
    MessageExitOnFailure1(hr, msierrSMBFailedDrop, "failed to delete share: '%ls'", pwzFsKey);

    hr = WcaProgressMessage(COST_SMB_DROPSMB, FALSE);

LExit:
    ReleaseStr(pwzFsKey);
    ReleaseStr(pwzData);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


static HRESULT AddUserToGroup(
    __in LPWSTR wzUser,
    __in LPCWSTR wzUserDomain,
    __in LPCWSTR wzGroup,
    __in LPCWSTR wzGroupDomain
    )
{
    Assert(wzUser && *wzUser && wzUserDomain && wzGroup && *wzGroup && wzGroupDomain);

    HRESULT hr = S_OK;
    IADsGroup *pGroup = NULL;
    BSTR bstrUser = NULL;
    BSTR bstrGroup = NULL;
    LPCWSTR wz = NULL;
    LPWSTR pwzUser = NULL;
    LOCALGROUP_MEMBERS_INFO_3 lgmi;

    if (*wzGroupDomain)
    {
        wz = wzGroupDomain;
    }

    // Try adding it to the global group first
    UINT ui = ::NetGroupAddUser(wz, wzGroup, wzUser);
    if (NERR_GroupNotFound == ui)
    {
        // Try adding it to the local group
        if (wzUserDomain)
        {
            hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzUserDomain, wzUser);
            ExitOnFailure(hr, "failed to allocate user domain string");
        }

        lgmi.lgrmi3_domainandname = (NULL == pwzUser ? wzUser : pwzUser);
        ui = ::NetLocalGroupAddMembers(wz, wzGroup, 3 , reinterpret_cast<LPBYTE>(&lgmi), 1);
    }
    hr = HRESULT_FROM_WIN32(ui);
    if (HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr) // if they're already a member of the group don't report an error
        hr = S_OK;

    //
    // If we failed, try active directory
    //
    if (FAILED(hr))
    {
        WcaLog(LOGMSG_VERBOSE, "Failed to add user: %ls, domain %ls to group: %ls, domain: %ls with error 0x%x.  Attempting to use Active Directory", wzUser, wzUserDomain, wzGroup, wzGroupDomain, hr);

        hr = UserCreateADsPath(wzUserDomain, wzUser, &bstrUser);
        ExitOnFailure2(hr, "failed to create user ADsPath for user: %ls domain: %ls", wzUser, wzUserDomain);

        hr = UserCreateADsPath(wzGroupDomain, wzGroup, &bstrGroup);
        ExitOnFailure2(hr, "failed to create group ADsPath for group: %ls domain: %ls", wzGroup, wzGroupDomain);

        hr = ::ADsGetObject(bstrGroup,IID_IADsGroup, reinterpret_cast<void**>(&pGroup));
        ExitOnFailure1(hr, "Failed to get group '%ls'.", reinterpret_cast<WCHAR*>(bstrGroup) );

        hr = pGroup->Add(bstrUser);
        if ((HRESULT_FROM_WIN32(ERROR_OBJECT_ALREADY_EXISTS) == hr) || (HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr))
            hr = S_OK;

        ExitOnFailure2(hr, "Failed to add user %ls to group '%ls'.", reinterpret_cast<WCHAR*>(bstrUser), reinterpret_cast<WCHAR*>(bstrGroup) );
    }

LExit:
    ReleaseObject(pGroup);
    ReleaseBSTR(bstrUser);
    ReleaseBSTR(bstrGroup);

    return hr;
}

static HRESULT RemoveUserFromGroup(
    __in LPWSTR wzUser,
    __in LPCWSTR wzUserDomain,
    __in LPCWSTR wzGroup,
    __in LPCWSTR wzGroupDomain
    )
{
    Assert(wzUser && *wzUser && wzUserDomain && wzGroup && *wzGroup && wzGroupDomain);

    HRESULT hr = S_OK;
    IADsGroup *pGroup = NULL;
    BSTR bstrUser = NULL;
    BSTR bstrGroup = NULL;
    LPCWSTR wz = NULL;
    LPWSTR pwzUser = NULL;
    LOCALGROUP_MEMBERS_INFO_3 lgmi;

    if (*wzGroupDomain)
    {
        wz = wzGroupDomain;
    }

    // Try removing it from the global group first
    UINT ui = ::NetGroupDelUser(wz, wzGroup, wzUser);
    if (NERR_GroupNotFound == ui)
    {
        // Try removing it from the local group
        if (wzUserDomain)
        {
            hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzUserDomain, wzUser);
            ExitOnFailure(hr, "failed to allocate user domain string");
        }

        lgmi.lgrmi3_domainandname = (NULL == pwzUser ? wzUser : pwzUser);
        ui = ::NetLocalGroupDelMembers(wz, wzGroup, 3 , reinterpret_cast<LPBYTE>(&lgmi), 1);
    }
    hr = HRESULT_FROM_WIN32(ui);

    //
    // If we failed, try active directory
    //
    if (FAILED(hr))
    {
        WcaLog(LOGMSG_VERBOSE, "Failed to remove user: %ls, domain %ls from group: %ls, domain: %ls with error 0x%x.  Attempting to use Active Directory", wzUser, wzUserDomain, wzGroup, wzGroupDomain, hr);

        hr = UserCreateADsPath(wzUserDomain, wzUser, &bstrUser);
        ExitOnFailure2(hr, "failed to create user ADsPath in order to remove user: %ls domain: %ls from a group", wzUser, wzUserDomain);

        hr = UserCreateADsPath(wzGroupDomain, wzGroup, &bstrGroup);
        ExitOnFailure2(hr, "failed to create group ADsPath in order to remove user from group: %ls domain: %ls", wzGroup, wzGroupDomain);

        hr = ::ADsGetObject(bstrGroup,IID_IADsGroup, reinterpret_cast<void**>(&pGroup));
        ExitOnFailure1(hr, "Failed to get group '%ls'.", reinterpret_cast<WCHAR*>(bstrGroup) );

        hr = pGroup->Remove(bstrUser);
        ExitOnFailure2(hr, "Failed to remove user %ls from group '%ls'.", reinterpret_cast<WCHAR*>(bstrUser), reinterpret_cast<WCHAR*>(bstrGroup) );
    }

LExit:
    ReleaseObject(pGroup);
    ReleaseBSTR(bstrUser);
    ReleaseBSTR(bstrGroup);

    return hr;
}


static HRESULT ModifyUserLocalServiceRight(
    __in_opt LPCWSTR wzDomain,
    __in LPCWSTR wzName,
    __in BOOL fAdd
    )
{
    HRESULT hr = S_OK;
    NTSTATUS nt = 0;

    LPWSTR pwzUser = NULL;
    PSID psid = NULL;
    LSA_HANDLE hPolicy = NULL;
    LSA_OBJECT_ATTRIBUTES ObjectAttributes = { 0 };
    LSA_UNICODE_STRING lucPrivilege = { 0 };

    if (wzDomain && *wzDomain)
    {
        hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzDomain, wzName);
        ExitOnFailure(hr, "Failed to allocate user with domain string");
    }
    else
    {
        hr = StrAllocString(&pwzUser, wzName, 0);
        ExitOnFailure(hr, "Failed to allocate string from user name.");
    }

    hr = AclGetAccountSid(NULL, pwzUser, &psid);
    ExitOnFailure1(hr, "Failed to get SID for user: %ls", pwzUser);

    nt = ::LsaOpenPolicy(NULL, &ObjectAttributes, POLICY_ALL_ACCESS, &hPolicy);
    hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
    ExitOnFailure(hr, "Failed to open LSA policy store.");

    lucPrivilege.Buffer = L"SeServiceLogonRight";
    lucPrivilege.Length = static_cast<USHORT>(lstrlenW(lucPrivilege.Buffer) * sizeof(WCHAR));
    lucPrivilege.MaximumLength = (lucPrivilege.Length + 1) * sizeof(WCHAR);

    if (fAdd)
    {
        nt = ::LsaAddAccountRights(hPolicy, psid, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure1(hr, "Failed to add 'logon as service' bit to user: %ls", pwzUser);
    }
    else
    {
        nt = ::LsaRemoveAccountRights(hPolicy, psid, FALSE, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure1(hr, "Failed to remove 'logon as service' bit from user: %ls", pwzUser);
    }

LExit:
    if (hPolicy)
    {
        ::LsaClose(hPolicy);
    }

    ReleaseSid(psid);
    ReleaseStr(pwzUser);
    return hr;
}


static HRESULT ModifyUserLocalBatchRight(
  __in_opt LPCWSTR wzDomain,
  __in LPCWSTR wzName,
  __in BOOL fAdd
  )
{
    HRESULT hr = S_OK;
    NTSTATUS nt = 0;

    LPWSTR pwzUser = NULL;
    PSID psid = NULL;
    LSA_HANDLE hPolicy = NULL;
    LSA_OBJECT_ATTRIBUTES ObjectAttributes = { 0 };
    LSA_UNICODE_STRING lucPrivilege = { 0 };

    if (wzDomain && *wzDomain)
    {
        hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzDomain, wzName);
        ExitOnFailure(hr, "Failed to allocate user with domain string");
    }
    else
    {
        hr = StrAllocString(&pwzUser, wzName, 0);
        ExitOnFailure(hr, "Failed to allocate string from user name.");
    }

    hr = AclGetAccountSid(NULL, pwzUser, &psid);
    ExitOnFailure1(hr, "Failed to get SID for user: %ls", pwzUser);

    nt = ::LsaOpenPolicy(NULL, &ObjectAttributes, POLICY_ALL_ACCESS, &hPolicy);
    hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
    ExitOnFailure(hr, "Failed to open LSA policy store.");

    lucPrivilege.Buffer = L"SeBatchLogonRight";
    lucPrivilege.Length = static_cast<USHORT>(lstrlenW(lucPrivilege.Buffer) * sizeof(WCHAR));
    lucPrivilege.MaximumLength = (lucPrivilege.Length + 1) * sizeof(WCHAR);

    if (fAdd)
    {
        nt = ::LsaAddAccountRights(hPolicy, psid, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure1(hr, "Failed to add 'logon as batch job' bit to user: %ls", pwzUser);
    }
    else
    {
        nt = ::LsaRemoveAccountRights(hPolicy, psid, FALSE, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure1(hr, "Failed to remove 'logon as batch job' bit from user: %ls", pwzUser);
    }

  LExit:
    if (hPolicy)
    {
        ::LsaClose(hPolicy);
    }

    ReleaseSid(psid);
    ReleaseStr(pwzUser);
    return hr;
}

static void SetUserPasswordAndAttributes(
    __in USER_INFO_1* puserInfo,
    __in LPWSTR wzPassword,
    __in int iAttributes
    )
{
    Assert(puserInfo);

    // Set the User's password
    puserInfo->usri1_password = wzPassword;

    // Apply the Attributes
    if (SCAU_DONT_EXPIRE_PASSWRD & iAttributes)
    {
        puserInfo->usri1_flags |= UF_DONT_EXPIRE_PASSWD;
    }
    else
    {
        puserInfo->usri1_flags &= ~UF_DONT_EXPIRE_PASSWD;
    }

    if (SCAU_PASSWD_CANT_CHANGE & iAttributes)
    {
        puserInfo->usri1_flags |= UF_PASSWD_CANT_CHANGE;
    }
    else
    {
        puserInfo->usri1_flags &= ~UF_PASSWD_CANT_CHANGE;
    }

    if (SCAU_DISABLE_ACCOUNT & iAttributes)
    {
        puserInfo->usri1_flags |= UF_ACCOUNTDISABLE;
    }
    else
    {
        puserInfo->usri1_flags &= ~UF_ACCOUNTDISABLE;
    }

    if (SCAU_PASSWD_CHANGE_REQD_ON_LOGIN & iAttributes) // TODO: for some reason this doesn't work
    {
        puserInfo->usri1_flags |= UF_PASSWORD_EXPIRED;
    }
    else
    {
        puserInfo->usri1_flags &= ~UF_PASSWORD_EXPIRED;
    }
}


/********************************************************************
 CreateUser - CUSTOM ACTION ENTRY POINT for creating users

  Input:  deferred CustomActionData - UserName\tDomain\tPassword\tAttributes\tGroupName\tDomain\tGroupName\tDomain...
 * *****************************************************************/
extern "C" UINT __stdcall CreateUser(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(0, "Debug CreateUser");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzPassword = NULL;
    LPWSTR pwzGroup = NULL;
    LPWSTR pwzGroupDomain = NULL;
    PDOMAIN_CONTROLLER_INFOW pDomainControllerInfo = NULL;
    int iAttributes = 0;
    BOOL fInitializedCom = FALSE;

    USER_INFO_1 userInfo;
    USER_INFO_1* puserInfo = NULL;
    DWORD dw;
    LPCWSTR wz = NULL;

    hr = WcaInitialize(hInstall, "CreateUser");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read user name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
    ExitOnFailure(hr, "failed to read password from custom action data");

    if (!(SCAU_DONT_CREATE_USER & iAttributes))
    {
        ::ZeroMemory(&userInfo, sizeof(USER_INFO_1));
        userInfo.usri1_name = pwzName;
        userInfo.usri1_priv = USER_PRIV_USER;
        userInfo.usri1_flags = UF_SCRIPT;
        userInfo.usri1_home_dir = NULL;
        userInfo.usri1_comment = NULL;
        userInfo.usri1_script_path = NULL;

        SetUserPasswordAndAttributes(&userInfo, pwzPassword, iAttributes);

        //
        // Create the User
        //
        if (pwzDomain && *pwzDomain)
        {
            er = ::DsGetDcNameW( NULL, (LPCWSTR)pwzDomain, NULL, NULL, NULL, &pDomainControllerInfo );
            if (RPC_S_SERVER_UNAVAILABLE == er)
            {
                // MSDN says, if we get the above error code, try again with the "DS_FORCE_REDISCOVERY" flag
                er = ::DsGetDcNameW( NULL, (LPCWSTR)pwzDomain, NULL, NULL, DS_FORCE_REDISCOVERY, &pDomainControllerInfo );
            }
            if (ERROR_SUCCESS == er)
            {
                wz = pDomainControllerInfo->DomainControllerName + 2;  //Add 2 so that we don't get the \\ prefix
            }
            else
            {
                wz = pwzDomain;
            }
        }

        er = ::NetUserAdd(wz, 1, reinterpret_cast<LPBYTE>(&userInfo), &dw);
        if (NERR_UserExists == er)
        {
            if (SCAU_UPDATE_IF_EXISTS & iAttributes)
            {
                er = ::NetUserGetInfo(wz, pwzName, 1, reinterpret_cast<LPBYTE*>(&puserInfo));
                if (NERR_Success == er)
                {
                    // Change the existing user's password and attributes again then try
                    // to update user with this new data
                    SetUserPasswordAndAttributes(puserInfo, pwzPassword, iAttributes);

                    er = ::NetUserSetInfo(wz, pwzName, 1, reinterpret_cast<LPBYTE>(puserInfo), &dw);
                }
            }
            else if (!(SCAU_FAIL_IF_EXISTS & iAttributes))
            {
                er = NERR_Success;
            }
        }
        else if (NERR_PasswordTooShort == er || NERR_PasswordTooLong == er)
        {
            MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(er), msierrUSRFailedUserCreatePswd, "failed to create user: %ls due to invalid password.", pwzName);
        }
        MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(er), msierrUSRFailedUserCreate, "failed to create user: %ls", pwzName);
    }

    if (SCAU_ALLOW_LOGON_AS_SERVICE & iAttributes)
    {
        hr = ModifyUserLocalServiceRight(pwzDomain, pwzName, TRUE);
        MessageExitOnFailure1(hr, msierrUSRFailedGrantLogonAsService, "Failed to grant logon as service rights to user: %ls", pwzName);
    }

    if (SCAU_ALLOW_LOGON_AS_BATCH & iAttributes)
    {
        hr = ModifyUserLocalBatchRight(pwzDomain, pwzName, TRUE);
        MessageExitOnFailure1(hr, msierrUSRFailedGrantLogonAsService, "Failed to grant logon as batch job rights to user: %ls", pwzName);
    }

    //
    // Add the users to groups
    //
    while (S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzGroup)))
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzGroupDomain);
        ExitOnFailure1(hr, "failed to get domain for group: %ls", pwzGroup);

        hr = AddUserToGroup(pwzName, pwzDomain, pwzGroup, pwzGroupDomain);
        MessageExitOnFailure2(hr, msierrUSRFailedUserGroupAdd, "failed to add user: %ls to group %ls", pwzName, pwzGroup);
    }
    if (E_NOMOREITEMS == hr) // if there are no more items, all is well
    {
        hr = S_OK;
    }
    ExitOnFailure1(hr, "failed to get next group in which to include user:%ls", pwzName);

LExit:
    if (puserInfo)
    {
        ::NetApiBufferFree((LPVOID)puserInfo);
    }

    if (pDomainControllerInfo)
    {
        ::NetApiBufferFree((LPVOID)pDomainControllerInfo);
    }

    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzPassword);
    ReleaseStr(pwzGroup);
    ReleaseStr(pwzGroupDomain);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (SCAU_NON_VITAL & iAttributes)
    {
        er = ERROR_SUCCESS;
    }
    else if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 RemoveUser - CUSTOM ACTION ENTRY POINT for removing users

  Input:  deferred CustomActionData - Name\tDomain
 * *****************************************************************/
extern "C" UINT __stdcall RemoveUser(
    MSIHANDLE hInstall
    )
{
    //AssertSz(0, "Debug RemoveAccount");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain= NULL;
    LPWSTR pwzGroup = NULL;
    LPWSTR pwzGroupDomain = NULL;
    int iAttributes = 0;
    LPCWSTR wz = NULL;
    PDOMAIN_CONTROLLER_INFOW pDomainControllerInfo = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "RemoveUser");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    //
    // Remove the logon as service privilege.
    //
    if (SCAU_ALLOW_LOGON_AS_SERVICE & iAttributes)
    {
        hr = ModifyUserLocalServiceRight(pwzDomain, pwzName, FALSE);
        if (FAILED(hr))
        {
            WcaLogError(hr, "Failed to remove logon as service right from user, continuing...");
            hr = S_OK;
        }
    }

    if (SCAU_ALLOW_LOGON_AS_BATCH & iAttributes)
    {
        hr = ModifyUserLocalBatchRight(pwzDomain, pwzName, FALSE);
        if (FAILED(hr))
        {
            WcaLogError(hr, "Failed to remove logon as batch job right from user, continuing...");
            hr = S_OK;
        }
    }

    //
    // Remove the User Account if the user was created by us.
    //
    if (!(SCAU_DONT_CREATE_USER & iAttributes))
    {
        if (pwzDomain && *pwzDomain)
        {
            er = ::DsGetDcNameW( NULL, (LPCWSTR)pwzDomain, NULL, NULL, NULL, &pDomainControllerInfo );
            if (RPC_S_SERVER_UNAVAILABLE == er)
            {
                // MSDN says, if we get the above error code, try again with the "DS_FORCE_REDISCOVERY" flag
                er = ::DsGetDcNameW( NULL, (LPCWSTR)pwzDomain, NULL, NULL, DS_FORCE_REDISCOVERY, &pDomainControllerInfo );
            }
            if (ERROR_SUCCESS == er)
            {
                wz = pDomainControllerInfo->DomainControllerName + 2;  //Add 2 so that we don't get the \\ prefix
            }
            else
            {
                wz = pwzDomain;
            }
        }

        er = ::NetUserDel(wz, pwzName);
        if (NERR_UserNotFound == er)
        {
            er = NERR_Success;
        }
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to delete user account: %ls", pwzName);
    }
    else
    {
        //
        // Remove the user from the groups
        //
        while (S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzGroup)))
        {
            hr = WcaReadStringFromCaData(&pwz, &pwzGroupDomain);

            if (FAILED(hr))
            {
                WcaLogError(hr, "failed to get domain for group: %ls, continuing anyway.", pwzGroup);
            }
            else
            {
                hr = RemoveUserFromGroup(pwzName, pwzDomain, pwzGroup, pwzGroupDomain);
                if (FAILED(hr))
                {
                    WcaLogError(hr, "failed to remove user: %ls from group %ls, continuing anyway.", pwzName, pwzGroup);
                }
            }
        }

        if (E_NOMOREITEMS == hr) // if there are no more items, all is well
        {
            hr = S_OK;
        }

        ExitOnFailure1(hr, "failed to get next group from which to remove user:%ls", pwzName);
    }

LExit:
    if (pDomainControllerInfo)
    {
        ::NetApiBufferFree(static_cast<LPVOID>(pDomainControllerInfo));
    }

    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}
/********************************************************************
 WriteIIS7ConfigChanges - CUSTOM ACTION ENTRY POINT for IIS7 config changes

 *******************************************************************/
extern "C" UINT __stdcall WriteIIS7ConfigChanges(MSIHANDLE hInstall)
{
    //AssertSz(FALSE, "debug WriteIIS7ConfigChanges here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzHashString = NULL;
    BYTE rgbActualHash[SHA1_HASH_LEN] = { };
    DWORD dwHashedBytes = SHA1_HASH_LEN;

    WCA_CASCRIPT_HANDLE hWriteIis7Script = NULL;

    hr = WcaInitialize(hInstall, "WriteIIS7ConfigChanges");
    ExitOnFailure(hr, "Failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzScriptKey);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    WcaLog(LOGMSG_TRACEONLY, "Script WriteIIS7ConfigChanges: %ls", pwzScriptKey);

    hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_SCHEDULED, FALSE, pwzScriptKey, &hWriteIis7Script);
    ExitOnFailure(hr, "Failed to open CaScript file");

    hr = WcaCaScriptReadAsCustomActionData(hWriteIis7Script, &pwzData);
    ExitOnFailure(hr, "Failed to read script into CustomAction data.");

    hr = CrypHashBuffer((BYTE*)pwzData, sizeof(pwzData) * sizeof(WCHAR), PROV_RSA_AES, CALG_SHA1, rgbActualHash, dwHashedBytes);
    ExitOnFailure(hr, "Failed to calculate hash of CustomAction data.");

    hr = StrAlloc(&pwzHashString, ((dwHashedBytes * 2) + 1));
    ExitOnFailure(hr, "Failed to allocate string for script hash");

    hr = StrHexEncode(rgbActualHash, dwHashedBytes, pwzHashString, ((dwHashedBytes * 2) + 1));
    ExitOnFailure(hr, "Failed to convert hash bytes to string.");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData WriteIIS7ConfigChanges: %ls", pwzData);
    WcaLog(LOGMSG_VERBOSE,  "Custom action data hash: %ls", pwzHashString);
    WcaLog(LOGMSG_VERBOSE, "CustomActionData WriteIIS7ConfigChanges length: %d", wcslen(pwzData));

    hr = IIS7ConfigChanges(hInstall, pwzData);
    ExitOnFailure(hr, "WriteIIS7ConfigChanges Failed.");

LExit:
    WcaCaScriptClose(hWriteIis7Script, WCA_CASCRIPT_CLOSE_DELETE);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzData);
    ReleaseStr(pwzHashString);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 CommitIIS7ConfigTransaction - CUSTOM ACTION ENTRY POINT for unbacking up config

  Input:  deferred CustomActionData - BackupName
 * *****************************************************************/
extern "C" UINT __stdcall CommitIIS7ConfigTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    WCHAR wzConfigCopy[MAX_PATH];
    DWORD dwSize = 0;

    BOOL fIsWow64Process = FALSE;
    BOOL fIsFSRedirectDisabled = FALSE;

    hr = WcaInitialize(hInstall, "CommitIIS7ConfigTransaction");
    ExitOnFailure(hr, "failed to initialize IIS7 commit transaction");

    WcaInitializeWow64();
    fIsWow64Process = WcaIsWow64Process();
    if (fIsWow64Process)
    {
        hr = WcaDisableWow64FSRedirection();
        if(FAILED(hr))
        {
            //eat this error
            hr = S_OK;
        }
        else
        {
            fIsFSRedirectDisabled = TRUE;
        }
    }

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    // Config AdminMgr changes already committed, just
    // delete backup config file.

    dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigCopy,
                                      MAX_PATH
                                      );
    if ( dwSize == 0 )
    {
        ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
    }

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, L".");
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of .");

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, pwzData);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of extension");

    if (!::DeleteFileW(wzConfigCopy))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr ||
            HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to delete backup applicationHost, not found - continuing");
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "failed to delete config backup");
        }
    }

LExit:
    ReleaseStr(pwzData);

    // Make sure we revert FS Redirection if necessary before exiting
    if (fIsFSRedirectDisabled)
    {
        fIsFSRedirectDisabled = FALSE;
        WcaRevertWow64FSRedirection();
    }
    WcaFinalizeWow64();


    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
/********************************************************************
 StartIIS7Config Transaction - CUSTOM ACTION ENTRY POINT for backing up config

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall StartIIS7ConfigTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    WCHAR wzConfigSource[MAX_PATH];
    WCHAR wzConfigCopy[MAX_PATH];
    DWORD dwSize = 0;


    BOOL fIsWow64Process = FALSE;
    BOOL fIsFSRedirectDisabled = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "StartIIS7ConfigTransaction");
    ExitOnFailure(hr, "failed to initialize StartIIS7ConfigTransaction");

    WcaInitializeWow64();
    fIsWow64Process = WcaIsWow64Process();
    if (fIsWow64Process)
    {
        hr = WcaDisableWow64FSRedirection();
        if(FAILED(hr))
        {
            //eat this error
            hr = S_OK;
        }
        else
        {
            fIsFSRedirectDisabled = TRUE;
        }
    }

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");


    dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource,
                                      MAX_PATH
                                      );
    if ( dwSize == 0 )
    {
        ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
    }
    hr = ::StringCchCopyW(wzConfigCopy, MAX_PATH, wzConfigSource);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCopyW");

    //add ca action as extension

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, L".");
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of .");

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, pwzData);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of extension");

    if ( !::CopyFileW(wzConfigSource, wzConfigCopy, FALSE) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr ||
            HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        {
            // IIS may not be installed on the machine, we'll fail later if we try to install anything
            WcaLog(LOGMSG_STANDARD, "Failed to back up applicationHost, not found - continuing");
            hr = S_OK;
        }
        else
        {
            ExitOnFailure2(hr, "Failed to copy config backup %ls -> %ls", wzConfigSource, wzConfigCopy);
        }
    }


    hr = WcaProgressMessage(COST_IIS_TRANSACTIONS, FALSE);


LExit:

    ReleaseStr(pwzData);

    // Make sure we revert FS Redirection if necessary before exiting
    if (fIsFSRedirectDisabled)
    {
        fIsFSRedirectDisabled = FALSE;
        WcaRevertWow64FSRedirection();
    }
    WcaFinalizeWow64();

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 RollbackIIS7ConfigTransaction - CUSTOM ACTION ENTRY POINT for unbacking up config

  Input:  deferred CustomActionData - BackupName
********************************************************************/
extern "C" UINT __stdcall RollbackIIS7ConfigTransaction(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzData = NULL;
    WCHAR wzConfigSource[MAX_PATH];
    WCHAR wzConfigCopy[MAX_PATH];
    DWORD dwSize = 0;

    BOOL fIsWow64Process = FALSE;
    BOOL fIsFSRedirectDisabled = FALSE;

    hr = WcaInitialize(hInstall, "RollbackIIS7ConfigTransaction");
    ExitOnFailure(hr, "failed to initialize");

    WcaInitializeWow64();
    fIsWow64Process = WcaIsWow64Process();
    if (fIsWow64Process)
    {
        hr = WcaDisableWow64FSRedirection();
        if(FAILED(hr))
        {
            //eat this error
            hr = S_OK;
        }
        else
        {
            fIsFSRedirectDisabled = TRUE;
        }
    }

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource,
                                      MAX_PATH
                                      );
    if ( dwSize == 0 )
    {
        ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
    }
    hr = ::StringCchCopyW(wzConfigCopy, MAX_PATH, wzConfigSource);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCopyW");

    //add ca action as extension

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, L".");
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of .");

    hr = ::StringCchCatW(wzConfigCopy, MAX_PATH, pwzData);
    ExitOnFailure(hr, "Commit IIS7 failed StringCchCatW of extension");

    //copy is reverse of start transaction
    if (!::CopyFileW(wzConfigCopy, wzConfigSource, FALSE))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr ||
            HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to restore applicationHost, not found - continuing");
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "failed to restore config backup");
        }
    }

    if (!::DeleteFileW(wzConfigCopy))
    {
        ExitWithLastError(hr, "failed to delete config backup");
    }

    hr = WcaProgressMessage(COST_IIS_TRANSACTIONS, FALSE);

LExit:
    ReleaseStr(pwzData);

    // Make sure we revert FS Redirection if necessary before exiting
    if (fIsFSRedirectDisabled)
    {
        fIsFSRedirectDisabled = FALSE;
        WcaRevertWow64FSRedirection();
    }
    WcaFinalizeWow64();

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
