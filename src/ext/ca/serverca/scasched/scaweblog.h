#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaweblog.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Custom Actions for handling log settings for a particular IIS Website
// </summary>
//-------------------------------------------------------------------------------------------------

struct SCA_WEB_LOG
{
	// iis configuation information
	WCHAR wzLog[MAX_DARWIN_KEY + 1];

	// for specifying the log format
	WCHAR wzFormat[MAX_DARWIN_KEY + 1];
	WCHAR wzFormatGUID[MAX_DARWIN_KEY + 1];
};


// prototypes
HRESULT ScaGetWebLog(
	IMSAdminBase* piMetabase,
	LPCWSTR wzLog,
    __in WCA_WRAPQUERY_HANDLE hWebLogQuery,
	SCA_WEB_LOG* pswl
	);
HRESULT ScaWriteWebLog(
	IMSAdminBase* piMetabase,
	LPCWSTR wzRootOfWeb,
	SCA_WEB_LOG *pswl
	);
