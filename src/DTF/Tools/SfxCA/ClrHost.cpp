// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

void Log(MSIHANDLE hSession, const wchar_t* szMessage, ...);

//---------------------------------------------------------------------
// CLR HOSTING
//---------------------------------------------------------------------

/// <summary>
/// Binds to the CLR after determining the appropriate version.
/// </summary>
/// <param name="hSession">Handle to the installer session,
/// used just for logging.</param>
/// <param name="version">Specific version of the CLR to load.
/// If null, then the config file and/or primary assembly are
/// used to determine the version.</param>
/// <param name="szConfigFile">XML .config file which may contain
/// a startup section to direct which version of the CLR to use.
/// May be NULL.</param>
/// <param name="szPrimaryAssembly">Assembly to be used to determine
/// the version of the CLR in the absence of other configuration.
/// May be NULL.</param>
/// <param name="ppHost">Returned runtime host interface.</param>
/// <returns>True if the CLR was loaded successfully, false if
/// there was some error.</returns>
/// <remarks>
/// If szPrimaryAssembly is NULL and szConfigFile is also NULL or
/// does not contain any version configuration, the CLR will not be loaded.
/// </remarks>
bool LoadCLR(MSIHANDLE hSession, const wchar_t* szVersion, const wchar_t* szConfigFile,
	const wchar_t* szPrimaryAssembly, ICorRuntimeHost** ppHost)
{
	typedef HRESULT (__stdcall *PGetRequestedRuntimeInfo)(LPCWSTR pExe, LPCWSTR pwszVersion,
		LPCWSTR pConfigurationFile, DWORD startupFlags, DWORD runtimeInfoFlags,
		LPWSTR pDirectory, DWORD dwDirectory, DWORD *dwDirectoryLength,
		LPWSTR pVersion, DWORD cchBuffer, DWORD* dwlength);
	typedef HRESULT (__stdcall *PCorBindToRuntimeEx)(LPCWSTR pwszVersion, LPCWSTR pwszBuildFlavor,
		DWORD startupFlags, REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv);

	HMODULE hmodMscoree = LoadLibrary(L"mscoree.dll");
	if (hmodMscoree == NULL)
	{
		Log(hSession, L"Failed to load mscoree.dll (Error code %d). This custom action "
			L"requires the .NET Framework to be installed.", GetLastError());
		return false;
	}
	PGetRequestedRuntimeInfo pGetRequestedRuntimeInfo = (PGetRequestedRuntimeInfo)
		GetProcAddress(hmodMscoree, "GetRequestedRuntimeInfo");
	PCorBindToRuntimeEx pCorBindToRuntimeEx = (PCorBindToRuntimeEx)
		GetProcAddress(hmodMscoree, "CorBindToRuntimeEx");
	if (pGetRequestedRuntimeInfo == NULL || pCorBindToRuntimeEx == NULL)
	{
		Log(hSession, L"Failed to locate functions in mscoree.dll (Error code %d). This custom action "
			L"requires the .NET Framework to be installed.", GetLastError());
		FreeLibrary(hmodMscoree);
		return false;
	}

	wchar_t szClrVersion[20];
	HRESULT hr;

	if (szVersion != NULL && szVersion[0] != L'\0')
	{
		wcsncpy_s(szClrVersion, 20, szVersion, 20);
	}
	else
	{
		wchar_t szVersionDir[MAX_PATH];
		hr = pGetRequestedRuntimeInfo(szPrimaryAssembly, NULL,
			szConfigFile, 0, 0, szVersionDir, MAX_PATH, NULL, szClrVersion, 20, NULL);
		if (FAILED(hr))
		{
			Log(hSession, L"Failed to get requested CLR info. Error code 0x%x", hr);
			Log(hSession, L"Ensure that the proper version of the .NET Framework is installed, or "
				L"that there is a matching supportedRuntime element in CustomAction.config. "
				L"If you are binding to .NET 4 or greater add "
				L"useLegacyV2RuntimeActivationPolicy=true to the <startup> element.");
			FreeLibrary(hmodMscoree);
			return false;
		}
	}

	Log(hSession, L"Binding to CLR version %s", szClrVersion);

	ICorRuntimeHost* pHost;
	hr = pCorBindToRuntimeEx(szClrVersion, NULL,
		STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN,
		CLSID_CorRuntimeHost, IID_ICorRuntimeHost, (void**) &pHost);
	if (FAILED(hr))
	{
		Log(hSession, L"Failed to bind to the CLR. Error code 0x%X", hr);
		FreeLibrary(hmodMscoree);
		return false;
	}
	hr = pHost->Start();
	if (FAILED(hr))
	{
		Log(hSession, L"Failed to start the CLR. Error code 0x%X", hr);
		pHost->Release();
		FreeLibrary(hmodMscoree);
		return false;
	}
	*ppHost = pHost;
	FreeLibrary(hmodMscoree);
	return true;
}

/// <summary>
/// Creates a new CLR application domain.
/// </summary>
/// <param name="hSession">Handle to the installer session,
/// used just for logging</param>
/// <param name="pHost">Interface to the runtime host where the
/// app domain will be created.</param>
/// <param name="szName">Name of the app domain to create.</param>
/// <param name="szAppBase">Application base directory path, where
/// the app domain will look first to load its assemblies.</param>
/// <param name="szConfigFile">Optional XML .config file containing any
/// configuration for thae app domain.</param>
/// <param name="ppAppDomain">Returned app domain interface.</param>
/// <returns>True if the app domain was created successfully, false if
/// there was some error.</returns>
bool CreateAppDomain(MSIHANDLE hSession, ICorRuntimeHost* pHost,
	const wchar_t* szName, const wchar_t* szAppBase,
	const wchar_t* szConfigFile, _AppDomain** ppAppDomain)
{
	IUnknown* punkAppDomainSetup = NULL;
	IAppDomainSetup* pAppDomainSetup = NULL;
	HRESULT hr = pHost->CreateDomainSetup(&punkAppDomainSetup);
	if (SUCCEEDED(hr))
	{
		hr = punkAppDomainSetup->QueryInterface(__uuidof(IAppDomainSetup), (void**) &pAppDomainSetup);
		punkAppDomainSetup->Release();
	}
	if (FAILED(hr))
	{
		Log(hSession, L"Failed to create app domain setup. Error code 0x%X", hr);
		return false;
	}

	const wchar_t* szUrlPrefix = L"file:///";
	size_t cchApplicationBase = wcslen(szUrlPrefix) + wcslen(szAppBase);
	wchar_t* szApplicationBase = (wchar_t*) _alloca((cchApplicationBase + 1) * sizeof(wchar_t));
	if (szApplicationBase == NULL) hr = E_OUTOFMEMORY;
	else
	{
		StringCchCopy(szApplicationBase, cchApplicationBase + 1, szUrlPrefix);
		StringCchCat(szApplicationBase, cchApplicationBase + 1, szAppBase);
		BSTR bstrApplicationBase = SysAllocString(szApplicationBase);
		if (bstrApplicationBase == NULL) hr = E_OUTOFMEMORY;
		else
		{
			hr = pAppDomainSetup->put_ApplicationBase(bstrApplicationBase);
			SysFreeString(bstrApplicationBase);
		}
	}

	if (SUCCEEDED(hr) && szConfigFile != NULL)
	{
		BSTR bstrConfigFile = SysAllocString(szConfigFile);
		if (bstrConfigFile == NULL) hr = E_OUTOFMEMORY;
		else
		{
			hr = pAppDomainSetup->put_ConfigurationFile(bstrConfigFile);
			SysFreeString(bstrConfigFile);
		}
	}

	if (FAILED(hr))
	{
		Log(hSession, L"Failed to configure app domain setup. Error code 0x%X", hr);
		pAppDomainSetup->Release();
		return false;
	}

	IUnknown* punkAppDomain;
	hr = pHost->CreateDomainEx(szName, pAppDomainSetup, NULL, &punkAppDomain);
	pAppDomainSetup->Release();
	if (SUCCEEDED(hr))
	{
		hr = punkAppDomain->QueryInterface(__uuidof(_AppDomain), (void**) ppAppDomain);
		punkAppDomain->Release();
	}

	if (FAILED(hr))
	{
		Log(hSession, L"Failed to create app domain. Error code 0x%X", hr);
		return false;
	}

	return true;
}

/// <summary>
/// Locates a specific method in a specific class and assembly.
/// </summary>
/// <param name="hSession">Handle to the installer session,
/// used just for logging</param>
/// <param name="pAppDomain">Application domain in which to
/// load assemblies.</param>
/// <param name="szAssembly">Display name of the assembly
/// containing the method.</param>
/// <param name="szClass">Fully-qualified name of the class
/// containing the method.</param>
/// <param name="szMethod">Name of the method.</param>
/// <param name="ppMethod">Returned method interface.</param>
/// <returns>True if the method was located, otherwise false.</returns>
/// <remarks>Only public static methods are searched. Method
/// parameter types are not considered; if there are multiple
/// matching methods with different parameters, an error results.</remarks>
bool GetMethod(MSIHANDLE hSession, _AppDomain* pAppDomain,
	const wchar_t* szAssembly, const wchar_t* szClass,
	const wchar_t* szMethod, _MethodInfo** ppMethod)
{
	HRESULT hr;
	_Assembly* pAssembly = NULL;
	BSTR bstrAssemblyName = SysAllocString(szAssembly);
	if (bstrAssemblyName == NULL) hr = E_OUTOFMEMORY;
	else
	{
		hr = pAppDomain->Load_2(bstrAssemblyName, &pAssembly);
		SysFreeString(bstrAssemblyName);
	}
	if (FAILED(hr))
	{
		Log(hSession, L"Failed to load assembly %s. Error code 0x%X", szAssembly, hr);
		return false;
	}

	_Type* pType = NULL;
	BSTR bstrClass = SysAllocString(szClass);
	if (bstrClass == NULL) hr = E_OUTOFMEMORY;
	else
	{
		hr = pAssembly->GetType_2(bstrClass, &pType);
		SysFreeString(bstrClass);
	}
	pAssembly->Release();
	if (FAILED(hr) || pType == NULL)
	{
		Log(hSession, L"Failed to load class %s. Error code 0x%X", szClass, hr);
		return false;
	}

	BSTR bstrMethod = SysAllocString(szMethod);
	if (bstrMethod == NULL) hr = E_OUTOFMEMORY;
	else
	{
		hr = pType->GetMethod_2(bstrMethod,
			(BindingFlags) (BindingFlags_Public | BindingFlags_Static), ppMethod);
		SysFreeString(bstrMethod);
	}
	pType->Release();
	if (FAILED(hr) || *ppMethod == NULL)
	{
		Log(hSession, L"Failed to get method %s. Error code 0x%X", szMethod, hr);
		return false;
	}
	return true;
}
