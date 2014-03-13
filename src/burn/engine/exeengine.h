//-------------------------------------------------------------------------------------------------
// <copyright file="exeengine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: EXE Engine
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT ExeEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    );
void ExeEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
HRESULT ExeEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables
    );
HRESULT ExeEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage
    );
HRESULT ExeEnginePlanAddPackage(
    __in_opt DWORD *pdwInsertSequence,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    );
HRESULT ExeEngineExecutePackage(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericExecuteProgress,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );


#if defined(__cplusplus)
}
#endif
