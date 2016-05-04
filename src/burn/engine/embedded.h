#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
