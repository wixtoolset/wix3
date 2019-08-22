#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct SCA_WEBDIR
{
    // darwin information
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    // metabase information
    WCHAR wzWebKey[MAX_DARWIN_KEY + 1];
    WCHAR wzWebBase[METADATA_MAX_NAME_LEN + 1];
    WCHAR wzWebDirRoot[METADATA_MAX_NAME_LEN + 1];

    // iis configuation information
    WCHAR wzDirectory[MAX_PATH];

    BOOL fHasProperties;
    SCA_WEB_PROPERTIES swp;

    BOOL fHasApplication;
    SCA_WEB_APPLICATION swapp;

    SCA_WEBDIR* pswdNext;
};


// prototypes
UINT __stdcall ScaWebDirsRead(
    __in IMSAdminBase* piMetabase, 
    __in SCA_WEB* pswList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebBaseQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData,
    __out SCA_WEBDIR** ppswdList
    );

HRESULT ScaWebDirsInstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEBDIR* pswdList,
    __in SCA_APPPOOL* psapList
    );

HRESULT ScaWebDirsUninstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEBDIR* pswdList
    );

void ScaWebDirsFreeList(
    __in SCA_WEBDIR* pswdList
    );
