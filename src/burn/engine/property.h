//-------------------------------------------------------------------------------------------------
// <copyright file="property.h" company="Outercurve Foundation">
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


// typedefs

typedef HRESULT (*PFN_INITIALIZEPROPERTY)(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );


// structs

typedef struct _BURN_PROPERTY
{
    LPWSTR sczName;
    BURN_VARIANT Value;

    // used for late initialization of built-in properties
    BOOL fBuiltIn;
    PFN_INITIALIZEPROPERTY pfnInitialize;
    DWORD_PTR dwpInitializeData;
} BURN_PROPERTY;

typedef struct _BURN_PROPERTIES
{
    DWORD dwMaxProperties;
    DWORD cProperties;
    BURN_PROPERTY* rgProperties;
} BURN_PROPERTIES;


// function declarations

HRESULT PropertyInitializeBuiltIn(
    __in BURN_PROPERTIES* pProperties
    );
void PropertiesUninitialize(
    __in BURN_PROPERTIES* pProperties
    );
HRESULT PropertyGetNumeric(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __out LONGLONG* pllValue
    );
HRESULT PropertyGetString(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __out_z LPWSTR* psczValue
    );
HRESULT PropertyGetVersion(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __in DWORD64* pqwValue
    );
HRESULT PropertyGetVariant(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __in BURN_VARIANT* pValue
    );
HRESULT PropertyGetFormatted(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __out_z LPWSTR* psczValue
    );
HRESULT PropertySetNumeric(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __in LONGLONG llValue
    );
HRESULT PropertySetString(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __in_z_opt LPCWSTR wzValue
    );
HRESULT PropertySetVersion(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __in DWORD64 qwValue
    );
HRESULT PropertySetVariant(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzProperty,
    __in BURN_VARIANT* pVariant
    );
HRESULT PropertyFormatString(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* ppwzOut
    );
HRESULT PropertyEscapeString(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* ppwzOut
    );
HRESULT PropertySerialize(
    __in BURN_PROPERTIES* pProperties,
    __inout BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    );
HRESULT PropertySaveToFile(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzPersistPath
    );
HRESULT PropertyDeserialize(
    __in BURN_PROPERTIES* pProperties,
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer
    );
HRESULT PropertyLoadFromFile(
    __in BURN_PROPERTIES* pProperties,
    __in_z LPCWSTR wzPersistPath
    );

#if defined(__cplusplus)
}
#endif
