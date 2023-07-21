// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// function definitions

HRESULT PcaGuidToRegFormat(
    LPWSTR pwzGuid,
    LPWSTR pwzDest,
    SIZE_T cchDest
    )
{
    Assert(cchDest < INT_MAX);
  
    HRESULT hr = S_OK;
    GUID guid = GUID_NULL;
    WCHAR wz[39];
    ::ZeroMemory(wz, sizeof(wz));

    int cch = lstrlenW(pwzGuid);

    if (38 == cch && L'{' == pwzGuid[0] && L'}' == pwzGuid[37])
    {
        StringCchCopyW(wz, countof(wz), pwzGuid);
    }
    else if (36 == cch)
    {
        StringCchPrintfW(wz, countof(wz), L"{%s}", pwzGuid);
    }
    else
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    // convert string to guid
    hr = ::CLSIDFromString(wz, &guid);
    ExitOnFailure(hr, "Failed to parse guid string");

    // convert guid to string
    if (0 == ::StringFromGUID2(guid, pwzDest, static_cast<int>(cchDest)))
    {
        ExitOnFailure(hr = E_FAIL, "Failed to convert guid to string");
    }

    hr = S_OK;

LExit:
    return hr;
}
