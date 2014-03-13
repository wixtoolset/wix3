//-------------------------------------------------------------------------------------------------
// <copyright file="catalog.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif

// structs

typedef struct _BURN_CATALOG
{
    LPWSTR sczKey;
    LPWSTR sczPayload;

    // mutable members
    LPWSTR sczLocalFilePath; // location of extracted or downloaded copy
    HANDLE hFile;
} BURN_CATALOG;

typedef struct _BURN_CATALOGS
{
    BURN_CATALOG* rgCatalogs;
    DWORD cCatalogs;
} BURN_CATALOGS;

typedef struct _BURN_PAYLOADS BURN_PAYLOADS;


// functions

HRESULT CatalogsParseFromXml(
    __in BURN_CATALOGS* pCatalogs,
    __in IXMLDOMNode* pixnBundle
    );
HRESULT CatalogFindById(
    __in BURN_CATALOGS* pCatalogs,
    __in_z LPCWSTR wzId,
    __out BURN_CATALOG** ppCatalog
    );
HRESULT CatalogLoadFromPayload(
    __in BURN_CATALOGS* pCatalogs,
    __in BURN_PAYLOADS* pPayloads
    );
HRESULT CatalogElevatedUpdateCatalogFile(
    __in BURN_CATALOGS* pCatalogs,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzPath
    );
void CatalogUninitialize(
    __in BURN_CATALOGS* pCatalogs
    );

#if defined(__cplusplus)
}
#endif
