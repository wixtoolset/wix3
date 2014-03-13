//-------------------------------------------------------------------------------------------------
// <copyright file="manifest.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

interface IBurnPayload; // forward declare.

#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT ManifestLoadXmlFromBuffer(
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __in BURN_ENGINE_STATE* pEngineState
    );


#if defined(__cplusplus)
}
#endif
