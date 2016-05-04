#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

void CabExtractInitialize();
HRESULT CabExtractOpen(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in LPCWSTR wzFilePath
    );
HRESULT CabExtractNextStream(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout_z LPWSTR* psczStreamName
    );
HRESULT CabExtractStreamToFile(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    );
HRESULT CabExtractStreamToBuffer(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __out BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    );
HRESULT CabExtractSkipStream(
    __in BURN_CONTAINER_CONTEXT* pContext
    );
HRESULT CabExtractClose(
    __in BURN_CONTAINER_CONTEXT* pContext
    );


#if defined(__cplusplus)
}
#endif
