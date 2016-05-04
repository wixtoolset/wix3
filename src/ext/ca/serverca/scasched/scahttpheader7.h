#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
