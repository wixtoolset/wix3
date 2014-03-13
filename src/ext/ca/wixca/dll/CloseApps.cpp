//-------------------------------------------------------------------------------------------------
// <copyright file="CloseApps.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Code to close applications via custom actions when the installer cannot.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define DEFAULT_PROCESS_EXIT_WAIT_TIME 5000

// WixCloseApplication     Target      Description     Condition       Attributes      Sequence

// structs
LPCWSTR wzQUERY_CLOSEAPPS = L"SELECT `WixCloseApplication`, `Target`, `Description`, `Condition`, `Attributes`, `Property`, `TerminateExitCode`, `Timeout` FROM `WixCloseApplication` ORDER BY `Sequence`";
enum eQUERY_CLOSEAPPS { QCA_ID = 1, QCA_TARGET, QCA_DESCRIPTION, QCA_CONDITION, QCA_ATTRIBUTES, QCA_PROPERTY, QCA_TERMINATEEXITCODE, QCA_TIMEOUT };

// CloseApplication.Attributes
enum CLOSEAPP_ATTRIBUTES
{
    CLOSEAPP_ATTRIBUTE_NONE = 0x0,
    CLOSEAPP_ATTRIBUTE_CLOSEMESSAGE = 0x1,
    CLOSEAPP_ATTRIBUTE_REBOOTPROMPT = 0x2,
    CLOSEAPP_ATTRIBUTE_ELEVATEDCLOSEMESSAGE = 0x4,
    CLOSEAPP_ATTRIBUTE_ENDSESSIONMESSAGE = 0x8,
    CLOSEAPP_ATTRIBUTE_ELEVATEDENDSESSIONMESSAGE = 0x10,
    CLOSEAPP_ATTRIBUTE_TERMINATEPROCESS = 0x20,
    CLOSEAPP_ATTRIBUTE_PROMPTTOCONTINUE = 0x40,
};

struct PROCESS_AND_MESSAGE
{
    DWORD dwProcessId;
    DWORD dwMessageId;
    DWORD dwTimeout;
};


/******************************************************************
 EnumWindowsProc - callback function which sends message if the
 current window matches the passed in process ID

******************************************************************/
BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam)
{
    PROCESS_AND_MESSAGE* pPM = reinterpret_cast<PROCESS_AND_MESSAGE*>(lParam);
    DWORD dwProcessId = 0;
    DWORD_PTR dwResult = 0;
    BOOL fQueryEndSession = WM_QUERYENDSESSION == pPM->dwMessageId;
    BOOL fContinueWindowsInProcess = TRUE; // assume we will send message to all top-level windows in a process.

    ::GetWindowThreadProcessId(hwnd, &dwProcessId);

    // check if the process Id is the one we're looking for
    if (dwProcessId != pPM->dwProcessId)
    {
        return TRUE;
    }

    WcaLog(LOGMSG_VERBOSE, "Sending message to process id 0x%x", dwProcessId);

    if (::SendMessageTimeoutW(hwnd, pPM->dwMessageId, 0, fQueryEndSession ? ENDSESSION_CLOSEAPP : 0, SMTO_BLOCK, pPM->dwTimeout, &dwResult))
    {
        WcaLog(LOGMSG_VERBOSE, "Result 0x%x", dwResult);

        if (fQueryEndSession)
        {
            // If application said it was okay to close, do that.
            if (dwResult)
            {
                ::SendMessageTimeoutW(hwnd, WM_ENDSESSION, 0, ENDSESSION_CLOSEAPP, SMTO_BLOCK, pPM->dwTimeout, &dwResult);
            }
            else // application said don't try to close it, so don't bother sending messages to any other top-level windows.
            {
                fContinueWindowsInProcess = FALSE;
            }
        }
    }
    else // log result message.
    {
        WcaLog(LOGMSG_VERBOSE, "Failed to send message id: %u, error: 0x%x", pPM->dwMessageId, ::GetLastError());
    }

    // so we know we succeeded
    ::SetLastError(ERROR_SUCCESS);

    return fContinueWindowsInProcess;
}

/******************************************************************
 PromptToContinue - displays the prompt if the application is still
  running.

******************************************************************/
static HRESULT PromptToContinue(
    __in_z LPCWSTR wzApplication,
    __in_z LPCWSTR wzPrompt
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PMSIHANDLE hRecMessage = NULL;
    DWORD *prgProcessIds = NULL;
    DWORD cProcessIds = 0;

    hRecMessage = ::MsiCreateRecord(1);
    ExitOnNull(hRecMessage, hr, E_OUTOFMEMORY, "Failed to create record for prompt.");

    er = ::MsiRecordSetStringW(hRecMessage, 0, wzPrompt);
    ExitOnWin32Error(er, hr, "Failed to set prompt record field string");

    do
    {
        hr = ProcFindAllIdsFromExeName(wzApplication, &prgProcessIds, &cProcessIds);
        if (SUCCEEDED(hr) && 0 < cProcessIds)
        {
            er = WcaProcessMessage(static_cast<INSTALLMESSAGE>(INSTALLMESSAGE_WARNING | MB_ABORTRETRYIGNORE | MB_DEFBUTTON3 | MB_ICONWARNING), hRecMessage);
            if (IDABORT == er)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
            }
            else if (IDRETRY == er)
            {
                hr = S_FALSE;
            }
            else if (IDIGNORE == er)
            {
                hr = S_OK;
            }
            else
            {
                ExitOnWin32Error(er, hr, "Unexpected return value from prompt to continue.");
            }
        }

        ReleaseNullMem(prgProcessIds);
        cProcessIds = 0;
    } while (S_FALSE == hr);

LExit:
    ReleaseMem(prgProcessIds);
    return hr;
}

/******************************************************************
 SendProcessMessage - helper function to enumerate the top-level 
 windows and send to all matching a process ID.

******************************************************************/
void SendProcessMessage(
    __in DWORD dwProcessId,
    __in DWORD dwMessageId,
    __in DWORD dwTimeout
    )
{
    WcaLog(LOGMSG_VERBOSE, "Attempting to send process id 0x%x message id: %u", dwProcessId, dwMessageId);

    PROCESS_AND_MESSAGE pm = { };
    pm.dwProcessId = dwProcessId;
    pm.dwMessageId = dwMessageId;
    pm.dwTimeout = dwTimeout;

    if (!::EnumWindows(EnumWindowsProc, reinterpret_cast<LPARAM>(&pm)))
    {
        DWORD dwLastError = ::GetLastError();
        if (ERROR_SUCCESS != dwLastError)
        {
            WcaLog(LOGMSG_VERBOSE, "CloseApp enumeration error: 0x%x", dwLastError);
        }
    }
}

/******************************************************************
 SendApplicationMessage - helper function to iterate through the 
 processes for the specified application and send all
 applicable process Ids a message and give them time to process
 the message.

******************************************************************/
void SendApplicationMessage(
    __in LPCWSTR wzApplication,
    __in DWORD dwMessageId,
    __in DWORD dwTimeout
    )
{
    DWORD *prgProcessIds = NULL;
    DWORD cProcessIds = 0, iProcessId;
    HRESULT hr = S_OK;

    WcaLog(LOGMSG_VERBOSE, "Checking App: %ls ", wzApplication);

    hr = ProcFindAllIdsFromExeName(wzApplication, &prgProcessIds, &cProcessIds);

    if (SUCCEEDED(hr) && 0 < cProcessIds)
    {
        WcaLog(LOGMSG_VERBOSE, "App: %ls found running, %d processes, attempting to send message.", wzApplication, cProcessIds);

        for (iProcessId = 0; iProcessId < cProcessIds; ++iProcessId)
        {
            SendProcessMessage(prgProcessIds[iProcessId], dwMessageId, dwTimeout);
        }

        ProcWaitForIds(prgProcessIds, cProcessIds, dwTimeout);
    }

    ReleaseMem(prgProcessIds);
}

/******************************************************************
 SetRunningProcessProperty - helper function that sets the specified
 property if there are any instances of the specified executable
 running. Useful to show custom UI to ask for shutdown.
******************************************************************/
void SetRunningProcessProperty(
    __in LPCWSTR wzApplication,
    __in LPCWSTR wzProperty
    )
{
    DWORD *prgProcessIds = NULL;
    DWORD cProcessIds = 0;
    HRESULT hr = S_OK;

    WcaLog(LOGMSG_VERBOSE, "Checking App: %ls ", wzApplication);

    hr = ProcFindAllIdsFromExeName(wzApplication, &prgProcessIds, &cProcessIds);

    if (SUCCEEDED(hr) && 0 < cProcessIds)
    {
        WcaLog(LOGMSG_VERBOSE, "App: %ls found running, %d processes, setting '%ls' property.", wzApplication, cProcessIds, wzProperty);
        WcaSetIntProperty(wzProperty, cProcessIds);
    }

    ReleaseMem(prgProcessIds);
}

/******************************************************************
 TerminateProcesses - helper function that kills the provided set of
 process ids such that they return a particular exit code.
******************************************************************/
void TerminateProcesses(
    __in_ecount(cProcessIds) DWORD rgdwProcessIds[],
    __in DWORD cProcessIds,
    __in DWORD dwExitCode
    )
{
    for (DWORD i = 0; i < cProcessIds; ++i)
    {
        HANDLE hProcess = ::OpenProcess(PROCESS_TERMINATE, FALSE, rgdwProcessIds[i]);
        if (hProcess)
        {
            ::TerminateProcess(hProcess, dwExitCode);
            ::CloseHandle(hProcess);
        }
    }
}

/******************************************************************
 WixCloseApplications - entry point for WixCloseApplications Custom Action

 called as Type 1 CustomAction (binary DLL) from Windows Installer 
 in InstallExecuteSequence before InstallFiles
******************************************************************/
extern "C" UINT __stdcall WixCloseApplications(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug WixCloseApplications");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwzId = NULL;
    LPWSTR pwzTarget = NULL;
    LPWSTR pwzDescription = NULL;
    LPWSTR pwzCondition = NULL;
    LPWSTR pwzProperty = NULL;
    DWORD dwAttributes = 0;
    DWORD dwTimeout = 0;
    DWORD dwTerminateExitCode = 0;
    MSICONDITION condition = MSICONDITION_NONE;

    DWORD cCloseApps = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;
    MSIHANDLE hListboxTable = NULL;
    MSIHANDLE hListboxColumns = NULL;

    LPWSTR pwzCustomActionData = NULL;
    //DWORD cchCustomActionData = 0;

    //
    // initialize
    //
    hr = WcaInitialize(hInstall, "WixCloseApplications");
    ExitOnFailure(hr, "failed to initialize");

    //
    // loop through all the objects to be secured
    //
    hr = WcaOpenExecuteView(wzQUERY_CLOSEAPPS, &hView);
    ExitOnFailure(hr, "failed to open view on WixCloseApplication table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, QCA_ID, &pwzId);
        ExitOnFailure(hr, "failed to get id from WixCloseApplication table");

        hr = WcaGetRecordString(hRec, QCA_CONDITION, &pwzCondition);
        ExitOnFailure(hr, "failed to get condition from WixCloseApplication table");

        if (pwzCondition && *pwzCondition)
        {
            condition = ::MsiEvaluateConditionW(hInstall, pwzCondition);
            if (MSICONDITION_ERROR == condition)
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "failed to process condition for WixCloseApplication '%ls'", pwzId);
            }
            else if (MSICONDITION_FALSE == condition)
            {
                continue; // skip processing this target
            }
        }

        hr = WcaGetRecordFormattedString(hRec, QCA_TARGET, &pwzTarget);
        ExitOnFailure(hr, "failed to get target from WixCloseApplication table");

        hr = WcaGetRecordFormattedString(hRec, QCA_DESCRIPTION, &pwzDescription);
        ExitOnFailure(hr, "failed to get description from WixCloseApplication table");

        hr = WcaGetRecordInteger(hRec, QCA_ATTRIBUTES, reinterpret_cast<int*>(&dwAttributes));
        ExitOnFailure(hr, "failed to get attributes from WixCloseApplication table");

        hr = WcaGetRecordFormattedString(hRec, QCA_PROPERTY, &pwzProperty);
        ExitOnFailure(hr, "failed to get property from WixCloseApplication table");

        hr = WcaGetRecordInteger(hRec, QCA_TERMINATEEXITCODE, reinterpret_cast<int*>(&dwTerminateExitCode));
        if (S_FALSE == hr)
        {
            dwTerminateExitCode = 0;
            hr = S_OK;
        }
        ExitOnFailure(hr, "failed to get timeout from WixCloseApplication table");

        hr = WcaGetRecordInteger(hRec, QCA_TIMEOUT, reinterpret_cast<int*>(&dwTimeout));
        if (S_FALSE == hr)
        {
            dwTimeout = DEFAULT_PROCESS_EXIT_WAIT_TIME;
            hr = S_OK;
        }
        ExitOnFailure(hr, "failed to get timeout from WixCloseApplication table");

        // Before trying any changes to the machine, prompt if requested.
        if (dwAttributes & CLOSEAPP_ATTRIBUTE_PROMPTTOCONTINUE)
        {
            hr = PromptToContinue(pwzTarget, pwzDescription ? pwzDescription : L"");
            if (HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) == hr)
            {
                // Skip error message if user canceled.
                ExitFunction();
            }
            ExitOnFailure(hr, "Failure while prompting user to continue to close application.");
        }

        //
        // send WM_CLOSE or WM_QUERYENDSESSION to currently running applications
        //
        if (dwAttributes & CLOSEAPP_ATTRIBUTE_CLOSEMESSAGE)
        {
            SendApplicationMessage(pwzTarget, WM_CLOSE, dwTimeout);
        }

        if (dwAttributes & CLOSEAPP_ATTRIBUTE_ENDSESSIONMESSAGE)
        {
            SendApplicationMessage(pwzTarget, WM_QUERYENDSESSION, dwTimeout);
        }

        //
        // Pass the targets to the deferred action in case the app comes back
        // even if we close it now.
        //
        if (dwAttributes & (CLOSEAPP_ATTRIBUTE_ELEVATEDCLOSEMESSAGE | CLOSEAPP_ATTRIBUTE_ELEVATEDENDSESSIONMESSAGE | CLOSEAPP_ATTRIBUTE_REBOOTPROMPT | CLOSEAPP_ATTRIBUTE_TERMINATEPROCESS))
        {
            hr = WcaWriteStringToCaData(pwzTarget, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add target data to CustomActionData");

            hr = WcaWriteIntegerToCaData(dwAttributes, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add attribute data to CustomActionData");

            hr = WcaWriteIntegerToCaData(dwTimeout, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add timeout data to CustomActionData");

            hr = WcaWriteIntegerToCaData(dwTerminateExitCode, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to add timeout data to CustomActionData");
        }

        if (pwzProperty && *pwzProperty)
        {
            SetRunningProcessProperty(pwzTarget, pwzProperty);
        }

        ++cCloseApps;
    }

    // if we looped through all records all is well
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed while looping through all apps to close");

    //
    // Do the UI dance now.
    //
    /*

    TODO: Do this eventually

    if (cCloseApps)
    {
        while (TRUE)
        {
            for (DWORD i = 0; i < cCloseApps; ++i)
            {
                hr = WcaAddTempRecord(&hListboxTable, &hListboxColumns, L"ListBox", NULL, 0, 4, L"FileInUseProcess", i, target, description);
                if (FAILED(hr))
                {
                }
            }
        }
    }
    */

    //
    // schedule the custom action and add to progress bar
    //
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        Assert(0 < cCloseApps);

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"WixCloseApplicationsDeferred"), pwzCustomActionData, cCloseApps * COST_CLOSEAPP);
        ExitOnFailure(hr, "failed to schedule WixCloseApplicationsDeferred action");
    }

LExit:
    if (hListboxColumns)
    {
        ::MsiCloseHandle(hListboxColumns);
    }
    if (hListboxTable)
    {
        ::MsiCloseHandle(hListboxTable);
    }

    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzData);
    ReleaseStr(pwzProperty);
    ReleaseStr(pwzCondition);
    ReleaseStr(pwzDescription);
    ReleaseStr(pwzTarget);
    ReleaseStr(pwzId);

    if (FAILED(hr))
    {
        er = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) == hr ? ERROR_INSTALL_USEREXIT : ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/******************************************************************
 WixCloseApplicationsDeferred - entry point for 
                                WixCloseApplicationsDeferred Custom Action
                                called as Type 1025 CustomAction 
                                (deferred binary DLL)

 NOTE: deferred CustomAction since it modifies the machine
 NOTE: CustomActionData == wzTarget\tdwAttributes\tdwTimeout\tdwTerminateExitCode\t...
******************************************************************/
extern "C" UINT __stdcall WixCloseApplicationsDeferred(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug WixCloseApplicationsDeferred");
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    LPWSTR pwz = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzTarget = NULL;
    DWORD dwAttributes = 0;
    DWORD dwTimeout = 0;
    DWORD dwTerminateExitCode = 0;

    DWORD *prgProcessIds = NULL;
    DWORD cProcessIds = 0;

    //
    // initialize
    //
    hr = WcaInitialize(hInstall, "WixCloseApplicationsDeferred");
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
        hr = WcaReadStringFromCaData(&pwz, &pwzTarget);
        ExitOnFailure(hr, "failed to process target from CustomActionData");

        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwAttributes));
        ExitOnFailure(hr, "failed to process attributes from CustomActionData");

        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwTimeout));
        ExitOnFailure(hr, "failed to process timeout from CustomActionData");

        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&dwTerminateExitCode));
        ExitOnFailure(hr, "failed to process terminate exit code from CustomActionData");

        WcaLog(LOGMSG_VERBOSE, "Checking for App: %ls Attributes: %d", pwzTarget, dwAttributes);

        //
        // send WM_CLOSE or WM_QUERYENDSESSION to currently running applications
        //
        if (dwAttributes & CLOSEAPP_ATTRIBUTE_ELEVATEDCLOSEMESSAGE)
        {
            SendApplicationMessage(pwzTarget, WM_CLOSE, dwTimeout);
        }

        if (dwAttributes & CLOSEAPP_ATTRIBUTE_ELEVATEDENDSESSIONMESSAGE)
        {
            SendApplicationMessage(pwzTarget, WM_QUERYENDSESSION, dwTimeout);
        }

        // If we find that an app that we need closed is still runing, require a
        // restart or kill the process as directed.
        ProcFindAllIdsFromExeName(pwzTarget, &prgProcessIds, &cProcessIds);
        if (0 < cProcessIds)
        {
            if (dwAttributes & CLOSEAPP_ATTRIBUTE_REBOOTPROMPT)
            {
                WcaLog(LOGMSG_VERBOSE, "App: %ls found running, requiring a reboot.", pwzTarget);

                WcaDeferredActionRequiresReboot();
            }
            else if (dwAttributes & CLOSEAPP_ATTRIBUTE_TERMINATEPROCESS)
            {
                TerminateProcesses(prgProcessIds, cProcessIds, dwTerminateExitCode);
            }
        }

        hr = WcaProgressMessage(COST_CLOSEAPP, FALSE);
        ExitOnFailure(hr, "failed to send progress message");
    }

LExit:
    ReleaseMem(prgProcessIds);

    ReleaseStr(pwzTarget);
    ReleaseStr(pwzData);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
