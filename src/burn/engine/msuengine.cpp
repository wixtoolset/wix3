//-------------------------------------------------------------------------------------------------
// <copyright file="msuengine.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: MSU Engine
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// constants

#define WU_S_REBOOT_REQUIRED        0x00240005L
#define WU_S_ALREADY_INSTALLED      0x00240006L


// function definitions
static HRESULT EnsureWUServiceEnabled(
    __in BOOL fStopWusaService,
    __out SC_HANDLE* pschWu,
    __out BOOL* pfPreviouslyDisabled
    );
static HRESULT SetServiceStartType(
    __in SC_HANDLE sch,
    __in DWORD stratType
    );
static HRESULT StopWUService(
    __in SC_HANDLE schWu
    );


extern "C" HRESULT MsuEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnMsuPackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;

    // @KB
    hr = XmlGetAttributeEx(pixnMsuPackage, L"KB", &pPackage->Msu.sczKB);
    ExitOnFailure(hr, "Failed to get @KB.");

    // @DetectCondition
    hr = XmlGetAttributeEx(pixnMsuPackage, L"DetectCondition", &pPackage->Msu.sczDetectCondition);
    ExitOnFailure(hr, "Failed to get @DetectCondition.");

LExit:
    return hr;
}

extern "C" void MsuEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
    ReleaseNullStr(pPackage->Msu.sczKB);
    ReleaseNullStr(pPackage->Msu.sczDetectCondition);
}

extern "C" HRESULT MsuEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BOOL fDetected = FALSE;

    // evaluate detect condition
    if (pPackage->Msu.sczDetectCondition && *pPackage->Msu.sczDetectCondition)
    {
        hr = ConditionEvaluate(pVariables, pPackage->Msu.sczDetectCondition, &fDetected);
        ExitOnFailure(hr, "Failed to evaluate MSU package detect condition.");
    }

    // update detect state
    pPackage->currentState = fDetected ? BOOTSTRAPPER_PACKAGE_STATE_PRESENT : BOOTSTRAPPER_PACKAGE_STATE_ABSENT;

LExit:
    return hr;
}

//
// PlanCalculate - calculates the execute and rollback state for the requested package state.
//
extern "C" HRESULT MsuEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ACTION_STATE execute = BOOTSTRAPPER_ACTION_STATE_NONE;
    BOOTSTRAPPER_ACTION_STATE rollback = BOOTSTRAPPER_ACTION_STATE_NONE;

    BOOL fAllowUninstall = FALSE;
    OS_VERSION osVersion = OS_VERSION_UNKNOWN;
    DWORD dwServicePack = 0;

    // We can only uninstall MSU packages if they have a KB and we are on Win7 or newer.
    OsGetVersion(&osVersion, &dwServicePack);
    fAllowUninstall = (pPackage->Msu.sczKB && *pPackage->Msu.sczKB) && OS_VERSION_WIN7 <= osVersion;

    // execute action
    switch (pPackage->currentState)
    {
    case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;

        case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
            execute = fAllowUninstall && pPackage->fUninstallable ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;

        case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
            execute = fAllowUninstall ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;

        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
        }
        break;

    case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
            break;

        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid package state.");
    }

    // Calculate the rollback action if there is an execute action.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != execute)
    {
        switch (BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN != pPackage->expected ? pPackage->expected : pPackage->currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
            switch (pPackage->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
                rollback = BOOTSTRAPPER_ACTION_STATE_INSTALL;
                break;

            default:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
            switch (pPackage->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                rollback = fAllowUninstall ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
                break;

            default:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnRootFailure(hr, "Invalid package expected state.");
        }
    }

    // return values
    pPackage->execute = execute;
    pPackage->rollback = rollback;

LExit:
    return hr;
}

//
// PlanAdd - adds the calculated execute and rollback actions for the package.
//
extern "C" HRESULT MsuEnginePlanAddPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    // add wait for cache
    if (hCacheEvent)
    {
        hr = PlanExecuteCacheSyncAndRollback(pPlan, pPackage, hCacheEvent, fPlanPackageCacheRollback);
        ExitOnFailure(hr, "Failed to plan package cache syncpoint");
    }

    hr = DependencyPlanPackage(NULL, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

    // add execute action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute)
    {
        hr = PlanAppendExecuteAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append execute action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE;
        pAction->msuPackage.pPackage = pPackage;
        pAction->msuPackage.action = pPackage->execute;

        LoggingSetPackageVariable(pPackage, NULL, FALSE, pLog, pVariables, &pAction->msuPackage.sczLogPath); // ignore errors.
    }

    // add rollback action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE;
        pAction->msuPackage.pPackage = pPackage;
        pAction->msuPackage.action = pPackage->rollback;

        LoggingSetPackageVariable(pPackage, NULL, TRUE, pLog, pVariables, &pAction->msuPackage.sczLogPath); // ignore errors.
    }

LExit:
    return hr;
}

extern "C" HRESULT MsuEngineExecutePackage(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in BOOL fStopWusaService,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    int nResult = IDNOACTION;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczMsuPath = NULL;
    LPWSTR sczWindowsPath = NULL;
    LPWSTR sczSystemPath = NULL;
    LPWSTR sczWusaPath = NULL;
    LPWSTR sczCommand = NULL;
    SC_HANDLE schWu = NULL;
    BOOL fWuWasDisabled = FALSE;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };
    GENERIC_EXECUTE_MESSAGE message = { };
    DWORD dwExitCode = 0;
    BOOL fUseSysNativePath = FALSE;

#if !defined(_WIN64)
    hr = ProcWow64(::GetCurrentProcess(), &fUseSysNativePath);
    ExitOnFailure(hr, "Failed to determine WOW64 status.");
#endif

    *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;

    // get wusa.exe path
    if (fUseSysNativePath)
    {
        hr = PathGetKnownFolder(CSIDL_WINDOWS, &sczWindowsPath);
        ExitOnFailure(hr, "Failed to find Windows directory.");

        hr = PathConcat(sczWindowsPath, L"SysNative\\", &sczSystemPath);
        ExitOnFailure(hr, "Failed to append SysNative directory.");
    }
    else
    {
        hr = PathGetKnownFolder(CSIDL_SYSTEM, &sczSystemPath);
        ExitOnFailure(hr, "Failed to find System32 directory.");
    }

    hr = PathConcat(sczSystemPath, L"wusa.exe", &sczWusaPath);
    ExitOnFailure(hr, "Failed to allocate WUSA.exe path.");

    // build command
    switch (pExecuteAction->msuPackage.action)
    {
    case BOOTSTRAPPER_ACTION_STATE_INSTALL:
        // get cached MSU path
        hr = CacheGetCompletedPath(TRUE, pExecuteAction->msuPackage.pPackage->sczCacheId, &sczCachedDirectory);
        ExitOnFailure1(hr, "Failed to get cached path for package: %ls", pExecuteAction->msuPackage.pPackage->sczId);

        hr = PathBackslashTerminate(&sczCachedDirectory);
        ExitOnFailure(hr, "Failed to backslashify.");

        // Best effort to set the execute package cache folder variable.
        VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, sczCachedDirectory, TRUE);

        hr = PathConcat(sczCachedDirectory, pExecuteAction->msuPackage.pPackage->rgPayloads[0].pPayload->sczFilePath, &sczMsuPath);
        ExitOnFailure(hr, "Failed to build MSU path.");

        // format command
        hr = StrAllocFormatted(&sczCommand, L"\"%ls\" \"%ls\" /quiet /norestart", sczWusaPath, sczMsuPath);
        ExitOnFailure(hr, "Failed to format MSU install command.");
        break;

    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        // format command
        hr = StrAllocFormatted(&sczCommand, L"\"%ls\" /uninstall /kb:%ls /quiet /norestart", sczWusaPath, pExecuteAction->msuPackage.pPackage->Msu.sczKB);
        ExitOnFailure(hr, "Failed to format MSU uninstall command.");
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Failed to get action arguments for MSU package.");
    }

    if (pExecuteAction->msuPackage.sczLogPath && *pExecuteAction->msuPackage.sczLogPath)
    {
        hr = StrAllocConcat(&sczCommand, L" /log:", 0);
        ExitOnFailure(hr, "Failed to append log switch to MSU command-line.");

        hr = StrAllocConcat(&sczCommand, pExecuteAction->msuPackage.sczLogPath, 0);
        ExitOnFailure(hr, "Failed to append log path to MSU command-line.");
    }

    LogId(REPORT_STANDARD, MSG_APPLYING_PACKAGE, LoggingRollbackOrExecute(fRollback), pExecuteAction->msuPackage.pPackage->sczId, LoggingActionStateToString(pExecuteAction->msuPackage.action), sczMsuPath ? sczMsuPath : pExecuteAction->msuPackage.pPackage->Msu.sczKB, sczCommand);

    hr = EnsureWUServiceEnabled(fStopWusaService, &schWu, &fWuWasDisabled);
    ExitOnFailure(hr, "Failed to ensure WU service was enabled to install MSU package.");

    // create process
    si.cb = sizeof(si);
    if (!::CreateProcessW(sczWusaPath, sczCommand, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi))
    {
        ExitWithLastError1(hr, "Failed to CreateProcess on path: %ls", sczWusaPath);
    }

    do
    {
        message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
        message.dwAllowedResults = MB_OKCANCEL;
        message.progress.dwPercentage = 50;
        nResult = pfnGenericMessageHandler(&message, pvContext);
        hr = (IDOK == nResult || IDNOACTION == nResult) ? S_OK : IDCANCEL == nResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
        ExitOnRootFailure(hr, "Bootstrapper application aborted during MSU progress.");

        // wait for process to terminate
        hr = ProcWaitForCompletion(pi.hProcess, 500, &dwExitCode);
        if (HRESULT_FROM_WIN32(WAIT_TIMEOUT) != hr)
        {
            ExitOnFailure1(hr, "Failed to wait for executable to complete: %ls", sczWusaPath);
        }
    } while (HRESULT_FROM_WIN32(WAIT_TIMEOUT) == hr);

    // get process exit code
    if (!::GetExitCodeProcess(pi.hProcess, &dwExitCode))
    {
        ExitWithLastError(hr, "Failed to get process exit code.");
    }

    // We'll normalize the restart required error code from wusa.exe just in case. Most likely
    // that on reboot we'll actually get WU_S_REBOOT_REQUIRED.
    if (HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED) == static_cast<HRESULT>(dwExitCode))
    {
        dwExitCode = ERROR_SUCCESS_REBOOT_REQUIRED;
    }

    // handle exit code
    switch (dwExitCode)
    {
    case S_OK: __fallthrough;
    case S_FALSE: __fallthrough;
    case WU_S_ALREADY_INSTALLED:
        hr = S_OK;
        break;

    case ERROR_SUCCESS_REBOOT_REQUIRED: __fallthrough;
    case WU_S_REBOOT_REQUIRED:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_REQUIRED;
        hr = S_OK;
        break;

    default:
        hr = static_cast<HRESULT>(dwExitCode);
        break;
    }

LExit:
    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczMsuPath);
    ReleaseStr(sczSystemPath);
    ReleaseStr(sczWindowsPath);
    ReleaseStr(sczWusaPath);
    ReleaseStr(sczCommand);

    ReleaseHandle(pi.hProcess);
    ReleaseHandle(pi.hThread);

    if (fWuWasDisabled)
    {
        SetServiceStartType(schWu, SERVICE_DISABLED);
    }

    // Best effort to clear the execute package cache folder variable.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, NULL, TRUE);

    return hr;
}

static HRESULT EnsureWUServiceEnabled(
    __in BOOL fStopWusaService,
    __out SC_HANDLE* pschWu,
    __out BOOL* pfPreviouslyDisabled
    )
{
    HRESULT hr = S_OK;
    SC_HANDLE schSCM = NULL;
    SC_HANDLE schWu = NULL;
    SERVICE_STATUS serviceStatus = { };
    QUERY_SERVICE_CONFIGW* pConfig = NULL;

    schSCM = ::OpenSCManagerW(NULL, NULL, SC_MANAGER_ALL_ACCESS);
    ExitOnNullWithLastError(schSCM, hr, "Failed to open service control manager.");

    schWu = ::OpenServiceW(schSCM, L"wuauserv", SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG | SERVICE_QUERY_STATUS | SERVICE_STOP );
    ExitOnNullWithLastError(schWu, hr, "Failed to open WU service.");

    if (!::QueryServiceStatus(schWu, &serviceStatus) )
    {
        ExitWithLastError(hr, "Failed to query status of WU service.");
    }

    // Stop service if requested to.
    if (SERVICE_STOPPED != serviceStatus.dwCurrentState && fStopWusaService)
    {
        hr = StopWUService(schWu);
    }

    // If the service is not running then it might be disabled so let's check.
    if (SERVICE_RUNNING != serviceStatus.dwCurrentState)
    {
        hr = SvcQueryConfig(schWu, &pConfig);
        ExitOnFailure(hr, "Failed to read configuration for WU service.");

        // If WU is disabled, change it to a demand start service (but touch nothing else).
        if (SERVICE_DISABLED == pConfig->dwStartType)
        {
            hr = SetServiceStartType(schWu, SERVICE_DEMAND_START);
            ExitOnFailure(hr, "Failed to mark WU service to start on demand.");

            *pfPreviouslyDisabled = TRUE;
        }
    }

    *pschWu = schWu;
    schWu = NULL;

LExit:
    ReleaseMem(pConfig);
    ReleaseServiceHandle(schWu);
    ReleaseServiceHandle(schSCM);

    return hr;
}

static HRESULT SetServiceStartType(
    __in SC_HANDLE sch,
    __in DWORD startType
    )
{
    HRESULT hr = S_OK;

    if (!::ChangeServiceConfigW(sch, SERVICE_NO_CHANGE, startType, SERVICE_NO_CHANGE, NULL, NULL, NULL, NULL, NULL, NULL, NULL))
    {
        ExitWithLastError(hr, "Failed to set service start type.");
    }

LExit:
    return hr;
}

static HRESULT StopWUService(
    __in SC_HANDLE schWu
    )
{
    HRESULT hr = S_OK;
    SERVICE_STATUS serviceStatus = { };

    if(!::ControlService(schWu, SERVICE_CONTROL_STOP, &serviceStatus))
    {
        ExitWithLastError(hr, "Failed to stop wusa service.");
    }

LExit:
    return hr;
}
