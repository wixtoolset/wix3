// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// constants we'll pick up from later SDKs
#define SM_TABLETPC    86
#define SM_MEDIACENTER 87
#define SM_STARTER     88
#define SM_SERVERR2    89
#define VER_SUITE_WH_SERVER 0x00008000

/********************************************************************
WixQueryOsInfo - entry point for WixQueryOsInfo custom action

 Called as Type 1 custom action (DLL from the Binary table) from 
 Windows Installer to set properties that identify OS information 
 and predefined directories
********************************************************************/
extern "C" UINT __stdcall WixQueryOsInfo(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    OSVERSIONINFOEXW ovix = {0};

    hr = WcaInitialize(hInstall, "WixQueryOsInfo");
    ExitOnFailure(hr, "WixQueryOsInfo failed to initialize");

    // identify product suites
    ovix.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
    ::GetVersionExW(reinterpret_cast<LPOSVERSIONINFOW>(&ovix));

    if (VER_SUITE_SMALLBUSINESS == (ovix.wSuiteMask & VER_SUITE_SMALLBUSINESS))
    {
        WcaSetIntProperty(L"WIX_SUITE_SMALLBUSINESS", 1);
    }

    if (VER_SUITE_ENTERPRISE == (ovix.wSuiteMask & VER_SUITE_ENTERPRISE))
    {
        WcaSetIntProperty(L"WIX_SUITE_ENTERPRISE", 1);
    }

    if (VER_SUITE_BACKOFFICE == (ovix.wSuiteMask & VER_SUITE_BACKOFFICE))
    {
        WcaSetIntProperty(L"WIX_SUITE_BACKOFFICE", 1);
    }

    if (VER_SUITE_COMMUNICATIONS == (ovix.wSuiteMask & VER_SUITE_COMMUNICATIONS))
    {
        WcaSetIntProperty(L"WIX_SUITE_COMMUNICATIONS", 1);
    }

    if (VER_SUITE_TERMINAL == (ovix.wSuiteMask & VER_SUITE_TERMINAL))
    {
        WcaSetIntProperty(L"WIX_SUITE_TERMINAL", 1);
    }

    if (VER_SUITE_SMALLBUSINESS_RESTRICTED == (ovix.wSuiteMask & VER_SUITE_SMALLBUSINESS_RESTRICTED))
    {
        WcaSetIntProperty(L"WIX_SUITE_SMALLBUSINESS_RESTRICTED", 1);
    }

    if (VER_SUITE_EMBEDDEDNT == (ovix.wSuiteMask & VER_SUITE_EMBEDDEDNT))
    {
        WcaSetIntProperty(L"WIX_SUITE_EMBEDDEDNT", 1);
    }

    if (VER_SUITE_DATACENTER == (ovix.wSuiteMask & VER_SUITE_DATACENTER))
    {
        WcaSetIntProperty(L"WIX_SUITE_DATACENTER", 1);
    }

    if (VER_SUITE_SINGLEUSERTS == (ovix.wSuiteMask & VER_SUITE_SINGLEUSERTS))
    {
        WcaSetIntProperty(L"WIX_SUITE_SINGLEUSERTS", 1);
    }

    if (VER_SUITE_PERSONAL == (ovix.wSuiteMask & VER_SUITE_PERSONAL))
    {
        WcaSetIntProperty(L"WIX_SUITE_PERSONAL", 1);
    }

    if (VER_SUITE_BLADE == (ovix.wSuiteMask & VER_SUITE_BLADE))
    {
        WcaSetIntProperty(L"WIX_SUITE_BLADE", 1);
    }

    if (VER_SUITE_EMBEDDED_RESTRICTED == (ovix.wSuiteMask & VER_SUITE_EMBEDDED_RESTRICTED))
    {
        WcaSetIntProperty(L"WIX_SUITE_EMBEDDED_RESTRICTED", 1);
    }

    if (VER_SUITE_SECURITY_APPLIANCE == (ovix.wSuiteMask & VER_SUITE_SECURITY_APPLIANCE))
    {
        WcaSetIntProperty(L"WIX_SUITE_SECURITY_APPLIANCE", 1);
    }

    if (VER_SUITE_STORAGE_SERVER == (ovix.wSuiteMask & VER_SUITE_STORAGE_SERVER))
    {
        WcaSetIntProperty(L"WIX_SUITE_STORAGE_SERVER", 1);
    }

    if (VER_SUITE_COMPUTE_SERVER == (ovix.wSuiteMask & VER_SUITE_COMPUTE_SERVER))
    {
        WcaSetIntProperty(L"WIX_SUITE_COMPUTE_SERVER", 1);
    }

    if (VER_SUITE_WH_SERVER == (ovix.wSuiteMask & VER_SUITE_WH_SERVER))
    {
        WcaSetIntProperty(L"WIX_SUITE_WH_SERVER", 1);
    }

    // only for XP and later
    if (5 < ovix.dwMajorVersion || (5 == ovix.dwMajorVersion && 0 < ovix.dwMinorVersion))
    {
        if (::GetSystemMetrics(SM_SERVERR2))
        {
            WcaSetIntProperty(L"WIX_SUITE_SERVERR2", 1);
        }

        if (::GetSystemMetrics(SM_MEDIACENTER))
        {
            WcaSetIntProperty(L"WIX_SUITE_MEDIACENTER", 1);
        }

        if (::GetSystemMetrics(SM_STARTER))
        {
            WcaSetIntProperty(L"WIX_SUITE_STARTER", 1);
        }

        if (::GetSystemMetrics(SM_TABLETPC))
        {
            WcaSetIntProperty(L"WIX_SUITE_TABLETPC", 1);
        }
    }

LExit:
    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
WixQueryOsDirs - entry point for WixQueryOsDirs custom action

 Called as Type 1 custom action (DLL from the Binary table) from 
 Windows Installer to set properties that identify predefined directories
********************************************************************/
extern "C" UINT __stdcall WixQueryOsDirs(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixQueryOsDirs");
    ExitOnFailure(hr, "WixQueryOsDirs failed to initialize");

    // get the paths of the CSIDLs that represent real paths and for which MSI
    // doesn't yet have standard folder properties
    WCHAR path[MAX_PATH];
    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_ADMINTOOLS, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_ADMINTOOLS", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_ALTSTARTUP, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_ALTSTARTUP", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_CDBURN_AREA, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_CDBURN_AREA", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COMMON_ADMINTOOLS, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COMMON_ADMINTOOLS", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COMMON_ALTSTARTUP, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COMMON_ALTSTARTUP", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COMMON_DOCUMENTS, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COMMON_DOCUMENTS", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COMMON_FAVORITES, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COMMON_FAVORITES", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COMMON_MUSIC, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COMMON_MUSIC", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COMMON_PICTURES, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COMMON_PICTURES", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COMMON_VIDEO, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COMMON_VIDEO", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_COOKIES, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_COOKIES", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_DESKTOP, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_DESKTOP", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_HISTORY, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_HISTORY", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_INTERNET_CACHE, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_INTERNET_CACHE", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_MYMUSIC, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_MYMUSIC", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_MYPICTURES, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_MYPICTURES", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_MYVIDEO, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_MYVIDEO", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_NETHOOD, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_NETHOOD", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_PERSONAL, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_PERSONAL", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_PRINTHOOD, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_PRINTHOOD", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_PROFILE, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_PROFILE", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_RECENT, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_RECENT", path);
    }

    if (ERROR_SUCCESS == ::SHGetFolderPathW(NULL, CSIDL_RESOURCES, NULL, SHGFP_TYPE_CURRENT, path))
    {
        WcaSetProperty(L"WIX_DIR_RESOURCES", path);
    }

LExit:
    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
SetPropertyWellKnownSID

 Set a property with the localized name of a well known windows SID
********************************************************************/
static HRESULT SetPropertyWellKnownSID(
    __in WELL_KNOWN_SID_TYPE sidType,
    __in LPCWSTR wzPropertyName,
    __in BOOL fIncludeDomainName
    )
{
    HRESULT hr = S_OK;
    PSID psid = NULL;
    WCHAR wzRefDomain[MAX_PATH] = {0};
    SID_NAME_USE nameUse;
    DWORD refSize = MAX_PATH;
    WCHAR wzName[MAX_PATH] = {0};
    LPWSTR pwzPropertyValue = NULL;
    DWORD size = MAX_PATH;

    hr = AclGetWellKnownSid(sidType, &psid);
    ExitOnFailure1(hr, "Failed to get SID; skipping account %ls", wzPropertyName);

    if (!::LookupAccountSidW(NULL, psid, wzName, &size, wzRefDomain, &refSize, &nameUse))
    {
        ExitWithLastError1(hr, "Failed to look up account for SID; skipping account %ls.", wzPropertyName);
    }

    if (fIncludeDomainName)
    {
        hr = StrAllocFormatted(&pwzPropertyValue, L"%s\\%s", wzRefDomain, wzName);
        ExitOnFailure(hr, "Failed to format property value");

        hr = WcaSetProperty(wzPropertyName, pwzPropertyValue);
        ExitOnFailure(hr, "Failed write domain\\name property");
    }
    else
    {
        hr = WcaSetProperty(wzPropertyName, wzName);
        ExitOnFailure(hr, "Failed write name-only property");
    }
 
LExit:
    if (NULL != psid)
    {
        ::LocalFree(psid);
    }
    ReleaseStr(pwzPropertyValue);
    return hr;
}

/********************************************************************
WixQueryOsWellKnownSID - entry point for WixQueryOsWellKnownSID custom action

 Called as Type 1 custom action (DLL from the Binary table) from 
 Windows Installer to set properties with the localized names of built-in
 Windows Security IDs
********************************************************************/
extern "C" UINT __stdcall WixQueryOsWellKnownSID(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixQueryOsWellKnownSID");
    ExitOnFailure(hr, "WixQueryOsWellKnownSID failed to initialize");

    SetPropertyWellKnownSID(WinLocalSystemSid, L"WIX_ACCOUNT_LOCALSYSTEM", TRUE);
    SetPropertyWellKnownSID(WinLocalServiceSid, L"WIX_ACCOUNT_LOCALSERVICE", TRUE);
    SetPropertyWellKnownSID(WinNetworkServiceSid, L"WIX_ACCOUNT_NETWORKSERVICE", TRUE);
    SetPropertyWellKnownSID(WinBuiltinAdministratorsSid, L"WIX_ACCOUNT_ADMINISTRATORS", TRUE);
    SetPropertyWellKnownSID(WinBuiltinUsersSid, L"WIX_ACCOUNT_USERS", TRUE);
    SetPropertyWellKnownSID(WinBuiltinGuestsSid, L"WIX_ACCOUNT_GUESTS", TRUE);
    SetPropertyWellKnownSID(WinBuiltinPerfLoggingUsersSid, L"WIX_ACCOUNT_PERFLOGUSERS", TRUE);
    SetPropertyWellKnownSID(WinBuiltinPerfLoggingUsersSid, L"WIX_ACCOUNT_PERFLOGUSERS_NODOMAIN", FALSE);

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/********************************************************************
DetectWDDMDriver

 Set a property if the driver on the machine is a WDDM driver. One
 reliable way to detect the presence of a WDDM driver is to try and
 use the Direct3DCreate9Ex() function. This method attempts that
 then sets the property appropriately.
********************************************************************/
static HRESULT DetectWDDMDriver()
{
    HRESULT hr = S_OK;
    HMODULE hModule = NULL;

    // Manually load the d3d9.dll library. If the library couldn't be loaded then we obviously won't be able
    // to try calling the function so just return.
    hr = LoadSystemLibrary(L"d3d9.dll", &hModule);
    if (E_MODNOTFOUND == hr)
    {
        TraceError(hr, "Unable to load DirectX APIs, skipping WDDM driver check.");
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to the load the existing DirectX APIs.");

    // Obtain the address of the Direct3DCreate9Ex function. If this fails we know it isn't a WDDM
    // driver so just exit.
    const void* Direct3DCreate9ExPtr = ::GetProcAddress(hModule, "Direct3DCreate9Ex");
    ExitOnNull(Direct3DCreate9ExPtr, hr, S_OK, "Unable to load Direct3DCreateEx function, so the driver is not a WDDM driver.");

    // At this point we know it's a WDDM driver so set the property.
    hr = WcaSetIntProperty(L"WIX_WDDM_DRIVER_PRESENT", 1);
    ExitOnFailure(hr, "Failed write property");

LExit:
    if (NULL != hModule)
    {
        FreeLibrary(hModule);
    }

    return hr;
}

/********************************************************************
DetectIsCompositionEnabled

 Set a property based on the return value of DwmIsCompositionEnabled().
********************************************************************/
static HRESULT DetectIsCompositionEnabled()
{
    HRESULT hr = S_OK;
    HMODULE hModule = NULL;
    BOOL compositionState = false;

    // Manually load the d3d9.dll library. If the library can't load it's likely because we are not on a Vista
    // OS. Just return ok, and the property won't get set.
    hr = LoadSystemLibrary(L"dwmapi.dll", &hModule);
    if (E_MODNOTFOUND == hr)
    {
        TraceError(hr, "Unable to load Vista desktop window manager APIs, skipping Composition Enabled check.");
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to load the existing window manager APIs.");

    // If for some reason we can't get the function pointer that's ok, just return.
    typedef HRESULT (WINAPI *DWMISCOMPOSITIONENABLEDPTR)(BOOL*);
    DWMISCOMPOSITIONENABLEDPTR DwmIsCompositionEnabledPtr = (DWMISCOMPOSITIONENABLEDPTR)::GetProcAddress(hModule, "DwmIsCompositionEnabled");
    ExitOnNull(hModule, hr, S_OK, "Unable to obtain function information, skipping Composition Enabled check.");
    
    hr = DwmIsCompositionEnabledPtr(&compositionState);
    ExitOnFailure(hr, "Failed to retrieve Composition state");

    if (compositionState)
    {
        hr = WcaSetIntProperty(L"WIX_DWM_COMPOSITION_ENABLED", 1);
        ExitOnFailure(hr, "Failed write property");
    }

LExit:
    if (NULL != hModule)
    {
        FreeLibrary(hModule);
    }
    return hr;
}

/********************************************************************
WixQueryOsDriverInfo - entry point for WixQueryOsDriverInfo custom action

 Called as Type 1 custom action (DLL from the Binary table) from 
 Windows Installer to set properties about drivers installed on
 the target machine
********************************************************************/
extern "C" UINT __stdcall WixQueryOsDriverInfo(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixQueryOsDriverInfo");
    ExitOnFailure(hr, "WixQueryOsDriverInfo failed to initialize");

    // Detect the WDDM driver status
    hr = DetectWDDMDriver();
    ExitOnFailure(hr, "Failed to detect WIX_WDDM_DRIVER_PRESENT");

    // Detect whether composition is enabled
    hr = DetectIsCompositionEnabled();
    ExitOnFailure(hr, "Failed to detect WIX_DWM_COMPOSITION_ENABLED");

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}
