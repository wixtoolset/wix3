// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


/*******************************************************************
 InternetGetSizeByHandle - returns size of file by url handle

*******************************************************************/
extern "C" HRESULT DAPI InternetGetSizeByHandle(
    __in HINTERNET hiFile,
    __out LONGLONG* pllSize
    )
{
    Assert(pllSize);

    HRESULT hr = S_OK;
    DWORD dwSize;
    DWORD cb;

    cb = sizeof(dwSize);
    if (!::HttpQueryInfoW(hiFile, HTTP_QUERY_CONTENT_LENGTH | HTTP_QUERY_FLAG_NUMBER, reinterpret_cast<LPVOID>(&dwSize), &cb, NULL))
    {
        ExitOnLastError(hr, "Failed to get size for internet file handle");
    }

    *pllSize = dwSize;
LExit:
    return hr;
}


/*******************************************************************
 InetGetCreateTimeByHandle - returns url creation time

******************************************************************/
extern "C" HRESULT DAPI InternetGetCreateTimeByHandle(
    __in HINTERNET hiFile,
    __out LPFILETIME pft
    )
{
    Assert(pft);

    HRESULT hr = S_OK;
    SYSTEMTIME st = {0 };
    DWORD cb = sizeof(SYSTEMTIME);

    if (!::HttpQueryInfoW(hiFile, HTTP_QUERY_LAST_MODIFIED | HTTP_QUERY_FLAG_SYSTEMTIME, reinterpret_cast<LPVOID>(&st), &cb, NULL))
    {
        ExitWithLastError(hr, "failed to get create time for internet file handle");
    }

    if (!::SystemTimeToFileTime(&st, pft))
    {
        ExitWithLastError(hr, "failed to convert system time to file time");
    }

LExit:
    return hr;
}


/*******************************************************************
 InternetQueryInfoString - query info string

*******************************************************************/
extern "C" HRESULT DAPI InternetQueryInfoString(
    __in HINTERNET hRequest,
    __in DWORD dwInfo,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    DWORD_PTR cbValue = 0;
    DWORD dwIndex = 0;

    // If nothing was provided start off with some arbitrary size.
    if (!*psczValue)
    {
        hr = StrAlloc(psczValue, 64);
        ExitOnFailure(hr, "Failed to allocate memory for value.");
    }

    hr = StrSize(*psczValue, &cbValue);
    ExitOnFailure(hr, "Failed to get size of value.");

    if (!::HttpQueryInfoW(hRequest, dwInfo, static_cast<void*>(*psczValue), reinterpret_cast<DWORD*>(&cbValue), &dwIndex))
    {
        DWORD er = ::GetLastError();
        if (ERROR_INSUFFICIENT_BUFFER == er)
        {
            cbValue += sizeof(WCHAR); // add one character for the null terminator.

            hr = StrAlloc(psczValue, cbValue / sizeof(WCHAR));
            ExitOnFailure(hr, "Failed to allocate value.");

            if (!::HttpQueryInfoW(hRequest, dwInfo, static_cast<void*>(*psczValue), reinterpret_cast<DWORD*>(&cbValue), &dwIndex))
            {
                er = ::GetLastError();
            }
            else
            {
                er = ERROR_SUCCESS;
            }
        }

        hr = HRESULT_FROM_WIN32(er);
        ExitOnRootFailure(hr, "Failed to get query information.");
    }

LExit:
    return hr;
}


/*******************************************************************
 InternetQueryInfoNumber - query info number

*******************************************************************/
extern "C" HRESULT DAPI InternetQueryInfoNumber(
    __in HINTERNET hRequest,
    __in DWORD dwInfo,
    __out LONG* plInfo
    )
{
    HRESULT hr = S_OK;
    DWORD cbCode = sizeof(LONG);
    DWORD dwIndex = 0;

    if (!::HttpQueryInfoW(hRequest, dwInfo | HTTP_QUERY_FLAG_NUMBER, static_cast<void*>(plInfo), &cbCode, &dwIndex))
    {
        ExitWithLastError(hr, "Failed to get query information.");
    }

LExit:
    return hr;
}
