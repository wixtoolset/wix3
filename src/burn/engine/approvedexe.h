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
    LPWSTR sczKey;
    LPWSTR sczValueName;
    BOOL fWin64;
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

void ApprovedExesUninitialize(
    __in BURN_APPROVED_EXES* pApprovedExes
    );
void ApprovedExesUninitializeLaunch(
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    );
HRESULT ApprovedExesFindById(
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in_z LPCWSTR wzId,
    __out BURN_APPROVED_EXE** ppApprovedExe
    );
HRESULT ApprovedExesLaunch(
    __in BURN_VARIABLES* pVariables,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe,
    __out DWORD* pdwProcessId
    );
HRESULT ApprovedExesVerifySecureLocation(
    __in BURN_VARIABLES* pVariables,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    );


#if defined(__cplusplus)
}
#endif
