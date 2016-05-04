// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
 MessageQueuingExecuteInstall - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - MessageQueuingExecuteInstall
********************************************************************/
extern "C" UINT __stdcall MessageQueuingExecuteInstall(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "MessageQueuingExecuteInstall");
    ExitOnFailure(hr, "Failed to initialize MessageQueuingExecuteInstall");

    hr = MqiInitialize();
    ExitOnFailure(hr, "Failed to initialize");

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // create message queues
    hr = MqiCreateMessageQueues(&pwzData);
    ExitOnFailure(hr, "Failed to create message queues");
    if (S_FALSE == hr) ExitFunction();

    // add message queue permissions
    hr = MqiAddMessageQueuePermissions(&pwzData);
    ExitOnFailure(hr, "Failed to add message queue permissions");
    if (S_FALSE == hr) ExitFunction();

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);

    // uninitialize
    MqiUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 MessageQueuingRollbackInstall - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - MessageQueuingRollbackInstall
********************************************************************/
extern "C" UINT __stdcall MessageQueuingRollbackInstall(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "MessageQueuingRollbackInstall");
    ExitOnFailure(hr, "Failed to initialize MessageQueuingRollbackInstall");

    hr = MqiInitialize();
    ExitOnFailure(hr, "Failed to initialize");

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // add message queue permissions
    hr = MqiRollbackAddMessageQueuePermissions(&pwzData);
    ExitOnFailure(hr, "Failed to rollback add message queue permissions");

    // create message queues
    hr = MqiRollbackCreateMessageQueues(&pwzData);
    ExitOnFailure(hr, "Failed to rollback create message queues");

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);

    // uninitialize
    MqiUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 MessageQueuingExecuteUninstall - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - MessageQueuingExecuteUninstall
********************************************************************/
extern "C" UINT __stdcall MessageQueuingExecuteUninstall(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "MessageQueuingExecuteUninstall");
    ExitOnFailure(hr, "Failed to initialize MessageQueuingExecuteUninstall");

    hr = MqiInitialize();
    ExitOnFailure(hr, "Failed to initialize");

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // remove message queue permissions
    hr = MqiRemoveMessageQueuePermissions(&pwzData);
    ExitOnFailure(hr, "Failed to remove message queue permissions");
    if (S_FALSE == hr) ExitFunction();

    // delete message queues
    hr = MqiDeleteMessageQueues(&pwzData);
    ExitOnFailure(hr, "Failed to delete message queues");
    if (S_FALSE == hr) ExitFunction();

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);

    // uninitialize
    MqiUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
 MessageQueuingRollbackUninstall - CUSTOM ACTION ENTRY POINT

  Input:  deferred CustomActionData - MessageQueuingRollbackUninstall
********************************************************************/
extern "C" UINT __stdcall MessageQueuingRollbackUninstall(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "MessageQueuingRollbackUninstall");
    ExitOnFailure(hr, "Failed to initialize MessageQueuingRollbackUninstall");

    hr = MqiInitialize();
    ExitOnFailure(hr, "Failed to initialize");

    // get custom action data
    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData");
    pwzData = pwzCustomActionData;

    // delete message queues
    hr = MqiRollbackDeleteMessageQueues(&pwzData);
    ExitOnFailure(hr, "Failed to delete message queues");

    // remove message queue permissions
    hr = MqiRollbackRemoveMessageQueuePermissions(&pwzData);
    ExitOnFailure(hr, "Failed to remove message queue permissions");

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzCustomActionData);

    // uninitialize
    MqiUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
