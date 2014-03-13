//-------------------------------------------------------------------------------------------------
// <copyright file="VariableHelpers.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Variable helper functions for unit tests for Burn.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{


void VariableSetStringHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable, LPCWSTR wzValue);
void VariableSetNumericHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable, LONGLONG llValue);
void VariableSetVersionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable, DWORD64 qwValue);
System::String^ VariableGetStringHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable);
__int64 VariableGetNumericHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable);
unsigned __int64 VariableGetVersionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable);
System::String^ VariableGetFormattedHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable);
System::String^ VariableFormatStringHelper(BURN_VARIABLES* pVariables, LPCWSTR wzIn);
System::String^ VariableEscapeStringHelper(LPCWSTR wzIn);
bool EvaluateConditionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzCondition);
bool EvaluateFailureConditionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzCondition);
bool VariableExistsHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable);
int VariableGetTypeHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable);


}
}
}
}
}
