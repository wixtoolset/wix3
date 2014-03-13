//-------------------------------------------------------------------------------------------------
// <copyright file="update.cpp" company="Outercurve Foundation">
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


// internal function declarations


// function definitions

extern "C" HRESULT UpdateParseFromXml(
    __in BURN_UPDATE* pUpdate,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnUpdateNode = NULL;

    hr = XmlSelectSingleNode(pixnBundle, L"Update", &pixnUpdateNode);
    if (S_FALSE == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to select Bundle/Update node.");

    // @Location
    hr = XmlGetAttributeEx(pixnUpdateNode, L"Location", &pUpdate->sczUpdateSource);
    ExitOnFailure(hr, "Failed to get Update@Location.");

LExit:
    ReleaseObject(pixnUpdateNode);

    return hr;
}

extern "C" void UpdateUninitialize(
    __in BURN_UPDATE* pUpdate
    )
{
    PackageUninitialize(&pUpdate->package);

    ReleaseStr(pUpdate->sczUpdateSource);
    memset(pUpdate, 0, sizeof(BURN_UPDATE));
}
