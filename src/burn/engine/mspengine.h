#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants


// structures


// typedefs


// function declarations

HRESULT MspEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnBundle,
    __in BURN_PACKAGE* pPackage
    );
void MspEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
HRESULT MspEngineDetectInitialize(
    __in BURN_PACKAGES* pPackages
    );
HRESULT MspEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT MspEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out_opt BOOL* pfBARequestedCache
    );
HRESULT MspEnginePlanAddPackage(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    );
HRESULT MspEngineExecutePackage(
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
void MspEngineSlipstreamUpdateState(
    __in BURN_PACKAGE* pMspPackage,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback
    );


#if defined(__cplusplus)
}
#endif
