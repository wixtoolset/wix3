// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "EntryPoints.h"
#include "SfxUtil.h"

#define MANAGED_CAs_OUT_OF_PROC 1

HMODULE g_hModule;
bool g_fRunningOutOfProc = false;

RemoteMsiSession* g_pRemote = NULL;

// Prototypes for local functions.
// See the function definitions for comments.

bool InvokeManagedCustomAction(MSIHANDLE hSession,
        _AppDomain* pAppDomain, const wchar_t* szEntryPoint, int* piResult);

/// <summary>
/// Entry-point for the CA DLL when re-launched as a separate process;
/// connects the comm channel for remote MSI APIs, then invokes the
/// managed custom action entry-point.
/// </summary>
/// <remarks>
/// Do not change the parameters or calling-convention: RUNDLL32
/// requires this exact signature.
/// </remarks>
extern "C"
void __stdcall InvokeManagedCustomActionOutOfProc(
        __in HWND hwnd, __in HINSTANCE hinst, __in_z wchar_t* szCmdLine, int nCmdShow)
{
        UNREFERENCED_PARAMETER(hwnd);
        UNREFERENCED_PARAMETER(hinst);
        UNREFERENCED_PARAMETER(nCmdShow);

        g_fRunningOutOfProc = true;

        const wchar_t* szSessionName = szCmdLine;
        MSIHANDLE hSession;
        const wchar_t* szEntryPoint;

        int i;
        for (i = 0; szCmdLine[i] && szCmdLine[i] != L' '; i++);
        if (szCmdLine[i] != L'\0') szCmdLine[i++] = L'\0';
        hSession = _wtoi(szCmdLine + i);

        for (; szCmdLine[i] && szCmdLine[i] != L' '; i++);
        if (szCmdLine[i] != L'\0') szCmdLine[i++] = L'\0';
        szEntryPoint = szCmdLine + i;

        g_pRemote = new RemoteMsiSession(szSessionName, false);
        g_pRemote->Connect();

        int ret = InvokeCustomAction(hSession, NULL, szEntryPoint);

        RemoteMsiSession::RequestData requestData;
        SecureZeroMemory(&requestData, sizeof(RemoteMsiSession::RequestData));
        requestData.fields[0].vt = VT_I4;
        requestData.fields[0].iValue = ret;
        g_pRemote->SendRequest(RemoteMsiSession::EndSession, &requestData, NULL);
        delete g_pRemote;
}

/// <summary>
/// Re-launch this CA DLL as a separate process, and setup a comm channel
/// for remote MSI API calls back to this process.
/// </summary>
int InvokeOutOfProcManagedCustomAction(MSIHANDLE hSession, const wchar_t* szEntryPoint)
{
        wchar_t szSessionName[100] = {0};
        swprintf_s(szSessionName, 100, L"SfxCA_%d", ::GetTickCount());
        
        RemoteMsiSession remote(szSessionName, true);

        DWORD ret = remote.Connect();
        if (ret != 0)
        {
                Log(hSession, L"Failed to create communication pipe for new CA process. Error code: %d", ret);
                return ERROR_INSTALL_FAILURE;
        }

        ret = remote.ProcessRequests();
        if (ret != 0)
        {
                Log(hSession, L"Failed to open communication pipe for new CA process. Error code: %d", ret);
                return ERROR_INSTALL_FAILURE;
        }

        wchar_t szModule[MAX_PATH] = {0};
        GetModuleFileName(g_hModule, szModule, MAX_PATH);

        const wchar_t* rundll32 = L"rundll32.exe";
        wchar_t szRunDll32Path[MAX_PATH] = {0};
        GetSystemDirectory(szRunDll32Path, MAX_PATH);
        wcscat_s(szRunDll32Path, MAX_PATH, L"\\");
        wcscat_s(szRunDll32Path, MAX_PATH, rundll32);

        const wchar_t* entry = L"zzzzInvokeManagedCustomActionOutOfProc";
        wchar_t szCommandLine[1024] = {0};
        swprintf_s(szCommandLine, 1024, L"%s \"%s\",%s %s %d %s",
                rundll32, szModule, entry, szSessionName, hSession, szEntryPoint);

        STARTUPINFO si;
        SecureZeroMemory(&si, sizeof(STARTUPINFO));
        si.cb = sizeof(STARTUPINFO);
        
        PROCESS_INFORMATION pi;
        SecureZeroMemory(&pi, sizeof(PROCESS_INFORMATION));

        if (!CreateProcess(szRunDll32Path, szCommandLine, NULL, NULL, FALSE,
                0, NULL, NULL, &si, &pi))
        {
                DWORD err = GetLastError();
                Log(hSession, L"Failed to create new CA process via RUNDLL32. Error code: %d", err);
                return ERROR_INSTALL_FAILURE;
        }

        DWORD dwWait = WaitForSingleObject(pi.hProcess, INFINITE);
        if (dwWait != WAIT_OBJECT_0)
        {
                DWORD err = GetLastError();
                Log(hSession, L"Failed to wait for CA process. Error code: %d", err);
                return ERROR_INSTALL_FAILURE;
        }

        DWORD dwExitCode;
        BOOL bRet = GetExitCodeProcess(pi.hProcess, &dwExitCode);
        if (!bRet)
        {
                DWORD err = GetLastError();
                Log(hSession, L"Failed to get exit code of CA process. Error code: %d", err);
                return ERROR_INSTALL_FAILURE;
        }
        else if (dwExitCode != 0)
        {
                Log(hSession, L"RUNDLL32 returned error code: %d", dwExitCode);
                return ERROR_INSTALL_FAILURE;
        }

        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);

        remote.WaitExitCode();
        return remote.ExitCode;
}

/// <summary>
/// Entrypoint for the managed CA proxy (RemotableNativeMethods) to
/// call MSI APIs remotely.
/// </summary>
void __stdcall MsiRemoteInvoke(RemoteMsiSession::RequestId id, RemoteMsiSession::RequestData* pRequest, RemoteMsiSession::RequestData** ppResponse)
{
        if (g_fRunningOutOfProc)
        {
                g_pRemote->SendRequest(id, pRequest, ppResponse);
        }
        else
        {
                *ppResponse = NULL;
        }
}

/// <summary>
/// Invokes a managed custom action from native code by
/// extracting the package to a temporary working directory
/// then hosting the CLR and locating and calling the entrypoint.
/// </summary>
/// <param name="hSession">Handle to the installation session.
/// Passed to custom action entrypoints by the installer engine.</param>
/// <param name="szWorkingDir">Directory containing the CA binaries
/// and the CustomAction.config file defining the entrypoints.
/// This may be NULL, in which case the current module must have
/// a concatenated cabinet containing those files, which will be
/// extracted to a temporary directory.</param>
/// <param name="szEntryPoint">Name of the CA entrypoint to be invoked.
/// This must be either an explicit &quot;AssemblyName!Namespace.Class.Method&quot;
/// string, or a simple name that maps to a full entrypoint definition
/// in CustomAction.config.</param>
/// <returns>The value returned by the managed custom action method,
/// or ERROR_INSTALL_FAILURE if the CA could not be invoked.</returns>
int InvokeCustomAction(MSIHANDLE hSession,
        const wchar_t* szWorkingDir, const wchar_t* szEntryPoint)
{
#ifdef MANAGED_CAs_OUT_OF_PROC
        if (!g_fRunningOutOfProc && szWorkingDir == NULL)
        {
                return InvokeOutOfProcManagedCustomAction(hSession, szEntryPoint);
        }
#endif

        wchar_t szTempDir[MAX_PATH];
        bool fDeleteTemp = false;
        if (szWorkingDir == NULL)
        {
                if (!ExtractToTempDirectory(hSession, g_hModule, szTempDir, MAX_PATH))
                {
                        return ERROR_INSTALL_FAILURE;
                }
                szWorkingDir = szTempDir;
                fDeleteTemp = true;
        }

        wchar_t szConfigFilePath[MAX_PATH + 20];
        StringCchCopy(szConfigFilePath, MAX_PATH + 20, szWorkingDir);
        StringCchCat(szConfigFilePath, MAX_PATH + 20, L"\\CustomAction.config");

        const wchar_t* szConfigFile = szConfigFilePath;
        if (!::PathFileExists(szConfigFilePath))
        {
                szConfigFile = NULL;
        }

        wchar_t szWIAssembly[MAX_PATH + 50];
        StringCchCopy(szWIAssembly, MAX_PATH + 50, szWorkingDir);
        StringCchCat(szWIAssembly, MAX_PATH + 50, L"\\Microsoft.Deployment.WindowsInstaller.dll");

        int iResult = ERROR_INSTALL_FAILURE;
        ICorRuntimeHost* pHost;
        if (LoadCLR(hSession, NULL, szConfigFile, szWIAssembly, &pHost))
        {
                _AppDomain* pAppDomain;
                if (CreateAppDomain(hSession, pHost, L"CustomAction", szWorkingDir,
                        szConfigFile, &pAppDomain))
                {
                        if (!InvokeManagedCustomAction(hSession, pAppDomain, szEntryPoint, &iResult))
                        {
                                iResult = ERROR_INSTALL_FAILURE;
                        }
                        HRESULT hr = pHost->UnloadDomain(pAppDomain);
                        if (FAILED(hr))
                        {
                                Log(hSession, L"Failed to unload app domain. Error code 0x%X", hr);
                        }
                        pAppDomain->Release();
                }

                pHost->Stop();
                pHost->Release();
        }

        if (fDeleteTemp)
        {
                DeleteDirectory(szTempDir);
        }
        return iResult;
}

/// <summary>
/// Called by the system when the DLL is loaded.
/// Saves the module handle for later use.
/// </summary>
BOOL WINAPI DllMain(HMODULE hModule, DWORD  dwReason, void* pReserved)
{
        UNREFERENCED_PARAMETER(pReserved);

        switch (dwReason)
        {
                case DLL_PROCESS_ATTACH:
                        g_hModule = hModule;
                        break;
                case DLL_THREAD_ATTACH:
                case DLL_THREAD_DETACH:
                case DLL_PROCESS_DETACH:
                        break;
        }
        return TRUE;
}

/// <summary>
/// Loads and invokes the managed portion of the proxy.
/// </summary>
/// <param name="hSession">Handle to the installer session,
/// used for logging errors and to be passed on to the custom action.</param>
/// <param name="pAppDomain">AppDomain which has its application
/// base set to the CA working directory.</param>
/// <param name="szEntryPoint">Name of the CA entrypoint to be invoked.
/// This must be either an explicit &quot;AssemblyName!Namespace.Class.Method&quot;
/// string, or a simple name that maps to a full entrypoint definition
/// in CustomAction.config.</param>
/// <param name="piResult">Return value of the invoked custom
/// action method.</param>
/// <returns>True if the managed proxy was invoked successfully,
/// false if there was some error. Note the custom action itself may
/// return an error via piResult while this method still returns true
/// since the invocation was successful.</returns>
bool InvokeManagedCustomAction(MSIHANDLE hSession, _AppDomain* pAppDomain,
        const wchar_t* szEntryPoint, int* piResult)
{
        VARIANT vResult;
        ::VariantInit(&vResult);

        const bool f64bit = (sizeof(void*) == sizeof(LONGLONG));
        const wchar_t* szMsiAssemblyName   = L"Microsoft.Deployment.WindowsInstaller";
        const wchar_t* szMsiCAProxyClass   = L"Microsoft.Deployment.WindowsInstaller.CustomActionProxy";
        const wchar_t* szMsiCAInvokeMethod = (f64bit ? L"InvokeCustomAction64" : L"InvokeCustomAction32");
        
        _MethodInfo* pCAInvokeMethod;
        if (!GetMethod(hSession, pAppDomain, szMsiAssemblyName,
                szMsiCAProxyClass, szMsiCAInvokeMethod, &pCAInvokeMethod))
        {
                return false;
        }

        HRESULT hr;
        VARIANT vNull;
        vNull.vt = VT_EMPTY;
        SAFEARRAY* saArgs = SafeArrayCreateVector(VT_VARIANT, 0, 3);
        VARIANT vSessionHandle;
        vSessionHandle.vt = VT_I4;
        vSessionHandle.intVal = hSession;
        LONG index = 0;
        hr = SafeArrayPutElement(saArgs, &index, &vSessionHandle);
        if (FAILED(hr)) goto LExit;
        VARIANT vEntryPoint;
        vEntryPoint.vt = VT_BSTR;
        vEntryPoint.bstrVal = SysAllocString(szEntryPoint);
        if (vEntryPoint.bstrVal == NULL)
        {
                hr = E_OUTOFMEMORY;
                goto LExit;
        }
        index = 1;
        hr = SafeArrayPutElement(saArgs, &index, &vEntryPoint);
        if (FAILED(hr)) goto LExit;
        VARIANT vRemotingFunctionPtr;
#pragma warning(push)
#pragma warning(disable:4127) // conditional expression is constant
        if (f64bit)
#pragma warning(pop)
        {
                vRemotingFunctionPtr.vt =  VT_I8;
                vRemotingFunctionPtr.llVal = (LONGLONG) (g_fRunningOutOfProc ? MsiRemoteInvoke : NULL);
        }
        else
        {
                vRemotingFunctionPtr.vt =  VT_I4;
#pragma warning(push)
#pragma warning(disable:4302) // truncation
#pragma warning(disable:4311) // pointer truncation
                vRemotingFunctionPtr.lVal = (LONG) (g_fRunningOutOfProc ? MsiRemoteInvoke : NULL);
#pragma warning(pop)
        }
        index = 2;
        hr = SafeArrayPutElement(saArgs, &index, &vRemotingFunctionPtr);
        if (FAILED(hr)) goto LExit;
        
        hr = pCAInvokeMethod->Invoke_3(vNull, saArgs, &vResult);

LExit:
        SafeArrayDestroy(saArgs);
        pCAInvokeMethod->Release();
        
        if (FAILED(hr))
        {
                Log(hSession, L"Failed to invoke custom action method. Error code 0x%X", hr);
                return false;
        }

        *piResult = vResult.intVal;
        return true;
}

