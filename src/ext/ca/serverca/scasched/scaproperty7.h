#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaproperty7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Property functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

// Settings
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

