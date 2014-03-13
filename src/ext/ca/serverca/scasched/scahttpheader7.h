#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scahttpheader7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS HTTP Header functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

// prototypes

HRESULT ScaGetHttpHeader7(
    __in int iParentType,
    __in_z LPCWSTR wzParentValue,
    __in SCA_HTTP_HEADER** ppshhList,
    __out SCA_HTTP_HEADER** ppshhOut
    );
HRESULT ScaWriteHttpHeader7(
    __in_z LPCWSTR wzWebName,
    __in_z LPCWSTR wzRoot,
    SCA_HTTP_HEADER* pshhList
    );
