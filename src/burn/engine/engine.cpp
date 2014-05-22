//-------------------------------------------------------------------------------------------------
// <copyright file="engine.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// constants

const DWORD RESTART_RETRIES = 10;

// internal function declarations

static HRESULT InitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    );
static void UninitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunNormal(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunElevated(
    __in HINSTANCE hInstance,
    __in LPCWSTR wzCommandLine,
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunEmbedded(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunRunOnce(
    __in_z_opt LPCWSTR wzCommandLine,
    __in int nCmdShow
    );
static HRESULT RunApplication(
    __in BURN_ENGINE_STATE* pEngineState,
    __out BOOL* pfReloadApp
    );
static HRESULT ProcessMessage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const MSG* pmsg
    );
static HRESULT DAPI RedirectLoggingOverPipe(
    __in_z LPCSTR szString,
    __in_opt LPVOID pvContext
    );
static HRESULT Restart();


// function definitions

extern "C" HRESULT EngineRun(
    __in HINSTANCE hInstance,
    __in_z_opt LPCWSTR wzCommandLine,
    __in int nCmdShow,
    __out DWORD* pdwExitCode
    )
{
    HRESULT hr = S_OK;
    BOOL fComInitialized = FALSE;
    BOOL fLogInitialized = FALSE;
    BOOL fCrypInitialized = FALSE;
    BOOL fRegInitialized = FALSE;
    BOOL fWiuInitialized = FALSE;
    BOOL fXmlInitialized = FALSE;
    OSVERSIONINFOEXW ovix = { };
    LPWSTR sczExePath = NULL;
    BOOL fRunNormal = FALSE;
    BOOL fRestart = FALSE;

    BURN_ENGINE_STATE engineState = { };

    hr = InitializeEngineState(&engineState);
    ExitOnFailure(hr, "Failed to initialize engine state.");

    engineState.command.nCmdShow = nCmdShow;

    // Ensure that log contains approriate level of information
#ifdef _DEBUG
    LogSetLevel(REPORT_DEBUG, FALSE);
#else
    LogSetLevel(REPORT_VERBOSE, FALSE); // FALSE means don't write an additional text line to the log saying the level changed
#endif

    // initialize platform layer
    PlatformInitialize();

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    // Initialize dutil.
    LogInitialize(::GetModuleHandleW(NULL));
    fLogInitialized = TRUE;

    hr = CrypInitialize();
    ExitOnFailure(hr, "Failed to initialize Cryputil.");
    fCrypInitialized = TRUE;

    hr = RegInitialize();
    ExitOnFailure(hr, "Failed to initialize Regutil.");
    fRegInitialized = TRUE;

    hr = WiuInitialize();
    ExitOnFailure(hr, "Failed to initialize Wiutil.");
    fWiuInitialized = TRUE;

    hr = XmlInitialize();
    ExitOnFailure(hr, "Failed to initialize XML util.");
    fXmlInitialized = TRUE;

    ovix.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
    if (!::GetVersionExW((LPOSVERSIONINFOW)&ovix))
    {
        ExitWithLastError(hr, "Failed to get OS info.");
    }

    PathForCurrentProcess(&sczExePath, NULL); // Ignore failure.
    LogId(REPORT_STANDARD, MSG_BURN_INFO, szVerMajorMinorBuild, ovix.dwMajorVersion, ovix.dwMinorVersion, ovix.dwBuildNumber, ovix.wServicePackMajor, sczExePath, wzCommandLine ? wzCommandLine : L"");
    ReleaseNullStr(sczExePath);

    // initialize core
    hr = CoreInitialize(wzCommandLine, &engineState);
    ExitOnFailure(hr, "Failed to initialize core.");

    // select run mode
    switch (engineState.mode)
    {
    case BURN_MODE_NORMAL:
        fRunNormal = TRUE;

        hr = RunNormal(hInstance, &engineState);
        ExitOnFailure(hr, "Failed to run per-user mode.");
        break;

    case BURN_MODE_ELEVATED:
        hr = RunElevated(hInstance, wzCommandLine, &engineState);
        ExitOnFailure(hr, "Failed to run per-machine mode.");
        break;

    case BURN_MODE_EMBEDDED:
        fRunNormal = TRUE;

        hr = RunEmbedded(hInstance, &engineState);
        ExitOnFailure(hr, "Failed to run embedded mode.");
        break;

    case BURN_MODE_RUNONCE:
        hr = RunRunOnce(wzCommandLine, nCmdShow);
        ExitOnFailure(hr, "Failed to run RunOnce mode.");
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Invalid run mode.");
    }

    // set exit code and remember if we are supposed to restart.
    *pdwExitCode = engineState.userExperience.dwExitCode;
    fRestart = engineState.fRestart;

LExit:
    ReleaseStr(sczExePath);

    // If anything went wrong but the log was never open, try to open a "failure" log
    // and that will dump anything captured in the log memory buffer to the log.
    if (FAILED(hr) && BURN_LOGGING_STATE_CLOSED == engineState.log.state)
    {
        LogOpen(NULL, L"Setup", L"_Failed", L"txt", FALSE, FALSE, NULL);
    }

    UserExperienceRemove(&engineState.userExperience);

    CacheRemoveWorkingFolder(engineState.registration.sczId);

    // If this is a related bundle (but not an update) suppress restart and return the standard restart error code.
    if (fRestart && BOOTSTRAPPER_RELATION_NONE != engineState.command.relationType && BOOTSTRAPPER_RELATION_UPDATE != engineState.command.relationType)
    {
        LogId(REPORT_STANDARD, MSG_RESTART_ABORTED, LoggingRelationTypeToString(engineState.command.relationType));

        fRestart = FALSE;
        hr = HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED);
    }

    UninitializeEngineState(&engineState);

    if (fXmlInitialized)
    {
        XmlUninitialize();
    }

    if (fWiuInitialized)
    {
        WiuUninitialize();
    }

    if (fRegInitialized)
    {
        RegUninitialize();
    }

    if (fCrypInitialized)
    {
        CrypUninitialize();
    }

    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    if (fRunNormal)
    {
        LogId(REPORT_STANDARD, MSG_EXITING, FAILED(hr) ? (int)hr : *pdwExitCode, LoggingBoolToString(fRestart));

        if (fRestart)
        {
            LogId(REPORT_STANDARD, MSG_RESTARTING);
        }
    }

    if (fLogInitialized)
    {
        LogClose(FALSE);
    }

    if (fRestart)
    {
        Restart();
    }

    if (fLogInitialized)
    {
        LogUninitialize(FALSE);
    }

    return hr;
}


// internal function definitions

static HRESULT InitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    BOOL fElevated = FALSE;

    pEngineState->automaticUpdates = BURN_AU_PAUSE_ACTION_IFELEVATED;
    pEngineState->dwElevatedLoggingTlsId = TLS_OUT_OF_INDEXES;
    ::InitializeCriticalSection(&pEngineState->csActive);
    ::InitializeCriticalSection(&pEngineState->userExperience.csEngineActive);
    PipeConnectionInitialize(&pEngineState->companionConnection);
    PipeConnectionInitialize(&pEngineState->embeddedConnection);

    ProcElevated(::GetCurrentProcess(), &fElevated);
    pEngineState->elevationState = fElevated ? BURN_ELEVATION_STATE_ELEVATED : BURN_ELEVATION_STATE_UNELEVATED;

    hr = SectionInitialize(&pEngineState->section);
    ExitOnFailure(hr, "Failed to initialize engine section.");

LExit:
    return hr;
}

static void UninitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    ReleaseStr(pEngineState->sczIgnoreDependencies);

    PipeConnectionUninitialize(&pEngineState->embeddedConnection);
    PipeConnectionUninitialize(&pEngineState->companionConnection);
    ReleaseStr(pEngineState->sczBundleEngineWorkingPath)

    ReleaseHandle(pEngineState->hMessageWindowThread);

    ::DeleteCriticalSection(&pEngineState->userExperience.csEngineActive);
    UserExperienceUninitialize(&pEngineState->userExperience);

    UpdateUninitialize(&pEngineState->update);
    VariablesUninitialize(&pEngineState->variables);
    SearchesUninitialize(&pEngineState->searches);
    RegistrationUninitialize(&pEngineState->registration);
    PayloadsUninitialize(&pEngineState->payloads);
    PackagesUninitialize(&pEngineState->packages);
    CatalogUninitialize(&pEngineState->catalogs);
    SectionUninitialize(&pEngineState->section);
    ReleaseStr(pEngineState->command.wzLayoutDirectory);
    ReleaseStr(pEngineState->command.wzCommandLine);

    ReleaseStr(pEngineState->log.sczExtension);
    ReleaseStr(pEngineState->log.sczPrefix);
    ReleaseStr(pEngineState->log.sczPath);
    ReleaseStr(pEngineState->log.sczPathVariable);

    if (TLS_OUT_OF_INDEXES != pEngineState->dwElevatedLoggingTlsId)
    {
        ::TlsFree(pEngineState->dwElevatedLoggingTlsId);
    }

    ::DeleteCriticalSection(&pEngineState->csActive);

    // clear struct
    memset(pEngineState, 0, sizeof(BURN_ENGINE_STATE));
}

static HRESULT RunNormal(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    HANDLE hPipesCreatedEvent = NULL;
    BOOL fContinueExecution = TRUE;
    BOOL fReloadApp = FALSE;

    // Initialize logging.
    hr = LoggingOpen(&pEngineState->log, &pEngineState->variables, pEngineState->command.display, pEngineState->registration.sczDisplayName);
    ExitOnFailure(hr, "Failed to open log.");

    // Ensure the cache functions are initialized since we might use them soon.
    hr = CacheInitialize(&pEngineState->registration, &pEngineState->variables);
    ExitOnFailure(hr, "Failed to initialize internal cache functionality.");

    // When launched explicitly unelevated, create the pipes so the elevated process can connect.
    if (BURN_ELEVATION_STATE_UNELEVATED_EXPLICITLY == pEngineState->elevationState)
    {
        Assert(pEngineState->companionConnection.dwProcessId);
        Assert(pEngineState->companionConnection.sczName);
        Assert(pEngineState->companionConnection.sczSecret);
        Assert(!pEngineState->companionConnection.hProcess);
        Assert(INVALID_HANDLE_VALUE == pEngineState->companionConnection.hPipe);
        Assert(INVALID_HANDLE_VALUE == pEngineState->companionConnection.hCachePipe);

        hr = PipeCreatePipes(&pEngineState->companionConnection, TRUE, &hPipesCreatedEvent);
        ExitOnFailure(hr, "Failed to create pipes to connect to elevated parent process.");

        hr = PipeWaitForChildConnect(&pEngineState->companionConnection);
        ExitOnFailure(hr, "Failed to connect to elevated parent process.");

        ReleaseHandle(hPipesCreatedEvent);
    }

    // Ensure we're on a supported operating system.
    hr = ConditionGlobalCheck(&pEngineState->variables, &pEngineState->condition, pEngineState->command.display, pEngineState->registration.sczDisplayName, &pEngineState->userExperience.dwExitCode, &fContinueExecution);
    ExitOnFailure(hr, "Failed to check global conditions");

    if (!fContinueExecution)
    {
        LogId(REPORT_STANDARD, MSG_FAILED_CONDITION_CHECK);

        // If the block told us to abort, abort!
        ExitFunction1(hr = S_OK);
    }

    if (pEngineState->userExperience.fSplashScreen && BOOTSTRAPPER_DISPLAY_NONE < pEngineState->command.display)
    {
        SplashScreenCreate(hInstance, NULL, &pEngineState->command.hwndSplashScreen);
    }

    // Create a top-level window to handle system messages.
    hr = UiCreateMessageWindow(hInstance, pEngineState);
    ExitOnFailure(hr, "Failed to create the message window.");

    // Query registration state.
    hr = CoreQueryRegistration(pEngineState);
    ExitOnFailure(hr, "Failed to query registration.");

    // Set some built-in variables before loading the BA.
    hr = PlanSetVariables(pEngineState->command.action, &pEngineState->variables);
    ExitOnFailure(hr, "Failed to set action variables.");

    hr = RegistrationSetVariables(&pEngineState->registration, &pEngineState->variables);
    ExitOnFailure(hr, "Failed to set registration variables.");

    // If a layout directory was specified on the command-line, set it as a well-known variable.
    if (pEngineState->command.wzLayoutDirectory && *pEngineState->command.wzLayoutDirectory)
    {
        hr = VariableSetString(&pEngineState->variables, BURN_BUNDLE_LAYOUT_DIRECTORY, pEngineState->command.wzLayoutDirectory, FALSE);
        ExitOnFailure(hr, "Failed to set layout directory variable to value provided from command-line.");
    }

    do
    {
        fReloadApp = FALSE;

        hr = RunApplication(pEngineState, &fReloadApp);
        ExitOnFailure(hr, "Failed while running ");
    } while (fReloadApp);

LExit:
    // If the message window is still around, close it.
    UiCloseMessageWindow(pEngineState);

    VariablesDump(&pEngineState->variables);

    // end per-machine process if running
    if (INVALID_HANDLE_VALUE != pEngineState->companionConnection.hPipe)
    {
        PipeTerminateChildProcess(&pEngineState->companionConnection, pEngineState->userExperience.dwExitCode, (BURN_ELEVATION_STATE_UNELEVATED_EXPLICITLY == pEngineState->elevationState) ? pEngineState->fRestart : FALSE);
    }

    // If the splash screen is still around, close it.
    if (::IsWindow(pEngineState->command.hwndSplashScreen))
    {
        ::PostMessageW(pEngineState->command.hwndSplashScreen, WM_CLOSE, 0, 0);
    }

    ReleaseHandle(hPipesCreatedEvent);

    return hr;
}

static HRESULT RunElevated(
    __in HINSTANCE hInstance,
    __in LPCWSTR wzCommandLine,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    HANDLE hLock = NULL;
    BOOL fDisabledAutomaticUpdates = FALSE;

    // If we were launched elevated implicitly, launch an unelevated copy of ourselves.
    if (BURN_ELEVATION_STATE_ELEVATED == pEngineState->elevationState)
    {
        Assert(!pEngineState->companionConnection.dwProcessId);
        Assert(!pEngineState->companionConnection.sczName);
        Assert(!pEngineState->companionConnection.sczSecret);

        hr = PipeCreateNameAndSecret(&pEngineState->companionConnection.sczName, &pEngineState->companionConnection.sczSecret);
        ExitOnFailure(hr, "Failed to create implicit elevated connection name and secret.");

        hr = PipeLaunchParentProcess(wzCommandLine, pEngineState->command.nCmdShow, pEngineState->companionConnection.sczName, pEngineState->companionConnection.sczSecret, pEngineState->fDisableUnelevate);
        ExitOnFailure(hr, "Failed to launch unelevated process.");
    }

    // connect to per-user process
    hr = PipeChildConnect(&pEngineState->companionConnection, TRUE);
    ExitOnFailure(hr, "Failed to connect to unelevated process.");

    // Set up the thread local storage to store the correct pipe to communicate logging.
    pEngineState->dwElevatedLoggingTlsId = ::TlsAlloc();
    if (TLS_OUT_OF_INDEXES == pEngineState->dwElevatedLoggingTlsId)
    {
        ExitWithLastError(hr, "Failed to allocate thread local storage for logging.");
    }

    if (!::TlsSetValue(pEngineState->dwElevatedLoggingTlsId, pEngineState->companionConnection.hPipe))
    {
        ExitWithLastError(hr, "Failed to set elevated pipe into thread local storage for logging.");
    }

    // Create a top-level window to prevent shutting down the elevated process.
    hr = UiCreateMessageWindow(hInstance, pEngineState);
    ExitOnFailure(hr, "Failed to create the message window.");

    SrpInitialize(TRUE);

    // Override logging to write over the pipe.
    LogRedirect(RedirectLoggingOverPipe, pEngineState);

    // Pump messages from parent process.
    hr = ElevationChildPumpMessages(pEngineState->dwElevatedLoggingTlsId, pEngineState->companionConnection.hPipe, pEngineState->companionConnection.hCachePipe, &pEngineState->containers, &pEngineState->packages, &pEngineState->payloads, &pEngineState->variables, &pEngineState->registration, &pEngineState->userExperience, &hLock, &fDisabledAutomaticUpdates, &pEngineState->userExperience.dwExitCode, &pEngineState->fRestart);
    LogRedirect(NULL, NULL); // reset logging so the next failure gets written to "log buffer" for the failure log.
    ExitOnFailure(hr, "Failed to pump messages from parent process.");

LExit:
    LogRedirect(NULL, NULL); // we're done talking to the child so always reset logging now.

    // If the message window is still around, close it.
    UiCloseMessageWindow(pEngineState);

    if (fDisabledAutomaticUpdates)
    {
        ElevationChildResumeAutomaticUpdates();
    }

    if (hLock)
    {
        ::ReleaseMutex(hLock);
        ::CloseHandle(hLock);
    }

    return hr;
}

static HRESULT RunEmbedded(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;

    // Disable system restore since the parent bundle may have done it.
    pEngineState->fDisableSystemRestore = TRUE;

    // Connect to parent process.
    hr = PipeChildConnect(&pEngineState->embeddedConnection, FALSE);
    ExitOnFailure(hr, "Failed to connect to parent of embedded process.");

    // Now run the application like normal.
    hr = RunNormal(hInstance, pEngineState);
    ExitOnFailure(hr, "Failed to run bootstrapper application embedded.");

LExit:
    return hr;
}

static HRESULT RunRunOnce(
    __in_z_opt LPCWSTR wzCommandLine,
    __in int nCmdShow
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczNewCommandLine = NULL;
    LPWSTR sczBurnPath = NULL;
    HANDLE hProcess = NULL;
    int argc = 0;
    LPWSTR* argv = NULL;

    // rebuild the command line without the runonce switch
    if (wzCommandLine && *wzCommandLine)
    {
        argv = ::CommandLineToArgvW(wzCommandLine, &argc);
        ExitOnNullWithLastError(argv, hr, "Failed to get command line.");

        for (int i = 0; i < argc; ++i)
        {
            if (!((argv[i][0] == L'-' || argv[i][0] == L'/') && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_RUNONCE, -1)))
            {
                PathCommandLineAppend(&sczNewCommandLine, argv[i]);
            }
        }
    }

    // and re-launch
    hr = PathForCurrentProcess(&sczBurnPath, NULL);
    ExitOnFailure(hr, "Failed to get current process path.");

    hr = ProcExec(sczBurnPath, 0 < sczNewCommandLine ? sczNewCommandLine : L"", nCmdShow, &hProcess);
    ExitOnFailure1(hr, "Failed to re-launch bundle process after RunOnce: %ls", sczBurnPath);

LExit:
    if (argv)
    {
        ::LocalFree(argv);
    }

    ReleaseHandle(hProcess);
    ReleaseStr(sczNewCommandLine);
    ReleaseStr(sczBurnPath);

    return hr;
}

static HRESULT RunApplication(
    __in BURN_ENGINE_STATE* pEngineState,
    __out BOOL* pfReloadApp
    )
{
    HRESULT hr = S_OK;
    DWORD dwThreadId = 0;
    IBootstrapperEngine* pEngineForApplication = NULL;
    BOOL fStartupCalled = FALSE;
    BOOL fRet = FALSE;
    MSG msg = { };

    ::PeekMessageW(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);
    dwThreadId = ::GetCurrentThreadId();

    // Load the bootstrapper application.
    hr = EngineForApplicationCreate(pEngineState, dwThreadId, &pEngineForApplication);
    ExitOnFailure(hr, "Failed to create engine for UX.");

    hr = UserExperienceLoad(&pEngineState->userExperience, pEngineForApplication, &pEngineState->command);
    ExitOnFailure(hr, "Failed to load UX.");

    fStartupCalled = TRUE;
    hr = pEngineState->userExperience.pUserExperience->OnStartup();
    ExitOnFailure(hr, "Failed to start bootstrapper application.");

    // Enter the message pump.
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnRootFailure(hr, "Unexpected return value from message pump.");
        }
        else
        {
            ProcessMessage(pEngineState, &msg);
        }
    }

    // get exit code
    pEngineState->userExperience.dwExitCode = (DWORD)msg.wParam;

LExit:
    if (fStartupCalled)
    {
        int nResult = pEngineState->userExperience.pUserExperience->OnShutdown();
        if (IDRESTART == nResult)
        {
            LogId(REPORT_STANDARD, MSG_BA_REQUESTED_RESTART, LoggingBoolToString(pEngineState->fRestart));
            pEngineState->fRestart = TRUE;
        }
        else if (IDRELOAD_BOOTSTRAPPER == nResult)
        {
            LogId(REPORT_STANDARD, MSG_BA_REQUESTED_RELOAD);
            *pfReloadApp = TRUE;
        }
    }

    // unload UX
    UserExperienceUnload(&pEngineState->userExperience);

    ReleaseObject(pEngineForApplication);

    return hr;
}

static HRESULT ProcessMessage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const MSG* pmsg
    )
{
    HRESULT hr = S_OK;

    switch (pmsg->message)
    {
    case WM_BURN_DETECT:
        hr = CoreDetect(pEngineState);
        break;

    case WM_BURN_PLAN:
        hr = CorePlan(pEngineState, static_cast<BOOTSTRAPPER_ACTION>(pmsg->lParam));
        break;

    case WM_BURN_ELEVATE:
        hr = CoreElevate(pEngineState, reinterpret_cast<HWND>(pmsg->lParam));
        break;

    case WM_BURN_APPLY:
        hr = CoreApply(pEngineState, reinterpret_cast<HWND>(pmsg->lParam));
        break;

    case WM_BURN_QUIT:
        hr = CoreQuit(pEngineState, static_cast<int>(pmsg->wParam));
        break;
    }

    return hr;
}

static HRESULT DAPI RedirectLoggingOverPipe(
    __in_z LPCSTR szString,
    __in_opt LPVOID pvContext
    )
{
    static BOOL s_fCurrentlyLoggingToPipe = FALSE;

    HRESULT hr = S_OK;
    BURN_ENGINE_STATE* pEngineState = static_cast<BURN_ENGINE_STATE*>(pvContext);
    BOOL fStartedLogging = FALSE;
    HANDLE hPipe = INVALID_HANDLE_VALUE;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    // Prevent this function from being called recursively.
    if (s_fCurrentlyLoggingToPipe)
    {
        ExitFunction();
    }

    s_fCurrentlyLoggingToPipe = TRUE;
    fStartedLogging = TRUE;

    // Make sure the current thread set the pipe in TLS.
    hPipe = ::TlsGetValue(pEngineState->dwElevatedLoggingTlsId);
    if (!hPipe || INVALID_HANDLE_VALUE == hPipe)
    {
        hr = HRESULT_FROM_WIN32(ERROR_PIPE_NOT_CONNECTED);
        ExitFunction();
    }

    // Do not log or use ExitOnFailure() macro here because they will be discarded
    // by the recursive block at the top of this function.
    hr = BuffWriteStringAnsi(&pbData, &cbData, szString);
    if (SUCCEEDED(hr))
    {
        hr = PipeSendMessage(hPipe, static_cast<DWORD>(BURN_PIPE_MESSAGE_TYPE_LOG), pbData, cbData, NULL, NULL, &dwResult);
        if (SUCCEEDED(hr))
        {
            hr = (HRESULT)dwResult;
        }
    }

LExit:
    ReleaseBuffer(pbData);

    // We started logging so remember to say we are no longer logging.
    if (fStartedLogging)
    {
        s_fCurrentlyLoggingToPipe = FALSE;
    }

    return hr;
}

static HRESULT Restart()
{
    HRESULT hr = S_OK;
    HANDLE hProcessToken = NULL;
    TOKEN_PRIVILEGES priv = { };
    DWORD dwRetries = 0;

    if (!::OpenProcessToken(::GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES, &hProcessToken))
    {
        ExitWithLastError(hr, "Failed to get process token.");
    }

    priv.PrivilegeCount = 1;
    priv.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    if (!::LookupPrivilegeValueW(NULL, L"SeShutdownPrivilege", &priv.Privileges[0].Luid))
    {
        ExitWithLastError(hr, "Failed to get shutdown privilege LUID.");
    }

    if (!::AdjustTokenPrivileges(hProcessToken, FALSE, &priv, sizeof(TOKEN_PRIVILEGES), NULL, 0))
    {
        ExitWithLastError(hr, "Failed to adjust token to add shutdown privileges.");
    }

    do
    {
        hr = S_OK;

        // Wait a second to let the companion process (assuming we did an elevated install) to get to the
        // point where it too is thinking about restarting the computer. Only one will schedule the restart
        // but both will have their log files closed and otherwise be ready to exit.
        //
        // On retry, we'll also wait a second to let the OS try to get to a place where the restart can
        // be initiated.
        ::Sleep(1000);

        if (!vpfnInitiateSystemShutdownExW(NULL, NULL, 0, FALSE, TRUE, SHTDN_REASON_MAJOR_APPLICATION | SHTDN_REASON_MINOR_INSTALLATION | SHTDN_REASON_FLAG_PLANNED))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
        }
    } while (dwRetries++ < RESTART_RETRIES && (HRESULT_FROM_WIN32(ERROR_MACHINE_LOCKED) == hr || HRESULT_FROM_WIN32(ERROR_NOT_READY) == hr));
    ExitOnRootFailure(hr, "Failed to schedule restart.");

LExit:
    ReleaseHandle(hProcessToken);
    return hr;
}
