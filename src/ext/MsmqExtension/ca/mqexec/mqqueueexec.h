#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="mqqueueexec.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    MSMQ functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// function declarations

HRESULT MqiInitialize();
void MqiUninitialize();
HRESULT MqiCreateMessageQueues(
    LPWSTR* ppwzData
    );
HRESULT MqiRollbackCreateMessageQueues(
    LPWSTR* ppwzData
    );
HRESULT MqiDeleteMessageQueues(
    LPWSTR* ppwzData
    );
HRESULT MqiRollbackDeleteMessageQueues(
    LPWSTR* ppwzData
    );
HRESULT MqiAddMessageQueuePermissions(
    LPWSTR* ppwzData
    );
HRESULT MqiRollbackAddMessageQueuePermissions(
    LPWSTR* ppwzData
    );
HRESULT MqiRemoveMessageQueuePermissions(
    LPWSTR* ppwzData
    );
HRESULT MqiRollbackRemoveMessageQueuePermissions(
    LPWSTR* ppwzData
    );
