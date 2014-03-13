#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebprop7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    IIS Web Directory Property functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scauser.h"

HRESULT ScaWriteWebDirProperties7(
    __in_z LPCWSTR wzwWebName,
    __in_z LPCWSTR wzRootOfWeb,
    const SCA_WEB_PROPERTIES* pswp
    );

