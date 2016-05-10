#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


HRESULT ScaGetWebLog7(
    __in_z LPCWSTR wzLog,
    __in WCA_WRAPQUERY_HANDLE hWebLogQuery,
    __out SCA_WEB_LOG* pswl
    );

HRESULT ScaWriteWebLog7(
    __in_z LPCWSTR wzRootOfWeb,
    const SCA_WEB_LOG *pswl
    );
