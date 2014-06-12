//-------------------------------------------------------------------------------------------------
// <copyright file="ManifestHelpers.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Manifest helper functions for unit tests for Burn.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


using namespace System;
using namespace Xunit;


namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    void LoadBundleXmlHelper(LPCWSTR wzDocument, IXMLDOMElement** ppixeBundle)
    {
        HRESULT hr = S_OK;
        IXMLDOMDocument* pixdDocument = NULL;
        try
        {
            hr = XmlLoadDocument(wzDocument, &pixdDocument);
            TestThrowOnFailure(hr, L"Failed to load XML document.");

            hr = pixdDocument->get_documentElement(ppixeBundle);
            TestThrowOnFailure(hr, L"Failed to get bundle element.");
        }
        finally
        {
            ReleaseObject(pixdDocument);
        }
    }
}
}
}
}
}
