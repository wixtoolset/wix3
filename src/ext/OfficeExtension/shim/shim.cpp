// <copyright file="shim.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  Creative Commons shim implementation.
// </summary>
//
#include "precomp.h"

// global globals
HMODULE vhModule = NULL;


BOOL APIENTRY DllMain(
    __in HMODULE hModule,
    __in DWORD dwReason,
    __in LPVOID lpReserved
    )
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        ::DisableThreadLibraryCalls(hModule);

        vhModule = hModule;
        ClrLoaderInitialize();
        UpdateThreadInitialize();

        break;
    case DLL_PROCESS_DETACH:
        UpdateThreadUninitialize();
        ClrLoaderUninitialize();
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }

    return TRUE;
}


STDAPI DllCanUnloadNow()
{
    return S_OK;
}


STDAPI DllGetClassObject(
    __in const CLSID & rclsid,
    __in const IID & riid,
    __out void ** ppv
    )
{
    HRESULT hr = S_OK;
    IClassFactory *pClassFactory = NULL;

    *ppv = NULL; 

    hr = CreateClassFactory(&pClassFactory);
    ExitOnFailure(hr, "Failed to create class factory.");

    hr = pClassFactory->QueryInterface(riid, ppv);
    ExitOnFailure(hr, "Failed to query interface on class factory.");

LExit:
    ReleaseObject(pClassFactory);
    return hr;
}
