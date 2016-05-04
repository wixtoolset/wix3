#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
