//-------------------------------------------------------------------------------------------------
// <copyright file="embedded.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Module: Elevation
//
//    Burn elevation process handler.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

typedef enum _BURN_EMBEDDED_MESSAGE_TYPE
{
    BURN_EMBEDDED_MESSAGE_TYPE_UNKNOWN,
    BURN_EMBEDDED_MESSAGE_TYPE_ERROR,
    BURN_EMBEDDED_MESSAGE_TYPE_PROGRESS,
} BURN_EMBEDDED_MESSAGE_TYPE;


HRESULT EmbeddedRunBundle(
    __in LPCWSTR wzExecutablePath,
    __in LPCWSTR wzArguments,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out DWORD* pdwExitCode
    );

#ifdef __cplusplus
}
#endif
