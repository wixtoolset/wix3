#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scasmbexec.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    File share functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

//Structure used to hold the permission User Name pairs
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
