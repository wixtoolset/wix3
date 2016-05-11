#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


typedef struct _BURN_CONDITION
{
    // The is an expression a condition string to fire the built-in "need newer OS" message
    LPWSTR sczConditionString;
} BURN_CONDITION;


// function declarations

HRESULT ConditionEvaluate(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    );
HRESULT ConditionGlobalCheck(
    __in BURN_VARIABLES* pVariables,
    __in BURN_CONDITION* pBlock,
    __in BOOTSTRAPPER_DISPLAY display,
    __in_z LPCWSTR wzBundleName,
    __out DWORD *pdwExitCode,
    __out BOOL *pfContinueExecution
    );
HRESULT ConditionGlobalParseFromXml(
    __in BURN_CONDITION* pBlock,
    __in IXMLDOMNode* pixnBundle
    );

#if defined(__cplusplus)
}
#endif
