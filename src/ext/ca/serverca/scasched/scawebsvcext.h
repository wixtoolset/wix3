#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


enum SCA_WEBSVCEXT_ATTRIBUTES { SWSEATTRIB_ALLOW = 1, SWSEATTRIB_UIDELETABLE = 2 };

struct SCA_WEBSVCEXT
{
    // darwin information
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    // iis configuation information
    WCHAR wzFile[MAX_PATH + 1];
    WCHAR wzDescription[MAX_DARWIN_COLUMN + 1];
    WCHAR wzGroup[MAX_DARWIN_COLUMN + 1];

    int iAttributes;

    SCA_WEBSVCEXT* psWseNext;
};

HRESULT __stdcall ScaWebSvcExtRead(
    __in SCA_WEBSVCEXT** ppsWseList,
    __inout LPWSTR *ppwzCustomActionData
    );

HRESULT ScaWebSvcExtCommit(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEBSVCEXT* psWseList
    );

void ScaWebSvcExtFreeList(
    __in SCA_WEBSVCEXT* psWseList
    );
