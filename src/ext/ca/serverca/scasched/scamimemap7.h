#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scamimemap7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Mime Map functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

// prototypes

HRESULT ScaWriteMimeMap7(
    __in_z LPCWSTR wzWebName,
    __in_z LPCWSTR wzRootOfWeb,
    SCA_MIMEMAP* psmmList
    );

