#pragma once 
//-------------------------------------------------------------------------------------------------
// <copyright file="scadb.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    DB functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "scauser.h"
#include "sqlutil.h"

struct SCA_DB
{
    // darwin information
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    BOOL fHasComponent;
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    INSTALLSTATE isInstalled, isAction;

    WCHAR wzServer[MAX_DARWIN_COLUMN + 1];
    WCHAR wzInstance[MAX_DARWIN_COLUMN + 1];
    WCHAR wzDatabase[MAX_DARWIN_COLUMN + 1];

    int iAttributes;

    BOOL fUseIntegratedAuth;
    SCA_USER scau;

    BOOL fHasDbSpec;
    SQL_FILESPEC sfDb;
    BOOL fHasLogSpec;
    SQL_FILESPEC sfLog;

    SCA_DB* psdNext;
};


// prototypes
HRESULT ScaDbsRead(
    __inout SCA_DB** ppsdList,
    __in SCA_ACTION saAction
    );

SCA_DB* ScaDbsFindDatabase(
    __in LPCWSTR wzSqlDb,
    __in SCA_DB* psdList
    );

HRESULT ScaDbsInstall(
    __in SCA_DB* psdList
    );

HRESULT ScaDbsUninstall(
    __in SCA_DB* psdList
    );

void ScaDbsFreeList(
    __in SCA_DB* psdList
    );
