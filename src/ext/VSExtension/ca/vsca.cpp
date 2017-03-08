// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

typedef HRESULT (WINAPI *PFN_PROCESS_INSTANCE)(
    __in_opt ISetupInstance* pInstance,
    __in DWORD64 qwVersion,
    __in BOOL fComplete
    );

struct VS_INSTANCE
{
    DWORD64 qwMinVersion;
    DWORD64 qwMaxVersion;
    PFN_PROCESS_INSTANCE pfnProcessInstance;
};

struct VS_COMPONENT_PROPERTY
{
    LPCWSTR pwzComponent;
    LPCWSTR pwzProperty;
};

static HRESULT InstanceIsGreater(
    __in_opt ISetupInstance* pPreviousInstance,
    __in DWORD64 qwPreviousVersion,
    __in ISetupInstance* pCurrentInstance,
    __in DWORD64 qwCurrentVersion
    );

static HRESULT ProcessInstance(
    __in ISetupInstance* pInstance,
    __in LPCWSTR wzProperty,
    __in DWORD cComponents,
    __in VS_COMPONENT_PROPERTY* rgComponents
    );

static HRESULT ProcessVS2017(
    __in_opt ISetupInstance* pInstance,
    __in DWORD64 qwVersion,
    __in BOOL fComplete
    );

static HRESULT SetPropertyForComponent(
    __in DWORD cComponents,
    __in VS_COMPONENT_PROPERTY* rgComponents,
    __in LPCWSTR wzComponent
    );

static VS_INSTANCE vrgInstances[] =
{
    { FILEMAKEVERSION(15, 0, 0, 0), FILEMAKEVERSION(15, 0xffff, 0xffff, 0xffff), ProcessVS2017 },
};

/******************************************************************
 FindInstances - entry point for VS custom action to find instances

*******************************************************************/
extern "C" UINT __stdcall FindInstances(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    BOOL fComInitialized = FALSE;
    ISetupConfiguration* pConfiguration = NULL;
    ISetupHelper* pHelper = NULL;
    IEnumSetupInstances* pEnumInstances = NULL;
    ISetupInstance* rgpInstances[1] = {};
    ISetupInstance* pInstance = NULL;
    ULONG cInstancesFetched = 0;
    BSTR bstrVersion = NULL;
    DWORD64 qwVersion = 0;

    hr = WcaInitialize(hInstall, "VSFindInstances");
    ExitOnFailure(hr, "Failed to initialize custom action.");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM.");

    fComInitialized = TRUE;

    hr = ::CoCreateInstance(__uuidof(SetupConfiguration), NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pConfiguration));
    if (REGDB_E_CLASSNOTREG != hr)
    {
        ExitOnFailure(hr, "Failed to initialize setup configuration class.");
    }
    else
    {
        WcaLog(LOGMSG_VERBOSE, "Setup configuration not registered; assuming no instances installed.");

        hr = S_OK;
        ExitFunction();
    }

    hr = pConfiguration->QueryInterface(IID_PPV_ARGS(&pHelper));
    if (FAILED(hr))
    {
        WcaLog(LOGMSG_VERBOSE, "Setup configuration helpers not implemented; assuming Visual Studio 2017.");

        qwVersion = FILEMAKEVERSION(15, 0, 0, 0);
        hr = S_OK;
    }

    hr = pConfiguration->EnumInstances(&pEnumInstances);
    ExitOnFailure(hr, "Failed to get instance enumerator.");

    do
    {
        hr = pEnumInstances->Next(1, rgpInstances, &cInstancesFetched);
        if (SUCCEEDED(hr) && cInstancesFetched)
        {
            pInstance = rgpInstances[0];
            if (pInstance)
            {
                if (pHelper)
                {
                    hr = pInstance->GetInstallationVersion(&bstrVersion);
                    ExitOnFailure(hr, "Failed to get installation version.");

                    hr = pHelper->ParseVersion(bstrVersion, &qwVersion);
                    ExitOnFailure(hr, "Failed to parse installation version.");
                }

                for (DWORD i = 0; i < countof(vrgInstances); ++i)
                {
                    const VS_INSTANCE* pElem = &vrgInstances[i];

                    if (pElem->qwMinVersion <= qwVersion && qwVersion <= pElem->qwMaxVersion)
                    {
                        hr = pElem->pfnProcessInstance(pInstance, qwVersion, FALSE);
                        ExitOnFailure(hr, "Failed to process instance.");
                    }
                }
            }

            ReleaseNullBSTR(bstrVersion);
            ReleaseNullObject(pInstance);
        }
    } while (SUCCEEDED(hr) && cInstancesFetched);

    // Complete all registered processing functions.
    for (DWORD i = 0; i < countof(vrgInstances); ++i)
    {
        const VS_INSTANCE* pElem = &vrgInstances[i];

        if (pElem->qwMinVersion <= qwVersion && qwVersion <= pElem->qwMaxVersion)
        {
            hr = pElem->pfnProcessInstance(NULL, 0, TRUE);
            ExitOnFailure(hr, "Failed to process latest instance.");
        }
    }

LExit:
    ReleaseBSTR(bstrVersion);
    ReleaseObject(pInstance);
    ReleaseObject(pEnumInstances);
    ReleaseObject(pHelper);
    ReleaseObject(pConfiguration);

    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}

static HRESULT InstanceIsGreater(
    __in_opt ISetupInstance* pPreviousInstance,
    __in DWORD64 qwPreviousVersion,
    __in ISetupInstance* pCurrentInstance,
    __in DWORD64 qwCurrentVersion
    )
{
    HRESULT hr = S_OK;
    FILETIME ftPreviousInstance = {};
    FILETIME ftCurrentInstance = {};

    if (qwPreviousVersion != qwCurrentVersion)
    {
        return qwPreviousVersion < qwCurrentVersion ? S_OK : S_FALSE;
    }

    hr = pPreviousInstance->GetInstallDate(&ftPreviousInstance);
    ExitOnFailure(hr, "Failed to get previous install date.");

    hr = pCurrentInstance->GetInstallDate(&ftCurrentInstance);
    ExitOnFailure(hr, "Failed to get current install date.");

    return 0 > ::CompareFileTime(&ftPreviousInstance, &ftCurrentInstance) ? S_OK : S_FALSE;

LExit:
    return hr;
}

static HRESULT ProcessInstance(
    __in ISetupInstance* pInstance,
    __in LPCWSTR wzProperty,
    __in DWORD cComponents,
    __in VS_COMPONENT_PROPERTY* rgComponents
    )
{
    HRESULT hr = S_OK;
    ISetupInstance2* pInstance2 = NULL;
    BSTR bstrPath = NULL;
    LPSAFEARRAY psaPackages = NULL;
    LONG lPackageIndex = 0;
    LONG clMaxPackages = 0;
    ISetupPackageReference** rgpPackages = NULL;
    ISetupPackageReference* pPackage = NULL;
    BSTR bstrPackageId = NULL;

    hr = pInstance->GetInstallationPath(&bstrPath);
    ExitOnFailure(hr, "Failed to get installation path.");

    hr = WcaSetProperty(wzProperty, bstrPath);
    ExitOnFailure(hr, "Failed to set installation path property: %ls", wzProperty);

    hr = pInstance->QueryInterface(IID_PPV_ARGS(&pInstance2));
    if (FAILED(hr))
    {
        // Older implementation did not expose installed components.
        hr = S_OK;
        ExitFunction();
    }

    hr = pInstance2->GetPackages(&psaPackages);
    ExitOnFailure(hr, "Failed to get packages from instance.");

    hr = ::SafeArrayGetLBound(psaPackages, 1, &lPackageIndex);
    ExitOnFailure(hr, "Failed to get lower bound of packages array.");

    hr = ::SafeArrayGetUBound(psaPackages, 1, &clMaxPackages);
    ExitOnFailure(hr, "Failed to get upper bound of packages array.");

    // Faster access to single dimension SAFEARRAY elements.
    hr = ::SafeArrayAccessData(psaPackages, reinterpret_cast<LPVOID*>(&rgpPackages));
    ExitOnFailure(hr, "Failed to access packages array.")

    for (; lPackageIndex <= clMaxPackages; ++lPackageIndex)
    {
        pPackage = rgpPackages[lPackageIndex];

        if (pPackage)
        {
            hr = pPackage->GetId(&bstrPackageId);
            ExitOnFailure(hr, "Failed to get package ID.");

            hr = SetPropertyForComponent(cComponents, rgComponents, bstrPackageId);
            ExitOnFailure(hr, "Failed to set property for component: %ls", bstrPackageId);

            ReleaseNullBSTR(bstrPackageId);
        }
    }

LExit:
    ReleaseBSTR(bstrPackageId);

    if (rgpPackages)
    {
        ::SafeArrayUnaccessData(psaPackages);
    }

    if (psaPackages)
    {
        // This will Release() all objects in the array.
        ::SafeArrayDestroy(psaPackages);
    }

    ReleaseObject(pInstance2);
    ReleaseBSTR(bstrPath);

    return hr;
}

static HRESULT ProcessVS2017(
    __in_opt ISetupInstance* pInstance,
    __in DWORD64 qwVersion,
    __in BOOL fComplete
    )
{
    static ISetupInstance* pLatest = NULL;
    static DWORD64 qwLatest = 0;

    // TODO: Consider making table-driven with these defaults per-version for easy customization.
    static VS_COMPONENT_PROPERTY rgComponents[] =
    {
        { L"Microsoft.VisualStudio.Component.FSharp", L"VS2017_IDE_FSHARP_PROJECTSYSTEM_INSTALLED" },
        { L"Microsoft.VisualStudio.Component.Roslyn.LanguageServices", L"VS2017_IDE_VB_PROJECTSYSTEM_INSTALLED" },
        { L"Microsoft.VisualStudio.Component.Roslyn.LanguageServices", L"VS2017_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED" },
        { L"Microsoft.VisualStudio.Component.TestTools.Core", L"VS2017_IDE_VSTS_TESTSYSTEM_INSTALLED" },
        { L"Microsoft.VisualStudio.Component.VC.CoreIde", L"VS2017_IDE_VC_PROJECTSYSTEM_INSTALLED" },
        { L"Microsoft.VisualStudio.Component.Web", L"VS2017_IDE_VWD_PROJECTSYSTEM_INSTALLED" },
        { L"Microsoft.VisualStudio.PackageGroup.DslRuntime", L"VS2017_IDE_MODELING_PROJECTSYSTEM_INSTALLED" },
    };

    HRESULT hr = S_OK;

    if (fComplete)
    {
        if (pLatest)
        {
            hr = ProcessInstance(pLatest, L"VS2017_ROOT_FOLDER", countof(rgComponents), rgComponents);
            ExitOnFailure(hr, "Failed to process VS2017 instance.");
        }
    }
    else if (pInstance)
    {
        hr = InstanceIsGreater(pLatest, qwLatest, pInstance, qwVersion);
        ExitOnFailure(hr, "Failed to compare instances.");

        if (S_OK == hr)
        {
            ReleaseNullObject(pLatest);

            pLatest = pInstance;
            qwLatest = qwVersion;

            // Caller will do a final Release() otherwise.
            pLatest->AddRef();
        }
    }

LExit:
    if (fComplete)
    {
        ReleaseObject(pLatest);
    }

    return hr;
}

static HRESULT SetPropertyForComponent(
    __in DWORD cComponents,
    __in VS_COMPONENT_PROPERTY* rgComponents,
    __in LPCWSTR wzComponent
    )
{
    HRESULT hr = S_OK;

    // For small arrays, faster looping through than hashing. There may also be duplicates like with VS2017.
    for (DWORD i = 0; i < cComponents; ++i)
    {
        const VS_COMPONENT_PROPERTY* pComponent = &rgComponents[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pComponent->pwzComponent, -1, wzComponent, -1))
        {
            hr = WcaSetIntProperty(pComponent->pwzProperty, 1);
            ExitOnFailure(hr, "Failed to set property: %ls", pComponent->pwzProperty);
        }
    }

LExit:
    return hr;
}
