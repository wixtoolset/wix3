//-------------------------------------------------------------------------------------------------
// <copyright file="mqsched.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Windows Installer XML MSMQ Scheduling CustomAction.
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
 MessageQueuingInstall - CUSTOM ACTION ENTRY POINT for installing MSMQ message queues

********************************************************************/
extern "C" UINT __stdcall MessageQueuingInstall(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    MQI_MESSAGE_QUEUE_LIST lstMessageQueues;
    MQI_MESSAGE_QUEUE_PERMISSION_LIST lstMessageQueuePermissions;

    int iCost = 0;
    LPWSTR pwzRollbackActionData = NULL;
    LPWSTR pwzExecuteActionData = NULL;

    ::ZeroMemory(&lstMessageQueues, sizeof(lstMessageQueues));
    ::ZeroMemory(&lstMessageQueuePermissions, sizeof(lstMessageQueuePermissions));

    // initialize
    hr = WcaInitialize(hInstall, "MessageQueuingInstall");
    ExitOnFailure(hr, "Failed to initialize");

    do
    {
        hr = MqiInitialize();
        if (S_FALSE == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to load mqrt.dll.");
            er = WcaErrorMessage(msierrMsmqCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT:
                ExitFunction1(hr = E_FAIL);   // bail with error
            case IDRETRY:
                break; // retry
            case IDIGNORE: __fallthrough;
            default:
                ExitFunction1(hr = S_OK);  // pretend everything is okay and bail
            }
        }
        ExitOnFailure(hr, "Failed to initialize MSMQ.");
    } while (S_FALSE == hr);

    // read message queues
    hr = MqiMessageQueueRead(&lstMessageQueues);
    ExitOnFailure(hr, "Failed to read MessageQueue table");

    // read message queue permissions
    hr = MqiMessageQueuePermissionRead(&lstMessageQueues, &lstMessageQueuePermissions);
    ExitOnFailure(hr, "Failed to read message queue permissions");

    // verify message queue elementes
    hr = MqiMessageQueueVerify(&lstMessageQueues);
    ExitOnFailure(hr, "Failed to verify message queue elements.");

    if (lstMessageQueues.iInstallCount || lstMessageQueuePermissions.iInstallCount)
    {
        // schedule rollback action
        hr = MqiMessageQueuePermissionInstall(&lstMessageQueuePermissions, &pwzRollbackActionData);
        ExitOnFailure(hr, "Failed to add message queue permissions to rollback action data");

        hr = MqiMessageQueueInstall(&lstMessageQueues, TRUE, &pwzRollbackActionData);
        ExitOnFailure(hr, "Failed to add message queues to rollback action data");

        hr = WcaDoDeferredAction(L"MessageQueuingRollbackInstall", pwzRollbackActionData, 0);
        ExitOnFailure(hr, "Failed to schedule MessageQueuingRollbackInstall");

        // schedule execute action
        hr = MqiMessageQueueInstall(&lstMessageQueues, FALSE, &pwzExecuteActionData);
        ExitOnFailure(hr, "Failed to add message queues to execute action data");
        iCost += lstMessageQueues.iInstallCount * COST_MESSAGE_QUEUE_CREATE;

        hr = MqiMessageQueuePermissionInstall(&lstMessageQueuePermissions, &pwzExecuteActionData);
        ExitOnFailure(hr, "Failed to add message queue permissions to execute action data");
        iCost += lstMessageQueues.iInstallCount * COST_MESSAGE_QUEUE_PERMISSION_ADD;

        hr = WcaDoDeferredAction(L"MessageQueuingExecuteInstall", pwzExecuteActionData, iCost);
        ExitOnFailure(hr, "Failed to schedule MessageQueuingExecuteInstall");
    }

    hr = S_OK;

LExit:
    // clean up
    MqiMessageQueueFreeList(&lstMessageQueues);
    MqiMessageQueuePermissionFreeList(&lstMessageQueuePermissions);

    ReleaseStr(pwzRollbackActionData);
    ReleaseStr(pwzExecuteActionData);

    // uninitialize
    MqiUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
 MessageQueuingUninstall - CUSTOM ACTION ENTRY POINT for uninstalling MSMQ message queues

********************************************************************/
extern "C" UINT __stdcall MessageQueuingUninstall(MSIHANDLE hInstall)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    MQI_MESSAGE_QUEUE_LIST lstMessageQueues;
    MQI_MESSAGE_QUEUE_PERMISSION_LIST lstMessageQueuePermissions;

    int iCost = 0;
    LPWSTR pwzRollbackActionData = NULL;
    LPWSTR pwzExecuteActionData = NULL;

    ::ZeroMemory(&lstMessageQueues, sizeof(lstMessageQueues));
    ::ZeroMemory(&lstMessageQueuePermissions, sizeof(lstMessageQueuePermissions));

    // initialize
    hr = WcaInitialize(hInstall, "MessageQueuingUninstall");
    ExitOnFailure(hr, "Failed to initialize");

    do
    {
        hr = MqiInitialize();
        if (S_FALSE == hr)
        {
            WcaLog(LOGMSG_STANDARD, "Failed to load mqrt.dll.");
            er = WcaErrorMessage(msierrMsmqCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT:
                ExitFunction1(hr = E_FAIL);   // bail with error
            case IDRETRY:
                break; // retry
            case IDIGNORE: __fallthrough;
            default:
                ExitFunction1(hr = S_OK);  // pretend everything is okay and bail
            }
        }
        ExitOnFailure(hr, "Failed to initialize MSMQ.");
    } while (S_FALSE == hr);

    // read message queues
    hr = MqiMessageQueueRead(&lstMessageQueues);
    ExitOnFailure(hr, "Failed to read MessageQueue table");

    // read message queue permissions
    hr = MqiMessageQueuePermissionRead(&lstMessageQueues, &lstMessageQueuePermissions);
    ExitOnFailure(hr, "Failed to read message queue permissions");

    if (lstMessageQueues.iUninstallCount || lstMessageQueuePermissions.iUninstallCount)
    {
        // schedule rollback action
        hr = MqiMessageQueueUninstall(&lstMessageQueues, TRUE, &pwzRollbackActionData);
        ExitOnFailure(hr, "Failed to add message queues to rollback action data");

        hr = MqiMessageQueuePermissionUninstall(&lstMessageQueuePermissions, &pwzRollbackActionData);
        ExitOnFailure(hr, "Failed to add message queue permissions to rollback action data");

        hr = WcaDoDeferredAction(L"MessageQueuingRollbackUninstall", pwzRollbackActionData, 0);
        ExitOnFailure(hr, "Failed to schedule MessageQueuingRollbackUninstall");

        // schedule execute action
        hr = MqiMessageQueuePermissionUninstall(&lstMessageQueuePermissions, &pwzExecuteActionData);
        ExitOnFailure(hr, "Failed to add message queue permissions to execute action data");

        hr = MqiMessageQueueUninstall(&lstMessageQueues, FALSE, &pwzExecuteActionData);
        ExitOnFailure(hr, "Failed to add message queues to execute action data");
        iCost += lstMessageQueues.iUninstallCount * COST_MESSAGE_QUEUE_DELETE;

        hr = WcaDoDeferredAction(L"MessageQueuingExecuteUninstall", pwzExecuteActionData, iCost);
        ExitOnFailure(hr, "Failed to schedule MessageQueuingExecuteUninstall");
    }

    hr = S_OK;

LExit:
    // clean up
    MqiMessageQueueFreeList(&lstMessageQueues);
    MqiMessageQueuePermissionFreeList(&lstMessageQueuePermissions);

    ReleaseStr(pwzRollbackActionData);
    ReleaseStr(pwzExecuteActionData);

    // uninitialize
    MqiUninitialize();

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
