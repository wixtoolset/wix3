//-------------------------------------------------------------------------------------------------
// <copyright file="test.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Code useful in testing custom actions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define WIXCA_UITHREAD_CLASS_WINDOW L"WixCaMessageWindow"

extern HMODULE g_hInstCADLL;


// structs

struct UITHREAD_CONTEXT
{
    HANDLE hInitializedEvent;
    HINSTANCE hInstance;
    HWND hWnd;
};


// internal function declarations

static HRESULT CreateMessageWindow(
    __out HWND* phWnd
    );

static void CloseMessageWindow(
    __in HWND hWnd
    );

static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    );

static LRESULT CALLBACK WndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );


/******************************************************************
WixFailWhenDeferred - entry point for WixFailWhenDeferred
    custom action which always fails when running as a deferred
    custom action (otherwise it blindly succeeds). It's useful when
    testing the rollback of deferred custom actions: Schedule it
    immediately after the rollback/deferred CA pair you're testing
    and it will fail, causing your rollback CA to get invoked.
********************************************************************/
extern "C" UINT __stdcall WixFailWhenDeferred(
    __in MSIHANDLE hInstall
    )
{
    return ::MsiGetMode(hInstall, MSIRUNMODE_SCHEDULED) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS;
}

/******************************************************************
WixWaitForEvent - entry point for WixWaitForEvent custom action
    which waits for either the WixWaitForEventFail or
    WixWaitForEventSucceed named auto reset events. Signaling the
    WixWaitForEventFail event will return ERROR_INSTALL_FAILURE or
    signaling the WixWaitForEventSucceed event will return
    ERROR_SUCCESS. Both events are declared in the Global\ namespace.
********************************************************************/
extern "C" UINT __stdcall WixWaitForEvent(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    HWND hMessageWindow = NULL;
    LPCWSTR wzSDDL = L"D:(A;;GA;;;WD)";
    OS_VERSION version = OS_VERSION_UNKNOWN;
    DWORD dwServicePack = 0;
    PSECURITY_DESCRIPTOR pSD = NULL;
    SECURITY_ATTRIBUTES sa = { };
    HANDLE rghEvents[2];

    hr = WcaInitialize(hInstall, "WixWaitForEvent");
    ExitOnFailure(hr, "Failed to initialize.");

    // Create a window to prevent shutdown requests.
    hr = CreateMessageWindow(&hMessageWindow);
    ExitOnFailure(hr, "Failed to create message window.");

    // If running on Vista/2008 or newer use integrity enhancements.
    OsGetVersion(&version, &dwServicePack);
    if (OS_VERSION_VISTA <= version)
    {
        // Add SACL to allow Everyone to signal from a medium integrity level.
        wzSDDL = L"D:(A;;GA;;;WD)S:(ML;;NW;;;ME)";
    }

    // Create the security descriptor and attributes for the events.
    if (!::ConvertStringSecurityDescriptorToSecurityDescriptorW(wzSDDL, SDDL_REVISION_1, &pSD, NULL))
    {
        ExitWithLastError(hr, "Failed to create the security descriptor for the events.");
    }

    sa.nLength = sizeof(sa);
    sa.lpSecurityDescriptor = pSD;
    sa.bInheritHandle = FALSE;

    rghEvents[0] = ::CreateEventW(&sa, FALSE, FALSE, L"Global\\WixWaitForEventFail");
    ExitOnNullWithLastError(rghEvents[0], hr, "Failed to create the Global\\WixWaitForEventFail event.");

    rghEvents[1] = ::CreateEventW(&sa, FALSE, FALSE, L"Global\\WixWaitForEventSucceed");
    ExitOnNullWithLastError(rghEvents[1], hr, "Failed to create the Global\\WixWaitForEventSucceed event.");

    // Wait for either of the events to be signaled and handle accordingly.
    er = ::WaitForMultipleObjects(countof(rghEvents), rghEvents, FALSE, INFINITE);
    switch (er)
    {
    case WAIT_OBJECT_0 + 0:
        er = ERROR_INSTALL_FAILURE;
        break;
    case WAIT_OBJECT_0 + 1:
        er = ERROR_SUCCESS;
        break;
    default:
        ExitOnWin32Error(er, hr, "Unexpected failure.");
    }

LExit:
    ReleaseHandle(rghEvents[1]);
    ReleaseHandle(rghEvents[0]);

    if (pSD)
    {
        ::LocalFree(pSD);
    }

    if (hMessageWindow)
    {
        CloseMessageWindow(hMessageWindow);
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


// internal function definitions

static HRESULT CreateMessageWindow(
    __out HWND* phWnd
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
    context.hInstance = (HINSTANCE)g_hInstCADLL;

    // Create our separate UI thread.
    rgWaitHandles[1] = ::CreateThread(NULL, 0, ThreadProc, &context, 0, NULL);
    ExitOnNullWithLastError(rgWaitHandles[1], hr, "Failed to create the UI thread.");

    // Wait for either the thread to be initialized or the window to exit / fail prematurely.
    ::WaitForMultipleObjects(countof(rgWaitHandles), rgWaitHandles, FALSE, INFINITE);

    // Pass the window back to the caller.
    *phWnd = context.hWnd;

LExit:
    ReleaseHandle(rgWaitHandles[1]);
    ReleaseHandle(rgWaitHandles[0]);

    return hr;
}

static void CloseMessageWindow(
    __in HWND hWnd
    )
{
    if (::IsWindow(hWnd))
    {
        ::PostMessageW(hWnd, WM_CLOSE, 0, 0);
    }
}

static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    UITHREAD_CONTEXT* pContext = static_cast<UITHREAD_CONTEXT*>(pvContext);

    WNDCLASSW wc = { };
    BOOL fRegistered = TRUE;
    HWND hWnd = NULL;

    BOOL fRet = FALSE;
    MSG msg = { };

    wc.lpfnWndProc = WndProc;
    wc.hInstance = pContext->hInstance;
    wc.lpszClassName = WIXCA_UITHREAD_CLASS_WINDOW;

    if (!::RegisterClassW(&wc))
    {
        ExitWithLastError(hr, "Failed to register window.");
    }

    fRegistered = TRUE;

    // Create the window to handle reboots without activating it.
    hWnd = ::CreateWindowExW(WS_EX_TOOLWINDOW, wc.lpszClassName, NULL, WS_POPUP | WS_VISIBLE, CW_USEDEFAULT, SW_SHOWNA, 0, 0, HWND_DESKTOP, NULL, pContext->hInstance, NULL);
    ExitOnNullWithLastError(hWnd, hr, "Failed to create window.");

    // Persist the window handle and let the caller know we've initialized.
    pContext->hWnd = hWnd;
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
        ::UnregisterClassW(WIXCA_UITHREAD_CLASS_WINDOW, pContext->hInstance);
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
    case WM_QUERYENDSESSION:
        // Prevent the process from being shut down.
        WcaLog(LOGMSG_VERBOSE, "Disallowed system request to shut down the custom action server.");
        return FALSE;

    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;
    }

    return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
}
