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

#ifdef __cplusplus
}
#endif
