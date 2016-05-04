#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct SCA_SMBP_USER_PERMS
{
	DWORD nPermissions;
	ACCESS_MODE accessMode;
	WCHAR* wzUser;
	//Not adding Password because I can't find anywhere that it is used
};

struct SCA_SMBP  // hungarian ssp
{
	WCHAR* wzKey;
	WCHAR* wzDescription;
	WCHAR* wzComponent;
	WCHAR* wzDirectory;  // full path of the dir to share to

	DWORD dwUserPermissionCount;  //Count of SCA_SMBP_EX_USER_PERMS structures
	SCA_SMBP_USER_PERMS* pUserPerms;
	BOOL fUseIntegratedAuth;
};


HRESULT ScaEnsureSmbExists(SCA_SMBP* pssp);
HRESULT ScaDropSmb(SCA_SMBP* pssp);
