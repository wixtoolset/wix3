#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


enum eWebErrorParentType { weptVDir = 1, weptWeb };

struct SCA_WEB_ERROR
{
	int iErrorCode;
	int iSubCode;

	int iParentType;
	WCHAR wzParentValue[MAX_DARWIN_KEY + 1];

	WCHAR wzFile[MAX_PATH];
	WCHAR wzURL[MAX_PATH]; // TODO: this needs to be bigger than MAX_PATH
	
	SCA_WEB_ERROR *psweNext;
};

// prototypes
HRESULT ScaWebErrorRead(
                        SCA_WEB_ERROR **ppsweList,
                        __inout LPWSTR *ppwzCustomActionData
                        );
void ScaWebErrorFreeList(SCA_WEB_ERROR *psweList);
HRESULT ScaWebErrorCheckList(SCA_WEB_ERROR* psweList);
HRESULT ScaGetWebError(int iParentType, LPCWSTR wzParentValue, SCA_WEB_ERROR **ppsweList, SCA_WEB_ERROR **ppsweOut);
HRESULT ScaWriteWebError(IMSAdminBase* piMetabase, int iParentType, LPCWSTR wzRoot, SCA_WEB_ERROR* psweList);

