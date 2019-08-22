#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT MsiEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnBundle,
    __in BURN_PACKAGE* pPackage
    );
HRESULT MsiEngineParsePropertiesFromXml(
    __in IXMLDOMNode* pixnPackage,
    __out BURN_MSIPROPERTY** prgProperties,
    __out DWORD* pcProperties
    );
void MsiEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
HRESULT MsiEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT MsiEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out_opt BOOL* pfBARequestedCache
    );
HRESULT MsiEnginePlanAddPackage(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    );
HRESULT MsiEngineAddCompatiblePackage(
    __in BURN_PACKAGES* pPackages,
    __in const BURN_PACKAGE* pPackage,
    __out_opt BURN_PACKAGE** ppCompatiblePackage
    );
HRESULT MsiEngineExecutePackage(
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
HRESULT MsiEngineConcatProperties(
    __in_ecount(cProperties) BURN_MSIPROPERTY* rgProperties,
    __in DWORD cProperties,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __deref_out_z LPWSTR* psczProperties,
    __in BOOL fObfuscateHiddenVariables
    );
INSTALLUILEVEL MsiEngineCalculateInstallUiLevel(
    __in BOOL fDisplayInternalUI,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_ACTION_STATE actionState
    );

#if defined(__cplusplus)
}
#endif
