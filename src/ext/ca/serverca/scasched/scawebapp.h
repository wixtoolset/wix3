#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scawebapp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Web Application functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

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

