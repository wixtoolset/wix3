//-------------------------------------------------------------------------------------------------
// <copyright file="apputil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Application helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

const DWORD PRIVATE_LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
typedef BOOL(WINAPI *LPFN_SETDEFAULTDLLDIRECTORIES)(DWORD);
typedef BOOL(WINAPI *LPFN_SETDLLDIRECTORYW)(LPCWSTR);

extern "C" void DAPI AppFreeCommandLineArgs(
    __in LPWSTR* argv
    )
{
    // The "ignored" hack in AppParseCommandLine requires an adjustment.
    LPWSTR* argvOriginal = argv - 1;
    ::LocalFree(argvOriginal);
}

/********************************************************************
AppInitialize - initializes the standard safety precautions for an
                installation application.

********************************************************************/
extern "C" void DAPI AppInitialize(
    __in_ecount(cSafelyLoadSystemDlls) LPCWSTR rgsczSafelyLoadSystemDlls[],
    __in DWORD cSafelyLoadSystemDlls
    )
{
    HRESULT hr = S_OK;
    HMODULE hIgnored = NULL;
    BOOL fSetDefaultDllDirectories = FALSE;

    ::HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    // Best effort call to initialize default DLL directories to system only.
    HMODULE hKernel32 = ::GetModuleHandleW(L"kernel32");
    LPFN_SETDEFAULTDLLDIRECTORIES pfnSetDefaultDllDirectories = (LPFN_SETDEFAULTDLLDIRECTORIES)::GetProcAddress(hKernel32, "SetDefaultDllDirectories");
    if (pfnSetDefaultDllDirectories)
    {
        if (pfnSetDefaultDllDirectories(PRIVATE_LOAD_LIBRARY_SEARCH_SYSTEM32))
        {
            fSetDefaultDllDirectories = TRUE;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            TraceError(hr, "Failed to call SetDefaultDllDirectories.");
        }
    }

    // Only need to safely load if the default DLL directories was not
    // able to be set.
    if (!fSetDefaultDllDirectories)
    {
        // Remove current working directory from search order.
        LPFN_SETDLLDIRECTORYW pfnSetDllDirectory = (LPFN_SETDLLDIRECTORYW)::GetProcAddress(hKernel32, "SetDllDirectoryW");
        if (!pfnSetDllDirectory || !pfnSetDllDirectory(L""))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            TraceError(hr, "Failed to call SetDllDirectory.");
        }

        for (DWORD i = 0; i < cSafelyLoadSystemDlls; ++i)
        {
            hr = LoadSystemLibrary(rgsczSafelyLoadSystemDlls[i], &hIgnored);
            if (FAILED(hr))
            {
                TraceError(hr, "Failed to safety load: %ls", rgsczSafelyLoadSystemDlls[i]);
            }
        }
    }
}

extern "C" DAPI_(HRESULT) AppParseCommandLine(
    __in LPCWSTR wzCommandLine,
    __in int* pArgc,
    __in LPWSTR** pArgv
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCommandLine = NULL;
    LPWSTR* argv = NULL;
    int argc = 0;

    // CommandLineToArgvW tries to treat the first argument as the path to the process,
    // which fails pretty miserably if your first argument is something like
    // FOO="C:\Program Files\My Company". So give it something harmless to play with.
    hr = StrAllocConcat(&sczCommandLine, L"ignored ", 0);
    ExitOnFailure(hr, "Failed to initialize command line.");

    hr = StrAllocConcat(&sczCommandLine, wzCommandLine, 0);
    ExitOnFailure(hr, "Failed to copy command line.");

    argv = ::CommandLineToArgvW(sczCommandLine, &argc);
    ExitOnNullWithLastError(argv, hr, "Failed to parse command line.");

    // Skip "ignored" argument/hack.
    *pArgv = argv + 1;
    *pArgc = argc - 1;

LExit:
    ReleaseStr(sczCommandLine);

    return hr;
}
