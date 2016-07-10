#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


enum eHttpHeaderParentType { hhptVDir = 1, hhptWeb };

struct SCA_HTTP_HEADER
{
    int iParentType;
    WCHAR wzParentValue[MAX_DARWIN_KEY + 1];

    WCHAR wzName[MAX_PATH];
    WCHAR wzValue[MAX_PATH];
    int iAttributes;

    SCA_HTTP_HEADER* pshhNext;
};

// prototypes
HRESULT ScaHttpHeaderRead(
    __in SCA_HTTP_HEADER **ppshhList,
    __inout LPWSTR *ppwzCustomActionData
    );
void ScaHttpHeaderFreeList(
    __in SCA_HTTP_HEADER *pshhList
    );
HRESULT ScaHttpHeaderCheckList(
    __in SCA_HTTP_HEADER* pshhList
    );
HRESULT ScaGetHttpHeader(
    __in int iParentType,
    __in LPCWSTR wzParentValue,
    __in SCA_HTTP_HEADER** ppshhList,
    __out SCA_HTTP_HEADER** ppshhOut
    );
HRESULT ScaWriteHttpHeader(
    __in IMSAdminBase* piMetabase,
    LPCWSTR wzRoot,
    SCA_HTTP_HEADER* pshhList
    );
