#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebdir.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Web Directory functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

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
