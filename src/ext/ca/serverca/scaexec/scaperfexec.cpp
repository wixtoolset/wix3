// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

typedef DWORD (STDAPICALLTYPE *PFNPERFCOUNTERTEXTSTRINGS)(LPWSTR lpCommandLine, BOOL bQuietModeArg);

static HRESULT ExecutePerfCounterData(
    __in MSIHANDLE hInstall,
    __in BOOL fInstall
    );
static HRESULT CreateDataFile(
    __in LPCWSTR wzTempFolder,
    __in LPCWSTR wzData,
    __in BOOL fIniData,
    __out HANDLE *phFile,
    __out_opt LPWSTR *ppwzFile
    );


/********************************************************************
 RegisterPerfCounterData - CUSTOM ACTION ENTRY POINT for registering
                           performance counters

 Input: deferred CustomActionData: wzName\twzIniData\twzConstantData\twzName\twzIniData\twzConstantData\t...
*******************************************************************/
extern "C" UINT __stdcall RegisterPerfCounterData(
    __in MSIHANDLE hInstall
    )
{
    // AssertSz(FALSE, "debug RegisterPerfCounterData()");
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "RegisterPerfCounterData");
    ExitOnFailure(hr, "Failed to initialize RegisterPerfCounterData.");

    hr = ExecutePerfCounterData(hInstall, TRUE);
    MessageExitOnFailure(hr, msierrInstallPerfCounterData, "Failed to execute PerformanceCategory table.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 UnregisterPerfCounterData - CUSTOM ACTION ENTRY POINT for registering
                           performance counters

 Input: deferred CustomActionData: wzName\twzIniData\twzConstantData\twzName\twzIniData\twzConstantData\t...
*******************************************************************/
extern "C" UINT __stdcall UnregisterPerfCounterData(
    __in MSIHANDLE hInstall
    )
{
    // AssertSz(FALSE, "debug UnregisterPerfCounterData()");
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "UnregisterPerfCounterData");
    ExitOnFailure(hr, "Failed to initialize UnregisterPerfCounterData.");

    hr = ExecutePerfCounterData(hInstall, FALSE);
    MessageExitOnFailure(hr, msierrUninstallPerfCounterData, "Failed to execute PerformanceCategory table.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 RegisterPerfmon - CUSTOM ACTION ENTRY POINT for registering
                   counters

 Input:  deferred CustomActionData - 
    wzFile or wzName
*******************************************************************/
extern "C" UINT __stdcall RegisterPerfmon(
    __in MSIHANDLE hInstall
    )
{
//    Assert(FALSE);
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;
    LPWSTR pwzData = NULL;

    HMODULE hMod = NULL;
    PFNPERFCOUNTERTEXTSTRINGS pfnPerfCounterTextString;
    DWORD_PTR dwRet;
    LPWSTR pwzShortPath = NULL;
    DWORD_PTR cchShortPath = MAX_PATH;
    DWORD_PTR cchShortPathLength  = 0;

    LPWSTR pwzCommand = NULL;

    hr = WcaInitialize(hInstall, "RegisterPerfmon");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    // do the perfmon registration
    if (NULL == hMod)
    {
        hr = LoadSystemLibrary(L"loadperf.dll", &hMod);
    }
    ExitOnFailure(hr, "failed to load DLL for PerfMon");

    pfnPerfCounterTextString = (PFNPERFCOUNTERTEXTSTRINGS)::GetProcAddress(hMod, "LoadPerfCounterTextStringsW");
    ExitOnNullWithLastError(pfnPerfCounterTextString, hr, "failed to get DLL function for PerfMon");

    hr = StrAlloc(&pwzShortPath, cchShortPath);
    ExitOnFailure(hr, "failed to allocate string");

    WcaLog(LOGMSG_VERBOSE, "Converting DLL path to short format: %ls", pwzData);
    cchShortPathLength = ::GetShortPathNameW(pwzData, pwzShortPath, cchShortPath);
    if (cchShortPathLength > cchShortPath)
    {
        cchShortPath = cchShortPathLength + 1;
        hr = StrAlloc(&pwzShortPath, cchShortPath);
        ExitOnFailure(hr, "failed to allocate string");

        cchShortPathLength = ::GetShortPathNameW(pwzData, pwzShortPath, cchShortPath);
    }

    if (0 == cchShortPathLength)
    {
        ExitOnLastError1(hr, "failed to get short path format of path: %ls", pwzData);
    }

    hr = StrAllocFormatted(&pwzCommand, L"lodctr \"%s\"", pwzShortPath);
    ExitOnFailure(hr, "failed to format lodctr string");

    WcaLog(LOGMSG_VERBOSE, "RegisterPerfmon running command: '%ls'", pwzCommand);
    dwRet = (*pfnPerfCounterTextString)(pwzCommand, TRUE);
    if (dwRet != ERROR_SUCCESS && dwRet != ERROR_ALREADY_EXISTS)
    {
        hr = HRESULT_FROM_WIN32(dwRet);
        MessageExitOnFailure1(hr, msierrPERFMONFailedRegisterDLL, "failed to register with PerfMon, DLL: %ls", pwzData);
    }

    hr = S_OK;
LExit:
    ReleaseStr(pwzData);

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


extern "C" UINT __stdcall UnregisterPerfmon(
    __in MSIHANDLE hInstall
    )
{
//    Assert(FALSE);
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;
    LPWSTR pwzData = NULL;

    HMODULE hMod = NULL;
    PFNPERFCOUNTERTEXTSTRINGS pfnPerfCounterTextString;
    DWORD dwRet;
    WCHAR wz[255];

    hr = WcaInitialize(hInstall, "UnregisterPerfmon");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    // do the perfmon unregistration
    hr = E_FAIL;
    if (hMod == NULL)
    {
        hr = LoadSystemLibrary(L"loadperf.dll", &hMod);
    }
    ExitOnFailure(hr, "failed to load DLL for PerfMon");

    pfnPerfCounterTextString = (PFNPERFCOUNTERTEXTSTRINGS)::GetProcAddress(hMod, "UnloadPerfCounterTextStringsW");
    ExitOnNullWithLastError(pfnPerfCounterTextString, hr, "failed to get DLL function for PerfMon");

    hr = ::StringCchPrintfW(wz, countof(wz), L"unlodctr \"%s\"", pwzData);
    ExitOnFailure1(hr, "Failed to format unlodctr string with: %ls", pwzData);
    WcaLog(LOGMSG_VERBOSE, "UnregisterPerfmon running command: '%ls'", wz);
    dwRet = (*pfnPerfCounterTextString)(wz, TRUE);
    // if the counters aren't registered, then OK to continue
    if (dwRet != ERROR_SUCCESS && dwRet != ERROR_FILE_NOT_FOUND && dwRet != ERROR_BADKEY)
    {
        hr = HRESULT_FROM_WIN32(dwRet);
        MessageExitOnFailure1(hr, msierrPERFMONFailedUnregisterDLL, "failed to unregsister with PerfMon, DLL: %ls", pwzData);
    }

    hr = S_OK;
LExit:
    ReleaseStr(pwzData);

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


static HRESULT ExecutePerfCounterData(
    __in MSIHANDLE /*hInstall*/,
    __in BOOL fInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    HMODULE hModule = NULL;
    PFNPERFCOUNTERTEXTSTRINGS pfnPerfCounterTextString = NULL;
    LPCWSTR wzPrefix = NULL;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwz = NULL;

    LPWSTR pwzName = NULL;
    LPWSTR pwzIniData = NULL;
    LPWSTR pwzConstantData = NULL;
    LPWSTR pwzTempFolder = NULL;
    LPWSTR pwzIniFile = NULL;
    LPWSTR pwzExecute = NULL;

    HANDLE hIniData = INVALID_HANDLE_VALUE;
    HANDLE hConstantData = INVALID_HANDLE_VALUE;

    // Load the system performance counter helper DLL then get the appropriate
    // entrypoint out of it. Fortunately, they have the same signature so we
    // can use one function pointer to point to both.
    hr = LoadSystemLibrary(L"loadperf.dll", &hModule);
    ExitOnFailure(hr, "failed to load DLL for PerfMon");

    if (fInstall)
    {
        wzPrefix = L"lodctr";
        pfnPerfCounterTextString = (PFNPERFCOUNTERTEXTSTRINGS)::GetProcAddress(hModule, "LoadPerfCounterTextStringsW");
    }
    else
    {
        wzPrefix = L"unlodctr";
        pfnPerfCounterTextString = (PFNPERFCOUNTERTEXTSTRINGS)::GetProcAddress(hModule, "UnloadPerfCounterTextStringsW");
    }
    ExitOnNullWithLastError(pfnPerfCounterTextString, hr, "Failed to get DLL function for PerfMon");

    // Now get the CustomActionData and execute it.
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData.");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    pwz = pwzCustomActionData;

    while (S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzName)))
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzIniData);
        ExitOnFailure(hr, "Failed to read IniData from custom action data.");

        hr = WcaReadStringFromCaData(&pwz, &pwzConstantData);
        ExitOnFailure(hr, "Failed to read ConstantData from custom action data.");

        if (fInstall)
        {
            hr = PathCreateTempDirectory(NULL, L"WIXPF%03x", 999, &pwzTempFolder);
            ExitOnFailure(hr, "Failed to create temp directory.");

            hr = CreateDataFile(pwzTempFolder, pwzIniData, TRUE, &hIniData, &pwzIniFile);
            ExitOnFailure1(hr, "Failed to create .ini file for performance counter category: %ls", pwzName);

            hr = CreateDataFile(pwzTempFolder, pwzConstantData, FALSE, &hConstantData, NULL);
            ExitOnFailure1(hr, "Failed to create .h file for performance counter category: %ls", pwzName);

            hr = StrAllocFormatted(&pwzExecute, L"%s \"%s\"", wzPrefix, pwzIniFile);
            ExitOnFailure(hr, "Failed to allocate string to execute.");

            // Execute the install.
            er = (*pfnPerfCounterTextString)(pwzExecute, TRUE);
            hr = HRESULT_FROM_WIN32(er);
            ExitOnFailure1(hr, "Failed to execute install of performance counter category: %ls", pwzName);

            if (INVALID_HANDLE_VALUE != hIniData)
            {
                ::CloseHandle(hIniData);
                hIniData = INVALID_HANDLE_VALUE;
            }
    
            if (INVALID_HANDLE_VALUE != hConstantData)
            {
                ::CloseHandle(hConstantData);
                hConstantData = INVALID_HANDLE_VALUE;
            }

            DirEnsureDelete(pwzTempFolder, TRUE, TRUE);
        }
        else
        {
            hr = StrAllocFormatted(&pwzExecute, L"%s \"%s\"", wzPrefix, pwzName);
            ExitOnFailure(hr, "Failed to allocate string to execute.");

            // Execute the uninstall and if the counter isn't registered then ignore
            // the error since it won't hurt anything.
            er = (*pfnPerfCounterTextString)(pwzExecute, TRUE);
            if (ERROR_FILE_NOT_FOUND == er || ERROR_BADKEY == er)
            {
                er = ERROR_SUCCESS;
            }
            hr = HRESULT_FROM_WIN32(er);
            ExitOnFailure1(hr, "Failed to execute uninstall of performance counter category: %ls", pwzName);
        }
    }

    if (E_NOMOREITEMS == hr) // If there are no more items, all is well
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to execute all perf counter data.");

    hr = S_OK;

LExit:
    if (INVALID_HANDLE_VALUE != hIniData)
    {
        ::CloseHandle(hIniData);
    }

    if (INVALID_HANDLE_VALUE != hConstantData)
    {
        ::CloseHandle(hConstantData);
    }

    ReleaseStr(pwzExecute);
    ReleaseStr(pwzIniFile);
    ReleaseStr(pwzTempFolder);
    ReleaseStr(pwzConstantData);
    ReleaseStr(pwzIniData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzCustomActionData);

    if (hModule)
    {
        ::FreeLibrary(hModule);
    }

    return hr;
}


static HRESULT CreateDataFile(
    __in LPCWSTR wzTempFolder,
    __in LPCWSTR wzData,
    __in BOOL fIniData,
    __out HANDLE *phFile,
    __out_opt LPWSTR *ppwzFile
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    LPWSTR pwzFile = NULL;
    LPSTR pszData = NULL;
    DWORD cbData = 0;
    DWORD cbWritten = 0;

    // Convert the data to UTF-8 because lodctr/unloctr
    // doesn't like unicode.
    hr = StrAnsiAllocString(&pszData, wzData, 0, CP_UTF8);
    ExitOnFailure(hr, "Failed to covert data to ANSI.");

    cbData = lstrlen(pszData);

    // Concatenate the paths together, open the file data file
    // and dump the data in there.
    hr = StrAllocString(&pwzFile, wzTempFolder, 0);
    ExitOnFailure(hr, "Failed to copy temp directory name.");

    hr = StrAllocConcat(&pwzFile, L"wixperf", 0);
    ExitOnFailure(hr, "Failed to add name of file.");

    hr = StrAllocConcat(&pwzFile, fIniData ? L".ini" : L".h", 0);
    ExitOnFailure(hr, "Failed to add extension of file.");

    hFile = ::CreateFileW(pwzFile, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError1(hr, "Failed to open new temp file: %ls", pwzFile);
    }

    if (!::WriteFile(hFile, pszData, cbData, &cbWritten, NULL))
    {
        ExitWithLastError1(hr, "Failed to write data to new temp file: %ls", pwzFile);
    }

    if (INVALID_HANDLE_VALUE != hFile)
    {
        ::CloseHandle(hFile);
        hFile = INVALID_HANDLE_VALUE;
    }

    // Return the requested values.
    *phFile = hFile;
    hFile = INVALID_HANDLE_VALUE;

    if (ppwzFile)
    {
        *ppwzFile = pwzFile;
        pwzFile = NULL;
    }

LExit:
    if (INVALID_HANDLE_VALUE != hFile)
    {
        ::CloseHandle(hFile);
    }
    ReleaseStr(pszData);
    ReleaseStr(pwzFile);

    return hr;
}
