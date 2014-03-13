#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebdir7.h" company="Outercurve Foundation">
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


struct SCA_WEBDIR7
{
    // darwin information
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;


    // iis configuation information
    WCHAR wzPath[MAX_PATH];
    WCHAR wzWebSite[MAX_PATH];

    BOOL fHasProperties;
    SCA_WEB_PROPERTIES swp;

    BOOL fHasApplication;
    SCA_WEB_APPLICATION swapp;

    SCA_WEBDIR7* pswdNext;
};


// prototypes
UINT __stdcall ScaWebDirsRead7(
    __in SCA_WEB7* pswList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebBaseQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData,
    __out SCA_WEBDIR7** ppswdList
    );

HRESULT ScaWebDirsInstall7(
    __in SCA_WEBDIR7* pswdList,
    __in SCA_APPPOOL* psapList
    );

HRESULT ScaWebDirsUninstall7(
    __in SCA_WEBDIR7* pswdList
    );

void ScaWebDirsFreeList7(
    __in SCA_WEBDIR7* pswdList
    );
