//-------------------------------------------------------------------------------------------------
// <copyright file="DriveCheck.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Validate install drive
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static HRESULT PathIsRemote(__in LPCWSTR pTargetFolder, __inout BOOL* fPathRemote);
static HRESULT PathIsRemovable(__in LPCWSTR pTargetFolder, __inout BOOL* fPathRemovable);

/********************************************************************
 ValidatePath - Custom Action entry point

********************************************************************/
UINT __stdcall ValidatePath(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    
    LPWSTR pwszWixUIDir = NULL;
    LPWSTR pwszInstallPath = NULL;
    BOOL fInstallPathIsRemote = TRUE;
    BOOL fInstallPathIsRemoveable = TRUE;
    
    hr = WcaInitialize(hInstall, "ValidatePath");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty(L"WIXUI_INSTALLDIR", &pwszWixUIDir);
    ExitOnFailure(hr, "failed to get WixUI Installation Directory");
    
    hr = WcaGetProperty(pwszWixUIDir, &pwszInstallPath);
    ExitOnFailure(hr, "failed to get Installation Directory");
    
    hr = PathIsRemote(pwszInstallPath, &fInstallPathIsRemote);
    if (FAILED(hr))
    {
        TraceError(hr, "Unable to determine if path is remote");
        //reset HR, as we need to continue and find out if is a UNC path
        hr = S_OK;
    }
    
    hr = PathIsRemovable(pwszInstallPath, &fInstallPathIsRemoveable);
    if (FAILED(hr))
    {
        TraceError(hr, "Unable to determine if path is removable");
        //reset HR, as we need to continue and find out if is a UNC path
        hr = S_OK;
    }

    // If the path does not point to a network drive, mapped drive, or removable drive,
    // then set WIXUI_INSTALLDIR_VALID to "1" otherwise set it to 0
    BOOL fInstallPathIsUnc = PathIsUNCW(pwszInstallPath);
    if (!fInstallPathIsUnc && !fInstallPathIsRemote && !fInstallPathIsRemoveable)
    {
        // path is valid
        hr = WcaSetProperty(L"WIXUI_INSTALLDIR_VALID",  L"1");
        ExitOnFailure(hr, "failed to set WIXUI_INSTALLDIR_VALID");
    }
    else
    {
        // path is invalid; we can't log it because we're being called from a DoAction control event
        // but we can at least call WcaLog to get it to write to the debugger from a debug build
        WcaLog(LOGMSG_STANDARD, "Installation path %ls is invalid: it is %s UNC path, %s remote path, or %s path on a removable drive, and must be none of these.",
            pwszInstallPath, fInstallPathIsUnc ? "a" : "not a", fInstallPathIsRemote ? "a" : "not a", fInstallPathIsRemoveable ? "a" : "not a");
        hr = WcaSetProperty(L"WIXUI_INSTALLDIR_VALID",  L"0");
        ExitOnFailure(hr, "failed to set WIXUI_INSTALLDIR_VALID");
    }
    
LExit:
    ReleaseStr(pwszInstallPath);
    ReleaseStr(pwszWixUIDir);

    return WcaFinalize(SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE);
}

/********************************************************************
 PathIsRemote - helper function for ValidatePath

********************************************************************/
static HRESULT PathIsRemote(__in LPCWSTR pTargetFolder, __inout BOOL* fPathRemote)
{
    HRESULT hr = S_OK;
    LPWSTR pStrippedTargetFolder = NULL;

    hr = StrAllocString(&pStrippedTargetFolder, pTargetFolder, 0);
    
    // Terminate the path at the root
    if(!::PathStripToRootW(pStrippedTargetFolder))
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DRIVE);
        ExitOnFailure(hr, "failed to parse target folder");    
    }
    
    UINT uResult = GetDriveTypeW(pStrippedTargetFolder);
    
    *fPathRemote = (DRIVE_REMOTE == uResult) ;

LExit:
    ReleaseStr(pStrippedTargetFolder);

    return hr;        
}

/********************************************************************
 PathIsRemovable - helper function for ValidatePath

********************************************************************/
static HRESULT PathIsRemovable(__in LPCWSTR pTargetFolder, __inout BOOL* fPathRemovable)
{
    HRESULT hr = S_OK;
    LPWSTR pStrippedTargetFolder = NULL;

    hr = StrAllocString(&pStrippedTargetFolder, pTargetFolder, 0);
    
    // Terminate the path at the root
    if(!::PathStripToRootW(pStrippedTargetFolder))
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DRIVE);
        ExitOnFailure(hr, "failed to parse target folder");    
    }
    
    UINT uResult = GetDriveTypeW(pStrippedTargetFolder);
    
    *fPathRemovable = ((DRIVE_CDROM == uResult) || (DRIVE_REMOVABLE == uResult) || (DRIVE_RAMDISK == uResult) || (DRIVE_UNKNOWN == uResult));

LExit:
    ReleaseStr(pStrippedTargetFolder);

    return hr;
}
