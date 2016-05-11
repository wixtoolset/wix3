#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// constants

enum WM_BURN
{
    WM_BURN_FIRST = WM_APP + 0xFFF, // this enum value must always be first.

    WM_BURN_DETECT,
    WM_BURN_PLAN,
    WM_BURN_ELEVATE,
    WM_BURN_APPLY,
    WM_BURN_LAUNCH_APPROVED_EXE,
    WM_BURN_QUIT,

    WM_BURN_LAST, // this enum value must always be last.
};

// function declarations

HRESULT EngineForApplicationCreate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in DWORD dwThreadId,
    __out IBootstrapperEngine** ppEngineForApplication
    );

#if defined(__cplusplus)
}
#endif
