// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
    LPWSTR sczPath = NULL;
    HANDLE hEngineFile = INVALID_HANDLE_VALUE;

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

    // Best effort attempt to get our file handle as soon as possible.
    // Eventually we'll pass file handles to child processes as a fallback.
    hr = PathForCurrentProcess(&sczPath, NULL);
    if (SUCCEEDED(hr))
    {
        hEngineFile = ::CreateFileW(sczPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    }

    // If the engine is in the clean room, we'll do the unsafe initialization
    // because some systems in Windows (namely GDI+) will fail when run in
    // a process that protects against DLL hijacking. Since we know the clean
    // room is in a clean folder and not subject to DLL hijacking we won't
    // make ourselves perfectly secure so that we can load BAs that still
    // depend on those parts of Windows that are insecure to DLL hijacking.
    if (EngineInCleanRoom(lpCmdLine))
    {
        AppInitializeUnsafe();
    }
    else
    {
        AppInitialize(rgsczSafelyLoadSystemDlls, countof(rgsczSafelyLoadSystemDlls));
    }

    // call run
    hr = EngineRun(hInstance, hEngineFile, lpCmdLine, nCmdShow, &dwExitCode);
    ExitOnFailure(hr, "Failed to run application.");

LExit:
    ReleaseFileHandle(hEngineFile);
    ReleaseStr(sczPath);

    return FAILED(hr) ? (int)hr : (int)dwExitCode;
}
