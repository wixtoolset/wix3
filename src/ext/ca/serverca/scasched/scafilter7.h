#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
