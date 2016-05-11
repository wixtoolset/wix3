#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "scauser.h"

HRESULT ScaWriteWebDirProperties7(
    __in_z LPCWSTR wzwWebName,
    __in_z LPCWSTR wzRootOfWeb,
    const SCA_WEB_PROPERTIES* pswp
    );

