// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsPerfCounterDataQuery = L"SELECT `PerformanceCategory`, `Component_`, `Name`, `IniData`, `ConstantData` FROM `PerformanceCategory`";
enum ePerfCounterDataQuery { pcdqId = 1, pcdqComponent, pcdqName, pcdqIniData, pcdqConstantData };

LPCWSTR vcsPerfMonQuery = L"SELECT `Component_`, `File`, `Name` FROM `Perfmon`";
enum ePerfMonQuery { pmqComponent = 1, pmqFile, pmqName };


static HRESULT ProcessPerformanceCategory(
    __in MSIHANDLE hInstall,
    __in BOOL fInstall
    );


/********************************************************************
 InstallPerfCounterData - CUSTOM ACTION ENTRY POINT for installing
                          Performance Counters.

********************************************************************/
extern "C" UINT __stdcall InstallPerfCounterData(
    __in MSIHANDLE hInstall
    )
{
    // AssertSz(FALSE, "debug InstallPerfCounterData{}");
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "InstallPerfCounterData");
    ExitOnFailure(hr, "Failed to initialize InstallPerfCounterData.");

    hr = ProcessPerformanceCategory(hInstall, TRUE);
    MessageExitOnFailure(hr, msierrInstallPerfCounterData, "Failed to process PerformanceCategory table.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 UninstallPerfCounterData - CUSTOM ACTION ENTRY POINT for installing
                          Performance Counters.

********************************************************************/
extern "C" UINT __stdcall UninstallPerfCounterData(
    __in MSIHANDLE hInstall
    )
{
    // AssertSz(FALSE, "debug UninstallPerfCounterData{}");
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "UninstallPerfCounterData");
    ExitOnFailure(hr, "Failed to initialize UninstallPerfCounterData.");

    hr = ProcessPerformanceCategory(hInstall, FALSE);
    MessageExitOnFailure(hr, msierrUninstallPerfCounterData, "Failed to process PerformanceCategory table.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 RegisterPerfmon - CUSTOM ACTION ENTRY POINT for installing Perfmon counters

********************************************************************/
extern "C" UINT __stdcall ConfigurePerfmonInstall(
    __in MSIHANDLE hInstall
    )
{
//    Assert(FALSE);
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    LPWSTR pwzData = NULL, pwzName = NULL, pwzFile = NULL;
    INSTALLSTATE isInstalled, isAction;

    hr = WcaInitialize(hInstall, "ConfigurePerfmonInstall");
    ExitOnFailure(hr, "Failed to initialize");

    // check to see if necessary tables are specified
    if (S_OK != WcaTableExists(L"Perfmon"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping RegisterPerfmon() because Perfmon table not present");
        ExitFunction1(hr = S_FALSE);
    }

    hr = WcaOpenExecuteView(vcsPerfMonQuery, &hView);
    ExitOnFailure(hr, "failed to open view on PerfMon table");
    while ((hr = WcaFetchRecord(hView, &hRec)) == S_OK)
    {
        // get component install state
        hr = WcaGetRecordString(hRec, pmqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Component for PerfMon");
        er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for PerfMon");
        if (!WcaIsInstalling(isInstalled, isAction))
        {
            continue;
        }

        hr = WcaGetRecordString(hRec, pmqName, &pwzName);
        ExitOnFailure(hr, "failed to get Name for PerfMon");

        hr = WcaGetRecordFormattedString(hRec, pmqFile, &pwzFile);
        ExitOnFailure(hr, "failed to get File for PerfMon");

        WcaLog(LOGMSG_VERBOSE, "ConfigurePerfmonInstall's CustomActionData: '%ls', '%ls'", pwzName, pwzFile);
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RegisterPerfmon"), pwzFile, COST_PERFMON_REGISTER);
        ExitOnFailure(hr, "failed to schedule RegisterPerfmon action");
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackRegisterPerfmon"), pwzName, COST_PERFMON_UNREGISTER);
        ExitOnFailure(hr, "failed to schedule RollbackRegisterPerfmon action");
    }

    if (hr == E_NOMOREITEMS)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing PerfMon");

    hr = S_OK;

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzFile);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 ConfigurePerfmonUninstall - CUSTOM ACTION ENTRY POINT for uninstalling 
                             Perfmon counters

********************************************************************/
extern "C" UINT __stdcall ConfigurePerfmonUninstall(
    __in MSIHANDLE hInstall
    )
{
//    Assert(FALSE);
    HRESULT hr;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    LPWSTR pwzData = NULL, pwzName = NULL, pwzFile = NULL;
    INSTALLSTATE isInstalled, isAction;

    hr = WcaInitialize(hInstall, "ConfigurePerfmonUninstall");
    ExitOnFailure(hr, "Failed to initialize");

    // check to see if necessary tables are specified
    if (WcaTableExists(L"Perfmon") != S_OK)
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping UnregisterPerfmon() because Perfmon table not present");
        ExitFunction1(hr = S_FALSE);
    }

    hr = WcaOpenExecuteView(vcsPerfMonQuery, &hView);
    ExitOnFailure(hr, "failed to open view on PerfMon table");
    while ((hr = WcaFetchRecord(hView, &hRec)) == S_OK)
    {
        // get component install state
        hr = WcaGetRecordString(hRec, pmqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Component for PerfMon");
        er = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for PerfMon");
        if (!WcaIsUninstalling(isInstalled, isAction))
        {
            continue;
        }

        hr = WcaGetRecordString(hRec, pmqName, &pwzName);
        ExitOnFailure(hr, "failed to get Name for PerfMon");

        hr = WcaGetRecordFormattedString(hRec, pmqFile, &pwzFile);
        ExitOnFailure(hr, "failed to get File for PerfMon");

        WcaLog(LOGMSG_VERBOSE, "ConfigurePerfmonUninstall's CustomActionData: '%ls', '%ls'", pwzName, pwzFile);
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"UnregisterPerfmon"), pwzName, COST_PERFMON_UNREGISTER);
        ExitOnFailure(hr, "failed to schedule UnregisterPerfmon action");
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackUnregisterPerfmon"), pwzFile, COST_PERFMON_REGISTER);
        ExitOnFailure(hr, "failed to schedule RollbackUnregisterPerfmon action");
    }

    if (hr == E_NOMOREITEMS)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing PerfMon");

    hr = S_OK;

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzFile);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}



static HRESULT ProcessPerformanceCategory(
    __in MSIHANDLE hInstall,
    __in BOOL fInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    LPWSTR pwzId = NULL;
    LPWSTR pwzComponent = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzData = NULL;
    INSTALLSTATE isInstalled, isAction;

    LPWSTR pwzCustomActionData = NULL;

    // check to see if necessary tables are specified
    if (S_OK != WcaTableExists(L"PerformanceCategory"))
    {
        ExitFunction1(hr = S_FALSE);
    }

    hr = WcaOpenExecuteView(vcsPerfCounterDataQuery, &hView);
    ExitOnFailure(hr, "failed to open view on PerformanceCategory table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, pcdqId, &pwzId);
        ExitOnFailure(hr, "Failed to get id for PerformanceCategory.");

        // Check to see if the Component is being installed or uninstalled
        // when we are processing the same.
        hr = WcaGetRecordString(hRec, pcdqComponent, &pwzComponent);
        ExitOnFailure1(hr, "Failed to get Component for PerformanceCategory: %ls", pwzId);

        er = ::MsiGetComponentStateW(hInstall, pwzComponent, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure1(hr, "Failed to get Component state for PerformanceCategory: %ls", pwzId);

        if ((fInstall && !WcaIsInstalling(isInstalled, isAction)) ||
            (!fInstall && !WcaIsUninstalling(isInstalled, isAction)))
        {
            continue;
        }

        hr = WcaGetRecordString(hRec, pcdqName, &pwzName);
        ExitOnFailure1(hr, "Failed to get Name for PerformanceCategory: %ls", pwzId);
        hr = WcaWriteStringToCaData(pwzName, &pwzCustomActionData);
        ExitOnFailure1(hr, "Failed to add Name to CustomActionData for PerformanceCategory: %ls", pwzId);

        hr = WcaGetRecordString(hRec, pcdqIniData, &pwzData);
        ExitOnFailure1(hr, "Failed to get IniData for PerformanceCategory: %ls", pwzId);
        hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
        ExitOnFailure1(hr, "Failed to add IniData to CustomActionData for PerformanceCategory: %ls", pwzId);

        hr = WcaGetRecordString(hRec, pcdqConstantData, &pwzData);
        ExitOnFailure1(hr, "Failed to get ConstantData for PerformanceCategory: %ls", pwzId);
        hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
        ExitOnFailure1(hr, "Failed to add ConstantData to CustomActionData for PerformanceCategory: %ls", pwzId);
    }

    if (hr == E_NOMOREITEMS)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure while processing PerformanceCategory table.");

    // If there was any data built up, schedule it for execution.
    if (pwzCustomActionData)
    {
        if (fInstall)
        {
            hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackRegisterPerfCounterData"), pwzCustomActionData, COST_PERFMON_UNREGISTER);
            ExitOnFailure1(hr, "Failed to schedule RollbackRegisterPerfCounterData action for PerformanceCategory: %ls", pwzId);

            hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RegisterPerfCounterData"), pwzCustomActionData, COST_PERFMON_REGISTER);
            ExitOnFailure1(hr, "Failed to schedule RegisterPerfCounterData action for PerformanceCategory: %ls", pwzId);
        }
        else
        {
            hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackUnregisterPerfCounterData"), pwzCustomActionData, COST_PERFMON_REGISTER);
            ExitOnFailure1(hr, "Failed to schedule RollbackUnregisterPerfCounterData action for PerformanceCategory: %ls", pwzId);

            hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"UnregisterPerfCounterData"), pwzCustomActionData, COST_PERFMON_UNREGISTER);
            ExitOnFailure1(hr, "Failed to schedule UnregisterPerfCounterData action for PerformanceCategory: %ls", pwzId);
        }
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwzId);

    return hr;
}
