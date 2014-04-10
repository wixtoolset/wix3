//-------------------------------------------------------------------------------------------------
// <copyright file="variant.h" company="Outercurve Foundation">
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


// constants

enum BURN_VARIANT_TYPE
{
    BURN_VARIANT_TYPE_NONE,
    BURN_VARIANT_TYPE_NUMERIC,
    BURN_VARIANT_TYPE_STRING,
    BURN_VARIANT_TYPE_VERSION,
};


// struct

typedef struct _BURN_VARIANT
{
    union
    {
        LONGLONG llValue;
        DWORD64 qwValue;
        LPWSTR sczValue;
    };
    BURN_VARIANT_TYPE Type;
    BOOL fEncryptValue;
} BURN_VARIANT;


// function declarations

void BVariantUninitialize(
    __in BURN_VARIANT* pVariant
    );
HRESULT BVariantGetNumeric(
    __in BURN_VARIANT* pVariant,
    __out LONGLONG* pllValue
    );
HRESULT BVariantGetString(
    __in BURN_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    );
HRESULT BVariantGetVersion(
    __in BURN_VARIANT* pVariant,
    __out DWORD64* pqwValue
    );
HRESULT BVariantSetNumeric(
    __in BURN_VARIANT* pVariant,
    __in LONGLONG llValue
    );
HRESULT BVariantSetString(
    __in BURN_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    );
HRESULT BVariantSetVersion(
    __in BURN_VARIANT* pVariant,
    __in DWORD64 qwValue
    );
HRESULT BVariantCopy(
    __in BURN_VARIANT* pSource,
    __out BURN_VARIANT* pTarget
    );
HRESULT BVariantChangeType(
    __in BURN_VARIANT* pVariant,
    __in BURN_VARIANT_TYPE type
    );
HRESULT BVariantSetEncryption(
    __in BURN_VARIANT* pVariant,
    __in BOOL fEncrypt
    );

#if defined(__cplusplus)
}
#endif
