// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define BURN_UITHREAD_CLASS_WINDOW L"WixBurnMessageWindow"


// structs

struct UITHREAD_CONTEXT
{
    HANDLE hInitializedEvent;
    HINSTANCE hInstance;
    BURN_ENGINE_STATE* pEngineState;
};

struct UITHREAD_INFO
{
    BOOL fElevated;
    BURN_USER_EXPERIENCE* pUserExperience;
};


// internal function declarations

static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    );

static LRESULT CALLBACK WndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );


// function definitions

HRESULT UiCreateMessageWindow(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    HANDLE rgWaitHandles[2] = { };
    UITHREAD_CONTEXT context = { };

    // Create event to signal after the UI thread / window is initialized.
    rgWaitHandles[0] = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(rgWaitHandles[0], hr, "Failed to create initialization event.");

    // Pass necessary information to create the window.
    context.hInitializedEvent = rgWaitHandles[0];
    context.hInstance = hInstance;
    context.pEngineState = pEngineState;

    // Create our separate UI thread.
    rgWaitHandles[1] = ::CreateThread(NULL, 0, ThreadProc, &context, 0, NULL);
    ExitOnNullWithLastError(rgWaitHandles[1], hr, "Failed to create the UI thread.");

    // Wait for either the thread to be initialized or the window to exit / fail prematurely.
    ::WaitForMultipleObjects(countof(rgWaitHandles), rgWaitHandles, FALSE, INFINITE);

    pEngineState->hMessageWindowThread = rgWaitHandles[1];
    rgWaitHandles[1] = NULL;

LExit:
    ReleaseHandle(rgWaitHandles[1]);
    ReleaseHandle(rgWaitHandles[0]);

    return hr;
}

void UiCloseMessageWindow(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    if (::IsWindow(pEngineState->hMessageWindow))
    {
        ::PostMessageW(pEngineState->hMessageWindow, WM_CLOSE, 0, 0);

        // Give the window 15 seconds to close because if it stays open it can prevent
        // the engine from starting a reboot (should a reboot actually be necessary).
        ::WaitForSingleObject(pEngineState->hMessageWindowThread, 15 * 1000);
    }
}


// internal function definitions

static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    UITHREAD_CONTEXT* pContext = static_cast<UITHREAD_CONTEXT*>(pvContext);
    UITHREAD_INFO info = { };

    WNDCLASSW wc = { };
    BOOL fRegistered = TRUE;
    HWND hWnd = NULL;

    BOOL fRet = FALSE;
    MSG msg = { };

    BURN_ENGINE_STATE* pEngineState = pContext->pEngineState;
    BOOL fElevated = BURN_MODE_ELEVATED == pContext->pEngineState->mode;

    // If elevated, set up the thread local storage to store the correct pipe to communicate logging.
    if (fElevated)
    {
        Assert(TLS_OUT_OF_INDEXES != pEngineState->dwElevatedLoggingTlsId);

        if (!::TlsSetValue(pEngineState->dwElevatedLoggingTlsId, pEngineState->companionConnection.hPipe))
        {
            // If the function failed we cannot write to the pipe so just terminate.
            ExitFunction1(hr = E_INVALIDSTATE);
        }
    }

    wc.lpfnWndProc = WndProc;
    wc.hInstance = pContext->hInstance;
    wc.lpszClassName = BURN_UITHREAD_CLASS_WINDOW;

    if (!::RegisterClassW(&wc))
    {
        ExitWithLastError(hr, "Failed to register window.");
    }

    fRegistered = TRUE;

    info.fElevated = fElevated;
    info.pUserExperience = &pEngineState->userExperience;

    // Create the window to handle reboots without activating it.
    hWnd = ::CreateWindowExW(WS_EX_NOACTIVATE, wc.lpszClassName, NULL, WS_POPUP, CW_USEDEFAULT, SW_HIDE, 0, 0, HWND_DESKTOP, NULL, pContext->hInstance, &info);
    ExitOnNullWithLastError(hWnd, hr, "Failed to create window.");

    // Persist the window handle and let the caller know we've initialized.
    pEngineState->hMessageWindow = hWnd;
    ::SetEvent(pContext->hInitializedEvent);

    // Pump messages until the window is closed.
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected return value from message pump.");
        }
        else if (!::IsDialogMessageW(msg.hwnd, &msg))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessageW(&msg);
        }
    }

LExit:
    if (fRegistered)
    {
        ::UnregisterClassW(BURN_UITHREAD_CLASS_WINDOW, pContext->hInstance);
    }

    return hr;
}

static LRESULT CALLBACK WndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    switch (uMsg)
    {
    case WM_NCCREATE:
        {
        LPCREATESTRUCTW lpcs = reinterpret_cast<LPCREATESTRUCTW>(lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(lpcs->lpCreateParams));
        break;
        }

    case WM_NCDESTROY:
        {
        LRESULT lRes = ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        return lRes;
        }

    case WM_QUERYENDSESSION:
        {
        DWORD dwEndSession = static_cast<DWORD>(lParam);
        BOOL fCritical = ENDSESSION_CRITICAL & dwEndSession;
        BOOL fRet = FALSE;

        // Always block shutdown in the elevated process, but ask the BA in the non-elevated.
        UITHREAD_INFO* pInfo = reinterpret_cast<UITHREAD_INFO*>(::GetWindowLongW(hWnd, GWLP_USERDATA));
        if (!pInfo->fElevated)
        {
            // TODO: instead of recommending canceling all non-critical shutdowns, maybe we should only recommend cancel
            //       when the engine is doing work?
            fRet = IDCANCEL != pInfo->pUserExperience->pUserExperience->OnSystemShutdown(dwEndSession, fCritical ? IDNOACTION : IDCANCEL);
        }

        LogId(REPORT_STANDARD, MSG_SYSTEM_SHUTDOWN, LoggingBoolToString(fCritical), LoggingBoolToString(pInfo->fElevated), LoggingBoolToString(fRet));
        return fRet;
        }

    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;
    }

    return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
}
