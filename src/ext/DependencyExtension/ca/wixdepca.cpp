// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define IDNOACTION 0
#define INITIAL_STRINGDICT_SIZE 4

LPCWSTR vcsDependencyProviderQuery =
    L"SELECT `WixDependencyProvider`.`WixDependencyProvider`, `WixDependencyProvider`.`Component_`, `WixDependencyProvider`.`ProviderKey`, `WixDependencyProvider`.`Attributes` "
    L"FROM `WixDependencyProvider`";
enum eDependencyProviderQuery { dpqId = 1, dpqComponent, dpqProviderKey, dpqAttributes };

LPCWSTR vcsDependencyQuery =
    L"SELECT `WixDependency`.`WixDependency`, `WixDependencyProvider`.`Component_`, `WixDependency`.`ProviderKey`, `WixDependency`.`MinVersion`, `WixDependency`.`MaxVersion`, `WixDependency`.`Attributes` "
    L"FROM `WixDependencyProvider`, `WixDependency`, `WixDependencyRef` "
    L"WHERE `WixDependency`.`WixDependency` = `WixDependencyRef`.`WixDependency_` AND `WixDependencyProvider`.`WixDependencyProvider` = `WixDependencyRef`.`WixDependencyProvider_`";
enum eDependencyComponentQuery { dqId = 1, dqComponent, dqProviderKey, dqMinVersion, dqMaxVersion, dqAttributes };

static HRESULT EnsureRequiredDependencies(
    __in MSIHANDLE hInstall,
    __in BOOL fMachineContext
    );

static HRESULT EnsureAbsentDependents(
    __in MSIHANDLE hInstall,
    __in BOOL fMachineContext
    );

static HRESULT SplitIgnoredDependents(
    __deref_inout STRINGDICT_HANDLE* psdIgnoredDependents
    );

static HRESULT CreateDependencyRecord(
    __in int iMessageId,
    __in_ecount(cDependencies) const DEPENDENCY* rgDependencies,
    __in UINT cDependencies,
    __out MSIHANDLE *phRecord
    );

static LPCWSTR LogDependencyName(
    __in_z LPCWSTR wzName
    );

/***************************************************************************
 WixDependencyRequire - Checks that all required dependencies are installed.

***************************************************************************/
extern "C" UINT __stdcall WixDependencyRequire(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    BOOL fMachineContext = FALSE;

    hr = WcaInitialize(hInstall, "WixDependencyRequire");
    ExitOnFailure(hr, "Failed to initialize.");

    hr = RegInitialize();
    ExitOnFailure(hr, "Failed to initialize the registry functions.");

    // Determine whether we're installing per-user or per-machine.
    fMachineContext = WcaIsPropertySet("ALLUSERS");

    // Check for any provider components being (re)installed that their requirements are already installed.
    hr = EnsureRequiredDependencies(hInstall, fMachineContext);
    ExitOnFailure(hr, "Failed to ensure required dependencies for (re)installing components.");

LExit:
    RegUninitialize();

    er = FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS;
    return WcaFinalize(er);
}

/***************************************************************************
 WixDependencyCheck - Check dependencies based on component state.

 Note: may return ERROR_NO_MORE_ITEMS to terminate the session early.
***************************************************************************/
extern "C" UINT __stdcall WixDependencyCheck(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    BOOL fMachineContext = FALSE;

    hr = WcaInitialize(hInstall, "WixDependencyCheck");
    ExitOnFailure(hr, "Failed to initialize.");

    hr = RegInitialize();
    ExitOnFailure(hr, "Failed to initialize the registry functions.");

    // Determine whether we're installing per-user or per-machine.
    fMachineContext = WcaIsPropertySet("ALLUSERS");

    // Check for any dependents of provider components being uninstalled.
    hr = EnsureAbsentDependents(hInstall, fMachineContext);
    ExitOnFailure(hr, "Failed to ensure absent dependents for uninstalling components.");

LExit:
    RegUninitialize();

    er = FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS;
    return WcaFinalize(er);
}

/***************************************************************************
 EnsureRequiredDependencies - Check that dependencies are installed for
  any provider component that is being installed or reinstalled.

 Note: Skipped if DISABLEDEPENDENCYCHECK is set.
***************************************************************************/
static HRESULT EnsureRequiredDependencies(
    __in MSIHANDLE /*hInstall*/,
    __in BOOL fMachineContext
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    STRINGDICT_HANDLE sdDependencies = NULL;
    HKEY hkHive = NULL;
    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;
    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    LPWSTR sczProviderKey = NULL;
    LPWSTR sczMinVersion = NULL;
    LPWSTR sczMaxVersion = NULL;
    int iAttributes = 0;
    WCA_TODO tComponentAction = WCA_TODO_UNKNOWN;
    DEPENDENCY* rgDependencies = NULL;
    UINT cDependencies = 0;
    PMSIHANDLE hDependencyRec = NULL;

    // Skip the dependency check if the WixDependency table is missing (no dependencies to check for).
    hr = WcaTableExists(L"WixDependency");
    if (S_FALSE == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Skipping the dependency check since no dependencies are authored.");
        ExitFunction1(hr = S_OK);
    }

    // If the table exists but not the others, the database was not authored correctly.
    ExitOnFailure(hr, "Failed to check if the WixDependency table exists.");

    // Initialize the dictionary to keep track of unique dependency keys.
    hr = DictCreateStringList(&sdDependencies, INITIAL_STRINGDICT_SIZE, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to initialize the unique dependency string list.");

    // Set the registry hive to use depending on install context.
    hkHive = fMachineContext ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

    // Loop over the provider components.
    hr = WcaOpenExecuteView(vcsDependencyQuery, &hView);
    ExitOnFailure(hr, "Failed to open the query view for dependencies.");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, dqId, &sczId);
        ExitOnFailure(hr, "Failed to get WixDependency.WixDependency.");

        hr = WcaGetRecordString(hRec, dqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get WixDependencyProvider.Component_.");

        // Skip the current component if its not being installed or reinstalled.
        tComponentAction = WcaGetComponentToDo(sczComponent);
        if (WCA_TODO_INSTALL != tComponentAction && WCA_TODO_REINSTALL != tComponentAction)
        {
            WcaLog(LOGMSG_STANDARD, "Skipping dependency check for %ls because the component %ls is not being (re)installed.", sczId, sczComponent);
            continue;
        }

        hr = WcaGetRecordString(hRec, dqProviderKey, &sczProviderKey);
        ExitOnFailure(hr, "Failed to get WixDependency.ProviderKey.");

        hr = WcaGetRecordString(hRec, dqMinVersion, &sczMinVersion);
        ExitOnFailure(hr, "Failed to get WixDependency.MinVersion.");

        hr = WcaGetRecordString(hRec, dqMaxVersion, &sczMaxVersion);
        ExitOnFailure(hr, "Failed to get WixDependency.MaxVersion.");

        hr = WcaGetRecordInteger(hRec, dqAttributes, &iAttributes);
        ExitOnFailure(hr, "Failed to get WixDependency.Attributes.");

        // Check the registry to see if the required providers (dependencies) exist.
        hr = DepCheckDependency(hkHive, sczProviderKey, sczMinVersion, sczMaxVersion, iAttributes, sdDependencies, &rgDependencies, &cDependencies);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure1(hr, "Failed dependency check for %ls.", sczId);
        }
    }

    if (E_NOMOREITEMS != hr)
    {
        ExitOnFailure(hr, "Failed to enumerate all of the rows in the dependency query view.");
    }
    else
    {
        hr = S_OK;
    }

    // If we collected any dependencies in the previous check, pump a message and prompt the user.
    if (0 < cDependencies)
    {
        hr = CreateDependencyRecord(msierrDependencyMissingDependencies, rgDependencies, cDependencies, &hDependencyRec);
        ExitOnFailure1(hr, "Failed to create the dependency record for message %d.", msierrDependencyMissingDependencies);

        // Send a yes/no message with a warning icon since continuing could be detrimental.
        // This is sent as a USER message to better detect whether a user or dependency-aware bootstrapper is responding
        // or if Windows Installer or a dependency-unaware boostrapper is returning a typical default response.
        er = WcaProcessMessage(static_cast<INSTALLMESSAGE>(INSTALLMESSAGE_USER | MB_ICONWARNING | MB_YESNO | MB_DEFBUTTON2), hDependencyRec);
        switch (er)
        {
        // Only a user or dependency-aware bootstrapper that prompted the user should return IDYES to continue anyway.
        case IDYES:
            ExitFunction1(hr = S_OK);

        // Only a user or dependency-aware bootstrapper that prompted the user should return IDNO to terminate the operation.
        case IDNO:
            WcaSetReturnValue(ERROR_INSTALL_USEREXIT);
            ExitFunction1(hr = S_OK);

        // A dependency-aware bootstrapper should return IDCANCEL if running silently and the operation should be canceled.
        case IDCANCEL:
            __fallthrough;

        // Bootstrappers which are not dependency-aware may return IDOK for unhandled messages.
        case IDOK:
            __fallthrough;

        // Windows Installer returns 0 for USER messages when silent or passive, or when a bootstrapper does not handle the message.
        case IDNOACTION:
            WcaSetReturnValue(ERROR_INSTALL_FAILURE);
            ExitFunction1(hr = S_OK);

        default:
            ExitOnFailure1(hr = E_UNEXPECTED, "Unexpected message response %d from user or bootstrapper application.", er);
        }
    }

LExit:
    ReleaseDependencyArray(rgDependencies, cDependencies);
    ReleaseStr(sczId);
    ReleaseStr(sczComponent);
    ReleaseStr(sczProviderKey);
    ReleaseStr(sczMinVersion);
    ReleaseStr(sczMaxVersion);
    ReleaseDict(sdDependencies);

    return hr;
}

/***************************************************************************
 EnsureAbsentDependents - Checks that there are no dependents
  registered for providers that are being uninstalled.

 Note: Skipped if UPGRADINGPRODUCTCODE is set.
***************************************************************************/
static HRESULT EnsureAbsentDependents(
    __in MSIHANDLE /*hInstall*/,
    __in BOOL fMachineContext
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    STRINGDICT_HANDLE sdIgnoredDependents = NULL;
    HKEY hkHive = NULL;
    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;
    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    LPWSTR sczProviderKey = NULL;
    int iAttributes = 0;
    WCA_TODO tComponentAction = WCA_TODO_UNKNOWN;
    DEPENDENCY* rgDependents = NULL;
    UINT cDependents = 0;
    PMSIHANDLE hDependencyRec = NULL;

    // Split the IGNOREDEPENDENCIES property for use below if set. If it is "ALL", then quit now.
    hr = SplitIgnoredDependents(&sdIgnoredDependents);
    ExitOnFailure(hr, "Failed to get the ignored dependents.");

    hr = DictKeyExists(sdIgnoredDependents, L"ALL");
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to check if \"ALL\" was set in IGNOREDEPENDENCIES.");

        // Otherwise...
        WcaLog(LOGMSG_STANDARD, "Skipping the dependencies check since IGNOREDEPENDENCIES contains \"ALL\".");
        ExitFunction();
    }
    else
    {
        // Key was not found, so proceed.
        hr = S_OK;
    }

    // Skip the dependent check if the WixDependencyProvider table is missing (no dependency providers).
    hr = WcaTableExists(L"WixDependencyProvider");
    if (S_FALSE == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Skipping the dependents check since no dependency providers are authored.");
        ExitFunction();
    }

    ExitOnFailure(hr, "Failed to check if the WixDependencyProvider table exists.");

    // Set the registry hive to use depending on install context.
    hkHive = fMachineContext ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

    // Loop over the provider components.
    hr = WcaOpenExecuteView(vcsDependencyProviderQuery, &hView);
    ExitOnFailure(hr, "Failed to open the query view for dependency providers.");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, dpqId, &sczId);
        ExitOnFailure(hr, "Failed to get WixDependencyProvider.WixDependencyProvider.");

        hr = WcaGetRecordString(hRec, dpqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get WixDependencyProvider.Component.");

        // Skip the current component if its not being uninstalled.
        tComponentAction = WcaGetComponentToDo(sczComponent);
        if (WCA_TODO_UNINSTALL != tComponentAction)
        {
            WcaLog(LOGMSG_STANDARD, "Skipping dependents check for %ls because the component %ls is not being uninstalled.", sczId, sczComponent);
            continue;
        }

        hr = WcaGetRecordString(hRec, dpqProviderKey, &sczProviderKey);
        ExitOnFailure(hr, "Failed to get WixDependencyProvider.ProviderKey.");

        hr = WcaGetRecordInteger(hRec, dpqAttributes, &iAttributes);
        ExitOnFailure(hr, "Failed to get WixDependencyProvider.Attributes.");

        // Check the registry to see if the provider has any dependents registered.
        hr = DepCheckDependents(hkHive, sczProviderKey, iAttributes, sdIgnoredDependents, &rgDependents, &cDependents);
        ExitOnFailure1(hr, "Failed dependents check for %ls.", sczId);
    }

    if (E_NOMOREITEMS != hr)
    {
        ExitOnFailure(hr, "Failed to enumerate all of the rows in the dependency provider query view.");
    }
    else
    {
        hr = S_OK;
    }

    // If we collected any providers with dependents in the previous check, pump a message and prompt the user.
    if (0 < cDependents)
    {
        hr = CreateDependencyRecord(msierrDependencyHasDependents, rgDependents, cDependents, &hDependencyRec);
        ExitOnFailure1(hr, "Failed to create the dependency record for message %d.", msierrDependencyHasDependents);

        // Send a yes/no message with a warning icon since continuing could be detrimental.
        // This is sent as a USER message to better detect whether a user or dependency-aware bootstrapper is responding
        // or if Windows Installer or a dependency-unaware boostrapper is returning a typical default response.
        er = WcaProcessMessage(static_cast<INSTALLMESSAGE>(INSTALLMESSAGE_USER | MB_ICONWARNING | MB_YESNO | MB_DEFBUTTON2), hDependencyRec);
        switch (er)
        {
        // Only a user or dependency-aware bootstrapper that prompted the user should return IDYES to continue anyway.
        case IDYES:
            ExitFunction1(hr = S_OK);

        // Only a user or dependency-aware bootstrapper that prompted the user should return IDNO to terminate the operation.
        case IDNO:
            __fallthrough;

        // Bootstrappers which are not dependency-aware may return IDOK for unhandled messages.
        case IDOK:
            __fallthrough;

        // Windows Installer returns 0 for USER messages when silent or passive, or when a bootstrapper does not handle the message.
        case IDNOACTION:
            WcaSetReturnValue(ERROR_NO_MORE_ITEMS);
            ExitFunction1(hr = S_OK);

        // A dependency-aware bootstrapper should return IDCANCEL if running silently and the operation should be canceled.
        case IDCANCEL:
            WcaSetReturnValue(ERROR_INSTALL_FAILURE);
            ExitFunction1(hr = S_OK);

        default:
            hr = E_UNEXPECTED;
            ExitOnFailure1(hr, "Unexpected message response %d from user or bootstrapper application.", er);
        }
    }

LExit:
    ReleaseDependencyArray(rgDependents, cDependents);
    ReleaseStr(sczId);
    ReleaseStr(sczComponent);
    ReleaseStr(sczProviderKey);

    return hr;
}

/***************************************************************************
 SplitIgnoredDependents - Splits the IGNOREDEPENDENCIES property into a map.

***************************************************************************/
static HRESULT SplitIgnoredDependents(
    __deref_inout STRINGDICT_HANDLE* psdIgnoredDependents
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczIgnoreDependencies = NULL;
    LPCWSTR wzDelim = L";";
    LPWSTR wzContext = NULL;

    hr = WcaGetProperty(L"IGNOREDEPENDENCIES", &sczIgnoreDependencies);
    ExitOnFailure(hr, "Failed to get the string value of the IGNOREDEPENDENCIES property.");

    hr = DictCreateStringList(psdIgnoredDependents, INITIAL_STRINGDICT_SIZE, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create the string dictionary.");

    // Parse through the semicolon-delimited tokens and add to the string dictionary.
    for (LPCWSTR wzToken = ::wcstok_s(sczIgnoreDependencies, wzDelim, &wzContext); wzToken; wzToken = ::wcstok_s(NULL, wzDelim, &wzContext))
    {
        hr = DictAddKey(*psdIgnoredDependents, wzToken);
        ExitOnFailure1(hr, "Failed to ignored dependency \"%ls\" to the string dictionary.", wzToken);
    }

LExit:
    ReleaseStr(sczIgnoreDependencies);

    return hr;
}

/***************************************************************************
 CreateDependencyRecord - Creates a record containing the message template
  and records to send to the UI handler.

 Notes: Callers should call WcaProcessMessage and handle return codes.
***************************************************************************/
static HRESULT CreateDependencyRecord(
    __in int iMessageId,
    __in_ecount(cDependencies) const DEPENDENCY* rgDependencies,
    __in UINT cDependencies,
    __out MSIHANDLE *phRecord
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    UINT cParams = 0;
    UINT iParam = 0;

    // Should not be PMSIHANDLE.
    MSIHANDLE hRec = NULL;

    // Calculate the number of parameters based on the format:
    // msgId, count, key1, name1, key2, name2, etc.
    cParams = 2 + 2 * cDependencies;

    hRec = ::MsiCreateRecord(cParams);
    ExitOnNull(hRec, hr, E_OUTOFMEMORY, "Not enough memory to create the message record.");

    er = ::MsiRecordSetInteger(hRec, ++iParam, iMessageId);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set the message identifier into the message record.");

    er = ::MsiRecordSetInteger(hRec, ++iParam, cDependencies);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set the number of dependencies into the message record.");

    // Now loop through each dependency and add the key and name to the record.
    for (UINT i = 0; i < cDependencies; i++)
    {
        const DEPENDENCY* pDependency = &rgDependencies[i];

        // Log message type-specific information.
        switch (iMessageId)
        {
        // Send a user message when installing a component that is missing some dependencies.
        case msierrDependencyMissingDependencies:
            WcaLog(LOGMSG_VERBOSE, "The dependency \"%ls\" is missing or is not the required version.", pDependency->sczKey);
            break;

        // Send a user message when uninstalling a component that still has registered dependents.
        case msierrDependencyHasDependents:
            WcaLog(LOGMSG_VERBOSE, "Found dependent \"%ls\", name: \"%ls\".", pDependency->sczKey, LogDependencyName(pDependency->sczName));
            break;
        }

        er = ::MsiRecordSetStringW(hRec, ++iParam, pDependency->sczKey);
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "Failed to set the dependency key \"%ls\" into the message record.", pDependency->sczKey);

        er = ::MsiRecordSetStringW(hRec, ++iParam, pDependency->sczName);
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "Failed to set the dependency name \"%ls\" into the message record.", pDependency->sczName);
    }

    // Only assign the out parameter if successful to this point.
    *phRecord = hRec;
    hRec = NULL;

LExit:
    if (hRec)
    {
        ::MsiCloseHandle(hRec);
    }

    return hr;
}

/***************************************************************************
 LogDependencyName - Returns the dependency name or "Unknown" if null.

***************************************************************************/
static LPCWSTR LogDependencyName(
    __in_z LPCWSTR wzName
    )
{
    return wzName ? wzName : L"Unknown";
}
