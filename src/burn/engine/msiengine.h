//-------------------------------------------------------------------------------------------------
// <copyright file="msiengine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: MSI Engine
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
    __in BURN_USER_EXPERIENCE* pUserExperience
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
