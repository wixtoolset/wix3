#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

// from WinUser.h
#ifndef WM_DPICHANGED
#define WM_DPICHANGED       0x02E0
#endif
#ifndef USER_DEFAULT_SCREEN_DPI
#define USER_DEFAULT_SCREEN_DPI 96
#endif

typedef enum DPIU_AWARENESS
{
    DPIU_AWARENESS_NONE = 0x0,
    DPIU_AWARENESS_SYSTEM = 0x1,
    DPIU_AWARENESS_PERMONITOR = 0x2,
    DPIU_AWARENESS_PERMONITORV2 = 0x4,
    DPIU_AWARENESS_GDISCALED = 0x8,
} DPIU_PROCESS_AWARENESS;

#ifndef DPI_ENUMS_DECLARED

typedef enum PROCESS_DPI_AWARENESS {
    PROCESS_DPI_UNAWARE = 0,
    PROCESS_SYSTEM_DPI_AWARE = 1,
    PROCESS_PER_MONITOR_DPI_AWARE = 2
} PROCESS_DPI_AWARENESS;

typedef enum MONITOR_DPI_TYPE {
    MDT_EFFECTIVE_DPI = 0,
    MDT_ANGULAR_DPI = 1,
    MDT_RAW_DPI = 2,
    MDT_DEFAULT = MDT_EFFECTIVE_DPI
} MONITOR_DPI_TYPE;

typedef HANDLE DPI_AWARENESS_CONTEXT;

#define DPI_AWARENESS_CONTEXT_UNAWARE               ((DPI_AWARENESS_CONTEXT)-1)
#define DPI_AWARENESS_CONTEXT_SYSTEM_AWARE          ((DPI_AWARENESS_CONTEXT)-2)
#define DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE     ((DPI_AWARENESS_CONTEXT)-3)
#define DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2  ((DPI_AWARENESS_CONTEXT)-4)
#define DPI_AWARENESS_CONTEXT_UNAWARE_GDISCALED     ((DPI_AWARENESS_CONTEXT)-5)

#define DPI_ENUMS_DECLARED
#endif // (DPI_ENUMS_DECLARED)

typedef struct _DPIU_WINDOW_CONTEXT
{
    UINT nDpi;
} DPIU_WINDOW_CONTEXT;

typedef HRESULT (APIENTRY *PFN_GETDPIFORMONITOR)(
    __in HMONITOR hmonitor,
    __in MONITOR_DPI_TYPE dpiType,
    __in UINT* dpiX,
    __in UINT* dpiY
    );
typedef UINT (APIENTRY *PFN_GETDPIFORWINDOW)(
    __in HWND hwnd
    );
typedef BOOL (APIENTRY* PFN_SETPROCESSDPIAWARE)();
typedef HRESULT (APIENTRY* PFN_SETPROCESSDPIAWARENESS)(
    __in PROCESS_DPI_AWARENESS value
    );
typedef BOOL (APIENTRY* PFN_SETPROCESSDPIAWARENESSCONTEXT)(
    __in DPI_AWARENESS_CONTEXT value
    );

void DAPI DpiuInitialize();
void DAPI DpiuUninitialize();

/********************************************************************
 DpiuGetWindowContext - get the DPI context of the given window.

*******************************************************************/
void DAPI DpiuGetWindowContext(
    __in HWND hWnd,
    __in DPIU_WINDOW_CONTEXT* pWindowContext
    );

/********************************************************************
 DpiuScaleValue - scale the value to the target DPI.

*******************************************************************/
int DAPI DpiuScaleValue(
    __in int nDefaultDpiValue,
    __in UINT nTargetDpi
    );

/********************************************************************
 DpiuUnscaleValue - scale the back to the default DPI.

*******************************************************************/
DAPI_(int) DpiuUnscaleValue(
    __in int nScaledValue,
    __in UINT nSourceDpi
    );

/********************************************************************
 DpiuSetProcessDpiAwareness - set the process DPI awareness. The ranking is
     PERMONITORV2 > PERMONITOR > SYSTEM > GDISCALED > NONE.

*******************************************************************/
HRESULT DAPI DpiuSetProcessDpiAwareness(
    __in DPIU_AWARENESS supportedAwareness,
    __in_opt DPIU_AWARENESS* pSelectedAwareness
    );

#ifdef __cplusplus
}
#endif
