#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "scaapppool.h"
#include "scawebappext.h"

// global sql queries provided for optimization
extern LPCWSTR vcsWebApplicationQuery;
const int MAX_APP_NAME = 255;

// structs
struct SCA_WEB_APPLICATION
{
	WCHAR wzName[MAX_APP_NAME + 1];

	int iIsolation;
	BOOL fAllowSessionState;
	int iSessionTimeout;
	BOOL fBuffer;
	BOOL fParentPaths;

	WCHAR wzDefaultScript[MAX_DARWIN_COLUMN + 1];
	int iScriptTimeout;
	BOOL fServerDebugging;
	BOOL fClientDebugging;
	WCHAR wzAppPool[MAX_DARWIN_COLUMN + 1];

	SCA_WEB_APPLICATION_EXTENSION* pswappextList;
};


// prototypes
HRESULT ScaGetWebApplication(MSIHANDLE hViewApplications, 
                             LPCWSTR pwzApplication,
                             __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
                             __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
                             SCA_WEB_APPLICATION* pswapp);

HRESULT ScaWriteWebApplication(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb, 
                               SCA_WEB_APPLICATION* pswapp, SCA_APPPOOL * psapList);

