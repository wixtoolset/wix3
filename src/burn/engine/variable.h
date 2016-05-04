#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

const LPCWSTR VARIABLE_DATE = L"Date";
const LPCWSTR VARIABLE_LOGONUSER = L"LogonUser";
const LPCWSTR VARIABLE_INSTALLERNAME = L"InstallerName";
const LPCWSTR VARIABLE_INSTALLERVERSION = L"InstallerVersion";


// typedefs

typedef HRESULT (*PFN_INITIALIZEVARIABLE)(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );


// constants

enum BURN_VARIABLE_INTERNAL_TYPE
{
    BURN_VARIABLE_INTERNAL_TYPE_NORMAL, // the BA can set this variable.
    BURN_VARIABLE_INTERNAL_TYPE_OVERRIDABLE_BUILTIN, // the BA can't set this variable, but the unelevated process can serialize it to the elevated process.
    BURN_VARIABLE_INTERNAL_TYPE_BUILTIN, // the BA can't set this variable, and the unelevated process can't serialize it to the elevated process.
};


// structs

typedef struct _BURN_VARIABLE
{
    LPWSTR sczName;
    BURN_VARIANT Value;
    BOOL fHidden;    
    BOOL fLiteral; // if fLiteral, then when formatting this variable its value should be used as is (don't continue recursively formatting).
    BOOL fPersisted;

    // used for late initialization of built-in variables
    BURN_VARIABLE_INTERNAL_TYPE internalType;
    PFN_INITIALIZEVARIABLE pfnInitialize;
    DWORD_PTR dwpInitializeData;
} BURN_VARIABLE;

typedef struct _BURN_VARIABLES
{
    CRITICAL_SECTION csAccess;
    DWORD dwMaxVariables;
    DWORD cVariables;
    BURN_VARIABLE* rgVariables;
} BURN_VARIABLES;


// function declarations

HRESULT VariableInitialize(
    __in BURN_VARIABLES* pVariables
    );
HRESULT VariablesParseFromXml(
    __in BURN_VARIABLES* pVariables,
    __in IXMLDOMNode* pixnBundle
    );
void VariablesUninitialize(
    __in BURN_VARIABLES* pVariables
    );
void VariablesDump(
    __in BURN_VARIABLES* pVariables
    );
HRESULT VariableGetNumeric(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );
HRESULT VariableGetString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
HRESULT VariableGetVersion(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    );
HRESULT VariableGetVariant(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in BURN_VARIANT* pValue
    );
HRESULT VariableGetFormatted(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
HRESULT VariableSetNumeric(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue,
    __in BOOL fOverwriteBuiltIn
    );
HRESULT VariableSetLiteralString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fOverwriteBuiltIn
    );
HRESULT VariableSetString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fOverwriteBuiltIn
    );
HRESULT VariableSetVersion(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue,
    __in BOOL fOverwriteBuiltIn
    );
HRESULT VariableSetLiteralVariant(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in BURN_VARIANT* pVariant
    );
HRESULT VariableFormatString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );
HRESULT VariableFormatStringObfuscated(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );
HRESULT VariableEscapeString(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    );
HRESULT VariableSerialize(
    __in BURN_VARIABLES* pVariables,
    __in BOOL fPersisting,
    __inout BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    );
HRESULT VariableDeserialize(
    __in BURN_VARIABLES* pVariables,
    __in BOOL fWasPersisted,
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer
    );
HRESULT VariableStrAlloc(
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in DWORD_PTR cch
    );
HRESULT VariableStrAllocString(
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_z(cchSource + 1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    );
HRESULT VariableStrAllocConcat(
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    );
HRESULT __cdecl VariableStrAllocFormatted(
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    );
HRESULT VariableIsHidden(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out BOOL* pfHidden
    );

#if defined(__cplusplus)
}
#endif
