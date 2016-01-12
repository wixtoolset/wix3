#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="apputil.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Application helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
extern "C" {
#endif

// functions
void DAPI AppInitialize(
    __in_ecount(cSafelyLoadSystemDlls) LPCWSTR rgsczSafelyLoadSystemDlls[],
    __in DWORD cSafelyLoadSystemDlls
    );

#ifdef __cplusplus
}
#endif
