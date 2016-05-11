// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT ParseAtomDocument(
    __in IXMLDOMDocument *pixd,
    __out ATOM_FEED **ppFeed
    );
static HRESULT ParseAtomFeed(
    __in IXMLDOMNode *pixnFeed,
    __out ATOM_FEED **ppFeed
    );
static HRESULT ParseAtomAuthor(
    __in IXMLDOMNode* pixnAuthor,
    __in ATOM_AUTHOR* pAuthor
    );
static HRESULT ParseAtomCategory(
    __in IXMLDOMNode* pixnCategory,
    __in ATOM_CATEGORY* pCategory
    );
static HRESULT ParseAtomEntry(
    __in IXMLDOMNode* pixnEntry,
    __in ATOM_ENTRY* pEntry
    );
static HRESULT ParseAtomLink(
    __in IXMLDOMNode* pixnLink,
    __in ATOM_LINK* pLink
    );
static HRESULT ParseAtomUnknownElement(
    __in IXMLDOMNode *pNode,
    __inout ATOM_UNKNOWN_ELEMENT** ppUnknownElement
    );
static HRESULT ParseAtomUnknownAttribute(
    __in IXMLDOMNode *pNode,
    __inout ATOM_UNKNOWN_ATTRIBUTE** ppUnknownAttribute
    );
static HRESULT AssignDateTime(
    __in FILETIME* pft,
    __in IXMLDOMNode* pNode
    );
static HRESULT AssignString(
    __out_z LPWSTR* pwzValue,
    __in IXMLDOMNode* pNode
    );
static void FreeAtomAuthor(
    __in_opt ATOM_AUTHOR* pAuthor
    );
static void FreeAtomContent(
    __in_opt ATOM_CONTENT* pContent
    );
static void FreeAtomCategory(
    __in_opt ATOM_CATEGORY* pCategory
    );
static void FreeAtomEntry(
    __in_opt ATOM_ENTRY* pEntry
    );
static void FreeAtomLink(
    __in_opt ATOM_LINK* pLink
    );
static void FreeAtomUnknownElementList(
    __in_opt ATOM_UNKNOWN_ELEMENT* pUnknownElement
    );
static void FreeAtomUnknownAttributeList(
    __in_opt ATOM_UNKNOWN_ATTRIBUTE* pUnknownAttribute
    );

template<class T> static HRESULT AllocateAtomType(
    __in IXMLDOMNode* pixnParent,
    __in LPCWSTR wzT,
    __out T** pprgT,
    __out DWORD* pcT
    );


/********************************************************************
 AtomInitialize - Initialize ATOM utilities.

*********************************************************************/
extern "C" HRESULT DAPI AtomInitialize()
{
    return XmlInitialize();
}


/********************************************************************
 AtomUninitialize - Uninitialize ATOM utilities.

*********************************************************************/
extern "C" void DAPI AtomUninitialize()
{
    XmlUninitialize();
}


/********************************************************************
 AtomParseFromString - parses out an ATOM feed from a string.

*********************************************************************/
extern "C" HRESULT DAPI AtomParseFromString(
    __in LPCWSTR wzAtomString,
    __out ATOM_FEED **ppFeed
    )
{
    Assert(wzAtomString);
    Assert(ppFeed);

    HRESULT hr = S_OK;
    ATOM_FEED *pNewFeed = NULL;
    IXMLDOMDocument *pixdAtom = NULL;

    hr = XmlLoadDocument(wzAtomString, &pixdAtom);
    ExitOnFailure(hr, "Failed to load ATOM string as XML document.");

    hr = ParseAtomDocument(pixdAtom, &pNewFeed);
    ExitOnFailure(hr, "Failed to parse ATOM document.");

    *ppFeed = pNewFeed;
    pNewFeed = NULL;

LExit:
    ReleaseAtomFeed(pNewFeed);
    ReleaseObject(pixdAtom);

    return hr;
}


/********************************************************************
 AtomParseFromFile - parses out an ATOM feed from a file path.

*********************************************************************/
extern "C" HRESULT DAPI AtomParseFromFile(
    __in LPCWSTR wzAtomFile,
    __out ATOM_FEED **ppFeed
    )
{
    Assert(wzAtomFile);
    Assert(ppFeed);

    HRESULT hr = S_OK;
    ATOM_FEED *pNewFeed = NULL;
    IXMLDOMDocument *pixdAtom = NULL;

    hr = XmlLoadDocumentFromFile(wzAtomFile, &pixdAtom);
    ExitOnFailure(hr, "Failed to load ATOM string as XML document.");

    hr = ParseAtomDocument(pixdAtom, &pNewFeed);
    ExitOnFailure(hr, "Failed to parse ATOM document.");

    *ppFeed = pNewFeed;
    pNewFeed = NULL;

LExit:
    ReleaseAtomFeed(pNewFeed);
    ReleaseObject(pixdAtom);

    return hr;
}


/********************************************************************
 AtomParseFromDocument - parses out an ATOM feed from an XML document.

*********************************************************************/
extern "C" HRESULT DAPI AtomParseFromDocument(
    __in IXMLDOMDocument* pixdDocument,
    __out ATOM_FEED **ppFeed
    )
{
    Assert(pixdDocument);
    Assert(ppFeed);

    HRESULT hr = S_OK;
    ATOM_FEED *pNewFeed = NULL;

    hr = ParseAtomDocument(pixdDocument, &pNewFeed);
    ExitOnFailure(hr, "Failed to parse ATOM document.");

    *ppFeed = pNewFeed;
    pNewFeed = NULL;

LExit:
    ReleaseAtomFeed(pNewFeed);

    return hr;
}


/********************************************************************
 AtomFreeFeed - parses out an ATOM feed from a string.

*********************************************************************/
extern "C" void DAPI AtomFreeFeed(
    __in_xcount(pFeed->cItems) ATOM_FEED *pFeed
    )
{
    if (pFeed)
    {
        FreeAtomUnknownElementList(pFeed->pUnknownElements);
        ReleaseObject(pFeed->pixn);

        for (DWORD i = 0; i < pFeed->cLinks; ++i)
        {
            FreeAtomLink(pFeed->rgLinks + i);
        }
        ReleaseMem(pFeed->rgLinks);

        for (DWORD i = 0; i < pFeed->cEntries; ++i)
        {
            FreeAtomEntry(pFeed->rgEntries + i);
        }
        ReleaseMem(pFeed->rgEntries);

        for (DWORD i = 0; i < pFeed->cCategories; ++i)
        {
            FreeAtomCategory(pFeed->rgCategories + i);
        }
        ReleaseMem(pFeed->rgCategories);

        for (DWORD i = 0; i < pFeed->cAuthors; ++i)
        {
            FreeAtomAuthor(pFeed->rgAuthors + i);
        }
        ReleaseMem(pFeed->rgAuthors);

        ReleaseStr(pFeed->wzGenerator);
        ReleaseStr(pFeed->wzIcon);
        ReleaseStr(pFeed->wzId);
        ReleaseStr(pFeed->wzLogo);
        ReleaseStr(pFeed->wzSubtitle);
        ReleaseStr(pFeed->wzTitle);

        MemFree(pFeed);
    }
}


/********************************************************************
 ParseAtomDocument - parses out an ATOM feed from a loaded XML DOM document.

*********************************************************************/
static HRESULT ParseAtomDocument(
    __in IXMLDOMDocument *pixd,
    __out ATOM_FEED **ppFeed
    )
{
    Assert(pixd);
    Assert(ppFeed);

    HRESULT hr = S_OK;
    IXMLDOMElement *pFeedElement = NULL;

    ATOM_FEED *pNewFeed = NULL;

    //
    // Get the document element and start processing feeds.
    //
    hr = pixd->get_documentElement(&pFeedElement);
    ExitOnFailure(hr, "failed get_documentElement in ParseAtomDocument");

    hr = ParseAtomFeed(pFeedElement, &pNewFeed);
    ExitOnFailure(hr, "Failed to parse ATOM feed.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

    *ppFeed = pNewFeed;
    pNewFeed = NULL;

LExit:
    ReleaseObject(pFeedElement);

    ReleaseAtomFeed(pNewFeed);

    return hr;
}


/********************************************************************
 ParseAtomFeed - parses out an ATOM feed from a loaded XML DOM element.

*********************************************************************/
static HRESULT ParseAtomFeed(
    __in IXMLDOMNode *pixnFeed,
    __out ATOM_FEED **ppFeed
    )
{
    Assert(pixnFeed);
    Assert(ppFeed);

    HRESULT hr = S_OK;
    IXMLDOMNodeList *pNodeList = NULL;

    ATOM_FEED *pNewFeed = NULL;
    DWORD cAuthors = 0;
    DWORD cCategories = 0;
    DWORD cEntries = 0;
    DWORD cLinks = 0;

    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;

    // First, allocate the new feed and all the possible sub elements.
    pNewFeed = (ATOM_FEED*)MemAlloc(sizeof(ATOM_FEED), TRUE);
    ExitOnNull(pNewFeed, hr, E_OUTOFMEMORY, "Failed to allocate ATOM feed structure.");

    pNewFeed->pixn = pixnFeed;
    pNewFeed->pixn->AddRef();

    hr = AllocateAtomType<ATOM_AUTHOR>(pixnFeed, L"author", &pNewFeed->rgAuthors, &pNewFeed->cAuthors);
    ExitOnFailure(hr, "Failed to allocate ATOM feed authors.");

    hr = AllocateAtomType<ATOM_CATEGORY>(pixnFeed, L"category", &pNewFeed->rgCategories, &pNewFeed->cCategories);
    ExitOnFailure(hr, "Failed to allocate ATOM feed categories.");

    hr = AllocateAtomType<ATOM_ENTRY>(pixnFeed, L"entry", &pNewFeed->rgEntries, &pNewFeed->cEntries);
    ExitOnFailure(hr, "Failed to allocate ATOM feed entries.");

    hr = AllocateAtomType<ATOM_LINK>(pixnFeed, L"link", &pNewFeed->rgLinks, &pNewFeed->cLinks);
    ExitOnFailure(hr, "Failed to allocate ATOM feed links.");

    // Second, process the elements under a feed.
    hr = pixnFeed->get_childNodes(&pNodeList);
    ExitOnFailure(hr, "Failed to get child nodes of ATOM feed element.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"generator", -1))
        {
            hr = AssignString(&pNewFeed->wzGenerator, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM feed generator.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"icon", -1))
        {
            hr = AssignString(&pNewFeed->wzIcon, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM feed icon.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"id", -1))
        {
            hr = AssignString(&pNewFeed->wzId, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM feed id.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"logo", -1))
        {
            hr = AssignString(&pNewFeed->wzLogo, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM feed logo.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"subtitle", -1))
        {
            hr = AssignString(&pNewFeed->wzSubtitle, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM feed subtitle.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"title", -1))
        {
            hr = AssignString(&pNewFeed->wzTitle, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM feed title.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"updated", -1))
        {
            hr = AssignDateTime(&pNewFeed->ftUpdated, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM feed updated.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"author", -1))
        {
            hr = ParseAtomAuthor(pNode, &pNewFeed->rgAuthors[cAuthors]);
            ExitOnFailure(hr, "Failed to parse ATOM author.");

            ++cAuthors;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"category", -1))
        {
            hr = ParseAtomCategory(pNode, &pNewFeed->rgCategories[cCategories]);
            ExitOnFailure(hr, "Failed to parse ATOM category.");

            ++cCategories;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"entry", -1))
        {
            hr = ParseAtomEntry(pNode, &pNewFeed->rgEntries[cEntries]);
            ExitOnFailure(hr, "Failed to parse ATOM entry.");

            ++cEntries;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"link", -1))
        {
            hr = ParseAtomLink(pNode, &pNewFeed->rgLinks[cLinks]);
            ExitOnFailure(hr, "Failed to parse ATOM link.");

            ++cLinks;
        }
        else
        {
            hr = ParseAtomUnknownElement(pNode, &pNewFeed->pUnknownElements);
            ExitOnFailure1(hr, "Failed to parse unknown ATOM feed element: %ls", bstrNodeName);
        }

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }

    if (!pNewFeed->wzId || !*pNewFeed->wzId)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Failed to find required feed/id element.");
    }
    else if (!pNewFeed->wzTitle || !*pNewFeed->wzTitle)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Failed to find required feed/title element.");
    }
    else if (0 == pNewFeed->ftUpdated.dwHighDateTime && 0 == pNewFeed->ftUpdated.dwLowDateTime)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Failed to find required feed/updated element.");
    }

    *ppFeed = pNewFeed;
    pNewFeed = NULL;

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    ReleaseAtomFeed(pNewFeed);

    return hr;
}


/********************************************************************
 AllocateAtomType - allocates enough space for all of the ATOM elements
                    of a particular type under a particular node.

*********************************************************************/
template<class T> static HRESULT AllocateAtomType(
    __in IXMLDOMNode* pixnParent,
    __in LPCWSTR wzT,
    __out T** pprgT,
    __out DWORD* pcT
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList *pNodeList = NULL;

    long cT = 0;
    T* prgT = NULL;

    hr = XmlSelectNodes(pixnParent, wzT, &pNodeList);
    ExitOnFailure1(hr, "Failed to select all ATOM %ls.", wzT);

    if (S_OK == hr)
    {
        hr = pNodeList->get_length(&cT);
        ExitOnFailure1(hr, "Failed to count the number of ATOM %ls.", wzT);

        if (cT == 0)
        {
            ExitFunction();
        }

        prgT = static_cast<T*>(MemAlloc(sizeof(T) * cT, TRUE));
        ExitOnNull(prgT, hr, E_OUTOFMEMORY, "Failed to allocate ATOM.");

        *pcT = cT;
        *pprgT = prgT;
        prgT = NULL;
    }
    else
    {
        *pprgT = NULL;
        *pcT = 0;
    }

LExit:
    ReleaseMem(prgT);
    ReleaseObject(pNodeList);

    return hr;
}


/********************************************************************
 ParseAtomAuthor - parses out an ATOM author from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseAtomAuthor(
    __in IXMLDOMNode* pixnAuthor,
    __in ATOM_AUTHOR* pAuthor
    )
{
    HRESULT hr = S_OK;

    IXMLDOMNodeList *pNodeList = NULL;
    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;

    hr = pixnAuthor->get_childNodes(&pNodeList);
    ExitOnFailure(hr, "Failed to get child nodes of ATOM author element.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"name", -1))
        {
            hr = AssignString(&pAuthor->wzName, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM author name.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"email", -1))
        {
            hr = AssignString(&pAuthor->wzEmail, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM author email.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"uri", -1))
        {
            hr = AssignString(&pAuthor->wzUrl, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM author uri.");
        }

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM author elements.");

    hr = S_OK;

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    return hr;
}


/********************************************************************
 ParseAtomCategory - parses out an ATOM category from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseAtomCategory(
    __in IXMLDOMNode* pixnCategory,
    __in ATOM_CATEGORY* pCategory
    )
{
    HRESULT hr = S_OK;

    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNodeList *pNodeList = NULL;
    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;

    // Process attributes first.
    hr = pixnCategory->get_attributes(&pixnnmAttributes);
    ExitOnFailure(hr, "Failed get attributes on ATOM unknown element.");

    while (S_OK == (hr = XmlNextAttribute(pixnnmAttributes, &pNode, &bstrNodeName)))
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"label", -1))
        {
            hr = AssignString(&pCategory->wzLabel, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM category label.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"scheme", -1))
        {
            hr = AssignString(&pCategory->wzScheme, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM category scheme.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"term", -1))
        {
            hr = AssignString(&pCategory->wzTerm, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM category term.");
        }

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM category attributes.");

    // Process elements second.
    hr = pixnCategory->get_childNodes(&pNodeList);
    ExitOnFailure(hr, "Failed to get child nodes of ATOM category element.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        hr = ParseAtomUnknownElement(pNode, &pCategory->pUnknownElements);
        ExitOnFailure1(hr, "Failed to parse unknown ATOM category element: %ls", bstrNodeName);

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM category elements.");

    hr = S_OK;

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);
    ReleaseObject(pixnnmAttributes);

    return hr;
}


/********************************************************************
 ParseAtomContent - parses out an ATOM content from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseAtomContent(
    __in IXMLDOMNode* pixnContent,
    __in ATOM_CONTENT* pContent
    )
{
    HRESULT hr = S_OK;

    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNodeList *pNodeList = NULL;
    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;

    // Process attributes first.
    hr = pixnContent->get_attributes(&pixnnmAttributes);
    ExitOnFailure(hr, "Failed get attributes on ATOM unknown element.");

    while (S_OK == (hr = XmlNextAttribute(pixnnmAttributes, &pNode, &bstrNodeName)))
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"type", -1))
        {
            hr = AssignString(&pContent->wzType, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM content type.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"url", -1))
        {
            hr = AssignString(&pContent->wzUrl, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM content scheme.");
        }

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM content attributes.");

    // Process elements second.
    hr = pixnContent->get_childNodes(&pNodeList);
    ExitOnFailure(hr, "Failed to get child nodes of ATOM content element.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        hr = ParseAtomUnknownElement(pNode, &pContent->pUnknownElements);
        ExitOnFailure1(hr, "Failed to parse unknown ATOM content element: %ls", bstrNodeName);

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM content elements.");

    hr = AssignString(&pContent->wzValue, pixnContent);
    ExitOnFailure(hr, "Failed to allocate ATOM content value.");

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);
    ReleaseObject(pixnnmAttributes);

    return hr;
}


/********************************************************************
 ParseAtomEntry - parses out an ATOM entry from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseAtomEntry(
    __in IXMLDOMNode* pixnEntry,
    __in ATOM_ENTRY* pEntry
    )
{
    HRESULT hr = S_OK;

    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNodeList *pNodeList = NULL;
    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;

    DWORD cAuthors = 0;
    DWORD cCategories = 0;
    DWORD cLinks = 0;

    pEntry->pixn = pixnEntry;
    pEntry->pixn->AddRef();

    // First, allocate all the possible sub elements.
    hr = AllocateAtomType<ATOM_AUTHOR>(pixnEntry, L"author", &pEntry->rgAuthors, &pEntry->cAuthors);
    ExitOnFailure(hr, "Failed to allocate ATOM entry authors.");

    hr = AllocateAtomType<ATOM_CATEGORY>(pixnEntry, L"category", &pEntry->rgCategories, &pEntry->cCategories);
    ExitOnFailure(hr, "Failed to allocate ATOM entry categories.");

    hr = AllocateAtomType<ATOM_LINK>(pixnEntry, L"link", &pEntry->rgLinks, &pEntry->cLinks);
    ExitOnFailure(hr, "Failed to allocate ATOM entry links.");

    // Second, process elements.
    hr = pixnEntry->get_childNodes(&pNodeList);
    ExitOnFailure(hr, "Failed to get child nodes of ATOM entry element.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"id", -1))
        {
            hr = AssignString(&pEntry->wzId, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM entry id.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"summary", -1))
        {
            hr = AssignString(&pEntry->wzSummary, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM entry summary.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"title", -1))
        {
            hr = AssignString(&pEntry->wzTitle, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM entry title.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"published", -1))
        {
            hr = AssignDateTime(&pEntry->ftPublished, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM entry published.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"updated", -1))
        {
            hr = AssignDateTime(&pEntry->ftUpdated, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM entry updated.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"author", -1))
        {
            hr = ParseAtomAuthor(pNode, &pEntry->rgAuthors[cAuthors]);
            ExitOnFailure(hr, "Failed to parse ATOM entry author.");

            ++cAuthors;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"category", -1))
        {
            hr = ParseAtomCategory(pNode, &pEntry->rgCategories[cCategories]);
            ExitOnFailure(hr, "Failed to parse ATOM entry category.");

            ++cCategories;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"content", -1))
        {
            if (NULL != pEntry->pContent)
            {
                hr = E_UNEXPECTED;
                ExitOnFailure(hr, "Cannot have two content elements in ATOM entry.");
            }

            pEntry->pContent = static_cast<ATOM_CONTENT*>(MemAlloc(sizeof(ATOM_CONTENT), TRUE));
            ExitOnNull(pEntry->pContent, hr, E_OUTOFMEMORY, "Failed to allocate ATOM entry content.");

            hr = ParseAtomContent(pNode, pEntry->pContent);
            ExitOnFailure(hr, "Failed to parse ATOM entry content.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"link", -1))
        {
            hr = ParseAtomLink(pNode, &pEntry->rgLinks[cLinks]);
            ExitOnFailure(hr, "Failed to parse ATOM entry link.");

            ++cLinks;
        }
        else
        {
            hr = ParseAtomUnknownElement(pNode, &pEntry->pUnknownElements);
            ExitOnFailure1(hr, "Failed to parse unknown ATOM entry element: %ls", bstrNodeName);
        }

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM entry elements.");

    if (!pEntry->wzId || !*pEntry->wzId)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Failed to find required feed/entry/id element.");
    }
    else if (!pEntry->wzTitle || !*pEntry->wzTitle)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Failed to find required feed/entry/title element.");
    }
    else if (0 == pEntry->ftUpdated.dwHighDateTime && 0 == pEntry->ftUpdated.dwLowDateTime)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Failed to find required feed/entry/updated element.");
    }

    hr = S_OK;

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);
    ReleaseObject(pixnnmAttributes);

    return hr;
}


/********************************************************************
 ParseAtomLink - parses out an ATOM link from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseAtomLink(
    __in IXMLDOMNode* pixnLink,
    __in ATOM_LINK* pLink
    )
{
    HRESULT hr = S_OK;

    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNodeList *pNodeList = NULL;
    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;

    // Process attributes first.
    hr = pixnLink->get_attributes(&pixnnmAttributes);
    ExitOnFailure(hr, "Failed get attributes for ATOM link.");

    while (S_OK == (hr = XmlNextAttribute(pixnnmAttributes, &pNode, &bstrNodeName)))
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"rel", -1))
        {
            hr = AssignString(&pLink->wzRel, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM link rel.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"href", -1))
        {
            hr = AssignString(&pLink->wzUrl, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM link href.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"length", -1))
        {
            hr = XmlGetAttributeLargeNumber(pixnLink, bstrNodeName, &pLink->dw64Length);
            if (E_INVALIDARG == hr)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            }
            ExitOnFailure(hr, "Failed to parse ATOM link length.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"title", -1))
        {
            hr = AssignString(&pLink->wzTitle, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM link title.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"type", -1))
        {
            hr = AssignString(&pLink->wzType, pNode);
            ExitOnFailure(hr, "Failed to allocate ATOM link type.");
        }
        else
        {
            hr = ParseAtomUnknownAttribute(pNode, &pLink->pUnknownAttributes);
            ExitOnFailure1(hr, "Failed to parse unknown ATOM link attribute: %ls", bstrNodeName);
        }

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM link attributes.");

    // Process elements second.
    hr = pixnLink->get_childNodes(&pNodeList);
    ExitOnFailure(hr, "Failed to get child nodes of ATOM link element.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        hr = ParseAtomUnknownElement(pNode, &pLink->pUnknownElements);
        ExitOnFailure1(hr, "Failed to parse unknown ATOM link element: %ls", bstrNodeName);

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to process all ATOM link elements.");

    hr = AssignString(&pLink->wzValue, pixnLink);
    ExitOnFailure(hr, "Failed to allocate ATOM link value.");

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);
    ReleaseObject(pixnnmAttributes);

    return hr;
}


/********************************************************************
 ParseAtomUnknownElement - parses out an unknown item from the ATOM feed from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseAtomUnknownElement(
    __in IXMLDOMNode *pNode,
    __inout ATOM_UNKNOWN_ELEMENT** ppUnknownElement
    )
{
    Assert(ppUnknownElement);

    HRESULT hr = S_OK;
    BSTR bstrNodeNamespace = NULL;
    BSTR bstrNodeName = NULL;
    BSTR bstrNodeValue = NULL;
    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNode* pixnAttribute = NULL;
    ATOM_UNKNOWN_ELEMENT* pNewUnknownElement;

    pNewUnknownElement = (ATOM_UNKNOWN_ELEMENT*)MemAlloc(sizeof(ATOM_UNKNOWN_ELEMENT), TRUE);
    ExitOnNull(pNewUnknownElement, hr, E_OUTOFMEMORY, "Failed to allocate unknown element.");

    hr = pNode->get_namespaceURI(&bstrNodeNamespace);
    if (S_OK == hr)
    {
        hr = StrAllocString(&pNewUnknownElement->wzNamespace, bstrNodeNamespace, 0);
        ExitOnFailure(hr, "Failed to allocate ATOM unknown element namespace.");
    }
    else if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get unknown element namespace.");

    hr = pNode->get_baseName(&bstrNodeName);
    ExitOnFailure(hr, "Failed to get unknown element name.");

    hr = StrAllocString(&pNewUnknownElement->wzElement, bstrNodeName, 0);
    ExitOnFailure(hr, "Failed to allocate ATOM unknown element name.");

    hr = XmlGetText(pNode, &bstrNodeValue);
    ExitOnFailure(hr, "Failed to get unknown element value.");

    hr = StrAllocString(&pNewUnknownElement->wzValue, bstrNodeValue, 0);
    ExitOnFailure(hr, "Failed to allocate ATOM unknown element value.");

    hr = pNode->get_attributes(&pixnnmAttributes);
    ExitOnFailure(hr, "Failed get attributes on ATOM unknown element.");

    while (S_OK == (hr = pixnnmAttributes->nextNode(&pixnAttribute)))
    {
        hr = ParseAtomUnknownAttribute(pixnAttribute, &pNewUnknownElement->pAttributes);
        ExitOnFailure(hr, "Failed to parse attribute on ATOM unknown element.");

        ReleaseNullObject(pixnAttribute);
    }

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to enumerate all attributes on ATOM unknown element.");

    ATOM_UNKNOWN_ELEMENT** ppTail = ppUnknownElement;
    while (*ppTail)
    {
        ppTail = &(*ppTail)->pNext;
    }

    *ppTail = pNewUnknownElement;
    pNewUnknownElement = NULL;

LExit:
    FreeAtomUnknownElementList(pNewUnknownElement);

    ReleaseBSTR(bstrNodeNamespace);
    ReleaseBSTR(bstrNodeName);
    ReleaseBSTR(bstrNodeValue);
    ReleaseObject(pixnnmAttributes);
    ReleaseObject(pixnAttribute);

    return hr;
}


/********************************************************************
 ParseAtomUnknownAttribute - parses out attribute from an unknown element

*********************************************************************/
static HRESULT ParseAtomUnknownAttribute(
    __in IXMLDOMNode *pNode,
    __inout ATOM_UNKNOWN_ATTRIBUTE** ppUnknownAttribute
    )
{
    Assert(ppUnknownAttribute);

    HRESULT hr = S_OK;
    BSTR bstrNodeNamespace = NULL;
    BSTR bstrNodeName = NULL;
    BSTR bstrNodeValue = NULL;
    ATOM_UNKNOWN_ATTRIBUTE* pNewUnknownAttribute;

    pNewUnknownAttribute = (ATOM_UNKNOWN_ATTRIBUTE*)MemAlloc(sizeof(ATOM_UNKNOWN_ATTRIBUTE), TRUE);
    ExitOnNull(pNewUnknownAttribute, hr, E_OUTOFMEMORY, "Failed to allocate unknown attribute.");

    hr = pNode->get_namespaceURI(&bstrNodeNamespace);
    if (S_OK == hr)
    {
        hr = StrAllocString(&pNewUnknownAttribute->wzNamespace, bstrNodeNamespace, 0);
        ExitOnFailure(hr, "Failed to allocate ATOM unknown attribute namespace.");
    }
    else if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get unknown attribute namespace.");

    hr = pNode->get_baseName(&bstrNodeName);
    ExitOnFailure(hr, "Failed to get unknown attribute name.");

    hr = StrAllocString(&pNewUnknownAttribute->wzAttribute, bstrNodeName, 0);
    ExitOnFailure(hr, "Failed to allocate ATOM unknown attribute name.");

    hr = XmlGetText(pNode, &bstrNodeValue);
    ExitOnFailure(hr, "Failed to get unknown attribute value.");

    hr = StrAllocString(&pNewUnknownAttribute->wzValue, bstrNodeValue, 0);
    ExitOnFailure(hr, "Failed to allocate ATOM unknown attribute value.");

    ATOM_UNKNOWN_ATTRIBUTE** ppTail = ppUnknownAttribute;
    while (*ppTail)
    {
        ppTail = &(*ppTail)->pNext;
    }

    *ppTail = pNewUnknownAttribute;
    pNewUnknownAttribute = NULL;

LExit:
    FreeAtomUnknownAttributeList(pNewUnknownAttribute);

    ReleaseBSTR(bstrNodeNamespace);
    ReleaseBSTR(bstrNodeName);
    ReleaseBSTR(bstrNodeValue);

    return hr;
}


/********************************************************************
 AssignDateTime - assigns the value of a node to a FILETIME struct.

*********************************************************************/
static HRESULT AssignDateTime(
    __in FILETIME* pft,
    __in IXMLDOMNode* pNode
    )
{
    HRESULT hr = S_OK;
    BSTR bstrValue = NULL;

    if (0 != pft->dwHighDateTime || 0 != pft->dwLowDateTime)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Already process this datetime value.");
    }

    hr = XmlGetText(pNode, &bstrValue);
    ExitOnFailure(hr, "Failed to get value.");

    if (S_OK == hr)
    {
        hr = TimeFromString3339(bstrValue, pft);
        ExitOnFailure(hr, "Failed to convert value to time.");
    }
    else
    {
        ZeroMemory(pft, sizeof(FILETIME));
        hr = S_OK;
    }

LExit:
    ReleaseBSTR(bstrValue);

    return hr;
}


/********************************************************************
 AssignString - assigns the value of a node to a dynamic string.

*********************************************************************/
static HRESULT AssignString(
    __out_z LPWSTR* pwzValue,
    __in IXMLDOMNode* pNode
    )
{
    HRESULT hr = S_OK;
    BSTR bstrValue = NULL;

    if (pwzValue && *pwzValue)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Already processed this value.");
    }

    hr = XmlGetText(pNode, &bstrValue);
    ExitOnFailure(hr, "Failed to get value.");

    if (S_OK == hr)
    {
        hr = StrAllocString(pwzValue, bstrValue, 0);
        ExitOnFailure(hr, "Failed to allocate value.");
    }
    else
    {
        ReleaseNullStr(pwzValue);
        hr = S_OK;
    }

LExit:
    ReleaseBSTR(bstrValue);

    return hr;
}


/********************************************************************
 FreeAtomAuthor - releases all of the memory used by an ATOM author.

*********************************************************************/
static void FreeAtomAuthor(
    __in_opt ATOM_AUTHOR* pAuthor
    )
{
    if (pAuthor)
    {
        ReleaseStr(pAuthor->wzUrl);
        ReleaseStr(pAuthor->wzEmail);
        ReleaseStr(pAuthor->wzName);
    }
}


/********************************************************************
 FreeAtomCategory - releases all of the memory used by an ATOM category.

*********************************************************************/
static void FreeAtomCategory(
    __in_opt ATOM_CATEGORY* pCategory
    )
{
    if (pCategory)
    {
        FreeAtomUnknownElementList(pCategory->pUnknownElements);

        ReleaseStr(pCategory->wzTerm);
        ReleaseStr(pCategory->wzScheme);
        ReleaseStr(pCategory->wzLabel);
    }
}


/********************************************************************
 FreeAtomContent - releases all of the memory used by an ATOM content.

*********************************************************************/
static void FreeAtomContent(
    __in_opt ATOM_CONTENT* pContent
    )
{
    if (pContent)
    {
        FreeAtomUnknownElementList(pContent->pUnknownElements);

        ReleaseStr(pContent->wzValue);
        ReleaseStr(pContent->wzUrl);
        ReleaseStr(pContent->wzType);
    }
}


/********************************************************************
 FreeAtomEntry - releases all of the memory used by an ATOM entry.

*********************************************************************/
static void FreeAtomEntry(
    __in_opt ATOM_ENTRY* pEntry
    )
{
    if (pEntry)
    {
        FreeAtomUnknownElementList(pEntry->pUnknownElements);
        ReleaseObject(pEntry->pixn);

        for (DWORD i = 0; i < pEntry->cLinks; ++i)
        {
            FreeAtomLink(pEntry->rgLinks + i);
        }
        ReleaseMem(pEntry->rgLinks);

        for (DWORD i = 0; i < pEntry->cCategories; ++i)
        {
            FreeAtomCategory(pEntry->rgCategories + i);
        }
        ReleaseMem(pEntry->rgCategories);

        for (DWORD i = 0; i < pEntry->cAuthors; ++i)
        {
            FreeAtomAuthor(pEntry->rgAuthors + i);
        }
        ReleaseMem(pEntry->rgAuthors);

        FreeAtomContent(pEntry->pContent);
        ReleaseMem(pEntry->pContent);

        ReleaseStr(pEntry->wzTitle);
        ReleaseStr(pEntry->wzSummary);
        ReleaseStr(pEntry->wzId);
    }
}


/********************************************************************
 FreeAtomLink - releases all of the memory used by an ATOM link.

*********************************************************************/
static void FreeAtomLink(
    __in_opt ATOM_LINK* pLink
    )
{
    if (pLink)
    {
        FreeAtomUnknownElementList(pLink->pUnknownElements);
        FreeAtomUnknownAttributeList(pLink->pUnknownAttributes);

        ReleaseStr(pLink->wzValue);
        ReleaseStr(pLink->wzUrl);
        ReleaseStr(pLink->wzType);
        ReleaseStr(pLink->wzTitle);
        ReleaseStr(pLink->wzRel);
    }
}


/********************************************************************
 FreeAtomUnknownElement - releases all of the memory used by a list of unknown elements

*********************************************************************/
static void FreeAtomUnknownElementList(
    __in_opt ATOM_UNKNOWN_ELEMENT* pUnknownElement
    )
{
    while (pUnknownElement)
    {
        ATOM_UNKNOWN_ELEMENT* pFree = pUnknownElement;
        pUnknownElement = pUnknownElement->pNext;

        FreeAtomUnknownAttributeList(pFree->pAttributes);
        ReleaseStr(pFree->wzNamespace);
        ReleaseStr(pFree->wzElement);
        ReleaseStr(pFree->wzValue);
        MemFree(pFree);
    }
}


/********************************************************************
 FreeAtomUnknownAttribute - releases all of the memory used by a list of unknown attributes

*********************************************************************/
static void FreeAtomUnknownAttributeList(
    __in_opt ATOM_UNKNOWN_ATTRIBUTE* pUnknownAttribute
    )
{
    while (pUnknownAttribute)
    {
        ATOM_UNKNOWN_ATTRIBUTE* pFree = pUnknownAttribute;
        pUnknownAttribute = pUnknownAttribute->pNext;

        ReleaseStr(pFree->wzNamespace);
        ReleaseStr(pFree->wzAttribute);
        ReleaseStr(pFree->wzValue);
        MemFree(pFree);
    }
}
