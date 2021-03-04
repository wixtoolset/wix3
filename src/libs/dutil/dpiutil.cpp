// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Exit macros
#define DpiuExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_DPIUTIL, x, s, __VA_ARGS__)
#define DpiuExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_DPIUTIL, x, s, __VA_ARGS__)
#define DpiuExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_DPIUTIL, x, s, __VA_ARGS__)
#define DpiuExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_DPIUTIL, x, s, __VA_ARGS__)
#define DpiuExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_DPIUTIL, x, s, __VA_ARGS__)
#define DpiuExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_DPIUTIL, x, s, __VA_ARGS__)
#define DpiuExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_DPIUTIL, p, x, e, s, __VA_ARGS__)
#define DpiuExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_DPIUTIL, p, x, s, __VA_ARGS__)
#define DpiuExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_DPIUTIL, p, x, e, s, __VA_ARGS__)
#define DpiuExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_DPIUTIL, p, x, s, __VA_ARGS__)
#define DpiuExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_DPIUTIL, e, x, s, __VA_ARGS__)

static PFN_GETDPIFORMONITOR vpfnGetDpiForMonitor = NULL;
static PFN_GETDPIFORWINDOW vpfnGetDpiForWindow = NULL;
static PFN_SETPROCESSDPIAWARE vpfnSetProcessDPIAware = NULL;
static PFN_SETPROCESSDPIAWARENESS vpfnSetProcessDpiAwareness = NULL;
static PFN_SETPROCESSDPIAWARENESSCONTEXT vpfnSetProcessDpiAwarenessContext = NULL;

static HMODULE vhShcoreDll = NULL;
static HMODULE vhUser32Dll = NULL;
static BOOL vfDpiuInitialized = FALSE;

DAPI_(void) DpiuInitialize()
{
    HRESULT hr = S_OK;

    hr = LoadSystemLibrary(L"Shcore.dll", &vhShcoreDll);
    if (SUCCEEDED(hr))
    {
        // Ignore failures.
        vpfnGetDpiForMonitor = reinterpret_cast<PFN_GETDPIFORMONITOR>(::GetProcAddress(vhShcoreDll, "GetDpiForMonitor"));
        vpfnSetProcessDpiAwareness = reinterpret_cast<PFN_SETPROCESSDPIAWARENESS>(::GetProcAddress(vhShcoreDll, "SetProcessDpiAwareness"));
    }

    hr = LoadSystemLibrary(L"User32.dll", &vhUser32Dll);
    if (SUCCEEDED(hr))
    {
        // Ignore failures.
        vpfnGetDpiForWindow = reinterpret_cast<PFN_GETDPIFORWINDOW>(::GetProcAddress(vhUser32Dll, "GetDpiForWindow"));
        vpfnSetProcessDPIAware = reinterpret_cast<PFN_SETPROCESSDPIAWARE>(::GetProcAddress(vhUser32Dll, "SetProcessDPIAware"));
        vpfnSetProcessDpiAwarenessContext = reinterpret_cast<PFN_SETPROCESSDPIAWARENESSCONTEXT>(::GetProcAddress(vhUser32Dll, "SetProcessDpiAwarenessContext"));
    }

    vfDpiuInitialized = TRUE;
}

DAPI_(void) DpiuUninitialize()
{
    if (vhShcoreDll)
    {
        ::FreeLibrary(vhShcoreDll);
    }

    if (vhUser32Dll)
    {
        ::FreeLibrary(vhUser32Dll);
    }

    vhShcoreDll = NULL;
    vhUser32Dll = NULL;
    vpfnGetDpiForMonitor = NULL;
    vpfnGetDpiForWindow = NULL;
    vfDpiuInitialized = FALSE;
}

DAPI_(void) DpiuGetWindowContext(
    __in HWND hWnd,
    __in DPIU_WINDOW_CONTEXT* pWindowContext
    )
{
    HRESULT hr = S_OK;
    HMONITOR hMonitor = NULL;
    UINT dpiX = 0;
    UINT dpiY = 0;
    HDC hdc = NULL;

    pWindowContext->nDpi = USER_DEFAULT_SCREEN_DPI;

    if (vpfnGetDpiForWindow)
    {
        pWindowContext->nDpi = vpfnGetDpiForWindow(hWnd);
        ExitFunction();
    }

    if (vpfnGetDpiForMonitor)
    {
        hMonitor = ::MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
        if (hMonitor)
        {
            hr = vpfnGetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, &dpiX, &dpiY);
            if (SUCCEEDED(hr))
            {
                pWindowContext->nDpi = dpiX;
                ExitFunction();
            }
        }
    }

    hdc = ::GetDC(hWnd);
    if (hdc)
    {
        pWindowContext->nDpi = ::GetDeviceCaps(hdc, LOGPIXELSX);
    }

LExit:
    if (hdc)
    {
        ::ReleaseDC(hWnd, hdc);
    }
}

DAPI_(int) DpiuScaleValue(
    __in int nDefaultDpiValue,
    __in UINT nTargetDpi
    )
{
    return ::MulDiv(nDefaultDpiValue, nTargetDpi, USER_DEFAULT_SCREEN_DPI);
}

DAPI_(HRESULT) DpiuSetProcessDpiAwareness(
    __in DPIU_AWARENESS supportedAwareness,
    __in_opt DPIU_AWARENESS* pSelectedAwareness
    )
{
    HRESULT hr = S_OK;
    DPIU_AWARENESS selectedAwareness = DPIU_AWARENESS_NONE;
    DPI_AWARENESS_CONTEXT awarenessContext = DPI_AWARENESS_CONTEXT_UNAWARE;
    PROCESS_DPI_AWARENESS awareness = PROCESS_DPI_UNAWARE;

    if (vpfnSetProcessDpiAwarenessContext)
    {
        if (DPIU_AWARENESS_PERMONITORV2 & supportedAwareness)
        {
            awarenessContext = DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2;
            selectedAwareness = DPIU_AWARENESS_PERMONITORV2;
        }
        else if (DPIU_AWARENESS_PERMONITOR & supportedAwareness)
        {
            awarenessContext = DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE;
            selectedAwareness = DPIU_AWARENESS_PERMONITOR;
        }
        else if (DPIU_AWARENESS_SYSTEM & supportedAwareness)
        {
            awarenessContext = DPI_AWARENESS_CONTEXT_SYSTEM_AWARE;
            selectedAwareness = DPIU_AWARENESS_SYSTEM;
        }
        else if (DPIU_AWARENESS_GDISCALED & supportedAwareness)
        {
            awarenessContext = DPI_AWARENESS_CONTEXT_UNAWARE_GDISCALED;
            selectedAwareness = DPIU_AWARENESS_GDISCALED;
        }

        if (!vpfnSetProcessDpiAwarenessContext(awarenessContext))
        {
            ExitOnLastError(hr, "Failed to set process DPI awareness context.");
        }
    }
    else if (vpfnSetProcessDpiAwareness)
    {
        if (DPIU_AWARENESS_PERMONITOR & supportedAwareness)
        {
            awareness = PROCESS_PER_MONITOR_DPI_AWARE;
            selectedAwareness = DPIU_AWARENESS_PERMONITOR;
        }
        else if (DPIU_AWARENESS_SYSTEM & supportedAwareness)
        {
            awareness = PROCESS_SYSTEM_DPI_AWARE;
            selectedAwareness = DPIU_AWARENESS_SYSTEM;
        }

        hr = vpfnSetProcessDpiAwareness(awareness);
        ExitOnFailure(hr, "Failed to set process DPI awareness.");
    }
    else if (vpfnSetProcessDPIAware && (DPIU_AWARENESS_SYSTEM & supportedAwareness))
    {
        selectedAwareness = DPIU_AWARENESS_SYSTEM;
        if (!vpfnSetProcessDPIAware())
        {
            ExitOnLastError(hr, "Failed to set process DPI aware.");
        }
    }

LExit:
    if (pSelectedAwareness)
    {
        *pSelectedAwareness = selectedAwareness;
    }

    return hr;
}
