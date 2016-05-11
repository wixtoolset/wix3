// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "SfxUtil.h"

// Globals for keeping track of things across UI messages.
static const wchar_t* g_szWorkingDir;
static ICorRuntimeHost* g_pClrHost;
static _AppDomain* g_pAppDomain;
static _MethodInfo* g_pProcessMessageMethod;
static _MethodInfo* g_pShutdownMethod;

// Reserve extra space for strings to be replaced at build time.
#define NULLSPACE \
L"\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" \
L"\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" \
L"\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" \
L"\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"

// Prototypes for local functions.
// See the function definitions for comments.

bool InvokeInitializeMethod(_MethodInfo* pInitMethod, MSIHANDLE hSession,
	const wchar_t* szClassName, LPDWORD pdwInternalUILevel, UINT* puiResult);

/// <summary>
/// First entry-point for the UI DLL when loaded and called by MSI.
/// Extracts the payload, hosts the CLR, and invokes the managed
/// initialize method.
/// </summary>
/// <param name="hSession">Handle to the installer session,
/// used for logging errors and to be passed on to the managed initialize method.</param>
/// <param name="szResourcePath">Path the directory where resources from the MsiEmbeddedUI table
/// have been extracted, and where additional payload from this package will be extracted.</param>
/// <param name="pdwInternalUILevel">MSI install UI level passed to and returned from
/// the managed initialize method.</param>
extern "C"
UINT __stdcall InitializeEmbeddedUI(MSIHANDLE hSession, LPCWSTR szResourcePath, LPDWORD pdwInternalUILevel)
{
	// If the managed initialize method cannot be called, continue the installation in BASIC UI mode.
	UINT uiResult = INSTALLUILEVEL_BASIC;

	const wchar_t* szClassName = L"InitializeEmbeddedUI_FullClassName" NULLSPACE;

	g_szWorkingDir = szResourcePath;

	wchar_t szModule[MAX_PATH];
	DWORD cchCopied = GetModuleFileName(g_hModule, szModule, MAX_PATH - 1);
	if (cchCopied == 0)
	{
		Log(hSession, L"Failed to get module path. Error code %d.", GetLastError());
		return uiResult;
	}
	else if (cchCopied == MAX_PATH - 1)
	{
		Log(hSession, L"Failed to get module path -- path is too long.");
		return uiResult;
	}

	Log(hSession, L"Extracting embedded UI to temporary directory: %s", g_szWorkingDir);
	int err = ExtractCabinet(szModule, g_szWorkingDir);
	if (err != 0)
	{
		Log(hSession, L"Failed to extract to temporary directory. Cabinet error code %d.", err);
		Log(hSession, L"Ensure that no MsiEmbeddedUI.FileName values are the same as "
					  L"any file contained in the embedded UI package.");
		return uiResult;
	}

	wchar_t szConfigFilePath[MAX_PATH + 20];
	StringCchCopy(szConfigFilePath, MAX_PATH + 20, g_szWorkingDir);
	StringCchCat(szConfigFilePath, MAX_PATH + 20, L"\\EmbeddedUI.config");

	const wchar_t* szConfigFile = szConfigFilePath;
	if (!PathFileExists(szConfigFilePath))
	{
		szConfigFile = NULL;
	}

	wchar_t szWIAssembly[MAX_PATH + 50];
	StringCchCopy(szWIAssembly, MAX_PATH + 50, g_szWorkingDir);
	StringCchCat(szWIAssembly, MAX_PATH + 50, L"\\Microsoft.Deployment.WindowsInstaller.dll");

	if (LoadCLR(hSession, NULL, szConfigFile, szWIAssembly, &g_pClrHost))
	{
		if (CreateAppDomain(hSession, g_pClrHost, L"EmbeddedUI", g_szWorkingDir,
			szConfigFile, &g_pAppDomain))
		{
			const wchar_t* szMsiAssemblyName  = L"Microsoft.Deployment.WindowsInstaller";
			const wchar_t* szProxyClass = L"Microsoft.Deployment.WindowsInstaller.EmbeddedUIProxy";
			const wchar_t* szInitMethod = L"Initialize";
			const wchar_t* szProcessMessageMethod = L"ProcessMessage";
			const wchar_t* szShutdownMethod = L"Shutdown";
	
			if (GetMethod(hSession, g_pAppDomain, szMsiAssemblyName,
					  szProxyClass, szProcessMessageMethod, &g_pProcessMessageMethod) &&
				GetMethod(hSession, g_pAppDomain, szMsiAssemblyName,
					  szProxyClass, szShutdownMethod, &g_pShutdownMethod))
			{
				_MethodInfo* pInitMethod;
				if (GetMethod(hSession, g_pAppDomain, szMsiAssemblyName,
							  szProxyClass, szInitMethod, &pInitMethod))
				{
					bool invokeSuccess = InvokeInitializeMethod(pInitMethod, hSession, szClassName, pdwInternalUILevel, &uiResult);
					pInitMethod->Release();
					if (invokeSuccess)
					{
						if (uiResult == 0)
						{
							return ERROR_SUCCESS;
						}
						else if (uiResult == ERROR_INSTALL_USEREXIT)
						{
							// InitializeEmbeddedUI is not allowed to return ERROR_INSTALL_USEREXIT.
							// So return success here and then IDCANCEL on the next progress message.
							uiResult = 0;
							*pdwInternalUILevel = INSTALLUILEVEL_NONE;
							Log(hSession, L"Initialization canceled by user.");
						}
					}
				}
			}

			g_pProcessMessageMethod->Release();
			g_pProcessMessageMethod = NULL;
			g_pShutdownMethod->Release();
			g_pShutdownMethod = NULL;

			g_pClrHost->UnloadDomain(g_pAppDomain);
			g_pAppDomain->Release();
			g_pAppDomain = NULL;
		}
		g_pClrHost->Stop();
		g_pClrHost->Release();
		g_pClrHost = NULL;
	}

	return uiResult;
}

/// <summary>
/// Entry-point for UI progress messages received from the MSI engine during an active installation.
/// Forwards the progress messages to the managed handler method and returns its result.
/// </summary>
extern "C"
INT __stdcall EmbeddedUIHandler(UINT uiMessageType, MSIHANDLE hRecord)
{
	if (g_pProcessMessageMethod == NULL)
	{
		// Initialization was canceled. 
		return IDCANCEL;
	}

	VARIANT vResult;
	VariantInit(&vResult);

	VARIANT vNull;
	vNull.vt = VT_EMPTY;

	SAFEARRAY* saArgs = SafeArrayCreateVector(VT_VARIANT, 0, 2);
	VARIANT vMessageType;
	vMessageType.vt = VT_I4;
	vMessageType.lVal = (LONG) uiMessageType;
	LONG index = 0;
	HRESULT hr = SafeArrayPutElement(saArgs, &index, &vMessageType);
	if (FAILED(hr)) goto LExit;
	VARIANT vRecord;
	vRecord.vt = VT_I4;
	vRecord.lVal = (LONG) hRecord;
	index = 1;
	hr = SafeArrayPutElement(saArgs, &index, &vRecord);
	if (FAILED(hr)) goto LExit;
	
	hr = g_pProcessMessageMethod->Invoke_3(vNull, saArgs, &vResult);

LExit:
	SafeArrayDestroy(saArgs);
	if (SUCCEEDED(hr))
	{
		return vResult.intVal;
	}
	else
	{
		return -1;
	}
}

/// <summary>
/// Entry-point for the UI shutdown message received from the MSI engine after installation has completed.
/// Forwards the shutdown message to the managed shutdown method, then shuts down the CLR.
/// </summary>
extern "C"
DWORD __stdcall ShutdownEmbeddedUI()
{
	if (g_pShutdownMethod != NULL)
	{
		VARIANT vNull;
		vNull.vt = VT_EMPTY;
		SAFEARRAY* saArgs = SafeArrayCreateVector(VT_VARIANT, 0, 0);
		g_pShutdownMethod->Invoke_3(vNull, saArgs, NULL);
		SafeArrayDestroy(saArgs);

		g_pClrHost->UnloadDomain(g_pAppDomain);
		g_pAppDomain->Release();
		g_pClrHost->Stop();
		g_pClrHost->Release();
	}

	return 0;
}

/// <summary>
/// Loads and invokes the managed portion of the proxy.
/// </summary>
/// <param name="pInitMethod">Managed initialize method to be invoked.</param>
/// <param name="hSession">Handle to the installer session,
/// used for logging errors and to be passed on to the managed initialize method.</param>
/// <param name="szClassName">Name of the UI class to be loaded.
/// This must be of the form: AssemblyName!Namespace.Class</param>
/// <param name="pdwInternalUILevel">MSI install UI level passed to and returned from
/// the managed initialize method.</param>
/// <param name="puiResult">Return value of the invoked initialize method.</param>
/// <returns>True if the managed proxy was invoked successfully, or an
/// error code if there was some error. Note the initialize method itself may
/// return an error via puiResult while this method still returns true
/// since the invocation was successful.</returns>
bool InvokeInitializeMethod(_MethodInfo* pInitMethod, MSIHANDLE hSession, const wchar_t* szClassName, LPDWORD pdwInternalUILevel, UINT* puiResult)
{
	VARIANT vResult;
	VariantInit(&vResult);

	VARIANT vNull;
	vNull.vt = VT_EMPTY;

	SAFEARRAY* saArgs = SafeArrayCreateVector(VT_VARIANT, 0, 3);
	VARIANT vSessionHandle;
	vSessionHandle.vt = VT_I4;
	vSessionHandle.lVal = (LONG) hSession;
	LONG index = 0;
	HRESULT hr = SafeArrayPutElement(saArgs, &index, &vSessionHandle);
	if (FAILED(hr)) goto LExit;
	VARIANT vEntryPoint;
	vEntryPoint.vt = VT_BSTR;
	vEntryPoint.bstrVal = SysAllocString(szClassName);
	if (vEntryPoint.bstrVal == NULL)
	{
		hr = E_OUTOFMEMORY;
		goto LExit;
	}
	index = 1;
	hr = SafeArrayPutElement(saArgs, &index, &vEntryPoint);
	if (FAILED(hr)) goto LExit;
	VARIANT vUILevel;
	vUILevel.vt = VT_I4;
	vUILevel.ulVal = *pdwInternalUILevel;
	index = 2;
	hr = SafeArrayPutElement(saArgs, &index, &vUILevel);
	if (FAILED(hr)) goto LExit;
	
	hr = pInitMethod->Invoke_3(vNull, saArgs, &vResult);
	
LExit:
	SafeArrayDestroy(saArgs);
	if (SUCCEEDED(hr))
	{
		*puiResult = (UINT) vResult.lVal;
		if ((*puiResult & 0xFFFF) == 0)
		{
			// Due to interop limitations, the successful resulting UILevel is returned
			// as the high-word of the return value instead of via a ref parameter.
			*pdwInternalUILevel = *puiResult >> 16;
			*puiResult = 0;
		}
		return true;
	}
	else
	{
		Log(hSession, L"Failed to invoke EmbeddedUI Initialize method. Error code 0x%X", hr);
		return false;
	}
}
