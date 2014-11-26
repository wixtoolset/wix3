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

#define VARIANT_ENCRYPTION_SCOPE CRYPTPROTECTMEMORY_SAME_PROCESS

// internal function declarations

static HRESULT BVariantEncryptNumeric(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    );

static HRESULT BVariantEncryptString(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    );

static HRESULT BVariantEncryptVersion(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    );

static HRESULT BVariantRetrieveDecryptedNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    );

static HRESULT BVariantRetrieveDecryptedString(
    __in BURN_VARIANT* pVariant,
    __out LPWSTR* psczValue
    );

static HRESULT BVariantRetrieveDecryptedVersion(
    __in BURN_VARIANT* pVariant,
    __out DWORD64* pqwValue
    );

// function definitions

extern "C" void BVariantUninitialize(
    __in BURN_VARIANT* pVariant
    )
{
    if (BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        StrSecureZeroFreeString(pVariant->sczValue);
    }
    SecureZeroMemory(pVariant, sizeof(BURN_VARIANT));
}

// The contents of pllValue may be sensitive, should keep encrypted and SecureZeroMemory.
extern "C" HRESULT BVariantGetNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        BVariantRetrieveDecryptedNumeric(pVariant, pllValue);
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantRetrieveDecryptedString(pVariant, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = StrStringToInt64(sczValue, 0, pllValue);
            if (FAILED(hr))
            {
                hr = DISP_E_TYPEMISMATCH;
            }
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        BVariantRetrieveDecryptedVersion(pVariant, (DWORD64*)pllValue);
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

// The contents of psczValue may be sensitive, should keep encrypted and SecureZeroFree.
extern "C" HRESULT BVariantGetString(
    __in BURN_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    DWORD64 qwValue = 0;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantRetrieveDecryptedNumeric(pVariant, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = StrAllocFormattedSecure(psczValue, L"%I64d", llValue);
            ExitOnFailure(hr, "Failed to convert int64 to string.");
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantRetrieveDecryptedString(pVariant, psczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantRetrieveDecryptedVersion(pVariant, &qwValue);
        if (SUCCEEDED(hr))
        {
            hr = StrAllocFormattedSecure(psczValue, L"%hu.%hu.%hu.%hu",
                (WORD)(qwValue >> 48),
                (WORD)(qwValue >> 32),
                (WORD)(qwValue >> 16),
                (WORD)qwValue);
            ExitOnFailure(hr, "Failed to convert version to string.");
        }
        SecureZeroMemory(&qwValue, sizeof(qwValue));
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

LExit:
    return hr;
}

// The contents of pqwValue may be sensitive, should keep encrypted and SecureZeroMemory.
extern "C" HRESULT BVariantGetVersion(
    __in BURN_VARIANT* pVariant,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NUMERIC:
        BVariantRetrieveDecryptedNumeric(pVariant, (LONGLONG*)pqwValue);
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantRetrieveDecryptedString(pVariant, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = FileVersionFromStringEx(sczValue, 0, pqwValue);
            if (FAILED(hr))
            {
                hr = DISP_E_TYPEMISMATCH;
            }
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        BVariantRetrieveDecryptedVersion(pVariant, pqwValue);
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
    BOOL fEncryptValue = pVariant->fEncryptValue;

    if (BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        StrSecureZeroFreeString(pVariant->sczValue);
    }
    memset(pVariant, 0, sizeof(BURN_VARIANT));
    pVariant->llValue = llValue;
    pVariant->Type = BURN_VARIANT_TYPE_NUMERIC;
    BVariantSetEncryption(pVariant, fEncryptValue);

    return hr;
}

extern "C" HRESULT BVariantSetString(
    __in BURN_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncryptValue = pVariant->fEncryptValue;

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
        else
        {
            // We're about to copy an unencrypted value.
            pVariant->fEncryptValue = FALSE;
        }

        hr = StrAllocStringSecure(&pVariant->sczValue, wzValue, cchValue);
        ExitOnFailure(hr, "Failed to copy string.");

        pVariant->Type = BURN_VARIANT_TYPE_STRING;
    }

LExit:
    BVariantSetEncryption(pVariant, fEncryptValue);
    return hr;
}

extern "C" HRESULT BVariantSetVersion(
    __in BURN_VARIANT* pVariant,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncryptValue = pVariant->fEncryptValue;

    if (BURN_VARIANT_TYPE_STRING == pVariant->Type)
    {
        StrSecureZeroFreeString(pVariant->sczValue);
    }
    memset(pVariant, 0, sizeof(BURN_VARIANT));
    pVariant->qwValue = qwValue;
    pVariant->Type = BURN_VARIANT_TYPE_VERSION;
    BVariantSetEncryption(pVariant, fEncryptValue);

    return hr;
}

extern "C" HRESULT BVariantSetVariant(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    DWORD64 qwValue = 0;
    BOOL fEncrypt = pVariant->fEncryptValue;

    switch (pValue->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
        BVariantUninitialize(pVariant);
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantGetNumeric(pValue, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetNumeric(pVariant, llValue);
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantGetString(pValue, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetString(pVariant, sczValue, 0);
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantGetVersion(pValue, &qwValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetVersion(pVariant, qwValue);
        }
        SecureZeroMemory(&qwValue, sizeof(qwValue));
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to copy variant.");
    hr = BVariantSetEncryption(pVariant, fEncrypt);

LExit:
    return hr;
}

extern "C" HRESULT BVariantCopy(
    __in BURN_VARIANT* pSource,
    __out BURN_VARIANT* pTarget
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    DWORD64 qwValue = 0;

    BVariantUninitialize(pTarget);

    switch (pSource->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantGetNumeric(pSource, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetNumeric(pTarget, llValue);
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantGetString(pSource, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetString(pTarget, sczValue, 0);
        }
        StrSecureZeroFreeString(sczValue);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantGetVersion(pSource, &qwValue);
        if (SUCCEEDED(hr))
        {
            hr = BVariantSetVersion(pTarget, qwValue);
        }
        SecureZeroMemory(&qwValue, sizeof(qwValue));
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to copy variant.");
    hr = BVariantSetEncryption(pTarget, pSource->fEncryptValue);

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
    BOOL fEncryptValue = pVariant->fEncryptValue;

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
    SecureZeroMemory(&variant, sizeof(BURN_VARIANT));
    BVariantSetEncryption(pVariant, fEncryptValue);

LExit:
    return hr;
}

extern "C" HRESULT BVariantSetEncryption(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;

    if (pVariant->fEncryptValue == fEncrypt)
    {
        // The requested encryption state is already applied.
        ExitFunction();
    }
    
    switch (pVariant->Type)
    {
    case BURN_VARIANT_TYPE_NONE:
        hr = S_OK;
        break;
    case BURN_VARIANT_TYPE_NUMERIC:
        hr = BVariantEncryptNumeric(pVariant, fEncrypt);
        break;
    case BURN_VARIANT_TYPE_STRING:
        hr = BVariantEncryptString(pVariant, fEncrypt);
        break;
    case BURN_VARIANT_TYPE_VERSION:
        hr = BVariantEncryptVersion(pVariant, fEncrypt);
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to set the variant's encryption state");
    pVariant->fEncryptValue = fEncrypt;

LExit:
    return hr;
}

static HRESULT BVariantEncryptNumeric(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;

    if (fEncrypt)
    {
        hr = CrypEncryptMemory(&pVariant->llValue, sizeof(pVariant->llValue), VARIANT_ENCRYPTION_SCOPE);
    }
    else
    {
        hr = CrypDecryptMemory(&pVariant->llValue, sizeof(pVariant->llValue), VARIANT_ENCRYPTION_SCOPE);
    }

//LExit:
    return hr;
}

static HRESULT BVariantEncryptString(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbData = 0;

    if (NULL == pVariant->sczValue)
    {
        ExitFunction();
    }

    cbData = MemSize(pVariant->sczValue);
    if (-1 == cbData)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Failed to get the size of the string");
    }
    
    DWORD remainder = fEncrypt ? cbData % CRYP_ENCRYPT_MEMORY_SIZE : 0;
    DWORD extraNeeded = 0 < remainder ? CRYP_ENCRYPT_MEMORY_SIZE - remainder : 0;
    if ((MAXDWORD - extraNeeded) < cbData)
    {
        hr = E_INVALIDDATA;
        ExitOnFailure1(hr, "The string is too big: size %u", cbData);
    }
    else if (0 < extraNeeded)
    {
        cbData += extraNeeded;
        LPVOID pvNew = NULL;
        hr = MemReAllocSecure(static_cast<LPVOID>(pVariant->sczValue), cbData, TRUE, &pvNew);
        ExitOnFailure(hr, "Failed to resize the string so it could be encrypted");
        pVariant->sczValue = static_cast<LPWSTR>(pvNew);
    }
    
    if (fEncrypt)
    {
        hr = CrypEncryptMemory(pVariant->sczValue, static_cast<DWORD>(cbData), VARIANT_ENCRYPTION_SCOPE);
    }
    else
    {
        hr = CrypDecryptMemory(pVariant->sczValue, static_cast<DWORD>(cbData), VARIANT_ENCRYPTION_SCOPE);
    }

LExit:
    return hr;
}

static HRESULT BVariantEncryptVersion(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;

    if (fEncrypt)
    {
        hr = CrypEncryptMemory(&pVariant->qwValue, sizeof(pVariant->qwValue), VARIANT_ENCRYPTION_SCOPE);
    }
    else
    {
        hr = CrypDecryptMemory(&pVariant->qwValue, sizeof(pVariant->qwValue), VARIANT_ENCRYPTION_SCOPE);
    }

//LExit:
    return hr;
}

// The contents of pllValue may be sensitive, should keep encrypted and SecureZeroMemory.
static HRESULT BVariantRetrieveDecryptedNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;

    Assert(NULL != pllValue);
    if (pVariant->fEncryptValue)
    {
        hr = BVariantEncryptNumeric(pVariant, FALSE);
        ExitOnFailure(hr, "Failed to decrypt numeric");
    }

    *pllValue = pVariant->llValue;

    if (pVariant->fEncryptValue)
    {
        hr = BVariantEncryptNumeric(pVariant, TRUE);
    }

LExit:
    return hr;
}

// The contents of psczValue may be sensitive, should keep encrypted and SecureZeroFree.
static HRESULT BVariantRetrieveDecryptedString(
    __in BURN_VARIANT* pVariant,
    __out LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    if (NULL == pVariant->sczValue)
    {
        *psczValue = NULL;
        ExitFunction();
    }

    if (pVariant->fEncryptValue)
    {
        hr = BVariantEncryptString(pVariant, FALSE);
        ExitOnFailure(hr, "Failed to decrypt string");
    }

    hr = StrAllocStringSecure(psczValue, pVariant->sczValue, 0);
    ExitOnFailure(hr, "Failed to copy value.");

    if (pVariant->fEncryptValue)
    {
        hr = BVariantEncryptString(pVariant, TRUE);
    }

LExit:
    return hr;
}

// The contents of pqwValue may be sensitive, should keep encrypted and SecureZeroMemory.
static HRESULT BVariantRetrieveDecryptedVersion(
    __in BURN_VARIANT* pVariant,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;

    Assert(NULL != pqwValue);
    if (pVariant->fEncryptValue)
    {
        hr = BVariantEncryptVersion(pVariant, FALSE);
        ExitOnFailure(hr, "Failed to decrypt version");
    }

    *pqwValue = pVariant->qwValue;

    if (pVariant->fEncryptValue)
    {
        hr = BVariantEncryptVersion(pVariant, TRUE);
    }

LExit:
    return hr;
}
