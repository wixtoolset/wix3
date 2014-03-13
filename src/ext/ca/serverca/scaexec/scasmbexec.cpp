//-------------------------------------------------------------------------------------------------
// <copyright file="scasmbexec.cpp" company="Outercurve Foundation">
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

#include "precomp.h"


/********************************************************************
 AllocateAcl - allocate an acl and populate it with this user and 
                 permission information user could be user or domain\user

********************************************************************/
HRESULT AllocateAcl(SCA_SMBP* pssp, PACL* ppACL)
{
    HRESULT hr = S_OK;
    EXPLICIT_ACCESSW* pEA = NULL;
    DWORD cEA = 0;
    DWORD dwCounter = 0;

    PSID psid = NULL;
    LPCWSTR wzUser = NULL;
    DWORD nPermissions = 0;
    DWORD nErrorReturn = 0;
    ACCESS_MODE accessMode = NOT_USED_ACCESS; 
    
    cEA = pssp->dwUserPermissionCount + 1;
    if (cEA >= MAXSIZE_T / sizeof(EXPLICIT_ACCESSW))
    {
        ExitOnFailure1(hr = E_OUTOFMEMORY, "Too many user permissions to allocate: %u", cEA);
    }

    pEA = static_cast<EXPLICIT_ACCESSW*>(MemAlloc(cEA * sizeof(EXPLICIT_ACCESSW), TRUE));
    ExitOnNull(pEA, hr, E_OUTOFMEMORY, "failed to allocate memory for explicit access structure");

    // figure out how big the psid is
    for (dwCounter = 0; dwCounter < pssp->dwUserPermissionCount; ++dwCounter)
    {
        wzUser = pssp->pUserPerms[dwCounter].wzUser;
        nPermissions = pssp->pUserPerms[dwCounter].nPermissions;
        accessMode = pssp->pUserPerms[dwCounter].accessMode;
        //
        // create the appropriate SID
        //

        // figure out the right user to put into the access block
        if (0 == lstrcmpW(wzUser, L"Everyone"))
        {
            hr = AclGetWellKnownSid(WinWorldSid, &psid);
        }
        else if (0 == lstrcmpW(wzUser, L"Administrators"))
        {
            hr = AclGetWellKnownSid(WinBuiltinAdministratorsSid, &psid);
        }
        else if (0 == lstrcmpW(wzUser, L"LocalSystem"))
        {
            hr = AclGetWellKnownSid(WinLocalSystemSid, &psid);
        }
        else if (0 == lstrcmpW(wzUser, L"LocalService"))
        {
            hr = AclGetWellKnownSid(WinLocalServiceSid, &psid);
        }
        else if (0 == lstrcmpW(wzUser, L"NetworkService"))
        {
            hr = AclGetWellKnownSid(WinNetworkServiceSid, &psid);
        }
        else if (0 == lstrcmpW(wzUser, L"AuthenticatedUser"))
        {
            hr = AclGetWellKnownSid(WinAuthenticatedUserSid, &psid);
        }
        else if (0 == lstrcmpW(wzUser, L"Guests"))
        {
            hr = AclGetWellKnownSid(WinBuiltinGuestsSid, &psid);
        }
        else if(0 == lstrcmpW(wzUser, L"CREATOR OWNER"))
        {
            hr = AclGetWellKnownSid(WinCreatorOwnerSid, &psid);
        }
        else
        {
            hr = AclGetAccountSid(NULL, wzUser, &psid);
        }
        ExitOnFailure1(hr, "failed to get sid for account: %ls", wzUser);

        // we now have a valid pSid, fill in the EXPLICIT_ACCESS

        /* Permissions options:   (see sca.sdh for defined sdl options)
        #define GENERIC_READ      (0x80000000L)    2147483648
        #define GENERIC_WRITE     (0x40000000L)    1073741824
        #define GENERIC_EXECUTE   (0x20000000L)    536870912
        #define GENERIC_ALL       (0x10000000L)    268435456
        */
        pEA[dwCounter].grfAccessPermissions = nPermissions;
        pEA[dwCounter].grfAccessMode = accessMode;
        pEA[dwCounter].grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
#pragma prefast(push)
#pragma prefast(disable:25029)
        ::BuildTrusteeWithSidW(&(pEA[dwCounter].Trustee), psid);
#pragma prefast(pop)
    }

    // create a new ACL that contains the ACE
    *ppACL = NULL;
#pragma prefast(push)
#pragma prefast(disable:25029)
    nErrorReturn = ::SetEntriesInAclW(dwCounter, pEA, NULL, ppACL);
#pragma prefast(pop)
    ExitOnFailure(hr = HRESULT_FROM_WIN32(nErrorReturn), "failed to allocate ACL");

LExit:
    if (psid)
    {
        AclFreeSid(psid);
    }

    ReleaseMem(pEA);

    return hr;
}



/********************************************************************
 FillShareInfo - fill the NetShareAdd data structure

********************************************************************/
void FillShareInfo(SHARE_INFO_502* psi, SCA_SMBP* pssp, PSECURITY_DESCRIPTOR pSD)
{
    psi->shi502_netname      = pssp->wzKey;
    psi->shi502_type         = STYPE_DISKTREE;
    psi->shi502_remark       = pssp->wzDescription;
    psi->shi502_permissions  = 0;  // not used
    psi->shi502_max_uses     = 0xFFFFFFFF;
    psi->shi502_current_uses = 0;
    psi->shi502_path         = pssp->wzDirectory;
    psi->shi502_passwd       = NULL;         // not file share perms
    psi->shi502_reserved     = 0;
    psi->shi502_security_descriptor = pSD;
}



/*  NET_API_STATUS return codes
NERR_Success         =    0
NERR_DuplicateShare  = 2118
NERR_BufTooSmall     = 2123
NERR_NetNameNotFound = 2310
NERR_RedirectedPath  = 2117
NERR_UnknownDevDir   = 2116
*/

/********************************************************************
 DoesShareExists -  Does a share of this name exist on this computer?

********************************************************************/
HRESULT DoesShareExist(__in LPWSTR wzShareName)
{
    HRESULT hr = S_OK;
    NET_API_STATUS s;
    SHARE_INFO_502* psi = NULL;
    s = ::NetShareGetInfo(NULL, wzShareName, 502, (BYTE**) &psi);

    switch (s)
    {
        case NERR_Success:
            hr = S_OK;
            break;
        case NERR_NetNameNotFound:
            hr = E_FILENOTFOUND;
            break;
        default:
            WcaLogError(s, "NetShareGetInfo returned an unexpected value.", NULL);
            hr = HRESULT_FROM_WIN32(s);
            break;
    }
                
    ::NetApiBufferFree(psi);

    return hr;
}



/********************************************************************
 CreateShare - create the file share on this computer

********************************************************************/
HRESULT CreateShare(SCA_SMBP* pssp)
{
    if (!pssp  || !(pssp->wzKey))
        return E_INVALIDARG;

    HRESULT hr = S_OK;
    PACL pACL = NULL;
    SHARE_INFO_502 si;
    NET_API_STATUS s;
    DWORD dwParamErr = 0;

    BOOL fShareExists = SUCCEEDED(DoesShareExist(pssp->wzKey));

    PSECURITY_DESCRIPTOR pSD = static_cast<PSECURITY_DESCRIPTOR>(MemAlloc(SECURITY_DESCRIPTOR_MIN_LENGTH, TRUE));
    ExitOnNull(pSD, hr, E_OUTOFMEMORY, "Failed to allocate memory for security descriptor");

#pragma prefast(push)
#pragma prefast(disable:25029)
    if (!::InitializeSecurityDescriptor(pSD, SECURITY_DESCRIPTOR_REVISION))
#pragma prefast(pop)
    {
        ExitOnLastError(hr, "failed to initialize security descriptor");
    }

    hr = AllocateAcl(pssp, &pACL);
    ExitOnFailure(hr, "Failed to allocate ACL for fileshare");

    if (NULL == pACL)
    {
        WcaLog(LOGMSG_VERBOSE, "Ignoring NULL DACL.");
    }
#pragma prefast(push)
#pragma prefast(disable:25028) // We only call this when pACL isn't NULL, so this call is safe according to the docs
    // add the ACL to the security descriptor. 
    else if (!::SetSecurityDescriptorDacl(pSD, TRUE, pACL, FALSE))
    {
        ExitOnLastError(hr, "Failed to set security descriptor");
    }
#pragma prefast(pop)

    // all that is left is to create the share
    FillShareInfo(&si, pssp, pSD);

    // Fail if the directory doesn't exist
    if (!DirExists(pssp->wzDirectory, NULL))
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_OBJECT_NOT_FOUND), "Can't create a file share on directory that doesn't exist: %ls.", pssp->wzDirectory);

    WcaLog(LOGMSG_VERBOSE, "Creating file share on directory \'%ls\' named \'%ls\'.", pssp->wzDirectory, pssp->wzKey);

    if (!fShareExists)
    {
        s = ::NetShareAdd(NULL, 502, (BYTE*) &si, &dwParamErr);
        WcaLog(LOGMSG_VERBOSE, "Adding a new file share.");
    }
    else
    {
        // The share exists.   Write our new permissions over the top.
        s = ::NetShareSetInfo(NULL, pssp->wzKey, 502, (BYTE*) &si, &dwParamErr);
        WcaLog(LOGMSG_VERBOSE, "Setting permissions on existing share.");
    }

    if (NERR_Success != s)
    {
        hr = E_FAIL;
        if (!fShareExists && NERR_DuplicateShare == s)
            WcaLog(LOGMSG_VERBOSE, "Duplicate error when existence check failed.");

        // error codes listed above.
        ExitOnFailure1(hr, "Failed to create/modify file share: Err: %d", s);
    }

LExit:
    if (pACL)
    {
        ::LocalFree(pACL);
    }

    ReleaseMem(pSD);

    return hr;
}


/********************************************************************
 ScaEnsureSmbExists

********************************************************************/
HRESULT ScaEnsureSmbExists(SCA_SMBP* pssp)
{
    HRESULT hr = S_OK;

    // create the share
    hr = CreateShare(pssp);

    return hr;
}


//
//     Delete File Shares - real work
//

/********************************************************************
 ScaDropSmb - delete this file share from this computer

********************************************************************/
HRESULT ScaDropSmb(SCA_SMBP* pssp)
{
    HRESULT hr = S_OK;
    NET_API_STATUS s;

    hr = DoesShareExist(pssp->wzKey);

    if (E_FILENOTFOUND == hr)
    {
        WcaLog(LOGMSG_VERBOSE, "Share doesn't exist, share removal skipped. (%ls)", pssp->wzKey);
        ExitFunction1(hr = S_OK);

    }

    ExitOnFailure1(hr, "Unable to detect share. (%ls)", pssp->wzKey);

    s = ::NetShareDel(NULL, pssp->wzKey, 0);
    if (NERR_Success != s)
    {
        hr = E_FAIL;
        ExitOnFailure1(hr, "Failed to remove file share: Err: %d", s);
    }

LExit:
    return hr;
}
