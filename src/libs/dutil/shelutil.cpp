//-------------------------------------------------------------------------------------------------
// <copyright file="shelutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Shell helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static PFN_SHELLEXECUTEEXW vpfnShellExecuteExW = ::ShellExecuteExW;

static HRESULT GetDesktopShellView(
    __in REFIID riid,
    __out void **ppv
    );
static HRESULT GetShellDispatchFromView(
    __in IShellView *psv,
    __in REFIID riid,
    __out void **ppv
    );

/********************************************************************
 ShelFunctionOverride - overrides the shell functions. Typically used
                       for unit testing.

*********************************************************************/
extern "C" void DAPI ShelFunctionOverride(
    __in_opt PFN_SHELLEXECUTEEXW pfnShellExecuteExW
    )
{
    vpfnShellExecuteExW = pfnShellExecuteExW ? pfnShellExecuteExW : ::ShellExecuteExW;
}


/********************************************************************
 ShelExec() - executes a target.

*******************************************************************/
extern "C" HRESULT DAPI ShelExec(
    __in_z LPCWSTR wzTargetPath,
    __in_z_opt LPCWSTR wzParameters,
    __in_z_opt LPCWSTR wzVerb,
    __in_z_opt LPCWSTR wzWorkingDirectory,
    __in int nShowCmd,
    __in_opt HWND hwndParent,
    __out_opt HANDLE* phProcess
    )
{
    HRESULT hr = S_OK;
    SHELLEXECUTEINFOW shExecInfo = {};

    shExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
    shExecInfo.fMask = SEE_MASK_FLAG_DDEWAIT | SEE_MASK_FLAG_NO_UI | SEE_MASK_NOCLOSEPROCESS;
    shExecInfo.hwnd = hwndParent;
    shExecInfo.lpVerb = wzVerb;
    shExecInfo.lpFile = wzTargetPath;
    shExecInfo.lpParameters = wzParameters;
    shExecInfo.lpDirectory = wzWorkingDirectory;
    shExecInfo.nShow = nShowCmd;

    if (!vpfnShellExecuteExW(&shExecInfo))
    {
        ExitWithLastError1(hr, "ShellExecEx failed with return code: %d", Dutil_er);
    }

    if (phProcess)
    {
        *phProcess = shExecInfo.hProcess;
        shExecInfo.hProcess = NULL;
    }

LExit:
    ReleaseHandle(shExecInfo.hProcess);

    return hr;
}


/********************************************************************
 ShelExecUnelevated() - executes a target unelevated.

*******************************************************************/
extern "C" HRESULT DAPI ShelExecUnelevated(
    __in_z LPCWSTR wzTargetPath,
    __in_z_opt LPCWSTR wzParameters,
    __in_z_opt LPCWSTR wzVerb,
    __in_z_opt LPCWSTR wzWorkingDirectory,
    __in int nShowCmd
    )
{
    HRESULT hr = S_OK;
    BSTR bstrTargetPath = NULL;
    VARIANT vtParameters = { };
    VARIANT vtVerb = { };
    VARIANT vtWorkingDirectory = { };
    VARIANT vtShow = { };
    IShellView* psv = NULL;
    IShellDispatch2* psd = NULL;

    bstrTargetPath = ::SysAllocString(wzTargetPath);
    ExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate target path BSTR.");

    if (wzParameters && *wzParameters)
    {
        vtParameters.vt = VT_BSTR;
        vtParameters.bstrVal = ::SysAllocString(wzParameters);
        ExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate parameters BSTR.");
    }

    if (wzVerb && *wzVerb)
    {
        vtVerb.vt = VT_BSTR;
        vtVerb.bstrVal = ::SysAllocString(wzVerb);
        ExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate verb BSTR.");
    }

    if (wzWorkingDirectory && *wzWorkingDirectory)
    {
        vtWorkingDirectory.vt = VT_BSTR;
        vtWorkingDirectory.bstrVal = ::SysAllocString(wzWorkingDirectory);
        ExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate working directory BSTR.");
    }

    vtShow.vt = VT_INT;
    vtShow.intVal = nShowCmd;

    hr = GetDesktopShellView(IID_PPV_ARGS(&psv));
    ExitOnFailure(hr, "Failed to get desktop shell view.");

    hr = GetShellDispatchFromView(psv, IID_PPV_ARGS(&psd));
    ExitOnFailure(hr, "Failed to get shell dispatch from view.");

    hr = psd->ShellExecute(bstrTargetPath, vtParameters, vtWorkingDirectory, vtVerb, vtShow);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_CANCELLED);
    }
    ExitOnRootFailure1(hr, "Failed to launch unelevate executable: %ls", bstrTargetPath);

LExit:
    ReleaseObject(psd);
    ReleaseObject(psv);
    ReleaseBSTR(vtWorkingDirectory.bstrVal);
    ReleaseBSTR(vtVerb.bstrVal);
    ReleaseBSTR(vtParameters.bstrVal);
    ReleaseBSTR(bstrTargetPath);

    return hr;
}


/********************************************************************
 ShelGetFolder() - gets a folder by CSIDL.

*******************************************************************/
extern "C" HRESULT DAPI ShelGetFolder(
    __out_z LPWSTR* psczFolderPath,
    __in int csidlFolder
    )
{
    HRESULT hr = S_OK;
    WCHAR wzPath[MAX_PATH];

    hr = ::SHGetFolderPathW(NULL, csidlFolder | CSIDL_FLAG_CREATE, NULL, SHGFP_TYPE_CURRENT, wzPath);
    ExitOnFailure1(hr, "Failed to get folder path for CSIDL: %d", csidlFolder);

    hr = StrAllocString(psczFolderPath, wzPath, 0);
    ExitOnFailure1(hr, "Failed to copy shell folder path: %ls", wzPath);

    hr = PathBackslashTerminate(psczFolderPath);
    ExitOnFailure1(hr, "Failed to backslash terminate shell folder path: %ls", *psczFolderPath);

LExit:
    return hr;
}


/********************************************************************
 ShelGetKnownFolder() - gets a folder by KNOWNFOLDERID.

 Note: return E_NOTIMPL if called on pre-Vista operating systems.
*******************************************************************/
#ifndef REFKNOWNFOLDERID
#define REFKNOWNFOLDERID REFGUID
#endif

#ifndef KF_FLAG_CREATE
#define KF_FLAG_CREATE              0x00008000  // Make sure that the folder already exists or create it and apply security specified in folder definition
#endif

EXTERN_C typedef HRESULT (STDAPICALLTYPE *PFN_SHGetKnownFolderPath)(
    REFKNOWNFOLDERID rfid,
    DWORD dwFlags,
    HANDLE hToken,
    PWSTR *ppszPath
    );

extern "C" HRESULT DAPI ShelGetKnownFolder(
    __out_z LPWSTR* psczFolderPath,
    __in REFKNOWNFOLDERID rfidFolder
    )
{
    HRESULT hr = S_OK;
    HMODULE hShell32Dll = NULL;
    PFN_SHGetKnownFolderPath pfn = NULL;
    LPWSTR pwzPath = NULL;

    hr = LoadSystemLibrary(L"shell32.dll", &hShell32Dll);
    if (E_MODNOTFOUND == hr)
    {
        TraceError(hr, "Failed to load shell32.dll");
        ExitFunction1(hr = E_NOTIMPL);
    }
    ExitOnFailure(hr, "Failed to load shell32.dll.");

    pfn = reinterpret_cast<PFN_SHGetKnownFolderPath>(::GetProcAddress(hShell32Dll, "SHGetKnownFolderPath"));
    ExitOnNull(pfn, hr, E_NOTIMPL, "Failed to find SHGetKnownFolderPath entry point.");

    hr = pfn(rfidFolder, KF_FLAG_CREATE, NULL, &pwzPath);
    ExitOnFailure(hr, "Failed to get known folder path.");

    hr = StrAllocString(psczFolderPath, pwzPath, 0);
    ExitOnFailure1(hr, "Failed to copy shell folder path: %ls", pwzPath);

    hr = PathBackslashTerminate(psczFolderPath);
    ExitOnFailure1(hr, "Failed to backslash terminate shell folder path: %ls", *psczFolderPath);

LExit:
    if (pwzPath)
    {
        ::CoTaskMemFree(pwzPath);
    }

    if (hShell32Dll)
    {
        ::FreeLibrary(hShell32Dll);
    }

    return hr;
}


// Internal functions.

static HRESULT GetDesktopShellView(
    __in REFIID riid,
    __out void **ppv
    )
{
    HRESULT hr = S_OK;
    IShellWindows* psw = NULL;
    HWND hwnd = NULL;
    IDispatch* pdisp = NULL;
    VARIANT vEmpty = {}; // VT_EMPTY
    IShellBrowser* psb = NULL;
    IShellView* psv = NULL;

    // use the shell view for the desktop using the shell windows automation to find the 
    // desktop web browser and then grabs its view
    // returns IShellView, IFolderView and related interfaces
    hr = ::CoCreateInstance(CLSID_ShellWindows, NULL, CLSCTX_LOCAL_SERVER, IID_PPV_ARGS(&psw));
    ExitOnFailure(hr, "Failed to get shell view.");

    hr = psw->FindWindowSW(&vEmpty, &vEmpty, SWC_DESKTOP, (long*)&hwnd, SWFO_NEEDDISPATCH, &pdisp);
    ExitOnFailure(hr, "Failed to get desktop window.");

    hr = IUnknown_QueryService(pdisp, SID_STopLevelBrowser, IID_PPV_ARGS(&psb));
    ExitOnFailure(hr, "Failed to get desktop window.");

    hr = psb->QueryActiveShellView(&psv);
    ExitOnFailure(hr, "Failed to get active shell view.");

    hr = psv->QueryInterface(riid, ppv);
    ExitOnFailure(hr, "Failed to query for the desktop shell view.");

LExit:
    ReleaseObject(psv);
    ReleaseObject(psb);
    ReleaseObject(pdisp);
    ReleaseObject(psw);

    return hr;
}

static HRESULT GetShellDispatchFromView(
    __in IShellView *psv,
    __in REFIID riid,
    __out void **ppv
    )
{
    HRESULT hr = S_OK;
    IDispatch *pdispBackground = NULL;
    IShellFolderViewDual *psfvd = NULL;
    IDispatch *pdisp = NULL;

    // From a shell view object, gets its automation interface and from that get the shell
    // application object that implements IShellDispatch2 and related interfaces.
    hr = psv->GetItemObject(SVGIO_BACKGROUND, IID_PPV_ARGS(&pdispBackground));
    ExitOnFailure(hr, "Failed to get the automation interface for shell.");

    hr = pdispBackground->QueryInterface(IID_PPV_ARGS(&psfvd));
    ExitOnFailure(hr, "Failed to get shell folder view dual.");

    hr = psfvd->get_Application(&pdisp);
    ExitOnFailure(hr, "Failed to application object.");

    hr = pdisp->QueryInterface(riid, ppv);
    ExitOnFailure(hr, "Failed to get IShellDispatch2.");

LExit:
    ReleaseObject(pdisp);
    ReleaseObject(psfvd);
    ReleaseObject(pdispBackground);

    return hr;
}
