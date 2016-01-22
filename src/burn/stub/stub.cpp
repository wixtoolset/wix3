//-------------------------------------------------------------------------------------------------
// <copyright file="stub.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Setup chainer/bootstrapper executable for WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


int WINAPI wWinMain(
    __in HINSTANCE hInstance,
    __in_opt HINSTANCE /* hPrevInstance */,
    __in_z_opt LPWSTR lpCmdLine,
    __in int nCmdShow
    )
{
    HRESULT hr = S_OK;
    DWORD dwExitCode = 0;

    LPCWSTR rgsczSafelyLoadSystemDlls[] =
    {
        L"cabinet.dll", // required by Burn.
        L"msi.dll", // required by Burn.
        L"version.dll", // required by Burn.
        L"wininet.dll", // required by Burn.

        L"comres.dll", // required by CLSIDFromProgID() when loading clbcatq.dll.
        L"clbcatq.dll", // required by CLSIDFromProgID() when loading msxml?.dll.

        L"msasn1.dll", // required by DecryptFile() when loading crypt32.dll.
        L"crypt32.dll", // required by DecryptFile() when loading feclient.dll.
        L"feclient.dll", // unsafely loaded by DecryptFile().
    };

    AppInitialize(rgsczSafelyLoadSystemDlls, countof(rgsczSafelyLoadSystemDlls));

    // call run
    hr = EngineRun(hInstance, lpCmdLine, nCmdShow, &dwExitCode);
    ExitOnFailure(hr, "Failed to run application.");

LExit:
    return FAILED(hr) ? (int)hr : (int)dwExitCode;
}
