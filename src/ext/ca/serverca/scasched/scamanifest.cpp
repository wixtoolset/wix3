// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsPerfmonManifestQuery = L"SELECT `Component_`, `File`, `ResourceFileDirectory` FROM `PerfmonManifest`";
LPCWSTR vcsEventManifestQuery = L"SELECT `Component_`, `File` FROM `EventManifest`";
enum ePerfMonManifestQuery { pfmComponent = 1, pfmFile, pfmResourceFileDir };
enum eEventManifestQuery { emComponent = 1, emFile};


/********************************************************************
 ConfigurePerfmonManifestRegister - CUSTOM ACTION ENTRY POINT for scheduling
 Perfmon counter manifest registering
 
********************************************************************/
extern "C" UINT __stdcall ConfigurePerfmonManifestRegister(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    LPWSTR pwzData = NULL, pwzResourceFilePath = NULL, pwzFile = NULL, pwzCommand = NULL;
    INSTALLSTATE isInstalled, isAction;

    hr = WcaInitialize(hInstall, "ConfigurePerfmonManifestReg");
    ExitOnFailure(hr, "Failed to initialize");

    if (!IsWindowsVistaOrGreater())
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigurePerfmonManifestRegister() because the target system does not support perfmon manifest");
        ExitFunction1(hr = S_FALSE);
    }
    // check to see if necessary tables are specified
    if (S_OK != WcaTableExists(L"PerfmonManifest"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigurePerfmonManifestRegister() because PerfmonManifest table not present");
        ExitFunction1(hr = S_FALSE);
    }

    hr = WcaOpenExecuteView(vcsPerfmonManifestQuery, &hView);
    ExitOnFailure(hr, "failed to open view on PerfMonManifest table");
    while ((hr = WcaFetchRecord(hView, &hRec)) == S_OK)
    {
        // get component install state
        hr = WcaGetRecordString(hRec, pfmComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Component for PerfMonManifest");
        er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for PerfMonManifest");
        if (!WcaIsInstalling(isInstalled, isAction))
        {
            continue;
        }

        hr = WcaGetRecordFormattedString(hRec, pfmFile, &pwzFile);
        ExitOnFailure(hr, "failed to get File for PerfMonManifest");

        hr = WcaGetRecordFormattedString(hRec, pfmResourceFileDir, &pwzResourceFilePath);
        ExitOnFailure(hr, "failed to get ApplicationIdentity for PerfMonManifest");
        size_t iResourcePath = lstrlenW(pwzResourceFilePath);
        if ( iResourcePath > 0 && *(pwzResourceFilePath + iResourcePath -1) == L'\\') 
            *(pwzResourceFilePath + iResourcePath -1) = 0;  //remove the trailing '\'

        hr = StrAllocFormatted(&pwzCommand, L"\"unlodctr.exe\" /m:\"%s\"", pwzFile);
        ExitOnFailure(hr, "failed to copy string in PerfMonManifest");

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackRegisterPerfmonManifest"), pwzCommand, COST_PERFMONMANIFEST_UNREGISTER);
        ExitOnFailure(hr, "failed to schedule RollbackRegisterPerfmonManifest action");

        if ( *pwzResourceFilePath )
        {
            hr = StrAllocFormatted(&pwzCommand, L"\"lodctr.exe\" /m:\"%s\" \"%s\"", pwzFile, pwzResourceFilePath);
            ExitOnFailure(hr, "failed to copy string in PerfMonManifest");
        }
        else
        {
            hr = StrAllocFormatted(&pwzCommand, L"\"lodctr.exe\" /m:\"%s\"", pwzFile);
            ExitOnFailure(hr, "failed to copy string in PerfMonManifest");
        }
        
        WcaLog(LOGMSG_VERBOSE, "RegisterPerfmonManifest's CustomActionData: '%ls'", pwzCommand);
        
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RegisterPerfmonManifest"), pwzCommand, COST_PERFMONMANIFEST_REGISTER);
        ExitOnFailure(hr, "failed to schedule RegisterPerfmonManifest action");
    }

    if (hr == E_NOMOREITEMS)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing PerfMonManifest");

    hr = S_OK;

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzResourceFilePath);
    ReleaseStr(pwzFile);
    ReleaseStr(pwzCommand);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 ConfigurePerfmonUninstall - CUSTOM ACTION ENTRY POINT for uninstalling 
                             Perfmon counters

********************************************************************/
extern "C" UINT __stdcall ConfigurePerfmonManifestUnregister(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    LPWSTR pwzData = NULL, pwzResourceFilePath = NULL, pwzFile = NULL, pwzCommand = NULL;
    INSTALLSTATE isInstalled, isAction;

    hr = WcaInitialize(hInstall, "ConfigurePerfmonManifestUnreg");
    ExitOnFailure(hr, "Failed to initialize");

    if (!IsWindowsVistaOrGreater())
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigurePerfmonManifestUnregister() because the target system does not support perfmon manifest");
        ExitFunction1(hr = S_FALSE);
    }
    // check to see if necessary tables are specified
    if (WcaTableExists(L"PerfmonManifest") != S_OK)
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigurePerfmonManifestUnregister() because PerfmonManifest table not present");
        ExitFunction1(hr = S_FALSE);
    }

    hr = WcaOpenExecuteView(vcsPerfmonManifestQuery, &hView);
    ExitOnFailure(hr, "failed to open view on PerfMonManifest table");
    while ((hr = WcaFetchRecord(hView, &hRec)) == S_OK)
    {
        // get component install state
        hr = WcaGetRecordString(hRec, pfmComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Component for PerfMonManifest");
        er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for PerfMonManifest");
        if (!WcaIsUninstalling(isInstalled, isAction))
        {
            continue;
        }

        hr = WcaGetRecordFormattedString(hRec, pfmFile, &pwzFile);
        ExitOnFailure(hr, "failed to get File for PerfMonManifest");

        hr = WcaGetRecordFormattedString(hRec, pfmResourceFileDir, &pwzResourceFilePath);
        ExitOnFailure(hr, "failed to get ApplicationIdentity for PerfMonManifest");
        size_t iResourcePath = lstrlenW(pwzResourceFilePath);
        if ( iResourcePath > 0 && *(pwzResourceFilePath + iResourcePath -1) == L'\\') 
            *(pwzResourceFilePath + iResourcePath -1) = 0;  //remove the trailing '\'

        hr = StrAllocFormatted(&pwzCommand, L"\"lodctr.exe\" /m:\"%s\" \"%s\"", pwzFile, pwzResourceFilePath);
        ExitOnFailure(hr, "failed to copy string in PerfMonManifest");

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackUnregisterPerfmonManifest"), pwzCommand, COST_PERFMONMANIFEST_REGISTER);
        ExitOnFailure(hr, "failed to schedule RollbackUnregisterPerfmonManifest action");

        hr = StrAllocFormatted(&pwzCommand, L"\"unlodctr.exe\" /m:\"%s\"", pwzFile);
        ExitOnFailure(hr, "failed to copy string in PerfMonManifest");

        WcaLog(LOGMSG_VERBOSE, "UnRegisterPerfmonManifest's CustomActionData: '%ls'", pwzCommand);
        
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"UnregisterPerfmonManifest"), pwzCommand, COST_PERFMONMANIFEST_UNREGISTER);
        ExitOnFailure(hr, "failed to schedule UnregisterPerfmonManifest action");
    }

    if (hr == E_NOMOREITEMS)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing PerfMonManifest");

    hr = S_OK;

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzResourceFilePath);
    ReleaseStr(pwzFile);
    ReleaseStr(pwzCommand);
    
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 ConfigureEventManifestRegister - CUSTOM ACTION ENTRY POINT for scheduling
 Event manifest registering
 
********************************************************************/
extern "C" UINT __stdcall ConfigureEventManifestRegister(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    LPWSTR pwzData = NULL, pwzFile = NULL, pwzCommand = NULL;
    INSTALLSTATE isInstalled, isAction;

    hr = WcaInitialize(hInstall, "ConfigureEventManifestReg");
    ExitOnFailure(hr, "Failed to initialize");
    
    if (!IsWindowsVistaOrGreater())
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigureEventManifestRegister() because the target system does not support event manifest");
        ExitFunction1(hr = S_FALSE);
    }
    // check to see if necessary tables are specified
    if (S_OK != WcaTableExists(L"EventManifest"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigureEventManifestRegister() because EventManifest table not present");
        ExitFunction1(hr = S_FALSE);
    }

    hr = WcaOpenExecuteView(vcsEventManifestQuery, &hView);
    ExitOnFailure(hr, "failed to open view on EventManifest table");
    while ((hr = WcaFetchRecord(hView, &hRec)) == S_OK)
    {
        // get component install state
        hr = WcaGetRecordString(hRec, emComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Component for EventManifest");
        er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for EventManifest");
        if (!WcaIsInstalling(isInstalled, isAction))
        {
            continue;
        }

        hr = WcaGetRecordFormattedString(hRec, emFile, &pwzFile);
        ExitOnFailure(hr, "failed to get File for EventManifest");

        hr = StrAllocFormatted(&pwzCommand, L"\"wevtutil.exe\" um \"%s\"", pwzFile);
        ExitOnFailure(hr, "failed to copy string in EventManifest");

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackRegisterEventManifest"), pwzCommand, COST_PERFMONMANIFEST_UNREGISTER);
        ExitOnFailure(hr, "failed to schedule RollbackRegisterEventManifest action");

        hr = StrAllocFormatted(&pwzCommand, L"\"wevtutil.exe\" im \"%s\"", pwzFile);
        ExitOnFailure(hr, "failed to copy string in EventManifest");
        WcaLog(LOGMSG_VERBOSE, "RegisterEventManifest's CustomActionData: '%ls'", pwzCommand);
        
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RegisterEventManifest"), pwzCommand, COST_EVENTMANIFEST_REGISTER);
        ExitOnFailure(hr, "failed to schedule RegisterEventManifest action");
    }

    if (hr == E_NOMOREITEMS)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing EventManifest");

    hr = S_OK;

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzFile);
    ReleaseStr(pwzCommand);
    
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}



/********************************************************************
 ConfigureEventManifestRegister - CUSTOM ACTION ENTRY POINT for scheduling
 Event manifest registering
 
********************************************************************/
extern "C" UINT __stdcall ConfigureEventManifestUnregister(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    LPWSTR pwzData = NULL, pwzFile = NULL, pwzCommand = NULL;
    INSTALLSTATE isInstalled, isAction;

    hr = WcaInitialize(hInstall, "ConfigureEventManifestUnreg");
    ExitOnFailure(hr, "Failed to initialize");
    
    if (!IsWindowsVistaOrGreater())
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigureEventManifestUnregister() because the target system does not support event manifest");
        ExitFunction1(hr = S_FALSE);
    }
    // check to see if necessary tables are specified
    if (S_OK != WcaTableExists(L"EventManifest"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigureEventManifestUnregister() because EventManifest table not present");
        ExitFunction1(hr = S_FALSE);
    }

    hr = WcaOpenExecuteView(vcsEventManifestQuery, &hView);
    ExitOnFailure(hr, "failed to open view on EventManifest table");
    while ((hr = WcaFetchRecord(hView, &hRec)) == S_OK)
    {
        // get component install state
        hr = WcaGetRecordString(hRec, emComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Component for EventManifest");
        er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for EventManifest");

        // nothing to do on an install
        // schedule the rollback action when reinstalling to re-register pre-patch manifest
        if (!WcaIsUninstalling(isInstalled, isAction) && !WcaIsReInstalling(isInstalled, isAction))
        {
            continue;
        }

        hr = WcaGetRecordFormattedString(hRec, emFile, &pwzFile);
        ExitOnFailure(hr, "failed to get File for EventManifest");

        hr = StrAllocFormatted(&pwzCommand, L"\"wevtutil.exe\" im \"%s\"", pwzFile);
        ExitOnFailure(hr, "failed to copy string in EventManifest");

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackUnregisterEventManifest"), pwzCommand, COST_PERFMONMANIFEST_REGISTER);
        ExitOnFailure(hr, "failed to schedule RollbackUnregisterEventManifest action");

        // no need to uninstall on a repair/patch.  Register action will re-register and update the manifest.
        if (!WcaIsReInstalling(isInstalled, isAction))
        {
            hr = StrAllocFormatted(&pwzCommand, L"\"wevtutil.exe\" um \"%s\"", pwzFile);
            ExitOnFailure(hr, "failed to copy string in EventManifest");
            WcaLog(LOGMSG_VERBOSE, "UnregisterEventManifest's CustomActionData: '%ls'", pwzCommand);
            
            hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"UnregisterEventManifest"), pwzCommand, COST_PERFMONMANIFEST_UNREGISTER);
            ExitOnFailure(hr, "failed to schedule UnregisterEventManifest action");
        }
    }

    if (hr == E_NOMOREITEMS)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing EventManifest");

    hr = S_OK;

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzFile);
    ReleaseStr(pwzCommand);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

