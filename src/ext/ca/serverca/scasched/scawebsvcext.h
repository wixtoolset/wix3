#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebsvcext.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Web Service Extension functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

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
