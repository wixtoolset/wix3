#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scaweberr.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Web Error functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

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

