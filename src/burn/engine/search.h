//-------------------------------------------------------------------------------------------------
// <copyright file="search.h" company="Outercurve Foundation">
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

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif


// constants

enum BURN_SEARCH_TYPE
{
    BURN_SEARCH_TYPE_NONE,
    BURN_SEARCH_TYPE_DIRECTORY,
    BURN_SEARCH_TYPE_FILE,
    BURN_SEARCH_TYPE_REGISTRY,
    BURN_SEARCH_TYPE_MSI_COMPONENT,
    BURN_SEARCH_TYPE_MSI_PRODUCT,
    BURN_SEARCH_TYPE_MSI_FEATURE,
};

enum BURN_DIRECTORY_SEARCH_TYPE
{
    BURN_DIRECTORY_SEARCH_TYPE_NONE,
    BURN_DIRECTORY_SEARCH_TYPE_EXISTS,
    BURN_DIRECTORY_SEARCH_TYPE_PATH,
};

enum BURN_FILE_SEARCH_TYPE
{
    BURN_FILE_SEARCH_TYPE_NONE,
    BURN_FILE_SEARCH_TYPE_EXISTS,
    BURN_FILE_SEARCH_TYPE_VERSION,
    BURN_FILE_SEARCH_TYPE_PATH,
};

enum BURN_REGISTRY_SEARCH_TYPE
{
    BURN_REGISTRY_SEARCH_TYPE_NONE,
    BURN_REGISTRY_SEARCH_TYPE_EXISTS,
    BURN_REGISTRY_SEARCH_TYPE_VALUE,
};

enum BURN_MSI_COMPONENT_SEARCH_TYPE
{
    BURN_MSI_COMPONENT_SEARCH_TYPE_NONE,
    BURN_MSI_COMPONENT_SEARCH_TYPE_KEYPATH,
    BURN_MSI_COMPONENT_SEARCH_TYPE_STATE,
    BURN_MSI_COMPONENT_SEARCH_TYPE_DIRECTORY,
};

enum BURN_MSI_PRODUCT_SEARCH_TYPE
{
    BURN_MSI_PRODUCT_SEARCH_TYPE_NONE,
    BURN_MSI_PRODUCT_SEARCH_TYPE_VERSION,
    BURN_MSI_PRODUCT_SEARCH_TYPE_LANGUAGE,
    BURN_MSI_PRODUCT_SEARCH_TYPE_STATE,
    BURN_MSI_PRODUCT_SEARCH_TYPE_ASSIGNMENT,
};

enum BURN_MSI_PRODUCT_SEARCH_GUID_TYPE
{
    BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_NONE,
    BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_PRODUCTCODE,
    BURN_MSI_PRODUCT_SEARCH_GUID_TYPE_UPGRADECODE
};

enum BURN_MSI_FEATURE_SEARCH_TYPE
{
    BURN_MSI_FEATURE_SEARCH_TYPE_NONE,
    BURN_MSI_FEATURE_SEARCH_TYPE_STATE,
};


// structs

typedef struct _BURN_SEARCH
{
    LPWSTR sczKey;
    LPWSTR sczVariable;
    LPWSTR sczCondition;

    BURN_SEARCH_TYPE Type;
    union
    {
        struct
        {
            BURN_DIRECTORY_SEARCH_TYPE Type;
            LPWSTR sczPath;
        } DirectorySearch;
        struct
        {
            BURN_FILE_SEARCH_TYPE Type;
            LPWSTR sczPath;
        } FileSearch;
        struct
        {
            BURN_REGISTRY_SEARCH_TYPE Type;
            BURN_VARIANT_TYPE VariableType;
            HKEY hRoot;
            LPWSTR sczKey;
            LPWSTR sczValue;
            BOOL fWin64;
            BOOL fExpandEnvironment;
        } RegistrySearch;
        struct
        {
            BURN_MSI_COMPONENT_SEARCH_TYPE Type;
            LPWSTR sczProductCode;
            LPWSTR sczComponentId;
        } MsiComponentSearch;
        struct
        {
            BURN_MSI_PRODUCT_SEARCH_TYPE Type;
            BURN_MSI_PRODUCT_SEARCH_GUID_TYPE GuidType;
            LPWSTR sczGuid;
        } MsiProductSearch;
        struct
        {
            BURN_MSI_FEATURE_SEARCH_TYPE Type;
            LPWSTR sczProductCode;
            LPWSTR sczFeatureId;
        } MsiFeatureSearch;
    };
} BURN_SEARCH;

typedef struct _BURN_SEARCHES
{
    BURN_SEARCH* rgSearches;
    DWORD cSearches;
} BURN_SEARCHES;


// function declarations

HRESULT SearchesParseFromXml(
    __in BURN_SEARCHES* pSearches,
    __in IXMLDOMNode* pixnBundle
    );
HRESULT SearchesExecute(
    __in BURN_SEARCHES* pSearches,
    __in BURN_VARIABLES* pVariables
    );
void SearchesUninitialize(
    __in BURN_SEARCHES* pSearches
    );


#if defined(__cplusplus)
}
#endif
