//-------------------------------------------------------------------------------------------------
// <copyright file="scasmbsched.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Schedule deferred custom action to create file shares.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


/********************************************************************
 Helper functions to maintain a list of file shares to create / remove

********************************************************************/
SCA_SMB* NewSmb()
{
    SCA_SMB* pss = (SCA_SMB*)MemAlloc(sizeof(SCA_SMB), TRUE);
    Assert(pss);
    return pss;
}


SCA_SMB_EX_USER_PERMS* NewExUserPermsSmb()
{
    SCA_SMB_EX_USER_PERMS* pExUserPerms = (SCA_SMB_EX_USER_PERMS*)MemAlloc(sizeof(SCA_SMB_EX_USER_PERMS), TRUE);
    Assert(pExUserPerms);
    return pExUserPerms;
}


SCA_SMB* AddSmbToList(SCA_SMB* pssList, SCA_SMB* pss)
{
    if (pssList)
    {
        SCA_SMB* pssT = pssList;
        while (pssT->pssNext)
        {
            pssT  = pssT->pssNext;
        }

        pssT->pssNext = pss;
    }
    else
    {
        pssList = pss;
    }

    return pssList;
}


SCA_SMB_EX_USER_PERMS* AddExUserPermsSmbToList(
    SCA_SMB_EX_USER_PERMS* pExUserPermsList, 
    SCA_SMB_EX_USER_PERMS* pExUserPerms
    )
{
    SCA_SMB_EX_USER_PERMS* pExUserPermsTemp = pExUserPermsList;
    if (pExUserPermsList)
    {
        while (pExUserPermsTemp->pExUserPermsNext)
        {
            pExUserPermsTemp = pExUserPermsTemp->pExUserPermsNext;
        }

        pExUserPermsTemp->pExUserPermsNext = pExUserPerms;
    }
    else
    {
        pExUserPermsList = pExUserPerms;
    }

    return pExUserPermsList;
}

void ScaSmbFreeList(SCA_SMB* pssList)
{
    SCA_SMB* pssDelete = pssList;
    while (pssList)
    {
        pssDelete = pssList;
        pssList = pssList->pssNext;

        MemFree(pssDelete);
    }
}

void ScaExUserPermsSmbFreeList(SCA_SMB_EX_USER_PERMS* pExUserPermsList)
{
    SCA_SMB_EX_USER_PERMS* pExUserPermsDelete = pExUserPermsList;
    while (pExUserPermsList)
    {
        pExUserPermsDelete = pExUserPermsList;
        pExUserPermsList = pExUserPermsList->pExUserPermsNext;

        MemFree(pExUserPermsDelete);
    }
}

// sql query constants
LPCWSTR vcsSmbQuery = L"SELECT `FileShare`, `ShareName`, `Description`, `Directory_`, "
    L"`Component_`, `User_`, `Permissions` FROM `FileShare`";

enum eSmbQuery {
    ssqFileShare = 1,
    ssqShareName,
    ssqDescription,
    ssqDirectory,
    ssqComponent,
    ssqUser,
    ssqPermissions
    };


/********************************************************************
 ScaSmbRead - read all of the information from the msi tables and 
              return a list of file share jobs to be done.

********************************************************************/
HRESULT ScaSmbRead(SCA_SMB** ppssList)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PMSIHANDLE hView, hRec;

    LPWSTR pwzData = NULL;

    SCA_SMB* pss = NULL;
    BOOL bUserPermissionsTableExists = FALSE;

    if (S_OK != WcaTableExists(L"FileShare"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaSmbCreateShare() - FileShare table not present");
        ExitFunction1(hr = S_FALSE);
    }

    if (S_OK == WcaTableExists(L"FileSharePermissions"))
    {
        bUserPermissionsTableExists = TRUE;
    }
    else
    {
        WcaLog(LOGMSG_VERBOSE, "No Additional Permissions - FileSharePermissions table not present");
    }

    WcaLog(LOGMSG_VERBOSE, "Reading File Share Tables");

    // loop through all the fileshares
    hr = WcaOpenExecuteView(vcsSmbQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on FileShare table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        pss = NewSmb();
        if (!pss)
        {
            hr = E_OUTOFMEMORY;
            break;
        }
        Assert(pss);
        ::ZeroMemory(pss, sizeof(*pss));

        hr = WcaGetRecordString(hRec, ssqFileShare, &pwzData);
        ExitOnFailure(hr, "Failed to get FileShare.FileShare");
        hr = ::StringCchCopyW(pss->wzId, countof(pss->wzId), pwzData);
        ExitOnFailure(hr, "Failed to copy ID string to smb object");

        hr = WcaGetRecordFormattedString(hRec, ssqShareName, &pwzData);
        ExitOnFailure(hr, "Failed to get FileShare.ShareName");
        hr = ::StringCchCopyW(pss->wzShareName, countof(pss->wzShareName), pwzData);
        ExitOnFailure(hr, "Failed to copy share name string to smb object");

        hr = WcaGetRecordString(hRec, ssqComponent, &pwzData);
        ExitOnFailure1(hr, "Failed to get Component for FileShare: '%ls'", pss->wzShareName);
        hr = ::StringCchCopyW(pss->wzComponent, countof(pss->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to smb object");

        hr = WcaGetRecordFormattedString(hRec, ssqDescription, &pwzData);
        ExitOnFailure1(hr, "Failed to get Share Description for FileShare: '%ls'", pss->wzShareName);
        hr = ::StringCchCopyW(pss->wzDescription, countof(pss->wzDescription), pwzData);
        ExitOnFailure(hr, "Failed to copy description string to smb object");

        // get user info from the user table
        hr = WcaGetRecordFormattedString(hRec, ssqUser, &pwzData);
        ExitOnFailure1(hr, "Failed to get User record for FileShare: '%ls'", pss->wzShareName);

        // get component install state
        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pss->wzComponent, &pss->isInstalled, &pss->isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "Failed to get Component state for FileShare");

        // if a user was specified
        if (*pwzData)
        {
            pss->fUseIntegratedAuth = FALSE;
            pss->fLegacyUserProvided = TRUE;
            hr = ScaGetUser(pwzData, &pss->scau);
            ExitOnFailure1(hr, "Failed to get user information for fileshare: '%ls'", pss->wzShareName);
        }
        else
        {
            pss->fLegacyUserProvided = FALSE;
            // TODO: figure out whether this is useful still
            //pss->fUseIntegratedAuth = TRUE;
            // integrated authorization doesn't have a User record
        }

        // get the share's directory
        hr = WcaGetRecordString(hRec, ssqDirectory, &pwzData);
        ExitOnFailure1(hr, "Failed to get directory for FileShare: '%ls'", pss->wzShareName);

        WCHAR wzPath[MAX_PATH];
        DWORD dwLen;
        dwLen = countof(wzPath);
        // review: relevant for file shares?
        if (INSTALLSTATE_SOURCE == pss->isAction)
        {
            er = ::MsiGetSourcePathW(WcaGetInstallHandle(), pwzData, wzPath, &dwLen);
        }
        else
        {
            er = ::MsiGetTargetPathW(WcaGetInstallHandle(), pwzData, wzPath, &dwLen);
        }
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "Failed to get Source/TargetPath for Directory");
        // remove trailing backslash
        if (dwLen > 0 && wzPath[dwLen-1] == L'\\')
        {
            wzPath[dwLen-1] = 0;
        }
        hr = ::StringCchCopyW(pss->wzDirectory, countof(pss->wzDirectory), wzPath);
        ExitOnFailure(hr, "Failed to copy directory string to smb object");

        hr = WcaGetRecordInteger(hRec, ssqPermissions, &pss->nPermissions);
        ExitOnFailure(hr, "Failed to get FileShare.Permissions");

        //Check to see if additional user & permissions are specified for this share
        if (bUserPermissionsTableExists)
        {
            hr = ScaSmbExPermsRead(pss);
            ExitOnFailure(hr, "Failed to get Additional File Share Permissions");
        }

        *ppssList = AddSmbToList(*ppssList, pss);
        pss = NULL; // set the smb NULL so it doesn't accidentally get freed below
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while processing FileShare table");

LExit:
    // if anything was left over after an error clean it all up
    if (pss)
    {
        ScaSmbFreeList(pss);
    }

    ReleaseStr(pwzData);

    return hr;
}


/********************************************************************
 RetrieveSMBShareUserPermList - retrieve SMB Share's user permission list

********************************************************************/
HRESULT RetrieveFileShareUserPerm(SCA_SMB* pss, SCA_SMB_EX_USER_PERMS** ppExUserPermsList, DWORD *pUserPermsCount)
{
    HRESULT hr = S_OK;
    SHARE_INFO_502* psi = NULL;
    NET_API_STATUS s;
    BOOL bValid, bDaclDefaulted;
    PACL acl = NULL;
    PEXPLICIT_ACCESSW pEA = NULL;
    ULONG nCount = 0;
    DWORD er = ERROR_SUCCESS;
    PSID pSID = NULL;
    DWORD nUserNameSize = MAX_DARWIN_COLUMN;
    DWORD nDomainNameSize = MAX_DARWIN_COLUMN;
    SID_NAME_USE peUse;
    DWORD dwCounter = 0;
    SCA_SMB_EX_USER_PERMS* pExUserPermsList = NULL;
    DWORD dwUserPermsCount = 0;

    *pUserPermsCount = 0;
    s = ::NetShareGetInfo(NULL, pss->wzShareName, 502, (LPBYTE*)&psi);
    WcaLog(LOGMSG_VERBOSE, "retrieving permissions on existing file share.");
    if (NERR_NetNameNotFound == s)
    {
        WcaLog(LOGMSG_VERBOSE, "File share has already been removed.");
        ExitFunction1(hr = S_OK);
    }
    else if (NERR_Success != s || psi == NULL)
    {
        hr = E_FAIL;
        ExitOnFailure1(hr, "Failed to get share information with return code: %d", s);
    }
    if (!::GetSecurityDescriptorDacl(psi->shi502_security_descriptor, &bValid, &acl, &bDaclDefaulted) || !bValid)
    {
        ExitOnLastError(hr, "Failed to get acl from security descriptor");
    }

    er = ::GetExplicitEntriesFromAclW(acl, &nCount, &pEA);
    hr = HRESULT_FROM_WIN32(er);
    ExitOnFailure1(hr, "Failed to get  access entries from acl for file share %ls", pss->wzShareName);
    for (dwCounter = 0; dwCounter < nCount; ++dwCounter)
    {
        if (TRUSTEE_IS_SID == pEA[dwCounter].Trustee.TrusteeForm)
        {
            SCA_SMB_EX_USER_PERMS* pExUserPerms = NewExUserPermsSmb();
            ::ZeroMemory(pExUserPerms, sizeof(*pExUserPerms));
            pExUserPermsList = AddExUserPermsSmbToList(pExUserPermsList, pExUserPerms);
            pSID = (PSID)(pEA[dwCounter].Trustee.ptstrName);
            if (!::LookupAccountSidW(NULL, pSID, pExUserPerms->scau.wzName, &nUserNameSize, pExUserPerms->scau.wzDomain, &nDomainNameSize, &peUse))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Failed to get account name from SID");
            }
            pExUserPerms->nPermissions = pEA[dwCounter].grfAccessPermissions;
            pExUserPerms->accessMode = pEA[dwCounter].grfAccessMode;
            ++dwUserPermsCount;
            nUserNameSize = MAX_DARWIN_COLUMN;
            nDomainNameSize = MAX_DARWIN_COLUMN;
        }
    }
    *ppExUserPermsList = pExUserPermsList;
    *pUserPermsCount = dwUserPermsCount;

LExit:
    if (psi)
    {
        ::NetApiBufferFree(psi);
    }

    if (pEA)
    {
        ::LocalFree(pEA);
    }

    return hr;
}


/********************************************************************
 SchedCreateSmb - schedule one instance of a file share creation

********************************************************************/
HRESULT SchedCreateSmb(SCA_SMB* pss)
{
    HRESULT hr = S_OK;

    WCHAR wzDomainUser[255]; // "domain\user"
    SCA_SMB_EX_USER_PERMS* pExUserPermsList = NULL;
    int nCounter = 0;
    WCHAR* pwzRollbackCustomActionData = NULL;
    WCHAR* pwzCustomActionData = NULL;

    hr = WcaWriteStringToCaData(pss->wzShareName, &pwzRollbackCustomActionData);
    ExitOnFailure(hr, "failed to add ShareName to CustomActionData");

    hr = WcaWriteStringToCaData(pss->wzShareName, &pwzCustomActionData);
    ExitOnFailure(hr, "failed to add ShareName to CustomActionData");

    hr = WcaWriteStringToCaData(pss->wzDescription, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add server name to CustomActionData");

    hr = WcaWriteStringToCaData(pss->wzDirectory, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add full path instance to CustomActionData");

    hr = WcaWriteStringToCaData(pss->fUseIntegratedAuth ? L"1" : L"0", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add server name to CustomActionData");

    if (pss->fLegacyUserProvided)
    {
        hr = WcaWriteIntegerToCaData(pss->nUserPermissionCount + 1, &pwzCustomActionData);
        ExitOnFailure(hr, "Failed to add additional user permission count to CustomActionData");

        hr = UserBuildDomainUserName(wzDomainUser, countof(wzDomainUser), pss->scau.wzName, pss->scau.wzDomain);
        ExitOnFailure(hr, "Failed to build user and domain name for CustomActionData");
        hr = WcaWriteStringToCaData(wzDomainUser, &pwzCustomActionData);
        ExitOnFailure(hr, "Failed to add server Domain\\UserName to CustomActionData");

        hr = WcaWriteIntegerToCaData(pss->nPermissions, &pwzCustomActionData);
        ExitOnFailure(hr, "Failed to add permissions to CustomActionData");
    }
    else
    {
        hr = WcaWriteIntegerToCaData(pss->nUserPermissionCount, &pwzCustomActionData);
        ExitOnFailure(hr, "Failed to add additional user permission count to CustomActionData");
    }

    if (pss->nUserPermissionCount > 0)
    {
        nCounter = 0;
        for (pExUserPermsList = pss->pExUserPerms; pExUserPermsList; pExUserPermsList = pExUserPermsList->pExUserPermsNext)
        {
            Assert(nCounter < pss->nUserPermissionCount);

            hr = UserBuildDomainUserName(wzDomainUser, countof(wzDomainUser), pExUserPermsList->scau.wzName, pExUserPermsList->scau.wzDomain);
            ExitOnFailure(hr, "Failed to build user and domain name for CustomActionData");
            hr = WcaWriteStringToCaData(wzDomainUser, &pwzCustomActionData);
            ExitOnFailure(hr, "Failed to add server Domain\\UserName to CustomActionData");

            hr = WcaWriteIntegerToCaData((int)pExUserPermsList->accessMode, &pwzCustomActionData);
            ExitOnFailure(hr, "Failed to add access mode to CustomActionData");
            
            hr = WcaWriteIntegerToCaData(pExUserPermsList->nPermissions, &pwzCustomActionData);
            ExitOnFailure(hr, "Failed to add permissions to CustomActionData");
            ++nCounter;
        }
        Assert(nCounter == pss->nUserPermissionCount);
    }

    // Schedule the rollback first
    hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"CreateSmbRollback"), pwzRollbackCustomActionData, COST_SMB_DROPSMB);
    ExitOnFailure(hr, "Failed to schedule DropSmb action");

    hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"CreateSmb"), pwzCustomActionData, COST_SMB_CREATESMB);
    ExitOnFailure(hr, "Failed to schedule CreateSmb action");

LExit:
    ReleaseStr(pwzRollbackCustomActionData);
    ReleaseStr(pwzCustomActionData);

    if (pExUserPermsList)
    {
        ScaExUserPermsSmbFreeList(pExUserPermsList);
    }

    return hr;
}


/********************************************************************
 ScaSmbInstall - for every file share, schedule the create custom action

********************************************************************/
HRESULT ScaSmbInstall(SCA_SMB* pssList)
{
    HRESULT hr = S_FALSE; // assume nothing will be done
    SCA_SMB* pss = NULL;

    for (pss = pssList; pss; pss = pss->pssNext)
    {
        // if installing this component
        if (WcaIsInstalling(pss->isInstalled, pss->isAction) )
        {
            hr = SchedCreateSmb(pss);
            ExitOnFailure1(hr, "Failed to schedule the creation of the fileshare: %ls", pss->wzShareName);
        }
    }

LExit:
    return hr;
}


/********************************************************************
 SchedDropSmb - schedule one instance of a file share removal

********************************************************************/
HRESULT SchedDropSmb(SCA_SMB* pss)
{
    HRESULT hr = S_OK;

    WCHAR* pwzCustomActionData = NULL;
    WCHAR* pwzRollbackCustomActionData = NULL;
    SCA_SMB_EX_USER_PERMS *pExUserPermsList = NULL;
    SCA_SMB_EX_USER_PERMS *pExUserPerm = NULL;
    WCHAR wzDomainUser[255]; // "domain\user"
    DWORD dwUserPermsCount = 0;
    
    // roll back DropSmb
    hr = WcaWriteStringToCaData(pss->wzShareName, &pwzRollbackCustomActionData);
    ExitOnFailure(hr, "failed to add ShareName to CustomActionData");

    hr = WcaWriteStringToCaData(pss->wzDescription, &pwzRollbackCustomActionData);
    ExitOnFailure(hr, "Failed to add server name to CustomActionData");

    hr = WcaWriteStringToCaData(pss->wzDirectory, &pwzRollbackCustomActionData);
    ExitOnFailure(hr, "Failed to add full path instance to CustomActionData");

    hr = WcaWriteStringToCaData(L"1", &pwzRollbackCustomActionData);
    ExitOnFailure(hr, "Failed to add useintegrated flag  to CustomActionData");

    hr = RetrieveFileShareUserPerm(pss, &pExUserPermsList, &dwUserPermsCount);
    ExitOnFailure(hr, "Failed to retrieve SMBShare's user permissions");

    hr = WcaWriteIntegerToCaData((int)dwUserPermsCount, &pwzRollbackCustomActionData);
    ExitOnFailure(hr, "Failed to add additional user permission count to CustomActionData");

    for (pExUserPerm = pExUserPermsList; pExUserPerm; pExUserPerm = pExUserPerm->pExUserPermsNext)
    {
        hr = UserBuildDomainUserName(wzDomainUser, countof(wzDomainUser), pExUserPerm->scau.wzName, pExUserPerm->scau.wzDomain);
        ExitOnFailure(hr, "Failed to build user and domain name for CustomActionData");
        hr = WcaWriteStringToCaData(wzDomainUser, &pwzRollbackCustomActionData);
        ExitOnFailure(hr, "Failed to add server Domain\\UserName to CustomActionData");

        hr = WcaWriteIntegerToCaData((int)pExUserPerm->accessMode, &pwzRollbackCustomActionData);
        ExitOnFailure(hr, "Failed to add access mode to CustomActionData");
        
        hr = WcaWriteIntegerToCaData(pExUserPerm->nPermissions, &pwzRollbackCustomActionData);
        ExitOnFailure(hr, "Failed to add permissions to CustomActionData");
    }

    hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"DropSmbRollback"), pwzRollbackCustomActionData, COST_SMB_CREATESMB);
    ExitOnFailure(hr, "Failed to schedule DropSmbRollback action");

    // DropSMB
    hr = WcaWriteStringToCaData(pss->wzShareName, &pwzCustomActionData);
    ExitOnFailure(hr, "failed to add ShareName to CustomActionData");

    hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"DropSmb"), pwzCustomActionData, COST_SMB_DROPSMB);
    ExitOnFailure(hr, "Failed to schedule DropSmb action");

LExit:
    ReleaseStr(pwzCustomActionData);

    if (pExUserPermsList)
    {
        ScaExUserPermsSmbFreeList(pExUserPermsList);
    }
    
    return hr;

}


/********************************************************************
 ScaSmbUninstall - for every file share, schedule the drop custom action

********************************************************************/
HRESULT ScaSmbUninstall(SCA_SMB* pssList)
{
    HRESULT hr = S_FALSE; // assume nothing will be done
    SCA_SMB* pss = NULL;

    for (pss = pssList; pss; pss = pss->pssNext)
    {
        // if uninstalling this component
        if (WcaIsUninstalling(pss->isInstalled, pss->isAction) )
        {
            hr = SchedDropSmb(pss);
            ExitOnFailure1(hr, "Failed to remove file share %ls", pss->wzShareName);
        }
    }

LExit:
    return hr;
}

LPCWSTR vcsSmbExUserPermsQuery = L"SELECT `FileShare_`,`User_`,`Permissions` "
    L"FROM `FileSharePermissions` WHERE `FileShare_`=?";

enum  eSmbUserPermsQuery {
    ssupqFileShare = 1,
    ssupqUser,
    ssupqPermissions

};


/********************************************************************
 ScaSmbExPermsRead - for Every entry in File Permissions table add a 
                     User Name & Permissions structure to the List

********************************************************************/
HRESULT ScaSmbExPermsRead(SCA_SMB* pss)
{
    HRESULT hr = S_OK;
    PMSIHANDLE hView, hRec;

    LPWSTR pwzData = NULL;
    SCA_SMB_EX_USER_PERMS* pExUserPermsList = pss->pExUserPerms;
    SCA_SMB_EX_USER_PERMS* pExUserPerms = NULL;
    int nCounter = 0;

    hRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hRec, 1, pss->wzId);
    ExitOnFailure(hr, "Failed to look up FileShare");

    hr = WcaOpenView(vcsSmbExUserPermsQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on FileSharePermissions table");
    hr = WcaExecuteView(hView, hRec);
    ExitOnFailure(hr, "Failed to execute view on FileSharePermissions table");

    // loop through all User/Permissions paris returned
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        pExUserPerms = NewExUserPermsSmb();
        if (!pExUserPerms)
        {
            hr = E_OUTOFMEMORY;
            break;
        }
        Assert(pExUserPerms);
        ::ZeroMemory(pExUserPerms, sizeof(*pExUserPerms));

        hr = WcaGetRecordString(hRec, ssupqUser, &pwzData);
        ExitOnFailure(hr, "Failed to get FileSharePermissions.User");
        hr = ScaGetUser(pwzData, &pExUserPerms->scau);
        ExitOnFailure1(hr, "Failed to get user information for fileshare: '%ls'", pss->wzShareName);

        hr = WcaGetRecordInteger(hRec, ssupqPermissions, &pExUserPerms->nPermissions);
        ExitOnFailure(hr, "Failed to get FileSharePermissions.Permissions");
        pExUserPerms->accessMode = SET_ACCESS;  // we only support SET_ACCESS here

        pExUserPermsList = AddExUserPermsSmbToList(pExUserPermsList, pExUserPerms);
        ++nCounter;
        pExUserPerms = NULL; // set the smb NULL so it doesn't accidentally get freed below
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
        pss->pExUserPerms = pExUserPermsList;
        pss->nUserPermissionCount = nCounter;
    }
    ExitOnFailure(hr, "Failure occured while processing FileShare table");

LExit:
    // if anything was left over after an error clean it all up
    if (pExUserPerms)
    {
        ScaExUserPermsSmbFreeList(pExUserPerms);
    }

    ReleaseStr(pwzData);

    return hr;
}
