//-------------------------------------------------------------------------------------------------
// <copyright file="secureobj.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Code to secure objects in custom actions when the installer cannot.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// structs
LPCWSTR wzQUERY_SECUREOBJECTS = L"SELECT `SecureObjects`.`SecureObject`, `SecureObjects`.`Table`, `SecureObjects`.`Domain`, `SecureObjects`.`User`, "
                                L"`SecureObjects`.`Permission`, `SecureObjects`.`Component_`, `Component`.`Attributes` FROM `SecureObjects`,`Component` WHERE "
                                L"`SecureObjects`.`Component_`=`Component`.`Component`";
enum eQUERY_SECUREOBJECTS { QSO_SECUREOBJECT = 1, QSO_TABLE, QSO_DOMAIN, QSO_USER, QSO_PERMISSION, QSO_COMPONENT, QSO_COMPATTRIBUTES };

LPCWSTR wzQUERY_REGISTRY = L"SELECT `Registry`.`Registry`, `Registry`.`Root`, `Registry`.`Key` FROM `Registry` WHERE `Registry`.`Registry`=?";
enum eQUERY_OBJECTCOMPONENT { QSOC_REGISTRY = 1, QSOC_REGROOT, QSOC_REGKEY };

LPCWSTR wzQUERY_SERVICEINSTALL = L"SELECT `ServiceInstall`.`Name` FROM `ServiceInstall` WHERE `ServiceInstall`.`ServiceInstall`=?";
enum eQUERY_SECURESERVICEINSTALL { QSSI_NAME = 1 };

enum eOBJECTTYPE { OT_UNKNOWN, OT_SERVICE, OT_FOLDER, OT_FILE, OT_REGISTRY };

static eOBJECTTYPE EObjectTypeFromString(
    __in LPCWSTR pwzTable
    )
{
    if (NULL == pwzTable)
    {
        return OT_UNKNOWN;
    }

    eOBJECTTYPE eType = OT_UNKNOWN;

    // ensure we're looking at a known table
    if (0 == lstrcmpW(L"ServiceInstall", pwzTable))
    {
        eType = OT_SERVICE;
    }
    else if (0 == lstrcmpW(L"CreateFolder", pwzTable))
    {
        eType = OT_FOLDER;
    }
    else if (0 == lstrcmpW(L"File", pwzTable))
    {
        eType = OT_FILE;
    }
    else if (0 == lstrcmpW(L"Registry", pwzTable))
    {
        eType = OT_REGISTRY;
    }

    return eType;
}

static SE_OBJECT_TYPE SEObjectTypeFromString(
    __in LPCWSTR pwzTable
    )
{
    if (NULL == pwzTable)
    {
        return SE_UNKNOWN_OBJECT_TYPE;
    }

    SE_OBJECT_TYPE objectType = SE_UNKNOWN_OBJECT_TYPE;

    if (0 == lstrcmpW(L"ServiceInstall", pwzTable))
    {
        objectType = SE_SERVICE;
    }
    else if (0 == lstrcmpW(L"CreateFolder", pwzTable) || 0 == lstrcmpW(L"File", pwzTable))
    {
        objectType = SE_FILE_OBJECT;
    }
    else if (0 == lstrcmpW(L"Registry", pwzTable))
    {
        objectType = SE_REGISTRY_KEY;
    }
    else
    {
        // Do nothing; we'll return SE_UNKNOWN_OBJECT_TYPE, and the caller should handle the situation
    }

    return objectType;
}

static HRESULT StoreACLRollbackInfo(
    __in LPWSTR pwzObject,
    __in LPCWSTR pwzTable
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    PSECURITY_DESCRIPTOR psd = NULL;
    SECURITY_DESCRIPTOR_CONTROL sdc = {0};
    DWORD dwRevision = 0;
    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzSecurityInfo = NULL;
    
    Assert(pwzObject && pwzTable);

    SE_OBJECT_TYPE objectType = SEObjectTypeFromString(const_cast<LPCWSTR> (pwzTable));

    if (SE_UNKNOWN_OBJECT_TYPE != objectType)
    {
        er = ::GetNamedSecurityInfoW(pwzObject, objectType, DACL_SECURITY_INFORMATION, NULL, NULL, NULL, NULL, &psd);
        if (ERROR_FILE_NOT_FOUND == er || ERROR_PATH_NOT_FOUND == er || ERROR_SERVICE_DOES_NOT_EXIST == HRESULT_CODE(er))
        {
            // If the file, path or service doesn't exist yet, skip rollback without a message
            hr = HRESULT_FROM_WIN32(er);
            ExitFunction();
        }

        ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "Unable to schedule rollback for object: %ls", pwzObject);

        //Need to see if DACL is protected so getting Descriptor information
        if (!::GetSecurityDescriptorControl(psd, &sdc, &dwRevision))
        {
            ExitOnLastError1(hr, "Unable to schedule rollback for object (failed to get security descriptor control): %ls", pwzObject);
        }

        // Convert the security information to a string, and write this to the custom action data
        if (!::ConvertSecurityDescriptorToStringSecurityDescriptorW(psd,SDDL_REVISION_1,DACL_SECURITY_INFORMATION,&pwzSecurityInfo,NULL))
        {
            hr = E_UNEXPECTED;
            ExitOnFailure1(hr, "Unable to schedule rollback for object (failed to convert security descriptor to a valid security descriptor string): %ls", pwzObject);
        }

        hr = WcaWriteStringToCaData(pwzObject, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to add object data to rollback CustomActionData");

        hr = WcaWriteStringToCaData(pwzTable, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to add table name to rollback CustomActionData");

        hr = WcaWriteStringToCaData(pwzSecurityInfo, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to add security info data to rollback CustomActionData");

        // Write a 1 if DACL is protected, 0 otherwise
        if (sdc & SE_DACL_PROTECTED)
        {
            hr = WcaWriteIntegerToCaData(1,&pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to rollbackCustomActionData");
        }
        else
        {
            hr = WcaWriteIntegerToCaData(0,&pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to rollback CustomActionData");
        }

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"ExecSecureObjectsRollback"), pwzCustomActionData, COST_SECUREOBJECT);
        ExitOnFailure2(hr, "failed to schedule ExecSecureObjectsRollback for item: %ls of type: %ls", pwzObject, pwzTable);

        ReleaseStr(pwzCustomActionData);
        pwzCustomActionData = NULL;

    }
    else
    {
        MessageExitOnFailure1(hr = E_UNEXPECTED, msierrSecureObjectsUnknownType, "unknown object type: %ls", pwzTable);
    }
LExit:
    ReleaseStr(pwzCustomActionData);

    if (psd)
    {
        ::LocalFree(psd);
    }

    return hr;
}

static HRESULT GetTargetPath(
    __in eOBJECTTYPE eType,
    __in LPCWSTR pwzSecureObject,
    __out LPWSTR* ppwzTargetPath
    )
{
    HRESULT hr = S_OK;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRecObject = NULL;
    PMSIHANDLE hRec = NULL;

    int iRoot = 0;
    int iAllUsers = 0;
    LPWSTR pwzKey = NULL;
    LPWSTR pwzFormattedString = NULL;

    if (OT_SERVICE == eType)
    {
        hr = WcaTableExists(L"ServiceInstall");
        if (S_FALSE == hr)
        {
            hr = E_UNEXPECTED;
        }
        ExitOnFailure(hr, "failed to open ServiceInstall table to secure object");

        hr = WcaOpenView(wzQUERY_SERVICEINSTALL, &hView);
        ExitOnFailure(hr, "failed to open view on ServiceInstall table");

        // create a record that stores the object to secure
        hRec = MsiCreateRecord(1);
        MsiRecordSetStringW(hRec, 1, pwzSecureObject);

        // execute a view looking for the object's ServiceInstall.ServiceInstall row.
        hr = WcaExecuteView(hView, hRec);
        ExitOnFailure(hr, "failed to execute view on ServiceInstall table");
        hr = WcaFetchSingleRecord(hView, &hRecObject);
        ExitOnFailure(hr, "failed to fetch ServiceInstall row for secure object");

        hr = WcaGetRecordFormattedString(hRecObject, QSSI_NAME, ppwzTargetPath);
        ExitOnFailure1(hr, "failed to get service name for secure object: %ls", pwzSecureObject);
    }
    else if (OT_FOLDER == eType)
    {
        hr = WcaGetTargetPath(pwzSecureObject, ppwzTargetPath);
        ExitOnFailure1(hr, "failed to get target path for directory id: %ls", pwzSecureObject);
    }
    else if (OT_FILE == eType)
    {
        hr = StrAllocFormatted(&pwzFormattedString, L"[#%s]", pwzSecureObject);
        ExitOnFailure1(hr, "failed to create formatted string for securing file object: %ls", pwzSecureObject);

        hr = WcaGetFormattedString(pwzFormattedString, ppwzTargetPath);
        ExitOnFailure2(hr, "failed to get file path from formatted string: %ls for secure object: %ls", pwzFormattedString, pwzSecureObject);
    }
    else if (OT_REGISTRY == eType)
    {
        hr = WcaTableExists(L"Registry");
        if (S_FALSE == hr)
        {
            hr = E_UNEXPECTED;
        }
        ExitOnFailure(hr, "failed to open Registry table to secure object");

        hr = WcaOpenView(wzQUERY_REGISTRY, &hView);
        ExitOnFailure(hr, "failed to open view on Registry table");

        // create a record that stores the object to secure
        hRec = MsiCreateRecord(1);
        MsiRecordSetStringW(hRec, 1, pwzSecureObject);

        // execute a view looking for the object's Registry row
        hr = WcaExecuteView(hView, hRec);
        ExitOnFailure(hr, "failed to execute view on Registry table");
        hr = WcaFetchSingleRecord(hView, &hRecObject);
        ExitOnFailure(hr, "failed to fetch Registry row for secure object");

        hr = WcaGetRecordInteger(hRecObject, QSOC_REGROOT, &iRoot);
        ExitOnFailure1(hr, "Failed to get reg key root for secure object: %ls", pwzSecureObject);

        hr = WcaGetRecordFormattedString(hRecObject, QSOC_REGKEY, &pwzKey);
        ExitOnFailure1(hr, "Failed to get reg key for secure object: %ls", pwzSecureObject);

        // Decode the root value
        if (-1 == iRoot)
        {
            // They didn't specify a root so that means it's either HKCU or HKLM depending on ALLUSERS property
            hr = WcaGetIntProperty(L"ALLUSERS", &iAllUsers);
            ExitOnFailure(hr, "failed to get value of ALLUSERS property");

            if (1 == iAllUsers)
            {
                hr = StrAllocString(ppwzTargetPath, L"MACHINE\\", 0);
                ExitOnFailure(hr, "failed to allocate target registry string with HKLM root");
            }
            else
            {
                hr = StrAllocString(ppwzTargetPath, L"CURRENT_USER\\", 0);
                ExitOnFailure(hr, "failed to allocate target registry string with HKCU root");
            }
        }
        else if (msidbRegistryRootClassesRoot == iRoot)
        {
            hr = StrAllocString(ppwzTargetPath, L"CLASSES_ROOT\\", 0);
            ExitOnFailure(hr, "failed to allocate target registry string with HKCR root");
        }
        else if (msidbRegistryRootCurrentUser == iRoot)
        {
            hr = StrAllocString(ppwzTargetPath, L"CURRENT_USER\\", 0);
            ExitOnFailure(hr, "failed to allocate target registry string with HKCU root");
        }
        else if (msidbRegistryRootLocalMachine == iRoot)
        {
            hr = StrAllocString(ppwzTargetPath, L"MACHINE\\", 0);
            ExitOnFailure(hr, "failed to allocate target registry string with HKLM root");
        }
        else if (msidbRegistryRootUsers == iRoot)
        {
            hr = StrAllocString(ppwzTargetPath, L"USERS\\", 0);
            ExitOnFailure(hr, "failed to allocate target registry string with HKU root");
        }
        else
        {
            ExitOnFailure2(hr = E_UNEXPECTED, "Unknown registry key root specified for secure object: '%ls' root: %d", pwzSecureObject, iRoot);
        }
        
        hr = StrAllocConcat(ppwzTargetPath, pwzKey, 0);
        ExitOnFailure2(hr, "Failed to concat key: %ls for secure object: %ls", pwzKey, pwzSecureObject);
    }
    else
    {
        AssertSz(FALSE, "How did you get here?");
        ExitOnFailure1(hr = E_UNEXPECTED, "Unknown secure object type: %d", eType);
    }

LExit:
    ReleaseStr(pwzFormattedString);
    ReleaseStr(pwzKey);

    return hr;
}

/******************************************************************
 SchedSecureObjects - entry point for SchedSecureObjects Custom Action

 called as Type 1 CustomAction (binary DLL) from Windows Installer 
 in InstallExecuteSequence, to schedule ExecSecureObjects
******************************************************************/
extern "C" UINT __stdcall SchedSecureObjects(
    __in MSIHANDLE hInstall
    )
{
//    AssertSz(FALSE, "debug SchedSecureObjects");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzSecureObject = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzTable = NULL;
    LPWSTR pwzTargetPath = NULL;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    LPWSTR pwzCustomActionData = NULL;

    DWORD cObjects = 0;
    eOBJECTTYPE eType = OT_UNKNOWN;

    //
    // initialize
    //
    hr = WcaInitialize(hInstall, "SchedSecureObjects");
    ExitOnFailure(hr, "failed to initialize");

    // anything to do?
    if (S_OK != WcaTableExists(L"SecureObjects"))
    {
        WcaLog(LOGMSG_STANDARD, "SecureObjects table doesn't exist, so there are no objects to secure.");
        ExitFunction();
    }

    //
    // loop through all the objects to be secured
    //
    hr = WcaOpenExecuteView(wzQUERY_SECUREOBJECTS, &hView);
    ExitOnFailure(hr, "failed to open view on SecureObjects table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, QSO_TABLE, &pwzTable);
        ExitOnFailure(hr, "failed to get object table");

        eType = EObjectTypeFromString(pwzTable);

        if (OT_UNKNOWN == eType)
        {
            ExitOnFailure1(hr = E_INVALIDARG, "unknown SecureObject.Table: %ls", pwzTable);
        }

        int iCompAttributes = 0;
        hr = WcaGetRecordInteger(hRec, QSO_COMPATTRIBUTES, &iCompAttributes);
        ExitOnFailure(hr, "failed to get Component attributes for secure object");

        BOOL fIs64Bit = iCompAttributes & msidbComponentAttributes64bit;

        // Only process entries in the SecureObjects table whose components match the bitness of this CA
#ifdef _WIN64
        if (!fIs64Bit)
        {
            continue;
        }
#else
        if (fIs64Bit)
        {
            continue;
        }
#endif

        // Get the object to secure
        hr = WcaGetRecordString(hRec, QSO_SECUREOBJECT, &pwzSecureObject);
        ExitOnFailure(hr, "failed to get name of object");

        hr = GetTargetPath(eType, pwzSecureObject, &pwzTargetPath);
        ExitOnFailure1(hr, "failed to get target path of object '%ls'", pwzSecureObject);

        hr = WcaGetRecordString(hRec, QSO_COMPONENT, &pwzData);
        ExitOnFailure(hr, "failed to get Component name for secure object");

        //
        // if we are installing this Component
        //
        er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to get install state for Component: %ls", pwzData);

        if (WcaIsInstalling(isInstalled, isAction))
        {
            hr = WcaWriteStringToCaData(pwzTargetPath, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            // add the data to the CustomActionData
            hr = WcaGetRecordString(hRec, QSO_SECUREOBJECT, &pwzData);
            ExitOnFailure(hr, "failed to get name of object");

            hr = WcaWriteStringToCaData(pwzTable, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordFormattedString(hRec, QSO_DOMAIN, &pwzData);
            ExitOnFailure(hr, "failed to get domain for user to configure object");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordFormattedString(hRec, QSO_USER, &pwzData);
            ExitOnFailure(hr, "failed to get user to configure object");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordString(hRec, QSO_PERMISSION, &pwzData);
            ExitOnFailure(hr, "failed to get permission to configure object");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            ++cObjects;
        }
    }

    // if we looped through all records all is well
    if (E_NOMOREITEMS == hr)
        hr = S_OK;
    ExitOnFailure(hr, "failed while looping through all objects to secure");

    //
    // schedule the custom action and add to progress bar
    //
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        Assert(0 < cObjects);

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"ExecSecureObjects"), pwzCustomActionData, cObjects * COST_SECUREOBJECT);
        ExitOnFailure(hr, "failed to schedule ExecSecureObjects action");
    }

LExit:
    ReleaseStr(pwzSecureObject);
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzData);
    ReleaseStr(pwzTable);
    ReleaseStr(pwzTargetPath);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}

/******************************************************************
 SchedSecureObjectsRollback - entry point for SchedSecureObjectsRollback Custom Action

 called as Type 1 CustomAction (binary DLL) from Windows Installer 
 in InstallExecuteSequence before SchedSecureObjects
******************************************************************/
extern "C" UINT __stdcall SchedSecureObjectsRollback(
    __in MSIHANDLE hInstall
    )
{
//    AssertSz(FALSE, "debug SchedSecureObjectsRollback");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzSecureObject = NULL;
    LPWSTR pwzTable = NULL;
    LPWSTR pwzTargetPath = NULL;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    LPWSTR pwzCustomActionData = NULL;

    eOBJECTTYPE eType = OT_UNKNOWN;

    //
    // initialize
    //
    hr = WcaInitialize(hInstall, "SchedSecureObjectsRollback");
    ExitOnFailure(hr, "failed to initialize");

    //
    // loop through all the objects to be secured
    //
    hr = WcaOpenExecuteView(wzQUERY_SECUREOBJECTS, &hView);
    ExitOnFailure(hr, "failed to open view on SecureObjects table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, QSO_TABLE, &pwzTable);
        ExitOnFailure(hr, "failed to get object table");

        eType = EObjectTypeFromString(pwzTable);

        if (OT_UNKNOWN == eType)
        {
            ExitOnFailure1(hr = E_INVALIDARG, "unknown SecureObject.Table: %ls", pwzTable);
        }

        int iCompAttributes = 0;
        hr = WcaGetRecordInteger(hRec, QSO_COMPATTRIBUTES, &iCompAttributes);
        ExitOnFailure(hr, "failed to get Component attributes for secure object");

        BOOL fIs64Bit = iCompAttributes & msidbComponentAttributes64bit;

        // Only process entries in the SecureObjects table whose components match the bitness of this CA
#ifdef _WIN64
        if (!fIs64Bit)
        {
            continue;
        }
#else
        if (fIs64Bit)
        {
            continue;
        }
#endif

        // get the object being secured that we are planning to schedule rollback for
        hr = WcaGetRecordString(hRec, QSO_SECUREOBJECT, &pwzSecureObject);
        ExitOnFailure(hr, "failed to get name of object");

        hr = GetTargetPath(eType, pwzSecureObject, &pwzTargetPath);
        ExitOnFailure1(hr, "failed to get target path of object '%ls' in order to schedule rollback", pwzSecureObject);

        hr = StoreACLRollbackInfo(pwzTargetPath, pwzTable);
        if (FAILED(hr))
        {
            WcaLog(LOGMSG_STANDARD, "Failed to store ACL rollback information with error 0x%x - continuing", hr);
        }
    }

    // if we looped through all records all is well
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed while looping through all objects to schedule rollback for");

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzSecureObject);
    ReleaseStr(pwzTable);
    ReleaseStr(pwzTargetPath);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}

/******************************************************************
 CaExecSecureObjects - entry point for SecureObjects Custom Action
                   called as Type 1025 CustomAction (deferred binary DLL)

 NOTE: deferred CustomAction since it modifies the machine
 NOTE: CustomActionData == wzObject\twzTable\twzDomain\twzUser\tdwPermissions\twzObject\t...
******************************************************************/
extern "C" UINT __stdcall ExecSecureObjects(
    __in MSIHANDLE hInstall
    )
{
//    AssertSz(FALSE, "debug ExecSecureObjects");
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    LPWSTR pwz = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzObject = NULL;
    LPWSTR pwzTable = NULL;
    LPWSTR pwzDomain = NULL;
    DWORD dwRevision = 0;
    LPWSTR pwzUser = NULL;
    DWORD dwPermissions = 0;
    LPWSTR pwzAccount = NULL;
    PSID psid = NULL;

    EXPLICIT_ACCESSW ea = {0};
    SE_OBJECT_TYPE objectType = SE_UNKNOWN_OBJECT_TYPE;
    PSECURITY_DESCRIPTOR psd = NULL;
    SECURITY_DESCRIPTOR_CONTROL sdc = {0};
    SECURITY_INFORMATION si = {0};
    PACL pAclExisting = NULL;   // doesn't get freed
    PACL pAclNew = NULL;

    PMSIHANDLE hActionRec = ::MsiCreateRecord(1);

    //
    // initialize
    //
    hr = WcaInitialize(hInstall, "ExecSecureObjects");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;

    //
    // loop through all the passed in data
    //
    while (pwz && *pwz)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzObject);
        ExitOnFailure(hr, "failed to process CustomActionData");

        hr = WcaReadStringFromCaData(&pwz, &pwzTable);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzUser);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwPermissions));
        ExitOnFailure(hr, "failed to processCustomActionData");

        WcaLog(LOGMSG_VERBOSE, "Securing Object: %ls Type: %ls User: %ls", pwzObject, pwzTable, pwzUser);

        //
        // create the appropriate SID
        //

        // figure out the right user to put into the access block
        if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Everyone"))
        {
            hr = AclGetWellKnownSid(WinWorldSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Administrators"))
        {
            hr = AclGetWellKnownSid(WinBuiltinAdministratorsSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"LocalSystem"))
        {
            hr = AclGetWellKnownSid(WinLocalSystemSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"LocalService"))
        {
            hr = AclGetWellKnownSid(WinLocalServiceSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"NetworkService"))
        {
            hr = AclGetWellKnownSid(WinNetworkServiceSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"AuthenticatedUser"))
        {
            hr = AclGetWellKnownSid(WinAuthenticatedUserSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Guests"))
        {
            hr = AclGetWellKnownSid(WinBuiltinGuestsSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"CREATOR OWNER"))
        {
            hr = AclGetWellKnownSid(WinCreatorOwnerSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"INTERACTIVE"))
        {
            hr = AclGetWellKnownSid(WinInteractiveSid, &psid);
        }
        else if (!*pwzDomain && 0 == lstrcmpW(pwzUser, L"Users"))
        {
            hr = AclGetWellKnownSid(WinBuiltinUsersSid, &psid);
        }
        else
        {
            hr = StrAllocFormatted(&pwzAccount, L"%s%s%s", pwzDomain, *pwzDomain ? L"\\" : L"", pwzUser);
            ExitOnFailure(hr, "failed to build domain user name");

            hr = AclGetAccountSid(NULL, pwzAccount, &psid);
        }
        ExitOnFailure3(hr, "failed to get sid for account: %ls%ls%ls", pwzDomain, *pwzDomain ? L"\\" : L"", pwzUser);

        //
        // build up the explicit access
        //
        ea.grfAccessMode = SET_ACCESS;

        if (0 == lstrcmpW(L"CreateFolder", pwzTable))
        {
            ea.grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
        }
        else
        {
            ea.grfInheritance = NO_INHERITANCE;
        }

#pragma prefast(push)
#pragma prefast(disable:25029)
        ::BuildTrusteeWithSidW(&ea.Trustee, psid);
#pragma prefast(pop)

        objectType = SEObjectTypeFromString(const_cast<LPCWSTR> (pwzTable));

        // always add these permissions for services
        // these are basic permissions that are often forgotten
        if (0 == lstrcmpW(L"ServiceInstall", pwzTable))
        {
            dwPermissions |= SERVICE_QUERY_CONFIG | SERVICE_QUERY_STATUS | SERVICE_ENUMERATE_DEPENDENTS | SERVICE_INTERROGATE;
        }

        ea.grfAccessPermissions = dwPermissions;

        if (SE_UNKNOWN_OBJECT_TYPE != objectType)
        {
            er = ::GetNamedSecurityInfoW(pwzObject, objectType, DACL_SECURITY_INFORMATION, NULL, NULL, &pAclExisting, NULL, &psd);
            ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to get security info for object: %ls", pwzObject);

            //Need to see if DACL is protected so getting Descriptor information
            if (!::GetSecurityDescriptorControl(psd, &sdc, &dwRevision))
            {
                ExitOnLastError1(hr, "failed to get security descriptor control for object: %ls", pwzObject);
            }

#pragma prefast(push)
#pragma prefast(disable:25029)
            er = ::SetEntriesInAclW(1, &ea, pAclExisting, &pAclNew);
#pragma prefast(pop)
            ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to add ACLs for object: %ls", pwzObject);

            if (sdc & SE_DACL_PROTECTED)
            {
                si = DACL_SECURITY_INFORMATION | PROTECTED_DACL_SECURITY_INFORMATION;
            }
            else
            {
                si = DACL_SECURITY_INFORMATION;
            }
            er = ::SetNamedSecurityInfoW(pwzObject, objectType, si, NULL, NULL, pAclNew, NULL);
            MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(er), msierrSecureObjectsFailedSet, "failed to set security info for object: %ls", pwzObject);
        }
        else
        {
            MessageExitOnFailure1(hr = E_UNEXPECTED, msierrSecureObjectsUnknownType, "unknown object type: %ls", pwzTable);
        }

        hr = WcaProgressMessage(COST_SECUREOBJECT, FALSE);
        ExitOnFailure(hr, "failed to send progress message");

        objectType = SE_UNKNOWN_OBJECT_TYPE;
    }

LExit:
    ReleaseStr(pwzUser);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzTable);
    ReleaseStr(pwzObject);
    ReleaseStr(pwzData);
    ReleaseStr(pwzAccount);

    if (pAclNew)
    {
        ::LocalFree(pAclNew);
    }
    if (psd)
    {
        ::LocalFree(psd);
    }
    if (psid)
    {
        AclFreeSid(psid);
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}

extern "C" UINT __stdcall ExecSecureObjectsRollback(
    __in MSIHANDLE hInstall
    )
{
//    AssertSz(FALSE, "debug ExecSecureObjectsRollback");
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    LPWSTR pwz = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzObject = NULL;
    LPWSTR pwzTable = NULL;
    LPWSTR pwzSecurityInfo = NULL;

    SE_OBJECT_TYPE objectType = SE_UNKNOWN_OBJECT_TYPE;
    PSECURITY_DESCRIPTOR psd = NULL;
    ULONG psdSize;
    SECURITY_DESCRIPTOR_CONTROL sdc = {0};
    SECURITY_INFORMATION si = DACL_SECURITY_INFORMATION;
    PACL pDacl = NULL;
    BOOL bDaclPresent = false;
    BOOL bDaclDefaulted = false;
    DWORD dwRevision = 0;
    int iProtected;

    // initialize
    hr = WcaInitialize(hInstall, "ExecSecureObjectsRollback");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;

    hr = WcaReadStringFromCaData(&pwz, &pwzObject);
    ExitOnFailure(hr, "failed to process CustomActionData");

    hr = WcaReadStringFromCaData(&pwz, &pwzTable);
    ExitOnFailure(hr, "failed to process CustomActionData");

    objectType = SEObjectTypeFromString(const_cast<LPCWSTR> (pwzTable));

    if (SE_UNKNOWN_OBJECT_TYPE != objectType)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzSecurityInfo);
        ExitOnFailure(hr, "failed to process CustomActionData");

        hr = WcaReadIntegerFromCaData(&pwz, &iProtected);
        ExitOnFailure(hr, "failed to process CustomActionData");

        if (!::ConvertStringSecurityDescriptorToSecurityDescriptorW(pwzSecurityInfo,SDDL_REVISION_1,&psd,&psdSize))
        {
            ExitOnLastError(hr, "failed to convert security descriptor string to a valid security descriptor");
        }

        if (!::GetSecurityDescriptorDacl(psd,&bDaclPresent,&pDacl,&bDaclDefaulted))
        {
            hr = E_UNEXPECTED;
            ExitOnFailure2(hr, "failed to get security descriptor's DACL - error code: %d",pwzSecurityInfo,GetLastError());
        }

        // The below situation may always be caught by the above if block - the documentation isn't very clear. To be safe, we're going to test for it.
        if (!bDaclPresent)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "security descriptor does not contain a DACL");
        }

        //Need to see if DACL is protected so getting Descriptor information
        if (!::GetSecurityDescriptorControl(psd, &sdc, &dwRevision))
        {
            ExitOnLastError1(hr, "failed to get security descriptor control for object: %ls", pwzObject);
        }

        // Write a 1 if DACL is protected, 0 otherwise
        switch (iProtected)
        {
        case 0:
            // Unnecessary to do anything - leave si to the default flags
            break;

        case 1:
            si = si | PROTECTED_DACL_SECURITY_INFORMATION;
            break;

        default:
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "unrecognized value in CustomActionData");
            break;
        }

        er = ::SetNamedSecurityInfoW(pwzObject, objectType, si, NULL, NULL, pDacl, NULL);
        ExitOnFailure2(hr = HRESULT_FROM_WIN32(er), "failed to set security info for object: %ls error code: %d", pwzObject, GetLastError());
    }
    else
    {
        MessageExitOnFailure1(hr = E_UNEXPECTED, msierrSecureObjectsUnknownType, "unknown object type: %ls", pwzTable);
    }

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzObject);
    ReleaseStr(pwzTable);
    ReleaseStr(pwzSecurityInfo);

    if (psd)
    {
        ::LocalFree(psd);
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
