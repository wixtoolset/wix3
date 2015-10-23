//-------------------------------------------------------------------------------------------------
// <copyright file="BroadcastSettingChange.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


/********************************************************************
WixBroadcastSettingChange

  Send WM_SETTINGCHANGE message to all top-level windows indicating
  that unspecified settings have changed.
********************************************************************/
extern "C" UINT __stdcall WixBroadcastSettingChange(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = WcaInitialize(hInstall, "WixBroadcastSettingChange");
    ExitOnFailure(hr, "failed to initialize WixBroadcastSettingChange");

    // best effort; ignore failures
    ::SendMessageTimeoutW(HWND_BROADCAST, WM_SETTINGCHANGE, NULL, NULL, SMTO_ABORTIFHUNG, 1000, NULL);

LExit:
    return WcaFinalize(ERROR_SUCCESS);
}


/********************************************************************
WixBroadcastEnvironmentChange

  Send WM_SETTINGCHANGE message to all top-level windows indicating
  that environment variables have changed.
********************************************************************/
extern "C" UINT __stdcall WixBroadcastEnvironmentChange(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = WcaInitialize(hInstall, "WixBroadcastEnvironmentChange");
    ExitOnFailure(hr, "failed to initialize WixBroadcastEnvironmentChange");

    // best effort; ignore failures
    ::SendMessageTimeoutW(HWND_BROADCAST, WM_SETTINGCHANGE, NULL, reinterpret_cast<LPARAM>(L"Environment"), SMTO_ABORTIFHUNG, 1000, NULL);

LExit:
    return WcaFinalize(ERROR_SUCCESS);
}
