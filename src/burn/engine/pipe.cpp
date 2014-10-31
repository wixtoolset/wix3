//-------------------------------------------------------------------------------------------------
// <copyright file="pipe.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Burn Client Server pipe communication handler.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static const DWORD PIPE_64KB = 64 * 1024;
static const DWORD PIPE_WAIT_FOR_CONNECTION = 100;   // wait a 10th of a second,
static const DWORD PIPE_RETRY_FOR_CONNECTION = 1800; // for up to 3 minutes.

static const LPCWSTR PIPE_NAME_FORMAT_STRING = L"\\\\.\\pipe\\%ls";
static const LPCWSTR CACHE_PIPE_NAME_FORMAT_STRING = L"\\\\.\\pipe\\%ls.Cache";

static HRESULT AllocatePipeMessage(
    __in DWORD dwMessage,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in DWORD cbData,
    __out_bcount(cb) LPVOID* ppvMessage,
    __out DWORD* cbMessage
    );
static void FreePipeMessage(
    __in BURN_PIPE_MESSAGE *pMsg
    );
static HRESULT WritePipeMessage(
    __in HANDLE hPipe,
    __in DWORD dwMessage,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in DWORD cbData
    );
static HRESULT GetPipeMessage(
    __in HANDLE hPipe,
    __in BURN_PIPE_MESSAGE* pMsg
    );
static HRESULT ChildPipeConnected(
    __in HANDLE hPipe,
    __in_z LPCWSTR wzSecret,
    __inout DWORD* pdwProcessId
    );



/*******************************************************************
 PipeConnectionInitialize - initialize pipe connection data.

*******************************************************************/
void PipeConnectionInitialize(
    __in BURN_PIPE_CONNECTION* pConnection
    )
{
    memset(pConnection, 0, sizeof(BURN_PIPE_CONNECTION));
    pConnection->hPipe = INVALID_HANDLE_VALUE;
    pConnection->hCachePipe = INVALID_HANDLE_VALUE;
}

/*******************************************************************
 PipeConnectionUninitialize - free data in a pipe connection.

*******************************************************************/
void PipeConnectionUninitialize(
    __in BURN_PIPE_CONNECTION* pConnection
    )
{
    ReleaseFileHandle(pConnection->hCachePipe);
    ReleaseFileHandle(pConnection->hPipe);
    ReleaseHandle(pConnection->hProcess);
    ReleaseStr(pConnection->sczSecret);
    ReleaseStr(pConnection->sczName);

    memset(pConnection, 0, sizeof(BURN_PIPE_CONNECTION));
    pConnection->hPipe = INVALID_HANDLE_VALUE;
    pConnection->hCachePipe = INVALID_HANDLE_VALUE;
}

/*******************************************************************
 PipeSendMessage - 

*******************************************************************/
extern "C" HRESULT PipeSendMessage(
    __in HANDLE hPipe,
    __in DWORD dwMessage,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in DWORD cbData,
    __in_opt PFN_PIPE_MESSAGE_CALLBACK pfnCallback,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    BURN_PIPE_RESULT result = { };

    hr = WritePipeMessage(hPipe, dwMessage, pvData, cbData);
    ExitOnFailure(hr, "Failed to write send message to pipe.");

    hr = PipePumpMessages(hPipe, pfnCallback, pvContext, &result);
    ExitOnFailure(hr, "Failed to pump messages during send message to pipe.");

    *pdwResult = result.dwResult;

LExit:
    return hr;
}

/*******************************************************************
 PipePumpMessages - 

*******************************************************************/
extern "C" HRESULT PipePumpMessages(
    __in HANDLE hPipe,
    __in_opt PFN_PIPE_MESSAGE_CALLBACK pfnCallback,
    __in_opt LPVOID pvContext,
    __in BURN_PIPE_RESULT* pResult
    )
{
    HRESULT hr = S_OK;
    BURN_PIPE_MESSAGE msg = { };
    SIZE_T iData = 0;
    LPSTR sczMessage = NULL;
    DWORD dwResult = 0;

    // Pump messages from child process.
    while (S_OK == (hr = GetPipeMessage(hPipe, &msg)))
    {
        switch (msg.dwMessage)
        {
        case BURN_PIPE_MESSAGE_TYPE_LOG:
            iData = 0;

            hr = BuffReadStringAnsi((BYTE*)msg.pvData, msg.cbData, &iData, &sczMessage);
            ExitOnFailure(hr, "Failed to read log message.");

            hr = LogStringWorkRaw(sczMessage);
            ExitOnFailure1(hr, "Failed to write log message:'%hs'.", sczMessage);

            dwResult = static_cast<DWORD>(hr);
            break;

        case BURN_PIPE_MESSAGE_TYPE_COMPLETE:
            if (!msg.pvData || sizeof(DWORD) != msg.cbData)
            {
                hr = E_INVALIDARG;
                ExitOnRootFailure(hr, "No status returned to PipePumpMessages()");
            }

            pResult->dwResult = *static_cast<DWORD*>(msg.pvData);
            ExitFunction1(hr = S_OK); // exit loop.

        case BURN_PIPE_MESSAGE_TYPE_TERMINATE:
            iData = 0;

            hr = BuffReadNumber(static_cast<BYTE*>(msg.pvData), msg.cbData, &iData, &pResult->dwResult);
            ExitOnFailure(hr, "Failed to read returned result to PipePumpMessages()");

            if (sizeof(DWORD) * 2 == msg.cbData)
            {
                hr = BuffReadNumber(static_cast<BYTE*>(msg.pvData), msg.cbData, &iData, (DWORD*)&pResult->fRestart);
                ExitOnFailure(hr, "Failed to read returned restart to PipePumpMessages()");
            }

            ExitFunction1(hr = S_OK); // exit loop.

        default:
            if (pfnCallback)
            {
                hr = pfnCallback(&msg, pvContext, &dwResult);
            }
            else
            {
                hr = E_INVALIDARG;
            }
            ExitOnFailure1(hr, "Failed to process message: %u", msg.dwMessage);
            break;
        }

        // post result
        hr = WritePipeMessage(hPipe, static_cast<DWORD>(BURN_PIPE_MESSAGE_TYPE_COMPLETE), &dwResult, sizeof(dwResult));
        ExitOnFailure(hr, "Failed to post result to child process.");

        FreePipeMessage(&msg);
    }
    ExitOnFailure(hr, "Failed to get message over pipe");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseStr(sczMessage);
    FreePipeMessage(&msg);

    return hr;
}

/*******************************************************************
 PipeCreateNameAndSecret - 

*******************************************************************/
extern "C" HRESULT PipeCreateNameAndSecret(
    __out_z LPWSTR *psczConnectionName,
    __out_z LPWSTR *psczSecret
    )
{
    HRESULT hr = S_OK;
    RPC_STATUS rs = RPC_S_OK;
    UUID guid = { };
    WCHAR wzGuid[39];
    LPWSTR sczConnectionName = NULL;
    LPWSTR sczSecret = NULL;

    // Create the unique pipe name.
    rs = ::UuidCreate(&guid);
    hr = HRESULT_FROM_RPC(rs);
    ExitOnFailure(hr, "Failed to create pipe guid.");

    if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
    {
        hr = E_OUTOFMEMORY;
        ExitOnRootFailure(hr, "Failed to convert pipe guid into string.");
    }

    hr = StrAllocFormatted(&sczConnectionName, L"BurnPipe.%s", wzGuid);
    ExitOnFailure(hr, "Failed to allocate pipe name.");

    // Create the unique client secret.
    rs = ::UuidCreate(&guid);
    hr = HRESULT_FROM_RPC(rs);
    ExitOnRootFailure(hr, "Failed to create pipe guid.");

    if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
    {
        hr = E_OUTOFMEMORY;
        ExitOnRootFailure(hr, "Failed to convert pipe guid into string.");
    }

    hr = StrAllocString(&sczSecret, wzGuid, 0);
    ExitOnFailure(hr, "Failed to allocate pipe secret.");

    *psczConnectionName = sczConnectionName;
    sczConnectionName = NULL;
    *psczSecret = sczSecret;
    sczSecret = NULL;

LExit:
    ReleaseStr(sczSecret);
    ReleaseStr(sczConnectionName);

    return hr;
}

/*******************************************************************
 PipeCreatePipes - create the pipes and event to signal child process.

*******************************************************************/
extern "C" HRESULT PipeCreatePipes(
    __in BURN_PIPE_CONNECTION* pConnection,
    __in BOOL fCreateCachePipe,
    __out HANDLE* phEvent
    )
{
    Assert(pConnection->sczName);
    Assert(INVALID_HANDLE_VALUE == pConnection->hPipe);
    Assert(INVALID_HANDLE_VALUE == pConnection->hCachePipe);

    HRESULT hr = S_OK;
    PSECURITY_DESCRIPTOR psd = NULL;
    SECURITY_ATTRIBUTES sa = { };
    LPWSTR sczFullPipeName = NULL;
    HANDLE hPipe = INVALID_HANDLE_VALUE;
    HANDLE hCachePipe = INVALID_HANDLE_VALUE;

    // Only the grant special rights when the pipe is being used for "embedded"
    // scenarios (aka: there is no cache pipe).
    if (!fCreateCachePipe)
    {
        // Create the security descriptor that grants read/write/sync access to Everyone.
        // TODO: consider locking down "WD" to LogonIds (logon session)
        LPCWSTR wzSddl = L"D:(A;;GA;;;SY)(A;;GA;;;BA)(A;;GRGW0x00100000;;;WD)";
        if (!::ConvertStringSecurityDescriptorToSecurityDescriptorW(wzSddl, SDDL_REVISION_1, &psd, NULL))
        {
            ExitWithLastError(hr, "Failed to create the security descriptor for the connection event and pipe.");
        }

        sa.nLength = sizeof(sa);
        sa.lpSecurityDescriptor = psd;
        sa.bInheritHandle = FALSE;
    }

    // Create the pipe.
    hr = StrAllocFormatted(&sczFullPipeName, PIPE_NAME_FORMAT_STRING, pConnection->sczName);
    ExitOnFailure1(hr, "Failed to allocate full name of pipe: %ls", pConnection->sczName);

    // TODO: consider using overlapped IO to do waits on the pipe and still be able to cancel and such.
    hPipe = ::CreateNamedPipeW(sczFullPipeName, PIPE_ACCESS_DUPLEX | FILE_FLAG_FIRST_PIPE_INSTANCE, PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT, 1, PIPE_64KB, PIPE_64KB, 1, psd ? &sa : NULL);
    if (INVALID_HANDLE_VALUE == hPipe)
    {
        ExitWithLastError1(hr, "Failed to create pipe: %ls", sczFullPipeName);
    }

    if (fCreateCachePipe)
    {
        // Create the cache pipe.
        hr = StrAllocFormatted(&sczFullPipeName, CACHE_PIPE_NAME_FORMAT_STRING, pConnection->sczName);
        ExitOnFailure1(hr, "Failed to allocate full name of cache pipe: %ls", pConnection->sczName);

        hCachePipe = ::CreateNamedPipeW(sczFullPipeName, PIPE_ACCESS_DUPLEX | FILE_FLAG_FIRST_PIPE_INSTANCE, PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT, 1, PIPE_64KB, PIPE_64KB, 1, NULL);
        if (INVALID_HANDLE_VALUE == hCachePipe)
        {
            ExitWithLastError1(hr, "Failed to create pipe: %ls", sczFullPipeName);
        }
    }

    pConnection->hCachePipe = hCachePipe;
    hCachePipe = INVALID_HANDLE_VALUE;

    pConnection->hPipe = hPipe;
    hPipe = INVALID_HANDLE_VALUE;

    // TODO: remove the following
    *phEvent = NULL;

LExit:
    ReleaseFileHandle(hCachePipe);
    ReleaseFileHandle(hPipe);
    ReleaseStr(sczFullPipeName);

    if (psd)
    {
        ::LocalFree(psd);
    }

    return hr;
}

/*******************************************************************
 PipeLaunchParentProcess - Called from the per-machine process to create
                           a per-user process and set up the
                           communication pipe.

*******************************************************************/
HRESULT PipeLaunchParentProcess(
    __in_z LPCWSTR wzCommandLine,
    __in int nCmdShow,
    __in_z LPWSTR sczConnectionName,
    __in_z LPWSTR sczSecret,
    __in BOOL /*fDisableUnelevate*/
    )
{
    HRESULT hr = S_OK;
    DWORD dwProcessId = 0;
    LPWSTR sczBurnPath = NULL;
    LPWSTR sczParameters = NULL;
    HANDLE hProcess = NULL;

    dwProcessId = ::GetCurrentProcessId();

    hr = PathForCurrentProcess(&sczBurnPath, NULL);
    ExitOnFailure(hr, "Failed to get current process path.");

    hr = StrAllocFormatted(&sczParameters, L"-%ls %ls %ls %u %ls", BURN_COMMANDLINE_SWITCH_UNELEVATED, sczConnectionName, sczSecret, dwProcessId, wzCommandLine);
    ExitOnFailure(hr, "Failed to allocate parameters for unelevated process.");

#ifdef ENABLE_UNELEVATE
    if (fDisableUnelevate)
    {
        hr = ProcExec(sczBurnPath, sczParameters, nCmdShow, &hProcess);
        ExitOnFailure1(hr, "Failed to launch parent process with unelevate disabled: %ls", sczBurnPath);
    }
    else
    {
        // Try to launch unelevated and if that fails for any reason, try launch our process normally (even though that may make it elevated).
        hr = ProcExecuteAsInteractiveUser(sczBurnPath, sczParameters, &hProcess);
        if (FAILED(hr))
        {
            hr = ShelExecUnelevated(sczBurnPath, sczParameters, L"open", NULL, nCmdShow);
            if (FAILED(hr))
            {
                hr = ShelExec(sczBurnPath, sczParameters, L"open", NULL, nCmdShow, NULL, NULL);
                ExitOnFailure1(hr, "Failed to launch parent process: %ls", sczBurnPath);
            }
        }
    }
#else
    hr = ProcExec(sczBurnPath, sczParameters, nCmdShow, &hProcess);
    ExitOnFailure1(hr, "Failed to launch parent process with unelevate disabled: %ls", sczBurnPath);
#endif

LExit:
    ReleaseHandle(hProcess);
    ReleaseStr(sczParameters);
    ReleaseStr(sczBurnPath);

    return hr;
}

/*******************************************************************
 PipeLaunchChildProcess - Called from the per-user process to create
                          the per-machine process and set up the
                          communication pipe.

*******************************************************************/
extern "C" HRESULT PipeLaunchChildProcess(
    __in_z LPCWSTR wzExecutablePath,
    __in BURN_PIPE_CONNECTION* pConnection,
    __in BOOL fElevate,
    __in_opt HWND hwndParent
    )
{
    HRESULT hr = S_OK;
    DWORD dwCurrentProcessId = ::GetCurrentProcessId();
    LPWSTR sczParameters = NULL;
    OS_VERSION osVersion = OS_VERSION_UNKNOWN;
    DWORD dwServicePack = 0;
    LPCWSTR wzVerb = NULL;
    HANDLE hProcess = NULL;

    hr = StrAllocFormatted(&sczParameters, L"-q -%ls %ls %ls %u", BURN_COMMANDLINE_SWITCH_ELEVATED, pConnection->sczName, pConnection->sczSecret, dwCurrentProcessId);
    ExitOnFailure(hr, "Failed to allocate parameters for elevated process.");

    OsGetVersion(&osVersion, &dwServicePack);
    wzVerb = (OS_VERSION_VISTA > osVersion) || !fElevate ? L"open" : L"runas";

    hr = ShelExec(wzExecutablePath, sczParameters, wzVerb, NULL, SW_HIDE, hwndParent, &hProcess);
    ExitOnFailure1(hr, "Failed to launch elevated child process: %ls", wzExecutablePath);

    pConnection->dwProcessId = ::GetProcessId(hProcess);
    pConnection->hProcess = hProcess;
    hProcess = NULL;

LExit:
    ReleaseHandle(hProcess);
    ReleaseStr(sczParameters);

    return hr;
}

/*******************************************************************
 PipeWaitForChildConnect - 

*******************************************************************/
extern "C" HRESULT PipeWaitForChildConnect(
    __in BURN_PIPE_CONNECTION* pConnection
    )
{
    HRESULT hr = S_OK;
    HANDLE hPipes[2] = { pConnection->hPipe, pConnection->hCachePipe};
    LPCWSTR wzSecret = pConnection->sczSecret;
    DWORD cbSecret = lstrlenW(wzSecret) * sizeof(WCHAR);
    DWORD dwCurrentProcessId = ::GetCurrentProcessId();
    DWORD dwAck = 0;
    DWORD cb = 0;

    for (DWORD i = 0; i < countof(hPipes) && INVALID_HANDLE_VALUE != hPipes[i]; ++i)
    {
        HANDLE hPipe = hPipes[i];
        DWORD dwPipeState = PIPE_READMODE_BYTE | PIPE_NOWAIT;

        // Temporarily make the pipe non-blocking so we will not get stuck in ::ConnectNamedPipe() forever
        // if the child decides not to show up.
        if (!::SetNamedPipeHandleState(hPipe, &dwPipeState, NULL, NULL))
        {
            ExitWithLastError(hr, "Failed to set pipe to non-blocking.");
        }

        // Loop for a while waiting for a connection from child process.
        DWORD cRetry = 0;
        do
        {
            if (!::ConnectNamedPipe(hPipe, NULL))
            {
                DWORD er = ::GetLastError();
                if (ERROR_PIPE_CONNECTED == er)
                {
                    hr = S_OK;
                    break;
                }
                else if (ERROR_PIPE_LISTENING == er)
                {
                    if (cRetry < PIPE_RETRY_FOR_CONNECTION)
                    {
                        hr = HRESULT_FROM_WIN32(er);
                    }
                    else
                    {
                        hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
                        break;
                    }

                    ++cRetry;
                    ::Sleep(PIPE_WAIT_FOR_CONNECTION);
                }
                else
                {
                    hr = HRESULT_FROM_WIN32(er);
                    break;
                }
            }
        } while (HRESULT_FROM_WIN32(ERROR_PIPE_LISTENING) == hr);
        ExitOnRootFailure(hr, "Failed to wait for child to connect to pipe.");

        // Put the pipe back in blocking mode.
        dwPipeState = PIPE_READMODE_BYTE | PIPE_WAIT;
        if (!::SetNamedPipeHandleState(hPipe, &dwPipeState, NULL, NULL))
        {
            ExitWithLastError(hr, "Failed to reset pipe to blocking.");
        }

        // Prove we are the one that created the elevated process by passing the secret.
        if (!::WriteFile(hPipe, &cbSecret, sizeof(cbSecret), &cb, NULL))
        {
            ExitWithLastError(hr, "Failed to write secret length to pipe.");
        }

        if (!::WriteFile(hPipe, wzSecret, cbSecret, &cb, NULL))
        {
            ExitWithLastError(hr, "Failed to write secret to pipe.");
        }

        if (!::WriteFile(hPipe, &dwCurrentProcessId, sizeof(dwCurrentProcessId), &cb, NULL))
        {
            ExitWithLastError(hr, "Failed to write our process id to pipe.");
        }

        // Wait until the elevated process responds that it is ready to go.
        if (!::ReadFile(hPipe, &dwAck, sizeof(dwAck), &cb, NULL))
        {
            ExitWithLastError(hr, "Failed to read ACK from pipe.");
        }

        // The ACK should match out expected child process id.
        //if (pConnection->dwProcessId != dwAck)
        //{
        //    hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        //    ExitOnRootFailure1(hr, "Incorrect ACK from elevated pipe: %u", dwAck);
        //}
    }

LExit:
    return hr;
}

/*******************************************************************
 PipeTerminateChildProcess - 

*******************************************************************/
extern "C" HRESULT PipeTerminateChildProcess(
    __in BURN_PIPE_CONNECTION* pConnection,
    __in DWORD dwParentExitCode,
    __in BOOL fRestart
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;

    // Prepare the exit message.
    hr = BuffWriteNumber(&pbData, &cbData, dwParentExitCode);
    ExitOnFailure(hr, "Failed to write exit code to message buffer.");

    hr = BuffWriteNumber(&pbData, &cbData, fRestart);
    ExitOnFailure(hr, "Failed to write restart to message buffer.");

    // Send the messages.
    if (INVALID_HANDLE_VALUE != pConnection->hCachePipe)
    {
        hr = WritePipeMessage(pConnection->hCachePipe, static_cast<DWORD>(BURN_PIPE_MESSAGE_TYPE_TERMINATE), pbData, cbData);
        ExitOnFailure(hr, "Failed to post terminate message to child process cache thread.");
    }

    hr = WritePipeMessage(pConnection->hPipe, static_cast<DWORD>(BURN_PIPE_MESSAGE_TYPE_TERMINATE), pbData, cbData);
    ExitOnFailure(hr, "Failed to post terminate message to child process.");

    // If we were able to get a handle to the other process, wait for it to exit.
    if (pConnection->hProcess)
    {
        if (WAIT_FAILED == ::WaitForSingleObject(pConnection->hProcess, PIPE_WAIT_FOR_CONNECTION * PIPE_RETRY_FOR_CONNECTION))
        {
            ExitWithLastError(hr, "Failed to wait for child process exit.");
        }

#ifdef DEBUG
        DWORD dwChildExitCode = 0;
        DWORD dwErrorCode = ERROR_SUCCESS;
        BOOL fReturnedExitCode = ::GetExitCodeProcess(pConnection->hProcess, &dwChildExitCode);
        if (!fReturnedExitCode)
        {
            dwErrorCode = ::GetLastError(); // if the other process is elevated and we are not, then we'll get ERROR_ACCESS_DENIED.

            // The unit test use a thread instead of a process so try to get the exit code from
            // the thread because we failed to get it from the process.
            if (ERROR_INVALID_HANDLE == dwErrorCode)
            {
                fReturnedExitCode = ::GetExitCodeThread(pConnection->hProcess, &dwChildExitCode);
            }
        }
        AssertSz((fReturnedExitCode && dwChildExitCode == dwParentExitCode) ||
                 (!fReturnedExitCode && ERROR_ACCESS_DENIED == dwErrorCode),
                 "Child elevated process did not return matching exit code to parent process.");
#endif
    }

LExit:
    return hr;
}

/*******************************************************************
 PipeChildConnect - Called from the child process to connect back
                    to the pipe provided by the parent process.

*******************************************************************/
extern "C" HRESULT PipeChildConnect(
    __in BURN_PIPE_CONNECTION* pConnection,
    __in BOOL fConnectCachePipe
    )
{
    Assert(pConnection->sczName);
    Assert(pConnection->sczSecret);
    Assert(!pConnection->hProcess);
    Assert(INVALID_HANDLE_VALUE == pConnection->hPipe);
    Assert(INVALID_HANDLE_VALUE == pConnection->hCachePipe);

    HRESULT hr = S_OK;
    LPWSTR sczPipeName = NULL;

    // Try to connect to the parent.
    hr = StrAllocFormatted(&sczPipeName, PIPE_NAME_FORMAT_STRING, pConnection->sczName);
    ExitOnFailure(hr, "Failed to allocate name of parent pipe.");

    hr = E_UNEXPECTED;
    for (DWORD cRetry = 0; FAILED(hr) && cRetry < PIPE_RETRY_FOR_CONNECTION; ++cRetry)
    {
        pConnection->hPipe = ::CreateFileW(sczPipeName, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
        if (INVALID_HANDLE_VALUE == pConnection->hPipe)
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (E_FILENOTFOUND == hr) // if the pipe isn't created, call it a timeout waiting on the parent.
            {
                hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
            }

            ::Sleep(PIPE_WAIT_FOR_CONNECTION);
        }
        else // we have a connection, go with it.
        {
            hr = S_OK;
        }
    }
    ExitOnRootFailure1(hr, "Failed to open parent pipe: %ls", sczPipeName)

    // Verify the parent and notify it that the child connected.
    hr = ChildPipeConnected(pConnection->hPipe, pConnection->sczSecret, &pConnection->dwProcessId);
    ExitOnFailure1(hr, "Failed to verify parent pipe: %ls", sczPipeName);

    if (fConnectCachePipe)
    {
        // Connect to the parent for the cache pipe.
        hr = StrAllocFormatted(&sczPipeName, CACHE_PIPE_NAME_FORMAT_STRING, pConnection->sczName);
        ExitOnFailure(hr, "Failed to allocate name of parent cache pipe.");

        pConnection->hCachePipe = ::CreateFileW(sczPipeName, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
        if (INVALID_HANDLE_VALUE == pConnection->hCachePipe)
        {
            ExitWithLastError1(hr, "Failed to open parent pipe: %ls", sczPipeName)
        }

        // Verify the parent and notify it that the child connected.
        hr = ChildPipeConnected(pConnection->hCachePipe, pConnection->sczSecret, &pConnection->dwProcessId);
        ExitOnFailure1(hr, "Failed to verify parent pipe: %ls", sczPipeName);
    }

    pConnection->hProcess = ::OpenProcess(SYNCHRONIZE, FALSE, pConnection->dwProcessId);
    ExitOnNullWithLastError1(pConnection->hProcess, hr, "Failed to open companion process with PID: %u", pConnection->dwProcessId);

LExit:
    ReleaseStr(sczPipeName);

    return hr;
}


static HRESULT AllocatePipeMessage(
    __in DWORD dwMessage,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in DWORD cbData,
    __out_bcount(cb) LPVOID* ppvMessage,
    __out DWORD* cbMessage
    )
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    DWORD cb = 0;

    // If no data was provided, ensure the count of bytes is zero.
    if (!pvData)
    {
        cbData = 0;
    }

    // Allocate the message.
    cb = sizeof(dwMessage) + sizeof(cbData) + cbData;
    pv = MemAlloc(cb, FALSE);
    ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate memory for message.");

    memcpy_s(pv, cb, &dwMessage, sizeof(dwMessage));
    memcpy_s(static_cast<BYTE*>(pv) + sizeof(dwMessage), cb - sizeof(dwMessage), &cbData, sizeof(cbData));
    if (cbData)
    {
        memcpy_s(static_cast<BYTE*>(pv) + sizeof(dwMessage) + sizeof(cbData), cb - sizeof(dwMessage) - sizeof(cbData), pvData, cbData);
    }

    *cbMessage = cb;
    *ppvMessage = pv;
    pv = NULL;

LExit:
    ReleaseMem(pv);
    return hr;
}

static void FreePipeMessage(
    __in BURN_PIPE_MESSAGE *pMsg
    )
{
    if (pMsg->fAllocatedData)
    {
        ReleaseNullMem(pMsg->pvData);
        pMsg->fAllocatedData = FALSE;
    }
}

static HRESULT WritePipeMessage(
    __in HANDLE hPipe,
    __in DWORD dwMessage,
    __in_bcount_opt(cbData) LPVOID pvData,
    __in DWORD cbData
    )
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    DWORD cb = 0;

    hr = AllocatePipeMessage(dwMessage, pvData, cbData, &pv, &cb);
    ExitOnFailure(hr, "Failed to allocate message to write.");

    // Write the message.
    DWORD cbWrote = 0;
    DWORD cbTotalWritten = 0;
    while (cbTotalWritten < cb)
    {
        if (!::WriteFile(hPipe, pv, cb - cbTotalWritten, &cbWrote, NULL))
        {
            ExitWithLastError(hr, "Failed to write message type to pipe.");
        }

        cbTotalWritten += cbWrote;
    }

LExit:
    ReleaseMem(pv);
    return hr;
}

static HRESULT GetPipeMessage(
    __in HANDLE hPipe,
    __in BURN_PIPE_MESSAGE* pMsg
    )
{
    HRESULT hr = S_OK;
    DWORD rgdwMessageAndByteCount[2] = { };
    DWORD cb = 0;
    DWORD cbRead = 0;

    while (cbRead < sizeof(rgdwMessageAndByteCount))
    {
        if (!::ReadFile(hPipe, reinterpret_cast<BYTE*>(rgdwMessageAndByteCount) + cbRead, sizeof(rgdwMessageAndByteCount) - cbRead, &cb, NULL))
        {
            DWORD er = ::GetLastError();
            if (ERROR_MORE_DATA == er)
            {
                hr = S_OK;
            }
            else if (ERROR_BROKEN_PIPE == er) // parent process shut down, time to exit.
            {
                memset(rgdwMessageAndByteCount, 0, sizeof(rgdwMessageAndByteCount));
                hr = S_FALSE;
                break;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(er);
            }
            ExitOnRootFailure(hr, "Failed to read message from pipe.");
        }

        cbRead += cb;
    }

    pMsg->dwMessage = rgdwMessageAndByteCount[0];
    pMsg->cbData = rgdwMessageAndByteCount[1];
    if (pMsg->cbData)
    {
        pMsg->pvData = MemAlloc(pMsg->cbData, FALSE);
        ExitOnNull(pMsg->pvData, hr, E_OUTOFMEMORY, "Failed to allocate data for message.");

        if (!::ReadFile(hPipe, pMsg->pvData, pMsg->cbData, &cb, NULL))
        {
            ExitWithLastError(hr, "Failed to read data for message.");
        }

        pMsg->fAllocatedData = TRUE;
    }

LExit:
    if (!pMsg->fAllocatedData && pMsg->pvData)
    {
        MemFree(pMsg->pvData);
    }

    return hr;
}

static HRESULT ChildPipeConnected(
    __in HANDLE hPipe,
    __in_z LPCWSTR wzSecret,
    __inout DWORD* pdwProcessId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczVerificationSecret = NULL;
    DWORD cbVerificationSecret = 0;
    DWORD dwVerificationProcessId = 0;
    DWORD dwRead = 0;
    DWORD dwAck = ::GetCurrentProcessId(); // send our process id as the ACK.
    DWORD cb = 0;

    // Read the verification secret.
    if (!::ReadFile(hPipe, &cbVerificationSecret, sizeof(cbVerificationSecret), &dwRead, NULL))
    {
        ExitWithLastError(hr, "Failed to read size of verification secret from parent pipe.");
    }

    if (255 < cbVerificationSecret / sizeof(WCHAR))
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Verification secret from parent is too big.");
    }

    hr = StrAlloc(&sczVerificationSecret, cbVerificationSecret / sizeof(WCHAR) + 1);
    ExitOnFailure(hr, "Failed to allocate buffer for verification secret.");

    if (!::ReadFile(hPipe, sczVerificationSecret, cbVerificationSecret, &dwRead, NULL))
    {
        ExitWithLastError(hr, "Failed to read verification secret from parent pipe.");
    }

    // Verify the secrets match.
    if (CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, 0, sczVerificationSecret, -1, wzSecret, -1))
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Verification secret from parent does not match.");
    }

    // Read the verification process id.
    if (!::ReadFile(hPipe, &dwVerificationProcessId, sizeof(dwVerificationProcessId), &dwRead, NULL))
    {
        ExitWithLastError(hr, "Failed to read verification process id from parent pipe.");
    }

    // If a process id was not provided, we'll trust the process id from the parent.
    if (*pdwProcessId == 0)
    {
        *pdwProcessId = dwVerificationProcessId;
    }
    else if (*pdwProcessId != dwVerificationProcessId) // verify the ids match.
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Verification process id from parent does not match.");
    }

    // All is well, tell the parent process.
    if (!::WriteFile(hPipe, &dwAck, sizeof(dwAck), &cb, NULL))
    {
        ExitWithLastError(hr, "Failed to inform parent process that child is running.");
    }

LExit:
    ReleaseStr(sczVerificationSecret);
    return hr;
}
