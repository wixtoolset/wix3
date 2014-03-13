#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaweb.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    IIS Web functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scawebapp.h"
#include "scawebprop.h"
#include "scahttpheader.h"
#include "scaweberr.h"
#include "scassl.h"
#include "scaapppool.h"
#include "scaweblog.h"
#include "scamimemap.h"

// globals
#define MAX_ADDRESSES_PER_WEB 10

enum eWebQuery { wqWeb = 1, wqComponent, wqId, wqDescription, wqConnectionTimeout, wqDirectory,
                 wqState, wqAttributes, wqProperties, wqApplication, wqAddress, wqIP, wqPort, wqHeader, wqSecure, wqLog, wqInstalled, wqAction, wqSourcePath, wqTargetPath};

enum eWebAddressQuery { waqAddress = 1, waqWeb, waqIP, waqPort, waqHeader, waqSecure };

enum SCA_WEB_ATTRIBUTES
{
    SWATTRIB_NOCONFIGUREIFEXISTS = 2
};

// structs
struct SCA_WEB_ADDRESS
{
    WCHAR wzKey [MAX_DARWIN_KEY + 1];

    WCHAR wzIP[MAX_DARWIN_COLUMN + 1];
    int iPort;
    WCHAR wzHeader[MAX_DARWIN_COLUMN + 1];
    BOOL fSecure;
};

struct SCA_WEB
{
    // darwin information
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    BOOL fHasComponent;
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    // metabase information
    WCHAR wzWebBase[METADATA_MAX_NAME_LEN + 1];
    BOOL fBaseExists;

    // iis configuation information
    SCA_WEB_ADDRESS swaKey;

    SCA_WEB_ADDRESS swaExtraAddresses[MAX_ADDRESSES_PER_WEB + 1];
    DWORD cExtraAddresses;

    WCHAR wzDirectory[MAX_PATH];
    WCHAR wzDescription[MAX_DARWIN_COLUMN + 1];

    int iState;
    int iAttributes;

    BOOL fHasProperties;
    SCA_WEB_PROPERTIES swp;

    BOOL fHasApplication;
    SCA_WEB_APPLICATION swapp;

    BOOL fHasSecurity;
    int dwAccessPermissions;
    int iConnectionTimeout;

    SCA_MIMEMAP* psmm; // mime mappings
    SCA_WEB_SSL_CERTIFICATE* pswscList;
    SCA_HTTP_HEADER* pshhList;
    SCA_WEB_ERROR* psweList;

    BOOL fHasLog;
    SCA_WEB_LOG swl;

    SCA_WEB* pswNext;
};


// prototypes
HRESULT ScaWebsRead(
    __in IMSAdminBase* piMetabase,
    __in SCA_MIMEMAP** ppsmmList,
    __in SCA_WEB** ppswList,
    __in SCA_HTTP_HEADER** pshhList,
    __in SCA_WEB_ERROR** psweList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hSslCertQuery,
    __in WCA_WRAPQUERY_HANDLE hWebLogQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData
    );

HRESULT ScaWebsGetBase(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in LPCWSTR wzWeb,
    __out_ecount(cchWebBase) LPWSTR wzWebBase,
    __in DWORD cchWebBase,
    __in WCA_WRAPQUERY_HANDLE hWrapQuery
    );

HRESULT ScaWebsInstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in SCA_APPPOOL * psapList
    );

HRESULT ScaWebsUninstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList
    );

void ScaWebsFreeList(
    __in SCA_WEB* pswHead
    );
