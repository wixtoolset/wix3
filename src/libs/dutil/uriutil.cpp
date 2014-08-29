//-------------------------------------------------------------------------------------------------
// <copyright file="uriutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    URI helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


//
// UriCanonicalize - canonicalizes a URI.
//
extern "C" HRESULT DAPI UriCanonicalize(
    __inout_z LPWSTR* psczUri
    )
{
    HRESULT hr = S_OK;
    WCHAR wz[INTERNET_MAX_URL_LENGTH] = { };
    DWORD cch = countof(wz);

    if (!::InternetCanonicalizeUrlW(*psczUri, wz, &cch, ICU_DECODE))
    {
        ExitWithLastError(hr, "Failed to canonicalize URI.");
    }

    hr = StrAllocString(psczUri, wz, cch);
    ExitOnFailure(hr, "Failed copy canonicalized URI.");

LExit:
    return hr;
}


//
// UriCrack - cracks a URI into constituent parts.
//
extern "C" HRESULT DAPI UriCrack(
    __in_z LPCWSTR wzUri,
    __out_opt INTERNET_SCHEME* pScheme,
    __deref_opt_out_z LPWSTR* psczHostName,
    __out_opt INTERNET_PORT* pPort,
    __deref_opt_out_z LPWSTR* psczUser,
    __deref_opt_out_z LPWSTR* psczPassword,
    __deref_opt_out_z LPWSTR* psczPath,
    __deref_opt_out_z LPWSTR* psczQueryString
    )
{
    HRESULT hr = S_OK;
    URL_COMPONENTSW components = { };
    WCHAR wzHostName[INTERNET_MAX_HOST_NAME_LENGTH + 1];
    WCHAR wzUserName[INTERNET_MAX_USER_NAME_LENGTH + 1];
    WCHAR wzPassword[INTERNET_MAX_PASSWORD_LENGTH + 1];
    WCHAR wzPath[INTERNET_MAX_PATH_LENGTH + 1];
    WCHAR wzQueryString[INTERNET_MAX_PATH_LENGTH + 1];

    components.dwStructSize = sizeof(URL_COMPONENTSW);

    if (psczHostName)
    {
        components.lpszHostName = wzHostName;
        components.dwHostNameLength = countof(wzHostName);
    }

    if (psczUser)
    {
        components.lpszUserName = wzUserName;
        components.dwUserNameLength = countof(wzUserName);
    }

    if (psczPassword)
    {
        components.lpszPassword = wzPassword;
        components.dwPasswordLength = countof(wzPassword);
    }

    if (psczPath)
    {
        components.lpszUrlPath = wzPath;
        components.dwUrlPathLength = countof(wzPath);
    }

    if (psczQueryString)
    {
        components.lpszExtraInfo = wzQueryString;
        components.dwExtraInfoLength = countof(wzQueryString);
    }

    if (!::InternetCrackUrlW(wzUri, 0, ICU_DECODE | ICU_ESCAPE, &components))
    {
        ExitWithLastError(hr, "Failed to crack URI.");
    }

    if (pScheme)
    {
        *pScheme = components.nScheme;
    }

    if (psczHostName)
    {
        hr = StrAllocString(psczHostName, components.lpszHostName, components.dwHostNameLength);
        ExitOnFailure(hr, "Failed to copy host name.");
    }

    if (pPort)
    {
        *pPort = components.nPort;
    }

    if (psczUser)
    {
        hr = StrAllocString(psczUser, components.lpszUserName, components.dwUserNameLength);
        ExitOnFailure(hr, "Failed to copy user name.");
    }

    if (psczPassword)
    {
        hr = StrAllocString(psczPassword, components.lpszPassword, components.dwPasswordLength);
        ExitOnFailure(hr, "Failed to copy password.");
    }

    if (psczPath)
    {
        hr = StrAllocString(psczPath, components.lpszUrlPath, components.dwUrlPathLength);
        ExitOnFailure(hr, "Failed to copy path.");
    }

    if (psczQueryString)
    {
        hr = StrAllocString(psczQueryString, components.lpszExtraInfo, components.dwExtraInfoLength);
        ExitOnFailure(hr, "Failed to copy query string.");
    }

LExit:
    return hr;
}


//
// UriCrackEx - cracks a URI into URI_INFO.
//
extern "C" HRESULT DAPI UriCrackEx(
    __in_z LPCWSTR wzUri,
    __in URI_INFO* pUriInfo
    )
{
    HRESULT hr = S_OK;

    hr = UriCrack(wzUri, &pUriInfo->scheme, &pUriInfo->sczHostName, &pUriInfo->port, &pUriInfo->sczUser, &pUriInfo->sczPassword, &pUriInfo->sczPath, &pUriInfo->sczQueryString);
    ExitOnFailure(hr, "Failed to crack URI.");

LExit:
    return hr;
}


//
// UriInfoUninitialize - frees the memory in a URI_INFO struct.
//
extern "C" void DAPI UriInfoUninitialize(
    __in URI_INFO* pUriInfo
    )
{
    ReleaseStr(pUriInfo->sczHostName);
    ReleaseStr(pUriInfo->sczUser);
    ReleaseStr(pUriInfo->sczPassword);
    ReleaseStr(pUriInfo->sczPath);
    ReleaseStr(pUriInfo->sczQueryString);
    memset(pUriInfo, 0, sizeof(URI_INFO));
}


//
// UriCreate - creates a URI from constituent parts.
//
extern "C" HRESULT DAPI UriCreate(
    __inout_z LPWSTR* psczUri,
    __in INTERNET_SCHEME scheme,
    __in_z_opt LPWSTR wzHostName,
    __in INTERNET_PORT port,
    __in_z_opt LPWSTR wzUser,
    __in_z_opt LPWSTR wzPassword,
    __in_z_opt LPWSTR wzPath,
    __in_z_opt LPWSTR wzQueryString
    )
{
    HRESULT hr = S_OK;
    WCHAR wz[INTERNET_MAX_URL_LENGTH] = { };
    DWORD cch = countof(wz);
    URL_COMPONENTSW components = { };

    components.dwStructSize = sizeof(URL_COMPONENTSW);
    components.nScheme = scheme;
    components.lpszHostName = wzHostName;
    components.nPort = port;
    components.lpszUserName = wzUser;
    components.lpszPassword = wzPassword;
    components.lpszUrlPath = wzPath;
    components.lpszExtraInfo = wzQueryString;

    if (!::InternetCreateUrlW(&components, ICU_ESCAPE, wz, &cch))
    {
        ExitWithLastError(hr, "Failed to create URI.");
    }

    hr = StrAllocString(psczUri, wz, cch);
    ExitOnFailure(hr, "Failed copy created URI.");

LExit:
    return hr;
}


//
// UriGetServerAndResource - gets the server and resource as independent strings from a URI.
//
// NOTE: This function is useful for the InternetConnect/HttpRequest APIs.
//
extern "C" HRESULT DAPI UriGetServerAndResource(
    __in_z LPCWSTR wzUri,
    __out_z LPWSTR* psczServer,
    __out_z LPWSTR* psczResource
    )
{
    HRESULT hr = S_OK;
    INTERNET_SCHEME scheme = INTERNET_SCHEME_UNKNOWN;
    LPWSTR sczHostName = NULL;
    INTERNET_PORT port = INTERNET_INVALID_PORT_NUMBER;
    LPWSTR sczUser = NULL;
    LPWSTR sczPassword = NULL;
    LPWSTR sczPath = NULL;
    LPWSTR sczQueryString = NULL;

    hr = UriCrack(wzUri, &scheme, &sczHostName, &port, &sczUser, &sczPassword, &sczPath, &sczQueryString);
    ExitOnFailure(hr, "Failed to crack URI.");

    hr = UriCreate(psczServer, scheme, sczHostName, port, sczUser, sczPassword, NULL, NULL);
    ExitOnFailure(hr, "Failed to allocate server URI.");

    hr = UriCreate(psczResource, INTERNET_SCHEME_UNKNOWN, NULL, INTERNET_INVALID_PORT_NUMBER, NULL, NULL, sczPath, sczQueryString);
    ExitOnFailure(hr, "Failed to allocate resource URI.");

LExit:
    ReleaseStr(sczQueryString);
    ReleaseStr(sczPath);
    ReleaseStr(sczPassword);
    ReleaseStr(sczUser);
    ReleaseStr(sczHostName);

    return hr;
}


//
// UriFile - returns the file part of the URI.
//
extern "C" HRESULT DAPI UriFile(
    __deref_out_z LPWSTR* psczFile,
    __in_z LPCWSTR wzUri
    )
{
    HRESULT hr = S_OK;
    WCHAR wz[MAX_PATH + 1];
    DWORD cch = countof(wz);
    URL_COMPONENTSW uc = { };

    uc.dwStructSize = sizeof(uc);
    uc.lpszUrlPath = wz;
    uc.dwUrlPathLength = cch;

    if (!::InternetCrackUrlW(wzUri, 0, ICU_DECODE | ICU_ESCAPE, &uc))
    {
        ExitWithLastError(hr, "Failed to crack URI.");
    }

    // Copy only the file name. Fortunately, PathFile() understands that
    // forward slashes can be directory separators like backslashes.
    hr = StrAllocString(psczFile, PathFile(wz), 0);
    ExitOnFailure(hr, "Failed to copy file name");

LExit:
    return hr;
}


/*******************************************************************
 UriProtocol - determines the protocol of a URI.

********************************************************************/
extern "C" HRESULT DAPI UriProtocol(
    __in_z LPCWSTR wzUri,
    __out URI_PROTOCOL* pProtocol
    )
{
    Assert(wzUri && *wzUri);
    Assert(pProtocol);

    HRESULT hr = S_OK;

    if (wcslen(wzUri) < 6)
    {
        *pProtocol = URI_PROTOCOL_UNKNOWN;
    }
    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzUri, 6, L"ftp://", 6))
    {
        *pProtocol = URI_PROTOCOL_FTP;
    }
    else if (wcslen(wzUri) < 7)
    {
        *pProtocol = URI_PROTOCOL_UNKNOWN;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzUri, 7, L"file://", 7))
    {
        *pProtocol = URI_PROTOCOL_FILE;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzUri, 7, L"http://", 7))
    {
        *pProtocol = URI_PROTOCOL_HTTP;
    }
    else if (wcslen(wzUri) < 8)
    {
        *pProtocol = URI_PROTOCOL_UNKNOWN;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzUri, 8, L"https://", 8))
    {
        *pProtocol = URI_PROTOCOL_HTTPS;
    }
    else
    {
        *pProtocol = URI_PROTOCOL_UNKNOWN;
    }

    return hr;
}


/*******************************************************************
 UriRoot - returns the root of the path specified in the URI.

 examples:
    file:///C:\path\path             -> C:\
    file://server/share/path/path    -> \\server\share
    http://www.example.com/path/path -> http://www.example.com/
    ftp://ftp.example.com/path/path  -> ftp://www.example.com/

 NOTE: This function should only be used on cannonicalized URIs.
       It does not cannonicalize itself.
********************************************************************/
extern "C" HRESULT DAPI UriRoot(
    __in_z LPCWSTR wzUri,
    __out LPWSTR* ppwzRoot,
    __out_opt URI_PROTOCOL* pProtocol
    )
{
    Assert(wzUri && *wzUri);
    Assert(ppwzRoot);

    HRESULT hr = S_OK;
    URI_PROTOCOL protocol = URI_PROTOCOL_UNKNOWN;
    LPCWSTR pwcSlash = NULL;

    hr = UriProtocol(wzUri, &protocol);
    ExitOnFailure(hr, "Invalid URI.");

    switch (protocol)
    {
    case URI_PROTOCOL_FILE:
        if (L'/' == wzUri[7]) // file path
        {
            if (((L'a' <= wzUri[8] && L'z' >= wzUri[8]) || (L'A' <= wzUri[8] && L'Z' >= wzUri[8])) && L':' == wzUri[9])
            {
                hr = StrAlloc(ppwzRoot, 4);
                ExitOnFailure(hr, "Failed to allocate string for root of URI.");
                *ppwzRoot[0] = wzUri[8];
                *ppwzRoot[1] = L':';
                *ppwzRoot[2] = L'\\';
                *ppwzRoot[3] = L'\0';
            }
            else
            {
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid file path in URI.");
            }
        }
        else // UNC share
        {
            pwcSlash = wcschr(wzUri + 8, L'/');
            if (!pwcSlash)
            {
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid server name in URI.");
            }
            else
            {
                hr = StrAllocString(ppwzRoot, L"\\\\", 64);
                ExitOnFailure(hr, "Failed to allocate string for root of URI.");

                pwcSlash = wcschr(pwcSlash + 1, L'/');
                if (pwcSlash)
                {
                    hr = StrAllocConcat(ppwzRoot, wzUri + 8, pwcSlash - wzUri - 8);
                    ExitOnFailure(hr, "Failed to add server/share to root of URI.");
                }
                else
                {
                    hr = StrAllocConcat(ppwzRoot, wzUri + 8, 0);
                    ExitOnFailure(hr, "Failed to add server/share to root of URI.");
                }

                // replace all slashes with backslashes to be truly UNC.
                for (LPWSTR pwc = *ppwzRoot; pwc && *pwc; ++pwc)
                {
                    if (L'/' == *pwc)
                    {
                        *pwc = L'\\';
                    }
                }
            }
        }
        break;

    case URI_PROTOCOL_FTP:
        pwcSlash = wcschr(wzUri + 6, L'/');
        if (pwcSlash)
        {
            hr = StrAllocString(ppwzRoot, wzUri, pwcSlash - wzUri);
            ExitOnFailure(hr, "Failed allocate root from URI.");
        }
        else
        {
            hr = StrAllocString(ppwzRoot, wzUri, 0);
            ExitOnFailure(hr, "Failed allocate root from URI.");
        }
        break;

    case URI_PROTOCOL_HTTP:
        pwcSlash = wcschr(wzUri + 7, L'/');
        if (pwcSlash)
        {
            hr = StrAllocString(ppwzRoot, wzUri, pwcSlash - wzUri);
            ExitOnFailure(hr, "Failed allocate root from URI.");
        }
        else
        {
            hr = StrAllocString(ppwzRoot, wzUri, 0);
            ExitOnFailure(hr, "Failed allocate root from URI.");
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unknown URI protocol.");
    }

    if (pProtocol)
    {
        *pProtocol = protocol;
    }

LExit:
    return hr;
}


extern "C" HRESULT DAPI UriResolve(
    __in_z LPCWSTR wzUri,
    __in_opt LPCWSTR wzBaseUri,
    __out LPWSTR* ppwzResolvedUri,
    __out_opt const URI_PROTOCOL* pResolvedProtocol
    )
{
    UNREFERENCED_PARAMETER(wzUri);
    UNREFERENCED_PARAMETER(wzBaseUri);
    UNREFERENCED_PARAMETER(ppwzResolvedUri);
    UNREFERENCED_PARAMETER(pResolvedProtocol);

    HRESULT hr = E_NOTIMPL;
#if 0
    URI_PROTOCOL protocol = URI_PROTOCOL_UNKNOWN;

    hr = UriProtocol(wzUri, &protocol);
    ExitOnFailure1(hr, "Failed to determine protocol for URL: %ls", wzUri);

    ExitOnNull(ppwzResolvedUri, hr, E_INVALIDARG, "Failed to resolve URI, because no method of output was provided");

    if (URI_PROTOCOL_UNKNOWN == protocol)
    {
        ExitOnNull(wzBaseUri, hr, E_INVALIDARG, "Failed to resolve URI - base URI provided was NULL");

        if (L'/' == *wzUri || L'\\' == *wzUri)
        {
            hr = UriRoot(wzBaseUri, ppwzResolvedUri, &protocol);
            ExitOnFailure1(hr, "Failed to get root from URI: %ls", wzBaseUri);

            hr = StrAllocConcat(ppwzResolvedUri, wzUri, 0);
            ExitOnFailure(hr, "Failed to concat file to base URI.");
        }
        else
        {
            hr = UriProtocol(wzBaseUri, &protocol);
            ExitOnFailure1(hr, "Failed to get protocol of base URI: %ls", wzBaseUri);

            LPCWSTR pwcFile = const_cast<LPCWSTR> (UriFile(wzBaseUri));
            if (!pwcFile)
            {
                hr = E_INVALIDARG;
                ExitOnFailure1(hr, "Failed to get file from base URI: %ls", wzBaseUri);
            }

            hr = StrAllocString(ppwzResolvedUri, wzBaseUri, pwcFile - wzBaseUri);
            ExitOnFailure(hr, "Failed to allocate string for resolved URI.");

            hr = StrAllocConcat(ppwzResolvedUri, wzUri, 0);
            ExitOnFailure(hr, "Failed to concat file to resolved URI.");
        }
    }
    else
    {
        hr = StrAllocString(ppwzResolvedUri, wzUri, 0);
        ExitOnFailure(hr, "Failed to copy resolved URI.");
    }

    if (pResolvedProtocol)
    {
        *pResolvedProtocol = protocol;
    }

LExit:
#endif
    return hr;
}
