// <copyright file="UpdateThread.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  Background thread for doing update checks implementation.
// </summary>
//
#include "precomp.h"

// external globals

// local globals
static CRITICAL_SECTION vUpdateThreadLock;
static HANDLE vhUpdateThread = NULL;
static BOOL vbShutdownThread = FALSE;

// structs
struct BACKGROUND_UPDATE_THREAD_CONTEXT
{
    LPWSTR pwzApplicationId;
    BOOL fExecuteUpdate;
};


// private functions
static DWORD WINAPI BackgroundUpdateThread(
    __in_opt LPVOID pvContext
    );

static HRESULT GetUpdateMutex(
    __in LPCWSTR wzAppId,
    __out HANDLE *phMutex
    );


extern "C" void WINAPI UpdateThreadInitialize()
{
    ::InitializeCriticalSection(&vUpdateThreadLock);
    AssertSz(!vhUpdateThread, "Update thread handle should be null in the beginning.");
    AssertSz(!vbShutdownThread, "Shutdown boolean should be FALSE in the beginning.");
}


extern "C" void WINAPI UpdateThreadUninitialize()
{
    ::InterlockedIncrement(reinterpret_cast<LONG*>(&vbShutdownThread));

    ::EnterCriticalSection(&vUpdateThreadLock);

    if (vhUpdateThread)
    {
        ::WaitForSingleObject(vhUpdateThread, INFINITE);
        ::CloseHandle(vhUpdateThread);
    }

    ::LeaveCriticalSection(&vUpdateThreadLock);

    ::DeleteCriticalSection(&vUpdateThreadLock);
}


extern "C" HRESULT WINAPI UpdateThreadCheck(
    __in LPCWSTR wzAppId,
    __in BOOL fTryExecuteUpdate
    )
{
    HRESULT hr = S_OK;
    BOOL fLocked = FALSE;
    BACKGROUND_UPDATE_THREAD_CONTEXT* pContext = NULL;

    ::EnterCriticalSection(&vUpdateThreadLock);
    fLocked = TRUE;

    if (vhUpdateThread)
    {
        DWORD er = ::WaitForSingleObject(vhUpdateThread, 0);
        if (WAIT_OBJECT_0 == er)
        {
            ::CloseHandle(vhUpdateThread);
            vhUpdateThread = NULL;
        }
        else
        {
            hr = S_FALSE;
            ExitFunction();
        }
    }

    pContext = static_cast<BACKGROUND_UPDATE_THREAD_CONTEXT*>(MemAlloc(sizeof(BACKGROUND_UPDATE_THREAD_CONTEXT), TRUE));
    ExitOnNull(pContext, hr, E_OUTOFMEMORY, "Failed to allocate memory for context.");

    hr= StrAllocString(&pContext->pwzApplicationId, wzAppId, 0);
    ExitOnFailure(hr, "Failed to copy app id into context.");

    pContext->fExecuteUpdate = fTryExecuteUpdate;

    vhUpdateThread = ::CreateThread(NULL, 0, BackgroundUpdateThread, reinterpret_cast<LPVOID>(pContext), 0, NULL);
    ExitOnNullWithLastError(vhUpdateThread, hr, "Failed to create background update thread.");

    pContext = NULL;

LExit:
    if (pContext)
    {
        ReleaseStr(pContext->pwzApplicationId);
        MemFree(pContext);
    }

    if (fLocked)
    {
        ::LeaveCriticalSection(&vUpdateThreadLock);
    }

   return hr;
}


static DWORD WINAPI BackgroundUpdateThread(
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BACKGROUND_UPDATE_THREAD_CONTEXT* pContext = reinterpret_cast<BACKGROUND_UPDATE_THREAD_CONTEXT*>(pvContext);
    DWORD64 dw64Version = 0;
    LPWSTR pwzFeedUri = NULL;
    DWORD64 dw64NextUpdateTime = 0;

    HANDLE hProcess = INVALID_HANDLE_VALUE;
    HANDLE hUpdateMutex = INVALID_HANDLE_VALUE;

    if (!pContext || !pContext->pwzApplicationId || !*pContext->pwzApplicationId)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Background thread was not passed application identifier.");
    }

    if (vbShutdownThread)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = GetUpdateMutex(pContext->pwzApplicationId, &hUpdateMutex);
    ExitOnFailure(hr, "Failed to get update mutex, skipping update check.");

    if (vbShutdownThread)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = RssUpdateGetAppInfo(pContext->pwzApplicationId, &dw64Version, &pwzFeedUri, NULL);
    ExitOnFailure(hr, "Failed to get app info.");

    if (pContext->fExecuteUpdate)
    {
        // If an update is available and higher version that the application currently on the local 
        // machine, launch the install and bail.
        hr = RssUpdateTryLaunchUpdate(pContext->pwzApplicationId, dw64Version, &hProcess, &dw64NextUpdateTime);
        if (SUCCEEDED(hr))
        {
            if (hProcess)
            {
                ::CloseHandle(hProcess);
                ExitFunction(); // bail since we're doing an update
            }
        }
    }

    if (vbShutdownThread)
    {
        ExitFunction1(hr = S_OK);
    }

    // If no update process was launched, go check for a feed update.
    hr = RssUpdateCheckFeed(pContext->pwzApplicationId, dw64Version, pwzFeedUri, dw64NextUpdateTime);

LExit:
    if (INVALID_HANDLE_VALUE != hUpdateMutex)
    {
        ::CloseHandle(hUpdateMutex);
    }

    if (INVALID_HANDLE_VALUE != hProcess)
    {
        ::CloseHandle(hProcess);
    }

    ReleaseStr(pwzFeedUri);

    if (pContext)
    {
        ReleaseStr(pContext->pwzApplicationId);
        MemFree(pContext);
    }

    return hr;
}


static HRESULT GetUpdateMutex(
    __in LPCWSTR wzAppId,
    __out HANDLE *phMutex
    )
{
    HRESULT hr = S_OK;
    HANDLE h = INVALID_HANDLE_VALUE;
    WCHAR wzMutexName[MAX_PATH];

    hr = ::StringCchPrintfW(wzMutexName, countof(wzMutexName), L"Local\\CT_UPDATE_%S",wzAppId);
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
