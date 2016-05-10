#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#define wzIISPROPERTY_IIS5_ISOLATION_MODE L"IIs5IsolationMode"
#define wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH L"MaxGlobalBandwidth"
#define wzIISPROPERTY_LOG_IN_UTF8 L"LogInUTF8"
#define wzIISPROPERTY_ETAG_CHANGENUMBER L"ETagChangeNumber"

// prototypes
HRESULT ScaPropertyInstall7(
    SCA_PROPERTY* pspList
    );

HRESULT ScaPropertyUninstall7(
    SCA_PROPERTY* pspList
    );

HRESULT ScaWriteProperty7(
    const SCA_PROPERTY* psp
    );

HRESULT ScaRemoveProperty7(
    SCA_PROPERTY* psp
    );

