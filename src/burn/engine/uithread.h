//-------------------------------------------------------------------------------------------------
// <copyright file="uithread.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Message handler window
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif


// functions

HRESULT UiCreateMessageWindow(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    );

void UiCloseMessageWindow(
    __in BURN_ENGINE_STATE* pEngineState
    );

#if defined(__cplusplus)
}
#endif
