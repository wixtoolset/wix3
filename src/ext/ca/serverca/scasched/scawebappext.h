#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct SCA_WEB_APPLICATION_EXTENSION
{
    WCHAR wzExtension[MAX_DARWIN_COLUMN + 1];

    WCHAR wzVerbs[MAX_DARWIN_COLUMN + 1];
    WCHAR wzExecutable[MAX_DARWIN_COLUMN + 1];
    int iAttributes;

    SCA_WEB_APPLICATION_EXTENSION* pswappextNext;
};


// prototypes
HRESULT ScaWebAppExtensionsRead(
    __in LPCWSTR wzApplication,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout SCA_WEB_APPLICATION_EXTENSION** ppswappextList
    );

HRESULT ScaWebAppExtensionsWrite(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzRootOfWeb,
    __in SCA_WEB_APPLICATION_EXTENSION* pswappextList
    );

void ScaWebAppExtensionsFreeList(
    __in SCA_WEB_APPLICATION_EXTENSION* pswappextList
    );
