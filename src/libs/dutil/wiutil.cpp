//-------------------------------------------------------------------------------------------------
// <copyright file="wiutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Windows Installer helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// constants

const DWORD WIU_MSI_PROGRESS_INVALID = 0xFFFFFFFF;
const DWORD WIU_GOOD_ENOUGH_PROPERTY_LENGTH = 64;


// structs


static PFN_MSIENABLELOGW vpfnMsiEnableLogW = ::MsiEnableLogW;
static PFN_MSIGETPRODUCTINFOW vpfnMsiGetProductInfoW = ::MsiGetProductInfoW;
static PFN_MSIQUERYFEATURESTATEW vpfnMsiQueryFeatureStateW = ::MsiQueryFeatureStateW;
static PFN_MSIGETCOMPONENTPATHW vpfnMsiGetComponentPathW = ::MsiGetComponentPathW;
static PFN_MSILOCATECOMPONENTW vpfnMsiLocateComponentW = ::MsiLocateComponentW;
static PFN_MSIINSTALLPRODUCTW vpfnMsiInstallProductW = ::MsiInstallProductW;
static PFN_MSICONFIGUREPRODUCTEXW vpfnMsiConfigureProductExW = ::MsiConfigureProductExW;
static PFN_MSIREMOVEPATCHESW vpfnMsiRemovePatchesW = ::MsiRemovePatchesW;
static PFN_MSISETINTERNALUI vpfnMsiSetInternalUI = ::MsiSetInternalUI;
static PFN_MSISETEXTERNALUIW vpfnMsiSetExternalUIW = ::MsiSetExternalUIW;
static PFN_MSIENUMPRODUCTSW vpfnMsiEnumProductsW = ::MsiEnumProductsW;
static PFN_MSIENUMRELATEDPRODUCTSW vpfnMsiEnumRelatedProductsW = ::MsiEnumRelatedProductsW;

// MSI 3.0+
static PFN_MSIDETERMINEPATCHSEQUENCEW vpfnMsiDeterminePatchSequenceW = NULL;
static PFN_MSIENUMPRODUCTSEXW vpfnMsiEnumProductsExW = NULL;
static PFN_MSIGETPATCHINFOEXW vpfnMsiGetPatchInfoExW = NULL;
static PFN_MSIGETPRODUCTINFOEXW vpfnMsiGetProductInfoExW = NULL;
static PFN_MSISETEXTERNALUIRECORD vpfnMsiSetExternalUIRecord = NULL;
static PFN_MSISOURCELISTADDSOURCEEXW vpfnMsiSourceListAddSourceExW = NULL;

static HMODULE vhMsiDll = NULL;
static PFN_MSIDETERMINEPATCHSEQUENCEW vpfnMsiDeterminePatchSequenceWFromLibrary = NULL;
static PFN_MSIENUMPRODUCTSEXW vpfnMsiEnumProductsExWFromLibrary = NULL;
static PFN_MSIGETPATCHINFOEXW vpfnMsiGetPatchInfoExWFromLibrary = NULL;
static PFN_MSIGETPRODUCTINFOEXW vpfnMsiGetProductInfoExWFromLibrary = NULL;
static PFN_MSISETEXTERNALUIRECORD vpfnMsiSetExternalUIRecordFromLibrary = NULL;
static PFN_MSISOURCELISTADDSOURCEEXW vpfnMsiSourceListAddSourceExWFromLibrary = NULL;
static BOOL vfWiuInitialized = FALSE;

// globals
static DWORD vdwMsiDllMajorMinor = 0;
static DWORD vdwMsiDllBuildRevision = 0;


// internal function declarations

static DWORD CheckForRestartErrorCode(
    __in DWORD dwErrorCode,
    __out WIU_RESTART* pRestart
    );
static INT CALLBACK InstallEngineCallback(
    __in LPVOID pvContext,
    __in UINT uiMessage,
    __in_z_opt LPCWSTR wzMessage
    );
static INT CALLBACK InstallEngineRecordCallback(
    __in LPVOID pvContext,
    __in UINT uiMessage,
    __in_opt MSIHANDLE hRecord
    );
static INT HandleInstallMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in INSTALLMESSAGE mt,
    __in UINT uiFlags,
    __in_z LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    );
static INT HandleInstallProgress(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in_z_opt LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    );
static INT SendMsiMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in INSTALLMESSAGE mt,
    __in UINT uiFlags,
    __in_z LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    );
static INT SendErrorMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in UINT uiFlags,
    __in_z LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    );
static INT SendFilesInUseMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in_opt MSIHANDLE hRecord,
    __in BOOL fRestartManagerRequest
    );
static INT SendProgressUpdate(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext
    );
static void ResetProgress(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext
    );
static DWORD CalculatePhaseProgress(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in DWORD dwProgressIndex,
    __in DWORD dwWeightPercentage
    );
void InitializeMessageData(
    __in MSIHANDLE hRecord,
    __out LPWSTR** prgsczData,
    __out DWORD* pcData
    );
void UninitializeMessageData(
    __in LPWSTR* rgsczData,
    __in DWORD cData
    );


/********************************************************************
 WiuInitialize - initializes wioutil

*********************************************************************/
extern "C" HRESULT DAPI WiuInitialize(
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczMsiDllPath = NULL;

    hr = LoadSystemLibraryWithPath(L"Msi.dll", &vhMsiDll, &sczMsiDllPath);
    ExitOnFailure(hr, "Failed to load Msi.DLL");

    // Ignore failures
    FileVersion(sczMsiDllPath, &vdwMsiDllMajorMinor, &vdwMsiDllBuildRevision);

    vpfnMsiDeterminePatchSequenceWFromLibrary = reinterpret_cast<PFN_MSIDETERMINEPATCHSEQUENCEW>(::GetProcAddress(vhMsiDll, "MsiDeterminePatchSequenceW"));
    if (NULL == vpfnMsiDeterminePatchSequenceW)
    {
        vpfnMsiDeterminePatchSequenceW = vpfnMsiDeterminePatchSequenceWFromLibrary;
    }

    vpfnMsiEnumProductsExWFromLibrary = reinterpret_cast<PFN_MSIENUMPRODUCTSEXW>(::GetProcAddress(vhMsiDll, "MsiEnumProductsExW"));
    if (NULL == vpfnMsiEnumProductsExW)
    {
        vpfnMsiEnumProductsExW = vpfnMsiEnumProductsExWFromLibrary;
    }

    vpfnMsiGetPatchInfoExWFromLibrary = reinterpret_cast<PFN_MSIGETPATCHINFOEXW>(::GetProcAddress(vhMsiDll, "MsiGetPatchInfoExW"));
    if (NULL == vpfnMsiGetPatchInfoExW)
    {
        vpfnMsiGetPatchInfoExW = vpfnMsiGetPatchInfoExWFromLibrary;
    }

    vpfnMsiGetProductInfoExWFromLibrary = reinterpret_cast<PFN_MSIGETPRODUCTINFOEXW>(::GetProcAddress(vhMsiDll, "MsiGetProductInfoExW"));
    if (NULL == vpfnMsiGetProductInfoExW)
    {
        vpfnMsiGetProductInfoExW = vpfnMsiGetProductInfoExWFromLibrary;
    }

    vpfnMsiSetExternalUIRecordFromLibrary = reinterpret_cast<PFN_MSISETEXTERNALUIRECORD>(::GetProcAddress(vhMsiDll, "MsiSetExternalUIRecord"));
    if (NULL == vpfnMsiSetExternalUIRecord)
    {
        vpfnMsiSetExternalUIRecord = vpfnMsiSetExternalUIRecordFromLibrary;
    }

    //static PFN_MSISOURCELISTADDSOURCEEXW vpfnMsiSourceListAddSourceExW = NULL;
    vpfnMsiSourceListAddSourceExWFromLibrary = reinterpret_cast<PFN_MSISOURCELISTADDSOURCEEXW>(::GetProcAddress(vhMsiDll, "MsiSourceListAddSourceExW"));
    if (NULL == vpfnMsiSourceListAddSourceExW)
    {
        vpfnMsiSourceListAddSourceExW = vpfnMsiSourceListAddSourceExWFromLibrary;
    }

    vfWiuInitialized = TRUE;

LExit:
    ReleaseStr(sczMsiDllPath);
    return hr;
}


/********************************************************************
 WiuUninitialize - uninitializes wiutil

*********************************************************************/
extern "C" void DAPI WiuUninitialize(
    )
{
    if (vhMsiDll)
    {
        ::FreeLibrary(vhMsiDll);
        vhMsiDll = NULL;
        vpfnMsiSetExternalUIRecordFromLibrary = NULL;
        vpfnMsiGetProductInfoExWFromLibrary = NULL;
        vpfnMsiGetPatchInfoExWFromLibrary = NULL;
        vpfnMsiEnumProductsExWFromLibrary = NULL;
        vpfnMsiDeterminePatchSequenceWFromLibrary = NULL;
        vpfnMsiSourceListAddSourceExWFromLibrary = NULL;
    }

    vfWiuInitialized = FALSE;
}


/********************************************************************
 WiuFunctionOverride - overrides the Windows installer functions. Typically used
                       for unit testing.

*********************************************************************/
extern "C" void DAPI WiuFunctionOverride(
    __in_opt PFN_MSIENABLELOGW pfnMsiEnableLogW,
    __in_opt PFN_MSIGETCOMPONENTPATHW pfnMsiGetComponentPathW,
    __in_opt PFN_MSILOCATECOMPONENTW pfnMsiLocateComponentW,
    __in_opt PFN_MSIQUERYFEATURESTATEW pfnMsiQueryFeatureStateW,
    __in_opt PFN_MSIGETPRODUCTINFOW pfnMsiGetProductInfoW,
    __in_opt PFN_MSIGETPRODUCTINFOEXW pfnMsiGetProductInfoExW,
    __in_opt PFN_MSIINSTALLPRODUCTW pfnMsiInstallProductW,
    __in_opt PFN_MSICONFIGUREPRODUCTEXW pfnMsiConfigureProductExW,
    __in_opt PFN_MSISETINTERNALUI pfnMsiSetInternalUI,
    __in_opt PFN_MSISETEXTERNALUIW pfnMsiSetExternalUIW,
    __in_opt PFN_MSIENUMRELATEDPRODUCTSW pfnMsiEnumRelatedProductsW,
    __in_opt PFN_MSISETEXTERNALUIRECORD pfnMsiSetExternalUIRecord,
    __in_opt PFN_MSISOURCELISTADDSOURCEEXW pfnMsiSourceListAddSourceExW
    )
{
    vpfnMsiEnableLogW = pfnMsiEnableLogW ? pfnMsiEnableLogW : ::MsiEnableLogW;
    vpfnMsiGetComponentPathW = pfnMsiGetComponentPathW ? pfnMsiGetComponentPathW : ::MsiGetComponentPathW;
    vpfnMsiLocateComponentW = pfnMsiLocateComponentW ? pfnMsiLocateComponentW : ::MsiLocateComponentW;
    vpfnMsiQueryFeatureStateW = pfnMsiQueryFeatureStateW ? pfnMsiQueryFeatureStateW : ::MsiQueryFeatureStateW;
    vpfnMsiGetProductInfoW = pfnMsiGetProductInfoW ? pfnMsiGetProductInfoW : vpfnMsiGetProductInfoW;
    vpfnMsiInstallProductW = pfnMsiInstallProductW ? pfnMsiInstallProductW : ::MsiInstallProductW;
    vpfnMsiConfigureProductExW = pfnMsiConfigureProductExW ? pfnMsiConfigureProductExW : ::MsiConfigureProductExW;
    vpfnMsiSetInternalUI = pfnMsiSetInternalUI ? pfnMsiSetInternalUI : ::MsiSetInternalUI;
    vpfnMsiSetExternalUIW = pfnMsiSetExternalUIW ? pfnMsiSetExternalUIW : ::MsiSetExternalUIW;
    vpfnMsiEnumRelatedProductsW = pfnMsiEnumRelatedProductsW ? pfnMsiEnumRelatedProductsW : ::MsiEnumRelatedProductsW;
    vpfnMsiGetProductInfoExW = pfnMsiGetProductInfoExW ? pfnMsiGetProductInfoExW : vpfnMsiGetProductInfoExWFromLibrary;
    vpfnMsiSetExternalUIRecord = pfnMsiSetExternalUIRecord ? pfnMsiSetExternalUIRecord : vpfnMsiSetExternalUIRecordFromLibrary;
    vpfnMsiSourceListAddSourceExW = pfnMsiSourceListAddSourceExW ? pfnMsiSourceListAddSourceExW : vpfnMsiSourceListAddSourceExWFromLibrary;
}


extern "C" HRESULT DAPI WiuGetComponentPath(
    __in_z LPCWSTR wzProductCode,
    __in_z LPCWSTR wzComponentId,
    __out INSTALLSTATE* pInstallState,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    DWORD cch = WIU_GOOD_ENOUGH_PROPERTY_LENGTH;
    DWORD cchCompare;

    hr = StrAlloc(psczValue, cch);
    ExitOnFailure(hr, "Failed to allocate string for component path.");

    cchCompare = cch;
    *pInstallState = vpfnMsiGetComponentPathW(wzProductCode, wzComponentId, *psczValue, &cch);
    if (INSTALLSTATE_MOREDATA == *pInstallState)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for component path.");

        cchCompare = cch;
        *pInstallState = vpfnMsiGetComponentPathW(wzProductCode, wzComponentId, *psczValue, &cch);
    }

    if (INSTALLSTATE_INVALIDARG == *pInstallState)
    {
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid argument when getting component path.");
    }
    else if (INSTALLSTATE_UNKNOWN == *pInstallState)
    {
        ExitFunction();
    }

    // If the actual path length is greater than or equal to the original buffer
    // allocate a larger buffer and get the path again, just in case we are 
    // missing any part of the path.
    if (cchCompare <= cch)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for component path.");

        *pInstallState = vpfnMsiGetComponentPathW(wzProductCode, wzComponentId, *psczValue, &cch);
    }

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuLocateComponent(
    __in_z LPCWSTR wzComponentId,
    __out INSTALLSTATE* pInstallState,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    DWORD cch = WIU_GOOD_ENOUGH_PROPERTY_LENGTH;
    DWORD cchCompare;

    hr = StrAlloc(psczValue, cch);
    ExitOnFailure(hr, "Failed to allocate string for component path.");

    cchCompare = cch;
    *pInstallState = vpfnMsiLocateComponentW(wzComponentId, *psczValue, &cch);
    if (INSTALLSTATE_MOREDATA == *pInstallState)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for component path.");

        cchCompare = cch;
        *pInstallState = vpfnMsiLocateComponentW(wzComponentId, *psczValue, &cch);
    }

    if (INSTALLSTATE_INVALIDARG == *pInstallState)
    {
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid argument when locating component.");
    }
    else if (INSTALLSTATE_UNKNOWN == *pInstallState)
    {
        ExitFunction();
    }

    // If the actual path length is greater than or equal to the original buffer
    // allocate a larger buffer and get the path again, just in case we are 
    // missing any part of the path.
    if (cchCompare <= cch)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for component path.");

        *pInstallState = vpfnMsiLocateComponentW(wzComponentId, *psczValue, &cch);
    }

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuQueryFeatureState(
    __in_z LPCWSTR wzProduct,
    __in_z LPCWSTR wzFeature,
    __out INSTALLSTATE* pInstallState
    )
{
    HRESULT hr = S_OK;

    *pInstallState = vpfnMsiQueryFeatureStateW(wzProduct, wzFeature);
    if (INSTALLSTATE_INVALIDARG == *pInstallState)
    {
        hr = E_INVALIDARG;
        ExitOnRootFailure2(hr, "Failed to query state of feature: %ls in product: %ls", wzFeature, wzProduct);
    }

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuGetProductInfo(
    __in_z LPCWSTR wzProductCode,
    __in_z LPCWSTR wzProperty,
    __out LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    DWORD cch = WIU_GOOD_ENOUGH_PROPERTY_LENGTH;

    hr = StrAlloc(psczValue, cch);
    ExitOnFailure(hr, "Failed to allocate string for product info.");

    er = vpfnMsiGetProductInfoW(wzProductCode, wzProperty, *psczValue, &cch);
    if (ERROR_MORE_DATA == er)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for product info.");

        er = vpfnMsiGetProductInfoW(wzProductCode, wzProperty, *psczValue, &cch);
    }
    ExitOnWin32Error(er, hr, "Failed to get product info.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuGetProductInfoEx(
    __in_z LPCWSTR wzProductCode,
    __in_z_opt LPCWSTR wzUserSid,
    __in MSIINSTALLCONTEXT dwContext,
    __in_z LPCWSTR wzProperty,
    __out LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    DWORD cch = WIU_GOOD_ENOUGH_PROPERTY_LENGTH;

    if (!vpfnMsiGetProductInfoExW)
    {
        hr = WiuGetProductInfo(wzProductCode, wzProperty, psczValue);
        ExitOnFailure(hr, "Failed to get product info when extended info was not available.");

        ExitFunction();
    }

    hr = StrAlloc(psczValue, cch);
    ExitOnFailure(hr, "Failed to allocate string for extended product info.");

    er = vpfnMsiGetProductInfoExW(wzProductCode, wzUserSid, dwContext, wzProperty, *psczValue, &cch);
    if (ERROR_MORE_DATA == er)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for extended product info.");

        er = vpfnMsiGetProductInfoExW(wzProductCode, wzUserSid, dwContext, wzProperty, *psczValue, &cch);
    }
    ExitOnWin32Error(er, hr, "Failed to get extended product info.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuGetProductProperty(
    __in MSIHANDLE hProduct,
    __in_z LPCWSTR wzProperty,
    __out LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    DWORD cch = WIU_GOOD_ENOUGH_PROPERTY_LENGTH;

    hr = StrAlloc(psczValue, cch);
    ExitOnFailure(hr, "Failed to allocate string for product property.");

    er = ::MsiGetProductPropertyW(hProduct, wzProperty, *psczValue, &cch);
    if (ERROR_MORE_DATA == er)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for product property.");

        er = ::MsiGetProductPropertyW(hProduct, wzProperty, *psczValue, &cch);
    }
    ExitOnWin32Error(er, hr, "Failed to get product property.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuGetPatchInfoEx(
    __in_z LPCWSTR wzPatchCode,
    __in_z LPCWSTR wzProductCode,
    __in_z_opt LPCWSTR wzUserSid,
    __in MSIINSTALLCONTEXT dwContext,
    __in_z LPCWSTR wzProperty,
    __out LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    DWORD cch = WIU_GOOD_ENOUGH_PROPERTY_LENGTH;

    if (!vpfnMsiGetPatchInfoExW)
    {
        ExitFunction1(hr = E_NOTIMPL);
    }

    hr = StrAlloc(psczValue, cch);
    ExitOnFailure(hr, "Failed to allocate string for extended patch info.");

    er = vpfnMsiGetPatchInfoExW(wzPatchCode, wzProductCode, wzUserSid, dwContext, wzProperty, *psczValue, &cch);
    if (ERROR_MORE_DATA == er)
    {
        ++cch;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to reallocate string for extended patch info.");

        er = vpfnMsiGetPatchInfoExW(wzPatchCode, wzProductCode, wzUserSid, dwContext, wzProperty, *psczValue, &cch);
    }
    ExitOnWin32Error(er, hr, "Failed to get extended patch info.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuDeterminePatchSequence(
    __in_z LPCWSTR wzProductCode,
    __in_z_opt LPCWSTR wzUserSid,
    __in MSIINSTALLCONTEXT context,
    __in PMSIPATCHSEQUENCEINFOW pPatchInfo,
    __in DWORD cPatchInfo
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    if (!vpfnMsiDeterminePatchSequenceW)
    {
        ExitFunction1(hr = E_NOTIMPL);
    }

    er = vpfnMsiDeterminePatchSequenceW(wzProductCode, wzUserSid, context, cPatchInfo, pPatchInfo);
    ExitOnWin32Error(er, hr, "Failed to determine patch sequence for product code.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuEnumProducts(
    __in DWORD iProductIndex,
    __out_ecount(MAX_GUID_CHARS + 1) LPWSTR wzProductCode
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnMsiEnumProductsW(iProductIndex, wzProductCode);
    if (ERROR_NO_MORE_ITEMS == er)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(er));
    }
    ExitOnWin32Error(er, hr, "Failed to enumerate products.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuEnumProductsEx(
    __in_z_opt LPCWSTR wzProductCode,
    __in_z_opt LPCWSTR wzUserSid,
    __in DWORD dwContext,
    __in DWORD dwIndex,
    __out_opt WCHAR wzInstalledProductCode[39],
    __out_opt MSIINSTALLCONTEXT *pdwInstalledContext,
    __out_opt LPWSTR wzSid,
    __inout_opt LPDWORD pcchSid
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    if (!vpfnMsiEnumProductsExW)
    {
        ExitFunction1(hr = E_NOTIMPL);
    }

    er = vpfnMsiEnumProductsExW(wzProductCode, wzUserSid, dwContext, dwIndex, wzInstalledProductCode, pdwInstalledContext, wzSid, pcchSid);
    if (ERROR_NO_MORE_ITEMS == er)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(er));
    }
    ExitOnWin32Error(er, hr, "Failed to enumerate products.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuEnumRelatedProducts(
    __in_z LPCWSTR wzUpgradeCode,
    __in DWORD iProductIndex,
    __out_ecount(MAX_GUID_CHARS + 1) LPWSTR wzProductCode
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnMsiEnumRelatedProductsW(wzUpgradeCode, 0, iProductIndex, wzProductCode);
    if (ERROR_NO_MORE_ITEMS == er)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(er));
    }
    ExitOnWin32Error1(er, hr, "Failed to enumerate related products for updgrade code: %ls", wzUpgradeCode);

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuEnableLog(
    __in DWORD dwLogMode,
    __in_z LPCWSTR wzLogFile,
    __in DWORD dwLogAttributes
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnMsiEnableLogW(dwLogMode, wzLogFile, dwLogAttributes);
    ExitOnWin32Error(er, hr, "Failed to enable MSI internal logging.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuInitializeExternalUI(
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in INSTALLUILEVEL internalUILevel,
    __in HWND hwndParent,
    __in LPVOID pvContext,
    __in BOOL fRollback,
    __in WIU_MSI_EXECUTE_CONTEXT* pExecuteContext
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    DWORD dwMessageFilter = INSTALLLOGMODE_INITIALIZE | INSTALLLOGMODE_TERMINATE |
                            INSTALLLOGMODE_FATALEXIT | INSTALLLOGMODE_ERROR | INSTALLLOGMODE_WARNING |
                            INSTALLLOGMODE_RESOLVESOURCE | INSTALLLOGMODE_OUTOFDISKSPACE |
                            INSTALLLOGMODE_ACTIONSTART | INSTALLLOGMODE_ACTIONDATA | INSTALLLOGMODE_COMMONDATA|
                            INSTALLLOGMODE_PROGRESS | INSTALLLOGMODE_FILESINUSE;

    if (MAKEDWORD(0, 4) <= vdwMsiDllMajorMinor)
    {
        dwMessageFilter |= INSTALLLOGMODE_RMFILESINUSE;
    }

    memset(pExecuteContext, 0, sizeof(WIU_MSI_EXECUTE_CONTEXT));
    pExecuteContext->fRollback = fRollback;
    pExecuteContext->pfnMessageHandler = pfnMessageHandler;
    pExecuteContext->pvContext = pvContext;

    // Wire the internal and external UI handler.
    pExecuteContext->previousInstallUILevel = vpfnMsiSetInternalUI(internalUILevel, &hwndParent);
    pExecuteContext->hwndPreviousParentWindow = hwndParent;

    // If the external UI record is available (MSI version >= 3.1) use it but fall back to the standard external
    // UI handler if necesary.
    if (vpfnMsiSetExternalUIRecord)
    {
        er = vpfnMsiSetExternalUIRecord(InstallEngineRecordCallback, dwMessageFilter, pExecuteContext, &pExecuteContext->pfnPreviousExternalUIRecord);
        ExitOnWin32Error(er, hr, "Failed to wire up external UI record handler.");
        pExecuteContext->fSetPreviousExternalUIRecord = TRUE;
    }
    else
    {
        pExecuteContext->pfnPreviousExternalUI = vpfnMsiSetExternalUIW(InstallEngineCallback, dwMessageFilter, pExecuteContext);
        pExecuteContext->fSetPreviousExternalUI = TRUE;
    }

LExit:
    return hr;
}


extern "C" void DAPI WiuUninitializeExternalUI(
    __in WIU_MSI_EXECUTE_CONTEXT* pExecuteContext
    )
{
    if (INSTALLUILEVEL_NOCHANGE != pExecuteContext->previousInstallUILevel)
    {
        pExecuteContext->previousInstallUILevel = vpfnMsiSetInternalUI(pExecuteContext->previousInstallUILevel, &pExecuteContext->hwndPreviousParentWindow);
    }

    if (pExecuteContext->fSetPreviousExternalUI)  // unset the UI handler
    {
        vpfnMsiSetExternalUIW(pExecuteContext->pfnPreviousExternalUI, 0, NULL);
    }

    if (pExecuteContext->fSetPreviousExternalUIRecord)  // unset the UI record handler
    {
        vpfnMsiSetExternalUIRecord(pExecuteContext->pfnPreviousExternalUIRecord, 0, NULL, NULL);
    }

    memset(pExecuteContext, 0, sizeof(WIU_MSI_EXECUTE_CONTEXT));
}


extern "C" HRESULT DAPI WiuConfigureProductEx(
    __in_z LPCWSTR wzProduct,
    __in int iInstallLevel,
    __in INSTALLSTATE eInstallState,
    __in_z LPCWSTR wzCommandLine,
    __out WIU_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnMsiConfigureProductExW(wzProduct, iInstallLevel, eInstallState, wzCommandLine);
    er = CheckForRestartErrorCode(er, pRestart);
    ExitOnWin32Error1(er, hr, "Failed to configure product: %ls", wzProduct);

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuInstallProduct(
    __in_z LPCWSTR wzPackagePath,
    __in_z LPCWSTR wzCommandLine,
    __out WIU_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnMsiInstallProductW(wzPackagePath, wzCommandLine);
    er = CheckForRestartErrorCode(er, pRestart);
    ExitOnWin32Error1(er, hr, "Failed to install product: %ls", wzPackagePath);

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuRemovePatches(
    __in_z LPCWSTR wzPatchList,
    __in_z LPCWSTR wzProductCode,
    __in_z LPCWSTR wzPropertyList,
    __out WIU_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnMsiRemovePatchesW(wzPatchList, wzProductCode, INSTALLTYPE_SINGLE_INSTANCE, wzPropertyList);
    er = CheckForRestartErrorCode(er, pRestart);
    ExitOnWin32Error(er, hr, "Failed to remove patches.");

LExit:
    return hr;
}


extern "C" HRESULT DAPI WiuSourceListAddSourceEx(
    __in_z LPCWSTR wzProductCodeOrPatchCode,
    __in_z_opt LPCWSTR wzUserSid,
    __in MSIINSTALLCONTEXT dwContext,
    __in DWORD dwCode,
    __in_z LPCWSTR wzSource,
    __in_opt DWORD dwIndex
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    er = vpfnMsiSourceListAddSourceExW(wzProductCodeOrPatchCode, wzUserSid, dwContext, MSISOURCETYPE_NETWORK | dwCode, wzSource, dwIndex);
    ExitOnWin32Error(er, hr, "Failed to add source.");

LExit:
    return hr;
}



static DWORD CheckForRestartErrorCode(
    __in DWORD dwErrorCode,
    __out WIU_RESTART* pRestart
    )
{
    switch (dwErrorCode)
    {
    case ERROR_SUCCESS_REBOOT_REQUIRED:
    case ERROR_SUCCESS_RESTART_REQUIRED:
        *pRestart = WIU_RESTART_REQUIRED;
        dwErrorCode = ERROR_SUCCESS;
        break;

    case ERROR_SUCCESS_REBOOT_INITIATED:
    case ERROR_INSTALL_SUSPEND:
        *pRestart = WIU_RESTART_INITIATED;
        dwErrorCode = ERROR_SUCCESS;
        break;
    }

    return dwErrorCode;
}

static INT CALLBACK InstallEngineCallback(
    __in LPVOID pvContext,
    __in UINT uiMessage,
    __in_z_opt LPCWSTR wzMessage
    )
{
    INT nResult = IDNOACTION;
    WIU_MSI_EXECUTE_CONTEXT* pContext = (WIU_MSI_EXECUTE_CONTEXT*)pvContext;
    INSTALLMESSAGE mt = static_cast<INSTALLMESSAGE>(0xFF000000 & uiMessage);
    UINT uiFlags = 0x00FFFFFF & uiMessage;

    if (wzMessage)
    {
        if (INSTALLMESSAGE_PROGRESS == mt)
        {
            nResult = HandleInstallProgress(pContext, wzMessage, NULL);
        }
        else
        {
            nResult = HandleInstallMessage(pContext, mt, uiFlags, wzMessage, NULL);
        }
    }

    return nResult;
}

static INT CALLBACK InstallEngineRecordCallback(
    __in LPVOID pvContext,
    __in UINT uiMessage,
    __in_opt MSIHANDLE hRecord
    )
{
    INT nResult = IDNOACTION;
    HRESULT hr = S_OK;
    WIU_MSI_EXECUTE_CONTEXT* pContext = (WIU_MSI_EXECUTE_CONTEXT*)pvContext;

    INSTALLMESSAGE mt = static_cast<INSTALLMESSAGE>(0xFF000000 & uiMessage);
    UINT uiFlags = 0x00FFFFFF & uiMessage;
    LPWSTR sczMessage = NULL;
    DWORD cchMessage = 0;

    if (hRecord)
    {
        if (INSTALLMESSAGE_PROGRESS == mt)
        {
            nResult = HandleInstallProgress(pContext, NULL, hRecord);
        }
        else
        {
            // create formated message string
#pragma prefast(push)
#pragma prefast(disable:6298) // docs explicitly say this is a valid option for getting the buffer size
            DWORD er = ::MsiFormatRecordW(NULL, hRecord, L"", &cchMessage);
#pragma prefast(pop)
            if (ERROR_MORE_DATA == er || ERROR_SUCCESS == er)
            {
                hr = StrAlloc(&sczMessage, ++cchMessage);
            }
            else
            {
                hr = HRESULT_FROM_WIN32(er);
            }
            ExitOnFailure(hr, "Failed to allocate string for formated message.");

            er = ::MsiFormatRecordW(NULL, hRecord, sczMessage, &cchMessage);
            ExitOnWin32Error(er, hr, "Failed to format message record.");

            // Pass to handler including both the formated message and the original record.
            nResult = HandleInstallMessage(pContext, mt, uiFlags, sczMessage, hRecord);
        }
    }

LExit:
    ReleaseStr(sczMessage);
    return nResult;
}

static INT HandleInstallMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in INSTALLMESSAGE mt,
    __in UINT uiFlags,
    __in_z LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    )
{
    INT nResult = IDNOACTION;

Trace2(REPORT_STANDARD, "MSI install[%x]: %ls", pContext->dwCurrentProgressIndex, wzMessage);

    // Handle the message.
    switch (mt)
    {
    case INSTALLMESSAGE_INITIALIZE: // this message is received prior to internal UI initialization, no string data
        ResetProgress(pContext);
        break;

    case INSTALLMESSAGE_TERMINATE: // sent after UI termination, no string data
        break;

    case INSTALLMESSAGE_ACTIONSTART:
        if (WIU_MSI_PROGRESS_INVALID != pContext->dwCurrentProgressIndex && pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fEnableActionData)
        {
            pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fEnableActionData = FALSE;
        }

        nResult = SendMsiMessage(pContext, mt, uiFlags, wzMessage, hRecord);
        break;

    case INSTALLMESSAGE_ACTIONDATA:
        if (WIU_MSI_PROGRESS_INVALID != pContext->dwCurrentProgressIndex && pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fEnableActionData)
        {
            if (pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fMoveForward)
            {
                pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwCompleted += pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwStep;
            }
            else // rollback.
            {
                pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwCompleted -= pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwStep;
            }

            nResult = SendProgressUpdate(pContext);
        }
        else
        {
            nResult = SendMsiMessage(pContext, mt, uiFlags, wzMessage, hRecord);
        }
        break;

    case INSTALLMESSAGE_OUTOFDISKSPACE: __fallthrough;
    case INSTALLMESSAGE_FATALEXIT: __fallthrough;
    case INSTALLMESSAGE_ERROR:
        nResult = SendErrorMessage(pContext, uiFlags, wzMessage, hRecord);
        break;

    case INSTALLMESSAGE_FILESINUSE:
    case INSTALLMESSAGE_RMFILESINUSE:
        nResult = SendFilesInUseMessage(pContext, hRecord, INSTALLMESSAGE_RMFILESINUSE == mt);
        break;

/*
#if 0
    case INSTALLMESSAGE_COMMONDATA:
        if (L'1' == wzMessage[0] && L':' == wzMessage[1] && L' ' == wzMessage[2])
        {
            if (L'0' == wzMessage[3])
            {
                // TODO: handle the language common data message.
                lres = IDOK;
                return lres;
            }
            else if (L'1' == wzMessage[3])
            {
                // TODO: really handle sending the caption.
                lres = ::SendSuxMessage(pInstallContext->pSetupUXInformation, SRM_EXEC_SET_CAPTION, uiFlags, reinterpret_cast<LPARAM>(wzMessage + 3));
                return lres;
            }
            else if (L'2' == wzMessage[3])
            {
                // TODO: really handle sending the cancel button status.
                lres = ::SendSuxMessage(pInstallContext->pSetupUXInformation, SRM_EXEC_SET_CANCEL, uiFlags, reinterpret_cast<LPARAM>(wzMessage + 3));
                return lres;
            }
        }
        break;
#endif
*/

    //case INSTALLMESSAGE_WARNING:
    //case INSTALLMESSAGE_USER:
    //case INSTALLMESSAGE_INFO:
    //case INSTALLMESSAGE_SHOWDIALOG: // sent prior to display of authored dialog or wizard
    default:
        nResult = SendMsiMessage(pContext, mt, uiFlags, wzMessage, hRecord);
        break;
    }

    // Always return "no action" (0) for resolve source messages.
    return (INSTALLMESSAGE_RESOLVESOURCE == mt) ? IDNOACTION : nResult;
}

static INT HandleInstallProgress(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in_z_opt LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    )
{
    HRESULT hr = S_OK;
    INT nResult = IDNOACTION;
    INT iFields[4] = { };
    INT cFields = 0;
    LPCWSTR pwz = NULL;
    DWORD cch = 0;

    // get field values
    if (hRecord)
    {
        cFields = ::MsiRecordGetFieldCount(hRecord);
        cFields = min(cFields, countof(iFields)); // avoid buffer overrun if there are more fields than our buffer can hold
        for (INT i = 0; i < cFields; ++i)
        {
            iFields[i] = ::MsiRecordGetInteger(hRecord, i + 1);
        }
    }
    else
    {
        Assert(wzMessage);

        // parse message string
        pwz = wzMessage;
        while (cFields < 4)
        {
            // check if we have the start of a valid part
            if ((L'1' + cFields) != pwz[0] || L':' != pwz[1] || L' ' != pwz[2])
            {
                break;
            }
            pwz += 3;

            // find character count of number
            cch = 0;
            while (pwz[cch] && L' ' != pwz[cch])
            {
                ++cch;
            }

            // parse number
            hr = StrStringToInt32(pwz, cch, &iFields[cFields]);
            ExitOnFailure(hr, "Failed to parse MSI message part.");

            // increment field count
            ++cFields;
        }
    }

#ifdef _DEBUG
    WCHAR wz[256];
    ::StringCchPrintfW(wz, countof(wz), L"1: %d 2: %d 3: %d 4: %d", iFields[0], iFields[1], iFields[2], iFields[3]);
    Trace2(REPORT_STANDARD, "MSI progress[%x]: %ls", pContext->dwCurrentProgressIndex, wz);
#endif

    // Verify that we have the enough field values.
    if (1 > cFields)
    {
        ExitFunction(); // unknown message, bail
    }

    // Handle based on message type.
    switch (iFields[0])
    {
    case 0: // master progress reset
        if (4 > cFields)
        {
            Trace2(REPORT_STANDARD, "INSTALLMESSAGE_PROGRESS - Invalid field count %d, '%ls'", cFields, wzMessage);
            ExitFunction();
        }
        //Trace3(REPORT_STANDARD, "INSTALLMESSAGE_PROGRESS - MASTER RESET - %d, %d, %d", iFields[1], iFields[2], iFields[3]);

        // Update the index into progress array.
        if (WIU_MSI_PROGRESS_INVALID == pContext->dwCurrentProgressIndex)
        {
            pContext->dwCurrentProgressIndex = 0;
        }
        else if (pContext->dwCurrentProgressIndex + 1 < countof(pContext->rgMsiProgress))
        {
            ++pContext->dwCurrentProgressIndex;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            ExitOnRootFailure(hr, "Insufficient space to hold progress information.");
        }

        // we only care about the first stage after script execution has started
        //if (!pEngineInfo->fMsiProgressScriptInProgress && 1 != iFields[3])
        //{
        //    pEngineInfo->fMsiProgressFinished = TRUE;
        //}

        pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwTotal = iFields[1];
        pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwCompleted = 0 == iFields[2] ? 0 : iFields[1]; // if forward start at 0, if backwards start at max
        pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fMoveForward = (0 == iFields[2]);
        pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fEnableActionData = FALSE;
        pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fScriptInProgress = (1 == iFields[3]);

        if (0 == pContext->dwCurrentProgressIndex)
        {
            // HACK!!! this is a hack courtesy of the Windows Installer team. It seems the script planning phase
            // is always off by "about 50".  So we'll toss an extra 50 ticks on so that the standard progress
            // doesn't go over 100%.  If there are any custom actions, they may blow the total so we'll call this
            // "close" and deal with the rest.
            pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwTotal += 50;
        }
        break;

    case 1: // action info.
        if (3 > cFields)
        {
            Trace2(REPORT_STANDARD, "INSTALLMESSAGE_PROGRESS - Invalid field count %d, '%ls'", cFields, wzMessage);
            ExitFunction();
        }
        //Trace3(REPORT_STANDARD, "INSTALLMESSAGE_PROGRESS - ACTION INFO - %d, %d, %d", iFields[1], iFields[2], iFields[3]);

        if (0 == iFields[2])
        {
            pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fEnableActionData = FALSE;
        }
        else
        {
            pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fEnableActionData = TRUE;
            pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwStep = iFields[1];
        }
        break;

    case 2: // progress report.
        if (2 > cFields)
        {
            Trace2(REPORT_STANDARD, "INSTALLMESSAGE_PROGRESS - Invalid field count %d, '%ls'", cFields, wzMessage);
            break;
        }

        //Trace3(REPORT_STANDARD, "INSTALLMESSAGE_PROGRESS - PROGRESS REPORT - %d, %d, %d", iFields[1], iFields[2], iFields[3]);

        if (WIU_MSI_PROGRESS_INVALID == pContext->dwCurrentProgressIndex)
        {
            break;
        }
        else if (0 == pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwTotal)
        {
            break;
        }

        // Update progress.
        if (pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].fMoveForward)
        {
            pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwCompleted += iFields[1];
        }
        else // rollback.
        {
            pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwCompleted -= iFields[1];
        }
        break;

    case 3: // extend the progress bar.
        pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwTotal += iFields[1];
        break;

    default:
        ExitFunction(); // unknown message, bail
    }

    // If we have a valid progress index, send an update.
    if (WIU_MSI_PROGRESS_INVALID != pContext->dwCurrentProgressIndex)
    {
        nResult = SendProgressUpdate(pContext);
    }

LExit:
    return nResult;
}

static INT SendMsiMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in INSTALLMESSAGE mt,
    __in UINT uiFlags,
    __in_z LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    )
{
    INT nResult = IDNOACTION;
    WIU_MSI_EXECUTE_MESSAGE message = { };
    LPWSTR* rgsczData = NULL;
    DWORD cData = 0;

    InitializeMessageData(hRecord, &rgsczData, &cData);

    message.type = WIU_MSI_EXECUTE_MESSAGE_MSI_MESSAGE;
    message.dwAllowedResults = uiFlags;
    message.cData = cData;
    message.rgwzData = (LPCWSTR*)rgsczData;
    message.msiMessage.mt = mt;
    message.msiMessage.wzMessage = wzMessage;
    nResult = pContext->pfnMessageHandler(&message, pContext->pvContext);

    UninitializeMessageData(rgsczData, cData);
    return nResult;
}

static INT SendErrorMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in UINT uiFlags,
    __in_z LPCWSTR wzMessage,
    __in_opt MSIHANDLE hRecord
    )
{
    INT nResult = IDNOACTION;
    WIU_MSI_EXECUTE_MESSAGE message = { };
    DWORD dwErrorCode = 0;
    LPWSTR* rgsczData = NULL;
    DWORD cData = 0;

    if (hRecord)
    {
        dwErrorCode = ::MsiRecordGetInteger(hRecord, 1);

        // Set the recommendation if it's a known error code.
        switch (dwErrorCode)
        {
        case 1605: // continue with install even if there isn't enough room for rollback.
            nResult = IDIGNORE;
            break;

        case 1704: // rollback suspended installs so our install can continue.
            nResult = IDOK;
            break;
        }
    }

    InitializeMessageData(hRecord, &rgsczData, &cData);

    message.type = WIU_MSI_EXECUTE_MESSAGE_ERROR;
    message.dwAllowedResults = uiFlags;
    message.nResultRecommendation = nResult;
    message.cData = cData;
    message.rgwzData = (LPCWSTR*)rgsczData;
    message.error.dwErrorCode = dwErrorCode;
    message.error.wzMessage = wzMessage;
    nResult = pContext->pfnMessageHandler(&message, pContext->pvContext);

    UninitializeMessageData(rgsczData, cData);
    return nResult;
}

static INT SendFilesInUseMessage(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in_opt MSIHANDLE hRecord,
    __in BOOL /*fRestartManagerRequest*/
    )
{
    INT nResult = IDNOACTION;
    WIU_MSI_EXECUTE_MESSAGE message = { };
    LPWSTR* rgsczData = NULL;
    DWORD cData = 0;

    InitializeMessageData(hRecord, &rgsczData, &cData);

    message.type = WIU_MSI_EXECUTE_MESSAGE_MSI_FILES_IN_USE;
    message.dwAllowedResults = WIU_MB_OKIGNORECANCELRETRY;
    message.cData = cData;
    message.rgwzData = (LPCWSTR*)rgsczData;
    message.msiFilesInUse.cFiles = message.cData;       // point the files in use information to the message record information.
    message.msiFilesInUse.rgwzFiles = message.rgwzData;
    nResult = pContext->pfnMessageHandler(&message, pContext->pvContext);

    UninitializeMessageData(rgsczData, cData);
    return nResult;
}

static INT SendProgressUpdate(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext
    )
{
    int nResult = IDNOACTION;
    DWORD dwPercentage = 0; // number representing 0 - 100%
    WIU_MSI_EXECUTE_MESSAGE message = { };

    //DWORD dwMsiProgressTotal = pEngineInfo->dwMsiProgressTotal;
    //DWORD dwMsiProgressComplete = pEngineInfo->dwMsiProgressComplete; //min(dwMsiProgressTotal, pEngineInfo->dwMsiProgressComplete);
    //double dProgressGauge = 0;
    //double dProgressStageTotal = (double)pEngineInfo->qwProgressStageTotal;

    // Calculate progress for the phases of Windows Installer.
    // TODO: handle upgrade progress which would add another phase.
    dwPercentage += CalculatePhaseProgress(pContext, 0, 15);
    dwPercentage += CalculatePhaseProgress(pContext, 1, 80);
    dwPercentage += CalculatePhaseProgress(pContext, 2, 5);
    dwPercentage = min(dwPercentage, 100); // ensure the percentage never goes over 100%.

    if (pContext->fRollback)
    {
        dwPercentage = 100 - dwPercentage;
    }

    //if (qwTotal) // avoid "divide by zero" if the MSI range is blank.
    //{
    //    // calculate gauge.
    //    double dProgressGauge = static_cast<double>(qwCompleted) / static_cast<double>(qwTotal);
    //    dProgressGauge = (1.0 / (1.0 + exp(3.7 - dProgressGauge * 7.5)) - 0.024127021417669196) / 0.975872978582330804;
    //    qwCompleted = (DWORD)(dProgressGauge * qwTotal);

    //    // calculate progress within range
    //    //qwProgressComplete = (DWORD64)(dwMsiProgressComplete * (dProgressStageTotal / dwMsiProgressTotal));
    //    //qwProgressComplete = min(qwProgressComplete, pEngineInfo->qwProgressStageTotal);
    //}

#ifdef _DEBUG
    DWORD64 qwCompleted = pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwCompleted;
    DWORD64 qwTotal = pContext->rgMsiProgress[pContext->dwCurrentProgressIndex].dwTotal;
    Trace3(REPORT_STANDARD, "MSI progress: %I64u/%I64u (%u%%)", qwCompleted, qwTotal, dwPercentage);
    //AssertSz(qwCompleted <= qwTotal, "Completed progress is larger than total progress.");
#endif

    message.type = WIU_MSI_EXECUTE_MESSAGE_PROGRESS;
    message.dwAllowedResults = MB_OKCANCEL;
    message.progress.dwPercentage = dwPercentage;
    nResult = pContext->pfnMessageHandler(&message, pContext->pvContext);

    return nResult;
}

static void ResetProgress(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext
    )
{
    memset(pContext->rgMsiProgress, 0, sizeof(pContext->rgMsiProgress));
    pContext->dwCurrentProgressIndex = WIU_MSI_PROGRESS_INVALID;
}

static DWORD CalculatePhaseProgress(
    __in WIU_MSI_EXECUTE_CONTEXT* pContext,
    __in DWORD dwProgressIndex,
    __in DWORD dwWeightPercentage
    )
{
    DWORD dwPhasePercentage = 0;

    // If we've already passed this progress index, return the maximum percentage possible (the weight)
    if (dwProgressIndex < pContext->dwCurrentProgressIndex)
    {
        dwPhasePercentage = dwWeightPercentage;
    }
    else if (dwProgressIndex == pContext->dwCurrentProgressIndex) // have to do the math for the current progress.
    {
        WIU_MSI_PROGRESS* pProgress = pContext->rgMsiProgress + dwProgressIndex;
        if (pProgress->dwTotal)
        {
            DWORD64 dw64Completed = pProgress->dwCompleted;
            dwPhasePercentage = static_cast<DWORD>(dw64Completed * dwWeightPercentage / pProgress->dwTotal);
        }
    }
    // else we're not there yet so it has to be zero.

    return dwPhasePercentage;
}

void InitializeMessageData(
    __in_opt MSIHANDLE hRecord,
    __deref_out_ecount(*pcData) LPWSTR** prgsczData,
    __out DWORD* pcData
    )
{
    DWORD cData = 0;
    LPWSTR* rgsczData = NULL;

    // If we have a record based message, try to get the extra data.
    if (hRecord)
    {
        cData = ::MsiRecordGetFieldCount(hRecord);
        if (cData)
        {
            rgsczData = (LPWSTR*)MemAlloc(sizeof(LPWSTR*) * cData, TRUE);
        }

        for (DWORD i = 0; rgsczData && i < cData; ++i)
        {
            DWORD cch = 0;

            // get string from record
#pragma prefast(push)
#pragma prefast(disable:6298)
            DWORD er = ::MsiRecordGetStringW(hRecord, i + 1, L"", &cch);
#pragma prefast(pop)
            if (ERROR_MORE_DATA == er)
            {
                HRESULT hr = StrAlloc(&rgsczData[i], ++cch);
                if (SUCCEEDED(hr))
                {
                    er = ::MsiRecordGetStringW(hRecord, i + 1, rgsczData[i], &cch);
                }
            }
        }
    }

    *prgsczData = rgsczData;
    *pcData = cData;
}

void UninitializeMessageData(
    __in LPWSTR* rgsczData,
    __in DWORD cData
    )
{
    // Clean up if there was any data allocated.
    if (rgsczData)
    {
        for (DWORD i = 0; i < cData; ++i)
        {
            ReleaseStr(rgsczData[i]);
        }

        MemFree(rgsczData);
    }
}
