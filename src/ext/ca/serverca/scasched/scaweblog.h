#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
