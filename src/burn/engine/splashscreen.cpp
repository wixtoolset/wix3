// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace Gdiplus;

#define BURN_SPLASHSCREEN_CLASS_WINDOW L"WixBurnSplashScreen"
#define IDB_SPLASHSCREEN 1

// struct

struct SPLASHSCREEN_INFO
{
    Bitmap* pBitmap;
    Point pt;
    Size size;
};

struct SPLASHSCREEN_CONTEXT
{
    HANDLE hIntializedEvent;
    HINSTANCE hInstance;
    LPCWSTR wzCaption;

    HWND* pHwnd;
};

// internal function definitions

static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    );
static LRESULT CALLBACK WndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static void OnPaint(
    __in HDC hdc,
    __in SPLASHSCREEN_INFO* pSplashScreen
    );
static HRESULT LoadSplashScreen(
    __in HMODULE hInstance,
    __in SPLASHSCREEN_INFO* pSplashScreen
    );


// function definitions

extern "C" void SplashScreenCreate(
    __in HINSTANCE hInstance,
    __in_z_opt LPCWSTR wzCaption,
    __out HWND* pHwnd
    )
{
    HRESULT hr = S_OK;
    SPLASHSCREEN_CONTEXT context = { };
    HANDLE rgSplashScreenEvents[2] = { };
    DWORD dwSplashScreenThreadId = 0;

    rgSplashScreenEvents[0] = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(rgSplashScreenEvents[0], hr, "Failed to create modal event.");

    // create splash screen thread.
    context.hIntializedEvent = rgSplashScreenEvents[0];
    context.hInstance = hInstance;
    context.wzCaption = wzCaption;
    context.pHwnd = pHwnd;

    rgSplashScreenEvents[1] = ::CreateThread(NULL, 0, ThreadProc, &context, 0, &dwSplashScreenThreadId);
    ExitOnNullWithLastError(rgSplashScreenEvents[1], hr, "Failed to create UI thread.");

    // It doesn't really matter if the thread gets initialized (WAIT_OBJECT_0) or fails and exits
    // prematurely (WAIT_OBJECT_0 + 1), we just want to wait long enough for one of those two
    // events to happen.
    ::WaitForMultipleObjects(countof(rgSplashScreenEvents), rgSplashScreenEvents, FALSE, INFINITE);

LExit:
    ReleaseHandle(rgSplashScreenEvents[1]);
    ReleaseHandle(rgSplashScreenEvents[0]);
}

extern "C" HRESULT SplashScreenDisplayError(
    __in BOOTSTRAPPER_DISPLAY display,
    __in_z LPCWSTR wzBundleName,
    __in HRESULT hrError
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDisplayString = NULL;

    hr = StrAllocFromError(&sczDisplayString, hrError, NULL);
    ExitOnFailure(hr, "Failed to allocate string to display error message");

    Trace1(REPORT_STANDARD, "Error message displayed because: %ls", sczDisplayString);

    if (BOOTSTRAPPER_DISPLAY_NONE == display || BOOTSTRAPPER_DISPLAY_PASSIVE == display || BOOTSTRAPPER_DISPLAY_EMBEDDED == display)
    {
        // Don't display the error dialog in these modes
        ExitFunction1(hr = S_OK);
    }

    ::MessageBoxW(NULL, sczDisplayString, wzBundleName, MB_OK | MB_ICONERROR | MB_SYSTEMMODAL);

LExit:
    ReleaseStr(sczDisplayString);

    return hr;
}


static DWORD WINAPI ThreadProc(
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;

    ULONG_PTR token = 0;
    GdiplusStartupInput input;
    GdiplusStartupOutput output = { };

    SPLASHSCREEN_CONTEXT* pContext = static_cast<SPLASHSCREEN_CONTEXT*>(pvContext);
    SPLASHSCREEN_INFO splashScreen = { };

    WNDCLASSW wc = { };
    BOOL fRegistered = TRUE;
    HWND hWnd = NULL;

    BOOL fRet = FALSE;
    MSG msg = { };

    input.GdiplusVersion = 1;

    hr = GdipInitialize(&input, &token, &output);
    ExitOnFailure(hr, "Failed to initialize GDI+.");

    hr = LoadSplashScreen(pContext->hInstance, &splashScreen);
    ExitOnFailure(hr, "Failed to load splash screen.");

    // Register the window class and create the window.
    wc.lpfnWndProc = WndProc;
    wc.hInstance = pContext->hInstance;
    wc.hCursor = ::LoadCursorW(NULL, (LPCWSTR)IDC_ARROW);
    wc.lpszClassName = BURN_SPLASHSCREEN_CLASS_WINDOW;
    if (!::RegisterClassW(&wc))
    {
        ExitWithLastError(hr, "Failed to register window.");
    }

    fRegistered = TRUE;

    hWnd = ::CreateWindowExW(WS_EX_TOOLWINDOW, wc.lpszClassName, pContext->wzCaption, WS_POPUP | WS_VISIBLE, splashScreen.pt.X, splashScreen.pt.Y, splashScreen.size.Width, splashScreen.size.Height, HWND_DESKTOP, NULL, pContext->hInstance, &splashScreen);
    ExitOnNullWithLastError(hWnd, hr, "Failed to create window.");

    // Return the splash screen window and free the main thread waiting for us to be initialized.
    *pContext->pHwnd = hWnd;
    ::SetEvent(pContext->hIntializedEvent);

    // Pump messages until the bootstrapper application destroys the window.
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected return value from message pump.");
        }
        else if (!::IsDialogMessageW(hWnd, &msg))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessageW(&msg);
        }
    }

LExit:
    if (fRegistered)
    {
        ::UnregisterClassW(BURN_SPLASHSCREEN_CLASS_WINDOW, pContext->hInstance);
    }

    if (splashScreen.pBitmap)
    {
        delete splashScreen.pBitmap;
    }
    
    if (token)
    {
        GdipUninitialize(token);
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
    LRESULT lres = 0;
    SPLASHSCREEN_INFO* pSplashScreen = reinterpret_cast<SPLASHSCREEN_INFO*>(::GetWindowLongW(hWnd, GWLP_USERDATA));

    switch (uMsg)
    {
    case WM_NCCREATE:
        {
        LPCREATESTRUCTW lpcs = reinterpret_cast<LPCREATESTRUCTW>(lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(lpcs->lpCreateParams));
        }
        break;

    case WM_NCDESTROY:
        lres = ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        return lres;

    case WM_NCHITTEST:
        return HTCAPTION; // allow window to be moved by grabbing any pixel.

    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;

    case WM_ERASEBKGND:
        // The splash screen image will be repainted in its entirety.
        return 1;

    case WM_PAINT:
        {
        PAINTSTRUCT ps = { };

        HDC hdc = BeginPaint(hWnd, &ps);
        OnPaint(hdc, pSplashScreen);
        EndPaint(hWnd, &ps);
        }
        return 0;
    }

    return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
}
 
static void OnPaint(
    __in HDC hdc,
    __in SPLASHSCREEN_INFO* pSplashScreen
    )
{
    // Use high-quality bicubuc stretching from GDI+ which looks better than GDI.
    Graphics graphics(hdc);
    graphics.SetInterpolationMode(InterpolationModeHighQualityBicubic);

    Rect dst(0, 0, pSplashScreen->size.Width, pSplashScreen->size.Height);
    Status status = graphics.DrawImage(pSplashScreen->pBitmap, dst);

#if DEBUG
    HRESULT hr = GdipHresultFromStatus(status);
    TraceError(hr, "Failed to draw splash screen bitmap.");
#else
    UNREFERENCED_PARAMETER(status);
#endif
}

static HRESULT LoadSplashScreen(
    __in HMODULE hInstance,
    __in SPLASHSCREEN_INFO* pSplashScreen
    )
{
    HRESULT hr = S_OK;
    POINT ptCursor = { };
    HMONITOR hMonitor = NULL;
    MONITORINFOEXW mi;
    HDC hdc = NULL;
    UINT dpiX = 0;
    UINT dpiY = 0;

    pSplashScreen->pBitmap = Bitmap::FromResource(hInstance, MAKEINTRESOURCEW(IDB_SPLASHSCREEN));
    ExitOnNull(pSplashScreen->pBitmap, hr, E_INVALIDDATA, "Failed to find the splash screen bitmap.");
    ExitOnGdipFailure(pSplashScreen->pBitmap->GetLastStatus(), hr, "Failed to load the splash screen bitmap.");

    pSplashScreen->pt.X = CW_USEDEFAULT;
    pSplashScreen->pt.Y = CW_USEDEFAULT;
    pSplashScreen->size.Width = pSplashScreen->pBitmap->GetWidth();
    pSplashScreen->size.Height = pSplashScreen->pBitmap->GetHeight();

    // Stretch and center the window on the monitor with the mouse.
    if (::GetCursorPos(&ptCursor))
    {
        hMonitor = ::MonitorFromPoint(ptCursor, MONITOR_DEFAULTTONEAREST);
        if (hMonitor)
        {
            ZeroMemory(&mi, sizeof(mi));
            mi.cbSize = sizeof(mi);

            if (::GetMonitorInfoW(hMonitor, &mi))
            {
                hdc = ::CreateDCW(L"DISPLAY", mi.szDevice, NULL, NULL);
                if (hdc)
                {
                    dpiX = ::GetDeviceCaps(hdc, LOGPIXELSX);
                    dpiY = ::GetDeviceCaps(hdc, LOGPIXELSY);

                    pSplashScreen->size.Width = pSplashScreen->size.Width * dpiX / 96;
                    pSplashScreen->size.Height = pSplashScreen->size.Height * dpiY / 96;

                    ::ReleaseDC(NULL, hdc);
                }

                pSplashScreen->pt.X = mi.rcWork.left + (mi.rcWork.right  - mi.rcWork.left - pSplashScreen->size.Width) / 2;
                pSplashScreen->pt.Y = mi.rcWork.top  + (mi.rcWork.bottom - mi.rcWork.top  - pSplashScreen->size.Height) / 2;
            }
        }
    }

LExit:
    return hr;
}
