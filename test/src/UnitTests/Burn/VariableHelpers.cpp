// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


using namespace System;
using namespace Xunit;


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
    void VariableSetStringHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable, LPCWSTR wzValue)
    {
        HRESULT hr = S_OK;

        hr = VariableSetString(pVariables, wzVariable, wzValue, FALSE);
        TestThrowOnFailure2(hr, L"Failed to set %s to: %s", wzVariable, wzValue);
    }

    void VariableSetNumericHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable, LONGLONG llValue)
    {
        HRESULT hr = S_OK;

        hr = VariableSetNumeric(pVariables, wzVariable, llValue, FALSE);
        TestThrowOnFailure2(hr, L"Failed to set %s to: %I64d", wzVariable, llValue);
    }

    void VariableSetVersionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable, DWORD64 qwValue)
    {
        HRESULT hr = S_OK;

        hr = VariableSetVersion(pVariables, wzVariable, qwValue, FALSE);
        TestThrowOnFailure2(hr, L"Failed to set %s to: 0x%016I64x", wzVariable, qwValue);
    }

    String^ VariableGetStringHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;
        try
        {
            hr = VariableGetString(pVariables, wzVariable, &scz);
            TestThrowOnFailure1(hr, L"Failed to get: %s", wzVariable);

            return gcnew String(scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    __int64 VariableGetNumericHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable)
    {
        HRESULT hr = S_OK;
        LONGLONG llValue = 0;

        hr = VariableGetNumeric(pVariables, wzVariable, &llValue);
        TestThrowOnFailure1(hr, L"Failed to get: %s", wzVariable);

        return llValue;
    }

    unsigned __int64 VariableGetVersionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable)
    {
        HRESULT hr = S_OK;
        DWORD64 qwValue = 0;

        hr = VariableGetVersion(pVariables, wzVariable, &qwValue);
        TestThrowOnFailure1(hr, L"Failed to get: %s", wzVariable);

        return qwValue;
    }

    String^ VariableGetFormattedHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;
        try
        {
            hr = VariableGetFormatted(pVariables, wzVariable, &scz);
            TestThrowOnFailure1(hr, L"Failed to get formatted: %s", wzVariable);

            return gcnew String(scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    String^ VariableFormatStringHelper(BURN_VARIABLES* pVariables, LPCWSTR wzIn)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;
        try
        {
            hr = VariableFormatString(pVariables, wzIn, &scz, NULL);
            TestThrowOnFailure1(hr, L"Failed to format string: '%s'", wzIn);

            return gcnew String(scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    String^ VariableEscapeStringHelper(LPCWSTR wzIn)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;
        try
        {
            hr = VariableEscapeString(wzIn, &scz);
            TestThrowOnFailure1(hr, L"Failed to escape string: '%s'", wzIn);

            return gcnew String(scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    bool EvaluateConditionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzCondition)
    {
        HRESULT hr = S_OK;
        BOOL f = FALSE;

        hr = ConditionEvaluate(pVariables, wzCondition, &f);
        TestThrowOnFailure1(hr, L"Failed to evaluate condition: '%s'", wzCondition);

        return f ? true : false;
    }

    bool EvaluateFailureConditionHelper(BURN_VARIABLES* pVariables, LPCWSTR wzCondition)
    {
        HRESULT hr = S_OK;
        BOOL f = FALSE;

        hr = ConditionEvaluate(pVariables, wzCondition, &f);
        return E_INVALIDDATA == hr ? true : false;
    }

    bool VariableExistsHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable)
    {
        HRESULT hr = S_OK;
        BURN_VARIANT value = { };

        try
        {
            hr = VariableGetVariant(pVariables, wzVariable, &value);
            if (E_NOTFOUND == hr || value.Type == BURN_VARIANT_TYPE_NONE)
            {
                return false;
            }
            else
            {
                TestThrowOnFailure1(hr, L"Failed to find variable: '%s'", wzVariable);
                return true;
            }
        }
        finally
        {
            BVariantUninitialize(&value);
        }
    }

    int VariableGetTypeHelper(BURN_VARIABLES* pVariables, LPCWSTR wzVariable)
    {
        HRESULT hr = S_OK;
        BURN_VARIANT value = { };

        try
        {
            hr = VariableGetVariant(pVariables, wzVariable, &value);
            TestThrowOnFailure1(hr, L"Failed to find variable: '%s'", wzVariable);

            return (int)value.Type;
        }
        finally
        {
            BVariantUninitialize(&value);
        }
    }
}
}
}
}
}
