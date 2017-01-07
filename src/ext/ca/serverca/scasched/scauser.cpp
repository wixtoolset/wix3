// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsUserQuery = L"SELECT `User`, `Component_`, `Name`, `Domain`, `Password` FROM `User` WHERE `User`=?";
enum eUserQuery { vuqUser = 1, vuqComponent, vuqName, vuqDomain, vuqPassword };

LPCWSTR vcsGroupQuery = L"SELECT `Group`, `Component_`, `Name`, `Domain` FROM `Group` WHERE `Group`=?";
enum eGroupQuery { vgqGroup = 1, vgqComponent, vgqName, vgqDomain };

LPCWSTR vcsUserGroupQuery = L"SELECT `User_`, `Group_` FROM `UserGroup` WHERE `User_`=?";
enum eUserGroupQuery { vugqUser = 1, vugqGroup };

LPCWSTR vActionableQuery = L"SELECT `User`,`Component_`,`Name`,`Domain`,`Password`,`Attributes` FROM `User` WHERE `Component_` IS NOT NULL";
enum eActionableQuery { vaqUser = 1, vaqComponent, vaqName, vaqDomain, vaqPassword, vaqAttributes };


static HRESULT AddUserToList(
    __inout SCA_USER** ppsuList
    );

static HRESULT AddGroupToList(
    __inout SCA_GROUP** ppsgList
    );


HRESULT __stdcall ScaGetUser(
    __in LPCWSTR wzUser,
    __out SCA_USER* pscau
    )
{
    if (!wzUser || !pscau)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    PMSIHANDLE hView, hRec;

    LPWSTR pwzData = NULL;

    // clear struct and bail right away if no user key was passed to search for
    ::ZeroMemory(pscau, sizeof(*pscau));
    if (!*wzUser)
    {
        ExitFunction1(hr = S_OK);
    }

    hRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hRec, 1, wzUser);
    ExitOnFailure(hr, "Failed to look up User");

    hr = WcaOpenView(vcsUserQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on User table");
    hr = WcaExecuteView(hView, hRec);
    ExitOnFailure(hr, "Failed to execute view on User table");

    hr = WcaFetchSingleRecord(hView, &hRec);
    if (S_OK == hr)
    {
        hr = WcaGetRecordString(hRec, vuqUser, &pwzData);
        ExitOnFailure(hr, "Failed to get User.User");
        hr = ::StringCchCopyW(pscau->wzKey, countof(pscau->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to user object");

        hr = WcaGetRecordString(hRec, vuqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Component_");
        hr = ::StringCchCopyW(pscau->wzComponent, countof(pscau->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqName, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Name");
        hr = ::StringCchCopyW(pscau->wzName, countof(pscau->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Domain");
        hr = ::StringCchCopyW(pscau->wzDomain, countof(pscau->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqPassword, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Password");
        hr = ::StringCchCopyW(pscau->wzPassword, countof(pscau->wzPassword), pwzData);
        ExitOnFailure(hr, "Failed to copy password string to user object");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate User.User='%ls'", wzUser);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error or found multiple matching User rows");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}

HRESULT __stdcall ScaGetUserDeferred(
    __in LPCWSTR wzUser,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __out SCA_USER* pscau
    )
{
    if (!wzUser || !pscau)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    MSIHANDLE hRec, hRecTest;

    LPWSTR pwzData = NULL;

    // clear struct and bail right away if no user key was passed to search for
    ::ZeroMemory(pscau, sizeof(*pscau));
    if (!*wzUser)
    {
        ExitFunction1(hr = S_OK);
    }

    // Reset back to the first record
    WcaFetchWrappedReset(hUserQuery);

    hr = WcaFetchWrappedRecordWhereString(hUserQuery, vuqUser, wzUser, &hRec);
    if (S_OK == hr)
    {
        hr = WcaFetchWrappedRecordWhereString(hUserQuery, vuqUser, wzUser, &hRecTest);
        if (S_OK == hr)
        {
            AssertSz(FALSE, "Found multiple matching User rows");
        }

        hr = WcaGetRecordString(hRec, vuqUser, &pwzData);
        ExitOnFailure(hr, "Failed to get User.User");
        hr = ::StringCchCopyW(pscau->wzKey, countof(pscau->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Component_");
        hr = ::StringCchCopyW(pscau->wzComponent, countof(pscau->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqName, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Name");
        hr = ::StringCchCopyW(pscau->wzName, countof(pscau->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Domain");
        hr = ::StringCchCopyW(pscau->wzDomain, countof(pscau->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqPassword, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Password");
        hr = ::StringCchCopyW(pscau->wzPassword, countof(pscau->wzPassword), pwzData);
        ExitOnFailure(hr, "Failed to copy password string to user object (in deferred CA)");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate User.User='%ls'", wzUser);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error fetching single User row");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


HRESULT __stdcall ScaGetGroup(
    __in LPCWSTR wzGroup,
    __out SCA_GROUP* pscag
    )
{
    if (!wzGroup || !pscag)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    PMSIHANDLE hView, hRec;

    LPWSTR pwzData = NULL;

    hRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hRec, 1, wzGroup);
    ExitOnFailure(hr, "Failed to look up Group");

    hr = WcaOpenView(vcsGroupQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on Group table");
    hr = WcaExecuteView(hView, hRec);
    ExitOnFailure(hr, "Failed to execute view on Group table");

    hr = WcaFetchSingleRecord(hView, &hRec);
    if (S_OK == hr)
    {
        hr = WcaGetRecordString(hRec, vgqGroup, &pwzData);
        ExitOnFailure(hr, "Failed to get Group.Group");
        hr = ::StringCchCopyW(pscag->wzKey, countof(pscag->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy Group.Group.");

        hr = WcaGetRecordString(hRec, vgqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Group.Component_");
        hr = ::StringCchCopyW(pscag->wzComponent, countof(pscag->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy Group.Component_.");

        hr = WcaGetRecordFormattedString(hRec, vgqName, &pwzData);
        ExitOnFailure(hr, "Failed to get Group.Name");
        hr = ::StringCchCopyW(pscag->wzName, countof(pscag->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy Group.Name.");

        hr = WcaGetRecordFormattedString(hRec, vgqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get Group.Domain");
        hr = ::StringCchCopyW(pscag->wzDomain, countof(pscag->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy Group.Domain.");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate Group.Group='%ls'", wzGroup);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error or found multiple matching Group rows");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


void ScaUserFreeList(
    __in SCA_USER* psuList
    )
{
    SCA_USER* psuDelete = psuList;
    while (psuList)
    {
        psuDelete = psuList;
        psuList = psuList->psuNext;

        ScaGroupFreeList(psuDelete->psgGroups);
        MemFree(psuDelete);
    }
}


void ScaGroupFreeList(
    __in SCA_GROUP* psgList
    )
{
    SCA_GROUP* psgDelete = psgList;
    while (psgList)
    {
        psgDelete = psgList;
        psgList = psgList->psgNext;

        MemFree(psgDelete);
    }
}


HRESULT ScaUserRead(
    __out SCA_USER** ppsuList
    )
{
    //Assert(FALSE);
    Assert(ppsuList);

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PMSIHANDLE hView, hRec, hUserRec, hUserGroupView;

    LPWSTR pwzData = NULL;

    BOOL fUserGroupExists  = FALSE;

    SCA_USER *psu = NULL;

    INSTALLSTATE isInstalled, isAction;

    if (S_OK != WcaTableExists(L"User"))
    {
        WcaLog(LOGMSG_VERBOSE, "User Table does not exist, exiting");
        ExitFunction1(hr = S_FALSE);
    }

    if (S_OK == WcaTableExists(L"UserGroup"))
    {
        fUserGroupExists = TRUE;
    }

    //
    // loop through all the users
    //
    hr = WcaOpenExecuteView(vActionableQuery, &hView);
    ExitOnFailure(hr, "failed to open view on User table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, vaqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get User.Component");

        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for User");

        // don't bother if we aren't installing or uninstalling this component
        if (WcaIsInstalling(isInstalled,  isAction) || WcaIsUninstalling(isInstalled, isAction))
        {
            //
            // Add the user to the list and populate it's values
            //
            hr = AddUserToList(ppsuList);
            ExitOnFailure(hr, "failed to add user to list");

            psu = *ppsuList;

            psu->isInstalled = isInstalled;
            psu->isAction = isAction;
            hr = ::StringCchCopyW(psu->wzComponent, countof(psu->wzComponent), pwzData);
            ExitOnFailure1(hr, "failed to copy component name: %ls", pwzData);

            hr = WcaGetRecordString(hRec, vaqUser, &pwzData);
            ExitOnFailure(hr, "failed to get User.User");
            hr = ::StringCchCopyW(psu->wzKey, countof(psu->wzKey), pwzData);
            ExitOnFailure1(hr, "failed to copy user key: %ls", pwzData);

            hr = WcaGetRecordFormattedString(hRec, vaqName, &pwzData);
            ExitOnFailure(hr, "failed to get User.Name");
            hr = ::StringCchCopyW(psu->wzName, countof(psu->wzName), pwzData);
            ExitOnFailure1(hr, "failed to copy user name: %ls", pwzData);

            hr = WcaGetRecordFormattedString(hRec, vaqDomain, &pwzData);
            ExitOnFailure(hr, "failed to get User.Domain");
            hr = ::StringCchCopyW(psu->wzDomain, countof(psu->wzDomain), pwzData);
            ExitOnFailure1(hr, "failed to copy user domain: %ls", pwzData);

            hr = WcaGetRecordFormattedString(hRec, vaqPassword, &pwzData);
            ExitOnFailure(hr, "failed to get User.Password");
            hr = ::StringCchCopyW(psu->wzPassword, countof(psu->wzPassword), pwzData);
            ExitOnFailure(hr, "failed to copy user password");

            hr = WcaGetRecordInteger(hRec, vaqAttributes, &psu->iAttributes);
            ExitOnFailure(hr, "failed to get User.Attributes");

            // Check if this user is to be added to any groups
            if (fUserGroupExists)
            {
                hUserRec = ::MsiCreateRecord(1);
                hr = WcaSetRecordString(hUserRec, 1, psu->wzKey);
                ExitOnFailure(hr, "Failed to create user record for querying UserGroup table");

                hr = WcaOpenView(vcsUserGroupQuery, &hUserGroupView);
                ExitOnFailure1(hr, "Failed to open view on UserGroup table for user %ls", psu->wzKey);
                hr = WcaExecuteView(hUserGroupView, hUserRec);
                ExitOnFailure1(hr, "Failed to execute view on UserGroup table for user: %ls", psu->wzKey);

                while (S_OK == (hr = WcaFetchRecord(hUserGroupView, &hRec)))
                {
                    hr = WcaGetRecordString(hRec, vugqGroup, &pwzData);
                    ExitOnFailure(hr, "failed to get UserGroup.Group");

                    hr = AddGroupToList(&(psu->psgGroups));
                    ExitOnFailure(hr, "failed to add group to list");

                    hr = ScaGetGroup(pwzData, psu->psgGroups);
                    ExitOnFailure1(hr, "failed to get information for group: %ls", pwzData);
                }

                if (E_NOMOREITEMS == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "failed to enumerate selected rows from UserGroup table");
            }
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed to enumerate selected rows from User table");

LExit:
    ReleaseStr(pwzData);

    return hr;
}


static HRESULT WriteGroupInfo(
    __in SCA_GROUP* psgList,
    __in LPWSTR *ppwzActionData
    )
{
    HRESULT hr = S_OK;

    for (SCA_GROUP* psg = psgList; psg; psg = psg->psgNext)
    {
        hr = WcaWriteStringToCaData(psg->wzName, ppwzActionData);
        ExitOnFailure1(hr, "failed to add group name to custom action data: %ls", psg->wzName);

        hr = WcaWriteStringToCaData(psg->wzDomain, ppwzActionData);
        ExitOnFailure1(hr, "failed to add group domain to custom action data: %ls", psg->wzDomain);
    }

LExit:
    return hr;
}


// Behaves like WriteGroupInfo, but it filters out groups the user is currently a member of,
// because we don't want to rollback those
static HRESULT WriteGroupRollbackInfo(
    __in LPCWSTR pwzName,
    __in LPCWSTR pwzDomain,
    __in SCA_GROUP* psgList,
    __in LPWSTR *ppwzActionData
    )
{
    HRESULT hr = S_OK;
    BOOL fIsMember = FALSE;

    for (SCA_GROUP* psg = psgList; psg; psg = psg->psgNext)
    {
        hr = UserCheckIsMember(pwzName, pwzDomain, psg->wzName, psg->wzDomain, &fIsMember);
        if (FAILED(hr))
        {
            WcaLog(LOGMSG_VERBOSE, "Failed to check if user: %ls (domain: %ls) is member of a group while collecting rollback information (error code 0x%x) - continuing", pwzName, pwzDomain, hr);
            hr = S_OK;
            continue;
        }

        // If the user is currently a member, we don't want to undo that on rollback, so skip adding
        // this group record to the list of groups to rollback
        if (fIsMember)
        {
            continue;
        }

        hr = WcaWriteStringToCaData(psg->wzName, ppwzActionData);
        ExitOnFailure1(hr, "failed to add group name to custom action data: %ls", psg->wzName);

        hr = WcaWriteStringToCaData(psg->wzDomain, ppwzActionData);
        ExitOnFailure1(hr, "failed to add group domain to custom action data: %ls", psg->wzDomain);
    }

LExit:
    return hr;
}


/* ****************************************************************
ScaUserExecute - Schedules user account creation or removal based on
component state.

******************************************************************/
HRESULT ScaUserExecute(
    __in SCA_USER *psuList
    )
{
    HRESULT hr = S_OK;
    DWORD er = 0;
    PDOMAIN_CONTROLLER_INFOW pDomainControllerInfo = NULL;

    USER_INFO_0 *pUserInfo = NULL;
    LPWSTR pwzActionData = NULL;
    LPWSTR pwzRollbackData = NULL;

    for (SCA_USER *psu = psuList; psu; psu = psu->psuNext)
    {
        USER_EXISTS ueUserExists = USER_EXISTS_INDETERMINATE;

        // Always put the User Name and Domain plus Attributes on the front of the CustomAction
        // data.  Sometimes we'll add more data.
        Assert(psu->wzName);
        hr = WcaWriteStringToCaData(psu->wzName, &pwzActionData);
        ExitOnFailure1(hr, "Failed to add user name to custom action data: %ls", psu->wzName);
        hr = WcaWriteStringToCaData(psu->wzDomain, &pwzActionData);
        ExitOnFailure1(hr, "Failed to add user domain to custom action data: %ls", psu->wzDomain);
        hr = WcaWriteIntegerToCaData(psu->iAttributes, &pwzActionData);
        ExitOnFailure1(hr, "failed to add user attributes to custom action data for user: %ls", psu->wzKey);

        // Check to see if the user already exists since we have to be very careful when adding
        // and removing users.  Note: MSDN says that it is safe to call these APIs from any
        // user, so we should be safe calling it during immediate mode.
        er = ::NetApiBufferAllocate(sizeof(USER_INFO_0), reinterpret_cast<LPVOID*>(&pUserInfo));
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure1(hr, "Failed to allocate memory to check existence of user: %ls", psu->wzName);

        LPCWSTR wzDomain = psu->wzDomain;
        if (wzDomain && *wzDomain)
        {
            er = ::DsGetDcNameW(NULL, wzDomain, NULL, NULL, NULL, &pDomainControllerInfo);
            if (RPC_S_SERVER_UNAVAILABLE == er)
            {
                // MSDN says, if we get the above error code, try again with the "DS_FORCE_REDISCOVERY" flag
                er = ::DsGetDcNameW(NULL, wzDomain, NULL, NULL, DS_FORCE_REDISCOVERY, &pDomainControllerInfo);
            }
            if (ERROR_SUCCESS == er)
            {
                wzDomain = pDomainControllerInfo->DomainControllerName + 2;  //Add 2 so that we don't get the \\ prefix
            }
        }

        er = ::NetUserGetInfo(wzDomain, psu->wzName, 0, reinterpret_cast<LPBYTE*>(pUserInfo));
        if (NERR_Success == er)
        {
            ueUserExists = USER_EXISTS_YES;
        }
        else if (NERR_UserNotFound == er)
        {
            ueUserExists = USER_EXISTS_NO;
        }
        else
        {
            ueUserExists = USER_EXISTS_INDETERMINATE;
            hr = HRESULT_FROM_WIN32(er);
            WcaLog(LOGMSG_VERBOSE, "Failed to check existence of domain: %ls, user: %ls (error code 0x%x) - continuing", wzDomain, psu->wzName, hr);
            hr = S_OK;
            er = ERROR_SUCCESS;
        }

        if (WcaIsInstalling(psu->isInstalled, psu->isAction))
        {
            // If the user exists, check to see if we are supposed to fail if user the exists before
            // the install.
            if (USER_EXISTS_YES == ueUserExists)
            {
                // Reinstalls will always fail if we don't remove the check for "fail if exists".
                if (WcaIsReInstalling(psu->isInstalled, psu->isAction))
                {
                    psu->iAttributes &= ~SCAU_FAIL_IF_EXISTS;
                }

                if ((SCAU_FAIL_IF_EXISTS & (psu->iAttributes)) && !(SCAU_UPDATE_IF_EXISTS & (psu->iAttributes)))
                {
                    hr = HRESULT_FROM_WIN32(NERR_UserExists);
                    MessageExitOnFailure1(hr, msierrUSRFailedUserCreateExists, "Failed to create user: %ls because user already exists.", psu->wzName);
                }
            }

            // Rollback only if the user already exists, we couldn't determine if the user exists, or we are going to create the user
            if ((USER_EXISTS_YES == ueUserExists) || (USER_EXISTS_INDETERMINATE == ueUserExists) || !(psu->iAttributes & SCAU_DONT_CREATE_USER))
            {
                INT iRollbackUserAttributes = psu->iAttributes;

                // If the user already exists, ensure this is accounted for in rollback
                if (USER_EXISTS_YES == ueUserExists)
                {
                    iRollbackUserAttributes |= SCAU_DONT_CREATE_USER;
                }
                else
                {
                    iRollbackUserAttributes &= ~SCAU_DONT_CREATE_USER;
                }

                hr = WcaWriteStringToCaData(psu->wzName, &pwzRollbackData);
                ExitOnFailure1(hr, "Failed to add user name to rollback custom action data: %ls", psu->wzName);
                hr = WcaWriteStringToCaData(psu->wzDomain, &pwzRollbackData);
                ExitOnFailure1(hr, "Failed to add user domain to rollback custom action data: %ls", psu->wzDomain);
                hr = WcaWriteIntegerToCaData(iRollbackUserAttributes, &pwzRollbackData);
                ExitOnFailure1(hr, "failed to add user attributes to rollback custom action data for user: %ls", psu->wzKey);

                // If the user already exists, add relevant group information to rollback data
                if (USER_EXISTS_YES == ueUserExists || USER_EXISTS_INDETERMINATE == ueUserExists)
                {
                    hr = WriteGroupRollbackInfo(psu->wzName, psu->wzDomain, psu->psgGroups, &pwzRollbackData);
                    ExitOnFailure(hr, "failed to add group information to rollback custom action data");
                }

                hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"CreateUserRollback"), pwzRollbackData, COST_USER_DELETE);
                ExitOnFailure(hr, "failed to schedule CreateUserRollback");
            }

            //
            // Schedule the creation now.
            //
            hr = WcaWriteStringToCaData(psu->wzPassword, &pwzActionData);
            ExitOnFailure1(hr, "failed to add user password to custom action data for user: %ls", psu->wzKey);

            // Add user's group information to custom action data
            hr = WriteGroupInfo(psu->psgGroups, &pwzActionData);
            ExitOnFailure(hr, "failed to add group information to custom action data");

            hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"CreateUser"), pwzActionData, COST_USER_ADD);
            ExitOnFailure(hr, "failed to schedule CreateUser");
        }
        else if (((USER_EXISTS_YES == ueUserExists) || (USER_EXISTS_INDETERMINATE == ueUserExists)) && WcaIsUninstalling(psu->isInstalled, psu->isAction) && !(psu->iAttributes & SCAU_DONT_REMOVE_ON_UNINSTALL))
        {
            // Add user's group information - this will ensure the user can be removed from any groups they were added to, if the user isn't be deleted
            hr = WriteGroupInfo(psu->psgGroups, &pwzActionData);
            ExitOnFailure(hr, "failed to add group information to custom action data");

            //
            // Schedule the removal because the user exists and we don't have any flags set
            // that say, don't remove the user on uninstall.
            //
            // Note: We can't rollback the removal of a user which is why RemoveUser is a commit
            // CustomAction.
            hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"RemoveUser"), pwzActionData, COST_USER_DELETE);
            ExitOnFailure(hr, "failed to schedule RemoveUser");
        }

        ReleaseNullStr(pwzActionData);
        ReleaseNullStr(pwzRollbackData);
        if (pUserInfo)
        {
            ::NetApiBufferFree(static_cast<LPVOID>(pUserInfo));
            pUserInfo = NULL;
        }
        if (pDomainControllerInfo)
        {
            ::NetApiBufferFree(static_cast<LPVOID>(pDomainControllerInfo));
            pDomainControllerInfo = NULL;
        }
    }

LExit:
    ReleaseStr(pwzActionData);
    ReleaseStr(pwzRollbackData);
    if (pUserInfo)
    {
        ::NetApiBufferFree(static_cast<LPVOID>(pUserInfo));
    }
    if (pDomainControllerInfo)
    {
        ::NetApiBufferFree(static_cast<LPVOID>(pDomainControllerInfo));
    }

    return hr;
}


static HRESULT AddUserToList(
    __inout SCA_USER** ppsuList
    )
{
    HRESULT hr = S_OK;
    SCA_USER* psu = static_cast<SCA_USER*>(MemAlloc(sizeof(SCA_USER), TRUE));
    ExitOnNull(psu, hr, E_OUTOFMEMORY, "failed to allocate memory for new user list element");

    psu->psuNext = *ppsuList;
    *ppsuList = psu;

LExit:
    return hr;
}


static HRESULT AddGroupToList(
    __inout SCA_GROUP** ppsgList
    )
{
    HRESULT hr = S_OK;
    SCA_GROUP* psg = static_cast<SCA_GROUP*>(MemAlloc(sizeof(SCA_GROUP), TRUE));
    ExitOnNull(psg, hr, E_OUTOFMEMORY, "failed to allocate memory for new group list element");

    psg->psgNext = *ppsgList;
    *ppsgList = psg;

LExit:
    return hr;
}
