#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
