// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// structs

typedef const struct _BUILT_IN_VARIABLE_DECLARATION
{
    LPCWSTR wzVariable;
    PFN_INITIALIZEVARIABLE pfnInitialize;
    DWORD_PTR dwpInitializeData;
    BOOL fPersist;
    BOOL fOverridable;
} BUILT_IN_VARIABLE_DECLARATION;


// constants

const DWORD GROW_VARIABLE_ARRAY = 3;

enum OS_INFO_VARIABLE
{
    OS_INFO_VARIABLE_NONE,
    OS_INFO_VARIABLE_VersionNT,
    OS_INFO_VARIABLE_VersionNT64,
    OS_INFO_VARIABLE_ServicePackLevel,
    OS_INFO_VARIABLE_NTProductType,
    OS_INFO_VARIABLE_NTSuiteBackOffice,
    OS_INFO_VARIABLE_NTSuiteDataCenter,
    OS_INFO_VARIABLE_NTSuiteEnterprise,
    OS_INFO_VARIABLE_NTSuitePersonal,
    OS_INFO_VARIABLE_NTSuiteSmallBusiness,
    OS_INFO_VARIABLE_NTSuiteSmallBusinessRestricted,
    OS_INFO_VARIABLE_NTSuiteWebServer,
    OS_INFO_VARIABLE_CompatibilityMode,
    OS_INFO_VARIABLE_TerminalServer,
    OS_INFO_VARIABLE_ProcessorArchitecture,
};

enum SET_VARIABLE
{
    SET_VARIABLE_NOT_BUILTIN,
    SET_VARIABLE_OVERRIDE_BUILTIN,
    SET_VARIABLE_OVERRIDE_PERSISTED_BUILTINS,
    SET_VARIABLE_ANY,
};

// internal function declarations

static HRESULT FormatString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut,
    __in BOOL fObfuscateHiddenVariables
    );
static HRESULT AddBuiltInVariable(
    __in BURN_VARIABLES* pVariables,
    __in LPCWSTR wzVariable,
    __in PFN_INITIALIZEVARIABLE pfnInitialize,
    __in DWORD_PTR dwpInitializeData,
    __in BOOL fPersist,
    __in BOOL fOverridable
    );
static HRESULT GetVariable(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out BURN_VARIABLE** ppVariable
    );
static HRESULT FindVariableIndexByName(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out DWORD* piVariable
    );
static HRESULT InsertVariable(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD iPosition
    );
static HRESULT SetVariableValue(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in BURN_VARIANT* pVariant,
    __in BOOL fLiteral,
    __in SET_VARIABLE setBuiltin,
    __in BOOL fLog
    );
static HRESULT InitializeVariableVersionNT(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableOsInfo(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableSystemInfo(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableComputerName(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableVersionMsi(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableCsidlFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableWindowsVolumeFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableTempFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableSystemFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariablePrivileged(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableRebootPending(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeSystemLanguageID(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeUserUILanguageID(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeUserLanguageID(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableString(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableNumeric(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableRegistryFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariable6432Folder(
        __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableDate(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableInstallerName(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableInstallerVersion(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableVersion(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT InitializeVariableLogonUser(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    );
static HRESULT Get64bitFolderFromRegistry(
    __in int nFolder,
    __deref_out_z LPWSTR* psczPath
    );


// function definitions

extern "C" HRESULT VariableInitialize(
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    ::InitializeCriticalSection(&pVariables->csAccess);

    const BUILT_IN_VARIABLE_DECLARATION vrgBuiltInVariables[] = {
        {L"AdminToolsFolder", InitializeVariableCsidlFolder, CSIDL_ADMINTOOLS},
        {L"AppDataFolder", InitializeVariableCsidlFolder, CSIDL_APPDATA},
        {L"CommonAppDataFolder", InitializeVariableCsidlFolder, CSIDL_COMMON_APPDATA},
#if defined(_WIN64)
        {L"CommonFiles64Folder", InitializeVariableCsidlFolder, CSIDL_PROGRAM_FILES_COMMON},
        {L"CommonFilesFolder", InitializeVariableCsidlFolder, CSIDL_PROGRAM_FILES_COMMONX86},
#else
        {L"CommonFiles64Folder", InitializeVariableRegistryFolder, CSIDL_PROGRAM_FILES_COMMON},
        {L"CommonFilesFolder", InitializeVariableCsidlFolder, CSIDL_PROGRAM_FILES_COMMON},
#endif
        {L"CommonFiles6432Folder", InitializeVariable6432Folder, CSIDL_PROGRAM_FILES_COMMON},
        {L"CompatibilityMode", InitializeVariableOsInfo, OS_INFO_VARIABLE_CompatibilityMode},
        {VARIABLE_DATE, InitializeVariableDate, 0},
        {L"ComputerName", InitializeVariableComputerName, 0},
        {L"DesktopFolder", InitializeVariableCsidlFolder, CSIDL_DESKTOP},
        {L"FavoritesFolder", InitializeVariableCsidlFolder, CSIDL_FAVORITES},
        {L"FontsFolder", InitializeVariableCsidlFolder, CSIDL_FONTS},
        {VARIABLE_INSTALLERNAME, InitializeVariableInstallerName, 0},
        {VARIABLE_INSTALLERVERSION, InitializeVariableInstallerVersion, 0},
        {L"LocalAppDataFolder", InitializeVariableCsidlFolder, CSIDL_LOCAL_APPDATA},
        {VARIABLE_LOGONUSER, InitializeVariableLogonUser, 0},
        {L"MyPicturesFolder", InitializeVariableCsidlFolder, CSIDL_MYPICTURES},
        {L"NTProductType", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTProductType},
        {L"NTSuiteBackOffice", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTSuiteBackOffice},
        {L"NTSuiteDataCenter", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTSuiteDataCenter},
        {L"NTSuiteEnterprise", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTSuiteEnterprise},
        {L"NTSuitePersonal", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTSuitePersonal},
        {L"NTSuiteSmallBusiness", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTSuiteSmallBusiness},
        {L"NTSuiteSmallBusinessRestricted", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTSuiteSmallBusinessRestricted},
        {L"NTSuiteWebServer", InitializeVariableOsInfo, OS_INFO_VARIABLE_NTSuiteWebServer},
        {L"PersonalFolder", InitializeVariableCsidlFolder, CSIDL_PERSONAL},
        {L"Privileged", InitializeVariablePrivileged, 0},
        {L"ProcessorArchitecture", InitializeVariableSystemInfo, OS_INFO_VARIABLE_ProcessorArchitecture},
#if defined(_WIN64)
        {L"ProgramFiles64Folder", InitializeVariableCsidlFolder, CSIDL_PROGRAM_FILES},
        {L"ProgramFilesFolder", InitializeVariableCsidlFolder, CSIDL_PROGRAM_FILESX86},
#else
        {L"ProgramFiles64Folder", InitializeVariableRegistryFolder, CSIDL_PROGRAM_FILES},
        {L"ProgramFilesFolder", InitializeVariableCsidlFolder, CSIDL_PROGRAM_FILES},
#endif
        {L"ProgramFiles6432Folder", InitializeVariable6432Folder, CSIDL_PROGRAM_FILES},
        {L"ProgramMenuFolder", InitializeVariableCsidlFolder, CSIDL_PROGRAMS},
        {L"RebootPending", InitializeVariableRebootPending, 0},
        {L"SendToFolder", InitializeVariableCsidlFolder, CSIDL_SENDTO},
        {L"ServicePackLevel", InitializeVariableVersionNT, OS_INFO_VARIABLE_ServicePackLevel},
        {L"StartMenuFolder", InitializeVariableCsidlFolder, CSIDL_STARTMENU},
        {L"StartupFolder", InitializeVariableCsidlFolder, CSIDL_STARTUP},
        {L"SystemFolder", InitializeVariableSystemFolder, FALSE},
        {L"System64Folder", InitializeVariableSystemFolder, TRUE},
        {L"SystemLanguageID", InitializeSystemLanguageID, 0},
        {L"TempFolder", InitializeVariableTempFolder, 0},
        {L"TemplateFolder", InitializeVariableCsidlFolder, CSIDL_TEMPLATES},
        {L"TerminalServer", InitializeVariableOsInfo, OS_INFO_VARIABLE_TerminalServer},
        {L"UserUILanguageID", InitializeUserUILanguageID, 0},
        {L"UserLanguageID", InitializeUserLanguageID, 0},
        {L"VersionMsi", InitializeVariableVersionMsi, 0},
        {L"VersionNT", InitializeVariableVersionNT, OS_INFO_VARIABLE_VersionNT},
        {L"VersionNT64", InitializeVariableVersionNT, OS_INFO_VARIABLE_VersionNT64},
        {L"WindowsFolder", InitializeVariableCsidlFolder, CSIDL_WINDOWS},
        {L"WindowsVolume", InitializeVariableWindowsVolumeFolder, 0},
        {BURN_BUNDLE_ACTION, InitializeVariableNumeric, 0, FALSE, TRUE},
        {BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, InitializeVariableString, NULL, FALSE, TRUE},
        {BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, InitializeVariableString, NULL, FALSE, TRUE},
        {BURN_BUNDLE_FORCED_RESTART_PACKAGE, InitializeVariableString, NULL, TRUE, TRUE},
        {BURN_BUNDLE_INSTALLED, InitializeVariableNumeric, 0, FALSE, TRUE},
        {BURN_BUNDLE_ELEVATED, InitializeVariableNumeric, 0, FALSE, TRUE},
        {BURN_BUNDLE_ACTIVE_PARENT, InitializeVariableString, NULL, FALSE, TRUE},
        {BURN_BUNDLE_PROVIDER_KEY, InitializeVariableString, (DWORD_PTR)L"", FALSE, TRUE},
        {BURN_BUNDLE_SOURCE_PROCESS_PATH, InitializeVariableString, NULL, FALSE, TRUE},
        {BURN_BUNDLE_SOURCE_PROCESS_FOLDER, InitializeVariableString, NULL, FALSE, TRUE},
        {BURN_BUNDLE_TAG, InitializeVariableString, (DWORD_PTR)L"", FALSE, TRUE},
        {BURN_BUNDLE_UILEVEL, InitializeVariableNumeric, 0, FALSE, TRUE},
        {BURN_BUNDLE_VERSION, InitializeVariableVersion, 0, FALSE, TRUE},
    };

    for (DWORD i = 0; i < countof(vrgBuiltInVariables); ++i)
    {
        BUILT_IN_VARIABLE_DECLARATION* pBuiltInVariable = &vrgBuiltInVariables[i];

        hr = AddBuiltInVariable(pVariables, pBuiltInVariable->wzVariable, pBuiltInVariable->pfnInitialize, pBuiltInVariable->dwpInitializeData, pBuiltInVariable->fPersist, pBuiltInVariable->fOverridable);
        ExitOnFailure(hr, "Failed to add built-in variable: %ls.", pBuiltInVariable->wzVariable);
    }

LExit:
    return hr;
}

extern "C" HRESULT VariablesParseFromXml(
    __in BURN_VARIABLES* pVariables,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR sczId = NULL;
    LPWSTR scz = NULL;
    BURN_VARIANT value = { };
    BURN_VARIANT_TYPE valueType = BURN_VARIANT_TYPE_NONE;
    BOOL fHidden = FALSE;
    BOOL fPersisted = FALSE;
    DWORD iVariable = 0;

    ::EnterCriticalSection(&pVariables->csAccess);

    // select variable nodes
    hr = XmlSelectNodes(pixnBundle, L"Variable", &pixnNodes);
    ExitOnFailure(hr, "Failed to select variable nodes.");

    // get variable node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get variable node count.");

    // parse package elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @Hidden
        hr = XmlGetYesNoAttribute(pixnNode, L"Hidden", &fHidden);
        ExitOnFailure(hr, "Failed to get @Hidden.");

        // @Persisted
        hr = XmlGetYesNoAttribute(pixnNode, L"Persisted", &fPersisted);
        ExitOnFailure(hr, "Failed to get @Persisted.");

        // @Value
        hr = XmlGetAttributeEx(pixnNode, L"Value", &scz);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Value.");

            hr = BVariantSetString(&value, scz, 0);
            ExitOnFailure(hr, "Failed to set variant value.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"numeric", -1))
            {
                if (!fHidden)
                {
                    LogStringLine(REPORT_STANDARD, "Initializing numeric variable '%ls' to value '%ls'", sczId, value.sczValue);
                }
                valueType = BURN_VARIANT_TYPE_NUMERIC;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"string", -1))
            {
                if (!fHidden)
                {
                    LogStringLine(REPORT_STANDARD, "Initializing string variable '%ls' to value '%ls'", sczId, value.sczValue);
                }
                valueType = BURN_VARIANT_TYPE_STRING;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"version", -1))
            {
                if (!fHidden)
                {
                    LogStringLine(REPORT_STANDARD, "Initializing version variable '%ls' to value '%ls'", sczId, value.sczValue);
                }
                valueType = BURN_VARIANT_TYPE_VERSION;
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid value for @Type: %ls", scz);
            }
        }
        else
        {
            valueType = BURN_VARIANT_TYPE_NONE;
        }

        if (fHidden)
        {
            LogStringLine(REPORT_STANDARD, "Initializing hidden variable '%ls'", sczId);
        }

        // change value variant to correct type
        hr = BVariantChangeType(&value, valueType);
        ExitOnFailure(hr, "Failed to change variant type.");

        // find existing variable
        hr = FindVariableIndexByName(pVariables, sczId, &iVariable);
        ExitOnFailure(hr, "Failed to find variable value '%ls'.", sczId);

        // insert element if not found
        if (S_FALSE == hr)
        {
            hr = InsertVariable(pVariables, sczId, iVariable);
            ExitOnFailure(hr, "Failed to insert variable '%ls'.", sczId);
        }
        else if (BURN_VARIABLE_INTERNAL_TYPE_NORMAL < pVariables->rgVariables[iVariable].internalType)
        {
            hr = E_INVALIDARG;
            ExitOnRootFailure(hr, "Attempt to set built-in variable value: %ls", sczId);
        }
        pVariables->rgVariables[iVariable].fHidden = fHidden;
        pVariables->rgVariables[iVariable].fPersisted = fPersisted;

        // update variable value
        hr = BVariantSetValue(&pVariables->rgVariables[iVariable].Value, &value);
        ExitOnFailure(hr, "Failed to set value of variable: %ls", sczId);

        hr = BVariantSetEncryption(&pVariables->rgVariables[iVariable].Value, fHidden);
        ExitOnFailure(hr, "Failed to set variant encryption");

        // prepare next iteration
        ReleaseNullObject(pixnNode);
        BVariantUninitialize(&value);
        ReleaseNullStrSecure(scz);
    }

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);
    ReleaseStr(sczId);
    BVariantUninitialize(&value);

    return hr;
}

extern "C" void VariablesUninitialize(
    __in BURN_VARIABLES* pVariables
    )
{
    ::DeleteCriticalSection(&pVariables->csAccess);

    if (pVariables->rgVariables)
    {
        for (DWORD i = 0; i < pVariables->cVariables; ++i)
        {
            BURN_VARIABLE* pVariable = &pVariables->rgVariables[i];
            if (pVariable)
            {
                ReleaseStr(pVariable->sczName);
                BVariantUninitialize(&pVariable->Value);
            }
        }
        MemFree(pVariables->rgVariables);
    }
}

extern "C" void VariablesDump(
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    for (DWORD i = 0; i < pVariables->cVariables; ++i)
    {
        BURN_VARIABLE* pVariable = &pVariables->rgVariables[i];
        if (pVariable && BURN_VARIANT_TYPE_NONE != pVariable->Value.Type)
        {
            hr = StrAllocFormatted(&sczValue, L"%ls = [%ls]", pVariable->sczName, pVariable->sczName);
            if (SUCCEEDED(hr))
            {
                if (pVariable->fHidden)
                {
                    hr = VariableFormatStringObfuscated(pVariables, sczValue, &sczValue, NULL);
                }
                else
                {
                    hr = VariableFormatString(pVariables, sczValue, &sczValue, NULL);
                }
            }

            if (FAILED(hr))
            {
                // already logged; best-effort to dump the rest on our way out the door
                continue;
            }

            LogId(REPORT_VERBOSE, MSG_VARIABLE_DUMP, sczValue);

            ReleaseNullStrSecure(sczValue);
        }
    }

    StrSecureZeroFreeString(sczValue);
}

// The contents of pllValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
extern "C" HRESULT VariableGetNumeric(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    BURN_VARIABLE* pVariable = NULL;

    ::EnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pVariables, wzVariable, &pVariable);
    if (SUCCEEDED(hr) && BURN_VARIANT_TYPE_NONE == pVariable->Value.Type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    else if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get value of variable: %ls", wzVariable);

    hr = BVariantGetNumeric(&pVariable->Value, pllValue);
    ExitOnFailure(hr, "Failed to get value as numeric for variable: %ls", wzVariable);

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

// The contents of psczValue may be sensitive, if variable is hidden should keep encrypted and SecureZeroFree.
extern "C" HRESULT VariableGetString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    BURN_VARIABLE* pVariable = NULL;

    ::EnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pVariables, wzVariable, &pVariable);
    if (SUCCEEDED(hr) && BURN_VARIANT_TYPE_NONE == pVariable->Value.Type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    else if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get value of variable: %ls", wzVariable);

    hr = BVariantGetString(&pVariable->Value, psczValue);
    ExitOnFailure(hr, "Failed to get value as string for variable: %ls", wzVariable);

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

// The contents of pqwValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
extern "C" HRESULT VariableGetVersion(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    BURN_VARIABLE* pVariable = NULL;

    ::EnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pVariables, wzVariable, &pVariable);
    if (SUCCEEDED(hr) && BURN_VARIANT_TYPE_NONE == pVariable->Value.Type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    else if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get value of variable: %ls", wzVariable);

    hr = BVariantGetVersion(&pVariable->Value, pqwValue);
    ExitOnFailure(hr, "Failed to get value as version for variable: %ls", wzVariable);

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

extern "C" HRESULT VariableGetVariant(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    BURN_VARIABLE* pVariable = NULL;

    ::EnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get value of variable: %ls", wzVariable);

    hr = BVariantCopy(&pVariable->Value, pValue);
    ExitOnFailure(hr, "Failed to copy value of variable: %ls", wzVariable);

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

// The contents of psczValue may be sensitive, should keep encrypted and SecureZeroFree.
extern "C" HRESULT VariableGetFormatted(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    BURN_VARIABLE* pVariable = NULL;
    LPWSTR scz = NULL;

    ::EnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pVariables, wzVariable, &pVariable);
    if (SUCCEEDED(hr) && BURN_VARIANT_TYPE_NONE == pVariable->Value.Type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    else if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get variable: %ls", wzVariable);

    // Strings need to get expanded unless they're built-in or literal because they're guaranteed not to have embedded variables.
    if (BURN_VARIANT_TYPE_STRING == pVariable->Value.Type &&
        BURN_VARIABLE_INTERNAL_TYPE_NORMAL == pVariable->internalType &&
        !pVariable->fLiteral)
    {
        hr = BVariantGetString(&pVariable->Value, &scz);
        ExitOnFailure(hr, "Failed to get unformatted string.");

        hr = VariableFormatString(pVariables, scz, psczValue, NULL);
        ExitOnFailure(hr, "Failed to format value '%ls' of variable: %ls", pVariable->fHidden ? L"*****" : pVariable->Value.sczValue, wzVariable);
    }
    else
    {
        hr = BVariantGetString(&pVariable->Value, psczValue);
        ExitOnFailure(hr, "Failed to get value as string for variable: %ls", wzVariable);
    }

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);
    StrSecureZeroFreeString(scz);

    return hr;
}

extern "C" HRESULT VariableSetNumeric(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue,
    __in BOOL fOverwriteBuiltIn
    )
{
    BURN_VARIANT variant = { };

    // We're not going to encrypt this value, so can access the value directly.
    variant.llValue = llValue;
    variant.Type = BURN_VARIANT_TYPE_NUMERIC;

    return SetVariableValue(pVariables, wzVariable, &variant, FALSE, fOverwriteBuiltIn ? SET_VARIABLE_OVERRIDE_BUILTIN : SET_VARIABLE_NOT_BUILTIN, TRUE);
}

extern "C" HRESULT VariableSetLiteralString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fOverwriteBuiltIn
    )
{
    BURN_VARIANT variant = { };

    // We're not going to encrypt this value, so can access the value directly.
    variant.sczValue = (LPWSTR)wzValue;
    variant.Type = BURN_VARIANT_TYPE_STRING;

    return SetVariableValue(pVariables, wzVariable, &variant, TRUE, fOverwriteBuiltIn ? SET_VARIABLE_OVERRIDE_BUILTIN : SET_VARIABLE_NOT_BUILTIN, TRUE);
}

extern "C" HRESULT VariableSetString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fOverwriteBuiltIn
    )
{
    BURN_VARIANT variant = { };

    // We're not going to encrypt this value, so can access the value directly.
    variant.sczValue = (LPWSTR)wzValue;
    variant.Type = BURN_VARIANT_TYPE_STRING;

    return SetVariableValue(pVariables, wzVariable, &variant, FALSE, fOverwriteBuiltIn ? SET_VARIABLE_OVERRIDE_BUILTIN : SET_VARIABLE_NOT_BUILTIN, TRUE);
}

extern "C" HRESULT VariableSetVersion(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue,
    __in BOOL fOverwriteBuiltIn
    )
{
    BURN_VARIANT variant = { };

    // We're not going to encrypt this value, so can access the value directly.
    variant.qwValue = qwValue;
    variant.Type = BURN_VARIANT_TYPE_VERSION;

    return SetVariableValue(pVariables, wzVariable, &variant, FALSE, fOverwriteBuiltIn ? SET_VARIABLE_OVERRIDE_BUILTIN : SET_VARIABLE_NOT_BUILTIN, TRUE);
}

extern "C" HRESULT VariableSetLiteralVariant(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in BURN_VARIANT* pVariant
    )
{
    return SetVariableValue(pVariables, wzVariable, pVariant, TRUE, SET_VARIABLE_NOT_BUILTIN, TRUE);
}

// The contents of psczOut may be sensitive, should keep encrypted and SecureZeroFree
extern "C" HRESULT VariableFormatString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    return FormatString(pVariables, wzIn, psczOut, pcchOut, FALSE);
}

extern "C" HRESULT VariableFormatStringObfuscated(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    return FormatString(pVariables, wzIn, psczOut, pcchOut, TRUE);
}

extern "C" HRESULT VariableEscapeString(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzRead = NULL;
    LPWSTR pwzEscaped = NULL;
    LPWSTR pwz = NULL;
    SIZE_T i = 0;

    // allocate buffer for escaped string
    hr = StrAlloc(&pwzEscaped, lstrlenW(wzIn) + 1);
    ExitOnFailure(hr, "Failed to allocate buffer for escaped string.");

    // read through string and move characters, inserting escapes as needed
    wzRead = wzIn;
    for (;;)
    {
        // find next character needing escaping
        i = wcscspn(wzRead, L"[]{}");

        // copy skipped characters
        if (0 < i)
        {
            hr = StrAllocConcat(&pwzEscaped, wzRead, i);
            ExitOnFailure(hr, "Failed to append characters.");
        }

        if (L'\0' == wzRead[i])
        {
            break; // end reached
        }

        // escape character
        hr = StrAllocFormatted(&pwz, L"[\\%c]", wzRead[i]);
        ExitOnFailure(hr, "Failed to format escape sequence.");

        hr = StrAllocConcat(&pwzEscaped, pwz, 0);
        ExitOnFailure(hr, "Failed to append escape sequence.");

        // update read pointer
        wzRead += i + 1;
    }

    // return value
    hr = StrAllocString(psczOut, pwzEscaped, 0);
    ExitOnFailure(hr, "Failed to copy string.");

LExit:
    ReleaseStr(pwzEscaped);
    ReleaseStr(pwz);
    return hr;
}

extern "C" HRESULT VariableSerialize(
    __in BURN_VARIABLES* pVariables,
    __in BOOL fPersisting,
    __inout BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    )
{
    HRESULT hr = S_OK;
    BOOL fIncluded = FALSE;
    LONGLONG ll = 0;
    LPWSTR scz = NULL;
    DWORD64 qw = 0;

    ::EnterCriticalSection(&pVariables->csAccess);

    // Write variable count.
    hr = BuffWriteNumber(ppbBuffer, piBuffer, pVariables->cVariables);
    ExitOnFailure(hr, "Failed to write variable count.");

    // Write variables.
    for (DWORD i = 0; i < pVariables->cVariables; ++i)
    {
        BURN_VARIABLE* pVariable = &pVariables->rgVariables[i];

        // If we aren't persisting, include only variables that aren't rejected by the elevated process.
        // If we are persisting, include only variables that should be persisted.
        fIncluded = (!fPersisting && BURN_VARIABLE_INTERNAL_TYPE_BUILTIN != pVariable->internalType) ||
                    (fPersisting && pVariable->fPersisted);

        // Write included flag.
        hr = BuffWriteNumber(ppbBuffer, piBuffer, (DWORD)fIncluded);
        ExitOnFailure(hr, "Failed to write included flag.");

        if (!fIncluded)
        {
            continue;
        }

        // Write variable name.
        hr = BuffWriteString(ppbBuffer, piBuffer, pVariable->sczName);
        ExitOnFailure(hr, "Failed to write variable name.");

        // Write variable value type.
        hr = BuffWriteNumber(ppbBuffer, piBuffer, (DWORD)pVariable->Value.Type);
        ExitOnFailure(hr, "Failed to write variable value type.");

        // Write variable value.
        switch (pVariable->Value.Type)
        {
        case BURN_VARIANT_TYPE_NONE:
            break;
        case BURN_VARIANT_TYPE_NUMERIC:
            hr = BVariantGetNumeric(&pVariable->Value, &ll);
            ExitOnFailure(hr, "Failed to get numeric.");

            hr = BuffWriteNumber64(ppbBuffer, piBuffer, static_cast<DWORD64>(ll));
            ExitOnFailure(hr, "Failed to write variable value as number.");

            SecureZeroMemory(&ll, sizeof(ll));
            break;
        case BURN_VARIANT_TYPE_VERSION:
            hr = BVariantGetVersion(&pVariable->Value, &qw);
            ExitOnFailure(hr, "Failed to get version.");

            hr = BuffWriteNumber64(ppbBuffer, piBuffer, qw);
            ExitOnFailure(hr, "Failed to write variable value as number.");

            SecureZeroMemory(&qw, sizeof(qw));
            break;
        case BURN_VARIANT_TYPE_STRING:
            hr = BVariantGetString(&pVariable->Value, &scz);
            ExitOnFailure(hr, "Failed to get string.");

            hr = BuffWriteString(ppbBuffer, piBuffer, scz);
            ExitOnFailure(hr, "Failed to write variable value as string.");

            ReleaseNullStrSecure(scz);
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported variable type.");
        }

        // Write literal flag.
        hr = BuffWriteNumber(ppbBuffer, piBuffer, (DWORD)pVariable->fLiteral);
        ExitOnFailure(hr, "Failed to write literal flag.");
    }

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);
    SecureZeroMemory(&ll, sizeof(ll));
    SecureZeroMemory(&qw, sizeof(qw));
    StrSecureZeroFreeString(scz);

    return hr;
}

extern "C" HRESULT VariableDeserialize(
    __in BURN_VARIABLES* pVariables,
    __in BOOL fWasPersisted,
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __inout SIZE_T* piBuffer
    )
{
    HRESULT hr = S_OK;
    DWORD cVariables = 0;
    LPWSTR sczName = NULL;
    BOOL fIncluded = FALSE;
    BOOL fLiteral = FALSE;
    BURN_VARIANT value = { };
    LPWSTR scz = NULL;
    DWORD64 qw = 0;

    ::EnterCriticalSection(&pVariables->csAccess);

    // Read variable count.
    hr = BuffReadNumber(pbBuffer, cbBuffer, piBuffer, &cVariables);
    ExitOnFailure(hr, "Failed to read variable count.");

    // Read variables.
    for (DWORD i = 0; i < cVariables; ++i)
    {
        // Read variable included flag.
        hr = BuffReadNumber(pbBuffer, cbBuffer, piBuffer, (DWORD*)&fIncluded);
        ExitOnFailure(hr, "Failed to read variable included flag.");

        if (!fIncluded)
        {
            continue; // if variable is not included, skip.
        }

        // Read variable name.
        hr = BuffReadString(pbBuffer, cbBuffer, piBuffer, &sczName);
        ExitOnFailure(hr, "Failed to read variable name.");

        // Read variable value type.
        hr = BuffReadNumber(pbBuffer, cbBuffer, piBuffer, (DWORD*)&value.Type);
        ExitOnFailure(hr, "Failed to read variable value type.");

        // Read variable value.
        switch (value.Type)
        {
        case BURN_VARIANT_TYPE_NONE:
            break;
        case BURN_VARIANT_TYPE_NUMERIC:
            hr = BuffReadNumber64(pbBuffer, cbBuffer, piBuffer, &qw);
            ExitOnFailure(hr, "Failed to read variable value as number.");

            hr = BVariantSetNumeric(&value, static_cast<LONGLONG>(qw));
            ExitOnFailure(hr, "Failed to set variable value.");

            SecureZeroMemory(&qw, sizeof(qw));
            break;
        case BURN_VARIANT_TYPE_VERSION:
            hr = BuffReadNumber64(pbBuffer, cbBuffer, piBuffer, &qw);
            ExitOnFailure(hr, "Failed to read variable value as number.");

            hr = BVariantSetVersion(&value, qw);
            ExitOnFailure(hr, "Failed to set variable value.");

            SecureZeroMemory(&qw, sizeof(qw));
            break;
        case BURN_VARIANT_TYPE_STRING:
            hr = BuffReadString(pbBuffer, cbBuffer, piBuffer, &scz);
            ExitOnFailure(hr, "Failed to read variable value as string.");

            hr = BVariantSetString(&value, scz, NULL);
            ExitOnFailure(hr, "Failed to set variable value.");

            ReleaseNullStrSecure(scz);
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported variable type.");
        }

        // Read variable literal flag.
        hr = BuffReadNumber(pbBuffer, cbBuffer, piBuffer, (DWORD*)&fLiteral);
        ExitOnFailure(hr, "Failed to read variable literal flag.");

        // Set variable.
        hr = SetVariableValue(pVariables, sczName, &value, fLiteral, fWasPersisted ? SET_VARIABLE_OVERRIDE_PERSISTED_BUILTINS : SET_VARIABLE_ANY, FALSE);
        ExitOnFailure(hr, "Failed to set variable.");

        // Clean up.
        BVariantUninitialize(&value);
    }

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    ReleaseStr(sczName);
    BVariantUninitialize(&value);
    SecureZeroMemory(&qw, sizeof(qw));
    StrSecureZeroFreeString(scz);

    return hr;
}

extern "C" HRESULT VariableStrAlloc(
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in DWORD_PTR cch
    )
{
    HRESULT hr = S_OK;

    if (fZeroOnRealloc)
    {
        hr = StrAllocSecure(ppwz, cch);
    }
    else
    {
        hr = StrAlloc(ppwz, cch);
    }

    return hr;
}

extern "C" HRESULT VariableStrAllocString(
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_z(cchSource + 1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    )
{
    HRESULT hr = S_OK;

    if (fZeroOnRealloc)
    {
        hr = StrAllocStringSecure(ppwz, wzSource, cchSource);
    }
    else
    {
        hr = StrAllocString(ppwz, wzSource, cchSource);
    }

    return hr;
}

extern "C" HRESULT VariableStrAllocConcat(
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    )
{
    HRESULT hr = S_OK;

    if (fZeroOnRealloc)
    {
        hr = StrAllocConcatSecure(ppwz, wzSource, cchSource);
    }
    else
    {
        hr = StrAllocConcat(ppwz, wzSource, cchSource);
    }

    return hr;
}

extern "C" HRESULT __cdecl VariableStrAllocFormatted(
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, wzFormat);
    if (fZeroOnRealloc)
    {
        hr = StrAllocFormattedArgsSecure(ppwz, wzFormat, args);
    }
    else
    {
        hr = StrAllocFormattedArgs(ppwz, wzFormat, args);
    }
    va_end(args);

    return hr;
}

extern "C" HRESULT VariableIsHidden(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out BOOL* pfHidden
    )
{
    HRESULT hr = S_OK;
    BURN_VARIABLE* pVariable = NULL;

    ::EnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        // A missing variable does not need its data hidden.
        *pfHidden = FALSE;

        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to get visibility of variable: %ls", wzVariable);

    *pfHidden = pVariable->fHidden;

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    return hr;
}


// internal function definitions

// The contents of psczOut may be sensitive, should keep encrypted and SecureZeroFree.
static HRESULT FormatString(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut,
    __in BOOL fObfuscateHiddenVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczUnformatted = NULL;
    LPWSTR sczFormat = NULL;
    LPCWSTR wzRead = NULL;
    LPCWSTR wzOpen = NULL;
    LPCWSTR wzClose = NULL;
    LPWSTR scz = NULL;
    LPWSTR* rgVariables = NULL;
    DWORD cVariables = 0;
    DWORD cch = 0;
    BOOL fHidden = FALSE;
    MSIHANDLE hRecord = NULL;

    ::EnterCriticalSection(&pVariables->csAccess);

    // allocate buffer for format string
    hr = StrAlloc(&sczFormat, lstrlenW(wzIn) + 1);
    ExitOnFailure(hr, "Failed to allocate buffer for format string.");

    // read out variables from the unformatted string and build a format string
    wzRead = wzIn;
    for (;;)
    {
        // scan for opening '['
        wzOpen = wcschr(wzRead, L'[');
        if (!wzOpen)
        {
            // end reached, append the remainder of the string and end loop
            hr = VariableStrAllocConcat(!fObfuscateHiddenVariables, &sczFormat, wzRead, 0);
            ExitOnFailure(hr, "Failed to append string.");
            break;
        }

        // scan for closing ']'
        wzClose = wcschr(wzOpen + 1, L']');
        if (!wzClose)
        {
            // end reached, treat unterminated expander as literal
            hr = VariableStrAllocConcat(!fObfuscateHiddenVariables, &sczFormat, wzRead, 0);
            ExitOnFailure(hr, "Failed to append string.");
            break;
        }
        cch = wzClose - wzOpen - 1;

        if (0 == cch)
        {
            // blank, copy all text including the terminator
            hr = VariableStrAllocConcat(!fObfuscateHiddenVariables, &sczFormat, wzRead, (DWORD_PTR)(wzClose - wzRead) + 1);
            ExitOnFailure(hr, "Failed to append string.");
        }
        else
        {
            // append text preceding expander
            if (wzOpen > wzRead)
            {
                hr = VariableStrAllocConcat(!fObfuscateHiddenVariables, &sczFormat, wzRead, (DWORD_PTR)(wzOpen - wzRead));
                ExitOnFailure(hr, "Failed to append string.");
            }

            // get variable name
            hr = VariableStrAllocString(!fObfuscateHiddenVariables, &scz, wzOpen + 1, cch);
            ExitOnFailure(hr, "Failed to get variable name.");

            // allocate space in variable array
            if (rgVariables)
            {
                LPVOID pv = MemReAlloc(rgVariables, sizeof(LPWSTR) * (cVariables + 1), TRUE);
                ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to reallocate variable array.");
                rgVariables = (LPWSTR*)pv;
            }
            else
            {
                rgVariables = (LPWSTR*)MemAlloc(sizeof(LPWSTR) * (cVariables + 1), TRUE);
                ExitOnNull(rgVariables, hr, E_OUTOFMEMORY, "Failed to allocate variable array.");
            }

            // set variable value
            if (2 <= cch && L'\\' == wzOpen[1])
            {
                // escape sequence, copy character
                hr = VariableStrAllocString(!fObfuscateHiddenVariables, &rgVariables[cVariables], &wzOpen[2], 1);
            }
            else
            {
                if (fObfuscateHiddenVariables)
                {
                    hr = VariableIsHidden(pVariables, scz, &fHidden);
                    ExitOnFailure1(hr, "Failed to determine variable visibility: '%ls'.", scz);
                }

                if (fHidden)
                {
                    hr = StrAllocString(&rgVariables[cVariables], L"*****", 0);
                }
                else
                {
                    // get formatted variable value
                    hr = VariableGetFormatted(pVariables, scz, &rgVariables[cVariables]);
                    if (E_NOTFOUND == hr) // variable not found
                    {
                        hr = StrAllocStringSecure(&rgVariables[cVariables], L"", 0);
                    }
                }
            }
            ExitOnFailure(hr, "Failed to set variable value.");
            ++cVariables;

            // append placeholder to format string
            hr = VariableStrAllocFormatted(!fObfuscateHiddenVariables, &scz, L"[%d]", cVariables);
            ExitOnFailure(hr, "Failed to format placeholder string.");

            hr = VariableStrAllocConcat(!fObfuscateHiddenVariables, &sczFormat, scz, 0);
            ExitOnFailure(hr, "Failed to append placeholder.");
        }

        // update read pointer
        wzRead = wzClose + 1;
    }

    // create record
    hRecord = ::MsiCreateRecord(cVariables);
    ExitOnNull(hRecord, hr, E_OUTOFMEMORY, "Failed to allocate record.");

    // set format string
    er = ::MsiRecordSetStringW(hRecord, 0, sczFormat);
    ExitOnWin32Error(er, hr, "Failed to set record format string.");

    // copy record fields
    for (DWORD i = 0; i < cVariables; ++i)
    {
        if (*rgVariables[i]) // not setting if blank
        {
            er = ::MsiRecordSetStringW(hRecord, i + 1, rgVariables[i]);
            ExitOnWin32Error(er, hr, "Failed to set record string.");
        }
    }

    // get formatted character count
    cch = 0;
#pragma prefast(push)
#pragma prefast(disable:6298)
    er = ::MsiFormatRecordW(NULL, hRecord, L"", &cch);
#pragma prefast(pop)
    if (ERROR_MORE_DATA != er)
    {
        ExitOnWin32Error(er, hr, "Failed to get formatted length.");
    }

    // return formatted string
    if (psczOut)
    {
        hr = VariableStrAlloc(!fObfuscateHiddenVariables, &scz, ++cch);
        ExitOnFailure(hr, "Failed to allocate string.");

        er = ::MsiFormatRecordW(NULL, hRecord, scz, &cch);
        ExitOnWin32Error(er, hr, "Failed to format record.");

        hr = VariableStrAllocString(!fObfuscateHiddenVariables, psczOut, scz, 0);
        ExitOnFailure(hr, "Failed to copy string.");
    }

    // return character count
    if (pcchOut)
    {
        *pcchOut = cch;
    }

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    if (rgVariables)
    {
        for (DWORD i = 0; i < cVariables; ++i)
        {
            if (fObfuscateHiddenVariables)
            {
                ReleaseStr(rgVariables[i]);
            }
            else
            {
                StrSecureZeroFreeString(rgVariables[i]);
            }
        }
        MemFree(rgVariables);
    }

    if (hRecord)
    {
        ::MsiCloseHandle(hRecord);
    }

    if (fObfuscateHiddenVariables)
    {
        ReleaseStr(sczUnformatted);
        ReleaseStr(sczFormat);
        ReleaseStr(scz);
    }
    else
    {
        StrSecureZeroFreeString(sczUnformatted);
        StrSecureZeroFreeString(sczFormat);
        StrSecureZeroFreeString(scz);
    }

    return hr;
}

static HRESULT AddBuiltInVariable(
    __in BURN_VARIABLES* pVariables,
    __in LPCWSTR wzVariable,
    __in PFN_INITIALIZEVARIABLE pfnInitialize,
    __in DWORD_PTR dwpInitializeData,
    __in BOOL fPersist,
    __in BOOL fOverridable
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;
    BURN_VARIABLE* pVariable = NULL;

    hr = FindVariableIndexByName(pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value.");

    // insert element if not found
    if (S_FALSE == hr)
    {
        hr = InsertVariable(pVariables, wzVariable, iVariable);
        ExitOnFailure(hr, "Failed to insert variable.");
    }

    // set variable values
    pVariable = &pVariables->rgVariables[iVariable];
    pVariable->fPersisted = fPersist;
    pVariable->internalType = fOverridable ? BURN_VARIABLE_INTERNAL_TYPE_OVERRIDABLE_BUILTIN : BURN_VARIABLE_INTERNAL_TYPE_BUILTIN;
    pVariable->pfnInitialize = pfnInitialize;
    pVariable->dwpInitializeData = dwpInitializeData;

LExit:
    return hr;
}

static HRESULT GetVariable(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out BURN_VARIABLE** ppVariable
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;
    BURN_VARIABLE* pVariable = NULL;

    hr = FindVariableIndexByName(pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    pVariable = &pVariables->rgVariables[iVariable];

    // initialize built-in variable
    if (BURN_VARIANT_TYPE_NONE == pVariable->Value.Type && BURN_VARIABLE_INTERNAL_TYPE_NORMAL < pVariable->internalType)
    {
        hr = pVariable->pfnInitialize(pVariable->dwpInitializeData, &pVariable->Value);
        ExitOnFailure(hr, "Failed to initialize built-in variable value '%ls'.", wzVariable);
    }

    *ppVariable = pVariable;

LExit:
    return hr;
}

static HRESULT FindVariableIndexByName(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __out DWORD* piVariable
    )
{
    HRESULT hr = S_OK;
    DWORD iRangeFirst = 0;
    DWORD cRangeLength = pVariables->cVariables;

    while (cRangeLength)
    {
        // get variable in middle of range
        DWORD iPosition = cRangeLength / 2;
        BURN_VARIABLE* pVariable = &pVariables->rgVariables[iRangeFirst + iPosition];

        switch (::CompareStringW(LOCALE_INVARIANT, SORT_STRINGSORT, wzVariable, -1, pVariable->sczName, -1))
        {
        case CSTR_LESS_THAN:
            // restrict range to elements before the current
            cRangeLength = iPosition;
            break;
        case CSTR_EQUAL:
            // variable found
            *piVariable = iRangeFirst + iPosition;
            ExitFunction1(hr = S_OK);
        case CSTR_GREATER_THAN:
            // restrict range to elements after the current
            iRangeFirst += iPosition + 1;
            cRangeLength -= iPosition + 1;
            break;
        default:
            ExitWithLastError(hr, "Failed to compare strings.");
        }
    }

    *piVariable = iRangeFirst;
    hr = S_FALSE; // variable not found

LExit:
    return hr;
}

static HRESULT InsertVariable(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD iPosition
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;

    // ensure there is room in the variable array
    if (pVariables->cVariables == pVariables->dwMaxVariables)
    {
        hr = ::DWordAdd(pVariables->dwMaxVariables, GROW_VARIABLE_ARRAY, &(pVariables->dwMaxVariables));
        ExitOnRootFailure(hr, "Overflow while growing variable array size");

        if (pVariables->rgVariables)
        {
            hr = ::SizeTMult(sizeof(BURN_VARIABLE), pVariables->dwMaxVariables, &cbAllocSize);
            ExitOnRootFailure(hr, "Overflow while calculating size of variable array buffer");

            LPVOID pv = MemReAlloc(pVariables->rgVariables, cbAllocSize, FALSE);
            ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate room for more variables.");

            // Prefast claims it's possible to hit this. Putting the check in just in case.
            if (pVariables->dwMaxVariables < pVariables->cVariables)
            {
                hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
                ExitOnRootFailure(hr, "Overflow while dealing with variable array buffer allocation");
            }

            pVariables->rgVariables = (BURN_VARIABLE*)pv;
            memset(&pVariables->rgVariables[pVariables->cVariables], 0, sizeof(BURN_VARIABLE) * (pVariables->dwMaxVariables - pVariables->cVariables));
        }
        else
        {
            pVariables->rgVariables = (BURN_VARIABLE*)MemAlloc(sizeof(BURN_VARIABLE) * pVariables->dwMaxVariables, TRUE);
            ExitOnNull(pVariables->rgVariables, hr, E_OUTOFMEMORY, "Failed to allocate room for variables.");
        }
    }

    // move variables
    if (0 < pVariables->cVariables - iPosition)
    {
        memmove(&pVariables->rgVariables[iPosition + 1], &pVariables->rgVariables[iPosition], sizeof(BURN_VARIABLE) * (pVariables->cVariables - iPosition));
        memset(&pVariables->rgVariables[iPosition], 0, sizeof(BURN_VARIABLE));
    }

    ++pVariables->cVariables;

    // allocate name
    hr = StrAllocString(&pVariables->rgVariables[iPosition].sczName, wzVariable, 0);
    ExitOnFailure(hr, "Failed to copy variable name.");

LExit:
    return hr;
}

static HRESULT SetVariableValue(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in BURN_VARIANT* pVariant,
    __in BOOL fLiteral,
    __in SET_VARIABLE setBuiltin,
    __in BOOL fLog
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    ::EnterCriticalSection(&pVariables->csAccess);

    hr = FindVariableIndexByName(pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    // Insert element if not found.
    if (S_FALSE == hr)
    {
        hr = InsertVariable(pVariables, wzVariable, iVariable);
        ExitOnFailure(hr, "Failed to insert variable '%ls'.", wzVariable);
    }
    else if (BURN_VARIABLE_INTERNAL_TYPE_NORMAL < pVariables->rgVariables[iVariable].internalType) // built-in variables must be overridden.
    {
        if (SET_VARIABLE_OVERRIDE_BUILTIN == setBuiltin ||
            (SET_VARIABLE_OVERRIDE_PERSISTED_BUILTINS == setBuiltin && pVariables->rgVariables[iVariable].fPersisted) ||
            SET_VARIABLE_ANY == setBuiltin && BURN_VARIABLE_INTERNAL_TYPE_BUILTIN != pVariables->rgVariables[iVariable].internalType)
        {
            hr = S_OK;
        }
        else
        {
            hr = E_INVALIDARG;
            ExitOnRootFailure(hr, "Attempt to set built-in variable value: %ls", wzVariable);
        }
    }
    else // must *not* be a built-in variable so caller should not have tried to override it as a built-in.
    {
        // Not possible from external callers so just assert.
        AssertSz(SET_VARIABLE_OVERRIDE_BUILTIN != setBuiltin, "Intent to overwrite non-built-in variable.");
    }

    // Log value when not overwriting a built-in variable.
    if (fLog && BURN_VARIABLE_INTERNAL_TYPE_NORMAL == pVariables->rgVariables[iVariable].internalType)
    {
        if (pVariables->rgVariables[iVariable].fHidden)
        {
            LogStringLine(REPORT_STANDARD, "Setting hidden variable '%ls'", wzVariable);
        }
        else
        {
            // Assume value isn't encrypted since it's not hidden.
            switch (pVariant->Type)
            {
            case BURN_VARIANT_TYPE_NONE:
                if (BURN_VARIANT_TYPE_NONE != pVariables->rgVariables[iVariable].Value.Type)
                {
                    LogStringLine(REPORT_STANDARD, "Unsetting variable '%ls'", wzVariable, pVariant->sczValue);
                }
                break;

            case BURN_VARIANT_TYPE_NUMERIC:
                LogStringLine(REPORT_STANDARD, "Setting numeric variable '%ls' to value %lld", wzVariable, pVariant->llValue);
                break;

            case BURN_VARIANT_TYPE_STRING:
                if (!pVariant->sczValue)
                {
                    LogStringLine(REPORT_STANDARD, "Unsetting variable '%ls'", wzVariable, pVariant->sczValue);
                }
                else
                {
                    LogStringLine(REPORT_STANDARD, "Setting string variable '%ls' to value '%ls'", wzVariable, pVariant->sczValue);
                }
                break;

            case BURN_VARIANT_TYPE_VERSION:
                LogStringLine(REPORT_STANDARD, "Setting version variable '%ls' to value '%hu.%hu.%hu.%hu'", wzVariable, (WORD)(pVariant->qwValue >> 48), (WORD)(pVariant->qwValue >> 32), (WORD)(pVariant->qwValue >> 16), (WORD)(pVariant->qwValue));
                break;

            default:
                AssertSz(FALSE, "Unknown variant type.");
                break;
            }
        }
    }

    // Update variable value.
    hr = BVariantSetValue(&pVariables->rgVariables[iVariable].Value, pVariant);
    ExitOnFailure(hr, "Failed to set value of variable: %ls", wzVariable);

    // Update variable literal flag.
    pVariables->rgVariables[iVariable].fLiteral = fLiteral;

LExit:
    ::LeaveCriticalSection(&pVariables->csAccess);

    if (FAILED(hr) && fLog)
    {
        LogStringLine(REPORT_STANDARD, "Setting variable failed: ID '%ls', HRESULT 0x%x", wzVariable, hr);
    }

    return hr;
}

extern "C" typedef NTSTATUS (NTAPI *RTL_GET_VERSION)(_Out_  PRTL_OSVERSIONINFOEXW lpVersionInformation);

static HRESULT InitializeVariableVersionNT(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    HMODULE ntdll = NULL;
    RTL_GET_VERSION rtlGetVersion = NULL;
    RTL_OSVERSIONINFOEXW ovix = { };
    BURN_VARIANT value = { };

    if (!::GetModuleHandleExW(0, L"ntdll", &ntdll))
    {
        ExitWithLastError(hr, "Failed to locate NTDLL.");
    }

    rtlGetVersion = reinterpret_cast<RTL_GET_VERSION>(::GetProcAddress(ntdll, "RtlGetVersion"));
    if (NULL == rtlGetVersion)
    {
        ExitWithLastError(hr, "Failed to locate RtlGetVersion.");
    }

    ovix.dwOSVersionInfoSize = sizeof(RTL_OSVERSIONINFOEXW);
    hr = static_cast<HRESULT>(rtlGetVersion(&ovix));
    ExitOnFailure(hr, "Failed to get OS info.");

    switch ((OS_INFO_VARIABLE)dwpData)
    {
    case OS_INFO_VARIABLE_ServicePackLevel:
        if (0 != ovix.wServicePackMajor)
        {
            value.qwValue = static_cast<DWORD64>(ovix.wServicePackMajor);
            value.Type = BURN_VARIANT_TYPE_NUMERIC;
        }
        break;
    case OS_INFO_VARIABLE_VersionNT:
        value.qwValue = MAKEQWORDVERSION(ovix.dwMajorVersion, ovix.dwMinorVersion, 0, 0);
        value.Type = BURN_VARIANT_TYPE_VERSION;
        break;
    case OS_INFO_VARIABLE_VersionNT64:
        {
#if !defined(_WIN64)
            BOOL fIsWow64 = FALSE;

            ProcWow64(::GetCurrentProcess(), &fIsWow64);
            if (fIsWow64)
#endif
            {
                value.qwValue = MAKEQWORDVERSION(ovix.dwMajorVersion, ovix.dwMinorVersion, 0, 0);
                value.Type = BURN_VARIANT_TYPE_VERSION;
            }
        }
        break;
    default:
        AssertSz(FALSE, "Unknown OS info type.");
        break;
    }

    hr = BVariantCopy(&value, pValue);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    if (NULL != ntdll)
    {
        FreeLibrary(ntdll);
    }

    return hr;
}

static HRESULT InitializeVariableOsInfo(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    OSVERSIONINFOEXW ovix = { };
    BURN_VARIANT value = { };

    ovix.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
    if (!::GetVersionExW((LPOSVERSIONINFOW)&ovix))
    {
        ExitWithLastError(hr, "Failed to get OS info.");
    }

    switch ((OS_INFO_VARIABLE)dwpData)
    {
    case OS_INFO_VARIABLE_NTProductType:
        value.llValue = ovix.wProductType;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_NTSuiteBackOffice:
        value.llValue = VER_SUITE_BACKOFFICE & ovix.wSuiteMask ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_NTSuiteDataCenter:
        value.llValue = VER_SUITE_DATACENTER & ovix.wSuiteMask ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_NTSuiteEnterprise:
        value.llValue = VER_SUITE_ENTERPRISE & ovix.wSuiteMask ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_NTSuitePersonal:
        value.llValue = VER_SUITE_PERSONAL & ovix.wSuiteMask ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_NTSuiteSmallBusiness:
        value.llValue = VER_SUITE_SMALLBUSINESS & ovix.wSuiteMask ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_NTSuiteSmallBusinessRestricted:
        value.llValue = VER_SUITE_SMALLBUSINESS_RESTRICTED & ovix.wSuiteMask ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_NTSuiteWebServer:
        value.llValue = VER_SUITE_BLADE & ovix.wSuiteMask ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    case OS_INFO_VARIABLE_CompatibilityMode:
        {
            DWORDLONG dwlConditionMask = 0;
            VER_SET_CONDITION(dwlConditionMask, VER_MAJORVERSION, VER_EQUAL);
            VER_SET_CONDITION(dwlConditionMask, VER_MINORVERSION, VER_EQUAL);
            VER_SET_CONDITION(dwlConditionMask, VER_SERVICEPACKMAJOR, VER_EQUAL);
            VER_SET_CONDITION(dwlConditionMask, VER_SERVICEPACKMINOR, VER_EQUAL);

            value.llValue = ::VerifyVersionInfoW(&ovix, VER_MAJORVERSION | VER_MINORVERSION | VER_SERVICEPACKMAJOR | VER_SERVICEPACKMINOR, dwlConditionMask);
            value.Type = BURN_VARIANT_TYPE_NUMERIC;
        }
        break;
    case OS_INFO_VARIABLE_TerminalServer:
        value.llValue = (VER_SUITE_TERMINAL == (ovix.wSuiteMask & VER_SUITE_TERMINAL)) && (VER_SUITE_SINGLEUSERTS != (ovix.wSuiteMask & VER_SUITE_SINGLEUSERTS)) ? 1 : 0;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    default:
        AssertSz(FALSE, "Unknown OS info type.");
        break;
    }

    hr = BVariantCopy(&value, pValue);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableSystemInfo(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    SYSTEM_INFO si = { };
    BURN_VARIANT value = { };

    ::GetNativeSystemInfo(&si);

    switch ((OS_INFO_VARIABLE)dwpData)
    {
    case OS_INFO_VARIABLE_ProcessorArchitecture:
        value.llValue = si.wProcessorArchitecture;
        value.Type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    default:
        AssertSz(FALSE, "Unknown OS info type.");
        break;
    }

    hr = BVariantCopy(&value, pValue);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableComputerName(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    WCHAR wzComputerName[MAX_COMPUTERNAME_LENGTH + 1] = { };
    DWORD cchComputerName = countof(wzComputerName);

    // get computer name
    if (!::GetComputerNameW(wzComputerName, &cchComputerName))
    {
        ExitWithLastError(hr, "Failed to get computer name.");
    }

    // set value
    hr = BVariantSetString(pValue, wzComputerName, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableVersionMsi(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    DLLGETVERSIONPROC pfnMsiDllGetVersion = NULL;
    DLLVERSIONINFO msiVersionInfo = { };

    // get DllGetVersion proc address
    pfnMsiDllGetVersion = (DLLGETVERSIONPROC)::GetProcAddress(::GetModuleHandleW(L"msi"), "DllGetVersion");
    ExitOnNullWithLastError(pfnMsiDllGetVersion, hr, "Failed to find DllGetVersion entry point in msi.dll.");

    // get msi.dll version info
    msiVersionInfo.cbSize = sizeof(DLLVERSIONINFO);
    hr = pfnMsiDllGetVersion(&msiVersionInfo);
    ExitOnFailure(hr, "Failed to get msi.dll version info.");

    hr = BVariantSetVersion(pValue, MAKEQWORDVERSION(msiVersionInfo.dwMajorVersion, msiVersionInfo.dwMinorVersion, 0, 0));
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableCsidlFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    int nFolder = (int)dwpData;

    // get folder path
    hr = ShelGetFolder(&sczPath, nFolder);
    ExitOnRootFailure(hr, "Failed to get shell folder.");

    // set value
    hr = BVariantSetString(pValue, sczPath, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    ReleaseStr(sczPath);

    return hr;
}

static HRESULT InitializeVariableTempFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    WCHAR wzPath[MAX_PATH] = { };

    // get volume path name
    if (!::GetTempPathW(MAX_PATH, wzPath))
    {
        ExitWithLastError(hr, "Failed to get temp path.");
    }

    // set value
    hr = BVariantSetString(pValue, wzPath, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableSystemFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    BOOL f64 = (BOOL)dwpData;
    WCHAR wzSystemFolder[MAX_PATH] = { };

#if !defined(_WIN64)
    BOOL fIsWow64 = FALSE;
    ProcWow64(::GetCurrentProcess(), &fIsWow64);

    if (fIsWow64)
    {
        if (f64)
        {
            if (!::GetSystemDirectoryW(wzSystemFolder, countof(wzSystemFolder)))
            {
                ExitWithLastError(hr, "Failed to get 64-bit system folder.");
            }
        }
        else
        {
            if (!::GetSystemWow64DirectoryW(wzSystemFolder, countof(wzSystemFolder)))
            {
                ExitWithLastError(hr, "Failed to get 32-bit system folder.");
            }
        }
    }
    else
    {
        if (!f64)
        {
            if (!::GetSystemDirectoryW(wzSystemFolder, countof(wzSystemFolder)))
            {
                ExitWithLastError(hr, "Failed to get 32-bit system folder.");
            }
        }
    }
#else
    if (f64)
    {
        if (!::GetSystemDirectoryW(wzSystemFolder, countof(wzSystemFolder)))
        {
            ExitWithLastError(hr, "Failed to get 64-bit system folder.");
        }
    }
    else
    {
        if (!::GetSystemWow64DirectoryW(wzSystemFolder, countof(wzSystemFolder)))
        {
            ExitWithLastError(hr, "Failed to get 32-bit system folder.");
        }
    }
#endif

    if (*wzSystemFolder)
    {
        hr = PathFixedBackslashTerminate(wzSystemFolder, countof(wzSystemFolder));
        ExitOnFailure(hr, "Failed to backslash terminate system folder.");
    }

    // set value
    hr = BVariantSetString(pValue, wzSystemFolder, 0);
    ExitOnFailure(hr, "Failed to set system folder variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableWindowsVolumeFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    WCHAR wzWindowsPath[MAX_PATH] = { };
    WCHAR wzVolumePath[MAX_PATH] = { };

    // get windows directory
    if (!::GetWindowsDirectoryW(wzWindowsPath, countof(wzWindowsPath)))
    {
        ExitWithLastError(hr, "Failed to get windows directory.");
    }

    // get volume path name
    if (!::GetVolumePathNameW(wzWindowsPath, wzVolumePath, MAX_PATH))
    {
        ExitWithLastError(hr, "Failed to get volume path name.");
    }

    // set value
    hr = BVariantSetString(pValue, wzVolumePath, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariablePrivileged(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    BOOL fPrivileged = FALSE;

    // check if process could run privileged.
    hr = OsCouldRunPrivileged(&fPrivileged);
    ExitOnFailure(hr, "Failed to check if process could run privileged.");

    // set value
    hr = BVariantSetNumeric(pValue, fPrivileged);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableRebootPending(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    BOOL fRebootPending = FALSE;
    BOOL fComInitialized = FALSE;

    // Do a best effort to ask WU if a reboot is required. If anything goes
    // wrong then let's pretend a reboot is not required.
    hr = ::CoInitialize(NULL);
    if (SUCCEEDED(hr) || RPC_E_CHANGED_MODE == hr)
    {
        fComInitialized = TRUE;

        hr = WuaRestartRequired(&fRebootPending);
        if (FAILED(hr))
        {
            fRebootPending = FALSE;
            hr = S_OK;
        }
    }

    hr = BVariantSetNumeric(pValue, fRebootPending);
    ExitOnFailure(hr, "Failed to set reboot pending variant value.");

LExit:
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return hr;
}

static HRESULT InitializeSystemLanguageID(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    LANGID langid = ::GetSystemDefaultLangID();

    hr = BVariantSetNumeric(pValue, langid);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeUserUILanguageID(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    LANGID langid = ::GetUserDefaultUILanguage();

    hr = BVariantSetNumeric(pValue, langid);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeUserLanguageID(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    UNREFERENCED_PARAMETER(dwpData);

    HRESULT hr = S_OK;
    LANGID langid = ::GetUserDefaultLangID();

    hr = BVariantSetNumeric(pValue, langid);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableString(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzValue = (LPCWSTR)dwpData;

    // set value
    hr = BVariantSetString(pValue, wzValue, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableNumeric(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = (LONGLONG)dwpData;

    // set value
    hr = BVariantSetNumeric(pValue, llValue);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableRegistryFolder(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    int nFolder = (int)dwpData;
    LPWSTR sczPath = NULL;

#if !defined(_WIN64)
    BOOL fIsWow64 = FALSE;

    ProcWow64(::GetCurrentProcess(), &fIsWow64);
    if (!fIsWow64) // on 32-bit machines, variables aren't set
    {
        ExitFunction();
    }
#endif

    hr = Get64bitFolderFromRegistry(nFolder, &sczPath);
    ExitOnFailure(hr, "Failed to get 64-bit folder.");

    // set value
    hr = BVariantSetString(pValue, sczPath, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    ReleaseStr(sczPath);

    return hr;
}

static HRESULT InitializeVariable6432Folder(
        __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    int nFolder = (int)dwpData;
    LPWSTR sczPath = NULL;

#if !defined(_WIN64)
    BOOL fIsWow64 = FALSE;

    // If 32-bit use shell-folder.
    ProcWow64(::GetCurrentProcess(), &fIsWow64);
    if (!fIsWow64)
    {
        hr = ShelGetFolder(&sczPath, nFolder);
        ExitOnRootFailure(hr, "Failed to get shell folder.");
    }
    else
#endif
    {
        hr = Get64bitFolderFromRegistry(nFolder, &sczPath);
        ExitOnFailure(hr, "Failed to get 64-bit folder.");
    }

    // set value
    hr = BVariantSetString(pValue, sczPath, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    ReleaseStr(sczPath);

    return hr;
}

// Get the date in the same format as Windows Installer.
static HRESULT InitializeVariableDate(
    __in DWORD_PTR /*dwpData*/,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    SYSTEMTIME systime = { };
    LPWSTR sczDate = NULL;
    int cchDate = 0;

    ::GetSystemTime(&systime);

    cchDate = ::GetDateFormatW(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &systime, NULL, NULL, cchDate);
    if (!cchDate)
    {
        ExitOnLastError(hr, "Failed to get the required buffer length for the Date.");
    }

    hr = StrAlloc(&sczDate, cchDate);
    ExitOnFailure(hr, "Failed to allocate the buffer for the Date.");

    if (!::GetDateFormatW(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &systime, NULL, sczDate, cchDate))
    {
        ExitOnLastError(hr, "Failed to get the Date.");
    }

    // set value
    hr = BVariantSetString(pValue, sczDate, cchDate);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    ReleaseStr(sczDate);

    return hr;
}

static HRESULT InitializeVariableInstallerName(
    __in DWORD_PTR /*dwpData*/,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;

    // set value
    hr = BVariantSetString(pValue, L"WiX Burn", 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT InitializeVariableInstallerVersion(
    __in DWORD_PTR /*dwpData*/,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczVersion = NULL;

    hr = StrAllocStringAnsi(&sczVersion, szVerMajorMinorBuild, 0, CP_ACP);
    ExitOnFailure(hr, "Failed to copy the engine version.");

    // set value
    hr = BVariantSetString(pValue, sczVersion, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    ReleaseStr(sczVersion);

    return hr;
}

static HRESULT InitializeVariableVersion(
    __in DWORD_PTR dwpData,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;

    // set value
    hr = BVariantSetVersion(pValue, static_cast<DWORD64>(dwpData));
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

// Get the current user the same as Windows Installer.
static HRESULT InitializeVariableLogonUser(
    __in DWORD_PTR /*dwpData*/,
    __inout BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    WCHAR wzUserName[UNLEN + 1];
    DWORD cchUserName = countof(wzUserName);

    if (!::GetUserNameW(wzUserName, &cchUserName))
    {
        ExitOnLastError(hr, "Failed to get the user name.");
    }

    // set value
    hr = BVariantSetString(pValue, wzUserName, 0);
    ExitOnFailure(hr, "Failed to set variant value.");

LExit:
    return hr;
}

static HRESULT Get64bitFolderFromRegistry(
    __in int nFolder,
    __deref_out_z LPWSTR* psczPath
    )
{
    HRESULT hr = S_OK;
    HKEY hkFolders = NULL;

    AssertSz(CSIDL_PROGRAM_FILES == nFolder || CSIDL_PROGRAM_FILES_COMMON == nFolder, "Unknown folder CSIDL.");
    LPCWSTR wzFolderValue = CSIDL_PROGRAM_FILES_COMMON == nFolder ? L"CommonFilesDir" : L"ProgramFilesDir";

    hr = RegOpen(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion", KEY_READ | KEY_WOW64_64KEY, &hkFolders);
    ExitOnFailure(hr, "Failed to open Windows folder key.");

    hr = RegReadString(hkFolders, wzFolderValue, psczPath);
    ExitOnFailure(hr, "Failed to read folder path for '%ls'.", wzFolderValue);

    hr = PathBackslashTerminate(psczPath);
    ExitOnFailure(hr, "Failed to ensure path was backslash terminated.");

LExit:
    ReleaseRegKey(hkFolders);

    return hr;
}
