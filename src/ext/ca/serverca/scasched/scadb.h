#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
