//-------------------------------------------------------------------------------------------------
// <copyright file="netfxchainer.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: NetFxChainer
//
//    Communication pipe for NetFx setup.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static VOID DestroyNetFxChainer(
    __in NetFxChainer* pChainer
    )
{
    if (pChainer)
    {
        ReleaseHandle(pChainer->hSection);
        ReleaseHandle(pChainer->hEventChaineeSend);
        ReleaseHandle(pChainer->hEventChainerSend);
        ReleaseHandle(pChainer->hMutex);

        if (pChainer->pData)
        {
            ::UnmapViewOfFile(pChainer->pData);
        }

        MemFree(pChainer);
    }
}

static HRESULT CreateNetFxChainer(
    __in LPCWSTR wzSectionName, 
    __in LPCWSTR wzEventName, 
    __out NetFxChainer** ppChainer
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczName = NULL;
    NetFxChainer* pChainer = NULL;

    pChainer = (NetFxChainer*)MemAlloc(sizeof(NetFxChainer), TRUE);
    ExitOnNull(pChainer, hr, E_OUTOFMEMORY, "Failed to allocate memory for NetFxChainer struct.");

    pChainer->hEventChaineeSend = ::CreateEvent(NULL, FALSE, FALSE, wzEventName);
    ExitOnNullWithLastError1(pChainer->hEventChaineeSend, hr, "Failed to create event: %ls", wzEventName);

    hr = StrAllocFormatted(&sczName, L"%ls_send", wzEventName);
    ExitOnFailure(hr, "failed to allocate memory for event name");

    pChainer->hEventChainerSend = ::CreateEvent(NULL, FALSE, FALSE, sczName);
    ExitOnNullWithLastError1(pChainer->hEventChainerSend, hr, "Failed to create event: %ls", sczName);

    hr = StrAllocFormatted(&sczName, L"%ls_mutex", wzEventName);
    ExitOnFailure(hr, "failed to allocate memory for mutex name");

    // Create the mutex, we initially own
    pChainer->hMutex = ::CreateMutex(NULL, TRUE, sczName);
    ExitOnNullWithLastError1(pChainer->hMutex, hr, "Failed to create mutex: %ls", sczName);

    pChainer->hSection = ::CreateFileMapping(INVALID_HANDLE_VALUE,
                                   NULL, // security attributes
                                   PAGE_READWRITE,
                                   0, // high-order DWORD of maximum size
                                   NETFXDATA_SIZE, // low-order DWORD of maximum size
                                   wzSectionName);
    ExitOnNullWithLastError1(pChainer->hSection, hr, "Failed to memory map cabinet file: %ls", wzSectionName);

    pChainer->pData = reinterpret_cast<NetFxDataStructure*>(::MapViewOfFile(pChainer->hSection,
                                                                          FILE_MAP_WRITE,
                                                                          0, 0, // offsets
                                                                          0 // map entire file
                                                                          ));
    ExitOnNullWithLastError1(pChainer->pData, hr, "Failed to MapViewOfFile for %ls.", wzSectionName);

    // Initialize the shared memory
    hr = ::StringCchCopyW(pChainer->pData->szEventName, countof(pChainer->pData->szEventName), wzEventName);
    ExitOnFailure(hr, "failed to copy event name to shared memory structure.");
    pChainer->pData->downloadFinished = false;
    pChainer->pData->downloadSoFar = 0;
    pChainer->pData->hrDownloadFinished = E_PENDING;
    pChainer->pData->downloadAbort = false;
    pChainer->pData->installFinished = false;
    pChainer->pData->installSoFar = 0;
    pChainer->pData->hrInstallFinished = E_PENDING;
    pChainer->pData->installAbort = false;
    pChainer->pData->hrInternalError = S_OK;
    pChainer->pData->version = NETFXDATA_VERSION;
    pChainer->pData->messageCode = 0;
    pChainer->pData->messageResponse = 0;
    pChainer->pData->messageDataLength = 0;

    // Done with initialization, allow others to access.
    ::ReleaseMutex(pChainer->hMutex);

    *ppChainer = pChainer;
    pChainer = NULL;

LExit:
    ReleaseStr(sczName);

    if (pChainer)
    {
        // Something failed, release the mutex and destroy the object
        if (pChainer->hMutex)
        {
            ::ReleaseMutex(pChainer->hMutex);
        }

        DestroyNetFxChainer(pChainer);
    }

    return  hr;
}


static VOID NetFxAbort(
    __in NetFxChainer* pChainer
    )
{
    ::WaitForSingleObject(pChainer->hMutex, INFINITE);

    pChainer->pData->downloadAbort = true;
    pChainer->pData->installAbort = true;

    ::ReleaseMutex(pChainer->hMutex);

    ::SetEvent(pChainer->hEventChainerSend);
}

static BYTE NetFxGetProgress(
    __in NetFxChainer* pChainer
    )
{
    BYTE bProgress = 0;
    ::WaitForSingleObject(pChainer->hMutex, INFINITE);

    bProgress = (pChainer->pData->installSoFar + pChainer->pData->downloadSoFar) / 2;

    ::ReleaseMutex(pChainer->hMutex);

    return bProgress;
}

static HRESULT NetFxGetMessage(
    __in NetFxChainer* pChainer,
    __out DWORD* pdwMessage, 
    __out LPVOID* ppBuffer, 
    __out DWORD* pdwBufferSize
    )
{
    HRESULT hr = S_OK;
    ::WaitForSingleObject(pChainer->hMutex, INFINITE);

    *pdwMessage = pChainer->pData->messageCode;
    *ppBuffer = NULL;
    *pdwBufferSize = 0;

    if (NETFX_NO_MESSAGE != *pdwMessage)
    {
        *ppBuffer = MemAlloc(pChainer->pData->messageDataLength, TRUE);
        ExitOnNull(*ppBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for message data");

        memcpy(*ppBuffer, pChainer->pData->messageData, pChainer->pData->messageDataLength);
        *pdwBufferSize = pChainer->pData->messageDataLength;
    }

LExit:
    ::ReleaseMutex(pChainer->hMutex);

    return hr;
}

static void NetFxRespond(
    __in NetFxChainer* pChainer,
    __in DWORD dwResponse
    )
{
    ::WaitForSingleObject(pChainer->hMutex, INFINITE);

    pChainer->pData->messageCode = NETFX_NO_MESSAGE;
    pChainer->pData->messageResponse = dwResponse;
    if (IDCANCEL == dwResponse)
    {
        pChainer->pData->downloadAbort = true;
        pChainer->pData->installAbort = true;
    }

    ::ReleaseMutex(pChainer->hMutex);

    ::SetEvent(pChainer->hEventChainerSend);
}

static HRESULT NetFxGetResult(
    __in NetFxChainer* pChainer,
    __out HRESULT* phrInternalError
    )
{
    HRESULT hr = S_OK;
    ::WaitForSingleObject(pChainer->hMutex, INFINITE);

    hr = pChainer->pData->hrInstallFinished;

    if (FAILED(pChainer->pData->hrDownloadFinished) && // Download failed
       (S_OK == hr || E_ABORT == hr))                  // Install succeeded or was aborted
    {
        hr = pChainer->pData->hrDownloadFinished;
    }

    if (phrInternalError)
    {
        *phrInternalError = pChainer->pData->hrInternalError;
    }

    ::ReleaseMutex(pChainer->hMutex);

    return hr;
}

static HRESULT OnNetFxFilesInUse(
    __in NetFxChainer* pNetfxChainer,
    __in NetFxCloseApplications* pCloseApps,
    __in PFN_GENERICMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    DWORD cFiles = 0;
    LPWSTR* rgwzFiles = NULL;
    GENERIC_EXECUTE_MESSAGE message = { };
    DWORD dwResponse = 0;

    cFiles = pCloseApps->dwApplicationsSize;
    rgwzFiles = (LPWSTR*)MemAlloc(sizeof(LPWSTR*) * cFiles, TRUE);
    ExitOnNull(rgwzFiles, hr, E_OUTOFMEMORY, "Failed to allocate buffer.");

    for (DWORD i = 0; i < pCloseApps->dwApplicationsSize; ++i)
    {
        rgwzFiles[i] = pCloseApps->applications[i].szName;
    }

    // send message
    message.type = GENERIC_EXECUTE_MESSAGE_FILES_IN_USE;
    message.dwAllowedResults = MB_ABORTRETRYIGNORE;
    message.filesInUse.cFiles = cFiles;
    message.filesInUse.rgwzFiles = (LPCWSTR*)rgwzFiles;
    dwResponse = (DWORD)pfnMessageHandler(&message, pvContext);

    NetFxRespond(pNetfxChainer, dwResponse);

LExit:
    ReleaseMem(rgwzFiles);

    return hr;
}

static HRESULT OnNetFxProgress(
    __in NetFxChainer* pNetfxChainer,
    __in BYTE bProgress,
    __in PFN_GENERICMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext
    )
{
    GENERIC_EXECUTE_MESSAGE message = { };
    DWORD dwResponse = 0;

    // send message
    message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
    message.dwAllowedResults = MB_OKCANCEL;
    message.progress.dwPercentage = 100 * (DWORD)bProgress / BYTE_MAX;
    dwResponse = (DWORD)pfnMessageHandler(&message, pvContext);

    if (IDCANCEL == dwResponse)
    {
        NetFxAbort(pNetfxChainer);
    }

    return S_OK;
}

static HRESULT OnNetFxError(
    __in NetFxChainer* /*pNetfxChainer*/,
    __in HRESULT hrError,
    __in PFN_GENERICMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext
    )
{
    GENERIC_EXECUTE_MESSAGE message = { };
    DWORD dwResponse = 0;

    // send message
    message.type = GENERIC_EXECUTE_MESSAGE_ERROR;
    message.dwAllowedResults = MB_OK;
    message.error.dwErrorCode = hrError;
    message.error.wzMessage = NULL;
    dwResponse = (DWORD)pfnMessageHandler(&message, pvContext);

    return S_OK;
}

static HRESULT ProcessNetFxMessage(
    __in NetFxChainer* pNetfxChainer,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    DWORD dwMessage = NETFX_NO_MESSAGE;
    DWORD dwBufferSize = 0;
    LPVOID pBuffer = NULL;

    // send progress
    hr = OnNetFxProgress(pNetfxChainer, NetFxGetProgress(pNetfxChainer), pfnGenericMessageHandler, pvContext);
    ExitOnFailure(hr, "Failed to send progress from netfx chainer.");

    // Check for message
    hr = NetFxGetMessage(pNetfxChainer, &dwMessage, &pBuffer, &dwBufferSize);
    ExitOnFailure(hr, "Failed to get message from netfx chainer.");

    switch(dwMessage)
    {
    case NETFX_CLOSE_APPS:
        hr = OnNetFxFilesInUse(pNetfxChainer, (NetFxCloseApplications*)pBuffer, pfnGenericMessageHandler, pvContext);
        ExitOnFailure(hr, "Failed to send files in use message from netfx chainer.");
        break;

    default:
        // No message we understand.
        break;
    }

LExit:
    ReleaseMem(pBuffer);

    return hr;
}

extern "C" HRESULT NetFxRunChainer(
    __in LPCWSTR wzExecutablePath,
    __in LPCWSTR wzArguments,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out DWORD* pdwExitCode
    )
{
    HRESULT hr = S_OK;
    DWORD er = 0;
    UUID guid = { };
    WCHAR wzGuid[39];
    RPC_STATUS rs = RPC_S_OK;
    LPWSTR sczEventName = NULL;
    LPWSTR sczSectionName = NULL;
    LPWSTR sczCommand = NULL;
    NetFxChainer* pNetfxChainer = NULL;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };
    HRESULT hrInternalError = 0;

    // Create the unique name suffix.
    rs = ::UuidCreate(&guid);
    hr = HRESULT_FROM_RPC(rs);
    ExitOnFailure(hr, "Failed to create netfx chainer guid.");

    if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
    {
        hr = E_OUTOFMEMORY;
        ExitOnRootFailure(hr, "Failed to convert netfx chainer guid into string.");
    }

    hr = StrAllocFormatted(&sczSectionName, L"NetFxSection.%ls", wzGuid);
    ExitOnFailure(hr, "Failed to allocate section name.");

    hr = StrAllocFormatted(&sczEventName, L"NetFxEvent.%ls", wzGuid);
    ExitOnFailure(hr, "Failed to allocate event name.");

    hr = CreateNetFxChainer(sczSectionName, sczEventName, &pNetfxChainer);
    ExitOnFailure(hr, "Failed to create netfx chainer.");

	hr = StrAllocateFormatted(&sczCommand, TRUE, L"%ls /pipe %ls", wzArguments, sczSectionName);
    ExitOnFailure(hr, "Failed to allocate netfx chainer arguments.");

    si.cb = sizeof(si);
    if (!::CreateProcessW(wzExecutablePath, sczCommand, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi))
    {
        ExitWithLastError1(hr, "Failed to CreateProcess on path: %ls", wzExecutablePath);
    }

    HANDLE handles[2] = { pi.hProcess, pNetfxChainer->hEventChaineeSend };

    for (;;)
    {
        er = ::WaitForMultipleObjects(2, handles, FALSE, 100);
        if (WAIT_OBJECT_0 == er)
        {
            // Process has exited
            *pdwExitCode = NetFxGetResult(pNetfxChainer, &hrInternalError);
            if (E_PENDING == *pdwExitCode)
            {
                if (!::GetExitCodeProcess(pi.hProcess, pdwExitCode))
                {
                    ExitWithLastError(hr, "Failed to get netfx return code.");
                }
            }
            else if (FAILED(hrInternalError))
            {
                // push internal error message
                OnNetFxError(pNetfxChainer, hrInternalError, pfnGenericMessageHandler, pvContext);
                ExitOnFailure(hr, "Failed to send internal error message from netfx chainer.");
            }           

            break;
        }
        else if (WAIT_OBJECT_0 + 1 == er)
        {
            // Chainee has notified us of a change.
            hr = ProcessNetFxMessage(pNetfxChainer, pfnGenericMessageHandler, pvContext);
            ExitOnFailure(hr, "Failed to process netfx chainer message.");
        }
        else if (WAIT_FAILED == er)
        {
            ExitWithLastError(hr, "Failed to wait for netfx chainer process to complete");
        }
    }

LExit:
    ReleaseStr(sczSectionName);
    ReleaseStr(sczEventName);
    StrSecureZeroFreeString(sczCommand);
    DestroyNetFxChainer(pNetfxChainer);
    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);

    return hr;
}
