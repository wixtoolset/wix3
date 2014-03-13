// <copyright file="ClrLoader.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  CLR Loader implementation.
// </summary>
//
#include "precomp.h"

// external globals
extern HMODULE vhModule;

// local globals
static CRITICAL_SECTION vClrLoaderLock;
static ICorRuntimeHost* vpHost = NULL;
static _AppDomain* vpLocalDomain = NULL;
static long vcHostedInstances = 0;


// private functions
static HRESULT LoadClr(
    __in_opt LPCWSTR wzClrVersion
    );
static HRESULT CreateLocalAppDomain(
    __in ICorRuntimeHost* pHost,
    __out _AppDomain** ppLocalDomain
    );
static HRESULT GetDllDirectory(
    __out BSTR* pbstrDllDirectory
    );


extern "C" void WINAPI ClrLoaderInitialize()
{
    ::InitializeCriticalSection(&vClrLoaderLock);
    AssertSz(0 == vcHostedInstances, "ClrLoaderInitialize() called but there were already hosted instances.");
}


extern "C" void WINAPI ClrLoaderUninitialize()
{
    AssertSz(0 == vcHostedInstances, "ClrLoaderUnitialize() called but there were still outstanding hosted objects.");
    ::DeleteCriticalSection(&vClrLoaderLock);
}


extern "C" HRESULT WINAPI ClrLoaderCreateInstance(
    __in_opt LPCWSTR wzClrVersion,
    __in LPCWSTR wzAssemblyName,
    __in LPCWSTR wzClassName,
    __in const IID &riid,
    __in void ** ppvObject
    )
{
    HRESULT hr = S_OK;
    BOOL fLocked = FALSE;
    VARIANT v;
    BSTR bstrAssemblyName = NULL;
    BSTR bstrClassName = NULL;
    _ObjectHandle *pObjHandle = NULL;

    ::VariantInit(&v);

    // Ensure the common language runtime is running.
    ::EnterCriticalSection(&vClrLoaderLock);
    fLocked = TRUE;

    if (!vpHost)
    {
        hr = LoadClr(wzClrVersion);
        ExitOnFailure(hr, "Failed to load CLR.");
    }

    // Create an instance of the managed class.
    bstrAssemblyName = ::SysAllocString(wzAssemblyName);
    ExitOnNull1(bstrAssemblyName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR from assembly name: %S", wzAssemblyName);

    bstrClassName = ::SysAllocString(wzClassName);
    ExitOnNull1(bstrClassName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR from class name: %S", wzClassName);

    hr = vpLocalDomain->CreateInstance(bstrAssemblyName, bstrClassName, &pObjHandle); 
    ExitOnFailure2(hr, "Failed to create instance of assembly: %S, class: %S", wzAssemblyName, wzClassName);

    // extract interface pointer from the object handle
    hr = pObjHandle->Unwrap(&v);
    ExitOnFailure(hr, "Failed to unwrap object handle to get interface pointer.");
    ExitOnNull(v.pdispVal, hr, E_UNEXPECTED, "Failed to get IDispatch pointer from CreateInstance");

    hr = v.pdispVal->QueryInterface(riid, ppvObject);
    ExitOnFailure(hr, "Failed to query interface from CreateInstance.");

    ++vcHostedInstances; // We've succeeded, so up the count of hosted instances that need to be destroyed.

LExit:
    if (fLocked)
    {
        ::LeaveCriticalSection(&vClrLoaderLock);
    }

    ReleaseBSTR(bstrClassName);
    ReleaseBSTR(bstrAssemblyName);
    ::VariantClear(&v);

   return hr;
}


extern "C" void WINAPI ClrLoaderDestroyInstance()
{
    ::EnterCriticalSection(&vClrLoaderLock);

    --vcHostedInstances;

    // When we have no more outstanding hosted instances, drop
    // the CLR.
    if (0 >= vcHostedInstances)
    {
        ReleaseNullObject(vpLocalDomain);

        if (vpHost)
        {
            vpHost->Stop();
        }
        ReleaseNullObject(vpHost);
    }

    ::LeaveCriticalSection(&vClrLoaderLock);
}


// Note: This function assumes the CLR loader critical section is locked.
static HRESULT LoadClr(
    __in_opt LPCWSTR wzClrVersion
    )
{
    HRESULT hr = S_OK;
    ICorRuntimeHost* pHost = NULL;
    _AppDomain* pLocalDomain = NULL;

    // Ensure the CLR is only loaded once.
    if (vpHost)
    {
        AssertSz(vpLocalDomain, "CLR host should never be initialized without also creating the local app domain.");
        return S_OK;
    }

    AssertSz(!vpLocalDomain, "Local app domain should never be inititalized without CLR host.");

    // Load default runtime into the process.
#pragma warning(push)
#pragma warning(disable : 4996)
    hr = CorBindToRuntimeEx(wzClrVersion, NULL, STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN | STARTUP_CONCURRENT_GC, CLSID_CorRuntimeHost, IID_ICorRuntimeHost, (PVOID*)&pHost);
#pragma warning(pop)
    if (S_FALSE == hr)
    {
        // The CLR has already been loaded into the process, so just create out local app domain with it.
    }
    else if (SUCCEEDED(hr))
    {
        // The CLR is now loaded into the process, so start it up so we can create our local app domain.
        hr = pHost->Start();
        ExitOnFailure(hr, "Failed to start CLR");
    }
    ExitOnFailure(hr, "Failed to load CLR.");

    // In order to securely load an assembly, its fully qualified strong name
    // and not the filename must be used. To do that, the target AppDomain's 
    // base directory needs to point to the directory where the assembly is
    // residing. CreateLocalAppDomain() ensures that such AppDomain exists.
    hr = CreateLocalAppDomain(pHost, &pLocalDomain);
    ExitOnFailure(hr, "Failed to create local app domain.");

    // Finally, remember all the hard work we've done thus far.
    vpHost = pHost;
    pHost = NULL;

    vpLocalDomain = pLocalDomain;
    pLocalDomain = NULL;

LExit:
    ReleaseObject(pHost);
    ReleaseObject(pLocalDomain);
    return hr;
}


// CreateLocalAppDomain: the function creates AppDomain with BaseDirectory
// set to location of unmanaged DLL containing this code. Assuming that the
// target assembly is located in the same directory, the classes from this
// assemblies can be instantiated by calling _AppDomain::Load() method.
static HRESULT CreateLocalAppDomain(
    __in ICorRuntimeHost* pHost,
    __out _AppDomain** ppLocalDomain
    )
{
    HRESULT hr = S_OK;
    BSTR bstrDllDirectory = NULL;
    IUnknown* pDomainSetupPunk = NULL;
    IAppDomainSetup* pDomainSetup = NULL;
    IUnknown* pLocalDomainPunk = NULL;

    // Get the directory for where the shim exists.
    hr = GetDllDirectory(&bstrDllDirectory);
    ExitOnFailure(hr, "Failed to get dll directory.");

    // Create an AppDomainSetup with the base directory pointing to the
    // location of the managed DLL. The assumption is made that the
    // target assembly is located in the same directory
    hr = pHost->CreateDomainSetup(&pDomainSetupPunk);
    ExitOnFailure(hr, "Failed to create domain setup.");

    hr = pDomainSetupPunk->QueryInterface(__uuidof(pDomainSetup), (LPVOID*)&pDomainSetup);
    ExitOnFailure(hr, "Failed to query for domain setup interface.");

    pDomainSetup->put_ApplicationBase(bstrDllDirectory);

    // Create an AppDomain that will run the managed assembly.
    hr = pHost->CreateDomainEx(bstrDllDirectory, pDomainSetupPunk, 0, &pLocalDomainPunk);
    ExitOnFailure(hr, "Failed to create domain.");

    hr = pLocalDomainPunk->QueryInterface(__uuidof(vpLocalDomain), (LPVOID*)ppLocalDomain);
    ExitOnFailure(hr, "Failed to query for local domain");

LExit:
    ReleaseObject(pLocalDomainPunk);
    ReleaseObject(pDomainSetup);
    ReleaseObject(pDomainSetupPunk);
    ReleaseBSTR(bstrDllDirectory);
    return hr;
}


static HRESULT GetDllDirectory(
    __out BSTR* pbstrDllDirectory
    )
{
    HRESULT hr = S_OK;
    WCHAR wzModule[MAX_PATH + 1] = { 0 };
    WCHAR wzPath[MAX_PATH + 1] = { 0 };
    LPWSTR pwzFileName = NULL;
    DWORD cch = 0;

    if (!vhModule)
    {
        return E_UNEXPECTED;
    }

    cch = ::GetModuleFileNameW(vhModule, wzModule, countof(wzModule));
    if (0 == cch)
    {
        ExitWithLastError(hr, "Failed to get module file name.");
    }

    cch = ::GetFullPathNameW(wzModule, countof(wzPath), wzPath, &pwzFileName);
    if (0 == cch)
    {
        ExitWithLastError(hr, "Failed to convert module path to full path.");
    }
    else if (countof(wzPath) < cch)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        ExitOnFailure(hr, "Failed to converte module path to full path because buffer is too small.");
    }

    *pwzFileName = L'\0'; // chop off the file name from the path.

    *pbstrDllDirectory = ::SysAllocString(wzPath);
    ExitOnNull1(*pbstrDllDirectory, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for DLL directory path: %S", wzPath);

LExit:
    return S_OK;
}
