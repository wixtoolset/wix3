//-------------------------------------------------------------------------------------------------
// <copyright file="updateexe.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Update executable for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include <initguid.h>


HRESULT GetUpdateMutex(
    __in const GUID *pGuid,
    __out HANDLE *phMutex
    )
{
    HRESULT hr = S_OK;
    HANDLE h = INVALID_HANDLE_VALUE;
    WCHAR wzMutexName[MAX_PATH];

    hr = ::StringCchPrintfW(
        wzMutexName, 
        countof(wzMutexName), 
        L"Local\\CT_UPDATE_%08lx-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x",
        pGuid->Data1, 
        pGuid->Data2, 
        pGuid->Data3, 
        pGuid->Data4[0], 
        pGuid->Data4[1], 
        pGuid->Data4[2], 
        pGuid->Data4[3], 
        pGuid->Data4[4], 
        pGuid->Data4[5], 
        pGuid->Data4[6], 
        pGuid->Data4[7]
    );
    ExitOnFailure(hr, "Failed to StringCchPrintfW the Update mutex's name.");

    h = ::CreateMutexW(NULL, FALSE, wzMutexName);
    ExitOnNullWithLastError1(h, hr, "Failed to open mutex %S", wzMutexName);

    if (WAIT_OBJECT_0 == ::WaitForSingleObject(h, 0))
    {
        *phMutex = h;
    }
    else
    {
        ::CloseHandle(h);
        *phMutex = INVALID_HANDLE_VALUE;
    }

LExit:
    return hr;
}


HRESULT LaunchTarget(
    __in LPWSTR wzCommandline,
    __in LPWSTR wzCurrentDirectory,
    __out HANDLE * phProcess
    )
{
    HRESULT hr = S_OK;
    STARTUPINFOW startupInfo = {0};
    PROCESS_INFORMATION procInfo = {0};

    if (!::CreateProcessW(NULL, wzCommandline, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL, wzCurrentDirectory, &startupInfo, &procInfo))
    {
        ExitWithLastError1(hr, "Failed to execute %S.", wzCommandline);
    }

LExit:
    if (procInfo.hThread)
    {
        ::CloseHandle(procInfo.hThread);
    }

    if (procInfo.hProcess)
    {
        *phProcess = procInfo.hProcess;
    }

    return hr;
}


int __cdecl wmain(
    __in int argc,
    __in WCHAR * argv[]
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzCommandLine = NULL;

    LPWSTR wzAppId = NULL;
    GUID guidApp;

    DWORD64 dw64Version;
    LPWSTR pwzFeedUri = NULL;
    LPWSTR pwzApplicationPath = NULL;
    LPWSTR pwzApplicationDirectory = NULL;

    DWORD64 dw64NextUpdateTime = 0;
    BOOL fUpdateReady = FALSE;
    DWORD64 dw64UpdateVersion = 0;
    LPWSTR pwzFeedPath = NULL;
    LPWSTR pwzSetupPath = NULL;

    DWORD dwTimeToLive = 0;
    LPWSTR pwzApplicationId = NULL;
    LPWSTR pwzApplicationSource = NULL;

    BOOL bDeleteUpdateInfoPath = FALSE;
    BOOL bDeleteUpdateBinaryPath = FALSE;
    HANDLE hProcess = INVALID_HANDLE_VALUE;
    HANDLE hUpdateMutex = INVALID_HANDLE_VALUE;

    //
    // Process command-line arguments.
    //
    for (int i=1; i<argc; i++)
    {
        if (argv[i][0] == L'-' || argv[i][0] == L'/')
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"ac", -1))
            {
                if (wzAppId)
                {
                    ExitOnFailure(hr = E_INVALIDARG, "May only specify one -ac switch.");
                }

                wzAppId = argv[++i];
                hr = ::CLSIDFromString(wzAppId, &guidApp);
                ExitOnFailure(hr, "Failed to parse the -ac argument.");
            }
        }
        else
        {
            ExitOnFailure1(hr = E_INVALIDARG, "Bad commandline argument: %S", argv[i]);
        }
    }
    ExitOnNull(wzAppId, hr, E_INVALIDARG, "Must specify a -ac switch.");

    hr = GetUpdateMutex(&guidApp, &hUpdateMutex);
    if (FAILED(hr))
    {
        TraceError(hr, "Failed to query the update mutex.  Proceeding as if this process didn't acquire the mutex.");
    }

    hr = RssUpdateGetAppInfo(wzAppId, &dw64Version, &pwzFeedUri, &pwzApplicationPath);
    ExitOnFailure(hr, "Failed to get app info.");

    // If we acquired the update lock and there is already an update downloaded, install that now.
    if (INVALID_HANDLE_VALUE != hUpdateMutex)
    {
        Trace(REPORT_DEBUG, "Got the update mutex.  Will check for updates on local machine before launching app.");

        // If an update is available and higher version that the application currently on the local 
        // machine, launch the install and bail.
        hr = RssUpdateTryLaunchUpdate(wzAppId, dw64Version, &hProcess, &dw64NextUpdateTime);
        if (SUCCEEDED(hr))
        {
            if (hProcess)
            {
                ::CloseHandle(hProcess);
                ExitFunction(); // bail since we're doing an update
            }
        }
    }
    else
    {
        Trace(REPORT_DEBUG, "Didn't get the update mutex.  Won't check for updates.");
    }

    hr = PathExpand(&pwzCommandLine, pwzApplicationPath, PATH_EXPAND_FULLPATH);
    ExitOnFailure(hr, "Failed to expand application path.");

    if (pwzCommandLine && L'\"' != pwzCommandLine[0])
    {
        // Get the working directory.
        hr = PathGetDirectory(pwzCommandLine, &pwzApplicationDirectory);
        ExitOnFailure(hr, "Failed to get application directory from command-line.");

        // Put quotes around the command line.
        hr = StrAllocPrefix(&pwzCommandLine, L"\"", 0);
        ExitOnFailure(hr, "Failed to prefix command-line with quote.");

        hr = StrAllocConcat(&pwzCommandLine, L"\"", 0);
        ExitOnFailure(hr, "Failed to concat command-line with quote.");
    }

    Trace1(REPORT_DEBUG, "Launching the target app with commandline: %ls.", pwzCommandLine);
    hr = LaunchTarget(pwzCommandLine, pwzApplicationDirectory, &hProcess);
    ExitOnFailure1(hr, "Failed to launch %ls", pwzCommandLine);

    // If we acquired the update lock then check to see if enough time has passed such that we look for more updates.
    if (INVALID_HANDLE_VALUE != hUpdateMutex)
    {
        hr = RssUpdateCheckFeed(wzAppId, dw64Version, pwzFeedUri, dw64NextUpdateTime);

        hr = S_OK;
    }

LExit:
    if (INVALID_HANDLE_VALUE != hUpdateMutex)
    {
        ::CloseHandle(hUpdateMutex);
    }

    if (INVALID_HANDLE_VALUE != hProcess)
    {
        ::CloseHandle(hProcess);
    }

    if (bDeleteUpdateInfoPath)
    {
        ::DeleteFileW(pwzFeedPath);
    }

    if (bDeleteUpdateBinaryPath)
    {
        ::DeleteFileW(pwzSetupPath);
    }

    ReleaseStr(pwzApplicationSource);
    ReleaseStr(pwzApplicationId);
    ReleaseStr(pwzSetupPath);
    ReleaseStr(pwzFeedPath);
    ReleaseStr(pwzApplicationPath);
    ReleaseStr(pwzFeedUri);
    ReleaseStr(pwzCommandLine);
    ReleaseStr(pwzApplicationDirectory);

    return SCODE_CODE(hr);
}
