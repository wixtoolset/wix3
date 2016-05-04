#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants


// structs


// functions

void DetectReset(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PACKAGES* pPackages
    );

HRESULT DetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_REGISTRATION* pRegistration
    );

HRESULT DetectReportRelatedBundles(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOTSTRAPPER_ACTION action
    );

HRESULT DetectUpdate(
    __in_z LPCWSTR wzBundleId,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    );

#if defined(__cplusplus)
}
#endif
