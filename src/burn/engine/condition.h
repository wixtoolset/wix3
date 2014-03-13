//-------------------------------------------------------------------------------------------------
// <copyright file="condition.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
