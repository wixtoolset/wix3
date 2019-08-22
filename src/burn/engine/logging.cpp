// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static DWORD vdwPackageSequence = 0;
static const DWORD LOG_OPEN_RETRY_COUNT = 3;
static const DWORD LOG_OPEN_RETRY_WAIT = 2000;
static CONST LPWSTR LOG_FAILED_EVENT_LOG_MESSAGE = L"Burn Engine Fatal Error: failed to open log file.";

// structs



// internal function declarations

static void CheckLoggingPolicy(
    __out DWORD *pdwAttributes
    );
static HRESULT GetNonSessionSpecificTempFolder(
    __deref_out_z LPWSTR* psczNonSessionTempFolder
    );


// function definitions

extern "C" HRESULT LoggingOpen(
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in_z LPCWSTR wzBundleName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLoggingBaseFolder = NULL;

    // Check if the logging policy is set and configure the logging appropriately.
    CheckLoggingPolicy(&pLog->dwAttributes);

    if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_VERBOSE || pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_EXTRADEBUG)
    {
        if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_EXTRADEBUG)
        {
            LogSetLevel(REPORT_DEBUG, FALSE);
        }
        else if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_VERBOSE)
        {
            LogSetLevel(REPORT_VERBOSE, FALSE);
        }

        if ((!pLog->sczPath || !*pLog->sczPath) && (!pLog->sczPrefix || !*pLog->sczPrefix))
        {
            PathCreateTimeBasedTempFile(NULL, L"Setup", NULL, L"log", &pLog->sczPath, NULL);
        }
    }

    // Open the log approriately.
    if (pLog->sczPath && *pLog->sczPath)
    {
        DWORD cRetry = 0;

        hr = DirGetCurrent(&sczLoggingBaseFolder);
        ExitOnFailure(hr, "Failed to get current directory.");

        // Try pretty hard to open the log file when appending.
        do
        {
            if (0 < cRetry)
            {
                ::Sleep(LOG_OPEN_RETRY_WAIT);
            }

            hr = LogOpen(sczLoggingBaseFolder, pLog->sczPath, NULL, NULL, pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_APPEND, FALSE, &pLog->sczPath);
            if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_APPEND && HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) == hr)
            {
                ++cRetry;
            }
        } while (cRetry > 0 && cRetry <= LOG_OPEN_RETRY_COUNT);

        if (FAILED(hr))
        {
            // Log is not open, so note that.
            LogDisable();
            pLog->state = BURN_LOGGING_STATE_DISABLED;

            if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_APPEND)
            {
                // If appending, ignore the failure and continue.
                hr = S_OK;
            }
            else // specifically tried to create a log file so show an error if appropriate and bail.
            {
                HRESULT hrOriginal = hr;

                hr = HRESULT_FROM_WIN32(ERROR_INSTALL_LOG_FAILURE);
                SplashScreenDisplayError(display, wzBundleName, hr);

                ExitOnFailure1(hrOriginal, "Failed to open log: %ls", pLog->sczPath);
            }
        }
        else
        {
            pLog->state = BURN_LOGGING_STATE_OPEN;
        }
    }
    else if (pLog->sczPrefix && *pLog->sczPrefix)
    {
        hr = GetNonSessionSpecificTempFolder(&sczLoggingBaseFolder);
        ExitOnFailure(hr, "Failed to get non-session specific TEMP folder.");

        // Best effort to open default logging.
        hr = LogOpen(sczLoggingBaseFolder, pLog->sczPrefix, NULL, pLog->sczExtension, FALSE, FALSE, &pLog->sczPath);
        if (FAILED(hr))
        {
            LogDisable();
            pLog->state = BURN_LOGGING_STATE_DISABLED;

            hr = S_OK;
        }
        else
        {
            pLog->state = BURN_LOGGING_STATE_OPEN;
        }
    }
    else // no logging enabled.
    {
        LogDisable();
        pLog->state = BURN_LOGGING_STATE_DISABLED;
    }

    // If the log was opened, write the header info and update the prefix and extension to match
    // the log name so future logs are opened with the same pattern.
    if (BURN_LOGGING_STATE_OPEN == pLog->state)
    {
        LPCWSTR wzExtension = PathExtension(pLog->sczPath);
        if (wzExtension && *wzExtension)
        {
            hr = StrAllocString(&pLog->sczPrefix, pLog->sczPath, wzExtension - pLog->sczPath);
            ExitOnFailure(hr, "Failed to copy log path to prefix.");

            hr = StrAllocString(&pLog->sczExtension, wzExtension + 1, 0);
            ExitOnFailure(hr, "Failed to copy log extension to extension.");
        }
        else
        {
            hr = StrAllocString(&pLog->sczPrefix, pLog->sczPath, 0);
            ExitOnFailure(hr, "Failed to copy full log path to prefix.");
        }

        if (pLog->sczPathVariable && *pLog->sczPathVariable)
        {
            VariableSetString(pVariables, pLog->sczPathVariable, pLog->sczPath, FALSE); // Ignore failure.
        }
    }

LExit:
    ReleaseStr(sczLoggingBaseFolder);

    return hr;
}

extern "C" void LoggingOpenFailed()
{
    HRESULT hr = S_OK;
    HANDLE hEventLog = NULL;
    LPCWSTR* lpStrings = const_cast<LPCWSTR*>(&LOG_FAILED_EVENT_LOG_MESSAGE);
    WORD wNumStrings = 1;

    hr = LogOpen(NULL, L"Setup", L"_Failed", L"txt", FALSE, FALSE, NULL);
    if (SUCCEEDED(hr))
    {
        ExitFunction();
    }

    // If opening the "failure" log failed, then attempt to record that in the Application event log.
    hEventLog = ::OpenEventLogW(NULL, L"Application");
    ExitOnNullWithLastError(hEventLog, hr, "Failed to open Application event log");

    hr = ::ReportEventW(hEventLog, EVENTLOG_ERROR_TYPE, 1, 1, NULL, wNumStrings, 0, lpStrings, NULL);
    ExitOnNullWithLastError(hEventLog, hr, "Failed to write event log entry");

LExit:
    if (hEventLog)
    {
        ::CloseEventLog(hEventLog);
    }
}

extern "C" void LoggingIncrementPackageSequence()
{
    ++vdwPackageSequence;
}

extern "C" HRESULT LoggingSetPackageVariable(
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzSuffix,
    __in BOOL fRollback,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __out_opt LPWSTR* psczLogPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLogPath = NULL;

    if (BURN_LOGGING_STATE_DISABLED == pLog->state)
    {
        if (psczLogPath)
        {
            *psczLogPath = NULL;
        }

        ExitFunction();
    }

    if ((!fRollback && pPackage->sczLogPathVariable && *pPackage->sczLogPathVariable) ||
        (fRollback && pPackage->sczRollbackLogPathVariable && *pPackage->sczRollbackLogPathVariable))
    {
        hr = StrAllocFormatted(&sczLogPath, L"%ls%hs%ls_%03u_%ls%ls.%ls", pLog->sczPrefix, wzSuffix && *wzSuffix ? "_" : "", wzSuffix && *wzSuffix ? wzSuffix : L"", vdwPackageSequence, pPackage->sczId, fRollback ? L"_rollback" : L"", pLog->sczExtension);
        ExitOnFailure(hr, "Failed to allocate path for package log.");

        hr = VariableSetString(pVariables, fRollback ? pPackage->sczRollbackLogPathVariable : pPackage->sczLogPathVariable, sczLogPath, FALSE);
        ExitOnFailure(hr, "Failed to set log path into variable.");

        if (psczLogPath)
        {
            hr = StrAllocString(psczLogPath, sczLogPath, 0);
            ExitOnFailure(hr, "Failed to copy package log path.");
        }
    }

LExit:
    ReleaseStr(sczLogPath);

    return hr;
}

extern "C" LPCSTR LoggingBurnActionToString(
    __in BOOTSTRAPPER_ACTION action
    )
{
    switch (action)
    {
    case BOOTSTRAPPER_ACTION_UNKNOWN:
        return "Unknown";
    case BOOTSTRAPPER_ACTION_HELP:
        return "Help";
    case BOOTSTRAPPER_ACTION_LAYOUT:
        return "Layout";
    case BOOTSTRAPPER_ACTION_CACHE:
        return "Cache";
    case BOOTSTRAPPER_ACTION_UNINSTALL:
        return "Uninstall";
    case BOOTSTRAPPER_ACTION_INSTALL:
        return "Install";
    case BOOTSTRAPPER_ACTION_MODIFY:
        return "Modify";
    case BOOTSTRAPPER_ACTION_REPAIR:
        return "Repair";
    case BOOTSTRAPPER_ACTION_UPDATE_REPLACE:
        return "UpdateReplace";
    case BOOTSTRAPPER_ACTION_UPDATE_REPLACE_EMBEDDED:
        return "UpdateReplaceEmbedded";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingActionStateToString(
    __in BOOTSTRAPPER_ACTION_STATE actionState
    )
{
    switch (actionState)
    {
    case BOOTSTRAPPER_ACTION_STATE_NONE:
        return "None";
    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        return "Uninstall";
    case BOOTSTRAPPER_ACTION_STATE_INSTALL:
        return "Install";
    case BOOTSTRAPPER_ACTION_STATE_ADMIN_INSTALL:
        return "AdminInstall";
    case BOOTSTRAPPER_ACTION_STATE_MODIFY:
        return "Modify";
    case BOOTSTRAPPER_ACTION_STATE_REPAIR:
        return "Repair";
    case BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE:
        return "MinorUpgrade";
    case BOOTSTRAPPER_ACTION_STATE_MAJOR_UPGRADE:
        return "MajorUpgrade";
    case BOOTSTRAPPER_ACTION_STATE_PATCH:
        return "Patch";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingDependencyActionToString(
    BURN_DEPENDENCY_ACTION action
    )
{
    switch (action)
    {
    case BURN_DEPENDENCY_ACTION_NONE:
        return "None";
    case BURN_DEPENDENCY_ACTION_REGISTER:
        return "Register";
    case BURN_DEPENDENCY_ACTION_UNREGISTER:
        return "Unregister";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingBoolToString(
    __in BOOL f
    )
{
    if (f)
    {
        return "Yes";
    }

    return "No";
}

extern "C" LPCSTR LoggingTrueFalseToString(
    __in BOOL f
    )
{
    if (f)
    {
        return "true";
    }

    return "false";
}

extern "C" LPCSTR LoggingPackageStateToString(
    __in BOOTSTRAPPER_PACKAGE_STATE packageState
    )
{
    switch (packageState)
    {
    case BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN:
        return "Unknown";
    case BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE:
        return "Obsolete";
    case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
        return "Absent";
    case BOOTSTRAPPER_PACKAGE_STATE_CACHED:
        return "Cached";
    case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
        return "Present";
    case BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED:
        return "Superseded";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingCacheStateToString(
    __in BURN_CACHE_STATE cacheState
    )
{
    switch (cacheState)
    {
    case BURN_CACHE_STATE_NONE:
        return "None";
    case BURN_CACHE_STATE_PARTIAL:
        return "Partial";
    case BURN_CACHE_STATE_COMPLETE:
        return "Complete";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingMsiFeatureStateToString(
    __in BOOTSTRAPPER_FEATURE_STATE featureState
    )
{
    switch (featureState)
    {
    case BOOTSTRAPPER_FEATURE_STATE_UNKNOWN:
        return "Unknown";
    case BOOTSTRAPPER_FEATURE_STATE_ABSENT:
        return "Absent";
    case BOOTSTRAPPER_FEATURE_STATE_ADVERTISED:
        return "Advertised";
    case BOOTSTRAPPER_FEATURE_STATE_LOCAL:
        return "Local";
    case BOOTSTRAPPER_FEATURE_STATE_SOURCE:
        return "Source";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingMsiFeatureActionToString(
    __in BOOTSTRAPPER_FEATURE_ACTION featureAction
    )
{
    switch (featureAction)
    {
    case BOOTSTRAPPER_FEATURE_ACTION_NONE:
        return "None";
    case BOOTSTRAPPER_FEATURE_ACTION_ADDLOCAL:
        return "AddLocal";
    case BOOTSTRAPPER_FEATURE_ACTION_ADDSOURCE:
        return "AddSource";
    case BOOTSTRAPPER_FEATURE_ACTION_ADDDEFAULT:
        return "AddDefault";
    case BOOTSTRAPPER_FEATURE_ACTION_REINSTALL:
        return "Reinstall";
    case BOOTSTRAPPER_FEATURE_ACTION_ADVERTISE:
        return "Advertise";
    case BOOTSTRAPPER_FEATURE_ACTION_REMOVE:
        return "Remove";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingMsiInstallContext(
    __in MSIINSTALLCONTEXT context
    )
{
    switch (context)
    {
    case MSIINSTALLCONTEXT_ALL:
        return "All";
    case MSIINSTALLCONTEXT_ALLUSERMANAGED:
        return "AllUserManaged";
    case MSIINSTALLCONTEXT_MACHINE:
        return "Machine";
    case MSIINSTALLCONTEXT_NONE:
        return "None";
    case MSIINSTALLCONTEXT_USERMANAGED:
        return "UserManaged";
    case MSIINSTALLCONTEXT_USERUNMANAGED:
        return "UserUnmanaged";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingPerMachineToString(
    __in BOOL fPerMachine
    )
{
    if (fPerMachine)
    {
        return "PerMachine";
    }

    return "PerUser";
}

extern "C" LPCSTR LoggingRestartToString(
    __in BOOTSTRAPPER_APPLY_RESTART restart
    )
{
    switch (restart)
    {
    case BOOTSTRAPPER_APPLY_RESTART_NONE:
        return "None";
    case BOOTSTRAPPER_APPLY_RESTART_REQUIRED:
        return "Required";
    case BOOTSTRAPPER_APPLY_RESTART_INITIATED:
        return "Initiated";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingResumeModeToString(
    __in BURN_RESUME_MODE resumeMode
    )
{
    switch (resumeMode)
    {
    case BURN_RESUME_MODE_NONE:
        return "None";
    case BURN_RESUME_MODE_ACTIVE:
        return "Active";
    case BURN_RESUME_MODE_SUSPEND:
        return "Suspend";
    case BURN_RESUME_MODE_ARP:
        return "ARP";
    case BURN_RESUME_MODE_REBOOT_PENDING:
        return "Reboot Pending";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRelationTypeToString(
    __in BOOTSTRAPPER_RELATION_TYPE type
    )
{
    switch (type)
    {
    case BOOTSTRAPPER_RELATION_NONE:
        return "None";
    case BOOTSTRAPPER_RELATION_DETECT:
        return "Detect";
    case BOOTSTRAPPER_RELATION_UPGRADE:
        return "Upgrade";
    case BOOTSTRAPPER_RELATION_ADDON:
        return "Addon";
    case BOOTSTRAPPER_RELATION_PATCH:
        return "Patch";
    case BOOTSTRAPPER_RELATION_DEPENDENT:
        return "Dependent";
    case BOOTSTRAPPER_RELATION_UPDATE:
        return "Update";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRelatedOperationToString(
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    )
{
    switch (operation)
    {
    case BOOTSTRAPPER_RELATED_OPERATION_NONE:
        return "None";
    case BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE:
        return "Downgrade";
    case BOOTSTRAPPER_RELATED_OPERATION_MINOR_UPDATE:
        return "MinorUpdate";
    case BOOTSTRAPPER_RELATED_OPERATION_MAJOR_UPGRADE:
        return "MajorUpgrade";
    case BOOTSTRAPPER_RELATED_OPERATION_REMOVE:
        return "Remove";
    case BOOTSTRAPPER_RELATED_OPERATION_INSTALL:
        return "Install";
    case BOOTSTRAPPER_RELATED_OPERATION_REPAIR:
        return "Repair";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRequestStateToString(
    __in BOOTSTRAPPER_REQUEST_STATE requestState
    )
{
    switch (requestState)
    {
    case BOOTSTRAPPER_REQUEST_STATE_NONE:
        return "None";
    case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
        return "ForceAbsent";
    case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
        return "Absent";
    case BOOTSTRAPPER_REQUEST_STATE_CACHE:
        return "Cache";
    case BOOTSTRAPPER_REQUEST_STATE_PRESENT:
        return "Present";
    case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
        return "Repair";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRollbackOrExecute(
    __in BOOL fRollback
    )
{
    return fRollback ? "rollback" : "execute";
}

extern "C" LPWSTR LoggingStringOrUnknownIfNull(
    __in LPCWSTR wz
    )
{
    return wz ? wz : L"Unknown";
}

// Note: this function is not thread safe.
extern "C" LPCSTR LoggingVersionToString(
    __in DWORD64 dw64Version
    )
{
    static CHAR szVersion[40] = { 0 };
    HRESULT hr = S_OK;

    hr = ::StringCchPrintfA(szVersion, countof(szVersion), "%I64u.%I64u.%I64u.%I64u", dw64Version >> 48 & 0xFFFF, dw64Version >> 32 & 0xFFFF, dw64Version >> 16 & 0xFFFF, dw64Version  & 0xFFFF);
    if (FAILED(hr))
    {
        memset(szVersion, 0, sizeof(szVersion));
    }

    return szVersion;
}


// internal function declarations

static void CheckLoggingPolicy(
    __out DWORD *pdwAttributes
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;
    LPWSTR sczLoggingPolicy = NULL;

    hr = RegOpen(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Policies\\Microsoft\\Windows\\Installer", KEY_READ, &hk);
    if (SUCCEEDED(hr))
    {
        hr = RegReadString(hk, L"Logging", &sczLoggingPolicy);
        if (SUCCEEDED(hr))
        {
            LPCWSTR wz = sczLoggingPolicy;
            while (*wz)
            {
                if (L'v' == *wz || L'V' == *wz)
                {
                    *pdwAttributes |= BURN_LOGGING_ATTRIBUTE_VERBOSE;
                }
                else if (L'x' == *wz || L'X' == *wz)
                {
                    *pdwAttributes |= BURN_LOGGING_ATTRIBUTE_EXTRADEBUG;
                }

                ++wz;
            }
        }
    }

    ReleaseStr(sczLoggingPolicy);
    ReleaseRegKey(hk);
}

static HRESULT GetNonSessionSpecificTempFolder(
    __deref_out_z LPWSTR* psczNonSessionTempFolder
    )
{
    HRESULT hr = S_OK;
    WCHAR wzTempFolder[MAX_PATH] = { };
    DWORD cchTempFolder = 0;
    DWORD dwSessionId = 0;
    LPWSTR sczSessionId = 0;
    DWORD cchSessionId = 0;

    if (!::GetTempPathW(countof(wzTempFolder), wzTempFolder))
    {
        ExitWithLastError(hr, "Failed to get temp folder.");
    }

    hr = ::StringCchLengthW(wzTempFolder, countof(wzTempFolder), reinterpret_cast<size_t*>(&cchTempFolder));
    ExitOnFailure(hr, "Failed to get length of temp folder.");

    // If our session id is in the TEMP path then remove that part so we get the non-session
    // specific temporary folder.
    if (::ProcessIdToSessionId(::GetCurrentProcessId(), &dwSessionId))
    {
        hr = StrAllocFormatted(&sczSessionId, L"%u\\", dwSessionId);
        ExitOnFailure(hr, "Failed to format session id as a string.");

        hr = ::StringCchLengthW(sczSessionId, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchSessionId));
        ExitOnFailure(hr, "Failed to get length of session id string.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzTempFolder + cchTempFolder - cchSessionId, cchSessionId, sczSessionId, cchSessionId))
        {
            cchTempFolder -= cchSessionId;
        }
    }

    hr = StrAllocString(psczNonSessionTempFolder, wzTempFolder, cchTempFolder);
    ExitOnFailure(hr, "Failed to copy temp folder.");

LExit:
    ReleaseStr(sczSessionId);

    return hr;
}
