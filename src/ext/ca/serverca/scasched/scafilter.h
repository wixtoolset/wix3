#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "scaweb.h"

enum eFilterQuery { fqWeb = 1, fqFilter, fqComponent , fqPath, fqDescription, fqFlags, fqLoadOrder, fqInstalled, fqAction };

struct SCA_FILTER
{
	// darwin information
	WCHAR wzKey[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	// metabase information
	WCHAR wzWebKey[MAX_DARWIN_KEY + 1];
	WCHAR wzWebBase[METADATA_MAX_NAME_LEN + 1];
	WCHAR wzFilterRoot[METADATA_MAX_NAME_LEN + 1];

	// iis configuation information
	WCHAR wzPath[MAX_PATH];
	WCHAR wzDescription[MAX_DARWIN_COLUMN + 1];
	int iFlags;
	int iLoadOrder;

	SCA_FILTER* psfNext;
};


// prototypes
HRESULT AddFilterToList(
    __in SCA_FILTER** ppsfList
    );

UINT __stdcall ScaFiltersRead(IMSAdminBase* piMetabase,
                              SCA_WEB* pswList, __in WCA_WRAPQUERY_HANDLE hWebBaseQuery, SCA_FILTER** ppsfList,
                              __inout LPWSTR *ppwzCustomActionData);

HRESULT ScaFiltersInstall(IMSAdminBase* piMetabase, SCA_FILTER* psfList);

HRESULT ScaFiltersUninstall(IMSAdminBase* piMetabase, SCA_FILTER* psfList);

void ScaFiltersFreeList(SCA_FILTER* psfList);

