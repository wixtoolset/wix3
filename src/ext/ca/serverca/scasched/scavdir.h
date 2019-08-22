#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "scawebprop.h"
#include "scawebapp.h"
#include "scamimemap.h"
#include "scaapppool.h"

enum eVDirQuery { vdqWeb = 1, vdqVDir, vdqComponent , vdqAlias, vdqDirectory, vdqProperties, vdqApplication, vdqInstalled, vdqAction, vdqSourcePath, vdqTargetPath };

struct SCA_VDIR
{
    // darwin information
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    // metabase information
    WCHAR wzWebKey[MAX_DARWIN_KEY + 1];
    WCHAR wzWebBase[METADATA_MAX_NAME_LEN + 1];
    WCHAR wzVDirRoot[METADATA_MAX_NAME_LEN + 1];

    // iis configuation information
    WCHAR wzDirectory[MAX_PATH];

    BOOL fHasProperties;
    SCA_WEB_PROPERTIES swp;

    BOOL fHasApplication;
    SCA_WEB_APPLICATION swapp;

    SCA_MIMEMAP* psmm; // mime mappings
    SCA_HTTP_HEADER* pshh; // custom web errors
    SCA_WEB_ERROR* pswe; // custom web errors

    SCA_VDIR* psvdNext;
};


// prototypes
HRESULT __stdcall ScaVirtualDirsRead(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in SCA_VDIR** ppsvdList,
    __in SCA_MIMEMAP** ppsmmList,
    __in SCA_HTTP_HEADER** ppshhList,
    __in SCA_WEB_ERROR** ppsweList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebBaseQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData
    );

HRESULT ScaVirtualDirsInstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_VDIR* psvdList,
    __in SCA_APPPOOL * psapList
    );

HRESULT ScaVirtualDirsUninstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_VDIR* psvdList
    );

void ScaVirtualDirsFreeList(
    __in SCA_VDIR* psvdList
    );
