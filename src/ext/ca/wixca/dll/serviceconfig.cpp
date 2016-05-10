// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// structs
LPCWSTR wzQUERY_SERVICECONFIG = L"SELECT `ServiceName`, `Component_`, `NewService`, `FirstFailureActionType`, `SecondFailureActionType`, `ThirdFailureActionType`, `ResetPeriodInDays`, `RestartServiceDelayInSeconds`, `ProgramCommandLine`, `RebootMessage` FROM `ServiceConfig`";
enum eQUERY_SERVICECONFIG { QSC_SERVICENAME = 1, QSC_COMPONENT, QSC_NEWSERVICE, QSC_FIRSTFAILUREACTIONTYPE, QSC_SECONDFAILUREACTIONTYPE, QSC_THIRDFAILUREACTIONTYPE, QSC_RESETPERIODINDAYS, QSC_RESTARTSERVICEDELAYINSECONDS, QSC_PROGRAMCOMMANDLINE, QSC_REBOOTMESSAGE };

// consts
LPCWSTR c_wzActionTypeNone = L"none";
LPCWSTR c_wzActionTypeReboot = L"reboot";
LPCWSTR c_wzActionTypeRestart = L"restart";
LPCWSTR c_wzActionTypeRunCommand = L"runCommand";

// prototypes
static SC_ACTION_TYPE GetSCActionType(
    __in LPCWSTR pwzActionTypeName
    );

static HRESULT GetSCActionTypeString(
    __in SC_ACTION_TYPE type,
    __out_ecount(cchActionTypeString) LPWSTR wzActionTypeString,
    __in DWORD cchActionTypeString
    );

static HRESULT GetService(
    __in SC_HANDLE hSCM,
    __in LPCWSTR wzService,
    __in DWORD dwOpenServiceAccess,
    __out SC_HANDLE* phService
    );

static HRESULT ConfigureService(
    __in SC_HANDLE hSCM,
    __in SC_HANDLE hService,
    __in LPCWSTR wzServiceName,
    __in DWORD dwRestartServiceDelayInSeconds,
    __in LPCWSTR wzFirstFailureActionType,
    __in LPCWSTR wzSecondFailureActionType,
    __in LPCWSTR wzThirdFailureActionType,
    __in DWORD dwResetPeriodInDays,
    __in LPWSTR wzRebootMessage,
    __in LPWSTR wzProgramCommandLine
    );


/******************************************************************
SchedServiceConfig - entry point for SchedServiceConfig Custom Action

called as Type 1 CustomAction (binary DLL) from Windows Installer 
in InstallExecuteSequence before CaExecServiceConfig
********************************************************************/
extern "C" UINT __stdcall SchedServiceConfig(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug SchedServiceConfig");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzCustomActionData = NULL;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;
    LPWSTR pwzData = NULL;
    int iData = 0;
    DWORD cServices = 0;

    // initialize
    hr = WcaInitialize(hInstall, "SchedServiceConfig");
    ExitOnFailure(hr, "Failed to initialize.");

    // Get the script key for this CustomAction and put it on the front of the
    // CustomActionData of the install action.
    hr = WcaCaScriptCreateKey(&pwzScriptKey);
    ExitOnFailure(hr, "Failed to get encoding key.");

    hr = WcaWriteStringToCaData(pwzScriptKey, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add encoding key to CustomActionData.");

    // Loop through all the services to be configured.
    hr = WcaOpenExecuteView(wzQUERY_SERVICECONFIG, &hView);
    ExitOnFailure(hr, "Failed to open view on ServiceConfig table.");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
        INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

        // Get component name to check if we are installing it. If so
        // then add the table data to the CustomActionData, otherwise
        // skip it.
        hr = WcaGetRecordString(hRec, QSC_COMPONENT, &pwzData);
        ExitOnFailure(hr, "Failed to get component name");

        hr = ::MsiGetComponentStateW(hInstall, pwzData, &isInstalled, &isAction);
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(hr), "Failed to get install state for Component: %ls", pwzData);

        if (WcaIsInstalling(isInstalled, isAction))
        {
            // Add the data to the CustomActionData (for install).
            hr = WcaGetRecordFormattedString(hRec, QSC_SERVICENAME, &pwzData);
            ExitOnFailure(hr, "Failed to get name of service.");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "Failed to add name to CustomActionData.");

            hr = WcaGetRecordInteger(hRec, QSC_NEWSERVICE, &iData);
            ExitOnFailure(hr, "Failed to get ServiceConfig.NewService.");
            hr = WcaWriteIntegerToCaData(0 != iData, &pwzCustomActionData);
            ExitOnFailure(hr, "Failed to add NewService data to CustomActionData");

            hr = WcaGetRecordString(hRec, QSC_FIRSTFAILUREACTIONTYPE, &pwzData);
            ExitOnFailure(hr, "failed to get first failure action type");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordString(hRec, QSC_SECONDFAILUREACTIONTYPE, &pwzData);
            ExitOnFailure(hr, "failed to get second failure action type");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordString(hRec, QSC_THIRDFAILUREACTIONTYPE, &pwzData);
            ExitOnFailure(hr, "failed to get third failure action type");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordInteger(hRec, QSC_RESETPERIODINDAYS, &iData);
            if (S_FALSE == hr) // deal w/ possible null value
            {
                iData = 0;
            }
            ExitOnFailure(hr, "failed to get reset period in days between service restart attempts.");
            hr = WcaWriteIntegerToCaData(iData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordInteger(hRec, QSC_RESTARTSERVICEDELAYINSECONDS, &iData);
            if (S_FALSE == hr) // deal w/ possible null value
            {
                iData = 0;
            }
            ExitOnFailure(hr, "failed to get server restart delay value.");
            hr = WcaWriteIntegerToCaData(iData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordFormattedString(hRec, QSC_PROGRAMCOMMANDLINE, &pwzData); // null value already dealt w/ properly
            ExitOnFailure(hr, "failed to get command line to run on service failure.");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaGetRecordString(hRec, QSC_REBOOTMESSAGE, &pwzData); // null value already dealt w/ properly
            ExitOnFailure(hr, "failed to get message to send to users when server reboots due to service failure.");
            hr = WcaWriteStringToCaData(pwzData, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            ++cServices;
        }
    }

    // if we looped through all records all is well
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed while looping through all objects to secure");

    // setup CustomActionData and add to progress bar for download
    if (0 < cServices)
    {
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RollbackServiceConfig"), pwzScriptKey, cServices * COST_SERVICECONFIG);
        ExitOnFailure(hr, "failed to schedule RollbackServiceConfig action");

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"ExecServiceConfig"), pwzCustomActionData, cServices * COST_SERVICECONFIG);
        ExitOnFailure(hr, "failed to schedule ExecServiceConfig action");
    }

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzScriptKey);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/******************************************************************
CaExecServiceConfig - entry point for ServiceConfig Custom Action.

NOTE: deferred CustomAction since it modifies the machine
NOTE: CustomActionData == wzServiceName\tfNewService\twzFirstFailureActionType\twzSecondFailureActionType\twzThirdFailureActionType\tdwResetPeriodInDays\tdwRestartServiceDelayInSeconds\twzProgramCommandLine\twzRebootMessage\twzServiceName\tfNewService\t...
*******************************************************************/
extern "C" UINT __stdcall ExecServiceConfig(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug ExecServiceConfig");
    HRESULT hr = S_OK;
    DWORD er = 0;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwz = NULL;

    LPWSTR pwzScriptKey = NULL;
    WCA_CASCRIPT_HANDLE hRollbackScript = NULL;

    LPWSTR pwzServiceName = NULL;
    BOOL fNewService = FALSE;
    LPWSTR pwzFirstFailureActionType = NULL;
    LPWSTR pwzSecondFailureActionType = NULL;
    LPWSTR pwzThirdFailureActionType = NULL;
    LPWSTR pwzProgramCommandLine = NULL;
    LPWSTR pwzRebootMessage = NULL;
    DWORD dwResetPeriodInDays = 0;
    DWORD dwRestartServiceDelayInSeconds = 0;

    LPVOID lpMsgBuf = NULL;
    SC_HANDLE hSCM = NULL;
    SC_HANDLE hService = NULL;

    DWORD dwRestartDelay = 0;
    WCHAR wzActionName[32] = { 0 };

    DWORD cbExistingServiceConfig = 0;

    SERVICE_FAILURE_ACTIONSW* psfa = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ExecServiceConfig");
    ExitOnFailure(hr, "failed to initialize");

    // Open the Services Control Manager up front.
    hSCM = ::OpenSCManagerW(NULL, NULL, SC_MANAGER_CONNECT);
    if (NULL == hSCM)
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);

#pragma prefast(push)
#pragma prefast(disable:25028)
        ::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, er, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPWSTR)&lpMsgBuf, 0, NULL);
#pragma prefast(pop)

        ExitOnFailure1(hr, "Failed to get handle to SCM. Error: %ls", (LPWSTR)lpMsgBuf);
    }

    // First, get the script key out of the CustomActionData and
    // use that to create the rollback script for this action.
    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    pwz = pwzCustomActionData;

    hr = WcaReadStringFromCaData(&pwz, &pwzScriptKey);
    if (!pwzScriptKey)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Failed due to unexpected CustomActionData passed.");
    }
    ExitOnFailure(hr, "Failed to read encoding key from CustomActionData.");

    hr = WcaCaScriptCreate(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, FALSE, &hRollbackScript);
    ExitOnFailure(hr, "Failed to open rollback CustomAction script.");

    // Next, loop through the rest of the CustomActionData, processing
    // each service config row in turn.
    while (pwz && *pwz)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzServiceName);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&fNewService));
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzFirstFailureActionType);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzSecondFailureActionType);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzThirdFailureActionType);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwResetPeriodInDays));
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwRestartServiceDelayInSeconds));
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzProgramCommandLine);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzRebootMessage);
        ExitOnFailure(hr, "failed to process CustomActionData");

        WcaLog(LOGMSG_VERBOSE, "Configuring Service: %ls", pwzServiceName);

        // Open the handle with all the permissions we might need:
        //  SERVICE_QUERY_CONFIG is needed for QueryServiceConfig2().
        //  SERVICE_CHANGE_CONFIG is needed for ChangeServiceConfig2().
        //  SERVICE_START is required in order to handle SC_ACTION_RESTART action.
        hr = GetService(hSCM, pwzServiceName, SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG | SERVICE_START, &hService);
        ExitOnFailure1(hr, "Failed to get service: %ls", pwzServiceName);

        // If we are configuring a service that existed on the machine, we need to
        // read the existing service configuration and write it out to the rollback
        // log so rollback can put it back if anything goes wrong.
        if (!fNewService)
        {
            // First, read the existing service config.
            if (!::QueryServiceConfig2W(hService, SERVICE_CONFIG_FAILURE_ACTIONS, NULL, 0, &cbExistingServiceConfig) && ERROR_INSUFFICIENT_BUFFER != ::GetLastError())
            {
                ExitWithLastError(hr, "Failed to get current service config info.");
            }

            psfa = static_cast<LPSERVICE_FAILURE_ACTIONSW>(MemAlloc(cbExistingServiceConfig, TRUE));
            ExitOnNull(psfa, hr, E_OUTOFMEMORY, "failed to allocate memory for service failure actions.");

            if (!::QueryServiceConfig2W(hService, SERVICE_CONFIG_FAILURE_ACTIONS, (LPBYTE)psfa, cbExistingServiceConfig, &cbExistingServiceConfig))
            {
                ExitOnLastError(hr, "failed to Query Service.");
            }

            // Build up rollback log so we can restore service state if necessary
            hr = WcaCaScriptWriteString(hRollbackScript, pwzServiceName);
            ExitOnFailure(hr, "Failed to add service name to Rollback Log");

            // If this service struct is empty, fill in default values
            if (3 > psfa->cActions)
            {
                hr = WcaCaScriptWriteString(hRollbackScript, c_wzActionTypeNone);
                ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

                hr = WcaCaScriptWriteString(hRollbackScript, c_wzActionTypeNone);
                ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

                hr = WcaCaScriptWriteString(hRollbackScript, c_wzActionTypeNone);
                ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");
            }
            else
            {
                // psfa actually had actions defined, so use the first three.
                for (int i = 0; i < 3; ++i)
                {
                    hr = GetSCActionTypeString(psfa->lpsaActions[i].Type, wzActionName, countof(wzActionName));
                    ExitOnFailure(hr, "failed to query SFA object");

                    if (SC_ACTION_RESTART == psfa->lpsaActions[i].Type)
                    {
                        dwRestartDelay = psfa->lpsaActions[i].Delay / 1000;
                    }

                    hr = WcaCaScriptWriteString(hRollbackScript, wzActionName);
                    ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");
                }
            }

            hr = WcaCaScriptWriteNumber(hRollbackScript, psfa->dwResetPeriod / (24 * 60 * 60));
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            hr = WcaCaScriptWriteNumber(hRollbackScript, dwRestartDelay);
            ExitOnFailure(hr, "failed to add data to CustomActionData");

            // Handle the null cases.
            if (!psfa->lpCommand)
            {
                psfa->lpCommand = L"";
            }
            hr = WcaCaScriptWriteString(hRollbackScript, psfa->lpCommand);
            ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

            // Handle the null cases.
            if (!psfa->lpRebootMsg)
            {
                psfa->lpRebootMsg = L"";
            }
            hr = WcaCaScriptWriteString(hRollbackScript, psfa->lpRebootMsg);
            ExitOnFailure(hr, "failed to add data to Rollback CustomActionData");

            // Nudge the system to get all our rollback data written to disk.
            WcaCaScriptFlush(hRollbackScript);

            ReleaseNullMem(psfa);
        }

        hr = ConfigureService(hSCM, hService, pwzServiceName, dwRestartServiceDelayInSeconds, pwzFirstFailureActionType,
                              pwzSecondFailureActionType, pwzThirdFailureActionType, dwResetPeriodInDays, pwzRebootMessage, pwzProgramCommandLine);
        ExitOnFailure1(hr, "Failed to configure service: %ls", pwzServiceName);

        hr = WcaProgressMessage(COST_SERVICECONFIG, FALSE);
        ExitOnFailure(hr, "failed to send progress message");

        // Per-service cleanup
        ::CloseServiceHandle(hService);
        hService = NULL;
        dwResetPeriodInDays = 0;
        dwRestartServiceDelayInSeconds = 0;
    }

LExit:
    WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_PRESERVE);

    if (lpMsgBuf)
    {
        ::LocalFree(lpMsgBuf);
    }

    if (hService)
    {
        ::CloseServiceHandle(hService);
    }

    if (hSCM)
    {
        ::CloseServiceHandle(hSCM);
    }

    ReleaseMem(psfa);

    ReleaseStr(pwzRebootMessage);
    ReleaseStr(pwzProgramCommandLine);
    ReleaseStr(pwzThirdFailureActionType);
    ReleaseStr(pwzSecondFailureActionType);
    ReleaseStr(pwzFirstFailureActionType);
    ReleaseStr(pwzServiceName);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzCustomActionData);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/******************************************************************
RollbackServiceConfig - entry point for ServiceConfig rollback
                        Custom Action.

NOTE: CustomActionScript Data == wzServiceName\twzFirstFailureActionType\twzSecondFailureActionType\twzThirdFailureActionType\tdwResetPeriodInDays\tdwRestartServiceDelayInSeconds\twzProgramCommandLine\twzRebootMessage\twzServiceName\t...
*******************************************************************/
extern "C" UINT __stdcall RollbackServiceConfig(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug RollbackServiceConfig");
    HRESULT hr = S_OK;
    DWORD er = 0;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwz = NULL;

    LPWSTR pwzScriptKey = NULL;
    WCA_CASCRIPT_HANDLE hRollbackScript = NULL;

    LPWSTR pwzServiceName = NULL;
    LPWSTR pwzFirstFailureActionType = NULL;
    LPWSTR pwzSecondFailureActionType = NULL;
    LPWSTR pwzThirdFailureActionType = NULL;
    LPWSTR pwzProgramCommandLine = NULL;
    LPWSTR pwzRebootMessage = NULL;
    DWORD dwResetPeriodInDays = 0;
    DWORD dwRestartServiceDelayInSeconds = 0;

    LPVOID lpMsgBuf = NULL;
    SC_HANDLE hSCM = NULL;
    SC_HANDLE hService = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "RollbackServiceConfig");
    ExitOnFailure(hr, "Failed to initialize 'RollbackServiceConfig'.");

    // Open the Services Control Manager up front.
    hSCM = ::OpenSCManagerW(NULL, NULL, SC_MANAGER_CONNECT);
    if (NULL == hSCM)
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);

#pragma prefast(push)
#pragma prefast(disable:25028)
        ::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, er, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPWSTR)&lpMsgBuf, 0, NULL);
#pragma prefast(pop)

        ExitOnFailure1(hr, "Failed to get handle to SCM. Error: %ls", (LPWSTR)lpMsgBuf);

        // Make sure we still abort, in case hSCM was NULL but no error was returned from GetLastError
        ExitOnNull(hSCM, hr, E_POINTER, "Getting handle to SCM reported success, but no handle was returned.");
    }

    // Get the script key from the CustomAction data and use it to open
    // the rollback log and read the data over the CustomActionData
    // because all of the information is in the script data not the
    // CustomActionData.
    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    pwz = pwzCustomActionData;

    hr = WcaReadStringFromCaData(&pwz, &pwzScriptKey);
    if (!pwzScriptKey)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Failed due to unexpected CustomActionData passed.");
    }
    ExitOnFailure(hr, "Failed to read encoding key from CustomActionData.");

    hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, &hRollbackScript);
    ExitOnFailure(hr, "Failed to open rollback CustomAction script.");

    hr = WcaCaScriptReadAsCustomActionData(hRollbackScript, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to read rollback script into CustomAction data.");

    // Loop through the script's CustomActionData, processing each
    // service config in turn.
    pwz = pwzCustomActionData;
    while (pwz && *pwz)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzServiceName);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzFirstFailureActionType);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzSecondFailureActionType);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzThirdFailureActionType);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwResetPeriodInDays));
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwRestartServiceDelayInSeconds));
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzProgramCommandLine);
        ExitOnFailure(hr, "failed to process CustomActionData");
        hr = WcaReadStringFromCaData(&pwz, &pwzRebootMessage);
        ExitOnFailure(hr, "failed to process CustomActionData");

        WcaLog(LOGMSG_VERBOSE, "Reconfiguring Service: %ls", pwzServiceName);

        // Open the handle with all the permissions we might need.
        //  SERVICE_CHANGE_CONFIG is needed for ChangeServiceConfig2().
        //  SERVICE_START is required in order to handle SC_ACTION_RESTART action.
        hr = GetService(hSCM, pwzServiceName, SERVICE_CHANGE_CONFIG | SERVICE_START, &hService);
        ExitOnFailure1(hr, "Failed to get service: %ls", pwzServiceName);

        hr = ConfigureService(hSCM, hService, pwzServiceName, dwRestartServiceDelayInSeconds, pwzFirstFailureActionType,
                              pwzSecondFailureActionType, pwzThirdFailureActionType, dwResetPeriodInDays, pwzRebootMessage, pwzProgramCommandLine);
        ExitOnFailure1(hr, "Failed to configure service: %ls", pwzServiceName);

        hr = WcaProgressMessage(COST_SERVICECONFIG, FALSE);
        ExitOnFailure(hr, "failed to send progress message");

        // Per-service cleanup
        ::CloseServiceHandle(hService);
        hService = NULL;
        dwResetPeriodInDays = 0;
        dwRestartServiceDelayInSeconds = 0;
    }

LExit:
    if (lpMsgBuf) // Allocated with FormatString.
    {
        ::LocalFree(lpMsgBuf);
    }

    if (hService)
    {
        ::CloseServiceHandle(hService);
    }

    if (hSCM)
    {
        ::CloseServiceHandle(hSCM);
    }

    WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_DELETE);

    ReleaseStr(pwzRebootMessage);
    ReleaseStr(pwzProgramCommandLine);
    ReleaseStr(pwzThirdFailureActionType);
    ReleaseStr(pwzSecondFailureActionType);
    ReleaseStr(pwzFirstFailureActionType);
    ReleaseStr(pwzServiceName);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzCustomActionData);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/**********************************************************
GetSCActionType - helper function to return the SC_ACTION_TYPE
for a given string matching the allowed set.
REBOOT, RESTART, RUN_COMMAND and NONE
**********************************************************/
static SC_ACTION_TYPE GetSCActionType(
    __in LPCWSTR pwzActionTypeName
    )
{
    SC_ACTION_TYPE actionType;

    // verify that action types are valid. if not, just default to NONE
    if (0 == lstrcmpiW(c_wzActionTypeReboot, pwzActionTypeName))
    {
        actionType = SC_ACTION_REBOOT;
    }
    else if (0 == lstrcmpiW(c_wzActionTypeRestart, pwzActionTypeName))
    {
        actionType = SC_ACTION_RESTART;
    }
    else if (0 == lstrcmpiW(c_wzActionTypeRunCommand, pwzActionTypeName))
    {
        actionType = SC_ACTION_RUN_COMMAND;
    }
    else
    {
        // default to none
        actionType = SC_ACTION_NONE;
    }

    return actionType;
}


static HRESULT GetSCActionTypeString(
    __in SC_ACTION_TYPE type,
    __out_ecount(cchActionTypeString) LPWSTR wzActionTypeString,
    __in DWORD cchActionTypeString
    )
{
    HRESULT hr = S_OK;

    switch (type)
    {
    case SC_ACTION_REBOOT:
        hr = StringCchCopyW(wzActionTypeString, cchActionTypeString, c_wzActionTypeReboot);
        ExitOnFailure(hr, "Failed to copy 'reboot' into action type.");
        break;
    case SC_ACTION_RESTART:
        hr = StringCchCopyW(wzActionTypeString, cchActionTypeString, c_wzActionTypeRestart);
        ExitOnFailure(hr, "Failed to copy 'restart' into action type.");
        break;
    case SC_ACTION_RUN_COMMAND:
        hr = StringCchCopyW(wzActionTypeString, cchActionTypeString, c_wzActionTypeRunCommand);
        ExitOnFailure(hr, "Failed to copy 'runCommand' into action type.");
        break;
    case SC_ACTION_NONE:
        hr = StringCchCopyW(wzActionTypeString, cchActionTypeString, c_wzActionTypeNone);
        ExitOnFailure(hr, "Failed to copy 'none' into action type.");
        break;
    default:
        break;
    }

LExit:
    return hr;
}


static HRESULT GetService(
    __in SC_HANDLE hSCM,
    __in LPCWSTR wzService,
    __in DWORD dwOpenServiceAccess,
    __out SC_HANDLE* phService
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPVOID lpMsgBuf = NULL;

    *phService = ::OpenServiceW(hSCM, wzService, dwOpenServiceAccess);
    if (NULL == *phService)
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);
        if (ERROR_SERVICE_DOES_NOT_EXIST == er)
        {
            ExitOnFailure1(hr, "Service '%ls' does not exist on this system.", wzService);
        }
        else
        {
#pragma prefast(push)
#pragma prefast(disable:25028)
            ::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, er, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPWSTR)&lpMsgBuf, 0, NULL);
#pragma prefast(pop)

            ExitOnFailure2(hr, "Failed to get handle to the service '%ls'. Error: %ls", wzService, (LPWSTR)lpMsgBuf);
        }
    }

LExit:
    if (lpMsgBuf) // Allocated with FormatString.
    {
        ::LocalFree(lpMsgBuf);
    }

    return hr;
}


static HRESULT ConfigureService(
    __in SC_HANDLE /*hSCM*/,
    __in SC_HANDLE hService,
    __in LPCWSTR wzServiceName,
    __in DWORD dwRestartServiceDelayInSeconds,
    __in LPCWSTR wzFirstFailureActionType,
    __in LPCWSTR wzSecondFailureActionType,
    __in LPCWSTR wzThirdFailureActionType,
    __in DWORD dwResetPeriodInDays,
    __in LPWSTR wzRebootMessage,
    __in LPWSTR wzProgramCommandLine
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    HANDLE hToken = NULL;
    TOKEN_PRIVILEGES priv = { 0 };
    TOKEN_PRIVILEGES* pPrevPriv = NULL;
    DWORD cbPrevPriv = 0;
    BOOL fAdjustedPrivileges = FALSE;

    SC_ACTION actions[3]; // the UI always shows 3 actions, so we'll always do 3
    SERVICE_FAILURE_ACTIONSW sfa;
    LPVOID lpMsgBuf = NULL;

    // Always get the shutdown privilege in case we need to configure service to reboot.
    if (!::OpenProcessToken(::GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, &hToken))
    {
        ExitWithLastError(hr, "Failed to get process token.");
    }

    priv.PrivilegeCount = 1;
    priv.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    if (!::LookupPrivilegeValueW(NULL, L"SeShutdownPrivilege", &priv.Privileges[0].Luid))
    {
        ExitWithLastError(hr, "Failed to get shutdown privilege LUID.");
    }

    cbPrevPriv = sizeof(TOKEN_PRIVILEGES);
    pPrevPriv = static_cast<TOKEN_PRIVILEGES*>(MemAlloc(cbPrevPriv, TRUE));
    ExitOnNull(pPrevPriv, hr, E_OUTOFMEMORY, "Failed to allocate memory for empty previous privileges.");

    if (!::AdjustTokenPrivileges(hToken, FALSE, &priv, cbPrevPriv, pPrevPriv, &cbPrevPriv))
    {
        LPVOID pv = MemReAlloc(pPrevPriv, cbPrevPriv, TRUE);
        ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate memory for previous privileges.");
        pPrevPriv = static_cast<TOKEN_PRIVILEGES*>(pv);

        if (!::AdjustTokenPrivileges(hToken, FALSE, &priv, cbPrevPriv, pPrevPriv, &cbPrevPriv))
        {
            ExitWithLastError(hr, "Failed to get shutdown privilege LUID.");
        }
    }

    fAdjustedPrivileges = TRUE;

    // build up SC_ACTION array
    // TODO: why is delay only respected when SC_ACTION_RESTART is requested?
    actions[0].Type = GetSCActionType(wzFirstFailureActionType);
    actions[0].Delay = 0;
    if (SC_ACTION_RESTART == actions[0].Type)
    {
        actions[0].Delay = dwRestartServiceDelayInSeconds * 1000; // seconds to milliseconds
    }

    actions[1].Type = GetSCActionType(wzSecondFailureActionType);
    actions[1].Delay = 0;
    if (SC_ACTION_RESTART == actions[1].Type)
    {
        actions[1].Delay = dwRestartServiceDelayInSeconds * 1000; // seconds to milliseconds
    }

    actions[2].Type = GetSCActionType(wzThirdFailureActionType);
    actions[2].Delay = 0;
    if (SC_ACTION_RESTART == actions[2].Type)
    {
        actions[2].Delay = dwRestartServiceDelayInSeconds * 1000; // seconds to milliseconds
    }

    // build up the SERVICE_FAILURE_ACTIONSW struct
    sfa.dwResetPeriod = dwResetPeriodInDays * (24 * 60 * 60); // days to seconds
    sfa.lpRebootMsg = wzRebootMessage;
    sfa.lpCommand = wzProgramCommandLine;
    sfa.cActions = countof(actions);
    sfa.lpsaActions = actions;

    // Call ChangeServiceConfig2 to actually set up the failure actions
    if (!::ChangeServiceConfig2W(hService, SERVICE_CONFIG_FAILURE_ACTIONS, (LPVOID)&sfa))
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);

#pragma prefast(push)
#pragma prefast(disable:25028)
        ::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, er, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPWSTR)&lpMsgBuf, 0, NULL);
#pragma prefast(pop)

        // Check if this is a service that can't be modified.
        if (ERROR_CANNOT_DETECT_PROCESS_ABORT == er)
        {
            WcaLog(LOGMSG_STANDARD, "WARNING: Service \"%ls\" is not configurable on this server and will not be set.", wzServiceName);
        }
        ExitOnFailure1(hr, "Cannot change service configuration. Error: %ls", (LPWSTR)lpMsgBuf);

        if (lpMsgBuf)
        {
            ::LocalFree(lpMsgBuf);
            lpMsgBuf = NULL;
        }
    }

LExit:
    if (lpMsgBuf)
    {
        ::LocalFree(lpMsgBuf);
    }

    if (fAdjustedPrivileges)
    {
        ::AdjustTokenPrivileges(hToken, FALSE, pPrevPriv, 0, NULL, NULL);
    }

    ReleaseMem(pPrevPriv);
    ReleaseHandle(hToken);

    return hr;
}
