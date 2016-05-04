// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

struct BALRETRY_INFO
{
    LPWSTR sczId;           // package or container id.
    LPWSTR sczPayloadId;    // optional payload id.
    DWORD cRetries;
    DWORD dwLastError;
};

static DWORD vdwMaxRetries = 0;
static DWORD vdwTimeout = 0;
static BALRETRY_INFO vrgRetryInfo[2];

// prototypes
static BOOL IsActiveRetryEntry(
    __in BALRETRY_TYPE type,
    __in_z LPCWSTR wzPackageId,
    __in_z_opt LPCWSTR wzPayloadId
    );


DAPI_(void) BalRetryInitialize(
    __in DWORD dwMaxRetries,
    __in DWORD dwTimeout
    )
{
    BalRetryUninitialize(); // clean everything out.

    vdwMaxRetries = dwMaxRetries;
    vdwTimeout = dwTimeout;
}


DAPI_(void) BalRetryUninitialize()
{
    for (DWORD i = 0; i < countof(vrgRetryInfo); ++i)
    {
        ReleaseStr(vrgRetryInfo[i].sczId);
        ReleaseStr(vrgRetryInfo[i].sczPayloadId);
        memset(vrgRetryInfo + i, 0, sizeof(BALRETRY_INFO));
    }

    vdwMaxRetries = 0;
    vdwTimeout = 0;
}


DAPI_(void) BalRetryStartPackage(
    __in BALRETRY_TYPE type,
    __in_z_opt LPCWSTR wzPackageId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    if (!wzPackageId || !*wzPackageId)
    {
        ReleaseNullStr(vrgRetryInfo[type].sczId);
        ReleaseNullStr(vrgRetryInfo[type].sczPayloadId);
    }
    else if (IsActiveRetryEntry(type, wzPackageId, wzPayloadId))
    {
        ++vrgRetryInfo[type].cRetries;
        ::Sleep(vdwTimeout);
    }
    else
    {
        StrAllocString(&vrgRetryInfo[type].sczId, wzPackageId, 0);
        if (wzPayloadId)
        {
            StrAllocString(&vrgRetryInfo[type].sczPayloadId, wzPayloadId, 0);
        }

        vrgRetryInfo[type].cRetries = 0;
    }

    vrgRetryInfo[type].dwLastError = ERROR_SUCCESS;
}


DAPI_(void) BalRetryErrorOccurred(
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwError
    )
{
    if (IsActiveRetryEntry(BALRETRY_TYPE_CACHE, wzPackageId, NULL))
    {
        vrgRetryInfo[BALRETRY_TYPE_CACHE].dwLastError = dwError;
    }
    else if (IsActiveRetryEntry(BALRETRY_TYPE_EXECUTE, wzPackageId, NULL))
    {
        vrgRetryInfo[BALRETRY_TYPE_EXECUTE].dwLastError = dwError;
    }
}


DAPI_(int) BalRetryEndPackage(
    __in BALRETRY_TYPE type,
    __in_z_opt LPCWSTR wzPackageId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrError
    )
{
    int nResult = IDNOACTION;

    if (!wzPackageId || !*wzPackageId)
    {
        ReleaseNullStr(vrgRetryInfo[type].sczId);
        ReleaseNullStr(vrgRetryInfo[type].sczPayloadId);
    }
    else if (FAILED(hrError) && vrgRetryInfo[type].cRetries < vdwMaxRetries && IsActiveRetryEntry(type, wzPackageId, wzPayloadId))
    {
        if (BALRETRY_TYPE_CACHE == type)
        {
            // Retry on all errors except the following.
            if (HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) != hrError &&
                BG_E_NETWORK_DISCONNECTED != hrError &&
                HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) != hrError &&
                HRESULT_FROM_WIN32(ERROR_INTERNET_NAME_NOT_RESOLVED) != hrError)
            {
                nResult = IDRETRY;
            }
        }
        else if (BALRETRY_TYPE_EXECUTE == type)
        {
            // If the service is out of whack, just try again.
            if (HRESULT_FROM_WIN32(ERROR_INSTALL_SERVICE_FAILURE) == hrError)
            {
                nResult = IDRETRY;
            }
            else if (HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE) == hrError)
            {
                DWORD dwError = vrgRetryInfo[type].dwLastError;

                // If we failed with one of these specific error codes, then retry since
                // we've seen these have a high success of succeeding on retry.
                if (1303 == dwError ||
                    1304 == dwError ||
                    1306 == dwError ||
                    1307 == dwError ||
                    1309 == dwError ||
                    1310 == dwError ||
                    1311 == dwError ||
                    1312 == dwError ||
                    1316 == dwError ||
                    1317 == dwError ||
                    1321 == dwError ||
                    1335 == dwError ||
                    1402 == dwError ||
                    1406 == dwError ||
                    1606 == dwError ||
                    1706 == dwError ||
                    1719 == dwError ||
                    1723 == dwError ||
                    1923 == dwError ||
                    1931 == dwError)
                {
                    nResult = IDRETRY;
                }
            }
            else if (HRESULT_FROM_WIN32(ERROR_INSTALL_ALREADY_RUNNING) == hrError)
            {
                nResult = IDRETRY;
            }
        }
    }

    return nResult;
}


// Internal functions.

static BOOL IsActiveRetryEntry(
    __in BALRETRY_TYPE type,
    __in_z LPCWSTR wzPackageId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    BOOL fActive = FALSE;

    fActive = vrgRetryInfo[type].sczId && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPackageId, -1, vrgRetryInfo[type].sczId, -1);
    if (fActive && wzPayloadId) // if a payload id was provided ensure it matches.
    {
        fActive = vrgRetryInfo[type].sczPayloadId && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPayloadId, -1, vrgRetryInfo[type].sczPayloadId, -1);
    }

    return fActive;
}
