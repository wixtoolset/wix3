#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#define wzIISPROPERTY_IIS5_ISOLATION_MODE L"IIs5IsolationMode"
#define wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH L"MaxGlobalBandwidth"
#define wzIISPROPERTY_LOG_IN_UTF8 L"LogInUTF8"
#define wzIISPROPERTY_ETAG_CHANGENUMBER L"ETagChangeNumber"

struct SCA_PROPERTY
{
	// iis configuation information
	WCHAR wzProperty[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;
	INT iAttributes;
	WCHAR wzValue[MAX_DARWIN_COLUMN + 1];

	SCA_PROPERTY *pspNext;
};


// prototypes

HRESULT ScaPropertyRead(
	SCA_PROPERTY** ppspList,
    __inout LPWSTR *ppwzCustomActionData
	);

void ScaPropertyFreeList(
	SCA_PROPERTY* pspList
	);

HRESULT ScaPropertyInstall(
	IMSAdminBase* piMetabase, 
	SCA_PROPERTY* pspList
	);

HRESULT ScaPropertyUninstall(
	IMSAdminBase* piMetabase, 
	SCA_PROPERTY* pspList
	);

HRESULT ScaWriteProperty(
	IMSAdminBase* piMetabase, 
	SCA_PROPERTY* psp
	);

HRESULT ScaRemoveProperty(
	IMSAdminBase* piMetabase, 
	SCA_PROPERTY* psp
	);

