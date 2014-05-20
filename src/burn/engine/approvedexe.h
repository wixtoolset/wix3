//-------------------------------------------------------------------------------------------------
// <copyright file="approvedexe.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif


// structs

typedef struct _BURN_APPROVED_EXE
{
    LPWSTR sczId;
    DWORD64 qwFileSize;
    BYTE* pbHash;
    DWORD cbHash;
} BURN_APPROVED_EXE;

typedef struct _BURN_APPROVED_EXES
{
    BURN_APPROVED_EXE* rgApprovedExes;
    DWORD cApprovedExes;
} BURN_APPROVED_EXES;

typedef struct _BURN_LAUNCH_APPROVED_EXE
{
    HWND hwndParent;
    LPWSTR sczId;
    LPWSTR sczExecutablePath;
    LPWSTR sczArguments;
    DWORD dwWaitForInputIdleTimeout;
} BURN_LAUNCH_APPROVED_EXE;


// function declarations

HRESULT ApprovedExesParseFromXml(
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in IXMLDOMNode* pixnBundle
    );

HRESULT ApprovedExesUninitialize(
    __in BURN_APPROVED_EXES* pApprovedExes
    );
HRESULT ApprovedExesUninitializeLaunch(
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    );
HRESULT ApprovedExesFindById(
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in_z LPCWSTR wzId,
    __out BURN_APPROVED_EXE** ppApprovedExe
    );
HRESULT ApprovedExesLaunch(
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe,
    __out DWORD* pdwProcessId
    );
HRESULT ApprovedExesVerifySecureLocation(
    __in BURN_VARIABLES* pVariables,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    );
HRESULT PathCanonicalizePath(
    __in_z LPCWSTR wzPath,
    __deref_out_z LPWSTR* psczCanonicalized
    );
HRESULT PathDirectoryContainsPath(
    __in_z LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPath
    );


#if defined(__cplusplus)
}
#endif
