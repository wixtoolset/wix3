//-------------------------------------------------------------------------------------------------
// <copyright file="splashscreen.h" company="Outercurve Foundation">
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

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif


// constants


// structs


// functions

void SplashScreenCreate(
    __in HINSTANCE hInstance,
    __in_z_opt LPCWSTR wzCaption,
    __out HWND* pHwnd
    );
HRESULT SplashScreenDisplayError(
    __in BOOTSTRAPPER_DISPLAY display,
    __in_z LPCWSTR wzBundleName,
    __in HRESULT hrError
    );

#if defined(__cplusplus)
}
#endif
