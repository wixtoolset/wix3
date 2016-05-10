// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#include <fdi.h>

#define ARRAY_GROWTH_SIZE 2

const LPSTR INVALID_CAB_NAME = "<the>.cab";

// structs

typedef struct _BURN_CAB_CONTEXT
{
    HANDLE hFile;
    DWORD64 qwOffset;
    DWORD64 qwSize;

    HANDLE hThread;
    HANDLE hBeginOperationEvent;
    HANDLE hOperationCompleteEvent;

    BURN_CAB_OPERATION operation;
    HRESULT hrError;

    LPWSTR* psczStreamName;
    LPCWSTR wzTargetFile;
    HANDLE hTargetFile;
    BYTE* pbTargetBuffer;
    DWORD cbTargetBuffer;
    DWORD iTargetBuffer;
} BURN_CAB_CONTEXT;


// internal function declarations

static HRESULT BeginAndWaitForOperation(
    __in BURN_CONTAINER_CONTEXT* pContext
    );
static HRESULT WaitForOperation(
    __in BURN_CONTAINER_CONTEXT* pContext
    );
static DWORD WINAPI ExtractThreadProc(
    __in LPVOID lpThreadParameter
    );
static INT_PTR DIAMONDAPI CabNotifyCallback(
    __in FDINOTIFICATIONTYPE iNotification,
    __inout FDINOTIFICATION *pFDINotify
    );
static INT_PTR CopyFileCallback(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout FDINOTIFICATION *pFDINotify
    );
static INT_PTR CloseFileInfoCallback(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout FDINOTIFICATION *pFDINotify
    );
static LPVOID DIAMONDAPI CabAlloc(
    __in DWORD dwSize
    );
static void DIAMONDAPI CabFree(
    __in LPVOID pvData
    );
static INT_PTR FAR DIAMONDAPI CabOpen(
    __in char FAR *pszFile,
    __in int /* oflag */,
    __in int /* pmode */
    );
static UINT FAR DIAMONDAPI CabRead(
    __in INT_PTR hf,
    __out void FAR *pv,
    __in UINT cb
    );
static UINT FAR DIAMONDAPI CabWrite(
    __in INT_PTR hf,
    __in void FAR *pv,
    __in UINT cb
    );
static long FAR DIAMONDAPI CabSeek(
    __in INT_PTR hf,
    __in long dist,
    __in int seektype
    );
static int FAR DIAMONDAPI CabClose(
    __in INT_PTR hf
    );
static HRESULT AddVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile,
    __in LONGLONG llInitialFilePointer
    );
static HRESULT ReadIfVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile,
    __in DWORD cbRead
    );
static BOOL SetIfVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile,
    __in LONGLONG llDistance,
    __out LONGLONG* pllNewPostion,
    __in DWORD dwSeekType
    );
static HRESULT CloseIfVirturalFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile
    );
static BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* GetVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile
    );


// internal variables

__declspec(thread) static BURN_CONTAINER_CONTEXT* vpContext;


// function definitions

extern "C" void CabExtractInitialize()
{
}

extern "C" HRESULT CabExtractOpen(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in LPCWSTR wzFilePath
    )
{
    HRESULT hr = S_OK;

    // initialize context
    pContext->Cabinet.hTargetFile = INVALID_HANDLE_VALUE;

    hr = StrAllocString(&pContext->Cabinet.sczFile, wzFilePath, 0);
    ExitOnFailure(hr, "Failed to copy file name.");

    // create events
    pContext->Cabinet.hBeginOperationEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(pContext->Cabinet.hBeginOperationEvent, hr, "Failed to create begin operation event.");

    pContext->Cabinet.hOperationCompleteEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(pContext->Cabinet.hOperationCompleteEvent, hr, "Failed to create operation complete event.");

    // create extraction thread
    pContext->Cabinet.hThread = ::CreateThread(NULL, 0, ExtractThreadProc, pContext, 0, NULL);
    ExitOnNullWithLastError(pContext->Cabinet.hThread, hr, "Failed to create extraction thread.");

    // wait for operation to complete
    hr = WaitForOperation(pContext);
    ExitOnFailure(hr, "Failed to wait for operation complete.");

LExit:
    return hr;
}

extern "C" HRESULT CabExtractNextStream(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout_z LPWSTR* psczStreamName
    )
{
    HRESULT hr = S_OK;

    // set operation to move to next stream
    pContext->Cabinet.operation = BURN_CAB_OPERATION_NEXT_STREAM;
    pContext->Cabinet.psczStreamName = psczStreamName;

    // begin operation and wait
    hr = BeginAndWaitForOperation(pContext);
    if (E_ABORT != hr && E_NOMOREITEMS != hr)
    {
        ExitOnFailure(hr, "Failed to begin and wait for operation.");
    }

LExit:
    return hr;
}

extern "C" HRESULT CabExtractStreamToFile(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    )
{
    HRESULT hr = S_OK;

    // set operation to move to next stream
    pContext->Cabinet.operation = BURN_CAB_OPERATION_STREAM_TO_FILE;
    pContext->Cabinet.wzTargetFile = wzFileName;

    // begin operation and wait
    hr = BeginAndWaitForOperation(pContext);
    ExitOnFailure(hr, "Failed to begin and wait for operation.");

    // clear file name
    pContext->Cabinet.wzTargetFile = NULL;

LExit:
    return hr;
}

extern "C" HRESULT CabExtractStreamToBuffer(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __out BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    )
{
    HRESULT hr = S_OK;

    // set operation to move to next stream
    pContext->Cabinet.operation = BURN_CAB_OPERATION_STREAM_TO_BUFFER;

    // begin operation and wait
    hr = BeginAndWaitForOperation(pContext);
    ExitOnFailure(hr, "Failed to begin and wait for operation.");

    // return values
    *ppbBuffer = pContext->Cabinet.pbTargetBuffer;
    *pcbBuffer = pContext->Cabinet.cbTargetBuffer;

    // clear buffer variables
    pContext->Cabinet.pbTargetBuffer = NULL;
    pContext->Cabinet.cbTargetBuffer = 0;
    pContext->Cabinet.iTargetBuffer = 0;

LExit:
    return hr;
}

extern "C" HRESULT CabExtractSkipStream(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    // set operation to move to next stream
    pContext->Cabinet.operation = BURN_CAB_OPERATION_SKIP_STREAM;

    // begin operation and wait
    hr = BeginAndWaitForOperation(pContext);
    ExitOnFailure(hr, "Failed to begin and wait for operation.");

LExit:
    return hr;
}

extern "C" HRESULT CabExtractClose(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    // terminate worker thread
    if (pContext->Cabinet.hThread)
    {
        // set operation to move to close
        pContext->Cabinet.operation = BURN_CAB_OPERATION_CLOSE;

        // set begin operation event
        if (!::SetEvent(pContext->Cabinet.hBeginOperationEvent))
        {
            ExitWithLastError(hr, "Failed to set begin operation event.");
        }

        // wait for thread to terminate
        if (WAIT_OBJECT_0 != ::WaitForSingleObject(pContext->Cabinet.hThread, INFINITE))
        {
            ExitWithLastError(hr, "Failed to wait for thread to terminate.");
        }
    }

LExit:
    ReleaseHandle(pContext->Cabinet.hThread);
    ReleaseHandle(pContext->Cabinet.hBeginOperationEvent);
    ReleaseHandle(pContext->Cabinet.hOperationCompleteEvent);
    ReleaseMem(pContext->Cabinet.rgVirtualFilePointers);
    ReleaseStr(pContext->Cabinet.sczFile);

    return hr;
}


// internal helper functions

static HRESULT BeginAndWaitForOperation(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    // set begin operation event
    if (!::SetEvent(pContext->Cabinet.hBeginOperationEvent))
    {
        ExitWithLastError(hr, "Failed to set begin operation event.");
    }

    // wait for operation to complete
    hr = WaitForOperation(pContext);

LExit:
    return hr;
}

static HRESULT WaitForOperation(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;
    HANDLE rghWait[2] = { };

    // wait for operation complete event
    rghWait[0] = pContext->Cabinet.hOperationCompleteEvent;
    rghWait[1] = pContext->Cabinet.hThread;
    switch (::WaitForMultipleObjects(countof(rghWait), rghWait, FALSE, INFINITE))
    {
    case WAIT_OBJECT_0:
        if (!::ResetEvent(pContext->Cabinet.hOperationCompleteEvent))
        {
            ExitWithLastError(hr, "Failed to reset operation complete event.");
        }
        break;

    case WAIT_OBJECT_0 + 1:
        if (!::GetExitCodeThread(pContext->Cabinet.hThread, (DWORD*)&hr))
        {
            ExitWithLastError(hr, "Failed to get extraction thread exit code.");
        }
        ExitFunction();

    case WAIT_FAILED: __fallthrough;
    default:
        ExitWithLastError(hr, "Failed to wait for operation complete event.");
    }

    // clear operation
    pContext->Cabinet.operation = BURN_CAB_OPERATION_NONE;

LExit:
    return hr;
}

static DWORD WINAPI ExtractThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER_CONTEXT* pContext = (BURN_CONTAINER_CONTEXT*)lpThreadParameter;
    BOOL fComInitialized = FALSE;
    HFDI hfdi = NULL;
    ERF erf = { };

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    // save context in TLS storage
    vpContext = pContext;

    // create FDI context
    hfdi = ::FDICreate(CabAlloc, CabFree, CabOpen, CabRead, CabWrite, CabClose, CabSeek, cpuUNKNOWN, &erf);
    ExitOnNull(hfdi, hr, E_FAIL, "Failed to initialize cabinet.dll.");

    // begin CAB extraction
    if (!::FDICopy(hfdi, INVALID_CAB_NAME, "", 0, CabNotifyCallback, NULL, NULL))
    {
        hr = pContext->Cabinet.hrError;
        if (E_ABORT == hr || E_NOMOREITEMS == hr)
        {
            ExitFunction();
        }
        else if (SUCCEEDED(hr))
        {
            if (ERROR_SUCCESS != erf.erfType)
            {
                hr = HRESULT_FROM_WIN32(erf.erfType);
            }
            else
            {
                switch (erf.erfOper)
                {
                case FDIERROR_NONE:
                    hr = E_UNEXPECTED;
                    break;
                case FDIERROR_CABINET_NOT_FOUND:
                    hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                    break;
                case FDIERROR_NOT_A_CABINET:
                    hr = HRESULT_FROM_WIN32(ERROR_INVALID_FUNCTION);
                    break;
                case FDIERROR_UNKNOWN_CABINET_VERSION:
                    hr = HRESULT_FROM_WIN32(ERROR_VERSION_PARSE_ERROR);
                    break;
                case FDIERROR_CORRUPT_CABINET:
                    hr = HRESULT_FROM_WIN32(ERROR_FILE_CORRUPT);
                    break;
                case FDIERROR_ALLOC_FAIL:
                    hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
                    break;
                case FDIERROR_BAD_COMPR_TYPE:
                    hr = HRESULT_FROM_WIN32(ERROR_UNSUPPORTED_COMPRESSION);
                    break;
                case FDIERROR_MDI_FAIL:
                    hr = HRESULT_FROM_WIN32(ERROR_BAD_COMPRESSION_BUFFER);
                    break;
                case FDIERROR_TARGET_FILE:
                    hr = HRESULT_FROM_WIN32(ERROR_WRITE_FAULT);
                    break;
                case FDIERROR_RESERVE_MISMATCH:
                    hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                    break;
                case FDIERROR_WRONG_CABINET:
                    hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
                    break;
                case FDIERROR_USER_ABORT:
                    hr = E_ABORT;
                    break;
                default:
                    hr = E_FAIL;
                    break;
                }
            }
        }
        ExitOnFailure3(hr, "Failed to extract all files from container, erf: %d:%X:%d", erf.fError, erf.erfOper, erf.erfType);
    }

    // set operation complete event
    if (!::SetEvent(pContext->Cabinet.hOperationCompleteEvent))
    {
        ExitWithLastError(hr, "Failed to set operation complete event.");
    }

    // wait for begin operation event
    if (WAIT_FAILED == ::WaitForSingleObject(pContext->Cabinet.hBeginOperationEvent, INFINITE))
    {
        ExitWithLastError(hr, "Failed to wait for begin operation event.");
    }

    if (!::ResetEvent(pContext->Cabinet.hBeginOperationEvent))
    {
        ExitWithLastError(hr, "Failed to reset begin operation event.");
    }

    // read operation
    switch (pContext->Cabinet.operation)
    {
    case BURN_CAB_OPERATION_NEXT_STREAM:
        ExitFunction1(hr = E_NOMOREITEMS);
        break;

    case BURN_CAB_OPERATION_CLOSE:
        ExitFunction1(hr = S_OK);

    default:
        hr = E_INVALIDSTATE;
        ExitOnRootFailure(hr, "Invalid operation for this state.");
    }

LExit:
    if (hfdi)
    {
        ::FDIDestroy(hfdi);
    }
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return (DWORD)hr;
}

static INT_PTR DIAMONDAPI CabNotifyCallback(
    __in FDINOTIFICATIONTYPE iNotification,
    __inout FDINOTIFICATION *pFDINotify
    )
{
    BURN_CONTAINER_CONTEXT* pContext = vpContext;
    INT_PTR ipResult = 0; // result to return on success

    switch (iNotification)
    {
    case fdintCOPY_FILE:
        ipResult = CopyFileCallback(pContext, pFDINotify);
        break;

    case fdintCLOSE_FILE_INFO: // resource extraction complete
        ipResult = CloseFileInfoCallback(pContext, pFDINotify);
        break;

    case fdintPARTIAL_FILE: __fallthrough; // no action needed for these messages
    case fdintNEXT_CABINET: __fallthrough;
    case fdintENUMERATE: __fallthrough;
    case fdintCABINET_INFO:
        break;

    default:
        AssertSz(FALSE, "CabExtractCallback() - unknown FDI notification command");
    };

//LExit:
    return ipResult;
}

static INT_PTR CopyFileCallback(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout FDINOTIFICATION* pFDINotify
    )
{
    HRESULT hr = S_OK;
    INT_PTR ipResult = 1; // result to return on success
    LPWSTR pwzPath = NULL;
    LARGE_INTEGER li = { };

    // set operation complete event
    if (!::SetEvent(pContext->Cabinet.hOperationCompleteEvent))
    {
        ExitWithLastError(hr, "Failed to set operation complete event.");
    }

    // wait for begin operation event
    if (WAIT_FAILED == ::WaitForSingleObject(pContext->Cabinet.hBeginOperationEvent, INFINITE))
    {
        ExitWithLastError(hr, "Failed to wait for begin operation event.");
    }

    if (!::ResetEvent(pContext->Cabinet.hBeginOperationEvent))
    {
        ExitWithLastError(hr, "Failed to reset begin operation event.");
    }

    // read operation
    switch (pContext->Cabinet.operation)
    {
    case BURN_CAB_OPERATION_NEXT_STREAM:
        break;

    case BURN_CAB_OPERATION_CLOSE:
        ExitFunction1(hr = E_ABORT);

    default:
        hr = E_INVALIDSTATE;
        ExitOnRootFailure(hr, "Invalid operation for this state.");
    }

    // copy stream name
    hr = StrAllocStringAnsi(pContext->Cabinet.psczStreamName, pFDINotify->psz1, 0, CP_UTF8);
    ExitOnFailure1(hr, "Failed to copy stream name: %ls", pFDINotify->psz1);

    // set operation complete event
    if (!::SetEvent(pContext->Cabinet.hOperationCompleteEvent))
    {
        ExitWithLastError(hr, "Failed to set operation complete event.");
    }

    // wait for begin operation event
    if (WAIT_FAILED == ::WaitForSingleObject(pContext->Cabinet.hBeginOperationEvent, INFINITE))
    {
        ExitWithLastError(hr, "Failed to wait for begin operation event.");
    }

    if (!::ResetEvent(pContext->Cabinet.hBeginOperationEvent))
    {
        ExitWithLastError(hr, "Failed to reset begin operation event.");
    }

    // read operation
    switch (pContext->Cabinet.operation)
    {
    case BURN_CAB_OPERATION_STREAM_TO_FILE:
        // create file
        pContext->Cabinet.hTargetFile = ::CreateFileW(pContext->Cabinet.wzTargetFile, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == pContext->Cabinet.hTargetFile)
        {
            ExitWithLastError1(hr, "Failed to create file: %ls", pContext->Cabinet.wzTargetFile);
        }

        // set file size
        li.QuadPart = pFDINotify->cb;
        if (!::SetFilePointerEx(pContext->Cabinet.hTargetFile, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to set file pointer to end of file.");
        }

        if (!::SetEndOfFile(pContext->Cabinet.hTargetFile))
        {
            ExitWithLastError(hr, "Failed to set end of file.");
        }

        li.QuadPart = 0;
        if (!::SetFilePointerEx(pContext->Cabinet.hTargetFile, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to set file pointer to beginning of file.");
        }

        break;

    case BURN_CAB_OPERATION_STREAM_TO_BUFFER:
        // allocate buffer for stream
        pContext->Cabinet.pbTargetBuffer = (BYTE*)MemAlloc(pFDINotify->cb, TRUE);
        ExitOnNull(pContext->Cabinet.pbTargetBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer for stream.");

        // set buffer size and write position
        pContext->Cabinet.cbTargetBuffer = pFDINotify->cb;
        pContext->Cabinet.iTargetBuffer = 0;

        break;

    case BURN_CAB_OPERATION_SKIP_STREAM:
        ipResult = 0;
        break;

    case BURN_CAB_OPERATION_CLOSE:
        ExitFunction1(hr = E_ABORT);

    default:
        hr = E_INVALIDSTATE;
        ExitOnRootFailure(hr, "Invalid operation for this state.");
    }

LExit:
    ReleaseStr(pwzPath);

    pContext->Cabinet.hrError = hr;
    return SUCCEEDED(hr) ? ipResult : -1;
}

static INT_PTR CloseFileInfoCallback(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout FDINOTIFICATION *pFDINotify
    )
{
    HRESULT hr = S_OK;
    INT_PTR ipResult = 1; // result to return on success
    FILETIME ftLocal = { };
    FILETIME ft = { };

    // read operation
    switch (pContext->Cabinet.operation)
    {
    case BURN_CAB_OPERATION_STREAM_TO_FILE:
        // Make a best effort to set the time on the new file before
        // we close it.
        if (::DosDateTimeToFileTime(pFDINotify->date, pFDINotify->time, &ftLocal))
        {
            if (::LocalFileTimeToFileTime(&ftLocal, &ft))
            {
                ::SetFileTime(pContext->Cabinet.hTargetFile, &ft, &ft, &ft);
            }
        }

        // close file
        ReleaseFile(pContext->Cabinet.hTargetFile);
        break;

    case BURN_CAB_OPERATION_STREAM_TO_BUFFER:
        break;

    case BURN_CAB_OPERATION_CLOSE:
        ExitFunction1(hr = E_ABORT);

    default:
        hr = E_INVALIDSTATE;
        ExitOnRootFailure(hr, "Invalid operation for this state.");
    }

    //if (pContext->pfnProgress)
    //{
    //    hr = StrAllocFormatted(&pwzPath, L"%s%ls", pContext->wzRootPath, pFDINotify->psz1);
    //    ExitOnFailure2(hr, "Failed to calculate file path from: %ls and %s", pContext->wzRootPath, pFDINotify->psz1);
    //    if (SUCCEEDED(hr))
    //    {
    //        hr = pContext->pfnProgress(BOX_PROGRESS_DECOMPRESSION_END, pwzPath, 0, pContext->pvContext);
    //        if (S_OK != hr)
    //        {
    //            pContext->hrError = hr;
    //            ExitFunction();
    //        }
    //    }
    //}

LExit:
    pContext->Cabinet.hrError = hr;
    return SUCCEEDED(hr) ? ipResult : -1;
}

static LPVOID DIAMONDAPI CabAlloc(
    __in DWORD dwSize
    )
{
    return MemAlloc(dwSize, FALSE);
}

static void DIAMONDAPI CabFree(
    __in LPVOID pvData
    )
{
    MemFree(pvData);
}

static INT_PTR FAR DIAMONDAPI CabOpen(
    __in char FAR * pszFile,
    __in int /* oflag */,
    __in int /* pmode */
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER_CONTEXT* pContext = vpContext;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // If this is the invalid cab name, use our file handle.
    if (CSTR_EQUAL == ::CompareStringA(LOCALE_NEUTRAL, 0, INVALID_CAB_NAME, -1, pszFile, -1))
    {
        if (!::DuplicateHandle(::GetCurrentProcess(), pContext->hFile, ::GetCurrentProcess(), &hFile, 0, FALSE, DUPLICATE_SAME_ACCESS))
        {
            ExitWithLastError(hr, "Failed to duplicate handle to cab container.");
        }

        // Use a virtual file pointer since duplicated file handles share their file pointer. Seek to container offset
        // to start.
        hr = AddVirtualFilePointer(&pContext->Cabinet, hFile, pContext->qwOffset);
        ExitOnFailure(hr, "Failed to add virtual file pointer for cab container.");
    }
    else // open file requested. This is used in the rare cases where the CAB API wants to create a temp file.
    {
        hFile = ::CreateFileA(pszFile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        ExitOnInvalidHandleWithLastError1(hFile, hr, "Failed to open cabinet file: %hs", pszFile);
    }

LExit:
    pContext->Cabinet.hrError = hr;
    return FAILED(hr) ? -1 : (INT_PTR)hFile;
}

static UINT FAR DIAMONDAPI CabRead(
    __in INT_PTR hf,
    __out void FAR *pv,
    __in UINT cb
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER_CONTEXT* pContext = vpContext;
    HANDLE hFile = (HANDLE)hf;
    DWORD cbRead = 0;

    ReadIfVirtualFilePointer(&pContext->Cabinet, hFile, cb);

    if (!::ReadFile(hFile, pv, cb, &cbRead, NULL))
    {
        ExitWithLastError(hr, "Failed to read during cabinet extraction.");
    }

LExit:
    pContext->Cabinet.hrError = hr;
    return FAILED(hr) ? -1 : cbRead;
}

static UINT FAR DIAMONDAPI CabWrite(
    __in INT_PTR /* hf */,
    __in void FAR *pv,
    __in UINT cb
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER_CONTEXT* pContext = vpContext;
    DWORD cbWrite = 0;

    switch (pContext->Cabinet.operation)
    {
    case BURN_CAB_OPERATION_STREAM_TO_FILE:
        // write file
        if (!::WriteFile(pContext->Cabinet.hTargetFile, pv, cb, &cbWrite, NULL))
        {
            ExitWithLastError(hr, "Failed to write during cabinet extraction.");
        }
        break;

    case BURN_CAB_OPERATION_STREAM_TO_BUFFER:
        // copy to target buffer
        memcpy_s(pContext->Cabinet.pbTargetBuffer + pContext->Cabinet.iTargetBuffer, pContext->Cabinet.cbTargetBuffer - pContext->Cabinet.iTargetBuffer, pv, cb);
        pContext->Cabinet.iTargetBuffer += cb;

        cbWrite = cb;
        break;

    default:
        hr = E_INVALIDSTATE;
        ExitOnFailure(hr, "Unexpected call to CabWrite().");
    }

LExit:
    pContext->Cabinet.hrError = hr;
    return FAILED(hr) ? -1 : cbWrite;
}

static long FAR DIAMONDAPI CabSeek(
    __in INT_PTR hf,
    __in long dist,
    __in int seektype
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER_CONTEXT* pContext = vpContext;
    HANDLE hFile = (HANDLE)hf;
    LARGE_INTEGER liDistance = { };
    LARGE_INTEGER liNewPointer = { };
    DWORD dwSeekType = 0;

    // We assume that CabSeek() will only be called to seek the
    // cabinet itself so we have to offset the seek operations to
    // where the internal cabinet starts.
    switch (seektype)
    {
    case FILE_BEGIN:
        liDistance.QuadPart = pContext->qwOffset + dist;
        dwSeekType = FILE_BEGIN;
        break;

    case FILE_CURRENT:
        liDistance.QuadPart = dist;
        dwSeekType = FILE_CURRENT;
        break;

    case FILE_END:
        liDistance.QuadPart = pContext->qwOffset + pContext->qwSize + dist;
        dwSeekType = FILE_BEGIN;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid seek type.");;
    }

    if (SetIfVirtualFilePointer(&pContext->Cabinet, hFile, liDistance.QuadPart, &liNewPointer.QuadPart, seektype))
    {
        // set file pointer
        if (!::SetFilePointerEx(hFile, liDistance, &liNewPointer, seektype))
        {
            ExitWithLastError1(hr, "Failed to move file pointer 0x%x bytes.", dist);
        }
    }

    liNewPointer.QuadPart -= pContext->qwOffset;

LExit:
    pContext->Cabinet.hrError = hr;
    return FAILED(hr) ? -1 : liNewPointer.LowPart;
}

static int FAR DIAMONDAPI CabClose(
    __in INT_PTR hf
    )
{
    BURN_CONTAINER_CONTEXT* pContext = vpContext;
    HANDLE hFile = (HANDLE)hf;

    CloseIfVirturalFilePointer(&pContext->Cabinet, hFile);
    ReleaseFileHandle(hFile);

    return 0;
}

static HRESULT AddVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile,
    __in LONGLONG llInitialFilePointer
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pCabinetContext->rgVirtualFilePointers), pCabinetContext->cVirtualFilePointers, sizeof(BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER), ARRAY_GROWTH_SIZE);
    ExitOnFailure(hr, "Failed to allocate memory for the virtual file pointer array.");

    pCabinetContext->rgVirtualFilePointers[pCabinetContext->cVirtualFilePointers].hFile = hFile;
    pCabinetContext->rgVirtualFilePointers[pCabinetContext->cVirtualFilePointers].liPosition.QuadPart = llInitialFilePointer;
    ++pCabinetContext->cVirtualFilePointers;

LExit:
    return hr;
}

static HRESULT ReadIfVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile,
    __in DWORD cbRead
    )
{
    HRESULT hr = E_NOTFOUND;

    BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* pVfp = GetVirtualFilePointer(pCabinetContext, hFile);
    if (pVfp)
    {
        // Set the file handle to the virtual file pointer.
        if (!::SetFilePointerEx(hFile, pVfp->liPosition, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to move to virtual file pointer.");
        }

        pVfp->liPosition.QuadPart += cbRead; // add the amount that will be read to advance the pointer.
        hr = S_OK;
    }

LExit:
    return hr;
}

static BOOL SetIfVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile,
    __in LONGLONG llDistance,
    __out LONGLONG* pllNewPostion,
    __in DWORD dwSeekType
    )
{
    BOOL fFound = FALSE;

    BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* pVfp = GetVirtualFilePointer(pCabinetContext, hFile);
    if (pVfp)
    {
        switch (dwSeekType)
        {
        case FILE_BEGIN:
            pVfp->liPosition.QuadPart = llDistance;
            break;

        case FILE_CURRENT:
            pVfp->liPosition.QuadPart += llDistance;
            break;

        case FILE_END: __fallthrough;
        default:
            AssertSz(FALSE, "Unsupported seek type.");
            break;
        }

        *pllNewPostion = pVfp->liPosition.QuadPart;
        fFound = TRUE;
    }

    return fFound;
}

static HRESULT CloseIfVirturalFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile
    )
{
    HRESULT hr = E_NOTFOUND;

    BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* pVfp = GetVirtualFilePointer(pCabinetContext, hFile);
    if (pVfp)
    {
        pVfp->hFile = INVALID_HANDLE_VALUE;
        pVfp->liPosition.QuadPart = 0;
        hr = S_OK;
    }

    return hr;
}

static BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* GetVirtualFilePointer(
    __in BURN_CONTAINER_CONTEXT_CABINET* pCabinetContext,
    __in HANDLE hFile
    )
{
    for (DWORD i = 0; i < pCabinetContext->cVirtualFilePointers; ++i)
    {
        BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* pVfp = pCabinetContext->rgVirtualFilePointers + i;
        if (pVfp->hFile == hFile)
        {
            return pVfp;
        }
    }

    return NULL;
}
