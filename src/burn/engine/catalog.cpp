//-------------------------------------------------------------------------------------------------
// <copyright file="catalog.cpp" company="Outercurve Foundation">
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

#include "precomp.h"


// function definitions

extern "C" HRESULT CatalogsParseFromXml(
    __in BURN_CATALOGS* pCatalogs,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select catalog nodes
    hr = XmlSelectNodes(pixnBundle, L"Catalog", &pixnNodes);
    ExitOnFailure(hr, "Failed to select catalog nodes.");

    // get catalog node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get payload node count.");
    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for catalogs
    pCatalogs->rgCatalogs = (BURN_CATALOG*)MemAlloc(sizeof(BURN_CATALOG) * cNodes, TRUE);
    ExitOnNull(pCatalogs->rgCatalogs, hr, E_OUTOFMEMORY, "Failed to allocate memory for payload structs.");

    pCatalogs->cCatalogs = cNodes;

    // parse catalog elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_CATALOG* pCatalog = &pCatalogs->rgCatalogs[i];
        pCatalog->hFile = INVALID_HANDLE_VALUE;

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pCatalog->sczKey);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @Payload
        hr = XmlGetAttributeEx(pixnNode, L"Payload", &pCatalog->sczPayload);
        ExitOnFailure(hr, "Failed to get @Payload.");

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" HRESULT CatalogFindById(
    __in BURN_CATALOGS* pCatalogs,
    __in_z LPCWSTR wzId,
    __out BURN_CATALOG** ppCatalog
    )
{
    HRESULT hr = S_OK;
    BURN_CATALOG* pCatalog = NULL;

    for (DWORD i = 0; i < pCatalogs->cCatalogs; ++i)
    {
        pCatalog = &pCatalogs->rgCatalogs[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pCatalog->sczKey, -1, wzId, -1))
        {
            *ppCatalog = pCatalog;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

extern "C" HRESULT CatalogLoadFromPayload(
    __in BURN_CATALOGS* pCatalogs,
    __in BURN_PAYLOADS* pPayloads
    )
{
    HRESULT hr = S_OK;
    BURN_CATALOG* pCatalog = NULL;
    BURN_PAYLOAD* pPayload = NULL;

    // go through each catalog file
    for (DWORD i = 0; i < pCatalogs->cCatalogs; i++)
    {
        pCatalog = &pCatalogs->rgCatalogs[i];

        // get the payload for this catalog file
        hr = PayloadFindById(pPayloads, pCatalog->sczPayload, &pPayload);
        ExitOnFailure(hr, "Failed to find payload for catalog file.");

        // Get the local file name
        hr = StrAllocString(&pCatalog->sczLocalFilePath, pPayload->sczLocalFilePath, 0);
        ExitOnFailure(hr, "Failed to get catalog local file path");

        // Get a handle to the file
        pCatalog->hFile = ::CreateFileW(pCatalog->sczLocalFilePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        if (INVALID_HANDLE_VALUE == pCatalog->hFile)
        {
            ExitWithLastError1(hr, "Failed to open catalog in working path: %ls", pCatalog->sczLocalFilePath);
        }

        // Verify the catalog file
        hr = CacheVerifyPayloadSignature(pPayload, pCatalog->sczLocalFilePath, pCatalog->hFile);
        ExitOnFailure1(hr, "Failed to verify catalog signature: %ls", pCatalog->sczLocalFilePath);
    }

LExit:
    return hr;
}

extern "C" HRESULT CatalogElevatedUpdateCatalogFile(
    __in BURN_CATALOGS* pCatalogs,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    BURN_CATALOG* pCatalog = NULL;

    // Find the catalog
    hr = CatalogFindById(pCatalogs, wzId, &pCatalog);
    ExitOnFailure(hr, "Failed to locate catalog information.");

    if (NULL == pCatalog->sczLocalFilePath)
    {
        hr = StrAllocString(&pCatalog->sczLocalFilePath, wzPath, 0);
        ExitOnFailure(hr, "Failed to allocated catalog path.");

        // Get a handle to the file
        pCatalog->hFile = ::CreateFileW(pCatalog->sczLocalFilePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        if (INVALID_HANDLE_VALUE == pCatalog->hFile)
        {
            ExitWithLastError1(hr, "Failed to open catalog in working path: %ls", pCatalog->sczLocalFilePath);
        }
    }

LExit:
    return hr;
}

extern "C" void CatalogUninitialize(
    __in BURN_CATALOGS* pCatalogs
    )
{
    if (pCatalogs->rgCatalogs)
    {
        for (DWORD i = 0; i < pCatalogs->cCatalogs; ++i)
        {
            BURN_CATALOG* pCatalog = &pCatalogs->rgCatalogs[i];

            ReleaseHandle(pCatalog->hFile);
            ReleaseStr(pCatalog->sczKey);
            ReleaseStr(pCatalog->sczLocalFilePath);
            ReleaseStr(pCatalog->sczPayload);
        }
        MemFree(pCatalogs->rgCatalogs);
    }

    // clear struct
    memset(pCatalogs, 0, sizeof(BURN_CATALOGS));
}
