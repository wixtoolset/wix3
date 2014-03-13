#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scasmb.h" company="Outercurve Foundation">
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

#include "scauser.h"

// structs
// Structure used to hold and extra user/permission pairs from the FileSharePermissions Table
struct SCA_SMB_EX_USER_PERMS
{
	int nPermissions;
	ACCESS_MODE accessMode;
	SCA_USER scau;
	SCA_SMB_EX_USER_PERMS* pExUserPermsNext;
};

struct SCA_SMB  // hungarian ss
{
	WCHAR wzId[MAX_DARWIN_KEY + 1];
	WCHAR wzShareName[MAX_DARWIN_KEY + 1];
	WCHAR wzDescription[MAX_DARWIN_KEY + 1];
	WCHAR wzComponent[MAX_DARWIN_KEY + 1];
	WCHAR wzDirectory[MAX_PATH + 1];

	int nUserPermissionCount;
	int nPermissions;
	SCA_SMB_EX_USER_PERMS* pExUserPerms;

	INSTALLSTATE isInstalled, isAction;

	BOOL fUseIntegratedAuth;
	BOOL fLegacyUserProvided;
	struct SCA_USER scau;

	struct SCA_SMB* pssNext;
};


#define RESERVED 0

// schedule prototypes
HRESULT ScaSmbRead(SCA_SMB** ppssList);
HRESULT ScaSmbExPermsRead(SCA_SMB* pss);
HRESULT ScaSmbUninstall(SCA_SMB* pssList);
HRESULT ScaSmbInstall(SCA_SMB* pssList);
void ScaSmbFreeList(SCA_SMB* pssList);
