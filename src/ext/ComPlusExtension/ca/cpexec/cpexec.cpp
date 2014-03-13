//-------------------------------------------------------------------------------------------------
// <copyright file="cpexec.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Windows Installer XML COM+ Execution CustomAction.
// </summary>
//-------------------------------------------------------------------------------------------------
#include "precomp.h"

/********************************************************************
 DllMain - standard entry point for all WiX CustomActions

********************************************************************/
extern "C" BOOL WINAPI DllMain(
    IN HINSTANCE hInst,
    IN ULONG ulReason,
    IN LPVOID)
{
    switch(ulReason)
    {
    case DLL_PROCESS_ATTACH:
        WcaGlobalInitialize(hInst);
        break;

    case DLL_PROCESS_DETACH:
        WcaGlobalFinalize();
        break;
    }

    return TRUE;
}

/********************************************************************
 ComPlusPrepare - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - ComPlusPrepare
********************************************************************/
extern "C" UINT __stdcall ComPlusPrepare(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;

    HANDLE hRollbackFile = INVALID_HANDLE_VALUE;

    // initialize
    hr = WcaInitialize(hInstall, "ComPlusPrepare");
    ExitOnFailure(hr, "Failed to initialize ComPlusPrepare");

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // create rollback file
    hRollbackFile = ::CreateFileW(pwzData, GENERIC_WRITE, 0, NULL, CREATE_NEW, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (INVALID_HANDLE_VALUE == hRollbackFile)
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to create rollback file, name: %S", pwzData);

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);

    if (INVALID_HANDLE_VALUE != hRollbackFile)
        ::CloseHandle(hRollbackFile);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 ComPlusCleanup - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - ComPlusCleanup
********************************************************************/
extern "C" UINT __stdcall ComPlusCleanup(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ComPlusCleanup");
    ExitOnFailure(hr, "Failed to initialize ComPlusCleanup");

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // delete rollback file
    if (!::DeleteFileW(pwzData))
    {
        // error, but not a showstopper
        hr = HRESULT_FROM_WIN32(::GetLastError());
        WcaLog(LOGMSG_STANDARD, "Failed to delete rollback file, hr: 0x%x, name: %S", hr, pwzData);
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 ComPlusInstallExecute - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - ComPlusInstallExecute
********************************************************************/
extern "C" UINT __stdcall ComPlusInstallExecute(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzRollbackFileName = NULL;

    HANDLE hRollbackFile = INVALID_HANDLE_VALUE;

    BOOL fInitializedCom = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "ComPlusInstallExecute");
    ExitOnFailure(hr, "Failed to initialize ComPlusInstallExecute");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM");
    fInitializedCom = TRUE;

    CpiInitialize();

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // open rollback file
    hr = WcaReadStringFromCaData(&pwzData, &pwzRollbackFileName);
    ExitOnFailure(hr, "Failed to read rollback file name");

    hRollbackFile = ::CreateFileW(pwzRollbackFileName, GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (INVALID_HANDLE_VALUE == hRollbackFile)
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to open rollback file, name: %S", pwzRollbackFileName);

    // create partitions
    hr = CpiConfigurePartitions(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create partitions");
    if (S_FALSE == hr) ExitFunction();

    // create users in partition roles
    hr = CpiConfigureUsersInPartitionRoles(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create users in partition roles");
    if (S_FALSE == hr) ExitFunction();

    // create partition users
    hr = CpiConfigurePartitionUsers(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to add partition users");
    if (S_FALSE == hr) ExitFunction();

    // create applications
    hr = CpiConfigureApplications(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create applications");
    if (S_FALSE == hr) ExitFunction();

    // create application roles
    hr = CpiConfigureApplicationRoles(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create application roles");
    if (S_FALSE == hr) ExitFunction();

    // create users in application roles
    hr = CpiConfigureUsersInApplicationRoles(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create users in application roles");
    if (S_FALSE == hr) ExitFunction();

    // register assemblies
    hr = CpiConfigureAssemblies(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to register assemblies");
    if (S_FALSE == hr) ExitFunction();

    // create role assignments
    hr = CpiConfigureRoleAssignments(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create role assignments");
    if (S_FALSE == hr) ExitFunction();

    // create subscriptions
    hr = CpiConfigureSubscriptions(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create subscriptions");
    if (S_FALSE == hr) ExitFunction();

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzRollbackFileName);

    if (INVALID_HANDLE_VALUE != hRollbackFile)
        ::CloseHandle(hRollbackFile);

    // unitialize
    CpiFinalize();

    if (fInitializedCom)
        ::CoUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 ComPlusInstallExecuteCommit - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - ComPlusInstallExecuteCommit
********************************************************************/
extern "C" UINT __stdcall ComPlusInstallExecuteCommit(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzRollbackFileName = NULL;

    HANDLE hRollbackFile = INVALID_HANDLE_VALUE;

    BOOL fInitializedCom = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "ComPlusInstallExecuteCommit");
    ExitOnFailure(hr, "Failed to initialize ComPlusInstallExecuteCommit");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM");
    fInitializedCom = TRUE;

    CpiInitialize();

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // open rollback file
    hr = WcaReadStringFromCaData(&pwzData, &pwzRollbackFileName);
    ExitOnFailure(hr, "Failed to read rollback file name");

    hRollbackFile = ::CreateFileW(pwzRollbackFileName, GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (INVALID_HANDLE_VALUE == hRollbackFile)
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to open rollback file, name: %S", pwzRollbackFileName);

    if (INVALID_SET_FILE_POINTER == ::SetFilePointer(hRollbackFile, 0, NULL, FILE_END))
        ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to set file pointer");

    // register assemblies
    hr = CpiConfigureAssemblies(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to register assemblies");
    if (S_FALSE == hr) ExitFunction();

    // create role assignments
    hr = CpiConfigureRoleAssignments(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create role assignments");
    if (S_FALSE == hr) ExitFunction();

    // create subscriptions
    hr = CpiConfigureSubscriptions(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to create subscriptions");
    if (S_FALSE == hr) ExitFunction();

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);

    if (INVALID_HANDLE_VALUE != hRollbackFile)
        ::CloseHandle(hRollbackFile);

    // unitialize
    CpiFinalize();

    if (fInitializedCom)
        ::CoUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 ComPlusRollbackInstallExecute - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - ComPlusRollbackInstallExecute
********************************************************************/
extern "C" UINT __stdcall ComPlusRollbackInstallExecute(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzRollbackFileName = NULL;

    HANDLE hRollbackFile = INVALID_HANDLE_VALUE;

    CPI_ROLLBACK_DATA* prdPartitions = NULL;
    CPI_ROLLBACK_DATA* prdUsersInPartitionRoles = NULL;
    CPI_ROLLBACK_DATA* prdPartitionUsers = NULL;
    CPI_ROLLBACK_DATA* prdApplications = NULL;
    CPI_ROLLBACK_DATA* prdApplicationRoles = NULL;
    CPI_ROLLBACK_DATA* prdUsersApplicationRoles = NULL;
    CPI_ROLLBACK_DATA* prdAssemblies = NULL;
    CPI_ROLLBACK_DATA* prdRoleAssignments = NULL;
    CPI_ROLLBACK_DATA* prdSubscriptions = NULL;

    BOOL fInitializedCom = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "ComPlusRollbackInstallExecute");
    ExitOnFailure(hr, "Failed to initialize ComPlusRollbackInstallExecute");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM");
    fInitializedCom = TRUE;

    CpiInitialize();

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // open rollback file
    hr = WcaReadStringFromCaData(&pwzData, &pwzRollbackFileName);
    ExitOnFailure(hr, "Failed to read rollback file name");

    hRollbackFile = ::CreateFileW(pwzRollbackFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (INVALID_HANDLE_VALUE == hRollbackFile)
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to open rollback file, name: %S", pwzRollbackFileName);

    // read rollback data (execute)
    hr = CpiReadRollbackDataList(hRollbackFile, &prdPartitions);
    ExitOnFailure(hr, "Failed to read partitions rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdUsersInPartitionRoles);
    ExitOnFailure(hr, "Failed to read users in partition roles rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdPartitionUsers);
    ExitOnFailure(hr, "Failed to read partition users rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdApplications);
    ExitOnFailure(hr, "Failed to read applications rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdApplicationRoles);
    ExitOnFailure(hr, "Failed to read application roles rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdUsersApplicationRoles);
    ExitOnFailure(hr, "Failed to read users in application roles rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdAssemblies);
    ExitOnFailure(hr, "Failed to read assemblies rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdRoleAssignments);
    ExitOnFailure(hr, "Failed to read role assignments rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdSubscriptions);
    ExitOnFailure(hr, "Failed to read subscription rollback data");

    // read rollback data (commit)
    hr = CpiReadRollbackDataList(hRollbackFile, &prdAssemblies);
    ExitOnFailure(hr, "Failed to read assemblies rollback data (commit)");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdRoleAssignments);
    ExitOnFailure(hr, "Failed to read role assignments rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdSubscriptions);
    ExitOnFailure(hr, "Failed to read subscription rollback data (commit)");

    ::CloseHandle(hRollbackFile);
    hRollbackFile = INVALID_HANDLE_VALUE;

    // rollback create subscriptions
    hr = CpiRollbackConfigureSubscriptions(&pwzData, prdSubscriptions);
    ExitOnFailure(hr, "Failed to rollback create subscriptions");

    // rollback create role assignments
    hr = CpiRollbackConfigureRoleAssignments(&pwzData, prdRoleAssignments);
    ExitOnFailure(hr, "Failed to rollback create role assignments");

    // rollback register assemblies
    hr = CpiRollbackConfigureAssemblies(&pwzData, prdAssemblies);
    ExitOnFailure(hr, "Failed to rollback register assemblies");

    // rollback create users in application roles
    hr = CpiRollbackConfigureUsersInApplicationRoles(&pwzData, prdUsersApplicationRoles);
    ExitOnFailure(hr, "Failed to rollback create users in application roles");

    // rollback create application roles
    hr = CpiRollbackConfigureApplicationRoles(&pwzData, prdApplicationRoles);
    ExitOnFailure(hr, "Failed to rollback create application roles");

    // rollback create applications
    hr = CpiRollbackConfigureApplications(&pwzData, prdApplications);
    ExitOnFailure(hr, "Failed to rollback create applications");

    // rollback create partition users
    hr = CpiRollbackConfigurePartitionUsers(&pwzData, prdPartitionUsers);
    ExitOnFailure(hr, "Failed to rollback create partition users");

    // rollback create users in partition roles
    hr = CpiRollbackConfigureUsersInPartitionRoles(&pwzData, prdUsersInPartitionRoles);
    ExitOnFailure(hr, "Failed to rollback create users in partition roles");

    // rollback create partitions
    hr = CpiRollbackConfigurePartitions(&pwzData, prdPartitions);
    ExitOnFailure(hr, "Failed to rollback create partitions");

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzRollbackFileName);

    if (INVALID_HANDLE_VALUE != hRollbackFile)
        ::CloseHandle(hRollbackFile);

    if (prdPartitions)
        CpiFreeRollbackDataList(prdPartitions);
    if (prdUsersInPartitionRoles)
        CpiFreeRollbackDataList(prdUsersInPartitionRoles);
    if (prdPartitionUsers)
        CpiFreeRollbackDataList(prdPartitionUsers);
    if (prdApplications)
        CpiFreeRollbackDataList(prdApplications);
    if (prdApplicationRoles)
        CpiFreeRollbackDataList(prdApplicationRoles);
    if (prdUsersApplicationRoles)
        CpiFreeRollbackDataList(prdUsersApplicationRoles);
    if (prdAssemblies)
        CpiFreeRollbackDataList(prdAssemblies);
    if (prdRoleAssignments)
        CpiFreeRollbackDataList(prdRoleAssignments);
    if (prdSubscriptions)
        CpiFreeRollbackDataList(prdSubscriptions);

    // unitialize
    CpiFinalize();

    if (fInitializedCom)
        ::CoUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 ComPlusUninstallExecute - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - ComPlusUninstallExecute
********************************************************************/
extern "C" UINT __stdcall ComPlusUninstallExecute(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzRollbackFileName = NULL;

    HANDLE hRollbackFile = INVALID_HANDLE_VALUE;

    BOOL fInitializedCom = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "ComPlusUninstallExecute");
    ExitOnFailure(hr, "Failed to initialize ComPlusUninstallExecute");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM");
    fInitializedCom = TRUE;

    CpiInitialize();

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // open rollback file
    hr = WcaReadStringFromCaData(&pwzData, &pwzRollbackFileName);
    ExitOnFailure(hr, "Failed to read rollback file name");

    hRollbackFile = ::CreateFileW(pwzRollbackFileName, GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (INVALID_HANDLE_VALUE == hRollbackFile)
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to open rollback file, name: %S", pwzRollbackFileName);

    // delete subscriptions
    hr = CpiConfigureSubscriptions(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to delete subscriptions");
    if (S_FALSE == hr) ExitFunction();

    // delete role assignments
    hr = CpiConfigureRoleAssignments(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to delete role assignments");
    if (S_FALSE == hr) ExitFunction();

    // unregister assemblies
    hr = CpiConfigureAssemblies(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to unregister assemblies");
    if (S_FALSE == hr) ExitFunction();

    // remove users in application roles
    hr = CpiConfigureUsersInApplicationRoles(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to delete users in application roles");
    if (S_FALSE == hr) ExitFunction();

    // remove application roles
    hr = CpiConfigureApplicationRoles(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to delete application roles");
    if (S_FALSE == hr) ExitFunction();

    // remove applications
    hr = CpiConfigureApplications(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to remove applications");
    if (S_FALSE == hr) ExitFunction();

    // remove partition users
    hr = CpiConfigurePartitionUsers(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to remove partition users");
    if (S_FALSE == hr) ExitFunction();

    // remove users in partition roles
    hr = CpiConfigureUsersInPartitionRoles(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to delete users in partition roles");
    if (S_FALSE == hr) ExitFunction();

    // remove partitions
    hr = CpiConfigurePartitions(&pwzData, hRollbackFile);
    ExitOnFailure(hr, "Failed to delete partitions");
    if (S_FALSE == hr) ExitFunction();

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzRollbackFileName);

    if (INVALID_HANDLE_VALUE != hRollbackFile)
        ::CloseHandle(hRollbackFile);

    // unitialize
    CpiFinalize();

    if (fInitializedCom)
        ::CoUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 ComPlusRollbackUninstallExecute - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - ComPlusRollbackUninstallExecute
********************************************************************/
extern "C" UINT __stdcall ComPlusRollbackUninstallExecute(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzRollbackFileName = NULL;

    HANDLE hRollbackFile = INVALID_HANDLE_VALUE;

    CPI_ROLLBACK_DATA* prdPartitions = NULL;
    CPI_ROLLBACK_DATA* prdUsersInPartitionRoles = NULL;
    CPI_ROLLBACK_DATA* prdPartitionUsers = NULL;
    CPI_ROLLBACK_DATA* prdApplications = NULL;
    CPI_ROLLBACK_DATA* prdApplicationRoles = NULL;
    CPI_ROLLBACK_DATA* prdUsersApplicationRoles = NULL;
    CPI_ROLLBACK_DATA* prdAssemblies = NULL;
    CPI_ROLLBACK_DATA* prdRoleAssignments = NULL;
    CPI_ROLLBACK_DATA* prdSubscriptions = NULL;

    BOOL fInitializedCom = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "ComPlusRollbackUninstallExecute");
    ExitOnFailure(hr, "Failed to initialize ComPlusRollbackUninstallExecute");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM");
    fInitializedCom = TRUE;

    CpiInitialize();

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // open rollback file
    hr = WcaReadStringFromCaData(&pwzData, &pwzRollbackFileName);
    ExitOnFailure(hr, "Failed to read rollback file name");

    hRollbackFile = ::CreateFileW(pwzRollbackFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (INVALID_HANDLE_VALUE == hRollbackFile)
        ExitOnFailure1(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to open rollback file, name: %S", pwzRollbackFileName);

    // read rollback data
    hr = CpiReadRollbackDataList(hRollbackFile, &prdSubscriptions);
    ExitOnFailure(hr, "Failed to read subscription rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdRoleAssignments);
    ExitOnFailure(hr, "Failed to read role assignments rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdAssemblies);
    ExitOnFailure(hr, "Failed to read assemblies rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdUsersApplicationRoles);
    ExitOnFailure(hr, "Failed to read users in application roles rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdApplicationRoles);
    ExitOnFailure(hr, "Failed to read application roles rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdApplications);
    ExitOnFailure(hr, "Failed to read applications rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdPartitionUsers);
    ExitOnFailure(hr, "Failed to read partition users rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdUsersInPartitionRoles);
    ExitOnFailure(hr, "Failed to read users in partition roles rollback data");
    hr = CpiReadRollbackDataList(hRollbackFile, &prdPartitions);
    ExitOnFailure(hr, "Failed to read partitions rollback data");

    ::CloseHandle(hRollbackFile);
    hRollbackFile = INVALID_HANDLE_VALUE;

    // rollback remove partitions
    hr = CpiRollbackConfigurePartitions(&pwzData, prdPartitions);
    ExitOnFailure(hr, "Failed to rollback delete partitions");

    // rollback remove users in partition roles
    hr = CpiRollbackConfigureUsersInPartitionRoles(&pwzData, prdUsersInPartitionRoles);
    ExitOnFailure(hr, "Failed to rollback delete users in partition roles");

    // rollback remove partition users
    hr = CpiRollbackConfigurePartitionUsers(&pwzData, prdPartitionUsers);
    ExitOnFailure(hr, "Failed to rollback delete partition users");

    // rollback remove applications
    hr = CpiRollbackConfigureApplications(&pwzData, prdApplications);
    ExitOnFailure(hr, "Failed to rollback delete applications");

    // rollback remove application roles
    hr = CpiRollbackConfigureApplicationRoles(&pwzData, prdApplicationRoles);
    ExitOnFailure(hr, "Failed to rollback delete application roles");

    // rollback remove users in application roles
    hr = CpiRollbackConfigureUsersInApplicationRoles(&pwzData, prdUsersApplicationRoles);
    ExitOnFailure(hr, "Failed to rollback delete users in application roles");

    // rollback unregister assemblies
    hr = CpiRollbackConfigureAssemblies(&pwzData, prdAssemblies);
    ExitOnFailure(hr, "Failed to rollback unregister assemblies");

    // rollback delete role assignments
    hr = CpiRollbackConfigureRoleAssignments(&pwzData, prdAssemblies);
    ExitOnFailure(hr, "Failed to rollback delete role assignments");

    // rollback delete subscriptions
    hr = CpiRollbackConfigureSubscriptions(&pwzData, prdSubscriptions);
    ExitOnFailure(hr, "Failed to rollback delete subscriptions");

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzRollbackFileName);

    if (INVALID_HANDLE_VALUE != hRollbackFile)
        ::CloseHandle(hRollbackFile);

    if (prdPartitions)
        CpiFreeRollbackDataList(prdPartitions);
    if (prdUsersInPartitionRoles)
        CpiFreeRollbackDataList(prdUsersInPartitionRoles);
    if (prdPartitionUsers)
        CpiFreeRollbackDataList(prdPartitionUsers);
    if (prdApplications)
        CpiFreeRollbackDataList(prdApplications);
    if (prdApplicationRoles)
        CpiFreeRollbackDataList(prdApplicationRoles);
    if (prdUsersApplicationRoles)
        CpiFreeRollbackDataList(prdUsersApplicationRoles);
    if (prdAssemblies)
        CpiFreeRollbackDataList(prdAssemblies);
    if (prdRoleAssignments)
        CpiFreeRollbackDataList(prdRoleAssignments);
    if (prdSubscriptions)
        CpiFreeRollbackDataList(prdSubscriptions);

    // unitialize
    CpiFinalize();

    if (fInitializedCom)
        ::CoUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
