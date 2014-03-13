//-------------------------------------------------------------------------------------------------
// <copyright file="wcawow64.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Windows Installer XML CustomAction utility library.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static HMODULE s_hKernel32;
static BOOL s_fWow64Initialized;
static BOOL (*s_pfnDisableWow64)(__out PVOID* );
static BOOL (*s_pfnRevertWow64)(__in PVOID );
static BOOL (*s_pfnIsWow64Process) (HANDLE, PBOOL);
static PVOID s_Wow64FSRevertState;
static BOOL s_fWow64FSDisabled;

/********************************************************************
 WcaInitializeWow64() - Initializes the Wow64 API

********************************************************************/
extern "C" HRESULT WIXAPI WcaInitializeWow64()
{
    AssertSz(WcaIsInitialized(), "WcaInitialize() should be called before calling WcaInitializeWow64()");
    AssertSz(!WcaIsWow64Initialized(), "WcaInitializeWow64() should not be called twice without calling WcaFinalizeWow64()");

    s_fWow64Initialized = FALSE;
    HRESULT hr = S_OK;
    s_Wow64FSRevertState = NULL;
    s_fWow64FSDisabled = false;

    // Test if we have access to the Wow64 API, and store the result in bWow64APIPresent
    s_hKernel32 = ::GetModuleHandleW(L"kernel32.dll");
    if (!s_hKernel32)
    {
        ExitWithLastError(hr, "failed to get handle to kernel32.dll");
    }

    // This will test if we have access to the Wow64 API
    s_pfnIsWow64Process = (BOOL (*)(HANDLE, PBOOL))::GetProcAddress(s_hKernel32, "IsWow64Process");
    if (NULL != s_pfnIsWow64Process)
    {
        s_pfnDisableWow64 = (BOOL (*)(PVOID *))::GetProcAddress(s_hKernel32, "Wow64DisableWow64FsRedirection");
        // If we fail, log the error but proceed, because we may not need a particular function, or the Wow64 API at all
        if (!s_pfnDisableWow64)
        {
            return S_FALSE;
        }

        s_pfnRevertWow64 = (BOOL (*)(PVOID))::GetProcAddress(s_hKernel32, "Wow64RevertWow64FsRedirection");
        if (!s_pfnRevertWow64)
        {
            return S_FALSE;
        }

        if (s_pfnDisableWow64 && s_pfnRevertWow64)
        {
            s_fWow64Initialized = TRUE;
        }
    }
    else
    {
        return S_FALSE;
    }

LExit:

    return hr;
}

/********************************************************************
 WcaIsWow64Process() - determines if the current process is running 
                       in WOW

********************************************************************/
extern "C" BOOL WIXAPI WcaIsWow64Process()
{
    BOOL fIsWow64Process = FALSE;
    if (s_fWow64Initialized)
    {
        if (!s_pfnIsWow64Process(GetCurrentProcess(), &fIsWow64Process))
        {
            // clear out the value since call failed
            fIsWow64Process = FALSE;
        }
    }
    return fIsWow64Process;
}

/********************************************************************
 WcaIsWow64Initialized() - determines if WcaInitializeWow64() has
                           been successfully called

********************************************************************/
extern "C" BOOL WIXAPI WcaIsWow64Initialized()
{
    return s_fWow64Initialized;
}

/********************************************************************
 WcaDisableWow64FSRedirection() - Disables Wow64 FS Redirection

********************************************************************/
extern "C" HRESULT WIXAPI WcaDisableWow64FSRedirection()
{
    AssertSz(s_fWow64Initialized && s_pfnDisableWow64 != NULL, "WcaDisableWow64FSRedirection() called, but Wow64 API was not initialized");

#ifdef DEBUG
    AssertSz(!s_fWow64FSDisabled, "You must call WcaRevertWow64FSRedirection() before calling WcaDisableWow64FSRedirection() again");
#endif

    HRESULT hr = S_OK;
    if (s_pfnDisableWow64(&s_Wow64FSRevertState))
    {
        s_fWow64FSDisabled = TRUE;
    }
    else
    {
        ExitWithLastError(hr, "Failed to disable WOW64.");
    }

LExit:
    return hr;
}

/********************************************************************
 WcaRevertWow64FSRedirection() - Reverts Wow64 FS Redirection to its
                                 pre-disabled state

********************************************************************/
extern "C" HRESULT WIXAPI WcaRevertWow64FSRedirection()
{
    AssertSz(s_fWow64Initialized && s_pfnDisableWow64 != NULL, "WcaRevertWow64FSRedirection() called, but Wow64 API was not initialized");

#ifdef DEBUG
    AssertSz(s_fWow64FSDisabled, "You must call WcaDisableWow64FSRedirection() before calling WcaRevertWow64FSRedirection()");
#endif

    HRESULT hr = S_OK;
    if (s_pfnRevertWow64(s_Wow64FSRevertState))
    {
        s_fWow64FSDisabled = FALSE;
    }
    else
    {
        ExitWithLastError(hr, "Failed to revert WOW64.");
    }

LExit:
    return hr;
}

/********************************************************************
 WcaFinalizeWow64() - Cleans up after Wow64 API Initialization

********************************************************************/
extern "C" HRESULT WIXAPI WcaFinalizeWow64()
{
    if (s_fWow64FSDisabled)
    {
#ifdef DEBUG
        AssertSz(FALSE, "WcaFinalizeWow64() called while Filesystem redirection was disabled.");
#else
        // If we aren't in debug mode, let's do our best to recover gracefully
        WcaRevertWow64FSRedirection();
#endif
    }

    s_fWow64Initialized = FALSE;
    s_pfnDisableWow64 = NULL;
    s_pfnRevertWow64 = NULL;

    return S_OK;
}
