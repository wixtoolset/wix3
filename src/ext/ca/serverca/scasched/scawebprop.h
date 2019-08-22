#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "scauser.h"
 
// global sql queries provided for optimization
extern LPCWSTR vcsWebDirPropertiesQuery;


// structs
struct SCA_WEB_PROPERTIES
{
	WCHAR wzKey[MAX_DARWIN_KEY + 1];

	int iAccess;

	int iAuthorization;
	BOOL fHasUser;
	SCA_USER scau;
	BOOL fIIsControlledPassword;

	BOOL fLogVisits;
	BOOL fIndex;

	BOOL fHasDefaultDoc;
	WCHAR wzDefaultDoc[MAX_DARWIN_COLUMN + 1];

	BOOL fHasHttpExp;
	WCHAR wzHttpExp[MAX_DARWIN_COLUMN + 1];

	BOOL fAspDetailedError;

	int iCacheControlMaxAge;

	BOOL fHasCacheControlCustom;
	WCHAR wzCacheControlCustom[MAX_DARWIN_COLUMN + 1];

	BOOL fNoCustomError;

	int iAccessSSLFlags;

	WCHAR wzAuthenticationProviders[MAX_DARWIN_COLUMN + 1];
};
 

// prototypes
HRESULT ScaGetWebDirProperties(
    __in LPCWSTR pwzProperties,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __inout SCA_WEB_PROPERTIES* pswp
    );

HRESULT ScaWriteWebDirProperties(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzRootOfWeb, 
    __inout SCA_WEB_PROPERTIES* pswp
    );

