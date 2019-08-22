// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "iis7util.h"

#define ISSTRINGVARIANT(vt) (VT_BSTR == vt || VT_LPWSTR == vt)

extern "C" HRESULT DAPI Iis7PutPropertyVariant(
    __in IAppHostElement *pElement,
    __in LPCWSTR wzPropName,
    __in VARIANT vtPut
    )
{
    HRESULT hr = S_OK;
    IAppHostProperty *pProperty = NULL;
    BSTR bstrPropName = NULL;

    bstrPropName = ::SysAllocString(wzPropName);
    ExitOnNull(bstrPropName, hr, E_OUTOFMEMORY, "failed SysAllocString");

    hr = pElement->GetPropertyByName(bstrPropName, &pProperty);
    ExitOnFailure1(hr, "Failed to get property object for %ls", wzPropName);

    hr = pProperty->put_Value(vtPut);
    ExitOnFailure1(hr, "Failed to set property value for %ls", wzPropName);

LExit:
    ReleaseBSTR(bstrPropName);
    // caller responsible for cleaning up variant vtPut
    ReleaseObject(pProperty);

    return hr;
}

extern "C" HRESULT DAPI Iis7PutPropertyString(
    __in IAppHostElement *pElement,
    __in LPCWSTR wzPropName,
    __in LPCWSTR wzString
    )
{
    HRESULT hr = S_OK;
    VARIANT vtPut;

    ::VariantInit(&vtPut);
    vtPut.vt = VT_BSTR;
    vtPut.bstrVal = ::SysAllocString(wzString);
    ExitOnNull(vtPut.bstrVal, hr, E_OUTOFMEMORY, "failed SysAllocString");

    hr = Iis7PutPropertyVariant(pElement, wzPropName, vtPut);

LExit:
    ReleaseVariant(vtPut);

    return hr;
}

extern "C" HRESULT DAPI Iis7PutPropertyInteger(
    __in IAppHostElement *pElement,
    __in LPCWSTR wzPropName,
    __in DWORD dValue
    )
{
    VARIANT vtPut;

    ::VariantInit(&vtPut);
    vtPut.vt = VT_I4;
    vtPut.lVal = dValue;
    return Iis7PutPropertyVariant(pElement, wzPropName, vtPut);
}

extern "C" HRESULT DAPI Iis7PutPropertyBool(
    __in IAppHostElement *pElement,
    __in LPCWSTR wzPropName,
    __in BOOL fValue)
{
    VARIANT vtPut;

    ::VariantInit(&vtPut);
    vtPut.vt = VT_BOOL;
    vtPut.boolVal = (fValue == FALSE) ? VARIANT_FALSE : VARIANT_TRUE;
    return Iis7PutPropertyVariant(pElement, wzPropName, vtPut);
}

extern "C" HRESULT DAPI Iis7GetPropertyVariant(
    __in IAppHostElement *pElement,
    __in LPCWSTR wzPropName,
    __in VARIANT* vtGet
    )
{
    HRESULT hr = S_OK;
    IAppHostProperty *pProperty = NULL;
    BSTR bstrPropName = NULL;

    bstrPropName = ::SysAllocString(wzPropName);
    ExitOnNull(bstrPropName, hr, E_OUTOFMEMORY, "failed SysAllocString");

    hr = pElement->GetPropertyByName(bstrPropName, &pProperty);
    ExitOnFailure1(hr, "Failed to get property object for %ls", wzPropName);

    hr = pProperty->get_Value(vtGet);
    ExitOnFailure1(hr, "Failed to get property value for %ls", wzPropName);

LExit:
    ReleaseBSTR(bstrPropName);
    // caller responsible for cleaning up variant vtGet
    ReleaseObject(pProperty);

    return hr;
}

extern "C" HRESULT DAPI Iis7GetPropertyString(
    __in IAppHostElement *pElement,
    __in LPCWSTR wzPropName,
    __in LPWSTR* psczGet
    )
{
    HRESULT hr = S_OK;
    VARIANT vtGet;

    ::VariantInit(&vtGet);
    hr = Iis7GetPropertyVariant(pElement, wzPropName, &vtGet);
    ExitOnFailure1(hr, "Failed to get iis7 property variant with name: %ls", wzPropName);

    if (!ISSTRINGVARIANT(vtGet.vt))
    {
        hr = E_UNEXPECTED;
        ExitOnFailure1(hr, "Tried to get property as a string, but type was %d instead.", vtGet.vt);
    }

    hr = StrAllocString(psczGet, vtGet.bstrVal, 0);

LExit:
    ReleaseVariant(vtGet);

    return hr;
}

BOOL DAPI CompareVariantDefault(
    __in VARIANT* pVariant1,
    __in VARIANT* pVariant2
    )
{
    BOOL fEqual = FALSE;

    switch (pVariant1->vt)
    {
        // VarCmp doesn't work for unsigned ints
        // We'd like to allow signed/unsigned comparison as well since
        // IIS doesn't document variant type for integer fields
    case VT_I1:
    case VT_UI1:
        if (VT_I1 == pVariant2->vt || VT_UI1 == pVariant2->vt)
        {
            fEqual = pVariant1->bVal == pVariant2->bVal;
        }
        break;
    case VT_I2:
    case VT_UI2:
        if (VT_I2 == pVariant2->vt || VT_UI2 == pVariant2->vt)
        {
            fEqual = pVariant1->uiVal == pVariant2->uiVal;
        }
        break;
    case VT_UI4:
    case VT_I4:
        if (VT_I4 == pVariant2->vt || VT_UI4 == pVariant2->vt)
        {
            fEqual = pVariant1->ulVal == pVariant2->ulVal;
        }
        break;
    case VT_UI8:
    case VT_I8:
        if (VT_I8 == pVariant2->vt || VT_UI8 == pVariant2->vt)
        {
            fEqual = pVariant1->ullVal == pVariant2->ullVal;
        }
        break;
    default:
        fEqual = VARCMP_EQ == ::VarCmp(pVariant1,
                                       pVariant2,
                                       LOCALE_INVARIANT,
                                       NORM_IGNORECASE);
    }

    return fEqual;
}

BOOL DAPI CompareVariantPath(
    __in VARIANT* pVariant1,
    __in VARIANT* pVariant2
    )
{
    HRESULT hr = S_OK;
    BOOL fEqual = FALSE;
    LPWSTR wzValue1 = NULL;
    LPWSTR wzValue2 = NULL;

    if (ISSTRINGVARIANT(pVariant1->vt))
    {
        hr = PathExpand(&wzValue1, pVariant1->bstrVal, PATH_EXPAND_ENVIRONMENT | PATH_EXPAND_FULLPATH);
        ExitOnFailure1(hr, "Failed to expand path %ls", pVariant1->bstrVal);
    }

    if (ISSTRINGVARIANT(pVariant2->vt))
    {
        hr = PathExpand(&wzValue2, pVariant2->bstrVal, PATH_EXPAND_ENVIRONMENT | PATH_EXPAND_FULLPATH);
        ExitOnFailure1(hr, "Failed to expand path %ls", pVariant2->bstrVal);
    }

    fEqual = CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzValue1, -1, wzValue2, -1);

LExit:
    ReleaseNullStr(wzValue1);
    ReleaseNullStr(wzValue2);
    return fEqual;
}

BOOL DAPI IsMatchingAppHostElementCallback(
    __in IAppHostElement *pElement,
    __in_bcount(sizeof(IIS7_APPHOSTELEMENTCOMPARISON)) LPVOID pContext
    )
{
    IIS7_APPHOSTELEMENTCOMPARISON* pComparison = (IIS7_APPHOSTELEMENTCOMPARISON*) pContext;

    return Iis7IsMatchingAppHostElement(pElement, pComparison);
}

extern "C" BOOL DAPI Iis7IsMatchingAppHostElement(
    __in IAppHostElement *pElement,
    __in IIS7_APPHOSTELEMENTCOMPARISON* pComparison
    )
{
    HRESULT hr = S_OK;
    BOOL fResult = FALSE;
    IAppHostProperty *pProperty = NULL;
    BSTR bstrElementName = NULL;

    VARIANT vPropValue;
    ::VariantInit(&vPropValue);

    // Use the default comparator if a comparator is not specified
    VARIANTCOMPARATORPROC pComparator = pComparison->pComparator ? pComparison->pComparator : CompareVariantDefault;

    hr = pElement->get_Name(&bstrElementName);
    ExitOnFailure(hr, "Failed to get name of element");
    if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pComparison->sczElementName, -1, bstrElementName, -1))
    {
        ExitFunction();
    }

    hr = Iis7GetPropertyVariant(pElement, pComparison->sczAttributeName, &vPropValue);
    ExitOnFailure2(hr, "Failed to get value of %ls attribute of %ls element", pComparison->sczAttributeName, pComparison->sczElementName);

    if (TRUE == pComparator(pComparison->pvAttributeValue, &vPropValue))
    {
        fResult = TRUE;
    }

LExit:
    ReleaseBSTR(bstrElementName);
    ReleaseVariant(vPropValue);
    ReleaseObject(pProperty);

    return fResult;
}

BOOL DAPI IsMatchingAppHostMethod(
    __in IAppHostMethod *pMethod,
    __in LPCWSTR wzMethodName
   )
{
    HRESULT hr = S_OK;
    BOOL fResult = FALSE;
    BSTR bstrName = NULL;

    hr = pMethod->get_Name(&bstrName);
    ExitOnFailure(hr, "Failed to get name of element");

    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzMethodName, -1, bstrName, -1))
    {
        fResult = TRUE;
    }

LExit:
    ReleaseBSTR(bstrName);

    return fResult;
}

extern "C" HRESULT DAPI Iis7FindAppHostElementPath(
    __in IAppHostElementCollection *pCollection,
    __in LPCWSTR wzElementName,
    __in LPCWSTR wzAttributeName,
    __in LPCWSTR wzAttributeValue,
    __out IAppHostElement** ppElement,
    __out DWORD* pdwIndex
    )
{
    HRESULT hr = S_OK;
    IIS7_APPHOSTELEMENTCOMPARISON comparison = { };
    VARIANT vtValue = { };
    ::VariantInit(&vtValue);

    vtValue.vt = VT_BSTR;
    vtValue.bstrVal = ::SysAllocString(wzAttributeValue);
    ExitOnNull(vtValue.bstrVal, hr, E_OUTOFMEMORY, "failed SysAllocString");

    comparison.sczElementName = wzElementName;
    comparison.sczAttributeName = wzAttributeName;
    comparison.pvAttributeValue = &vtValue;
    comparison.pComparator = CompareVariantPath;

    hr = Iis7EnumAppHostElements(pCollection,
                                 IsMatchingAppHostElementCallback,
                                 &comparison,
                                 ppElement,
                                 pdwIndex);

LExit:
    ReleaseVariant(vtValue);

    return hr;
}

extern "C" HRESULT DAPI Iis7FindAppHostElementString(
    __in IAppHostElementCollection *pCollection,
    __in LPCWSTR wzElementName,
    __in LPCWSTR wzAttributeName,
    __in LPCWSTR wzAttributeValue,
    __out IAppHostElement** ppElement,
    __out DWORD* pdwIndex
    )
{
    HRESULT hr = S_OK;
    VARIANT vtValue;
    ::VariantInit(&vtValue);

    vtValue.vt = VT_BSTR;
    vtValue.bstrVal = ::SysAllocString(wzAttributeValue);
    ExitOnNull(vtValue.bstrVal, hr, E_OUTOFMEMORY, "failed SysAllocString");

    hr = Iis7FindAppHostElementVariant(pCollection,
                                       wzElementName,
                                       wzAttributeName,
                                       &vtValue,
                                       ppElement,
                                       pdwIndex);

LExit:
    ReleaseVariant(vtValue);

    return hr;
}

extern "C" HRESULT DAPI Iis7FindAppHostElementInteger(
    __in IAppHostElementCollection *pCollection,
    __in LPCWSTR wzElementName,
    __in LPCWSTR wzAttributeName,
    __in DWORD dwAttributeValue,
    __out IAppHostElement** ppElement,
    __out DWORD* pdwIndex
    )
{
    HRESULT hr = S_OK;
    VARIANT vtValue;
    ::VariantInit(&vtValue);

    vtValue.vt = VT_UI4;
    vtValue.ulVal = dwAttributeValue;

    hr = Iis7FindAppHostElementVariant(pCollection,
                                       wzElementName,
                                       wzAttributeName,
                                       &vtValue,
                                       ppElement,
                                       pdwIndex);

    ReleaseVariant(vtValue);

    return hr;
}

extern "C" HRESULT DAPI Iis7FindAppHostElementVariant(
    __in IAppHostElementCollection *pCollection,
    __in LPCWSTR wzElementName,
    __in LPCWSTR wzAttributeName,
    __in VARIANT* pvAttributeValue,
    __out IAppHostElement** ppElement,
    __out DWORD* pdwIndex
    )
{
    IIS7_APPHOSTELEMENTCOMPARISON comparison = { };
    comparison.sczElementName = wzElementName;
    comparison.sczAttributeName = wzAttributeName;
    comparison.pvAttributeValue = pvAttributeValue;
    comparison.pComparator = CompareVariantDefault;

    return Iis7EnumAppHostElements(pCollection,
                                   IsMatchingAppHostElementCallback,
                                   &comparison,
                                   ppElement,
                                   pdwIndex);
}

extern "C" HRESULT DAPI Iis7EnumAppHostElements(
    __in IAppHostElementCollection *pCollection,
    __in ENUMAPHOSTELEMENTPROC pCallback,
    __in LPVOID pContext,
    __out IAppHostElement** ppElement,
    __out DWORD* pdwIndex
    )
{
    HRESULT hr = S_OK;
    IAppHostElement *pElement = NULL;
    DWORD dwElements = 0;

    VARIANT vtIndex;
    ::VariantInit(&vtIndex);

    if (NULL != ppElement)
    {
        *ppElement = NULL;
    }
    if (NULL != pdwIndex)
    {
        *pdwIndex = MAXDWORD;
    }

    hr = pCollection->get_Count(&dwElements);
    ExitOnFailure(hr, "Failed get application IAppHostElementCollection count");

    vtIndex.vt = VT_UI4;
    for (DWORD i = 0; i < dwElements; ++i)
    {
        vtIndex.ulVal = i;
        hr = pCollection->get_Item(vtIndex , &pElement);
        ExitOnFailure(hr, "Failed get IAppHostElement element");

        if (pCallback(pElement, pContext))
        {
            if (NULL != ppElement)
            {
                *ppElement = pElement;
                pElement = NULL;
            }
            if (NULL != pdwIndex)
            {
                *pdwIndex = i;
            }
            break;
        }

        ReleaseNullObject(pElement);
    }

LExit:
    ReleaseObject(pElement);
    ReleaseVariant(vtIndex);

    return hr;
}

extern "C" HRESULT DAPI Iis7FindAppHostMethod(
    __in IAppHostMethodCollection *pCollection,
    __in LPCWSTR wzMethodName,
    __out IAppHostMethod** ppMethod,
    __out DWORD* pdwIndex
    )
{
    HRESULT hr = S_OK;
    IAppHostMethod *pMethod = NULL;
    DWORD dwMethods = 0;

    VARIANT vtIndex;
    ::VariantInit(&vtIndex);

    if (NULL != ppMethod)
    {
        *ppMethod = NULL;
    }
    if (NULL != pdwIndex)
    {
        *pdwIndex = MAXDWORD;
    }

    hr = pCollection->get_Count(&dwMethods);
    ExitOnFailure(hr, "Failed get application IAppHostMethodCollection count");

    vtIndex.vt = VT_UI4;
    for (DWORD i = 0; i < dwMethods; ++i)
    {
        vtIndex.ulVal = i;
        hr = pCollection->get_Item(vtIndex , &pMethod);
        ExitOnFailure(hr, "Failed get IAppHostMethod element");

        if (IsMatchingAppHostMethod(pMethod, wzMethodName))
        {
            if (NULL != ppMethod)
            {
                *ppMethod = pMethod;
                pMethod = NULL;
            }
            if (NULL != pdwIndex)
            {
                *pdwIndex = i;
            }
            break;
        }

        ReleaseNullObject(pMethod);
    }

LExit:
    ReleaseObject(pMethod);
    ReleaseVariant(vtIndex);

    return hr;
}
