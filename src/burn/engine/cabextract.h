//-------------------------------------------------------------------------------------------------
// <copyright file="cabextract.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Cabinet
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
