#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaapppool7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Application Pool functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scauser.h"

// Identity
#define APATTR_NETSERVICE 0x0001 // Network Service
#define APATTR_LOCSERVICE 0x0002 // Local Service
#define APATTR_LOCSYSTEM 0x0004 // Local System
#define APATTR_OTHERUSER 0x0008 // Other User
#define APATTR_APPPOOLIDENTITY 0x0010 // ApplicationPoolIdentity

// prototypes
HRESULT ScaFindAppPool7(
    __in LPCWSTR wzAppPool,
    __out_ecount(cchName) LPWSTR wzName,
    __in DWORD cchName,
    __in SCA_APPPOOL *psapList
    );

HRESULT ScaAppPoolInstall7(
    __in SCA_APPPOOL* psapList
    );

HRESULT ScaAppPoolUninstall7(
    __in SCA_APPPOOL* psapList
    );

HRESULT ScaWriteAppPool7(
    __in const SCA_APPPOOL* psap
    );

HRESULT ScaRemoveAppPool7(
    __in const SCA_APPPOOL* psap
    );
