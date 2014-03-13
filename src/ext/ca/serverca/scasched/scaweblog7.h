#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaweblog7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Custom Actions for handling log settings for a particular IIS Website
// </summary>
//-------------------------------------------------------------------------------------------------

// prototypes
HRESULT ScaGetWebLog7(
    __in_z LPCWSTR wzLog,
    __in WCA_WRAPQUERY_HANDLE hWebLogQuery,
    __out SCA_WEB_LOG* pswl
    );

HRESULT ScaWriteWebLog7(
    __in_z LPCWSTR wzRootOfWeb,
    const SCA_WEB_LOG *pswl
    );
