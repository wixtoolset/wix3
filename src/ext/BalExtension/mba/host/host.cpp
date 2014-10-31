//-------------------------------------------------------------------------------------------------
// <copyright file="host.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Source for the managed bootstrapper application host.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include <BootstrapperCore.h> // includes the generated assembly name macros.

static const DWORD NET452_RELEASE = 379893;

using namespace mscorlib;

extern "C" typedef HRESULT (WINAPI *PFN_CORBINDTOCURRENTRUNTIME)(
    __in LPCWSTR pwszFileName,
    __in REFCLSID rclsid,
    __in REFIID riid,
    __out LPVOID *ppv
    );

extern "C" typedef HRESULT(WINAPI *PFN_MBAPREQ_BOOTSTRAPPER_APPLICATION_CREATE)(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppApplication
    );

static HINSTANCE vhInstance = NULL;
static ICorRuntimeHost *vpCLRHost = NULL;
static HMODULE vhMbapreqModule = NULL;


// internal function declarations

static HRESULT GetAppDomain(
    __out _AppDomain** ppAppDomain
    );
static HRESULT GetAppBase(
    __out LPWSTR* psczAppBase
    );
static HRESULT CheckSupportedFrameworks(
    __in LPCWSTR wzConfigPath
    );
static HRESULT UpdateSupportedRuntime(
    __in IXMLDOMDocument* pixdManifest,
    __in IXMLDOMNode* pixnSupportedFramework,
    __out BOOL* pfUpdatedManifest
    );
static HRESULT GetCLRHost(
    __in LPCWSTR wzConfigPath,
    __out ICorRuntimeHost** ppCLRHost
    );
static HRESULT CreateManagedBootstrapperApplication(
    __in _AppDomain* pAppDomain,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppApplication
    );
static HRESULT CreateManagedBootstrapperApplicationFactory(
    __in _AppDomain* pAppDomain,
    __out IBootstrapperApplicationFactory** ppAppFactory
    );
static HRESULT CreatePrerequisiteBA(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppApplication
    );
static HRESULT VerifyNET4RuntimeIsSupported(
    );


// function definitions

extern "C" BOOL WINAPI DllMain(
    IN HINSTANCE hInstance,
    IN DWORD dwReason,
    IN LPVOID /* pvReserved */
    )
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        ::DisableThreadLibraryCalls(hInstance);
        vhInstance = hInstance;
        break;

    case DLL_PROCESS_DETACH:
        vhInstance = NULL;
        break;
    }

    return TRUE;
}

// Note: This function assumes that COM was already initialized on the thread.
extern "C" HRESULT WINAPI BootstrapperApplicationCreate(
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppBA
    )
{
    HRESULT hr = S_OK; 
    HRESULT hrHostInitialization = S_OK;
    _AppDomain* pAppDomain = NULL;

    BalInitialize(pEngine);

    hr = GetAppDomain(&pAppDomain);
    if (SUCCEEDED(hr))
    {
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Loading managed bootstrapper application.");

        hr = CreateManagedBootstrapperApplication(pAppDomain, pEngine, pCommand, ppBA);
        BalExitOnFailure(hr, "Failed to create the managed bootstrapper application.");
    }
    else // fallback to the prerequisite BA.
    {
        if (E_MBAHOST_NET452_ON_WIN7RTM == hr)
        {
            BalLogError(hr, "The Burn engine cannot run with an MBA under the .NET 4 CLR on Windows 7 RTM with .NET 4.5.2 (or greater) installed.");
            hrHostInitialization = hr;
        }
        else
        {
            hrHostInitialization = S_OK;
        }

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Loading prerequisite bootstrapper application because managed host could not be loaded, error: 0x%08x.", hr);

        hr = CreatePrerequisiteBA(hrHostInitialization, pEngine, pCommand, ppBA);
        BalExitOnFailure(hr, "Failed to create the pre-requisite bootstrapper application.");
    }

LExit:
    ReleaseObject(pAppDomain);

    return hr;
}

extern "C" void WINAPI BootstrapperApplicationDestroy()
{
    ReleaseNullObject(vpCLRHost);

    if (vhMbapreqModule)
    {
        PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = reinterpret_cast<PFN_BOOTSTRAPPER_APPLICATION_DESTROY>(::GetProcAddress(vhMbapreqModule, "MbaPrereqBootstrapperApplicationDestroy"));
        if (pfnDestroy)
        {
            (*pfnDestroy)();
        }

        ::FreeLibrary(vhMbapreqModule);
        vhMbapreqModule = NULL;
    }

    BalUninitialize();
}

// Gets the custom AppDomain for loading managed BA.
static HRESULT GetAppDomain(
    __out _AppDomain **ppAppDomain
    )
{
    HRESULT hr = S_OK;
    ICorRuntimeHost *pCLRHost = NULL;
    IUnknown *pUnk = NULL;
    LPWSTR sczAppBase = NULL;
    LPWSTR sczConfigPath = NULL;
    IAppDomainSetup *pAppDomainSetup;
    BSTR bstrAppBase = NULL;
    BSTR bstrConfigPath = NULL;

    hr = GetAppBase(&sczAppBase);
    ExitOnFailure(hr, "Failed to get the host base path.");

    hr = PathConcat(sczAppBase, L"BootstrapperCore.config", &sczConfigPath);
    ExitOnFailure(hr, "Failed to get the full path to the application configuration file.");

    // Check that the supported framework is installed.
    hr = CheckSupportedFrameworks(sczConfigPath);
    ExitOnFailure(hr, "Failed to find supported framework.");

    // Load the CLR.
    hr = GetCLRHost(sczConfigPath, &pCLRHost);
    ExitOnFailure(hr, "Failed to create the CLR host.");

    hr = pCLRHost->Start();
    ExitOnRootFailure(hr, "Failed to start the CLR host.");

    // Create the setup information for a new AppDomain to set the app base and config.
    hr = pCLRHost->CreateDomainSetup(&pUnk);
    ExitOnRootFailure(hr, "Failed to create the AppDomainSetup object.");

    hr = pUnk->QueryInterface(__uuidof(IAppDomainSetup), reinterpret_cast<LPVOID*>(&pAppDomainSetup));
    ExitOnRootFailure(hr, "Failed to query for the IAppDomainSetup interface.");
    ReleaseNullObject(pUnk);

    // Set properties on the AppDomainSetup object.
    bstrAppBase = ::SysAllocString(sczAppBase);
    ExitOnNull(bstrAppBase, hr, E_OUTOFMEMORY, "Failed to allocate the application base path for the AppDomainSetup.");

    hr = pAppDomainSetup->put_ApplicationBase(bstrAppBase);
    ExitOnRootFailure(hr, "Failed to set the application base path for the AppDomainSetup.");

    bstrConfigPath = ::SysAllocString(sczConfigPath);
    ExitOnNull(bstrConfigPath, hr, E_OUTOFMEMORY, "Failed to allocate the application configuration file for the AppDomainSetup.");

    hr = pAppDomainSetup->put_ConfigurationFile(bstrConfigPath);
    ExitOnRootFailure(hr, "Failed to set the configuration file path for the AppDomainSetup.");

    // Create the AppDomain to load the factory type.
    hr = pCLRHost->CreateDomainEx(L"MBA", pAppDomainSetup, NULL, &pUnk);
    ExitOnRootFailure(hr, "Failed to create the MBA AppDomain.");

    hr = pUnk->QueryInterface(__uuidof(_AppDomain), reinterpret_cast<LPVOID*>(ppAppDomain));
    ExitOnRootFailure(hr, "Failed to query for the _AppDomain interface.");

LExit:
    ReleaseBSTR(bstrConfigPath);
    ReleaseBSTR(bstrAppBase);
    ReleaseStr(sczConfigPath);
    ReleaseStr(sczAppBase);
    ReleaseNullObject(pUnk);
    ReleaseNullObject(pCLRHost);

    return hr;
}

static HRESULT GetAppBase(
    __out LPWSTR *psczAppBase
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFullPath = NULL;

    hr = PathForCurrentProcess(&sczFullPath, vhInstance);
    ExitOnFailure(hr, "Failed to get the full host path.");

    hr = PathGetDirectory(sczFullPath, psczAppBase);
    ExitOnFailure(hr, "Failed to get the directory of the full process path.");

LExit:
    ReleaseStr(sczFullPath);

    return hr;
}

// Checks whether at least one of required supported frameworks is installed via the NETFX registry keys.
static HRESULT CheckSupportedFrameworks(
    __in LPCWSTR wzConfigPath
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixdManifest = NULL;
    IXMLDOMNodeList* pNodeList = NULL;
    IXMLDOMNode* pNode = NULL;
    DWORD cSupportedFrameworks = 0;
    LPWSTR sczSupportedFrameworkVersion = NULL;
    LPWSTR sczFrameworkRegistryKey = NULL;
    HKEY hkFramework = NULL;
    DWORD dwFrameworkInstalled = 0;
    BOOL fUpdatedManifest = FALSE;

    hr = XmlInitialize();
    ExitOnFailure(hr, "Failed to initialize XML.");

    hr = XmlLoadDocumentFromFile(wzConfigPath, &pixdManifest);
    ExitOnFailure1(hr, "Failed to load bootstrapper config file from path: %ls", wzConfigPath);

    hr = XmlSelectNodes(pixdManifest, L"/configuration/wix.bootstrapper/host/supportedFramework", &pNodeList);
    ExitOnFailure(hr, "Failed to select all supportedFramework elements.");

    hr = pNodeList->get_length(reinterpret_cast<long*>(&cSupportedFrameworks));
    ExitOnFailure(hr, "Failed to get the supported framework count.");

    if (cSupportedFrameworks)
    {
        while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, NULL)))
        {
            hr = XmlGetAttributeEx(pNode, L"version", &sczSupportedFrameworkVersion);
            ExitOnFailure(hr, "Failed to get supportedFramework/@version.");

            hr = StrAllocFormatted(&sczFrameworkRegistryKey, L"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\%ls", sczSupportedFrameworkVersion);
            ExitOnFailure(hr, "Failed to allocate path to supported framework Install registry key.");

            hr = RegOpen(HKEY_LOCAL_MACHINE, sczFrameworkRegistryKey, KEY_READ, &hkFramework);
            if (SUCCEEDED(hr))
            {
                hr = RegReadNumber(hkFramework, L"Install", &dwFrameworkInstalled);
                if (dwFrameworkInstalled)
                {
                    hr = S_OK;
                    break;
                }
            }

            ReleaseNullObject(pNode);
        }

        // If we looped through all the supported frameworks but didn't find anything, ensure we return a failure.
        if (S_FALSE == hr)
        {
            hr = E_NOTFOUND;
            ExitOnRootFailure(hr, "Failed to find a supported framework.");
        }

        hr = UpdateSupportedRuntime(pixdManifest, pNode, &fUpdatedManifest);
        ExitOnFailure(hr, "Failed to update supportedRuntime.");
    }
    // else no supported frameworks specified, so the startup/supportedRuntime must be enough.

    if (fUpdatedManifest)
    {
        hr = XmlSaveDocument(pixdManifest, wzConfigPath);
        ExitOnFailure1(hr, "Failed to save updated manifest over config file: %ls", wzConfigPath);
    }

LExit:
    ReleaseRegKey(hkFramework);
    ReleaseStr(sczFrameworkRegistryKey);
    ReleaseStr(sczSupportedFrameworkVersion);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);
    ReleaseObject(pixdManifest);

    XmlUninitialize();

    return hr;
}

// Fixes the supportedRuntime element if necessary.
static HRESULT UpdateSupportedRuntime(
    __in IXMLDOMDocument* pixdManifest,
    __in IXMLDOMNode* pixnSupportedFramework,
    __out BOOL* pfUpdatedManifest
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSupportedRuntimeVersion = NULL;
    IXMLDOMNode* pixnStartup = NULL;
    IXMLDOMNode* pixnSupportedRuntime = NULL;

    *pfUpdatedManifest = FALSE;

    // If the runtime version attribute is not specified, don't update the manifest.
    hr = XmlGetAttributeEx(pixnSupportedFramework, L"runtimeVersion", &sczSupportedRuntimeVersion);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to get supportedFramework/@runtimeVersion.");

    // Get the startup element. Fail if we can't find it since it'll be necessary to load the
    // correct runtime.
    hr = XmlSelectSingleNode(pixdManifest, L"/configuration/startup", &pixnStartup);
    ExitOnFailure(hr, "Failed to get startup element.");

    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
        ExitOnRootFailure(hr, "Failed to find startup element in bootstrapper application config.");
    }

    // Remove any pre-existing supported runtimes because they'll just get in the way and create our new one.
    hr = XmlRemoveChildren(pixnStartup, L"supportedRuntime");
    ExitOnFailure(hr, "Failed to remove pre-existing supportedRuntime elements.");

    hr = XmlCreateChild(pixnStartup, L"supportedRuntime", &pixnSupportedRuntime);
    ExitOnFailure(hr, "Failed to create supportedRuntime element.");

    hr = XmlSetAttribute(pixnSupportedRuntime, L"version", sczSupportedRuntimeVersion);
    ExitOnFailure1(hr, "Failed to set supportedRuntime/@version to '%ls'.", sczSupportedRuntimeVersion);

    *pfUpdatedManifest = TRUE;

LExit:
    ReleaseObject(pixnSupportedRuntime);
    ReleaseObject(pixnStartup);
    ReleaseStr(sczSupportedRuntimeVersion);

    return hr;
}

// Gets the CLR host and caches it.
static HRESULT GetCLRHost(
    __in LPCWSTR wzConfigPath,
    __out ICorRuntimeHost **ppCLRHost
    )
{
    HRESULT hr = S_OK;
    UINT uiMode = 0;
    HMODULE hModule = NULL;
    CLRCreateInstanceFnPtr pfnCLRCreateInstance = NULL;
    ICLRMetaHostPolicy* pCLRMetaHostPolicy = NULL;
    IStream* pCfgStream = NULL;
    LPWSTR pwzVersion = NULL;
    DWORD cchVersion = 0;
    ICLRRuntimeInfo* pCLRRuntimeInfo = NULL;
    PFN_CORBINDTOCURRENTRUNTIME pfnCorBindToCurrentRuntime = NULL;

    // Always set the error mode because we will always restore it below.
    uiMode = ::SetErrorMode(0);

    // Cache the CLR host to be shutdown later. This can occur on a different thread.
    if (!vpCLRHost)
    {
        // Disable message boxes from being displayed on error and blocking execution.
        ::SetErrorMode(uiMode | SEM_FAILCRITICALERRORS);

        hr = LoadSystemLibrary(L"mscoree.dll", &hModule);
        ExitOnFailure(hr, "Failed to load mscoree.dll");

        pfnCLRCreateInstance = reinterpret_cast<CLRCreateInstanceFnPtr>(::GetProcAddress(hModule, "CLRCreateInstance"));
        
        if (!pfnCLRCreateInstance)
        {
            pfnCorBindToCurrentRuntime = reinterpret_cast<PFN_CORBINDTOCURRENTRUNTIME>(::GetProcAddress(hModule, "CorBindToCurrentRuntime"));
            ExitOnNullWithLastError(pfnCorBindToCurrentRuntime, hr, "Failed to get procedure address for CorBindToCurrentRuntime.");

            hr = pfnCorBindToCurrentRuntime(wzConfigPath, CLSID_CorRuntimeHost, IID_ICorRuntimeHost, reinterpret_cast<LPVOID*>(&vpCLRHost));
            ExitOnRootFailure(hr, "Failed to create the CLR host using the application configuration file path.");
        }
        else
        {
            hr = pfnCLRCreateInstance(CLSID_CLRMetaHostPolicy, IID_ICLRMetaHostPolicy, reinterpret_cast<LPVOID*>(&pCLRMetaHostPolicy));
            ExitOnRootFailure(hr, "Failed to create instance of ICLRMetaHostPolicy.");

            hr = SHCreateStreamOnFileEx(wzConfigPath, STGM_READ | STGM_SHARE_DENY_WRITE, 0, FALSE, NULL, &pCfgStream);
            ExitOnFailure1(hr, "Failed to load bootstrapper config file from path: %ls", wzConfigPath);

            hr = pCLRMetaHostPolicy->GetRequestedRuntime(METAHOST_POLICY_HIGHCOMPAT, NULL, pCfgStream, NULL, &cchVersion, NULL, NULL, NULL, IID_ICLRRuntimeInfo, reinterpret_cast<LPVOID*>(&pCLRRuntimeInfo));
            ExitOnRootFailure(hr, "Failed to get the CLR runtime info using the application configuration file path.");

            // .NET 4 RTM had a bug where it wouldn't set pcchVersion if pwzVersion was NULL.
            if (!cchVersion)
            {
                hr = pCLRRuntimeInfo->GetVersionString(NULL, &cchVersion);
                if (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) != hr)
                {
                    ExitOnFailure(hr, "Failed to get the length of the CLR version string.");
                }
            }

            hr = StrAlloc(&pwzVersion, cchVersion);
            ExitOnFailure(hr, "Failed to allocate the CLR version string.");

            hr = pCLRRuntimeInfo->GetVersionString(pwzVersion, &cchVersion);
            ExitOnFailure(hr, "Failed to get the CLR version string.");

            if (CSTR_EQUAL == CompareString(LOCALE_NEUTRAL, 0, L"v4.0.30319", -1, pwzVersion, cchVersion))
            {
                hr = VerifyNET4RuntimeIsSupported();
                ExitOnFailure(hr, "Found unsupported .NET 4 Runtime.");
            }

            hr = pCLRRuntimeInfo->GetInterface(CLSID_CorRuntimeHost, IID_ICorRuntimeHost, reinterpret_cast<LPVOID*>(&vpCLRHost));
            ExitOnRootFailure(hr, "Failed to get instance of ICorRuntimeHost.");

            // TODO: use ICLRRuntimeHost instead of ICorRuntimeHost on .NET 4 since the former is faster and the latter is deprecated
            //hr = pCLRRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, reinterpret_cast<LPVOID*>(&pCLRRuntimeHost));
            //ExitOnRootFailure(hr, "Failed to get instance of ICLRRuntimeHost.");
        }
    }

    vpCLRHost->AddRef();
    *ppCLRHost = vpCLRHost;

LExit:
    ReleaseStr(pwzVersion);
    ReleaseNullObject(pCLRRuntimeInfo);
    ReleaseNullObject(pCfgStream);
    ReleaseNullObject(pCLRMetaHostPolicy);

    // Unload the module so it's not in use when we install .NET.
    if (FAILED(hr))
    {
        ::FreeLibrary(hModule);
    }

    ::SetErrorMode(uiMode); // restore the previous error mode.

    return hr;
}

// Creates the bootstrapper app and returns it for the engine.
static HRESULT CreateManagedBootstrapperApplication(
    __in _AppDomain* pAppDomain,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppApplication
    )
{
    HRESULT hr = S_OK;
    IBootstrapperApplicationFactory* pAppFactory = NULL;

    hr = CreateManagedBootstrapperApplicationFactory(pAppDomain, &pAppFactory);
    ExitOnFailure(hr, "Failed to create the factory to create the bootstrapper application.");

    hr = pAppFactory->Create(pEngine, pCommand, ppApplication);
    ExitOnFailure(hr, "Failed to create the bootstrapper application.");

LExit:
    ReleaseNullObject(pAppFactory);

    return hr;
}

// Creates the app factory to create the managed app in the default AppDomain.
static HRESULT CreateManagedBootstrapperApplicationFactory(
    __in _AppDomain* pAppDomain,
    __out IBootstrapperApplicationFactory** ppAppFactory
    )
{
    HRESULT hr = S_OK;
    BSTR bstrAssemblyName = NULL;
    BSTR bstrTypeName = NULL;
    _ObjectHandle* pObj = NULL;
    VARIANT vtBAFactory;

    ::VariantInit(&vtBAFactory);

    bstrAssemblyName = ::SysAllocString(MUX_ASSEMBLY_FULL_NAME);
    ExitOnNull(bstrAssemblyName, hr, E_OUTOFMEMORY, "Failed to allocate the full assembly name for the bootstrapper application factory.");

    bstrTypeName = ::SysAllocString(L"Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplicationFactory");
    ExitOnNull(bstrTypeName, hr, E_OUTOFMEMORY, "Failed to allocate the full type name for the BA factory.");

    hr = pAppDomain->CreateInstance(bstrAssemblyName, bstrTypeName, &pObj);
    ExitOnRootFailure(hr, "Failed to create the BA factory object.");

    hr = pObj->Unwrap(&vtBAFactory);
    ExitOnRootFailure(hr, "Failed to unwrap the BA factory object into the host domain.");
    ExitOnNull(vtBAFactory.punkVal, hr, E_UNEXPECTED, "The variant did not contain the expected IUnknown pointer.");

    hr = vtBAFactory.punkVal->QueryInterface(__uuidof(IBootstrapperApplicationFactory), reinterpret_cast<LPVOID*>(ppAppFactory));
    ExitOnRootFailure(hr, "Failed to query for the bootstrapper app factory interface.");

LExit:
    ReleaseVariant(vtBAFactory);
    ReleaseNullObject(pObj);
    ReleaseBSTR(bstrTypeName);
    ReleaseBSTR(bstrAssemblyName);

    return hr;
}

static HRESULT CreatePrerequisiteBA(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppApplication
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczMbapreqPath = NULL;
    HMODULE hModule = NULL;
    IBootstrapperApplication* pApp = NULL;

    hr = PathRelativeToModule(&sczMbapreqPath, L"mbapreq.dll", vhInstance);
    ExitOnFailure(hr, "Failed to get path to pre-requisite BA.");

    hModule = ::LoadLibraryW(sczMbapreqPath);
    ExitOnNullWithLastError(hModule, hr, "Failed to load pre-requisite BA DLL.");

    PFN_MBAPREQ_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = reinterpret_cast<PFN_MBAPREQ_BOOTSTRAPPER_APPLICATION_CREATE>(::GetProcAddress(hModule, "MbaPrereqBootstrapperApplicationCreate"));
    ExitOnNullWithLastError1(pfnCreate, hr, "Failed to get MbaPrereqBootstrapperApplicationCreate entry-point from: %ls", sczMbapreqPath);

    hr = pfnCreate(hrHostInitialization, pEngine, pCommand, &pApp);
    ExitOnFailure(hr, "Failed to create prequisite bootstrapper app.");

    vhMbapreqModule = hModule;
    hModule = NULL;

    *ppApplication = pApp;
    pApp = NULL;

LExit:
    ReleaseObject(pApp);
    if (hModule)
    {
        ::FreeLibrary(hModule);
    }
    ReleaseStr(sczMbapreqPath);

    return hr;
}

static HRESULT VerifyNET4RuntimeIsSupported(
    )
{
    HRESULT hr = S_OK;
    OS_VERSION osv = OS_VERSION_UNKNOWN;
    DWORD dwServicePack = 0;
    HKEY hKey = NULL;
    DWORD er = ERROR_SUCCESS;
    DWORD dwRelease = 0;
    DWORD cchRelease = sizeof(dwRelease);

    OsGetVersion(&osv, &dwServicePack);
    if (OS_VERSION_WIN7 == osv && 0 == dwServicePack)
    {
        hr = RegOpen(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full", KEY_QUERY_VALUE, &hKey);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        ExitOnFailure(hr, "Failed to open registry key for .NET 4.");

        er = ::RegQueryValueExW(hKey, L"Release", NULL, NULL, reinterpret_cast<LPBYTE>(&dwRelease), &cchRelease);
        if (ERROR_FILE_NOT_FOUND == er)
        {
            ExitFunction1(hr = S_OK);
        }
        ExitOnWin32Error(er, hr, "Failed to get Release value.");

        if (NET452_RELEASE <= dwRelease)
        {
            hr = E_MBAHOST_NET452_ON_WIN7RTM;
        }
    }

LExit:
    ReleaseRegKey(hKey);

    return hr;
}
