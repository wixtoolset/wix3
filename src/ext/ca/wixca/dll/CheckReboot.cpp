//-------------------------------------------------------------------------------------------------
// <copyright file="CheckReboot.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Code to schedule reboot for deferred custom actions whcih cannot.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


/********************************************************************
WixCheckRebootRequired - entry point for WixCheckRebootRequired Custom Action

 called as Type 1 CustomAction (binary DLL) from Windows Installer 
 in InstallExecuteSequence after InstallFinalize
********************************************************************/
extern "C" UINT __stdcall WixCheckRebootRequired(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixCheckRebootRequired");
    ExitOnFailure(hr, "failed to initialize");

    if (WcaDidDeferredActionRequireReboot())
    {
        WcaLog(LOGMSG_STANDARD, "Reboot required by deferred CustomAction.");

        er = ::MsiSetMode(hInstall, MSIRUNMODE_REBOOTATEND, TRUE);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "Failed to schedule reboot.");
    }

LExit:

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
