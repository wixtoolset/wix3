#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
