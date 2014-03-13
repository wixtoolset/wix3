#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scafilter7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Filter functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scaweb.h"

// prototypes
UINT __stdcall ScaFiltersRead7(
    __in SCA_WEB7* pswList,
    __in WCA_WRAPQUERY_HANDLE hWebBaseQuery, 
    __inout SCA_FILTER** ppsfList,
    __inout LPWSTR *ppwzCustomActionData
    );

HRESULT ScaFiltersInstall7(
    SCA_FILTER* psfList
    );

HRESULT ScaFiltersUninstall7(
    SCA_FILTER* psfList
    );
