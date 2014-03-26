//-------------------------------------------------------------------------------------------------
// <copyright file="search.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Search
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// internal function declarations

static HRESULT DirectorySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT DirectorySearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT FileSearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT FileSearchVersion(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT FileSearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT RegistrySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT RegistrySearchValue(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT MsiComponentSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT MsiProductSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT MsiFeatureSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    );


// function definitions

extern "C" HRESULT SearchesParseFromXml(
    __in BURN_SEARCHES* pSearches,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    BSTR bstrNodeName = NULL;
    LPWSTR scz = NULL;

    // select search nodes
    hr = XmlSelectNodes(pixnBundle, L"DirectorySearch|FileSearch|RegistrySearch|MsiComponentSearch|MsiProductSearch|MsiFeatureSearch", &pixnNodes);
    ExitOnFailure(hr, "Failed to select search nodes.");

    // get search node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get search node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for searches
    pSearches->rgSearches = (BURN_SEARCH*)MemAlloc(sizeof(BURN_SEARCH) * cNodes, TRUE);
    ExitOnNull(pSearches->rgSearches, hr, E_OUTOFMEMORY, "Failed to allocate memory for search structs.");

    pSearches->cSearches = cNodes;

    // parse search elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_SEARCH* pSearch = &pSearches->rgSearches[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, &bstrNodeName);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pSearch->sczKey);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @Variable
        hr = XmlGetAttributeEx(pixnNode, L"Variable", &pSearch->sczVariable);
        ExitOnFailure(hr, "Failed to get @Variable.");

        // @Condition
        hr = XmlGetAttributeEx(pixnNode, L"Condition", &pSearch->sczCondition);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Condition.");
        }

        // read type specific attributes
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"DirectorySearch", -1))
        {
            pSearch->Type = BURN_SEARCH_TYPE_DIRECTORY;

            // @Path
            hr = XmlGetAttributeEx(pixnNode, L"Path", &pSearch->DirectorySearch.sczPath);
            ExitOnFailure(hr, "Failed to get @Path.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"exists", -1))
            {
                pSearch->DirectorySearch.Type = BURN_DIRECTORY_SEARCH_TYPE_EXISTS;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"path", -1))
            {
                pSearch->DirectorySearch.Type = BURN_DIRECTORY_SEARCH_TYPE_PATH;
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"FileSearch", -1))
        {
            pSearch->Type = BURN_SEARCH_TYPE_FILE;

            // @Path
            hr = XmlGetAttributeEx(pixnNode, L"Path", &pSearch->FileSearch.sczPath);
            ExitOnFailure(hr, "Failed to get @Path.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"exists", -1))
            {
                pSearch->FileSearch.Type = BURN_FILE_SEARCH_TYPE_EXISTS;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"version", -1))
            {
                pSearch->FileSearch.Type = BURN_FILE_SEARCH_TYPE_VERSION;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"path", -1))
            {
                pSearch->FileSearch.Type = BURN_FILE_SEARCH_TYPE_PATH;
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"RegistrySearch", -1))
        {
            pSearch->Type = BURN_SEARCH_TYPE_REGISTRY;

            // @Root
            hr = XmlGetAttributeEx(pixnNode, L"Root", &scz);
            ExitOnFailure(hr, "Failed to get @Root.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"HKCR", -1))
            {
                pSearch->RegistrySearch.hRoot = HKEY_CLASSES_ROOT;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"HKCU", -1))
            {
                pSearch->RegistrySearch.hRoot = HKEY_CURRENT_USER;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"HKLM", -1))
            {
                pSearch->RegistrySearch.hRoot = HKEY_LOCAL_MACHINE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"HKU", -1))
            {
                pSearch->RegistrySearch.hRoot = HKEY_USERS;
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Invalid value for @Root: %ls", scz);
            }

            // @Key
            hr = XmlGetAttributeEx(pixnNode, L"Key", &pSearch->RegistrySearch.sczKey);
            ExitOnFailure(hr, "Failed to get Key attribute.");

            // @Value
            hr = XmlGetAttributeEx(pixnNode, L"Value", &pSearch->RegistrySearch.sczValue);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get Value attribute.");
            }

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            hr = XmlGetYesNoAttribute(pixnNode, L"Win64", &pSearch->RegistrySearch.fWin64);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get Win64 attribute.");
            }

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"exists", -1))
            {
                pSearch->RegistrySearch.Type = BURN_REGISTRY_SEARCH_TYPE_EXISTS;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"value", -1))
            {
                pSearch->RegistrySearch.Type = BURN_REGISTRY_SEARCH_TYPE_VALUE;

                // @ExpandEnvironment
                hr = XmlGetYesNoAttribute(pixnNode, L"ExpandEnvironment", &pSearch->RegistrySearch.fExpandEnvironment);
                if (E_NOTFOUND != hr)
                {
                    ExitOnFailure(hr, "Failed to get @ExpandEnvironment.");
                }

                // @VariableType
                hr = XmlGetAttributeEx(pixnNode, L"VariableType", &scz);
                ExitOnFailure(hr, "Failed to get @VariableType.");

                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"numeric", -1))
                {
                    pSearch->RegistrySearch.VariableType = BURN_VARIANT_TYPE_NUMERIC;
                }
                else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"string", -1))
                {
                    pSearch->RegistrySearch.VariableType = BURN_VARIANT_TYPE_STRING;
                }
                else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"version", -1))
                {
                    pSearch->RegistrySearch.VariableType = BURN_VARIANT_TYPE_VERSION;
                }
                else
                {
                    hr = E_INVALIDARG;
                    ExitOnFailure1(hr, "Invalid value for @VariableType: %ls", scz);
                }
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"MsiComponentSearch", -1))
        {
            pSearch->Type = BURN_SEARCH_TYPE_MSI_COMPONENT;

            // @ProductCode
            hr = XmlGetAttributeEx(pixnNode, L"ProductCode", &pSearch->MsiComponentSearch.sczProductCode);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @ProductCode.");
            }

            // @ComponentId
            hr = XmlGetAttributeEx(pixnNode, L"ComponentId", &pSearch->MsiComponentSearch.sczComponentId);
            ExitOnFailure(hr, "Failed to get @ComponentId.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"keyPath", -1))
            {
                pSearch->MsiComponentSearch.Type = BURN_MSI_COMPONENT_SEARCH_TYPE_KEYPATH;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"state", -1))
            {
                pSearch->MsiComponentSearch.Type = BURN_MSI_COMPONENT_SEARCH_TYPE_STATE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"directory", -1))
            {
                pSearch->MsiComponentSearch.Type = BURN_MSI_COMPONENT_SEARCH_TYPE_DIRECTORY;
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"MsiProductSearch", -1))
        {
            pSearch->Type = BURN_SEARCH_TYPE_MSI_PRODUCT;
            pSearch->MsiProductSearch.GuidType = BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_NONE;

            // @ProductCode (if we don't find a product code then look for an upgrade code)
            hr = XmlGetAttributeEx(pixnNode, L"ProductCode", &pSearch->MsiProductSearch.sczGuid);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @ProductCode.");
                pSearch->MsiProductSearch.GuidType = BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_PRODUCTCODE;
            }
            else
            {
                // @UpgradeCode
                hr = XmlGetAttributeEx(pixnNode, L"UpgradeCode", &pSearch->MsiProductSearch.sczGuid);
                if (E_NOTFOUND != hr)
                {
                    ExitOnFailure(hr, "Failed to get @UpgradeCode.");
                    pSearch->MsiProductSearch.GuidType = BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_UPGRADECODE;
                }
            }

            // make sure we found either a product or upgrade code
            if (BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_NONE == pSearch->MsiProductSearch.GuidType)
            {
                hr = E_NOTFOUND;
                ExitOnFailure(hr, "Failed to get @ProductCode or @UpgradeCode.");
            }

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"version", -1))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"language", -1))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"state", -1))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_STATE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"assignment", -1))
            {
                pSearch->MsiProductSearch.Type = BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT;
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Invalid value for @Type: %ls", scz);
            }
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"MsiFeatureSearch", -1))
        {
            pSearch->Type = BURN_SEARCH_TYPE_MSI_FEATURE;

            // @ProductCode
            hr = XmlGetAttributeEx(pixnNode, L"ProductCode", &pSearch->MsiFeatureSearch.sczProductCode);
            ExitOnFailure(hr, "Failed to get @ProductCode.");

            // @FeatureId
            hr = XmlGetAttributeEx(pixnNode, L"FeatureId", &pSearch->MsiFeatureSearch.sczFeatureId);
            ExitOnFailure(hr, "Failed to get @FeatureId.");

            // @Type
            hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
            ExitOnFailure(hr, "Failed to get @Type.");

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"state", -1))
            {
                pSearch->MsiFeatureSearch.Type = BURN_MSI_FEATURE_SEARCH_TYPE_STATE;
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Invalid value for @Type: %ls", scz);
            }
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure1(hr, "Unexpected element name: %ls", bstrNodeName);
        }

        // prepare next iteration
        ReleaseNullObject(pixnNode);
        ReleaseNullBSTR(bstrNodeName);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseBSTR(bstrNodeName);
    ReleaseStr(scz);
    return hr;
}

extern "C" HRESULT SearchesExecute(
    __in BURN_SEARCHES* pSearches,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BOOL f = FALSE;

    for (DWORD i = 0; i < pSearches->cSearches; ++i)
    {
        BURN_SEARCH* pSearch = &pSearches->rgSearches[i];

        // evaluate condition
        if (pSearch->sczCondition && *pSearch->sczCondition)
        {
            hr = ConditionEvaluate(pVariables, pSearch->sczCondition, &f);
            if (E_INVALIDDATA == hr)
            {
                TraceError2(hr, "Failed to parse search condition. Id = '%ls', Condition = '%ls'", pSearch->sczKey, pSearch->sczCondition);
                hr = S_OK;
                continue;
            }
            ExitOnFailure2(hr, "Failed to evaluate search condition. Id = '%ls', Condition = '%ls'", pSearch->sczKey, pSearch->sczCondition);

            if (!f)
            {
                continue; // condition evaluated to false, skip
            }
        }

        switch (pSearch->Type)
        {
        case BURN_SEARCH_TYPE_DIRECTORY:
            switch (pSearch->DirectorySearch.Type)
            {
            case BURN_DIRECTORY_SEARCH_TYPE_EXISTS:
                hr = DirectorySearchExists(pSearch, pVariables);
                break;
            case BURN_DIRECTORY_SEARCH_TYPE_PATH:
                hr = DirectorySearchPath(pSearch, pVariables);
                break;
            default:
                hr = E_UNEXPECTED;
            }
            break;
        case BURN_SEARCH_TYPE_FILE:
            switch (pSearch->FileSearch.Type)
            {
            case BURN_FILE_SEARCH_TYPE_EXISTS:
                hr = FileSearchExists(pSearch, pVariables);
                break;
            case BURN_FILE_SEARCH_TYPE_VERSION:
                hr = FileSearchVersion(pSearch, pVariables);
                break;
            case BURN_FILE_SEARCH_TYPE_PATH:
                hr = FileSearchPath(pSearch, pVariables);
                break;
            default:
                hr = E_UNEXPECTED;
            }
            break;
        case BURN_SEARCH_TYPE_REGISTRY:
            switch (pSearch->RegistrySearch.Type)
            {
            case BURN_REGISTRY_SEARCH_TYPE_EXISTS:
                hr = RegistrySearchExists(pSearch, pVariables);
                break;
            case BURN_REGISTRY_SEARCH_TYPE_VALUE:
                hr = RegistrySearchValue(pSearch, pVariables);
                break;
            default:
                hr = E_UNEXPECTED;
            }
            break;
        case BURN_SEARCH_TYPE_MSI_COMPONENT:
            hr = MsiComponentSearch(pSearch, pVariables);
            break;
        case BURN_SEARCH_TYPE_MSI_PRODUCT:
            hr = MsiProductSearch(pSearch, pVariables);
            break;
        case BURN_SEARCH_TYPE_MSI_FEATURE:
            hr = MsiFeatureSearch(pSearch, pVariables);
            break;
        default:
            hr = E_UNEXPECTED;
        }

        if (FAILED(hr))
        {
            TraceError1(hr, "Search failed. Id = '%ls'", pSearch->sczKey);
            continue;
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

extern "C" void SearchesUninitialize(
    __in BURN_SEARCHES* pSearches
    )
{
    if (pSearches->rgSearches)
    {
        for (DWORD i = 0; i < pSearches->cSearches; ++i)
        {
            BURN_SEARCH* pSearch = &pSearches->rgSearches[i];

            ReleaseStr(pSearch->sczKey);
            ReleaseStr(pSearch->sczVariable);
            ReleaseStr(pSearch->sczCondition);

            switch (pSearch->Type)
            {
            case BURN_SEARCH_TYPE_DIRECTORY:
                ReleaseStr(pSearch->DirectorySearch.sczPath);
                break;
            case BURN_SEARCH_TYPE_FILE:
                ReleaseStr(pSearch->FileSearch.sczPath);
                break;
            case BURN_SEARCH_TYPE_REGISTRY:
                ReleaseStr(pSearch->RegistrySearch.sczKey);
                ReleaseStr(pSearch->RegistrySearch.sczValue);
                break;
            case BURN_SEARCH_TYPE_MSI_COMPONENT:
                ReleaseStr(pSearch->MsiComponentSearch.sczProductCode);
                ReleaseStr(pSearch->MsiComponentSearch.sczComponentId);
                break;
            case BURN_SEARCH_TYPE_MSI_PRODUCT:
                ReleaseStr(pSearch->MsiProductSearch.sczGuid);
                break;
            case BURN_SEARCH_TYPE_MSI_FEATURE:
                ReleaseStr(pSearch->MsiFeatureSearch.sczProductCode);
                ReleaseStr(pSearch->MsiFeatureSearch.sczFeatureId);
                break;
            }
        }
        MemFree(pSearches->rgSearches);
    }
}


// internal function definitions

static HRESULT DirectorySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    BOOL fExists = FALSE;

    // format path
    hr = VariableFormatString(pVariables, pSearch->DirectorySearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
        if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
        {
            hr = S_OK; // didn't find file, fExists still is false.
        }
    }
    else if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY)
    {
        fExists = TRUE;
    }

    // else must have found a file.
    // what if there is a hidden variable in sczPath?
    ExitOnFailure2(hr, "Failed while searching directory search: %ls, for path: %ls", pSearch->sczKey, sczPath);

    // set variable
    hr = VariableSetNumeric(pVariables, pSearch->sczVariable, fExists, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    StrSecureZeroFreeString(sczPath);

    return hr;
}

static HRESULT DirectorySearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    // format path
    hr = VariableFormatString(pVariables, pSearch->DirectorySearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
    }
    else if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY)
    {
        hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE);
        ExitOnFailure(hr, "Failed to set directory search path variable.");
    }
    else // must have found a file.
    {
        hr = E_PATHNOTFOUND;
    }

    // what if there is a hidden variable in sczPath?
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        LogStringLine(REPORT_STANDARD, "Directory search: %ls, did not find path: %ls, reason: 0x%x", pSearch->sczKey, sczPath, hr);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure2(hr, "Failed while searching directory search: %ls, for path: %ls", pSearch->sczKey, sczPath);

LExit:
    StrSecureZeroFreeString(sczPath);

    return hr;
}

static HRESULT FileSearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczPath = NULL;
    BOOL fExists = FALSE;

    // format path
    hr = VariableFormatString(pVariables, pSearch->FileSearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    // find file
    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        er = ::GetLastError();
        if (ERROR_FILE_NOT_FOUND == er || ERROR_PATH_NOT_FOUND == er)
        {
            // what if there is a hidden variable in sczPath?
            LogStringLine(REPORT_STANDARD, "File search: %ls, did not find path: %ls", pSearch->sczKey, sczPath);
        }
        else
        {
            ExitOnWin32Error1(er, hr, "Failed get to file attributes. '%ls'", pSearch->DirectorySearch.sczPath);
        }
    }
    else if (FILE_ATTRIBUTE_DIRECTORY != (dwAttributes & FILE_ATTRIBUTE_DIRECTORY))
    {
        fExists = TRUE;
    }

    // set variable
    hr = VariableSetNumeric(pVariables, pSearch->sczVariable, fExists, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    StrSecureZeroFreeString(sczPath);
    return hr;
}

static HRESULT FileSearchVersion(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    ULARGE_INTEGER uliVersion = { };
    LPWSTR sczPath = NULL;

    // format path
    hr = VariableFormatString(pVariables, pSearch->FileSearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format path string.");

    // get file version
    hr = FileVersion(sczPath, &uliVersion.HighPart, &uliVersion.LowPart);
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        // what if there is a hidden variable in sczPath?
        LogStringLine(REPORT_STANDARD, "File search: %ls, did not find path: %ls", pSearch->sczKey, sczPath);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed get file version.");

    // set variable
    hr = VariableSetVersion(pVariables, pSearch->sczVariable, uliVersion.QuadPart, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    StrSecureZeroFreeString(sczPath);
    return hr;
}

static HRESULT FileSearchPath(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    // format path
    hr = VariableFormatString(pVariables, pSearch->FileSearch.sczPath, &sczPath, NULL);
    ExitOnFailure(hr, "Failed to format variable string.");

    DWORD dwAttributes = ::GetFileAttributesW(sczPath);
    if (INVALID_FILE_ATTRIBUTES == dwAttributes)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
    }
    else if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY) // found a directory.
    {
        hr = E_FILENOTFOUND;
    }
    else // found our file.
    {
        hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE);
        ExitOnFailure(hr, "Failed to set variable to file search path.");
    }

    // what if there is a hidden variable in sczPath?
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        LogStringLine(REPORT_STANDARD, "File search: %ls, did not find path: %ls", pSearch->sczKey, sczPath);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure2(hr, "Failed while searching file search: %ls, for path: %ls", pSearch->sczKey, sczPath);

LExit:
    StrSecureZeroFreeString(sczPath);

    return hr;
}

static HRESULT RegistrySearchExists(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczKey = NULL;
    LPWSTR sczValue = NULL;
    HKEY hKey = NULL;
    DWORD dwType = 0;
    BOOL fExists = FALSE;
    REGSAM samDesired = KEY_QUERY_VALUE;

    if (pSearch->RegistrySearch.fWin64)
    {
        samDesired = samDesired | KEY_WOW64_64KEY;
    }

    // format key string
    hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczKey, &sczKey, NULL);
    ExitOnFailure(hr, "Failed to format key string.");

    // open key
    hr = RegOpen(pSearch->RegistrySearch.hRoot, sczKey, samDesired, &hKey);
    if (SUCCEEDED(hr))
    {
        fExists = TRUE;
    }
    else if (E_FILENOTFOUND == hr)
    {
        // what if there is a hidden variable in sczKey?
        LogStringLine(REPORT_STANDARD, "Registry key not found. Key = '%ls'", sczKey);
        fExists = FALSE;
        hr = S_OK;
    }
    else
    {
        // what if there is a hidden variable in sczKey?
        ExitOnFailure1(hr, "Failed to open registry key. Key = '%ls'", sczKey);
    }

    if (fExists && pSearch->RegistrySearch.sczValue)
    {
        // format value string
        hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczValue, &sczValue, NULL);
        ExitOnFailure(hr, "Failed to format value string.");

        // query value
        er = ::RegQueryValueExW(hKey, sczValue, NULL, &dwType, NULL, NULL);
        switch (er)
        {
        case ERROR_SUCCESS:
            fExists = TRUE;
            break;
        case ERROR_FILE_NOT_FOUND:
            // what if there is a hidden variable in sczKey or sczValue?
            LogStringLine(REPORT_STANDARD, "Registry value not found. Key = '%ls', Value = '%ls'", sczKey, sczValue);
            fExists = FALSE;
            break;
        default:
            ExitOnWin32Error(er, hr, "Failed to query registry key value.");
        }
    }

    // set variable
    hr = VariableSetNumeric(pVariables, pSearch->sczVariable, fExists, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        // what if there is a hidden variable in sczKey?
        LogStringLine(REPORT_STANDARD, "RegistrySearchExists failed: ID '%ls', HRESULT 0x%x", sczKey, hr);
    }

    StrSecureZeroFreeString(sczKey);
    StrSecureZeroFreeString(sczValue);
    ReleaseRegKey(hKey);

    return hr;
}

static HRESULT RegistrySearchValue(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczKey = NULL;
    LPWSTR sczValue = NULL;
    HKEY hKey = NULL;
    DWORD dwType = 0;
    DWORD cbData = 0;
    LPBYTE pData = NULL;
    DWORD cch = 0;
    BURN_VARIANT value = { };
    REGSAM samDesired = KEY_QUERY_VALUE;

    if (pSearch->RegistrySearch.fWin64)
    {
        samDesired = samDesired | KEY_WOW64_64KEY;
    }

    // format key string
    hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczKey, &sczKey, NULL);
    ExitOnFailure(hr, "Failed to format key string.");

    // format value string
    if (pSearch->RegistrySearch.sczValue)
    {
        hr = VariableFormatString(pVariables, pSearch->RegistrySearch.sczValue, &sczValue, NULL);
        ExitOnFailure(hr, "Failed to format value string.");
    }

    // open key
    hr = RegOpen(pSearch->RegistrySearch.hRoot, sczKey, samDesired, &hKey);
    if (E_FILENOTFOUND == hr)
    {
        // what if there is a hidden variable in sczKey?
        LogStringLine(REPORT_STANDARD, "Registry key not found. Key = '%ls'", sczKey);
        hr = VariableSetVariant(pVariables, pSearch->sczVariable, &value, FALSE);
        ExitOnFailure(hr, "Failed to clear variable.");
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to open registry key.");

    // get value
    er = ::RegQueryValueExW(hKey, sczValue, NULL, &dwType, NULL, &cbData);
    if (ERROR_FILE_NOT_FOUND == er)
    {
        // what if there is a hidden variable in sczKey or sczValue?
        LogStringLine(REPORT_STANDARD, "Registry value not found. Key = '%ls', Value = '%ls'", sczKey, sczValue);
        hr = VariableSetVariant(pVariables, pSearch->sczVariable, &value, FALSE);
        ExitOnFailure(hr, "Failed to clear variable.");
        ExitFunction1(hr = S_OK);
    }
    ExitOnWin32Error(er, hr, "Failed to query registry key value size.");

    pData = (LPBYTE)MemAlloc(cbData + sizeof(WCHAR), TRUE); // + sizeof(WCHAR) here to ensure that we always have a null terminator for REG_SZ
    ExitOnNull(pData, hr, E_OUTOFMEMORY, "Failed to allocate memory registry value.");

    er = ::RegQueryValueExW(hKey, sczValue, NULL, &dwType, pData, &cbData);
    ExitOnWin32Error(er, hr, "Failed to query registry key value.");

    switch (dwType)
    {
    case REG_DWORD:
        if (sizeof(LONG) != cbData)
        {
            ExitFunction1(hr = E_UNEXPECTED);
        }
        hr = BVariantSetNumeric(&value, *((LONG*)pData));
        break;
    case REG_QWORD:
        if (sizeof(LONGLONG) != cbData)
        {
            ExitFunction1(hr = E_UNEXPECTED);
        }
        hr = BVariantSetNumeric(&value, *((LONGLONG*)pData));
        break;
    case REG_EXPAND_SZ:
        if (pSearch->RegistrySearch.fExpandEnvironment)
        {
            hr = StrAlloc(&value.sczValue, cbData);
            ExitOnFailure(hr, "Failed to allocate string buffer.");
            value.Type = BURN_VARIANT_TYPE_STRING;

            cch = ::ExpandEnvironmentStringsW((LPCWSTR)pData, value.sczValue, cbData);
            if (cch > cbData)
            {
                hr = StrAlloc(&value.sczValue, cch);
                ExitOnFailure(hr, "Failed to allocate string buffer.");

                if (cch != ::ExpandEnvironmentStringsW((LPCWSTR)pData, value.sczValue, cch))
                {
                    ExitWithLastError(hr, "Failed to get expand environment string.");
                }
            }
            break;
        }
        __fallthrough;
    case REG_SZ:
        hr = BVariantSetString(&value, (LPCWSTR)pData, 0);
        break;
    default:
        ExitOnFailure1(hr = E_NOTIMPL, "Unsupported registry key value type. Type = '%u'", dwType);
    }
    ExitOnFailure(hr, "Failed to read registry value.");

    // change value to requested type
    hr = BVariantChangeType(&value, pSearch->RegistrySearch.VariableType);
    ExitOnFailure(hr, "Failed to change value type.");

    // set variable
    hr = VariableSetVariant(pVariables, pSearch->sczVariable, &value, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        // what if there is a hidden variable in sczKey?
        LogStringLine(REPORT_STANDARD, "RegistrySearchValue failed: ID '%ls', HRESULT 0x%x", sczKey, hr);
    }

    StrSecureZeroFreeString(sczKey);
    StrSecureZeroFreeString(sczValue);
    ReleaseRegKey(hKey);
    ReleaseMem(pData);
    BVariantUninitialize(&value);

    return hr;
}

static HRESULT MsiComponentSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    INSTALLSTATE is = INSTALLSTATE_BROKEN;
    LPWSTR sczComponentId = NULL;
    LPWSTR sczProductCode = NULL;
    LPWSTR sczPath = NULL;

    // format component id string
    hr = VariableFormatString(pVariables, pSearch->MsiComponentSearch.sczComponentId, &sczComponentId, NULL);
    ExitOnFailure(hr, "Failed to format component id string.");

    if (pSearch->MsiComponentSearch.sczProductCode)
    {
        // format product code string
        hr = VariableFormatString(pVariables, pSearch->MsiComponentSearch.sczProductCode, &sczProductCode, NULL);
        ExitOnFailure(hr, "Failed to format product code string.");
    }

    if (sczProductCode)
    {
        hr = WiuGetComponentPath(sczProductCode, sczComponentId, &is, &sczPath);
    }
    else
    {
        hr = WiuLocateComponent(sczComponentId, &is, &sczPath);
    }

    if (INSTALLSTATE_SOURCEABSENT == is)
    {
        is = INSTALLSTATE_SOURCE;
    }
    else if (INSTALLSTATE_UNKNOWN == is || INSTALLSTATE_NOTUSED == is)
    {
        is = INSTALLSTATE_ABSENT;
    }
    else if (INSTALLSTATE_ABSENT != is && INSTALLSTATE_LOCAL != is && INSTALLSTATE_SOURCE != is)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "Failed to get component path: %d", is);
    }

    // set variable
    switch (pSearch->MsiComponentSearch.Type)
    {
    case BURN_MSI_COMPONENT_SEARCH_TYPE_KEYPATH:
        if (INSTALLSTATE_ABSENT == is || INSTALLSTATE_LOCAL == is || INSTALLSTATE_SOURCE == is)
        {
            hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE);
        }
        break;
    case BURN_MSI_COMPONENT_SEARCH_TYPE_STATE:
        hr = VariableSetNumeric(pVariables, pSearch->sczVariable, is, FALSE);
        break;
    case BURN_MSI_COMPONENT_SEARCH_TYPE_DIRECTORY:
        if (INSTALLSTATE_ABSENT == is || INSTALLSTATE_LOCAL == is || INSTALLSTATE_SOURCE == is)
        {
            // remove file part from path, if any
            LPWSTR wz = wcsrchr(sczPath, L'\\');
            if (wz)
            {
                wz[1] = L'\0';
            }

            hr = VariableSetString(pVariables, pSearch->sczVariable, sczPath, FALSE);
        }
        break;
    }
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        LogStringLine(REPORT_STANDARD, "MsiComponentSearch failed: ID '%ls', HRESULT 0x%x", pSearch->sczKey, hr);
    }

    StrSecureZeroFreeString(sczComponentId);
    StrSecureZeroFreeString(sczProductCode);
    ReleaseStr(sczPath);
    return hr;
}

static HRESULT MsiProductSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczGuid = NULL;
    LPCWSTR wzProperty = NULL;
    LPWSTR *rgsczRelatedProductCodes = NULL;
    DWORD dwRelatedProducts = 0;
    BURN_VARIANT_TYPE type = BURN_VARIANT_TYPE_NONE;
    BURN_VARIANT value = { };
    // we're not going to encrypt this value, so can access the value directly.

    switch (pSearch->MsiProductSearch.Type)
    {
    case BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION:
        wzProperty = INSTALLPROPERTY_VERSIONSTRING;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE:
        wzProperty = INSTALLPROPERTY_LANGUAGE;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_STATE:
        wzProperty = INSTALLPROPERTY_PRODUCTSTATE;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT:
        wzProperty = INSTALLPROPERTY_ASSIGNMENTTYPE;
        break;
    default:
        ExitOnFailure1(hr = E_NOTIMPL, "Unsupported product search type: %u", pSearch->MsiProductSearch.Type);
    }

    // format guid string
    hr = VariableFormatString(pVariables, pSearch->MsiProductSearch.sczGuid, &sczGuid, NULL);
    ExitOnFailure(hr, "Failed to format GUID string.");

    // get product info
    value.Type = BURN_VARIANT_TYPE_STRING;

    // if this is an upgrade code then get the product code of the highest versioned related product
    if (BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_UPGRADECODE == pSearch->MsiProductSearch.GuidType)
    {
        // WiuEnumRelatedProductCodes will log sczGuid on errors, what if there's a hidden variable in there?
        hr = WiuEnumRelatedProductCodes(sczGuid, &rgsczRelatedProductCodes, &dwRelatedProducts, TRUE);
        ExitOnFailure(hr, "Failed to enumerate related products for upgrade code.");

        // if we actually found a related product then use its upgrade code for the rest of the search
        if (1 == dwRelatedProducts)
        {
            hr = StrAllocateString(&sczGuid, rgsczRelatedProductCodes[0], 0, TRUE);
            ExitOnFailure(hr, "Failed to copy upgrade code.");
        }
        else
        {
            // set this here so we have a way of knowing that we don't need to bother
            // querying for the product information below
            hr = HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT);
        }
    }

    if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) != hr)
    {
        hr = WiuGetProductInfo(sczGuid, wzProperty, &value.sczValue);
        if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) == hr)
        {
            // product state is available only through MsiGetProductInfoEx
            // what if there is a hidden variable in sczGuid?
            LogStringLine(REPORT_VERBOSE, "Trying per-machine extended info for property '%ls' for product: %ls", wzProperty, sczGuid);
            hr = WiuGetProductInfoEx(sczGuid, NULL, MSIINSTALLCONTEXT_MACHINE, wzProperty, &value.sczValue);

            // if not in per-machine context, try per-user (unmanaged)
            if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) == hr)
            {
                // what if there is a hidden variable in sczGuid?
                LogStringLine(REPORT_STANDARD, "Trying per-user extended info for property '%ls' for product: %ls", wzProperty, sczGuid);
                hr = WiuGetProductInfoEx(sczGuid, NULL, MSIINSTALLCONTEXT_USERUNMANAGED, wzProperty, &value.sczValue);
            }
        }
    }

    if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) == hr)
    {
        // what if there is a hidden variable in sczGuid?
        LogStringLine(REPORT_STANDARD, "Product or related product not found: %ls", sczGuid);

        // set value to indicate absent
        switch (pSearch->MsiProductSearch.Type)
        {
        case BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT: __fallthrough;
        case BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION:
            value.Type = BURN_VARIANT_TYPE_NUMERIC;
            value.llValue = 0;
            break;
        case BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE:
            // is supposed to remain empty
            break;
        case BURN_MSI_PRODUCT_SEARCH_TYPE_STATE:
            value.Type = BURN_VARIANT_TYPE_NUMERIC;
            value.llValue = INSTALLSTATE_ABSENT;
            break;
        }

        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get product info.");

    // change value type
    switch (pSearch->MsiProductSearch.Type)
    {
    case BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION:
        type = BURN_VARIANT_TYPE_VERSION;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE:
        type = BURN_VARIANT_TYPE_STRING;
        break;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_STATE: __fallthrough;
    case BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT:
        type = BURN_VARIANT_TYPE_NUMERIC;
        break;
    }
    hr = BVariantChangeType(&value, type);
    ExitOnFailure(hr, "Failed to change value type.");

    // set variable
    hr = VariableSetVariant(pVariables, pSearch->sczVariable, &value, FALSE);
    ExitOnFailure(hr, "Failed to set variable.");

LExit:
    if (FAILED(hr))
    {
        LogStringLine(REPORT_STANDARD, "MsiProductSearch failed: ID '%ls', HRESULT 0x%x", pSearch->sczKey, hr);
    }

    StrSecureZeroFreeString(sczGuid);
    ReleaseStrArray(rgsczRelatedProductCodes, dwRelatedProducts);
    BVariantUninitialize(&value);

    return hr;
}

static HRESULT MsiFeatureSearch(
    __in BURN_SEARCH* pSearch,
    __in BURN_VARIABLES* /*pVariables*/
    )
{
    HRESULT hr = E_NOTIMPL;

//LExit:
    if (FAILED(hr))
    {
        LogStringLine(REPORT_STANDARD, "MsiFeatureSearch failed: ID '%ls', HRESULT 0x%x", pSearch->sczKey, hr);
    }

    return hr;
}
