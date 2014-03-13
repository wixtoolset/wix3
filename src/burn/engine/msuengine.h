//-------------------------------------------------------------------------------------------------
// <copyright file="msuengine.h" company="Outercurve Foundation">
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

HRESULT MsuEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnMsiPackage,
    __in BURN_PACKAGE* pPackage
    );
void MsuEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
HRESULT MsuEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables
    );
HRESULT MsuEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage
    );
HRESULT MsuEnginePlanAddPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    );
HRESULT MsuEngineExecutePackage(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );


#if defined(__cplusplus)
}
#endif
