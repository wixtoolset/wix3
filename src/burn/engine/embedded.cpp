//-------------------------------------------------------------------------------------------------
// <copyright file="embedded.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Module: Embedded
//
//    Burn embedded process handler.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// struct

struct BURN_EMBEDDED_CALLBACK_CONTEXT
{
    PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler;
    LPVOID pvContext;
};

// internal function declarations

static HRESULT EmbeddedRunBundleHelper(
    __in LPCWSTR wzExecutablePath,
    __in LPCWSTR wzArguments,
    __in BOOL fAsync,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out_opt DWORD* pdwExitCode
    );
static HRESULT ProcessEmbeddedMessages(
    __in BURN_PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT OnEmbeddedErrorMessage(
    __in PFN_GENERICMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __in_bcount(cbData) BYTE* pbData,
    __in DWORD cbData,
    __out DWORD* pdwResult
    );
static HRESULT OnEmbeddedProgress(
    __in PFN_GENERICMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __in_bcount(cbData) BYTE* pbData,
    __in DWORD cbData,
    __out DWORD* pdwResult
    );

// function definitions

/*******************************************************************
 EmbeddedLaunchChildProcess - 

*******************************************************************/
extern "C" HRESULT EmbeddedRunBundle(
    __in LPCWSTR wzExecutablePath,
    __in LPCWSTR wzArguments,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out DWORD* pdwExitCode
    )
{
    HRESULT hr = S_OK;

    hr = EmbeddedRunBundleHelper(wzExecutablePath, wzArguments, FALSE, pfnGenericMessageHandler, pvContext, pdwExitCode);
    ExitOnFailure(hr, "Failed to run embedded bundle.");

LExit:
    return hr;
}

extern "C" HRESULT EmbeddedRunBundleAsync(
    __in LPCWSTR wzExecutablePath,
    __in LPCWSTR wzArguments,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;

    hr = EmbeddedRunBundleHelper(wzExecutablePath, wzArguments, TRUE, pfnGenericMessageHandler, pvContext, NULL);
    ExitOnFailure(hr, "Failed to run embedded bundle asynchronously.");

LExit:
    return hr;
}

static HRESULT EmbeddedRunBundleHelper(
    __in LPCWSTR wzExecutablePath,
    __in LPCWSTR wzArguments,
    __in BOOL fAsync,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out_opt DWORD* pdwExitCode
    )
{
    HRESULT hr = S_OK;
    DWORD dwCurrentProcessId = ::GetCurrentProcessId();
    HANDLE hCreatedPipesEvent = NULL;
    LPWSTR sczCommand = NULL;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };
    BURN_PIPE_RESULT result = { };

    BURN_PIPE_CONNECTION connection = { };
    PipeConnectionInitialize(&connection);

    BURN_EMBEDDED_CALLBACK_CONTEXT context = { };
    context.pfnGenericMessageHandler = pfnGenericMessageHandler;
    context.pvContext = pvContext;

    LPCWSTR wzCommandLineSwitch = fAsync ? BURN_COMMANDLINE_SWITCH_EMBEDDED_ASYNC : BURN_COMMANDLINE_SWITCH_EMBEDDED;

    hr = PipeCreateNameAndSecret(&connection.sczName, &connection.sczSecret);
    ExitOnFailure(hr, "Failed to create embedded pipe name and client token.");

    hr = PipeCreatePipes(&connection, FALSE, &hCreatedPipesEvent);
    ExitOnFailure(hr, "Failed to create embedded pipe.");

    hr = StrAllocFormatted(&sczCommand, L"%ls -%ls %ls %ls %u", wzArguments, wzCommandLineSwitch, connection.sczName, connection.sczSecret, dwCurrentProcessId);
    ExitOnFailure(hr, "Failed to allocate embedded command.");

    if (!::CreateProcessW(wzExecutablePath, sczCommand, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi))
    {
        ExitWithLastError1(hr, "Failed to create embedded process atpath: %ls", wzExecutablePath);
    }

    connection.dwProcessId = ::GetProcessId(pi.hProcess);
    connection.hProcess = pi.hProcess;
    pi.hProcess = NULL;

    hr = PipeWaitForChildConnect(&connection);
    ExitOnFailure(hr, "Failed to wait for embedded process to connect to pipe.");

    hr = PipePumpMessages(connection.hPipe, ProcessEmbeddedMessages, &context, &result);
    ExitOnFailure(hr, "Failed to process messages from embedded message.");

    if (pdwExitCode && !fAsync)
    {
        // Get the return code from the embedded process.
        hr = ProcWaitForCompletion(connection.hProcess, INFINITE, pdwExitCode);
        ExitOnFailure1(hr, "Failed to wait for embedded executable: %ls", wzExecutablePath);
    }

LExit:
    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);

    StrSecureZeroFreeString(sczCommand);
    ReleaseHandle(hCreatedPipesEvent);
    PipeConnectionUninitialize(&connection);

    return hr;
}


// internal function definitions

static HRESULT ProcessEmbeddedMessages(
    __in BURN_PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    BURN_EMBEDDED_CALLBACK_CONTEXT* pContext = static_cast<BURN_EMBEDDED_CALLBACK_CONTEXT*>(pvContext);
    DWORD dwResult = 0;

    // Process the message.
    switch (pMsg->dwMessage)
    {
    case BURN_EMBEDDED_MESSAGE_TYPE_ERROR:
        hr = OnEmbeddedErrorMessage(pContext->pfnGenericMessageHandler, pContext->pvContext, static_cast<BYTE*>(pMsg->pvData), pMsg->cbData, &dwResult);
        ExitOnFailure(hr, "Failed to process embedded error message.");
        break;

    case BURN_EMBEDDED_MESSAGE_TYPE_PROGRESS:
        hr = OnEmbeddedProgress(pContext->pfnGenericMessageHandler, pContext->pvContext, static_cast<BYTE*>(pMsg->pvData), pMsg->cbData, &dwResult);
        ExitOnFailure(hr, "Failed to process embedded progress message.");
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure1(hr, "Unexpected embedded message sent to child process, msg: %u", pMsg->dwMessage);
    }

    *pdwResult = dwResult;

LExit:
    return hr;
}

static HRESULT OnEmbeddedErrorMessage(
    __in PFN_GENERICMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __in_bcount(cbData) BYTE* pbData,
    __in DWORD cbData,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    DWORD iData = 0;
    GENERIC_EXECUTE_MESSAGE message = { };
    LPWSTR sczMessage = NULL;

    message.type = GENERIC_EXECUTE_MESSAGE_ERROR;

    hr = BuffReadNumber(pbData, cbData, &iData, &message.error.dwErrorCode);
    ExitOnFailure(hr, "Failed to read error code from buffer.");

    hr = BuffReadString(pbData, cbData, &iData, &sczMessage);
    ExitOnFailure(hr, "Failed to read error message from buffer.");

    message.error.wzMessage = sczMessage;

    hr = BuffReadNumber(pbData, cbData, &iData, &message.dwAllowedResults);
    ExitOnFailure(hr, "Failed to read UI hint from buffer.");

    *pdwResult = (DWORD)pfnMessageHandler(&message, pvContext);

LExit:
    ReleaseStr(sczMessage);

    return hr;
}

static HRESULT OnEmbeddedProgress(
    __in PFN_GENERICMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __in_bcount(cbData) BYTE* pbData,
    __in DWORD cbData,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    DWORD iData = 0;
    GENERIC_EXECUTE_MESSAGE message = { };

    message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
    message.dwAllowedResults = MB_OKCANCEL;

    hr = BuffReadNumber(pbData, cbData, &iData, &message.progress.dwPercentage);
    ExitOnFailure(hr, "Failed to read progress from buffer.");

    *pdwResult = (DWORD)pfnMessageHandler(&message, pvContext);

LExit:
    return hr;
}
