// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsShortcutsQuery =
L"SELECT `Component_`, `Directory_`, `Name`, `Target`, `Attributes`, `IconFile`, `IconIndex` "
L"FROM `WixInternetShortcut`";
enum eShortcutsQuery { esqComponent = 1, esqDirectory, esqFilename, esqTarget, esqAttributes, esqIconFile, esqIconIndex };
enum eShortcutsAttributes { esaLink = 0, esaURL = 1 };

/******************************************************************
 WixSchedInternetShortcuts - entry point

********************************************************************/
extern "C" UINT __stdcall WixSchedInternetShortcuts(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    UINT uiCost = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    MSIHANDLE hCreateFolderTable = NULL;
    MSIHANDLE hCreateFolderColumns = NULL;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzComponent = NULL;
    LPWSTR pwzDirectory = NULL;
    LPWSTR pwzFilename = NULL;
    LPWSTR pwzTarget = NULL;
    LPWSTR pwzShortcutPath = NULL;
    int iAttr = 0;
    LPWSTR pwzIconFile = NULL;
    int iIconIndex = 0;
    IUniformResourceLocatorW* piURL = NULL;
    IShellLinkW* piShellLink = NULL;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "WixSchedInternetShortcuts");
    ExitOnFailure(hr, "failed to initialize WixSchedInternetShortcuts.");

    // anything to do?
    if (S_OK != WcaTableExists(L"WixInternetShortcut"))
    {
        WcaLog(LOGMSG_STANDARD, "WixInternetShortcut table doesn't exist, so there are no Internet shortcuts to process");
        goto LExit;
    }

    // check to see if we can create a shortcut - Server Core and others may not have a shell registered.  
    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = ::CoCreateInstance(CLSID_InternetShortcut, NULL, CLSCTX_ALL, IID_IUniformResourceLocatorW, (void**)&piURL);
    if (S_OK != hr)
    {
        WcaLog(LOGMSG_STANDARD, "failed to create an instance of IUniformResourceLocatorW, skipping shortcut creation");
        ExitFunction1(hr = S_OK);
    }

    hr = ::CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_ALL, IID_IShellLinkW, (void**)&piShellLink);
    if (S_OK != hr)
    {
        WcaLog(LOGMSG_STANDARD, "failed to create an instance of IShellLinkW, skipping shortcut creation");
        ExitFunction1(hr = S_OK);
    }

    // query and loop through all the shortcuts
    hr = WcaOpenExecuteView(vcsShortcutsQuery, &hView);
    ExitOnFailure(hr, "failed to open view on WixInternetShortcut table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // read column values
        hr = WcaGetRecordString(hRec, esqComponent, &pwzComponent);
        ExitOnFailure(hr, "failed to get shortcut component");
        hr = WcaGetRecordString(hRec, esqDirectory, &pwzDirectory);
        ExitOnFailure(hr, "failed to get shortcut directory");
        hr = WcaGetRecordString(hRec, esqFilename, &pwzFilename);
        ExitOnFailure(hr, "failed to get shortcut filename");
        hr = WcaGetRecordFormattedString(hRec, esqTarget, &pwzTarget);
        ExitOnFailure(hr, "failed to get shortcut target");
        hr = WcaGetRecordInteger(hRec, esqAttributes, &iAttr);
        ExitOnFailure(hr, "failed to get shortcut attributes");
        hr = WcaGetRecordFormattedString(hRec, esqIconFile, &pwzIconFile);
        ExitOnFailure(hr, "failed to get shortcut icon file");
        hr = WcaGetRecordInteger(hRec, esqIconIndex, &iIconIndex);
        ExitOnFailure(hr, "failed to get shortcut icon index");

        // skip processing this WixInternetShortcut row if the component isn't being configured
        WCA_TODO todo = WcaGetComponentToDo(pwzComponent);
        if (WCA_TODO_UNKNOWN == todo)
        {
            WcaLog(LOGMSG_VERBOSE, "Skipping shortcut for null-action component '%ls'", pwzComponent);
            continue;
        }

        // we need to create the directory where the shortcut is supposed to live; rather
        // than doing so in our deferred custom action, use the CreateFolder table to have MSI 
        // make (and remove) them on our behalf (including the correct cleanup of parent directories).
        MSIDBERROR dbError = MSIDBERROR_NOERROR;
        WcaLog(LOGMSG_STANDARD, "Adding folder '%ls', component '%ls' to the CreateFolder table", pwzDirectory, pwzComponent);
        hr = WcaAddTempRecord(&hCreateFolderTable, &hCreateFolderColumns, L"CreateFolder", &dbError, 0, 2, pwzDirectory, pwzComponent);
        if (MSIDBERROR_DUPLICATEKEY == dbError)
        {
            WcaLog(LOGMSG_STANDARD, "Folder '%ls' already exists in the CreateFolder table; the above error is harmless", pwzDirectory);
            hr = S_OK;
        }
        ExitOnFailure(hr, "Couldn't add temporary CreateFolder row");

        // only if we're installing/reinstalling do we need to schedule the deferred CA
        // (uninstallation is handled via permanent RemoveFile rows and temporary CreateFolder rows)
        if (WCA_TODO_INSTALL == todo || WCA_TODO_REINSTALL == todo)
        {
            // turn the Directory_ id into a path
            hr = WcaGetTargetPath(pwzDirectory, &pwzShortcutPath);
            ExitOnFailure(hr, "failed to allocate string for shortcut directory");

            // append the shortcut filename
            hr = StrAllocConcat(&pwzShortcutPath, pwzFilename, 0);
            ExitOnFailure(hr, "failed to allocate string for shortcut filename");

            // write the shortcut path and target to custom action data for deferred CAs
            hr = WcaWriteStringToCaData(pwzShortcutPath, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write shortcut path to custom action data");
            hr = WcaWriteStringToCaData(pwzTarget, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write shortcut target to custom action data");
            hr = WcaWriteIntegerToCaData(iAttr, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write shortcut attributes to custom action data");
            hr = WcaWriteStringToCaData(pwzIconFile, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write icon file to custom action data");
            hr = WcaWriteIntegerToCaData(iIconIndex, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write icon index to custom action data");

            uiCost += COST_INTERNETSHORTCUT;
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while processing WixInternetShortcut table");

    // if we have any shortcuts to install
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        // add cost to progress bar
        hr = WcaProgressMessage(uiCost, TRUE);
        ExitOnFailure(hr, "failed to extend progress bar for InternetShortcuts");

        // provide custom action data to deferred and rollback CAs
        hr = WcaSetProperty(PLATFORM_DECORATION(L"WixRollbackInternetShortcuts"), pwzCustomActionData);
        ExitOnFailure(hr, "failed to set WixRollbackInternetShortcuts rollback custom action data");
        hr = WcaSetProperty(PLATFORM_DECORATION(L"WixCreateInternetShortcuts"), pwzCustomActionData);
        ExitOnFailure(hr, "failed to set WixCreateInternetShortcuts custom action data");
    }

LExit:
    if (hCreateFolderTable)
    {
        ::MsiCloseHandle(hCreateFolderTable);
    }

    if (hCreateFolderColumns)
    {
        ::MsiCloseHandle(hCreateFolderColumns);
    }

    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwzDirectory);
    ReleaseStr(pwzFilename);
    ReleaseStr(pwzTarget);
    ReleaseStr(pwzShortcutPath);
    ReleaseObject(piShellLink);
    ReleaseObject(piURL);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}



/******************************************************************
 CreateUrl - Creates a shortcut via IUniformResourceLocatorW

*******************************************************************/
static HRESULT CreateUrl(
    __in_z LPCWSTR wzTarget,
    __in_z LPCWSTR wzShortcutPath,
    __in_z_opt LPCWSTR wzIconPath,
    __in int iconIndex
)
{
    HRESULT hr = S_OK;
    IUniformResourceLocatorW* piURL = NULL;
    IPersistFile* piPersistFile = NULL;
    IPropertySetStorage* piProperties = NULL;
    IPropertyStorage* piStorage = NULL;

    // create an internet shortcut object
    WcaLog(LOGMSG_STANDARD, "Creating IUniformResourceLocatorW shortcut '%ls' target '%ls'", wzShortcutPath, wzTarget);
    hr = ::CoCreateInstance(CLSID_InternetShortcut, NULL, CLSCTX_ALL, IID_IUniformResourceLocatorW, (void**)&piURL);
    ExitOnFailure(hr, "failed to create an instance of IUniformResourceLocatorW");

    // set shortcut target
    hr = piURL->SetURL(wzTarget, 0);
    ExitOnFailure2(hr, "failed to set shortcut '%ls' target '%ls'", wzShortcutPath, wzTarget);

    if (wzIconPath)
    {
        hr = piURL->QueryInterface(IID_IPropertySetStorage, (void **)&piProperties);
        ExitOnFailure1(hr, "failed to get IPropertySetStorage for shortcut '%ls'", wzShortcutPath);

        hr = piProperties->Open(FMTID_Intshcut, STGM_WRITE, &piStorage);
        ExitOnFailure1(hr, "failed to open storage for shortcut '%ls'", wzShortcutPath);

        PROPSPEC ppids[2] = { {PRSPEC_PROPID, PID_IS_ICONINDEX}, {PRSPEC_PROPID, PID_IS_ICONFILE} };
        PROPVARIANT ppvar[2];

        PropVariantInit(ppvar);
        PropVariantInit(ppvar + 1);

        ppvar[0].vt = VT_I4;
        ppvar[0].lVal = iconIndex;
        ppvar[1].vt = VT_LPWSTR;
        ppvar[1].pwszVal = (LPWSTR)wzIconPath;

        hr = piStorage->WriteMultiple(2, ppids, ppvar, 0);
        ExitOnFailure1(hr, "failed to write icon storage for shortcut '%ls'", wzShortcutPath);

        hr = piStorage->Commit(STGC_DEFAULT);
        ExitOnFailure1(hr, "failed to commit icon storage for shortcut '%ls'", wzShortcutPath);
    }

    // get an IPersistFile and save the shortcut
    hr = piURL->QueryInterface(IID_IPersistFile, (void**)&piPersistFile);
    ExitOnFailure1(hr, "failed to get IPersistFile for shortcut '%ls'", wzShortcutPath);

    hr = piPersistFile->Save(wzShortcutPath, TRUE);
    ExitOnFailure1(hr, "failed to save shortcut '%ls'", wzShortcutPath);

LExit:
    ReleaseObject(piPersistFile);
    ReleaseObject(piURL);
    ReleaseObject(piStorage);
    ReleaseObject(piProperties);

    return hr;
}

/******************************************************************
 CreateLink - Creates a shortcut via IShellLinkW

*******************************************************************/
static HRESULT CreateLink(
    __in_z LPCWSTR wzTarget,
    __in_z LPCWSTR wzShortcutPath,
    __in_z_opt LPCWSTR wzIconPath,
    __in int iconIndex
)
{
    HRESULT hr = S_OK;
    IShellLinkW* piShellLink = NULL;
    IPersistFile* piPersistFile = NULL;

    // create an internet shortcut object
    WcaLog(LOGMSG_STANDARD, "Creating IShellLinkW shortcut '%ls' target '%ls'", wzShortcutPath, wzTarget);
    hr = ::CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_ALL, IID_IShellLinkW, (void**)&piShellLink);
    ExitOnFailure(hr, "failed to create an instance of IShellLinkW");

    // set shortcut target
    hr = piShellLink->SetPath(wzTarget);
    ExitOnFailure2(hr, "failed to set shortcut '%ls' target '%ls'", wzShortcutPath, wzTarget);

    if (wzIconPath)
    {
        hr = piShellLink->SetIconLocation(wzIconPath, iconIndex);
        ExitOnFailure2(hr, "failed to set icon for shortcut '%ls'", wzShortcutPath);
    }

    // get an IPersistFile and save the shortcut
    hr = piShellLink->QueryInterface(IID_IPersistFile, (void**)&piPersistFile);
    ExitOnFailure1(hr, "failed to get IPersistFile for shortcut '%ls'", wzShortcutPath);

    hr = piPersistFile->Save(wzShortcutPath, TRUE);
    ExitOnFailure1(hr, "failed to save shortcut '%ls'", wzShortcutPath);

LExit:
    ReleaseObject(piPersistFile);
    ReleaseObject(piShellLink);

    return hr;
}



/******************************************************************
 WixCreateInternetShortcuts - entry point for Internet shortcuts
    custom action
*******************************************************************/
extern "C" UINT __stdcall WixCreateInternetShortcuts(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwz = NULL;
    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzTarget = NULL;
    LPWSTR pwzShortcutPath = NULL;
    LPWSTR pwzIconPath = NULL;
    BOOL fInitializedCom = FALSE;
    int iAttr = 0;
    int iIconIndex = 0;

    // initialize
    hr = WcaInitialize(hInstall, "WixCreateInternetShortcuts");
    ExitOnFailure(hr, "failed to initialize WixCreateInternetShortcuts");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    // extract the custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    // loop through all the custom action data
    pwz = pwzCustomActionData;
    while (pwz && *pwz)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzShortcutPath);
        ExitOnFailure(hr, "failed to read shortcut path from custom action data");
        hr = WcaReadStringFromCaData(&pwz, &pwzTarget);
        ExitOnFailure(hr, "failed to read shortcut target from custom action data");
        hr = WcaReadIntegerFromCaData(&pwz, &iAttr);
        ExitOnFailure(hr, "failed to read shortcut attributes from custom action data");
        hr = WcaReadStringFromCaData(&pwz, &pwzIconPath);
        ExitOnFailure(hr, "failed to read shortcut icon path from custom action data");
        hr = WcaReadIntegerFromCaData(&pwz, &iIconIndex);
        ExitOnFailure(hr, "failed to read shortcut icon index from custom action data");

        if ((iAttr & esaURL) == esaURL)
        {
            hr = CreateUrl(pwzTarget, pwzShortcutPath, pwzIconPath, iIconIndex);
        }
        else
        {
            hr = CreateLink(pwzTarget, pwzShortcutPath, pwzIconPath, iIconIndex);
        }
        ExitOnFailure(hr, "failed to create Internet shortcut");

        // tick the progress bar
        hr = WcaProgressMessage(COST_INTERNETSHORTCUT, FALSE);
        ExitOnFailure1(hr, "failed to tick progress bar for shortcut: %ls", pwzShortcutPath);
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzTarget);
    ReleaseStr(pwzShortcutPath);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er;
    return WcaFinalize(er);
}



/******************************************************************
 WixRollbackInternetShortcuts - entry point for Internet shortcuts
    custom action (rollback)
*******************************************************************/
extern "C" UINT __stdcall WixRollbackInternetShortcuts(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwz = NULL;
    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzShortcutPath = NULL;
    int iAttr = 0;

    // initialize
    hr = WcaInitialize(hInstall, "WixRemoveInternetShortcuts");
    ExitOnFailure(hr, "failed to initialize WixRemoveInternetShortcuts");

    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    // loop through all the custom action data
    pwz = pwzCustomActionData;
    while (pwz && *pwz)
    {
        // extract the custom action data we're interested in
        hr = WcaReadStringFromCaData(&pwz, &pwzShortcutPath);
        ExitOnFailure(hr, "failed to read shortcut path from custom action data for rollback");

        // delete file
        hr = FileEnsureDelete(pwzShortcutPath);
        ExitOnFailure1(hr, "failed to delete file '%ls'", pwzShortcutPath);

        // skip over the shortcut target and attributes
        hr = WcaReadStringFromCaData(&pwz, &pwzShortcutPath);
        ExitOnFailure(hr, "failed to skip shortcut target from custom action data for rollback");
        hr = WcaReadIntegerFromCaData(&pwz, &iAttr);
        ExitOnFailure(hr, "failed to read shortcut attributes from custom action data");
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzShortcutPath);

    er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er;
    return WcaFinalize(er);
}
