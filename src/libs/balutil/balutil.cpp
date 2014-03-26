//-------------------------------------------------------------------------------------------------
// <copyright file="balutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Bootstrapper Application Layer utility library.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

LPCWSTR BAL_MANIFEST_FILENAME = L"BootstrapperApplicationData.xml";
const DWORD VARIABLE_GROW_FACTOR = 80;
static IBootstrapperEngine* vpEngine = NULL;

// prototypes

DAPI_(void) BalInitialize(
    __in IBootstrapperEngine* pEngine
    )
{
    pEngine->AddRef();

    ReleaseObject(vpEngine);
    vpEngine = pEngine;
}


DAPI_(void) BalUninitialize()
{
    ReleaseNullObject(vpEngine);
}


DAPI_(HRESULT) BalManifestLoad(
    __in HMODULE hBootstrapperApplicationModule,
    __out IXMLDOMDocument** ppixdManifest
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    hr = PathRelativeToModule(&sczPath, BAL_MANIFEST_FILENAME, hBootstrapperApplicationModule);
    ExitOnFailure1(hr, "Failed to get path to bootstrapper application manifest: %ls", BAL_MANIFEST_FILENAME);

    hr = XmlLoadDocumentFromFile(sczPath, ppixdManifest);
    ExitOnFailure2(hr, "Failed to load bootstrapper application manifest '%ls' from path: %ls", BAL_MANIFEST_FILENAME, sczPath);

LExit:
    ReleaseStr(sczPath);
    return hr;
}


// The contents of psczOut may be sensitive, should keep encrypted and SecureZeroFree.
DAPI_(HRESULT) BalFormatString(
    __in_z LPCWSTR wzFormat,
    __inout LPWSTR* psczOut
    )
{
    HRESULT hr = S_OK;
    DWORD cch = 0;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    if (*psczOut)
    {
        hr = StrMaxLength(*psczOut, reinterpret_cast<DWORD_PTR*>(&cch));
        ExitOnFailure(hr, "Failed to determine length of value.");
    }

    hr = vpEngine->FormatString(wzFormat, *psczOut, &cch);
    if (E_MOREDATA == hr)
    {
        ++cch;

        hr = StrAllocate(psczOut, cch, TRUE);
        ExitOnFailure(hr, "Failed to allocate value.");

        hr = vpEngine->FormatString(wzFormat, *psczOut, &cch);
    }

LExit:
    return hr;
}


// The contents of pllValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
DAPI_(HRESULT) BalGetNumericVariable(
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = vpEngine->GetVariableNumeric(wzVariable, pllValue);

LExit:
    return hr;
}


DAPI_(BOOL) BalStringVariableExists(
    __in_z LPCWSTR wzVariable
    )
{
    HRESULT hr = S_OK;
    DWORD cch = 0;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = vpEngine->GetVariableString(wzVariable, NULL, &cch);

LExit:
    return E_MOREDATA == hr; // string exists only if there are more than zero characters in the variable.
}


// The contents of psczValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroFree.
DAPI_(HRESULT) BalGetStringVariable(
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    DWORD cch = 0;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    if (*psczValue)
    {
        hr = StrMaxLength(*psczValue, reinterpret_cast<DWORD_PTR*>(&cch));
        ExitOnFailure(hr, "Failed to determine length of value.");
    }

    hr = vpEngine->GetVariableString(wzVariable, *psczValue, &cch);
    if (E_MOREDATA == hr)
    {
        ++cch;

        hr = StrAllocate(psczValue, cch, TRUE);
        ExitOnFailure(hr, "Failed to allocate value.");

        hr = vpEngine->GetVariableString(wzVariable, *psczValue, &cch);
    }

LExit:
    return hr;
}


DAPIV_(HRESULT) BalLog(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;
    LPSTR sczFormattedAnsi = NULL;
    LPWSTR sczMessage = NULL;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    va_start(args, szFormat);
    hr = StrAnsiAllocFormattedArgs(&sczFormattedAnsi, szFormat, args);
    va_end(args);
    ExitOnFailure(hr, "Failed to format log string.");

    hr = StrAllocStringAnsi(&sczMessage, sczFormattedAnsi, 0, CP_UTF8);
    ExitOnFailure(hr, "Failed to convert log string to Unicode.");

    hr = vpEngine->Log(level, sczMessage);

LExit:
    ReleaseStr(sczMessage);
    ReleaseStr(sczFormattedAnsi);
    return hr;
}


DAPIV_(HRESULT) BalLogError(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;
    LPSTR sczFormattedAnsi = NULL;
    LPWSTR sczMessage = NULL;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    va_start(args, szFormat);
    hr = StrAnsiAllocFormattedArgs(&sczFormattedAnsi, szFormat, args);
    va_end(args);
    ExitOnFailure(hr, "Failed to format error log string.");

    hr = StrAllocFormatted(&sczMessage, L"Error 0x%08x: %S", hrError, sczFormattedAnsi);
    ExitOnFailure(hr, "Failed to prepend error number to error log string.");

    hr = vpEngine->Log(BOOTSTRAPPER_LOG_LEVEL_ERROR, sczMessage);

LExit:
    ReleaseStr(sczMessage);
    ReleaseStr(sczFormattedAnsi);
    return hr;
}
