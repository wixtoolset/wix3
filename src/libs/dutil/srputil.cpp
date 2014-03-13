//-------------------------------------------------------------------------------------------------
// <copyright file="srputil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  System restore point helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


typedef BOOL (WINAPI *PFN_SETRESTOREPTW)(
    __in PRESTOREPOINTINFOW pRestorePtSpec,
    __out PSTATEMGRSTATUS pSMgrStatus
    );

static PFN_SETRESTOREPTW vpfnSRSetRestorePointW = NULL;
static HMODULE vhSrClientDll = NULL;


static HRESULT InitializeComSecurity();


DAPI_(HRESULT) SrpInitialize(
    __in BOOL fInitializeComSecurity
    )
{
    HRESULT hr = S_OK;

    hr = LoadSystemLibrary(L"srclient.dll", &vhSrClientDll);
    if (FAILED(hr))
    {
        ExitFunction1(hr = E_NOTIMPL);
    }

    vpfnSRSetRestorePointW = reinterpret_cast<PFN_SETRESTOREPTW>(::GetProcAddress(vhSrClientDll, "SRSetRestorePointW"));
    ExitOnNullWithLastError(vpfnSRSetRestorePointW, hr, "Failed to find set restore point proc address.");

    // If allowed, initialize COM security to enable NetworkService,
    // LocalService and System to make callbacks to the process
    // calling System Restore. This is required for any process
    // that calls SRSetRestorePoint.
    if (fInitializeComSecurity)
    {
        hr = InitializeComSecurity();
        ExitOnFailure(hr, "Failed to initialize security for COM to talk to system restore.");
    }

LExit:
    if (FAILED(hr) && vhSrClientDll)
    {
        SrpUninitialize();
    }

    return hr;
}

DAPI_(void) SrpUninitialize()
{
    if (vhSrClientDll)
    {
        ::FreeLibrary(vhSrClientDll);
        vhSrClientDll = NULL;
        vpfnSRSetRestorePointW = NULL;
    }
}

DAPI_(HRESULT) SrpCreateRestorePoint(
    __in_z LPCWSTR wzApplicationName,
    __in SRP_ACTION action
    )
{
    HRESULT hr = S_OK;
    RESTOREPOINTINFOW restorePoint = { };
    STATEMGRSTATUS status = { };

    if (!vpfnSRSetRestorePointW)
    {
        ExitFunction1(hr = E_NOTIMPL);
    }

    restorePoint.dwEventType = BEGIN_SYSTEM_CHANGE;
    restorePoint.dwRestorePtType = (SRP_ACTION_INSTALL == action) ? APPLICATION_INSTALL : (SRP_ACTION_UNINSTALL == action) ? APPLICATION_UNINSTALL : MODIFY_SETTINGS;
    ::StringCbCopyW(restorePoint.szDescription, sizeof(restorePoint.szDescription), wzApplicationName);

    if (!vpfnSRSetRestorePointW(&restorePoint, &status))
    {
        ExitOnWin32Error(status.nStatus, hr, "Failed to create system restore point.");
    }

LExit:
    return hr;
}


// internal functions.

static HRESULT InitializeComSecurity()
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    SECURITY_DESCRIPTOR sd = {0};
    EXPLICIT_ACCESS ea[5] = {0};
    ACL* pAcl = NULL;
    ULONGLONG rgSidBA[(SECURITY_MAX_SID_SIZE+sizeof(ULONGLONG)-1)/sizeof(ULONGLONG)]={0};
    ULONGLONG rgSidLS[(SECURITY_MAX_SID_SIZE+sizeof(ULONGLONG)-1)/sizeof(ULONGLONG)]={0};
    ULONGLONG rgSidNS[(SECURITY_MAX_SID_SIZE+sizeof(ULONGLONG)-1)/sizeof(ULONGLONG)]={0};
    ULONGLONG rgSidPS[(SECURITY_MAX_SID_SIZE+sizeof(ULONGLONG)-1)/sizeof(ULONGLONG)]={0};
    ULONGLONG rgSidSY[(SECURITY_MAX_SID_SIZE+sizeof(ULONGLONG)-1)/sizeof(ULONGLONG)]={0};
    DWORD cbSid = 0;

    // Create the security descriptor explicitly as follows because
    // CoInitializeSecurity() will not accept the relative security descriptors
    // returned by ConvertStringSecurityDescriptorToSecurityDescriptor().
    //
    // The result is a security descriptor that is equivalent to the following 
    // security descriptor definition language (SDDL) string:
    //
    //   O:BAG:BAD:(A;;0x1;;;LS)(A;;0x1;;;NS)(A;;0x1;;;PS)(A;;0x1;;;SY)(A;;0x1;;;BA)
    //
 
    // Initialize the security descriptor.
    if (!::InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION))
    {
        ExitWithLastError(hr, "Failed to initialize security descriptor for system restore.");
    }

    // Create an administrator group security identifier (SID).
    cbSid = sizeof(rgSidBA);
    if (!::CreateWellKnownSid(WinBuiltinAdministratorsSid, NULL, rgSidBA, &cbSid))
    {
        ExitWithLastError(hr, "Failed to create administrator SID for system restore.");
    }

    // Create a local service security identifier (SID).
    cbSid = sizeof(rgSidLS);
    if (!::CreateWellKnownSid(WinLocalServiceSid, NULL, rgSidLS, &cbSid))
    {
        ExitWithLastError(hr, "Failed to create local service SID for system restore.");
    }

    // Create a network service security identifier (SID).
    cbSid = sizeof(rgSidNS);
    if (!::CreateWellKnownSid(WinNetworkServiceSid, NULL, rgSidNS, &cbSid))
    {
        ExitWithLastError(hr, "Failed to create network service SID for system restore.");
    }

    // Create a personal account security identifier (SID).
    cbSid = sizeof(rgSidPS);
    if (!::CreateWellKnownSid(WinSelfSid, NULL, rgSidPS, &cbSid))
    {
        ExitWithLastError(hr, "Failed to create self SID for system restore.");
    }

    // Create a local service security identifier (SID).
    cbSid = sizeof(rgSidSY);
    if (!::CreateWellKnownSid(WinLocalSystemSid, NULL, rgSidSY, &cbSid))
    {
        ExitWithLastError(hr, "Failed to create local system SID for system restore.");
    }

    // Setup the access control entries (ACE) for COM. COM_RIGHTS_EXECUTE and
    // COM_RIGHTS_EXECUTE_LOCAL are the minimum access rights required.
    ea[0].grfAccessPermissions = COM_RIGHTS_EXECUTE | COM_RIGHTS_EXECUTE_LOCAL;
    ea[0].grfAccessMode = SET_ACCESS;
    ea[0].grfInheritance = NO_INHERITANCE;
    ea[0].Trustee.pMultipleTrustee = NULL;
    ea[0].Trustee.MultipleTrusteeOperation = NO_MULTIPLE_TRUSTEE;
    ea[0].Trustee.TrusteeForm = TRUSTEE_IS_SID;
    ea[0].Trustee.TrusteeType = TRUSTEE_IS_GROUP;
    ea[0].Trustee.ptstrName = (LPTSTR)rgSidBA;

    ea[1].grfAccessPermissions = COM_RIGHTS_EXECUTE | COM_RIGHTS_EXECUTE_LOCAL;
    ea[1].grfAccessMode = SET_ACCESS;
    ea[1].grfInheritance = NO_INHERITANCE;
    ea[1].Trustee.pMultipleTrustee = NULL;
    ea[1].Trustee.MultipleTrusteeOperation = NO_MULTIPLE_TRUSTEE;
    ea[1].Trustee.TrusteeForm = TRUSTEE_IS_SID;
    ea[1].Trustee.TrusteeType = TRUSTEE_IS_GROUP;
    ea[1].Trustee.ptstrName = (LPTSTR)rgSidLS;

    ea[2].grfAccessPermissions = COM_RIGHTS_EXECUTE | COM_RIGHTS_EXECUTE_LOCAL;
    ea[2].grfAccessMode = SET_ACCESS;
    ea[2].grfInheritance = NO_INHERITANCE;
    ea[2].Trustee.pMultipleTrustee = NULL;
    ea[2].Trustee.MultipleTrusteeOperation = NO_MULTIPLE_TRUSTEE;
    ea[2].Trustee.TrusteeForm = TRUSTEE_IS_SID;
    ea[2].Trustee.TrusteeType = TRUSTEE_IS_GROUP;
    ea[2].Trustee.ptstrName = (LPTSTR)rgSidNS;

    ea[3].grfAccessPermissions = COM_RIGHTS_EXECUTE | COM_RIGHTS_EXECUTE_LOCAL;
    ea[3].grfAccessMode = SET_ACCESS;
    ea[3].grfInheritance = NO_INHERITANCE;
    ea[3].Trustee.pMultipleTrustee = NULL;
    ea[3].Trustee.MultipleTrusteeOperation = NO_MULTIPLE_TRUSTEE;
    ea[3].Trustee.TrusteeForm = TRUSTEE_IS_SID;
    ea[3].Trustee.TrusteeType = TRUSTEE_IS_GROUP;
    ea[3].Trustee.ptstrName = (LPTSTR)rgSidPS;

    ea[4].grfAccessPermissions = COM_RIGHTS_EXECUTE | COM_RIGHTS_EXECUTE_LOCAL;
    ea[4].grfAccessMode = SET_ACCESS;
    ea[4].grfInheritance = NO_INHERITANCE;
    ea[4].Trustee.pMultipleTrustee = NULL;
    ea[4].Trustee.MultipleTrusteeOperation = NO_MULTIPLE_TRUSTEE;
    ea[4].Trustee.TrusteeForm = TRUSTEE_IS_SID;
    ea[4].Trustee.TrusteeType = TRUSTEE_IS_GROUP;
    ea[4].Trustee.ptstrName = (LPTSTR)rgSidSY;

    // Create an access control list (ACL) using this ACE list.
    er = ::SetEntriesInAcl(countof(ea), ea, NULL, &pAcl);
    ExitOnWin32Error(er, hr, "Failed to create ACL for system restore.");

    // Set the security descriptor owner to Administrators.
    if (!::SetSecurityDescriptorOwner(&sd, rgSidBA, FALSE))
    {
        ExitWithLastError(hr, "Failed to set administrators owner for system restore.");
    }

    // Set the security descriptor group to Administrators.
    if (!::SetSecurityDescriptorGroup(&sd, rgSidBA, FALSE))
    {
        ExitWithLastError(hr, "Failed to set administrators group access for system restore.");
    }

    // Set the discretionary access control list (DACL) to the ACL.
    if (!::SetSecurityDescriptorDacl(&sd, TRUE, pAcl, FALSE))
    {
        ExitWithLastError(hr, "Failed to set DACL for system restore.");
    }

    // Note that an explicit security descriptor is being passed in.
    hr= ::CoInitializeSecurity(&sd, -1, NULL, NULL, RPC_C_AUTHN_LEVEL_PKT_PRIVACY, RPC_C_IMP_LEVEL_IDENTIFY, NULL, EOAC_DISABLE_AAA | EOAC_NO_CUSTOM_MARSHAL, NULL);
    ExitOnFailure(hr, "Failed to initialize COM security for system restore.");

LExit:
    if (pAcl)
    {
        ::LocalFree(pAcl);
    }

    return hr;
}
