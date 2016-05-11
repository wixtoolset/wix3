// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
