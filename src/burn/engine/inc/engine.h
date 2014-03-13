//-------------------------------------------------------------------------------------------------
// <copyright file="engine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
//
//    Setup chainer/bootstrapper stub entry-points for WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT EngineRun(
    __in HINSTANCE hInstance,
    __in_z_opt LPCWSTR wzCommandLine,
    __in int nCmdShow,
    __out DWORD* pdwExitCode
    );


#if defined(__cplusplus)
}
#endif
