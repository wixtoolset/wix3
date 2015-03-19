//-------------------------------------------------------------------------------------------------
// <copyright file="qtexec.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Executes command line instructions without popping up a shell.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define OUTPUT_BUFFER 1024


#define ONEMINUTE 60000

static HRESULT CreatePipes(
    __out HANDLE *phOutRead,
    __out HANDLE *phOutWrite,
    __out HANDLE *phErrWrite,
    __out HANDLE *phInRead,
    __out HANDLE *phInWrite
    )
{
    Assert(phOutRead);
    Assert(phOutWrite);
    Assert(phErrWrite);
    Assert(phInRead);
    Assert(phInWrite);

    HRESULT hr = S_OK;
    SECURITY_ATTRIBUTES sa;
    HANDLE hOutTemp = INVALID_HANDLE_VALUE;
    HANDLE hInTemp = INVALID_HANDLE_VALUE;

    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    // Fill out security structure so we can inherit handles
    ::ZeroMemory(&sa, sizeof(SECURITY_ATTRIBUTES));
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;

    // Create pipes
    if (!::CreatePipe(&hOutTemp, &hOutWrite, &sa, 0))
    {
        ExitOnLastError(hr, "failed to create output pipe");
    }

    if (!::CreatePipe(&hInRead, &hInTemp, &sa, 0))
    {
        ExitOnLastError(hr, "failed to create input pipe");
    }


    // Duplicate output pipe so standard error and standard output write to
    // the same pipe
    if (!::DuplicateHandle(::GetCurrentProcess(), hOutWrite, ::GetCurrentProcess(), &hErrWrite, 0, TRUE, DUPLICATE_SAME_ACCESS))
    {
        ExitOnLastError(hr, "failed to duplicate write handle");
    }

    // We need to create new output read and input write handles that are
    // non inheritable.  Otherwise it creates handles that can't be closed.
    if (!::DuplicateHandle(::GetCurrentProcess(), hOutTemp, ::GetCurrentProcess(), &hOutRead, 0, FALSE, DUPLICATE_SAME_ACCESS))
    {
        ExitOnLastError(hr, "failed to duplicate output pipe");
    }

    if (!::DuplicateHandle(::GetCurrentProcess(), hInTemp, ::GetCurrentProcess(), &hInWrite, 0, FALSE, DUPLICATE_SAME_ACCESS))
    {
        ExitOnLastError(hr, "failed to duplicate input pipe");
    }

    // now that everything has succeeded, assign to the outputs
    *phOutRead = hOutRead;
    hOutRead = INVALID_HANDLE_VALUE;

    *phOutWrite = hOutWrite;
    hOutWrite = INVALID_HANDLE_VALUE;

    *phErrWrite = hErrWrite;
    hErrWrite = INVALID_HANDLE_VALUE;

    *phInRead = hInRead;
    hInRead = INVALID_HANDLE_VALUE;

    *phInWrite = hInWrite;
    hInWrite = INVALID_HANDLE_VALUE;

LExit:
    ReleaseFile(hOutRead);
    ReleaseFile(hOutWrite);
    ReleaseFile(hErrWrite);
    ReleaseFile(hInRead);
    ReleaseFile(hInWrite);
    ReleaseFile(hOutTemp);
    ReleaseFile(hInTemp);

    return hr;
}

static HRESULT LogOutput(
    __in HANDLE hRead
    )
{
    BYTE *pBuffer = NULL;
    LPWSTR szLog = NULL;
    LPWSTR szTemp = NULL;
    LPWSTR pEnd = NULL;
    LPWSTR pNext = NULL;
    LPWSTR sczEscaped = NULL;
    LPSTR szWrite = NULL;
    DWORD dwBytes = OUTPUT_BUFFER;
    BOOL bFirst = TRUE;
    BOOL bUnicode = TRUE;
    HRESULT hr = S_OK;

    // Get buffer for output
    pBuffer = static_cast<BYTE *>(MemAlloc(OUTPUT_BUFFER, FALSE));
    ExitOnNull(pBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer for output.");

    while (0 != dwBytes)
    {
        ::ZeroMemory(pBuffer, OUTPUT_BUFFER);
        if (!::ReadFile(hRead, pBuffer, OUTPUT_BUFFER - 1, &dwBytes, NULL) && GetLastError() != ERROR_BROKEN_PIPE)
        {
            ExitOnLastError(hr, "Failed to read from handle.");
        }

        // Check for UNICODE or ANSI output
        if (bFirst)
        {
            if ((isgraph(pBuffer[0]) && isgraph(pBuffer[1])) ||
                (isgraph(pBuffer[0]) && isspace(pBuffer[1])) ||
                (isspace(pBuffer[0]) && isgraph(pBuffer[1])) ||
                (isspace(pBuffer[0]) && isspace(pBuffer[1])))
            {
                bUnicode = FALSE;
            }

            bFirst = FALSE;
        }

        // Keep track of output
        if (bUnicode)
        {
            hr = StrAllocConcat(&szLog, (LPCWSTR)pBuffer, 0);
            ExitOnFailure(hr, "failed to concatenate output strings");
        }
        else
        {
            hr = StrAllocStringAnsi(&szTemp, (LPCSTR)pBuffer, 0, CP_OEMCP);
            ExitOnFailure(hr, "failed to allocate output string");
            hr = StrAllocConcat(&szLog, szTemp, 0);
            ExitOnFailure(hr, "failed to concatenate output strings");
        }

        // Log each line of the output
        pNext = szLog;
        pEnd = wcschr(szLog, L'\r');
        if (NULL == pEnd)
        {
            pEnd = wcschr(szLog, L'\n');
        }
        while (pEnd && *pEnd)
        {
            // Find beginning of next line
            pEnd[0] = 0;
            ++pEnd;
            if ((pEnd[0] == L'\r') || (pEnd[0] == L'\n'))
            {
                ++pEnd;
            }

            // Log output
            hr = StrAllocString(&sczEscaped, pNext, 0);
            ExitOnFailure(hr, "Failed to allocate copy of string");

            hr = StrReplaceStringAll(&sczEscaped, L"%", L"%%");
            ExitOnFailure(hr, "Failed to escape percent signs in string");

            hr = StrAnsiAllocString(&szWrite, sczEscaped, 0, CP_OEMCP);
            ExitOnFailure(hr, "failed to convert output to ANSI");
            WcaLog(LOGMSG_STANDARD, szWrite);

            // Next line
            pNext = pEnd;
            pEnd = wcschr(pNext, L'\r');
            if (NULL == pEnd)
            {
                pEnd = wcschr(pNext, L'\n');
            }
        }

        hr = StrAllocString(&szTemp, pNext, 0);
        ExitOnFailure(hr, "failed to allocate string");

        hr = StrAllocString(&szLog, szTemp, 0);
        ExitOnFailure(hr, "failed to allocate string");
    }

    // Print any text that didn't end with a new line
    if (szLog && *szLog)
    {
        hr = StrReplaceStringAll(&szLog, L"%", L"%%");
        ExitOnFailure(hr, "Failed to escape percent signs in string");

        hr = StrAnsiAllocString(&szWrite, szLog, 0, CP_OEMCP);
        ExitOnFailure(hr, "failed to convert output to ANSI");

        WcaLog(LOGMSG_VERBOSE, szWrite);
    }

LExit:
    ReleaseMem(pBuffer);

    ReleaseStr(szLog);
    ReleaseStr(szTemp);
    ReleaseStr(szWrite);
    ReleaseStr(sczEscaped);

    return hr;
}

HRESULT WIXAPI QuietExec(
    __inout_z LPWSTR wzCommand,
    __in DWORD dwTimeout,
    __in BOOL fLogCommand,
    __in BOOL fLogOutput
    )
{
    HRESULT hr = S_OK;
    PROCESS_INFORMATION oProcInfo;
    STARTUPINFOW oStartInfo;
    DWORD dwExitCode = ERROR_SUCCESS;
    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    memset(&oProcInfo, 0, sizeof(oProcInfo));
    memset(&oStartInfo, 0, sizeof(oStartInfo));

    // Create output redirect pipes
    hr = CreatePipes(&hOutRead, &hOutWrite, &hErrWrite, &hInRead, &hInWrite);
    ExitOnFailure(hr, "failed to create output pipes");

    // Set up startup structure
    oStartInfo.cb = sizeof(STARTUPINFOW);
    oStartInfo.dwFlags = STARTF_USESTDHANDLES;
    oStartInfo.hStdInput = hInRead;
    oStartInfo.hStdOutput = hOutWrite;
    oStartInfo.hStdError = hErrWrite;

    // Log command if we were asked to do so
    if (fLogCommand)
    {
        WcaLog(LOGMSG_VERBOSE, "%ls", wzCommand);
    }

#pragma prefast(suppress:25028)
    if (::CreateProcessW(NULL,
        wzCommand, // command line
        NULL, // security info
        NULL, // thread info
        TRUE, // inherit handles
        ::GetPriorityClass(::GetCurrentProcess()) | CREATE_NO_WINDOW, // creation flags
        NULL, // environment
        NULL, // cur dir
        &oStartInfo,
        &oProcInfo))
    {
        ReleaseFile(oProcInfo.hThread);

        // Close child output/input handles so it doesn't hang
        ReleaseFile(hOutWrite);
        ReleaseFile(hErrWrite);
        ReleaseFile(hInRead);

        // Log output if we were asked to do so
        if (fLogOutput)
        {
            LogOutput(hOutRead);
        }

        // Wait for everything to finish
        ::WaitForSingleObject(oProcInfo.hProcess, dwTimeout);
        if (!::GetExitCodeProcess(oProcInfo.hProcess, &dwExitCode))
        {
            dwExitCode = ERROR_SEM_IS_SET;
        }

        ReleaseFile(hOutRead);
        ReleaseFile(hInWrite);
        ReleaseFile(oProcInfo.hProcess);
    }
    else
    {
        ExitOnLastError(hr, "Command failed to execute.");
    }

    ExitOnWin32Error(dwExitCode, hr, "Command line returned an error.");

LExit:
    return hr;
}

