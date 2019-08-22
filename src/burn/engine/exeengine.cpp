// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// internal function declarations

static HRESULT HandleExitCode(
    __in BURN_PACKAGE* pPackage,
    __in DWORD dwExitCode,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT ParseCommandLineArgumentsFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    );
static HRESULT ParseExitCodesFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    );


// function definitions

extern "C" HRESULT ExeEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    LPWSTR scz = NULL;

    // @DetectCondition
    hr = XmlGetAttributeEx(pixnExePackage, L"DetectCondition", &pPackage->Exe.sczDetectCondition);
    ExitOnFailure(hr, "Failed to get @DetectCondition.");

    // @InstallArguments
    hr = XmlGetAttributeEx(pixnExePackage, L"InstallArguments", &pPackage->Exe.sczInstallArguments);
    ExitOnFailure(hr, "Failed to get @InstallArguments.");

    // @UninstallArguments
    hr = XmlGetAttributeEx(pixnExePackage, L"UninstallArguments", &pPackage->Exe.sczUninstallArguments);
    ExitOnFailure(hr, "Failed to get @UninstallArguments.");

    // @RepairArguments
    hr = XmlGetAttributeEx(pixnExePackage, L"RepairArguments", &pPackage->Exe.sczRepairArguments);
    ExitOnFailure(hr, "Failed to get @RepairArguments.");

    // @Repairable
    hr = XmlGetYesNoAttribute(pixnExePackage, L"Repairable", &pPackage->Exe.fRepairable);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get @Repairable.");
    }

    // @Protocol
    hr = XmlGetAttributeEx(pixnExePackage, L"Protocol", &scz);
    if (SUCCEEDED(hr))
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"burn", -1))
        {
            pPackage->Exe.protocol = BURN_EXE_PROTOCOL_TYPE_BURN;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"netfx4", -1))
        {
            pPackage->Exe.protocol = BURN_EXE_PROTOCOL_TYPE_NETFX4;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"none", -1))
        {
            pPackage->Exe.protocol = BURN_EXE_PROTOCOL_TYPE_NONE;
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Invalid protocol type: %ls", scz);
        }
    }
    else if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get @Protocol.");
    }

    hr = ParseExitCodesFromXml(pixnExePackage, pPackage);
    ExitOnFailure(hr, "Failed to parse exit codes.");

    hr = ParseCommandLineArgumentsFromXml(pixnExePackage, pPackage);
    ExitOnFailure(hr, "Failed to parse command lines.");

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" void ExeEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
    ReleaseStr(pPackage->Exe.sczDetectCondition);
    ReleaseStr(pPackage->Exe.sczInstallArguments);
    ReleaseStr(pPackage->Exe.sczRepairArguments);
    ReleaseStr(pPackage->Exe.sczUninstallArguments);
    ReleaseStr(pPackage->Exe.sczIgnoreDependencies);
    ReleaseStr(pPackage->Exe.sczAncestors);
    //ReleaseStr(pPackage->Exe.sczProgressSwitch);
    ReleaseMem(pPackage->Exe.rgExitCodes);

    // free command-line arguments
    if (pPackage->Exe.rgCommandLineArguments)
    {
        for (DWORD i = 0; i < pPackage->Exe.cCommandLineArguments; ++i)
        {
            BURN_EXE_COMMAND_LINE_ARGUMENT* pCommandLineArgument = &pPackage->Exe.rgCommandLineArguments[i];
            ReleaseStr(pCommandLineArgument->sczInstallArgument);
            ReleaseStr(pCommandLineArgument->sczUninstallArgument);
            ReleaseStr(pCommandLineArgument->sczRepairArgument);
            ReleaseStr(pCommandLineArgument->sczCondition);
        }
        MemFree(pPackage->Exe.rgCommandLineArguments);
    }

    // clear struct
    memset(&pPackage->Exe, 0, sizeof(pPackage->Exe));
}

extern "C" HRESULT ExeEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BOOL fDetected = FALSE;

    // evaluate detect condition
    if (pPackage->Exe.sczDetectCondition && *pPackage->Exe.sczDetectCondition)
    {
        hr = ConditionEvaluate(pVariables, pPackage->Exe.sczDetectCondition, &fDetected);
        ExitOnFailure(hr, "Failed to evaluate executable package detect condition.");
    }

    // update detect state
    pPackage->currentState = fDetected ? BOOTSTRAPPER_PACKAGE_STATE_PRESENT : BOOTSTRAPPER_PACKAGE_STATE_ABSENT;

LExit:
    return hr;
}

//
// PlanCalculate - calculates the execute and rollback state for the requested package state.
//
extern "C" HRESULT ExeEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage,
    __out_opt BOOL* pfBARequestedCache
    )
{
    HRESULT hr = S_OK;
    //BOOL fCondition = FALSE;
    //BOOTSTRAPPER_PACKAGE_STATE expected = BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN;
    BOOTSTRAPPER_ACTION_STATE execute = BOOTSTRAPPER_ACTION_STATE_NONE;
    BOOTSTRAPPER_ACTION_STATE rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
    BOOL fBARequestedCache = FALSE;

    //// evaluate rollback install condition
    //if (pPackage->sczRollbackInstallCondition)
    //{
    //    hr = ConditionEvaluate(pVariables, pPackage->sczRollbackInstallCondition, &fCondition);
    //    ExitOnFailure(hr, "Failed to evaluate rollback install condition.");

    //    expected = fCondition ? BOOTSTRAPPER_PACKAGE_STATE_PRESENT : BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
    //}

    // execute action
    switch (pPackage->currentState)
    {
    case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT:
            execute = pPackage->Exe.fPseudoBundle ? BOOTSTRAPPER_ACTION_STATE_INSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = pPackage->Exe.fRepairable ? BOOTSTRAPPER_ACTION_STATE_REPAIR : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_ABSENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_CACHE:
            execute = pPackage->fUninstallable ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
            execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
            break;
        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        }
        break;

    case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_CACHE:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            fBARequestedCache = TRUE;
            break;
        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure1(hr, "Invalid package current state: %d.", pPackage->currentState);
    }

    // Calculate the rollback action if there is an execute action.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != execute)
    {
        switch (BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN != pPackage->expected ? pPackage->expected : pPackage->currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
            switch (pPackage->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
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
                rollback = pPackage->fUninstallable ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
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

    if (pfBARequestedCache)
    {
        *pfBARequestedCache = fBARequestedCache;
    }

LExit:
    return hr;
}

//
// PlanAdd - adds the calculated execute and rollback actions for the package.
//
extern "C" HRESULT ExeEnginePlanAddPackage(
    __in_opt DWORD *pdwInsertSequence,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt HANDLE hCacheEvent,
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

    hr = DependencyPlanPackage(pdwInsertSequence, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

    // add execute action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute)
    {
        if (NULL != pdwInsertSequence)
        {
            hr = PlanInsertExecuteAction(*pdwInsertSequence, pPlan, &pAction);
            ExitOnFailure(hr, "Failed to insert execute action.");
        }
        else
        {
            hr = PlanAppendExecuteAction(pPlan, &pAction);
            ExitOnFailure(hr, "Failed to append execute action.");
        }

        pAction->type = BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE;
        pAction->exePackage.pPackage = pPackage;
        pAction->exePackage.fFireAndForget = (BOOTSTRAPPER_ACTION_UPDATE_REPLACE == pPlan->action);
        pAction->exePackage.action = pPackage->execute;

        if (pPackage->Exe.sczIgnoreDependencies)
        {
            hr = StrAllocString(&pAction->exePackage.sczIgnoreDependencies, pPackage->Exe.sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
        }

        if (pPackage->Exe.sczAncestors)
        {
            hr = StrAllocString(&pAction->exePackage.sczAncestors, pPackage->Exe.sczAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        LoggingSetPackageVariable(pPackage, NULL, FALSE, pLog, pVariables, NULL); // ignore errors.
    }

    // add rollback action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE;
        pAction->exePackage.pPackage = pPackage;
        pAction->exePackage.action = pPackage->rollback;

        if (pPackage->Exe.sczIgnoreDependencies)
        {
            hr = StrAllocString(&pAction->exePackage.sczIgnoreDependencies, pPackage->Exe.sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
        }

        if (pPackage->Exe.sczAncestors)
        {
            hr = StrAllocString(&pAction->exePackage.sczAncestors, pPackage->Exe.sczAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        LoggingSetPackageVariable(pPackage, NULL, TRUE, pLog, pVariables, NULL); // ignore errors.
    }

LExit:
    return hr;
}

extern "C" HRESULT ExeEngineExecutePackage(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    WCHAR wzCurrentDirectory[MAX_PATH] = { };
    BOOL fChangedCurrentDirectory = FALSE;
    int nResult = IDNOACTION;
    LPCWSTR wzArguments = NULL;
    LPWSTR sczArguments = NULL;
    LPWSTR sczArgumentsFormatted = NULL;
    LPWSTR sczArgumentsObfuscated = NULL;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczExecutablePath = NULL;
    LPWSTR sczCommand = NULL;
    LPWSTR sczCommandObfuscated = NULL;
    HANDLE hExecutableFile = INVALID_HANDLE_VALUE;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };
    DWORD dwExitCode = 0;
    GENERIC_EXECUTE_MESSAGE message = { };

    // get cached executable path
    hr = CacheGetCompletedPath(pExecuteAction->exePackage.pPackage->fPerMachine, pExecuteAction->exePackage.pPackage->sczCacheId, &sczCachedDirectory);
    ExitOnFailure(hr, "Failed to get cached path for package: %ls", pExecuteAction->exePackage.pPackage->sczId);

    // Best effort to set the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, sczCachedDirectory, TRUE);
    VariableSetNumeric(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, pExecuteAction->exePackage.action, TRUE);

    hr = PathConcat(sczCachedDirectory, pExecuteAction->exePackage.pPackage->rgPayloads[0].pPayload->sczFilePath, &sczExecutablePath);
    ExitOnFailure(hr, "Failed to build executable path.");

    // pick arguments
    switch (pExecuteAction->exePackage.action)
    {
    case BOOTSTRAPPER_ACTION_STATE_INSTALL:
        wzArguments = pExecuteAction->exePackage.pPackage->Exe.sczInstallArguments;
        break;

    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        wzArguments = pExecuteAction->exePackage.pPackage->Exe.sczUninstallArguments;
        break;

    case BOOTSTRAPPER_ACTION_STATE_REPAIR:
        wzArguments = pExecuteAction->exePackage.pPackage->Exe.sczRepairArguments;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid Exe package action: %d.", pExecuteAction->exePackage.action);
    }

    // now add optional arguments
    hr = StrAllocString(&sczArguments, wzArguments && *wzArguments ? wzArguments : L"", 0);
    ExitOnFailure(hr, "Failed to copy package arguments.");

    for (DWORD i = 0; i < pExecuteAction->exePackage.pPackage->Exe.cCommandLineArguments; ++i)
    {
        BURN_EXE_COMMAND_LINE_ARGUMENT* commandLineArgument = &pExecuteAction->exePackage.pPackage->Exe.rgCommandLineArguments[i];
        BOOL fCondition = FALSE;

        hr = ConditionEvaluate(pVariables, commandLineArgument->sczCondition, &fCondition);
        ExitOnFailure(hr, "Failed to evaluate executable package command-line condition.");

        if (fCondition)
        {
            hr = StrAllocConcat(&sczArguments, L" ", 0);
            ExitOnFailure(hr, "Failed to separate command-line arguments.");

            switch (pExecuteAction->exePackage.action)
            {
            case BOOTSTRAPPER_ACTION_STATE_INSTALL:
                hr = StrAllocConcat(&sczArguments, commandLineArgument->sczInstallArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for install.");
                break;

            case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
                hr = StrAllocConcat(&sczArguments, commandLineArgument->sczUninstallArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for uninstall.");
                break;

            case BOOTSTRAPPER_ACTION_STATE_REPAIR:
                hr = StrAllocConcat(&sczArguments, commandLineArgument->sczRepairArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for repair.");
                break;

            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid Exe package action: %d.", pExecuteAction->exePackage.action);
            }
        }
    }

    // build command
    if (0 < lstrlenW(sczArguments))
    {
        hr = VariableFormatString(pVariables, sczArguments, &sczArgumentsFormatted, NULL);
        ExitOnFailure(hr, "Failed to format argument string.");

        hr = StrAllocFormattedSecure(&sczCommand, L"\"%ls\" %s", sczExecutablePath, sczArgumentsFormatted);
        ExitOnFailure(hr, "Failed to create executable command.");

        hr = VariableFormatStringObfuscated(pVariables, sczArguments, &sczArgumentsObfuscated, NULL);
        ExitOnFailure(hr, "Failed to format obfuscated argument string.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"\"%ls\" %s", sczExecutablePath, sczArgumentsObfuscated);
    }
    else
    {
        hr = StrAllocFormatted(&sczCommand, L"\"%ls\"", sczExecutablePath);
        ExitOnFailure(hr, "Failed to create executable command.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"\"%ls\"", sczExecutablePath);
    }
    ExitOnFailure(hr, "Failed to create obfuscated executable command.");

    if (pExecuteAction->exePackage.pPackage->Exe.fSupportsAncestors)
    {
        // Add the list of dependencies to ignore, if any, to the burn command line.
        if (pExecuteAction->exePackage.sczIgnoreDependencies && BURN_EXE_PROTOCOL_TYPE_BURN == pExecuteAction->exePackage.pPackage->Exe.protocol)
        {
            hr = StrAllocFormattedSecure(&sczCommand, L"%ls -%ls=%ls", sczCommand, BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES, pExecuteAction->exePackage.sczIgnoreDependencies);
            ExitOnFailure(hr, "Failed to append the list of dependencies to ignore to the command line.");

            hr = StrAllocFormatted(&sczCommandObfuscated, L"%ls -%ls=%ls", sczCommandObfuscated, BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES, pExecuteAction->exePackage.sczIgnoreDependencies);
            ExitOnFailure(hr, "Failed to append the list of dependencies to ignore to the obfuscated command line.");
        }

        // Add the list of ancestors, if any, to the burn command line.
        if (pExecuteAction->exePackage.sczAncestors)
        {
            hr = StrAllocFormattedSecure(&sczCommand, L"%ls -%ls=%ls", sczCommand, BURN_COMMANDLINE_SWITCH_ANCESTORS, pExecuteAction->exePackage.sczAncestors);
            ExitOnFailure(hr, "Failed to append the list of ancestors to the command line.");

            hr = StrAllocFormatted(&sczCommandObfuscated, L"%ls -%ls=%ls", sczCommandObfuscated, BURN_COMMANDLINE_SWITCH_ANCESTORS, pExecuteAction->exePackage.sczAncestors);
            ExitOnFailure(hr, "Failed to append the list of ancestors to the obfuscated command line.");
        }
    }

    if (BURN_EXE_PROTOCOL_TYPE_BURN == pExecuteAction->exePackage.pPackage->Exe.protocol)
    {
        hr = CoreAppendFileHandleSelfToCommandLine(sczExecutablePath, &hExecutableFile, &sczCommand, &sczCommandObfuscated);
        ExitOnFailure(hr, "Failed to append %ls", BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF);
    }

    // Log before we add the secret pipe name and client token for embedded processes.
    LogId(REPORT_STANDARD, MSG_APPLYING_PACKAGE, LoggingRollbackOrExecute(fRollback), pExecuteAction->exePackage.pPackage->sczId, LoggingActionStateToString(pExecuteAction->exePackage.action), sczExecutablePath, sczCommandObfuscated);

    if (!pExecuteAction->exePackage.fFireAndForget && BURN_EXE_PROTOCOL_TYPE_BURN == pExecuteAction->exePackage.pPackage->Exe.protocol)
    {
        hr = EmbeddedRunBundle(sczExecutablePath, sczCommand, pfnGenericMessageHandler, pvContext, &dwExitCode);
        ExitOnFailure(hr, "Failed to run bundle as embedded from path: %ls", sczExecutablePath);
    }
    else if (!pExecuteAction->exePackage.fFireAndForget && BURN_EXE_PROTOCOL_TYPE_NETFX4 == pExecuteAction->exePackage.pPackage->Exe.protocol)
    {
        hr = NetFxRunChainer(sczExecutablePath, sczCommand, pfnGenericMessageHandler, pvContext, &dwExitCode);
        ExitOnFailure(hr, "Failed to run netfx chainer: %ls", sczExecutablePath);
    }
    else // create and wait for the executable process while sending fake progress to allow cancel.
    {
        // Make the cache location of the executable the current directory to help those executables
        // that expect stuff to be relative to them.
        if (::GetCurrentDirectoryW(countof(wzCurrentDirectory), wzCurrentDirectory))
        {
            fChangedCurrentDirectory = ::SetCurrentDirectoryW(sczCachedDirectory);
        }

        si.cb = sizeof(si); // TODO: hookup the stdin/stdout/stderr pipes for logging purposes?
        if (!::CreateProcessW(sczExecutablePath, sczCommand, NULL, NULL, TRUE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi))
        {
            ExitWithLastError(hr, "Failed to CreateProcess on path: %ls", sczExecutablePath);
        }

        if (pExecuteAction->exePackage.fFireAndForget)
        {
            ::WaitForInputIdle(pi.hProcess, 5000);
            ExitFunction();
        }

        do
        {
            message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
            message.dwAllowedResults = MB_OKCANCEL;
            message.progress.dwPercentage = 50;
            nResult = pfnGenericMessageHandler(&message, pvContext);
            hr = (IDOK == nResult || IDNOACTION == nResult) ? S_OK : IDCANCEL == nResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
            ExitOnRootFailure(hr, "Bootstrapper application aborted during EXE progress.");

            hr = ProcWaitForCompletion(pi.hProcess, 500, &dwExitCode);
            if (HRESULT_FROM_WIN32(WAIT_TIMEOUT) != hr)
            {
                ExitOnFailure(hr, "Failed to wait for executable to complete: %ls", sczExecutablePath);
            }
        } while (HRESULT_FROM_WIN32(WAIT_TIMEOUT) == hr);
    }

    hr = HandleExitCode(pExecuteAction->exePackage.pPackage, dwExitCode, pRestart);
    ExitOnRootFailure(hr, "Process returned error: 0x%x", dwExitCode);

LExit:
    if (fChangedCurrentDirectory)
    {
        ::SetCurrentDirectoryW(wzCurrentDirectory);
    }

    StrSecureZeroFreeString(sczArguments);
    StrSecureZeroFreeString(sczArgumentsFormatted);
    ReleaseStr(sczArgumentsObfuscated);
    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczExecutablePath);
    StrSecureZeroFreeString(sczCommand);
    ReleaseStr(sczCommandObfuscated);

    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);
    ReleaseFileHandle(hExecutableFile);

    // Best effort to clear the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, NULL, TRUE);
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, NULL, TRUE);

    return hr;
}


// internal helper functions

static HRESULT ParseExitCodesFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select exit code nodes
    hr = XmlSelectNodes(pixnExePackage, L"ExitCode", &pixnNodes);
    ExitOnFailure(hr, "Failed to select exit code nodes.");

    // get exit code node count
    hr = pixnNodes->get_length((long*) &cNodes);
    ExitOnFailure(hr, "Failed to get exit code node count.");

    if (cNodes)
    {
        // allocate memory for exit codes
        pPackage->Exe.rgExitCodes = (BURN_EXE_EXIT_CODE*) MemAlloc(sizeof(BURN_EXE_EXIT_CODE) * cNodes, TRUE);
        ExitOnNull(pPackage->Exe.rgExitCodes, hr, E_OUTOFMEMORY, "Failed to allocate memory for exit code structs.");

        pPackage->Exe.cExitCodes = cNodes;

        // parse package elements
        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_EXE_EXIT_CODE* pExitCode = &pPackage->Exe.rgExitCodes[i];

            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next node.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"success", -1))
            {
                pExitCode->type = BURN_EXE_EXIT_CODE_TYPE_SUCCESS;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"error", -1))
            {
                pExitCode->type = BURN_EXE_EXIT_CODE_TYPE_ERROR;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"scheduleReboot", -1))
            {
                pExitCode->type = BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"forceReboot", -1))
            {
                pExitCode->type = BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT;
            }
            else
            {
                hr = E_UNEXPECTED;
                ExitOnFailure(hr, "Invalid exit code type: %ls", scz);
            }

            // @Code
            hr = XmlGetAttributeEx(pixnNode, L"Code", &scz);
            ExitOnFailure(hr, "Failed to get @Code.");

            if (L'*' == scz[0])
            {
                pExitCode->fWildcard = TRUE;
            }
            else
            {
                hr = StrStringToUInt32(scz, 0, (UINT*) &pExitCode->dwCode);
                ExitOnFailure(hr, "Failed to parse @Code value: %ls", scz);
            }

            // prepare next iteration
            ReleaseNullObject(pixnNode);
        }
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

static HRESULT ParseCommandLineArgumentsFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // Select command-line argument nodes.
    hr = XmlSelectNodes(pixnExePackage, L"CommandLine", &pixnNodes);
    ExitOnFailure(hr, "Failed to select command-line argument nodes.");

    // Get command-line argument node count.
    hr = pixnNodes->get_length((long*) &cNodes);
    ExitOnFailure(hr, "Failed to get command-line argument count.");

    if (cNodes)
    {
        pPackage->Exe.rgCommandLineArguments = (BURN_EXE_COMMAND_LINE_ARGUMENT*) MemAlloc(sizeof(BURN_EXE_COMMAND_LINE_ARGUMENT) * cNodes, TRUE);
        ExitOnNull(pPackage->Exe.rgCommandLineArguments, hr, E_OUTOFMEMORY, "Failed to allocate memory for command-line argument structs.");

        pPackage->Exe.cCommandLineArguments = cNodes;

        // Parse command-line argument elements.
        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_EXE_COMMAND_LINE_ARGUMENT* pCommandLineArgument = &pPackage->Exe.rgCommandLineArguments[i];

            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next command-line argument node.");

            // @InstallArgument
            hr = XmlGetAttributeEx(pixnNode, L"InstallArgument", &pCommandLineArgument->sczInstallArgument);
            ExitOnFailure(hr, "Failed to get @InstallArgument.");

            // @UninstallArgument
            hr = XmlGetAttributeEx(pixnNode, L"UninstallArgument", &pCommandLineArgument->sczUninstallArgument);
            ExitOnFailure(hr, "Failed to get @UninstallArgument.");

            // @RepairArgument
            hr = XmlGetAttributeEx(pixnNode, L"RepairArgument", &pCommandLineArgument->sczRepairArgument);
            ExitOnFailure(hr, "Failed to get @RepairArgument.");

            // @Condition
            hr = XmlGetAttributeEx(pixnNode, L"Condition", &pCommandLineArgument->sczCondition);
            ExitOnFailure(hr, "Failed to get @Condition.");

            // Prepare next iteration.
            ReleaseNullObject(pixnNode);
        }
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

static HRESULT HandleExitCode(
    __in BURN_PACKAGE* pPackage,
    __in DWORD dwExitCode,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BURN_EXE_EXIT_CODE_TYPE typeCode = BURN_EXE_EXIT_CODE_TYPE_NONE;

    for (DWORD i = 0; i < pPackage->Exe.cExitCodes; ++i)
    {
        BURN_EXE_EXIT_CODE* pExitCode = &pPackage->Exe.rgExitCodes[i];

        // If this is a wildcard, use the last one we come across.
        if (pExitCode->fWildcard)
        {
            typeCode = pExitCode->type;
        }
        else if (dwExitCode == pExitCode->dwCode) // If we have an exact match on the error code use that and stop looking.
        {
            typeCode = pExitCode->type;
            break;
        }
    }

    // If we didn't find a matching code then treat 0 as success, the standard restarts codes as restarts
    // and everything else as an error.
    if (BURN_EXE_EXIT_CODE_TYPE_NONE == typeCode)
    {
        if (0 == dwExitCode)
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_SUCCESS;
        }
        else if (ERROR_SUCCESS_REBOOT_REQUIRED == dwExitCode ||
                 HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED) == static_cast<HRESULT>(dwExitCode) ||
                 ERROR_SUCCESS_RESTART_REQUIRED == dwExitCode ||
                 HRESULT_FROM_WIN32(ERROR_SUCCESS_RESTART_REQUIRED) == static_cast<HRESULT>(dwExitCode))
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT;
        }
        else if (ERROR_SUCCESS_REBOOT_INITIATED == dwExitCode ||
                 HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_INITIATED) == static_cast<HRESULT>(dwExitCode))
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT;
        }
        else
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_ERROR;
        }
    }

    switch (typeCode)
    {
    case BURN_EXE_EXIT_CODE_TYPE_SUCCESS:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;
        hr = S_OK;
        break;

    case BURN_EXE_EXIT_CODE_TYPE_ERROR:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;
        hr = HRESULT_FROM_WIN32(dwExitCode);
        if (SUCCEEDED(hr))
        {
            hr = E_FAIL;
        }
        break;

    case BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_REQUIRED;
        hr = S_OK;
        break;

    case BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_INITIATED;
        hr = S_OK;
        break;

    default:
        hr = E_UNEXPECTED;
        break;
    }

//LExit:
    return hr;
}
