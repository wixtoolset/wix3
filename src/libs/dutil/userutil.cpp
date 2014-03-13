//-------------------------------------------------------------------------------------------------
// <copyright file="userutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    User helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static BOOL CheckIsMemberHelper(
    __in_z LPCWSTR pwzGroupUserDomain,
    __in_ecount(cguiGroupData) const GROUP_USERS_INFO_0 *pguiGroupData,
    __in DWORD cguiGroupData
    );

/*******************************************************************
 UserBuildDomainUserName - builds a DOMAIN\USERNAME string

********************************************************************/
extern "C" HRESULT DAPI UserBuildDomainUserName(
    __out_ecount_z(cchDest) LPWSTR wzDest,
    __in int cchDest,
    __in_z LPCWSTR pwzName,
    __in_z LPCWSTR pwzDomain
    )
{
    HRESULT hr = S_OK;
    DWORD cchLeft = cchDest;
    WCHAR* pwz = wzDest;
    DWORD cchWz = cchDest; 
    DWORD cch;

    cch = lstrlenW(pwzDomain);
    if (cch >= cchLeft)
    {
        hr = ERROR_MORE_DATA;
        ExitOnFailure1(hr, "Buffer size is not big enough to hold domain name: %ls", pwzDomain);
    }
    else if (cch > 0)
    {
        // handle the domain case

        hr = ::StringCchCopyNW(pwz, cchWz, pwzDomain, cchLeft - 1); // last parameter does not include '\0'
        ExitOnFailure(hr, "Failed to copy Domain onto string.");

        cchLeft -= cch;
        pwz += cch;
        cchWz -= cch;

        if (1 >= cchLeft)
        {
            hr = ERROR_MORE_DATA;
            ExitOnFailure(hr, "Insufficient buffer size while building domain user name");
        }

        hr = ::StringCchCopyNW(pwz, cchWz, L"\\", cchLeft - 1); // last parameter does not include '\0'
        ExitOnFailure(hr, "Failed to copy backslash onto string.");

        --cchLeft;
        ++pwz;
        --cchWz;
    }

    cch = lstrlenW(pwzName);
    if (cch >= cchLeft)
    {
        hr = ERROR_MORE_DATA;
        ExitOnFailure1(hr, "Buffer size is not big enough to hold user name: %ls", pwzName);
    }

    hr = ::StringCchCopyNW(pwz, cchWz, pwzName, cchLeft - 1); // last parameter does not include '\0'
    ExitOnFailure(hr, "Failed to copy User name onto string.");

LExit:
    return hr;
}


/*******************************************************************
 Checks whether a user is a member of a group - outputs the result via lpfMember
********************************************************************/
extern "C" HRESULT DAPI UserCheckIsMember(
    __in_z LPCWSTR pwzName,
    __in_z LPCWSTR pwzDomain,
    __in_z LPCWSTR pwzGroupName,
    __in_z LPCWSTR pwzGroupDomain,
    __out LPBOOL lpfMember
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    DWORD dwRead = 0;
    DWORD dwTotal = 0;
    LPCWSTR wz = NULL;
    GROUP_USERS_INFO_0 *pguiGroupData = NULL;
    WCHAR wzGroupUserDomain[MAX_DARWIN_COLUMN + 1]; // GROUPDOMAIN\GROUPNAME
    WCHAR wzUserDomain[MAX_DARWIN_COLUMN + 1]; // USERDOMAIN\USERNAME
    BSTR bstrUser = NULL;
    BSTR bstrGroup = NULL;
    
    IADsGroup *pGroup = NULL;
    VARIANT_BOOL vtBoolResult = VARIANT_FALSE;

    hr = UserBuildDomainUserName(wzGroupUserDomain, countof(wzGroupUserDomain), pwzGroupName, pwzGroupDomain);
    ExitOnFailure2(hr, "Failed to build group name from group domain %ls, group name %ls", pwzGroupDomain, pwzGroupName);

    hr = UserBuildDomainUserName(wzUserDomain, countof(wzUserDomain), pwzName, pwzDomain);
    ExitOnFailure2(hr, "Failed to build group name from group domain %ls, group name %ls", pwzGroupDomain, pwzGroupName);

    if (pwzDomain && *pwzDomain)
    {
        wz = pwzDomain;
    }

    er = ::NetUserGetGroups(wz, pwzName, 0, (LPBYTE *)&pguiGroupData, MAX_PREFERRED_LENGTH, &dwRead, &dwTotal);
    // Ignore these errors, and just go to the fallback checks
    if (ERROR_BAD_NETPATH == er || ERROR_INVALID_NAME == er || NERR_UserNotFound == er)
    {
        Trace3(REPORT_VERBOSE, "failed to get groups for user %ls from domain %ls with error code 0x%x - continuing", pwzName, (wz != NULL) ? wz : L"", HRESULT_FROM_WIN32(er));
        er = ERROR_SUCCESS;
    }
    ExitOnWin32Error1(er, hr, "Failed to get list of global groups for user while checking group membership information for user: %ls", pwzName);

    if (dwRead != dwTotal)
    {
        hr = HRESULT_FROM_WIN32(ERROR_MORE_DATA);
        ExitOnRootFailure1(hr, "Failed to get entire list of groups (global) for user while checking group membership information for user: %ls", pwzName);
    }

    if (CheckIsMemberHelper(wzGroupUserDomain, pguiGroupData, dwRead))
    {
        *lpfMember = TRUE;
        ExitFunction1(hr = S_OK);
    }

    if (NULL != pguiGroupData)
    {
        ::NetApiBufferFree(pguiGroupData);
        pguiGroupData = NULL;
    }

    // If we fail with the global groups, try again with the local groups
    er = ::NetUserGetLocalGroups(NULL, wzUserDomain, 0, LG_INCLUDE_INDIRECT, (LPBYTE *)&pguiGroupData, MAX_PREFERRED_LENGTH, &dwRead, &dwTotal);
    // Ignore these errors, and just go to the fallback checks
    if (NERR_UserNotFound == er || NERR_DCNotFound == er || RPC_S_SERVER_UNAVAILABLE == er)
    {
        Trace3(REPORT_VERBOSE, "failed to get local groups for user %ls from domain %ls with error code 0x%x - continuing", pwzName, (wz != NULL) ? wz : L"", HRESULT_FROM_WIN32(er));
        er = ERROR_SUCCESS;
    }
    ExitOnWin32Error1(er, hr, "Failed to get list of groups for user while checking group membership information for user: %ls", pwzName);

    if (dwRead != dwTotal)
    {
        hr = HRESULT_FROM_WIN32(ERROR_MORE_DATA);
        ExitOnRootFailure1(hr, "Failed to get entire list of groups (local) for user while checking group membership information for user: %ls", pwzName);
    }

    if (CheckIsMemberHelper(wzGroupUserDomain, pguiGroupData, dwRead))
    {
        *lpfMember = TRUE;
        ExitFunction1(hr = S_OK);
    }

    // If the above methods failed, let's try active directory
    hr = UserCreateADsPath(pwzDomain, pwzName, &bstrUser);
    ExitOnFailure2(hr, "failed to create user ADsPath in order to check group membership for group: %ls domain: %ls", pwzName, pwzDomain);

    hr = UserCreateADsPath(pwzGroupDomain, pwzGroupName, &bstrGroup);
    ExitOnFailure2(hr, "failed to create group ADsPath in order to check group membership for group: %ls domain: %ls", pwzGroupName, pwzGroupDomain);

    if (lstrlenW(pwzGroupDomain) > 0)
    {
        hr = ::ADsGetObject(bstrGroup, IID_IADsGroup, reinterpret_cast<void**>(&pGroup));
        ExitOnFailure1(hr, "Failed to get group '%ls' from active directory.", reinterpret_cast<WCHAR*>(bstrGroup) );

        hr = pGroup->IsMember(bstrUser, &vtBoolResult);
        ExitOnFailure2(hr, "Failed to check if user %ls is a member of group '%ls' using active directory.", reinterpret_cast<WCHAR*>(bstrUser), reinterpret_cast<WCHAR*>(bstrGroup) );
    }

    if (vtBoolResult)
    {
        *lpfMember = TRUE;
        ExitFunction1(hr = S_OK);
    }

    hr = ::ADsGetObject(bstrGroup, IID_IADsGroup, reinterpret_cast<void**>(&pGroup));
    ExitOnFailure1(hr, "Failed to get group '%ls' from active directory.", reinterpret_cast<WCHAR*>(bstrGroup) );

    hr = pGroup->IsMember(bstrUser, &vtBoolResult);
    ExitOnFailure2(hr, "Failed to check if user %ls is a member of group '%ls' using active directory.", reinterpret_cast<WCHAR*>(bstrUser), reinterpret_cast<WCHAR*>(bstrGroup) );

    if (vtBoolResult)
    {
        *lpfMember = TRUE;
        ExitFunction1(hr = S_OK);
    }

LExit:
    ReleaseObject(pGroup);
    ReleaseBSTR(bstrUser);
    ReleaseBSTR(bstrGroup);

    if (NULL != pguiGroupData)
    {
        ::NetApiBufferFree(pguiGroupData);
    }

    return hr;
}


/*******************************************************************
 Takes a domain and name, and allocates a BSTR which represents
 DOMAIN\NAME's active directory path. The BSTR this function returns
 should be released manually using the ReleaseBSTR() macro.
********************************************************************/
extern "C" HRESULT DAPI UserCreateADsPath(
    __in_z LPCWSTR wzObjectDomain, 
    __in_z LPCWSTR wzObjectName,
    __out BSTR *pbstrAdsPath
    )
{
    Assert(wzObjectDomain && wzObjectName && *wzObjectName);

    HRESULT hr = S_OK;
    LPWSTR pwzAdsPath = NULL;

    hr = StrAllocString(&pwzAdsPath, L"WinNT://", 0);
    ExitOnFailure(hr, "failed to allocate AdsPath string");

    if (*wzObjectDomain)
    {
        hr = StrAllocFormatted(&pwzAdsPath, L"%s/%s", wzObjectDomain, wzObjectName);
        ExitOnFailure(hr, "failed to allocate AdsPath string");
    }
    else if (NULL != wcsstr(wzObjectName, L"\\") || NULL != wcsstr(wzObjectName, L"/"))
    {
        hr = StrAllocConcat(&pwzAdsPath, wzObjectName, 0);
        ExitOnFailure1(hr, "failed to concat objectname: %ls", wzObjectName);
    }
    else
    {
        hr = StrAllocConcat(&pwzAdsPath, L"Localhost/", 0);
        ExitOnFailure(hr, "failed to concat LocalHost/");

        hr = StrAllocConcat(&pwzAdsPath, wzObjectName, 0);
        ExitOnFailure1(hr, "failed to concat object name: %ls", wzObjectName);
    }

    *pbstrAdsPath = ::SysAllocString(pwzAdsPath);
    if (NULL == *pbstrAdsPath)
    {
        hr = E_OUTOFMEMORY;
    }

LExit:
    ReleaseStr(pwzAdsPath);

    return hr;
}


/*******************************************************************
 Helper function to check if pwzGroupUserDomain (in form of "domain\username" is
 a member of a given LOCALGROUP_USERS_INFO_0 structure. Useful to pass in the
 output from both NetUserGetGroups() and NetUserGetLocalGroups()
********************************************************************/
static BOOL CheckIsMemberHelper(
    __in_z LPCWSTR pwzGroupUserDomain,
    __in_ecount(cguiGroupData) const GROUP_USERS_INFO_0 *pguiGroupData,
    __in DWORD cguiGroupData
    )
{
    if (NULL == pguiGroupData)
    {
        return FALSE;
    }

    for (DWORD dwCounter = 0; dwCounter < cguiGroupData; ++dwCounter)
    {
        // If the user is a member of the group, set the output flag to true
        if (0 == lstrcmpiW(pwzGroupUserDomain, pguiGroupData[dwCounter].grui0_name))
        {
            return TRUE;
        }
    }

    return FALSE;
}