//-------------------------------------------------------------------------------------------------
// <copyright file="download.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//   Download code for update executable for ClickThrough.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define RETRY_COUNT 20


HRESULT RetryInternetReadFile(
    __in HINTERNET hiUrl,
    __in LPVOID lpBuffer,
    __in DWORD dwNumberOfBytesToRead,
    __out LPDWORD lpdwNumberOfBytesRead
    )
{
    HRESULT hr = S_FALSE;
    DWORD dwLastError;
    int retry;
    for(retry=0; retry<RETRY_COUNT; retry++)
    {
        if(::InternetReadFile(hiUrl, lpBuffer, dwNumberOfBytesToRead, lpdwNumberOfBytesRead))
        {
            hr = S_OK;
            break;
        }
        else
        {
            dwLastError = ::GetLastError();
            hr = HRESULT_FROM_WIN32(dwLastError);
            if (dwLastError != ERROR_INTERNET_CONNECTION_RESET)
            {
                break;
            }
        }
    }
    ExitOnFailure(hr, "Failed while reading from internet.");

LExit:
    return hr;
}


HRESULT Download(
    __in_opt LPCWSTR wzBasePath,
    __in LPCWSTR wzSourcePath,
    __in LPCWSTR wzDestPath
    )
{
    HRESULT hr = S_OK;

    HANDLE hFile = INVALID_HANDLE_VALUE;
    HINTERNET hiSession = NULL;
    HINTERNET hiUrl = NULL;

    BYTE* pbData = NULL;
    DWORD cbMaxData = 0;
    DWORD cbData = 0;
    DWORD cbBytesWritten = 0;

    WCHAR wzUrl[INTERNET_MAX_URL_LENGTH];
    DWORD cch = 0;
    URI_PROTOCOL protocol = URI_PROTOCOL_UNKNOWN;
    LPWSTR pwzUrl = NULL;

    DWORD dwCode = 0;
    DWORD cbCode = sizeof(dwCode);
    DWORD dwIndex = 0;

    DWORD dwConnected = 0;
    if (!::InternetGetConnectedState(&dwConnected, 0))
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_CONNECTED);
        ExitOnRootFailure1(hr, "failed because there is no connection to URL: %S", wzUrl);
    }

    hiSession = ::InternetOpenW(L"ClickThrough", INTERNET_OPEN_TYPE_PRECONFIG, NULL, NULL, 0);
    if (!hiSession)
    {
        ExitWithLastError(hr, "failed to open internet session");
    }

    cch = countof(wzUrl);
    if (!::InternetCanonicalizeUrlW(wzSourcePath, wzUrl, &cch, 0))
    {
        ExitWithLastError1(hr, "failed to canonicalize url: %S", wzSourcePath);
    }

    hr = UriResolve(wzUrl, wzBasePath, &pwzUrl, &protocol);
    ExitOnFailure1(hr, "Failed to resolve URL: %S", wzUrl);

    // open the HTTP or FILE URL
    if (URI_PROTOCOL_FILE == protocol)
    {
        LPCWSTR pwzFilePrefix = L"file://";
        LPCWSTR pwzFilePath = NULL;
        const size_t cchFilePrefix = wcslen(pwzFilePrefix);
        BOOL fCopySucceeded;

        // Remove the "file://" prefix.
        if (cchFilePrefix < cch)
        {
            if (CSTR_EQUAL == CompareStringW(LOCALE_SYSTEM_DEFAULT, NORM_IGNORECASE, wzUrl, cchFilePrefix, pwzFilePrefix, cchFilePrefix))
            {
                pwzFilePath = wzUrl + cchFilePrefix;
            }
        }
        ExitOnNull1(pwzFilePath, hr, E_UNEXPECTED, "Unable to parse file url: %S", wzUrl);

        fCopySucceeded = CopyFileW(pwzFilePath, wzDestPath, FALSE);
        if (FALSE == fCopySucceeded)
        {
            ExitOnFailure2(ERROR_CANNOT_COPY, "Unable to copy from %S to %S", pwzFilePath, wzDestPath);
        }
    }
    else if (URI_PROTOCOL_HTTP == protocol)
    {
        BOOL fRetry = FALSE;
        do
        {
            // if url  was open close it, so we can reopen it again
            if (hiUrl)
            {
                ::InternetCloseHandle(hiUrl);
            }

            // open the url
            hiUrl = ::InternetOpenUrlW(hiSession, pwzUrl, NULL, 0, INTERNET_FLAG_KEEP_CONNECTION | INTERNET_FLAG_NO_UI | INTERNET_FLAG_NO_COOKIES | INTERNET_FLAG_RELOAD, NULL);
            if (!hiUrl)
            {
                ExitWithLastError1(hr, "failed to open url: %S", wzUrl);
            }

            // check the http status code
            if (!::HttpQueryInfoW(hiUrl, HTTP_QUERY_STATUS_CODE | HTTP_QUERY_FLAG_NUMBER, (LPVOID*)&dwCode, &cbCode, &dwIndex))
            {
                ExitWithLastError1(hr, "failed to get header information for url: %S", wzUrl);
            }

            switch (dwCode)
            {
            case 200:
                fRetry = FALSE;   // we're done
                break;

                // redirection cases
            case 301:  // file moved
            case 302:  // temporary
            case 303:  // redirect method
                // get the location to redirect to
                cch = INTERNET_MAX_URL_LENGTH;
                dwIndex = 0;
                if (!::HttpQueryInfoW(hiUrl, HTTP_QUERY_CONTENT_LOCATION, (LPVOID*)wzUrl, &cch, &dwIndex))
                {
                    ExitWithLastError1(hr, "failed to get redirect location for url: %S", wzSourcePath);
                }

                Trace2(REPORT_STANDARD, "URL %S redirected to %S", pwzUrl, wzUrl);
                fRetry = TRUE;
                break;

                // error cases
            case 401:   // unauthorized
            case 403:   // access denied
                hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
                TraceError2(hr, "HTTP %d, access denied to url: %S", dwCode, pwzUrl);
                break;
            case 404:   // file not found
            case 502:   // server (through a gateyway) was not found
            case 503:   // server unavailable
                hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                TraceError2(hr, "HTTP %d, could not locate url: %S", dwCode, pwzUrl);
                break;
            case 408:   // request timedout
            case 504:   // gateway timeout
                hr = HRESULT_FROM_WIN32(WAIT_TIMEOUT);
                TraceError2(hr, "HTTP %d, timed out while trying to connect to url: %S", dwCode, pwzUrl);
                break;
            default:
                {
                    hr = E_UNEXPECTED;
#if DEBUG
                    CHAR sz[INTERNET_MAX_URL_LENGTH];
                    StringCchPrintfA(sz, countof(sz), "unhandled HTTP status %d, unknown status code for URL: %S", dwCode, pwzUrl);
                    AssertSz(FALSE, sz);
#endif
                }
                break;
            }
        } while (S_OK == hr && fRetry);
        ExitOnFailure1(hr, "failed to open source file: %S", pwzUrl);

        // cbMaxData = (visGlobal.dwInternetReadBufferSize + visGlobal.dwPageSize - 1) & ~(visGlobal.dwPageSize - 1);   // actually rounds up to the next nearest page size
        cbMaxData = 1024*1024; // !!!
        pbData = static_cast<BYTE*>(::VirtualAlloc(NULL, cbMaxData, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE));
        if (!pbData)
        {
            ExitWithLastError(hr, "failed to allocate buffer to cache resource");
        }

        hFile = ::CreateFileW(wzDestPath, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == hFile)
        {
            ExitWithLastError1(hr, "Failed to CreateFile %ls.", wzDestPath);
        }

        //
        // read the source file
        //
        while (S_OK == hr)
        {
            // read from the file
            hr = RetryInternetReadFile(hiUrl, static_cast<void*>(pbData), cbMaxData, &cbData);
            ExitOnFailure1(hr, "failed while reading from url: %S", pwzUrl);

            // if there is data to be written
            if (cbData)
            {
                if (!::WriteFile(hFile, pbData, cbData, &cbBytesWritten, NULL))
                {
                    ExitWithLastError1(hr, "Failed to WriteFile to %ls.", wzDestPath);
                }

                if (cbData > cbBytesWritten)
                {
                    ExitWithLastError1(hr, "Filed to write all bytes to %ls.", wzDestPath);
                }
            }
            else // end of file
            {
                ::CloseHandle(hFile);
                hFile = INVALID_HANDLE_VALUE;
                break;
            }
        }
    }
    else
    {
        ExitOnFailure1(E_UNEXPECTED, "failed to open source file: %S", pwzUrl);
    }
    Assert(SUCCEEDED(hr));

LExit:
    if (pbData)
    {
        ::VirtualFree(pbData, 0, MEM_RELEASE);
    }

    if (hiUrl)
    {
        ::InternetCloseHandle(hiUrl);
    }

    if (hiSession)
    {
        ::InternetCloseHandle(hiSession);
    }

    if (INVALID_HANDLE_VALUE != hFile)
    {
        ::CloseHandle(hFile);
    }

    ReleaseStr(pwzUrl);

    return hr;
}
