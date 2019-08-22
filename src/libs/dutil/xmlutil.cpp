// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// intialization globals
CLSID vclsidXMLDOM = { 0, 0, 0, { 0, 0, 0, 0, 0, 0, 0, 0} };
static volatile LONG vcXmlInitialized = 0;
static BOOL vfMsxml40 = FALSE;
static BOOL fComInitialized = FALSE;
BOOL vfMsxml30 = FALSE;

/********************************************************************
 XmlInitialize - finds an appropriate version of the XML DOM

*********************************************************************/
extern "C" HRESULT DAPI XmlInitialize(
    )
{
    HRESULT hr = S_OK;

    if (!fComInitialized)
    {
        hr = ::CoInitialize(0);
        if (RPC_E_CHANGED_MODE != hr)
        {
            ExitOnFailure(hr, "failed to initialize COM");
            fComInitialized = TRUE;
        }
    }

    LONG cInitialized = ::InterlockedIncrement(&vcXmlInitialized);
    if (1 == cInitialized)
    {
        // NOTE: 4.0 behaves differently than 3.0 so there may be problems doing this
#if 0
        hr = ::CLSIDFromProgID(L"Msxml2.DOMDocument.4.0", &vclsidXMLDOM);
        if (S_OK == hr)
        {
            vfMsxml40 = TRUE;
            Trace(REPORT_VERBOSE, "found Msxml2.DOMDocument.4.0");
            ExitFunction();
        }
#endif
        hr = ::CLSIDFromProgID(L"Msxml2.DOMDocument", &vclsidXMLDOM);
        if (FAILED(hr))
        {
            // try to fall back to old MSXML
            hr = ::CLSIDFromProgID(L"MSXML.DOMDocument", &vclsidXMLDOM);
        }
        ExitOnFailure(hr, "failed to get CLSID for XML DOM");

        Assert(IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument) ||
               IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument20) ||
               IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument26) ||
               IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument30) ||
               IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument40) ||
               IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument50) ||
               IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument60));
    }

    hr = S_OK;
LExit:
    return hr;
}


/********************************************************************
 XmUninitialize -

*********************************************************************/
extern "C" void DAPI XmlUninitialize(
    )
{
    AssertSz(vcXmlInitialized, "XmlUninitialize called when not initialized");

    LONG cInitialized = ::InterlockedDecrement(&vcXmlInitialized);

    if (0 == cInitialized)
    {
        memset(&vclsidXMLDOM, 0, sizeof(vclsidXMLDOM));

        if (fComInitialized)
        {
            ::CoUninitialize();
        }
    } 
}

extern "C" HRESULT DAPI XmlCreateElement(
    __in IXMLDOMDocument *pixdDocument,
    __in_z LPCWSTR wzElementName,
    __out IXMLDOMElement **ppixnElement
    )
{
    if (!ppixnElement || !pixdDocument)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    BSTR bstrElementName = ::SysAllocString(wzElementName);
    ExitOnNull(bstrElementName, hr, E_OUTOFMEMORY, "failed SysAllocString");
    hr = pixdDocument->createElement(bstrElementName, ppixnElement);
LExit:
    ReleaseBSTR(bstrElementName);
    return hr;
}


/********************************************************************
 XmlCreateDocument -

*********************************************************************/
extern "C" HRESULT DAPI XmlCreateDocument(
    __in_opt LPCWSTR pwzElementName,
    __out IXMLDOMDocument** ppixdDocument,
    __out_opt IXMLDOMElement** ppixeRootElement
    )
{
    HRESULT hr = S_OK;
    BOOL (WINAPI *pfnDisableWow64)(__out PVOID* ) = NULL;
    BOOLEAN (WINAPI *pfnEnableWow64)(__in BOOLEAN ) = NULL;
    BOOL (WINAPI *pfnRevertWow64)(__in PVOID ) = NULL;
    BOOL fWow64Available = FALSE;
    void *pvWow64State = NULL;

    // RELEASEME
    IXMLDOMElement* pixeRootElement = NULL;
    IXMLDOMDocument *pixdDocument = NULL;

    // Test if we have access to the Wow64 API, and store the result in fWow64Available
    HMODULE hKernel32 = ::GetModuleHandleA("kernel32.dll");
    ExitOnNullWithLastError(hKernel32, hr, "failed to get handle to kernel32.dll");

    // This will test if we have access to the Wow64 API
    if (NULL != GetProcAddress(hKernel32, "IsWow64Process"))
    {
        pfnDisableWow64 = (BOOL (WINAPI *)(PVOID *))::GetProcAddress(hKernel32, "Wow64DisableWow64FsRedirection");
        pfnEnableWow64 = (BOOLEAN (WINAPI *)(BOOLEAN))::GetProcAddress(hKernel32, "Wow64EnableWow64FsRedirection");
        pfnRevertWow64 = (BOOL (WINAPI *)(PVOID))::GetProcAddress(hKernel32, "Wow64RevertWow64FsRedirection");

        fWow64Available = pfnDisableWow64 && pfnEnableWow64 && pfnRevertWow64;
    }

    // create the top level XML document
    AssertSz(vcXmlInitialized, "XmlInitialize() was not called");

    // Enable Wow64 Redirection, if possible
    if (fWow64Available)
    {
        // We want to enable Wow64 redirection, but the Wow64 API requires us to disable it first to get its current state (so we can revert it later)
        pfnDisableWow64(&pvWow64State);
        // If we fail to enable it, don't bother trying to disable it later on
        fWow64Available = pfnEnableWow64(TRUE);
    }

    hr = ::CoCreateInstance(vclsidXMLDOM, NULL, CLSCTX_INPROC_SERVER, XmlUtil_IID_IXMLDOMDocument, (void**)&pixdDocument);
    ExitOnFailure(hr, "failed to create XML DOM Document");
    Assert(pixdDocument);

    if (IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument30) || IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument20))
    {
        vfMsxml30 = TRUE;
    }

    if (pwzElementName)
    {
        hr = XmlCreateElement(pixdDocument, pwzElementName, &pixeRootElement);
        ExitOnFailure(hr, "failed XmlCreateElement");
        hr = pixdDocument->appendChild(pixeRootElement, NULL);
        ExitOnFailure(hr, "failed appendChild");
    }

    *ppixdDocument = pixdDocument;
    pixdDocument = NULL;

    if (ppixeRootElement)
    {
        *ppixeRootElement = pixeRootElement;
        pixeRootElement = NULL;
    }

LExit:
    // Re-disable Wow64 Redirection, if appropriate
    if (fWow64Available && !pfnRevertWow64(pvWow64State))
    {
        // If we expected to be able to revert, and couldn't, fail in the only graceful way we can
        ::ExitProcess(1);
    }

    ReleaseObject(pixeRootElement);
    ReleaseObject(pixdDocument);
    return hr;
}


/********************************************************************
 XmlLoadDocument -

*********************************************************************/
extern "C" HRESULT DAPI XmlLoadDocument(
    __in_z LPCWSTR wzDocument,
    __out IXMLDOMDocument** ppixdDocument
    )
{
    return XmlLoadDocumentEx(wzDocument, 0, ppixdDocument);
}


/********************************************************************
 XmlReportParseError -

*********************************************************************/
static void XmlReportParseError(
    __in IXMLDOMParseError* pixpe
    )
{
    HRESULT hr = S_OK;
    long lNumber = 0;
    BSTR bstr = NULL;

    Trace(REPORT_STANDARD, "Failed to parse XML. IXMLDOMParseError reports:");

    hr = pixpe->get_errorCode(&lNumber);
    ExitOnFailure(hr, "Failed to query IXMLDOMParseError.errorCode.");
    Trace1(REPORT_STANDARD, "errorCode = 0x%x", lNumber);

    hr = pixpe->get_filepos(&lNumber);
    ExitOnFailure(hr, "Failed to query IXMLDOMParseError.filepos.");
    Trace1(REPORT_STANDARD, "filepos = %d", lNumber);

    hr = pixpe->get_line(&lNumber);
    ExitOnFailure(hr, "Failed to query IXMLDOMParseError.line.");
    Trace1(REPORT_STANDARD, "line = %d", lNumber);

    hr = pixpe->get_linepos(&lNumber);
    ExitOnFailure(hr, "Failed to query IXMLDOMParseError.linepos.");
    Trace1(REPORT_STANDARD, "linepos = %d", lNumber);

    hr = pixpe->get_reason(&bstr);
    ExitOnFailure(hr, "Failed to query IXMLDOMParseError.reason.");
    Trace1(REPORT_STANDARD, "reason = %ls", bstr);
    ReleaseNullBSTR(bstr);

    hr = pixpe->get_srcText (&bstr);
    ExitOnFailure(hr, "Failed to query IXMLDOMParseError.srcText .");
    Trace1(REPORT_STANDARD, "srcText = %ls", bstr);
    ReleaseNullBSTR(bstr);

LExit:
    ReleaseBSTR(bstr);
}

/********************************************************************
 XmlLoadDocumentEx -

*********************************************************************/
extern "C" HRESULT DAPI XmlLoadDocumentEx(
    __in_z LPCWSTR wzDocument,
    __in DWORD dwAttributes,
    __out IXMLDOMDocument** ppixdDocument
    )
{
    HRESULT hr = S_OK;
    VARIANT_BOOL vbSuccess = 0;

    // RELEASEME
    IXMLDOMDocument* pixd = NULL;
    IXMLDOMParseError* pixpe = NULL;
    BSTR bstrLoad = NULL;

    if (!wzDocument || !*wzDocument)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "string must be non-null");
    }

    hr = XmlCreateDocument(NULL, &pixd);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed XmlCreateDocument");

    if (dwAttributes & XML_LOAD_PRESERVE_WHITESPACE)
    {
        hr = pixd->put_preserveWhiteSpace(VARIANT_TRUE);
        ExitOnFailure(hr, "failed put_preserveWhiteSpace");
    }

    // Security issue.  Avoid triggering anything external.
    hr = pixd->put_validateOnParse(VARIANT_FALSE);
    ExitOnFailure(hr, "failed put_validateOnParse");
    hr = pixd->put_resolveExternals(VARIANT_FALSE);
    ExitOnFailure(hr, "failed put_resolveExternals");

    bstrLoad = ::SysAllocString(wzDocument);
    ExitOnNull(bstrLoad, hr, E_OUTOFMEMORY, "failed to allocate bstr for Load in XmlLoadDocumentEx");

    hr = pixd->loadXML(bstrLoad, &vbSuccess);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OPEN_FAILED);
    }

    if (FAILED(hr) && S_OK == pixd->get_parseError(&pixpe))
    {
        XmlReportParseError(pixpe);
    }

    ExitOnFailure(hr, "failed loadXML");


    hr = S_OK;
LExit:
    if (ppixdDocument)
    {
        *ppixdDocument = pixd;
        pixd = NULL;
    }
    ReleaseBSTR(bstrLoad);
    ReleaseObject(pixd);
    ReleaseObject(pixpe);

    return hr;
}


/*******************************************************************
 XmlLoadDocumentFromFile

********************************************************************/
extern "C" HRESULT DAPI XmlLoadDocumentFromFile(
    __in_z LPCWSTR wzPath,
    __out IXMLDOMDocument** ppixdDocument
    )
{
    return XmlLoadDocumentFromFileEx(wzPath, 0, ppixdDocument);
}


/*******************************************************************
 XmlLoadDocumentFromFileEx

********************************************************************/
extern "C" HRESULT DAPI XmlLoadDocumentFromFileEx(
    __in_z LPCWSTR wzPath,
    __in DWORD dwAttributes,
    __out IXMLDOMDocument** ppixdDocument
    )
{
    HRESULT hr = S_OK;
    VARIANT varPath;
    VARIANT_BOOL vbSuccess = 0;

    IXMLDOMDocument* pixd = NULL;
    IXMLDOMParseError* pixpe = NULL;

    ::VariantInit(&varPath);
    varPath.vt = VT_BSTR;
    varPath.bstrVal = ::SysAllocString(wzPath);
    ExitOnNull(varPath.bstrVal, hr, E_OUTOFMEMORY, "failed to allocate bstr for Path in XmlLoadDocumentFromFileEx");

    hr = XmlCreateDocument(NULL, &pixd);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed XmlCreateDocument");

    if (dwAttributes & XML_LOAD_PRESERVE_WHITESPACE)
    {
        hr = pixd->put_preserveWhiteSpace(VARIANT_TRUE);
        ExitOnFailure(hr, "failed put_preserveWhiteSpace");
    }

    // Avoid triggering anything external.
    hr = pixd->put_validateOnParse(VARIANT_FALSE);
    ExitOnFailure(hr, "failed put_validateOnParse");
    hr = pixd->put_resolveExternals(VARIANT_FALSE);
    ExitOnFailure(hr, "failed put_resolveExternals");

    pixd->put_async(VARIANT_FALSE);
    hr = pixd->load(varPath, &vbSuccess);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OPEN_FAILED);
    }

    if (FAILED(hr) && S_OK == pixd->get_parseError(&pixpe))
    {
        XmlReportParseError(pixpe);
    }

    ExitOnFailure1(hr, "failed to load XML from: %ls", wzPath);

    if (ppixdDocument)
    {
        *ppixdDocument = pixd;
        pixd = NULL;
    }

    hr = S_OK;
LExit:
    ReleaseVariant(varPath);
    ReleaseObject(pixd);
    ReleaseObject(pixpe);

    return hr;
}


/********************************************************************
 XmlLoadDocumentFromBuffer

*********************************************************************/
extern "C" HRESULT DAPI XmlLoadDocumentFromBuffer(
    __in_bcount(cbSource) const BYTE* pbSource,
    __in DWORD cbSource,
    __out IXMLDOMDocument** ppixdDocument
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixdDocument = NULL;
    SAFEARRAY sa = { };
    VARIANT vtXmlSource;
    VARIANT_BOOL vbSuccess = 0;

    ::VariantInit(&vtXmlSource);

    // create document
    hr = XmlCreateDocument(NULL, &pixdDocument);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed XmlCreateDocument");

    // Security issue.  Avoid triggering anything external.
    hr = pixdDocument->put_validateOnParse(VARIANT_FALSE);
    ExitOnFailure(hr, "failed put_validateOnParse");
    hr = pixdDocument->put_resolveExternals(VARIANT_FALSE);
    ExitOnFailure(hr, "failed put_resolveExternals");

    // load document
    sa.cDims = 1;
    sa.fFeatures = FADF_STATIC | FADF_FIXEDSIZE;
    sa.cbElements = 1;
    sa.pvData = (PVOID)pbSource;
    sa.rgsabound[0].cElements = cbSource;
    vtXmlSource.vt = VT_ARRAY | VT_UI1;
    vtXmlSource.parray = &sa;

    hr = pixdDocument->load(vtXmlSource, &vbSuccess);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OPEN_FAILED);
    }
    ExitOnFailure(hr, "failed loadXML");

    // return value
    *ppixdDocument = pixdDocument;
    pixdDocument = NULL;

LExit:
    ReleaseObject(pixdDocument);
    return hr;
}


/********************************************************************
 XmlSetAttribute -

*********************************************************************/
extern "C" HRESULT DAPI XmlSetAttribute(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR pwzAttribute,
    __in_z LPCWSTR pwzAttributeValue
    )
{
    HRESULT hr = S_OK;
    VARIANT varAttributeValue;
    ::VariantInit(&varAttributeValue);

    // RELEASEME
    IXMLDOMDocument* pixdDocument = NULL;
    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMAttribute* pixaAttribute = NULL;
    IXMLDOMNode* pixaNode = NULL;
    BSTR bstrAttributeName = ::SysAllocString(pwzAttribute);
    ExitOnNull(bstrAttributeName, hr, E_OUTOFMEMORY, "failed to allocate bstr for AttributeName in XmlSetAttribute");

    hr = pixnNode->get_attributes(&pixnnmAttributes);
    ExitOnFailure1(hr, "failed get_attributes in XmlSetAttribute(%ls)", pwzAttribute);

    hr = pixnNode->get_ownerDocument(&pixdDocument);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed get_ownerDocument in XmlSetAttribute");

    hr = pixdDocument->createAttribute(bstrAttributeName, &pixaAttribute);
    ExitOnFailure1(hr, "failed createAttribute in XmlSetAttribute(%ls)", pwzAttribute);

    varAttributeValue.vt = VT_BSTR;
    varAttributeValue.bstrVal = ::SysAllocString(pwzAttributeValue);
    if (!varAttributeValue.bstrVal)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }
    ExitOnFailure(hr, "failed SysAllocString in XmlSetAttribute");

    hr = pixaAttribute->put_nodeValue(varAttributeValue);
    ExitOnFailure1(hr, "failed put_nodeValue in XmlSetAttribute(%ls)", pwzAttribute);

    hr = pixnnmAttributes->setNamedItem(pixaAttribute, &pixaNode);
    ExitOnFailure1(hr, "failed setNamedItem in XmlSetAttribute(%ls)", pwzAttribute);

LExit:
    ReleaseObject(pixdDocument);
    ReleaseObject(pixnnmAttributes);
    ReleaseObject(pixaAttribute);
    ReleaseObject(pixaNode);
    ReleaseBSTR(varAttributeValue.bstrVal);
    ReleaseBSTR(bstrAttributeName);

    return hr;
}


/********************************************************************
 XmlSelectSingleNode -

*********************************************************************/
extern "C" HRESULT DAPI XmlSelectSingleNode(
    __in IXMLDOMNode* pixnParent,
    __in_z LPCWSTR wzXPath,
    __out IXMLDOMNode **ppixnChild
    )
{
    HRESULT hr = S_OK;

    BSTR bstrXPath = NULL;

    ExitOnNull(pixnParent, hr, E_UNEXPECTED, "pixnParent parameter was null in XmlSelectSingleNode");
    ExitOnNull(ppixnChild, hr, E_UNEXPECTED, "ppixnChild parameter was null in XmlSelectSingleNode");

    bstrXPath = ::SysAllocString(wzXPath ? wzXPath : L"");
    ExitOnNull(bstrXPath, hr, E_OUTOFMEMORY, "failed to allocate bstr for XPath expression in XmlSelectSingleNode");

    hr = pixnParent->selectSingleNode(bstrXPath, ppixnChild);

LExit:
    ReleaseBSTR(bstrXPath);

    return hr;
}


/********************************************************************
 XmlCreateTextNode -

*********************************************************************/
extern "C" HRESULT DAPI XmlCreateTextNode(
    __in IXMLDOMDocument *pixdDocument,
    __in_z LPCWSTR wzText,
    __out IXMLDOMText **ppixnTextNode
    )
{
    if (!ppixnTextNode || !pixdDocument)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    BSTR bstrText = ::SysAllocString(wzText);
    ExitOnNull(bstrText, hr, E_OUTOFMEMORY, "failed SysAllocString");
    hr = pixdDocument->createTextNode(bstrText, ppixnTextNode);
LExit:
    ReleaseBSTR(bstrText);

    return hr;
}


/********************************************************************
 XmlGetText

*********************************************************************/
extern "C" HRESULT DAPI XmlGetText(
    __in IXMLDOMNode* pixnNode,
    __deref_out_z BSTR* pbstrText
    )
{
    return pixnNode->get_text(pbstrText);
}


/********************************************************************
 XmlGetAttribute

*********************************************************************/
extern "C" HRESULT DAPI XmlGetAttribute(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR pwzAttribute,
    __deref_out_z BSTR* pbstrAttributeValue
    )
{
    Assert(pixnNode);
    HRESULT hr = S_OK;

    // RELEASEME
    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNode* pixnAttribute = NULL;
    VARIANT varAttributeValue;
    BSTR bstrAttribute = SysAllocString(pwzAttribute);

    // INIT
    ::VariantInit(&varAttributeValue);

    // get attribute value from source
    hr = pixnNode->get_attributes(&pixnnmAttributes);
    ExitOnFailure(hr, "failed get_attributes");

    hr = XmlGetNamedItem(pixnnmAttributes, bstrAttribute, &pixnAttribute);
    if (S_FALSE == hr)
    {
        // hr = E_FAIL;
        ExitFunction();
    }
    ExitOnFailure1(hr, "failed getNamedItem in XmlGetAttribute(%ls)", pwzAttribute);

    hr = pixnAttribute->get_nodeValue(&varAttributeValue);
    ExitOnFailure1(hr, "failed get_nodeValue in XmlGetAttribute(%ls)", pwzAttribute);

    // steal the BSTR from the VARIANT
    if (S_OK == hr && pbstrAttributeValue)
    {
        *pbstrAttributeValue = varAttributeValue.bstrVal;
        varAttributeValue.bstrVal = NULL;
    }

LExit:
    ReleaseObject(pixnnmAttributes);
    ReleaseObject(pixnAttribute);
    ReleaseVariant(varAttributeValue);
    ReleaseBSTR(bstrAttribute);

    return hr;
}


/********************************************************************
 XmlGetAttributeEx

*********************************************************************/
HRESULT DAPI XmlGetAttributeEx(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR wzAttribute,
    __deref_out_z LPWSTR* psczAttributeValue
    )
{
    Assert(pixnNode);
    HRESULT hr = S_OK;
    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNode* pixnAttribute = NULL;
    VARIANT varAttributeValue;
    BSTR bstrAttribute = NULL;

    ::VariantInit(&varAttributeValue);

    // get attribute value from source
    hr = pixnNode->get_attributes(&pixnnmAttributes);
    ExitOnFailure(hr, "Failed get_attributes.");

    bstrAttribute = ::SysAllocString(wzAttribute);
    ExitOnNull(bstrAttribute, hr, E_OUTOFMEMORY, "Failed to allocate attribute name BSTR.");

    hr = XmlGetNamedItem(pixnnmAttributes, bstrAttribute, &pixnAttribute);
    if (S_FALSE == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    ExitOnFailure1(hr, "Failed getNamedItem in XmlGetAttribute(%ls)", wzAttribute);

    hr = pixnAttribute->get_nodeValue(&varAttributeValue);
    if (S_FALSE == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    ExitOnFailure1(hr, "Failed get_nodeValue in XmlGetAttribute(%ls)", wzAttribute);

    // copy value
    hr = StrAllocString(psczAttributeValue, varAttributeValue.bstrVal, 0);
    ExitOnFailure(hr, "Failed to copy attribute value.");

LExit:
    ReleaseObject(pixnnmAttributes);
    ReleaseObject(pixnAttribute);
    ReleaseVariant(varAttributeValue);
    ReleaseBSTR(bstrAttribute);

    return hr;
}


/********************************************************************
 XmlGetYesNoAttribute

*********************************************************************/
HRESULT DAPI XmlGetYesNoAttribute(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR wzAttribute,
    __out BOOL* pfYes
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    hr = XmlGetAttributeEx(pixnNode, wzAttribute, &sczValue);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get attribute.");

        *pfYes = CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, sczValue, -1, L"yes", -1);
    }

LExit:
    ReleaseStr(sczValue);

    return hr;
}



/********************************************************************
 XmlGetAttributeNumber

*********************************************************************/
extern "C" HRESULT DAPI XmlGetAttributeNumber(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR pwzAttribute,
    __out DWORD* pdwValue
    )
{
    HRESULT hr = XmlGetAttributeNumberBase(pixnNode, pwzAttribute, 10, pdwValue);
    return hr;
}


/********************************************************************
 XmlGetAttributeNumberBase

*********************************************************************/
extern "C" HRESULT DAPI XmlGetAttributeNumberBase(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR pwzAttribute,
    __in int nBase,
    __out DWORD* pdwValue
    )
{
    HRESULT hr = S_OK;
    BSTR bstrPointer = NULL;

    hr = XmlGetAttribute(pixnNode, pwzAttribute, &bstrPointer);
    ExitOnFailure(hr, "Failed to get value from attribute.");

    if (S_OK == hr)
    {
        *pdwValue = wcstoul(bstrPointer, NULL, nBase);
    }

LExit:
    ReleaseBSTR(bstrPointer);
    return hr;
}


/********************************************************************
 XmlGetAttributeLargeNumber

*********************************************************************/
extern "C" HRESULT DAPI XmlGetAttributeLargeNumber(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR pwzAttribute,
    __out DWORD64* pdw64Value
    )
{
    HRESULT hr = S_OK;
    BSTR bstrValue = NULL;

    hr = XmlGetAttribute(pixnNode, pwzAttribute, &bstrValue);
    ExitOnFailure(hr, "failed XmlGetAttribute");

    if (S_OK == hr)
    {
        LONGLONG ll = 0;
        hr = StrStringToInt64(bstrValue, 0, &ll);
        ExitOnFailure(hr, "Failed to treat attribute value as number.");

        *pdw64Value = ll;
    }
    else
    {
        *pdw64Value = 0;
    }

LExit:
    ReleaseBSTR(bstrValue);
    return hr;
}


/********************************************************************
 XmlGetNamedItem -

*********************************************************************/
extern "C" HRESULT DAPI XmlGetNamedItem(
    __in IXMLDOMNamedNodeMap *pixnmAttributes,
    __in_opt LPCWSTR wzName,
    __out IXMLDOMNode **ppixnNamedItem
    )
{
    if (!pixnmAttributes || !ppixnNamedItem)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    BSTR bstrName = ::SysAllocString(wzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString");

    hr = pixnmAttributes->getNamedItem(bstrName, ppixnNamedItem);

LExit:
    ReleaseBSTR(bstrName);
    return hr;
}


/********************************************************************
 XmlSetText -

*********************************************************************/
extern "C" HRESULT DAPI XmlSetText(
    __in IXMLDOMNode *pixnNode,
    __in_z LPCWSTR pwzText
    )
{
    Assert(pixnNode && pwzText);
    HRESULT hr = S_OK;
    DOMNodeType dnType;

    // RELEASEME
    IXMLDOMDocument* pixdDocument = NULL;
    IXMLDOMNodeList* pixnlNodeList = NULL;
    IXMLDOMNode* pixnChildNode = NULL;
    IXMLDOMText* pixtTextNode = NULL;
    VARIANT varText;

    ::VariantInit(&varText);

    // find the text node
    hr = pixnNode->get_childNodes(&pixnlNodeList);
    ExitOnFailure(hr, "failed to get child nodes");

    while (S_OK == (hr = pixnlNodeList->nextNode(&pixnChildNode)))
    {
        hr = pixnChildNode->get_nodeType(&dnType);
        ExitOnFailure(hr, "failed to get node type");

        if (NODE_TEXT == dnType)
            break;
        ReleaseNullObject(pixnChildNode);
    }
    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

    if (pixnChildNode)
    {
        varText.vt = VT_BSTR;
        varText.bstrVal = ::SysAllocString(pwzText);
        if (!varText.bstrVal)
        {
            hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
        }
        ExitOnFailure(hr, "failed SysAllocString in XmlSetText");

        hr = pixnChildNode->put_nodeValue(varText);
        ExitOnFailure(hr, "failed IXMLDOMNode::put_nodeValue");
    }
    else
    {
        hr = pixnNode->get_ownerDocument(&pixdDocument);
        if (hr == S_FALSE)
        {
            hr = E_FAIL;
        }
        ExitOnFailure(hr, "failed get_ownerDocument in XmlSetAttribute");

        hr = XmlCreateTextNode(pixdDocument, pwzText, &pixtTextNode);
        ExitOnFailure1(hr, "failed createTextNode in XmlSetText(%ls)", pwzText);

        hr = pixnNode->appendChild(pixtTextNode, NULL);
        ExitOnFailure1(hr, "failed appendChild in XmlSetText(%ls)", pwzText);
    }

    hr = *pwzText ? S_OK : S_FALSE;

LExit:
    ReleaseObject(pixnlNodeList);
    ReleaseObject(pixnChildNode);
    ReleaseObject(pixdDocument);
    ReleaseObject(pixtTextNode);
    ReleaseVariant(varText);
    return hr;
}


/********************************************************************
 XmlSetTextNumber -

*********************************************************************/
extern "C" HRESULT DAPI XmlSetTextNumber(
    __in IXMLDOMNode *pixnNode,
    __in DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    WCHAR wzValue[12];

    hr = ::StringCchPrintfW(wzValue, countof(wzValue), L"%u", dwValue);
    ExitOnFailure(hr, "Failed to format numeric value as string.");

    hr = XmlSetText(pixnNode, wzValue);

LExit:
    return hr;
}


/********************************************************************
 XmlCreateChild -

*********************************************************************/
extern "C" HRESULT DAPI XmlCreateChild(
    __in IXMLDOMNode* pixnParent,
    __in_z LPCWSTR pwzElementType,
    __out IXMLDOMNode** ppixnChild
    )
{
    HRESULT hr = S_OK;

    // RELEASEME
    IXMLDOMDocument* pixdDocument = NULL;
    IXMLDOMNode* pixnChild = NULL;

    hr = pixnParent->get_ownerDocument(&pixdDocument);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed get_ownerDocument");

    hr = XmlCreateElement(pixdDocument, pwzElementType, (IXMLDOMElement**) &pixnChild);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed createElement");

    pixnParent->appendChild(pixnChild,NULL);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed appendChild");

    if (ppixnChild)
    {
        *ppixnChild = pixnChild;
        pixnChild = NULL;
    }

LExit:
    ReleaseObject(pixdDocument);
    ReleaseObject(pixnChild);
    return hr;
}

/********************************************************************
 XmlRemoveAttribute -

*********************************************************************/
extern "C" HRESULT DAPI XmlRemoveAttribute(
    __in IXMLDOMNode* pixnNode,
    __in_z LPCWSTR pwzAttribute
    )
{
    HRESULT hr = S_OK;

    // RELEASEME
    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    BSTR bstrAttribute = ::SysAllocString(pwzAttribute);
    ExitOnNull(bstrAttribute, hr, E_OUTOFMEMORY, "failed to allocate bstr for attribute in XmlRemoveAttribute");

    hr = pixnNode->get_attributes(&pixnnmAttributes);
    ExitOnFailure1(hr, "failed get_attributes in RemoveXmlAttribute(%ls)", pwzAttribute);

    hr = pixnnmAttributes->removeNamedItem(bstrAttribute, NULL);
    ExitOnFailure1(hr, "failed removeNamedItem in RemoveXmlAttribute(%ls)", pwzAttribute);

LExit:
    ReleaseObject(pixnnmAttributes);
    ReleaseBSTR(bstrAttribute);

    return hr;
}


/********************************************************************
 XmlSelectNodes -

*********************************************************************/
extern "C" HRESULT DAPI XmlSelectNodes(
    __in IXMLDOMNode* pixnParent,
    __in_z LPCWSTR wzXPath,
    __out IXMLDOMNodeList **ppixnlChildren
    )
{
    HRESULT hr = S_OK;

    BSTR bstrXPath = NULL;

    ExitOnNull(pixnParent, hr, E_UNEXPECTED, "pixnParent parameter was null in XmlSelectNodes");
    ExitOnNull(ppixnlChildren, hr, E_UNEXPECTED, "ppixnChild parameter was null in XmlSelectNodes");

    bstrXPath = ::SysAllocString(wzXPath ? wzXPath : L"");
    ExitOnNull(bstrXPath, hr, E_OUTOFMEMORY, "failed to allocate bstr for XPath expression in XmlSelectNodes");

    hr = pixnParent->selectNodes(bstrXPath, ppixnlChildren);

LExit:
    ReleaseBSTR(bstrXPath);
    return hr;
}


/********************************************************************
 XmlNextAttribute - returns the next attribute in a node list

 NOTE: pbstrAttribute is optional
       returns S_OK if found an element
       returns S_FALSE if no element found
       returns E_* if something went wrong
********************************************************************/
extern "C" HRESULT DAPI XmlNextAttribute(
    __in IXMLDOMNamedNodeMap* pixnnm,
    __out IXMLDOMNode** pixnAttribute,
    __deref_opt_out_z_opt BSTR* pbstrAttribute
    )
{
    Assert(pixnnm && pixnAttribute);

    HRESULT hr = S_OK;
    IXMLDOMNode* pixn = NULL;
    DOMNodeType nt;

    // null out the return values
    *pixnAttribute = NULL;
    if (pbstrAttribute)
    {
        *pbstrAttribute = NULL;
    }

    hr = pixnnm->nextNode(&pixn);
    ExitOnFailure(hr, "Failed to get next attribute.");

    if (S_OK == hr)
    {
        hr = pixn->get_nodeType(&nt);
        ExitOnFailure(hr, "failed to get node type");

        if (NODE_ATTRIBUTE != nt)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Failed to get expected node type back: attribute");
        }

        // if the caller asked for the attribute name
        if (pbstrAttribute)
        {
            hr = pixn->get_baseName(pbstrAttribute);
            ExitOnFailure(hr, "failed to get attribute name");
        }

        *pixnAttribute = pixn;
        pixn = NULL;
    }

LExit:
    ReleaseObject(pixn);
    return hr;
}


/********************************************************************
 XmlNextElement - returns the next element in a node list

 NOTE: pbstrElement is optional
       returns S_OK if found an element
       returns S_FALSE if no element found
       returns E_* if something went wrong
********************************************************************/
extern "C" HRESULT DAPI XmlNextElement(
    __in IXMLDOMNodeList* pixnl,
    __out IXMLDOMNode** pixnElement,
    __deref_opt_out_z_opt BSTR* pbstrElement
    )
{
    Assert(pixnl && pixnElement);

    HRESULT hr = S_OK;
    IXMLDOMNode* pixn = NULL;
    DOMNodeType nt;

    // null out the return values
    *pixnElement = NULL;
    if (pbstrElement)
    {
        *pbstrElement = NULL;
    }

    //
    // find the next element in the list
    //
    while (S_OK == (hr = pixnl->nextNode(&pixn)))
    {
        hr = pixn->get_nodeType(&nt);
        ExitOnFailure(hr, "failed to get node type");

        if (NODE_ELEMENT == nt)
            break;

        ReleaseNullObject(pixn);
    }
    ExitOnFailure(hr, "failed to get next element");

    // if we have a node and the caller asked for the element name
    if (pixn && pbstrElement)
    {
        hr = pixn->get_baseName(pbstrElement);
        ExitOnFailure(hr, "failed to get element name");
    }

    *pixnElement = pixn;
    pixn = NULL;

    hr = *pixnElement ? S_OK : S_FALSE;
LExit:
    ReleaseObject(pixn);
    return hr;
}


/********************************************************************
 XmlRemoveChildren -

*********************************************************************/
extern "C" HRESULT DAPI XmlRemoveChildren(
    __in IXMLDOMNode* pixnSource,
    __in_z LPCWSTR pwzXPath
    )
{
    HRESULT hr = S_OK;

    // RELEASEME
    IXMLDOMNodeList* pixnlNodeList = NULL;
    IXMLDOMNode* pixnNode = NULL;
    IXMLDOMNode* pixnRemoveChild = NULL;

    if (pwzXPath)
    {
        hr = XmlSelectNodes(pixnSource, pwzXPath, &pixnlNodeList);
        ExitOnFailure(hr, "failed XmlSelectNodes");
    }
    else
    {
        hr = pixnSource->get_childNodes(&pixnlNodeList);
        ExitOnFailure(hr, "failed childNodes");
    }
    if (S_FALSE == hr)
    {
        ExitFunction();
    }

    while (S_OK == (hr = pixnlNodeList->nextNode(&pixnNode)))
    {
        hr = pixnSource->removeChild(pixnNode, &pixnRemoveChild);
        ExitOnFailure(hr, "failed removeChild");

        ReleaseNullObject(pixnRemoveChild);
        ReleaseNullObject(pixnNode);
    }
    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseObject(pixnlNodeList);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnRemoveChild);

    return hr;
}


/********************************************************************
 XmlSaveDocument -

*********************************************************************/
extern "C" HRESULT DAPI XmlSaveDocument(
    __in IXMLDOMDocument* pixdDocument,
    __inout LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;

    // RELEASEME
    VARIANT varsDestPath;

    ::VariantInit(&varsDestPath);
    varsDestPath.vt = VT_BSTR;
    varsDestPath.bstrVal = ::SysAllocString(wzPath);
    if (!varsDestPath.bstrVal)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }
    ExitOnFailure(hr, "failed to create BSTR");

    hr = pixdDocument->save(varsDestPath);
    if (hr == S_FALSE)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "failed save in WriteDocument");

LExit:
    ReleaseVariant(varsDestPath);
    return hr;
}


/********************************************************************
 XmlSaveDocumentToBuffer

*********************************************************************/
extern "C" HRESULT DAPI XmlSaveDocumentToBuffer(
    __in IXMLDOMDocument* pixdDocument,
    __deref_out_bcount(*pcbDest) BYTE** ppbDest,
    __out DWORD* pcbDest
    )
{
    HRESULT hr = S_OK;
    IStream* pStream = NULL;
    LARGE_INTEGER li = { };
    STATSTG statstg = { };
    BYTE* pbDest = NULL;
    ULONG cbRead = 0;
    VARIANT vtDestination;

    ::VariantInit(&vtDestination);

    // create stream
    hr = ::CreateStreamOnHGlobal(NULL, TRUE, &pStream);
    ExitOnFailure(hr, "Failed to create stream.");

    // write document to stream
    vtDestination.vt = VT_UNKNOWN;
    vtDestination.punkVal = (IUnknown*)pStream;
    hr = pixdDocument->save(vtDestination);
    ExitOnFailure(hr, "Failed to save document.");

    // get stream size
    hr = pStream->Stat(&statstg, STATFLAG_NONAME);
    ExitOnFailure(hr, "Failed to get stream size.");

    // allocate buffer
    pbDest = static_cast<BYTE*>(MemAlloc((SIZE_T)statstg.cbSize.LowPart, TRUE));
    ExitOnNull(pbDest, hr, E_OUTOFMEMORY, "Failed to allocate destination buffer.");

    // read data from stream
    li.QuadPart = 0;
    hr = pStream->Seek(li, STREAM_SEEK_SET, NULL);
    ExitOnFailure(hr, "Failed to seek stream.");

    hr = pStream->Read(pbDest, statstg.cbSize.LowPart, &cbRead);
    if (cbRead < statstg.cbSize.LowPart)
    {
        hr = E_FAIL;
    }
    ExitOnFailure(hr, "Failed to read stream content to buffer.");

    // return value
    *ppbDest = pbDest;
    pbDest = NULL;
    *pcbDest = statstg.cbSize.LowPart;

LExit:
    ReleaseObject(pStream);
    ReleaseMem(pbDest);
    return hr;
}
