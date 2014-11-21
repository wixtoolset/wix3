//-------------------------------------------------------------------------------------------------
// <copyright file="rmutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Restart Manager utility functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include <restartmanager.h>

#define ARRAY_GROWTH_SIZE 5

typedef DWORD (WINAPI *PFNRMJOINSESSION)(
    __out DWORD *pSessionHandle,
    __in_z const WCHAR strSessionKey[]
    );

typedef DWORD (WINAPI *PFNRMENDSESSION)(
    __in DWORD dwSessionHandle
    );

typedef DWORD (WINAPI *PFNRMREGISTERRESOURCES)(
    __in DWORD dwSessionHandle,
    __in UINT nFiles,
    __in_z_opt LPWSTR *rgsFilenames,
    __in UINT nApplications,
    __in_opt RM_UNIQUE_PROCESS *rgApplications,
    __in UINT nServices,
    __in_z_opt LPWSTR *rgsServiceNames
    );

typedef struct _RMU_SESSION
{
    CRITICAL_SECTION cs;
    DWORD dwSessionHandle;
    BOOL fStartedSessionHandle;
    BOOL fInitialized;

    UINT cFilenames;
    LPWSTR *rgsczFilenames;

    UINT cApplications;
    RM_UNIQUE_PROCESS *rgApplications;

    UINT cServiceNames;
    LPWSTR *rgsczServiceNames;

} RMU_SESSION;

static volatile LONG vcRmuInitialized = 0;
static HMODULE vhModule = NULL;
static PFNRMJOINSESSION vpfnRmJoinSession = NULL;
static PFNRMENDSESSION vpfnRmEndSession = NULL;
static PFNRMREGISTERRESOURCES vpfnRmRegisterResources = NULL;

static HRESULT RmuInitialize();
static void RmuUninitialize();

static HRESULT RmuApplicationArrayAlloc(
    __deref_inout_ecount(*pcApplications) RM_UNIQUE_PROCESS **prgApplications,
    __inout LPUINT pcApplications,
    __in DWORD dwProcessId,
    __in FILETIME ProcessStartTime
    );

static HRESULT RmuApplicationArrayFree(
    __in RM_UNIQUE_PROCESS *rgApplications
    );

#define ReleaseNullApplicationArray(rg, c) { if (rg) { RmuApplicationArrayFree(rg); c = 0; rg = NULL; } }

/********************************************************************
RmuJoinSession - Joins an existing Restart Manager session.

********************************************************************/
extern "C" HRESULT DAPI RmuJoinSession(
    __out PRMU_SESSION *ppSession,
    __in_z LPCWSTR wzSessionKey
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    PRMU_SESSION pSession = NULL;

    *ppSession = NULL;

    pSession = static_cast<PRMU_SESSION>(MemAlloc(sizeof(RMU_SESSION), TRUE));
    ExitOnNull(pSession, hr, E_OUTOFMEMORY, "Failed to allocate the RMU_SESSION structure.");

    hr = RmuInitialize();
    ExitOnFailure(hr, "Failed to initialize Restart Manager.");

    er = vpfnRmJoinSession(&pSession->dwSessionHandle, wzSessionKey);
    ExitOnWin32Error1(er, hr, "Failed to join Restart Manager session %ls.", wzSessionKey);

    ::InitializeCriticalSection(&pSession->cs);
    pSession->fInitialized = TRUE;

    *ppSession = pSession;

LExit:
    if (FAILED(hr))
    {
        ReleaseNullMem(pSession);
    }

    return hr;
}

/********************************************************************
RmuAddFile - Adds the file path to the Restart Manager session.

You should call this multiple times as necessary before calling
RmuRegisterResources.

********************************************************************/
extern "C" HRESULT DAPI RmuAddFile(
    __in PRMU_SESSION pSession,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&pSession->cs);

    // Create or grow the jagged array.
    hr = StrArrayAllocString(&pSession->rgsczFilenames, &pSession->cFilenames, wzPath, 0);
    ExitOnFailure(hr, "Failed to add the filename to the array.");

LExit:
    ::LeaveCriticalSection(&pSession->cs);
    return hr;
}

/********************************************************************
RmuAddProcessById - Adds the process ID to the Restart Manager sesion.

You should call this multiple times as necessary before calling
RmuRegisterResources.

********************************************************************/
extern "C" HRESULT DAPI RmuAddProcessById(
    __in PRMU_SESSION pSession,
    __in DWORD dwProcessId
    )
{
    HRESULT hr = S_OK;
    HANDLE hProcess = NULL;
    FILETIME CreationTime = {};
    FILETIME ExitTime = {};
    FILETIME KernelTime = {};
    FILETIME UserTime = {};
    BOOL fLocked = FALSE;

	HANDLE hToken = NULL;
	TOKEN_PRIVILEGES priv = { 0 };
	TOKEN_PRIVILEGES* pPrevPriv = NULL;
	DWORD cbPrevPriv = 0;

	BOOL fElevated = FALSE;
	ProcElevated(::GetCurrentProcess(), &fElevated);
	
	// Must be elevated to adjust process privileges
	if (TRUE == fElevated) {

		// This process must have SeDebugPrivilege, in the event that the process to be registered is runing under a different user,
		// otherwise OpenProcess will fail (when using either PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION).
		if (!::OpenProcessToken(::GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, &hToken))
		{
			ExitWithLastError(hr, "Failed to get process token.");
		}

		priv.PrivilegeCount = 1;
		priv.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
		if (!::LookupPrivilegeValueW(NULL, L"SeDebugPrivilege", &priv.Privileges[0].Luid))
		{
			ExitWithLastError(hr, "Failed to get debug privilege LUID.");
		}

		cbPrevPriv = sizeof(TOKEN_PRIVILEGES);
		pPrevPriv = static_cast<TOKEN_PRIVILEGES*>(MemAlloc(cbPrevPriv, TRUE));
		ExitOnNull(pPrevPriv, hr, E_OUTOFMEMORY, "Failed to allocate memory for empty previous privileges.");

		if (!::AdjustTokenPrivileges(hToken, FALSE, &priv, cbPrevPriv, pPrevPriv, &cbPrevPriv))
		{
			LPVOID pv = MemReAlloc(pPrevPriv, cbPrevPriv, TRUE);
			ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate memory for previous privileges.");
			pPrevPriv = static_cast<TOKEN_PRIVILEGES*>(pv);

			if (!::AdjustTokenPrivileges(hToken, FALSE, &priv, cbPrevPriv, pPrevPriv, &cbPrevPriv))
			{
				ExitWithLastError(hr, "Failed to get debug privilege LUID.");
			}
		}
	}

	// Calling process needs SeDebugPrivilege if the process to be opened running under a different user account.
	hProcess = ::OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, dwProcessId);
	if (TRUE == fElevated)
	{
		ExitOnNullWithLastError(hProcess, hr, "Failed to open the process ID %d.", dwProcessId);
		if (!::GetProcessTimes(hProcess, &CreationTime, &ExitTime, &KernelTime, &UserTime))
		{
			ExitWithLastError(hr, "Failed to get the process times for process ID %d.", dwProcessId);
		}

		::EnterCriticalSection(&pSession->cs);
		fLocked = TRUE;

		hr = RmuApplicationArrayAlloc(&pSession->rgApplications, &pSession->cApplications, dwProcessId, CreationTime);
		ExitOnFailure(hr, "Failed to add the application to the array.");

	}
	else
	{
		hr = E_NOTFOUND;
	}

LExit:
    if (hProcess)
    {
        ::CloseHandle(hProcess);
    }

	ReleaseMem(pPrevPriv);
	ReleaseHandle(hToken);

    if (fLocked)
    {
        ::LeaveCriticalSection(&pSession->cs);
    }

    return hr;
}

/********************************************************************
RmuAddProcessesByName - Adds all processes by the given process name
                        to the Restart Manager Session.

You should call this multiple times as necessary before calling
RmuRegisterResources.

********************************************************************/
extern "C" HRESULT DAPI RmuAddProcessesByName(
    __in PRMU_SESSION pSession,
    __in_z LPCWSTR wzProcessName
    )
{
    HRESULT hr = S_OK;
    DWORD *pdwProcessIds = NULL;
    DWORD cProcessIds = 0;
	BOOL fNotFound = FALSE;

    hr = ProcFindAllIdsFromExeName(wzProcessName, &pdwProcessIds, &cProcessIds);
    ExitOnFailure(hr, "Failed to enumerate all the processes by name %ls.", wzProcessName);

    for (DWORD i = 0; i < cProcessIds; ++i)
    {
        hr = RmuAddProcessById(pSession, pdwProcessIds[i]);
		if (E_NOTFOUND == hr)
		{
			// RmuAddProcessById returns E_NOTFOUND when this setup is not elevated and OpenProcess returned access denied (target process running under another user account). 
			fNotFound = TRUE;
		}
		else
		{
			ExitOnFailure(hr, "Failed to add process %ls (%d) to the Restart Manager session.", wzProcessName, pdwProcessIds[i]);
		}
    }

	// If one or more calls to RmuAddProcessById returned E_NOTFOUND, then return E_NOTFOUND even if other calls succeeded, so that caller can log the issue.
	if (TRUE == fNotFound)
	{
		hr =  E_NOTFOUND;
	}

LExit:
    ReleaseMem(pdwProcessIds);

	return hr;
}

/********************************************************************
RmuAddService - Adds the service name to the Restart Manager session.

You should call this multiple times as necessary before calling
RmuRegisterResources.

********************************************************************/
extern "C" HRESULT DAPI RmuAddService(
    __in PRMU_SESSION pSession,
    __in_z LPCWSTR wzServiceName
    )
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&pSession->cs);

    hr = StrArrayAllocString(&pSession->rgsczServiceNames, &pSession->cServiceNames, wzServiceName, 0);
    ExitOnFailure(hr, "Failed to add the service name to the array.");

LExit:
    ::LeaveCriticalSection(&pSession->cs);
    return hr;
}

/********************************************************************
RmuRegisterResources - Registers resources for the Restart Manager.

This should be called rarely because it is expensive to run. Call
functions like RmuAddFile for multiple resources then commit them
as a batch of updates to RmuRegisterResources.

Duplicate resources appear to be handled by Restart Manager.
Only one WM_QUERYENDSESSION is being sent for each top-level window.

********************************************************************/
extern "C" HRESULT DAPI RmuRegisterResources(
    __in PRMU_SESSION pSession
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    AssertSz(vcRmuInitialized, "Restart Manager was not properly initialized.");

    ::EnterCriticalSection(&pSession->cs);

    er = vpfnRmRegisterResources(
        pSession->dwSessionHandle,
        pSession->cFilenames,
        pSession->rgsczFilenames,
        pSession->cApplications,
        pSession->rgApplications,
        pSession->cServiceNames,
        pSession->rgsczServiceNames
        );
    ExitOnWin32Error(er, hr, "Failed to register the resources with the Restart Manager session.");

    // Empty the arrays if registered in case additional resources are added later.
    ReleaseNullStrArray(pSession->rgsczFilenames, pSession->cFilenames);
    ReleaseNullApplicationArray(pSession->rgApplications, pSession->cApplications);
    ReleaseNullStrArray(pSession->rgsczServiceNames, pSession->cServiceNames);

LExit:
    ::LeaveCriticalSection(&pSession->cs);
    return hr;
}

/********************************************************************
RmuEndSession - Ends the session.

If the session was joined by RmuJoinSession, any remaining resources
are registered before the session is ended (left).

********************************************************************/
extern "C" HRESULT DAPI RmuEndSession(
    __in PRMU_SESSION pSession
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    AssertSz(vcRmuInitialized, "Restart Manager was not properly initialized.");

    // Make sure all resources are registered if we joined the session.
    if (!pSession->fStartedSessionHandle)
    {
        hr = RmuRegisterResources(pSession);
        ExitOnFailure(hr, "Failed to register remaining resources.");
    }

    er = vpfnRmEndSession(pSession->dwSessionHandle);
    ExitOnWin32Error(er, hr, "Failed to end the Restart Manager session.");

LExit:
    if (pSession->fInitialized)
    {
        ::DeleteCriticalSection(&pSession->cs);
    }

    ReleaseNullStrArray(pSession->rgsczFilenames, pSession->cFilenames);
    ReleaseNullApplicationArray(pSession->rgApplications, pSession->cApplications);
    ReleaseNullStrArray(pSession->rgsczServiceNames, pSession->cServiceNames);
    ReleaseNullMem(pSession);

    RmuUninitialize();

    return hr;
}

static HRESULT RmuInitialize()
{
    HRESULT hr = S_OK;
    HMODULE hModule = NULL;

    LONG iRef = ::InterlockedIncrement(&vcRmuInitialized);
    if (1 == iRef && !vhModule)
    {
        hr = LoadSystemLibrary(L"rstrtmgr.dll", &hModule);
        ExitOnFailure(hr, "Failed to load the rstrtmgr.dll module.");

        vpfnRmJoinSession = reinterpret_cast<PFNRMJOINSESSION>(::GetProcAddress(hModule, "RmJoinSession"));
        ExitOnNullWithLastError(vpfnRmJoinSession, hr, "Failed to get the RmJoinSession procedure from rstrtmgr.dll.");

        vpfnRmRegisterResources = reinterpret_cast<PFNRMREGISTERRESOURCES>(::GetProcAddress(hModule, "RmRegisterResources"));
        ExitOnNullWithLastError(vpfnRmRegisterResources, hr, "Failed to get the RmRegisterResources procedure from rstrtmgr.dll.");

        vpfnRmEndSession = reinterpret_cast<PFNRMENDSESSION>(::GetProcAddress(hModule, "RmEndSession"));
        ExitOnNullWithLastError(vpfnRmEndSession, hr, "Failed to get the RmEndSession procedure from rstrtmgr.dll.");

        vhModule = hModule;
    }

LExit:
    return hr;
}

static void RmuUninitialize()
{
    LONG iRef = ::InterlockedDecrement(&vcRmuInitialized);
    if (0 == iRef && vhModule)
    {
        vpfnRmJoinSession = NULL;
        vpfnRmEndSession = NULL;
        vpfnRmRegisterResources = NULL;

        ::FreeLibrary(vhModule);
        vhModule = NULL;
    }
}

static HRESULT RmuApplicationArrayAlloc(
    __deref_inout_ecount(*pcApplications) RM_UNIQUE_PROCESS **prgApplications,
    __inout LPUINT pcApplications,
    __in DWORD dwProcessId,
    __in FILETIME ProcessStartTime
    )
{
    HRESULT hr = S_OK;
    RM_UNIQUE_PROCESS *pApplication = NULL;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgApplications), *pcApplications + 1, sizeof(RM_UNIQUE_PROCESS), ARRAY_GROWTH_SIZE);
    ExitOnFailure(hr, "Failed to allocate memory for the application array.");

    pApplication = static_cast<RM_UNIQUE_PROCESS*>(&(*prgApplications)[*pcApplications]);
    pApplication->dwProcessId = dwProcessId;
    pApplication->ProcessStartTime = ProcessStartTime;

    ++(*pcApplications);

LExit:
    return hr;
}

static HRESULT RmuApplicationArrayFree(
    __in RM_UNIQUE_PROCESS *rgApplications
    )
{
    HRESULT hr = S_OK;

    hr = MemFree(rgApplications);
    ExitOnFailure(hr, "Failed to free memory for the application array.");

LExit:
    return hr;
}
