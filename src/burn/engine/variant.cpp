//-------------------------------------------------------------------------------------------------
// <copyright file="variant.cpp" company="Outercurve Foundation">
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

#include "precomp.h"


// function definitions

extern "C" void BVariantUninitialize(
    __in BURN_VARIANT* pVariant
    )
{
    if (BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        ReleaseStr(pVariant->sczValue);
    }
    memset(pVariant, 0, sizeof(BURN_VARIANT));
}

extern "C" HRESULT BVariantGetNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        *pllValue = pVariant->llValue;
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = StrStringToInt64(pVariant->sczValue, 0, pllValue);
        if (FAILED(hr))
        {
            hr = DISP_E_TYPEMISMATCH;
        }
        break;
    case BURN_VARIANT_TYPE_VERSION:
        *pllValue = (LONGLONG)pVariant->qwValue;
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

extern "C" HRESULT BVariantGetString(
    __in BURN_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = StrAllocFormatted(psczValue, L"%I64d", pVariant->llValue);
        ExitOnFailure(hr, "Failed to convert int64 to string.");
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = StrAllocString(psczValue, pVariant->sczValue, 0);
        ExitOnFailure(hr, "Failed to copy value.");
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = StrAllocFormatted(psczValue, L"%hu.%hu.%hu.%hu",
            (WORD)(pVariant->qwValue >> 48),
            (WORD)(pVariant->qwValue >> 32),
            (WORD)(pVariant->qwValue >> 16),
            (WORD)pVariant->qwValue);
        ExitOnFailure(hr, "Failed to convert version to string.");
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT BVariantGetVersion(
    __in BURN_VARIANT* pVariant,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        *pqwValue = (DWORD64)pVariant->llValue;
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = FileVersionFromStringEx(pVariant->sczValue, 0, pqwValue);
        if (FAILED(hr))
        {
            hr = DISP_E_TYPEMISMATCH;
        }
        break;
    case BURN_VARIANT_TYPE_VERSION:
        *pqwValue = pVariant->qwValue;
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

extern "C" HRESULT BVariantSetNumeric(
    __in BURN_VARIANT* pVariant,
    __in LONGLONG llValue
    )
{
    HRESULT hr = S_OK;

    if (BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        ReleaseStr(pVariant->sczValue);
    }
    memset(pVariant, 0, sizeof(BURN_VARIANT));
    pVariant->llValue = llValue;
    pVariant->Type = BURN_VARIANT_TYPE_NUMERIC;

    return hr;
}

extern "C" HRESULT BVariantSetString(
    __in BURN_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    )
{
    HRESULT hr = S_OK;

    if (!wzValue) // if we're nulling out the string, make the variable NONE.
    {
        BVariantUninitialize(pVariant);
    }
    else // assign the value.
    {
        if (BURN_VARIANT_TYPE_STRING != pVariant->Type)
        {
            memset(pVariant, 0, sizeof(BURN_VARIANT));
        }

        hr = StrAllocString(&pVariant->sczValue, wzValue, cchValue);
        ExitOnFailure(hr, "Failed to copy string.");
        pVariant->Type = BURN_VARIANT_TYPE_STRING;
    }

LExit:
    return hr;
}

extern "C" HRESULT BVariantSetVersion(
    __in BURN_VARIANT* pVariant,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;

    if (BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        ReleaseStr(pVariant->sczValue);
    }
    memset(pVariant, 0, sizeof(BURN_VARIANT));
    pVariant->qwValue = qwValue;
    pVariant->Type = BURN_VARIANT_TYPE_VERSION;

    return hr;
}

extern "C" HRESULT BVariantCopy(
    __in BURN_VARIANT* pSource,
    __out BURN_VARIANT* pTarget
    )
{
    HRESULT hr = S_OK;

    switch (pSource->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
        BVariantUninitialize(pTarget);
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantSetNumeric(pTarget, pSource->llValue);
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantSetString(pTarget, pSource->sczValue, 0);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantSetVersion(pTarget, pSource->qwValue);
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to copy variant.");

LExit:
    return hr;
}

extern "C" HRESULT BVariantChangeType(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT_TYPE type
    )
{
    HRESULT hr = S_OK;
    BURN_VARIANT variant = { };

    if (pVariant->Type == type)
    {
        ExitFunction(); // variant already is of the requested type
    }

    switch (type)
    {
    case BURN_VARIANT_TYPE_NONE:
        hr = S_OK;
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantGetNumeric(pVariant, &variant.llValue);
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantGetString(pVariant, &variant.sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantGetVersion(pVariant, &variant.qwValue);
        break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }
    ExitOnFailure(hr, "Failed to copy variant value.");
    variant.Type = type;

    BVariantUninitialize(pVariant);
    memcpy_s(pVariant, sizeof(BURN_VARIANT), &variant, sizeof(BURN_VARIANT));

LExit:
    return hr;
}
