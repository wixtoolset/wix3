// <copyright file="appsynup.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  RSS update functions implementation.
// </summary>
//
#include "precomp.h"

// prototypes
static HRESULT GetUpdateInfoFileName(
    __in LPCWSTR wzApplicationId,
    __out LPWSTR* ppwzUpdateInfoPath
    );


HRESULT RssUpdateTryLaunchUpdate(
    __in LPCWSTR wzAppId,
    __in DWORD64 dw64AppVersion,
    __out HANDLE* phUpdateProcess,
    __out_opt DWORD64* pdw64NextUpdateTime
    )
{
    HRESULT hr = S_OK;
    DWORD64 dw64NextUpdateTime = 0;
    BOOL fUpdateReady = FALSE;
    DWORD64 dw64UpdateVersion = 0;
    LPWSTR pwzLocalFeedPath = NULL;
    LPWSTR pwzLocalSetupPath = NULL;

    STARTUPINFOW startupInfo = {0};
    PROCESS_INFORMATION procInfo = {0};

    // If an update is available and higher version that the application currently on the local 
    // machine, launch the install.
    hr = RssUpdateGetUpdateInfo(wzAppId, &dw64NextUpdateTime, &fUpdateReady, &dw64UpdateVersion, &pwzLocalFeedPath, &pwzLocalSetupPath);
    if (SUCCEEDED(hr) && fUpdateReady)
    {
        if (dw64AppVersion < dw64UpdateVersion)
        {
            Trace1(REPORT_DEBUG, "Launching a previously downloaded update at %ls.", pwzLocalSetupPath);

            if (!::CreateProcessW(NULL, pwzLocalSetupPath, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL, NULL, &startupInfo, &procInfo))
            {
                ExitWithLastError1(hr, "Failed to execute %S.", pwzLocalSetupPath);
            }

            RssUpdateDeleteUpdateInfo(wzAppId);
            ExitFunction();
        }
        else // update is not newer, ignore it and continue normally
        {
            RssUpdateSetUpdateInfo(wzAppId, dw64NextUpdateTime, 0, NULL, NULL);
        }
    }

    if (pdw64NextUpdateTime)
    {
        *pdw64NextUpdateTime = dw64NextUpdateTime;
    }

    *phUpdateProcess = procInfo.hProcess;
    procInfo.hProcess = NULL;

LExit:
    if (procInfo.hThread)
    {
        ::CloseHandle(procInfo.hThread);
    }

    if (procInfo.hProcess)
    {
        ::CloseHandle(procInfo.hProcess);
    }

    ReleaseStr(pwzLocalSetupPath);
    ReleaseStr(pwzLocalFeedPath);
    return hr;
}


HRESULT RssUpdateCheckFeed(
    __in LPCWSTR wzAppId,
    __in DWORD64 dw64AppVersion,
    __in LPCWSTR wzFeedUri,
    __in DWORD64 dw64NextUpdateTime
    )
{
    HRESULT hr = S_OK;

    FILETIME ft;
    LPWSTR pwzLocalFeedPath = NULL;
    LPWSTR pwzLocalSetupPath = NULL;
    DWORD dwTimeToLive = 0;
    LPWSTR pwzApplicationId = NULL;
    DWORD64 dw64UpdateVersion = 0;
    LPWSTR pwzApplicationSource = NULL;

    BOOL fDeleteDownloadedFeed = FALSE;
    BOOL fDeleteDownloadedSetup = FALSE;

    ::GetSystemTimeAsFileTime(&ft);
    DWORD64 dw64CurrentTime = (static_cast<DWORD64>(ft.dwHighDateTime ) << 32) + ft.dwLowDateTime;

    if (dw64NextUpdateTime < dw64CurrentTime)
    {
        hr = StrAlloc(&pwzLocalFeedPath, MAX_PATH);
        ExitOnFailure(hr, "Failed to allocate feed path string.")

        hr = DirCreateTempPath(L"CT", pwzLocalFeedPath, MAX_PATH);
        ExitOnFailure(hr, "Failed to get a temp file path for the update info.");

        fDeleteDownloadedFeed = TRUE;
        hr = Download(NULL, wzFeedUri, pwzLocalFeedPath);
        ExitOnFailure2(hr, "Failed to download from %ls to %ls.", wzFeedUri, pwzLocalFeedPath);

        hr = RssUpdateGetFeedInfo(pwzLocalFeedPath, &dwTimeToLive, &pwzApplicationId, &dw64UpdateVersion, &pwzApplicationSource);
        ExitOnFailure1(hr, "Failed to ReadUpdateInfo from %ls.", pwzLocalFeedPath);

        if (dw64AppVersion < dw64UpdateVersion)
        {
            hr = StrAlloc(&pwzLocalSetupPath, MAX_PATH);
            ExitOnFailure(hr, "Failed to allocate setup path string.")

            // Get a filename for the update.
            hr = DirCreateTempPath(L"CT", pwzLocalSetupPath, MAX_PATH);
            ExitOnFailure(hr, "Failed to get a temp file path for the update binary.");

            // Download the udpate.
            fDeleteDownloadedSetup = TRUE;
            hr = Download(wzFeedUri, pwzApplicationSource, pwzLocalSetupPath);
            ExitOnFailure2(hr, "Failed to download from %ls to %ls.", pwzApplicationSource, pwzLocalSetupPath);
            Trace2(REPORT_DEBUG, "Downloaded from %ls to %ls.", pwzApplicationSource, pwzLocalSetupPath);

            Trace(REPORT_DEBUG, "Queueing update for next launch.");

            // Queue the update for discovery at the next launch.
            fDeleteDownloadedFeed = FALSE;
            fDeleteDownloadedSetup = FALSE;

            ::GetSystemTimeAsFileTime(&ft);
            dw64NextUpdateTime = (static_cast<DWORD64>(ft.dwHighDateTime ) << 32) + ft.dwLowDateTime + dwTimeToLive;

            RssUpdateSetUpdateInfo(wzAppId, dw64NextUpdateTime, dw64UpdateVersion, pwzLocalFeedPath, pwzLocalSetupPath);
            if (0 != lstrcmpW(pwzApplicationId, wzAppId))
            {
                RssUpdateSetUpdateInfo(pwzApplicationId, dw64NextUpdateTime, 0, NULL, NULL);
            }
        }
    }
    else
    {
        Trace(REPORT_DEBUG, "Skipped update check because feed 'time to live' has not expired.");
    }

LExit:
    if (fDeleteDownloadedSetup)
    {
        ::DeleteFileW(pwzLocalSetupPath);
    }

    if (fDeleteDownloadedFeed)
    {
        ::DeleteFileW(pwzLocalFeedPath);
    }

    ReleaseStr(pwzApplicationSource);
    ReleaseStr(pwzApplicationId);
    ReleaseStr(pwzLocalSetupPath);
    ReleaseStr(pwzLocalFeedPath);

    return hr;
}


HRESULT RssUpdateGetAppInfo(
    __in LPCWSTR wzApplicationId,
    __out_opt DWORD64* pdw64Version,
    __out_opt LPWSTR* ppwzUpdateFeedUri,
    __out_opt LPWSTR* ppwzApplicationPath
    )
{
    Assert(wzApplicationId);

    HRESULT hr = S_OK;

    if (pdw64Version)
    {
        UINT er = ERROR_SUCCESS;
        WCHAR wzVersion[36];
        DWORD cch = countof(wzVersion);
        DWORD dwMajorVersion = 0;
        DWORD dwMinorVersion = 0;

        er = ::MsiGetProductInfoW(wzApplicationId, L"VersionString", wzVersion, &cch);
        ExitOnWin32Error(er, hr, "Failed to get application version.");

        hr = FileVersionFromString(wzVersion, &dwMajorVersion, &dwMinorVersion);
        ExitOnFailure(hr, "Failed to convert string version to numeric version.");

        *pdw64Version = static_cast<DWORD64>(dwMajorVersion) << 32 | static_cast<DWORD64>(dwMinorVersion);
    }

    if (ppwzUpdateFeedUri)
    {
        hr = WiuGetProductInfo(wzApplicationId, L"URLUpdateInfo", ppwzUpdateFeedUri);
        ExitOnFailure(hr, "Failed to get application feed URI.");
    }

    if (ppwzApplicationPath)
    {
        INSTALLSTATE is = INSTALLSTATE_UNKNOWN;

        hr = WiuGetComponentPath(wzApplicationId, wzApplicationId, &is, ppwzApplicationPath);
        ExitOnFailure(hr, "Failed to get application path.");

        if (INSTALLSTATE_LOCAL != is)
        {
            hr = E_NOTFOUND;
        }
    }

LExit:
    return hr;
}


HRESULT RssUpdateGetUpdateInfo(
    __in LPCWSTR wzApplicationId,
    __out_opt DWORD64* pdw64NextUpdate,
    __out_opt BOOL* pfUpdateReady,
    __out_opt DWORD64* pdw64UpdateVersion,
    __out_opt LPWSTR* ppwzLocalFeedPath,
    __out_opt LPWSTR* ppwzLocalSetupPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzUpdateInfoPath = NULL;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    DWORD dwRead = 0;
    DWORD dwExpected = 0;
    DWORD64 dw64NextUpdate = 0;
    DWORD64 dw64UpdateVersion = 0;
    BOOL fUpdateReady = TRUE;
    WCHAR wzBuffer[1024 * 64];
    LPWSTR pwzLocalFeedPath = NULL;
    LPWSTR pwzLocalSetupPath = NULL;

    hr = GetUpdateInfoFileName(wzApplicationId, &pwzUpdateInfoPath);
    ExitOnFailure(hr, "Failed to allocate path to update info.");

    hFile = ::CreateFileW(pwzUpdateInfoPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError(hr, "Failed to open update info.");
    }

    dwExpected = sizeof(dw64NextUpdate);
    if (!::ReadFile(hFile, &dw64NextUpdate, dwExpected, &dwRead, NULL))
    {
        ExitWithLastError(hr, "Failed to read next update time from update info.");
    }
    else if (dwExpected != dwRead)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Failed to find next update time at beginning of update info.");
    }

    dwExpected = sizeof(dw64UpdateVersion);
    if (!::ReadFile(hFile, &dw64UpdateVersion, dwExpected, &dwRead, NULL))
    {
        ExitWithLastError(hr, "Failed to read update version from update info.");
    }
    else if (0 == dwRead) // no other update information
    {
        fUpdateReady = FALSE;
    }
    else if (dwExpected != dwRead)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Failed to find update version in update info.");
    }

    if (fUpdateReady)
    {
        dwExpected = sizeof(WCHAR) * 2;
        if (!::ReadFile(hFile, wzBuffer, dwExpected, &dwRead, NULL))
        {
            ExitWithLastError(hr, "Failed to read newline after next update time in update info.");
        }
        else if (dwExpected != dwRead || wzBuffer[0] != L'\r' || wzBuffer[1] != L'\n')
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Failed to find newline after next update time in update info.");
        }

        LPWSTR pwcCrLn = NULL;
        do
        {
            dwExpected = sizeof(wzBuffer) - sizeof(WCHAR); // leave space to null terminate the buffer
            if (!::ReadFile(hFile, wzBuffer, dwExpected, &dwRead, NULL))
            {
                ExitWithLastError(hr, "Failed to read buffer in update info.");
            }
            else if (0 == dwRead)
            {
                break;
            }

            Assert(dwRead / sizeof(WCHAR) < countof(wzBuffer));
            wzBuffer[dwRead / sizeof(WCHAR)] = L'\0';

            pwcCrLn = wcsstr(wzBuffer, L"\r\n");
            if (pwcCrLn)
            {
                *pwcCrLn = L'\0';

                hr = StrAllocConcat(&pwzLocalFeedPath, wzBuffer, 0);
                ExitOnFailure(hr, "Failed to copy buffer into feed path.");

                pwcCrLn += 2;
                if (*pwcCrLn)
                {
                    hr = StrAllocString(&pwzLocalSetupPath, pwcCrLn, 0);
                    ExitOnFailure(hr, "Failed to copy remaining buffer into setup path.");
                }
            }
            else
            {
                hr = StrAllocConcat(&pwzLocalFeedPath, wzBuffer, 0);
                ExitOnFailure(hr, "Failed to copy buffer into feed path.");
            }
        } while (!pwcCrLn);

        do
        {
            dwExpected = sizeof(wzBuffer) - sizeof(WCHAR); // leave space to null terminate the buffer
            if (!::ReadFile(hFile, wzBuffer, dwExpected, &dwRead, NULL))
            {
                ExitWithLastError(hr, "Failed to read buffer in update info.");
            }
            else if (0 == dwRead)
            {
                break;
            }

            Assert(dwRead / sizeof(WCHAR) < countof(wzBuffer));
            wzBuffer[dwRead / sizeof(WCHAR)] = L'\0';

            hr = StrAllocConcat(&pwzLocalSetupPath, wzBuffer, 0);
            ExitOnFailure(hr, "Failed to copy buffer into setup path.");
        } while (0 < dwRead);

        if (!pwzLocalSetupPath)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Failed to parse update info.");
        }
    }

    if (pfUpdateReady)
    {
        *pfUpdateReady = fUpdateReady;
    }

    if (pdw64NextUpdate)
    {
        *pdw64NextUpdate = dw64NextUpdate;
    }

    if (fUpdateReady && pdw64UpdateVersion)
    {
        *pdw64UpdateVersion = dw64UpdateVersion;
    }

    if (fUpdateReady && ppwzLocalFeedPath)
    {
        hr = StrAllocString(ppwzLocalFeedPath, pwzLocalFeedPath, 0);
        ExitOnFailure(hr, "Failed to allocate local feed path.");
    }

    if (fUpdateReady && ppwzLocalSetupPath)
    {
        hr = StrAllocString(ppwzLocalSetupPath, pwzLocalSetupPath, 0);
        ExitOnFailure(hr, "Failed to allocate local setup path.");
    }

LExit:
    ReleaseStr(pwzLocalSetupPath);
    ReleaseStr(pwzLocalFeedPath);
    ReleaseFile(hFile);
    ReleaseStr(pwzUpdateInfoPath);
    return hr;
}

HRESULT RssUpdateSetUpdateInfo(
    __in LPCWSTR wzApplicationId,
    __in DWORD64 dw64NextUpdate,
    __in DWORD64 dw64UpdateVersion,
    __in LPCWSTR wzLocalFeedPath,
    __in LPCWSTR wzLocalSetupPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzUpdateInfoPath = NULL;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    DWORD cbWrite = 0;
    DWORD cbWrote = 0;
    WCHAR wzCrLn[] = L"\r\n";

    // Open the update info file.
    hr = GetUpdateInfoFileName(wzApplicationId, &pwzUpdateInfoPath);
    ExitOnFailure(hr, "Failed to allocate path to update info.");

    hFile = ::CreateFileW(pwzUpdateInfoPath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError(hr, "Failed to open update info.");
    }

    // Always save the next update time.
    cbWrite = sizeof(dw64NextUpdate);
    if (!::WriteFile(hFile, &dw64NextUpdate, cbWrite, &cbWrote, NULL))
    {
        ExitWithLastError(hr, "Failed to write next update time to update info.");
    }

    // If all of the update information is provided, save it.
    if (0 != dw64UpdateVersion && wzLocalFeedPath && *wzLocalFeedPath && wzLocalSetupPath && *wzLocalSetupPath)
    {
        cbWrite = sizeof(dw64UpdateVersion);
        if (!::WriteFile(hFile, &dw64UpdateVersion, cbWrite, &cbWrote, NULL))
        {
            ExitWithLastError(hr, "Failed to write update version to update info.");
        }

        cbWrite = lstrlenW(wzCrLn) * sizeof(WCHAR);
        if (!::WriteFile(hFile, wzCrLn, cbWrite, &cbWrote, NULL))
        {
            ExitWithLastError(hr, "Failed to write first new line separator to update info.");
        }

        cbWrite = lstrlenW(wzLocalFeedPath) * sizeof(WCHAR);
        if (!::WriteFile(hFile, wzLocalFeedPath, cbWrite, &cbWrote, NULL))
        {
            ExitWithLastError(hr, "Failed to write feed path to update info.");
        }

        cbWrite = lstrlenW(wzCrLn) * sizeof(WCHAR);
        if (!::WriteFile(hFile, wzCrLn, cbWrite, &cbWrote, NULL))
        {
            ExitWithLastError(hr, "Failed to write second new line separator to update info.");
        }

        cbWrite = lstrlenW(wzLocalSetupPath) * sizeof(WCHAR);
        if (!::WriteFile(hFile, wzLocalSetupPath, cbWrite, &cbWrote, NULL))
        {
            ExitWithLastError(hr, "Failed to write setup path to update info.");
        }
    }

LExit:
    ReleaseFile(hFile);
    ReleaseStr(pwzUpdateInfoPath);
    return hr;
}


HRESULT RssUpdateDeleteUpdateInfo(
    __in LPCWSTR wzApplicationId
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzUpdateInfoPath = NULL;

    hr = GetUpdateInfoFileName(wzApplicationId, &pwzUpdateInfoPath);
    ExitOnFailure(hr, "Failed to allocate path to update info.");

    if (!::DeleteFileW(pwzUpdateInfoPath))
    {
        ExitWithLastError(hr, "Failed to delete update info.");
    }

LExit:
    ReleaseStr(pwzUpdateInfoPath);
    return hr;
}


HRESULT RssUpdateGetFeedInfo(
    __in LPCWSTR wzRssPath,
    __out_opt DWORD* pdwTimeToLive,
    __out_opt LPWSTR* ppwzApplicationId,
    __out_opt DWORD64* pdw64Version,
    __out_opt LPWSTR* ppwzApplicationSource
    )
{
    Assert(wzRssPath);

    HRESULT hr = S_OK;
    BOOL bRssInitialized = FALSE;
    RSS_CHANNEL * pRssChannel = NULL;
    RSS_UNKNOWN_ELEMENT* pUnknownElement = NULL;
    RSS_ITEM * pItem = NULL;
    LPWSTR pwzDefaultApplicationId = NULL;

    hr = RssInitialize();
    ExitOnFailure(hr, "Failed to initialize RSS parser.");
    bRssInitialized = TRUE;

    hr = RssParseFromFile(wzRssPath, &pRssChannel);
    ExitOnFailure1(hr, "Failed to read RSS channel from %S.", wzRssPath);

    if (0 == pRssChannel->cItems)
    {
        ExitOnFailure(hr = E_INVALIDARG, "RSS Feed has zero items.");
    }

    if (pdwTimeToLive)
    {
        *pdwTimeToLive = pRssChannel->dwTimeToLive;
    }

    pUnknownElement = pRssChannel->pUnknownElements;
    while (pUnknownElement)
    {
        if (0 == lstrcmpW(pUnknownElement->wzNamespace, L"http://appsyndication.org/schemas/appsyn"))
        {
            if (0 == lstrcmpW(pUnknownElement->wzElement, L"application"))
            {
                hr = StrAllocString(&pwzDefaultApplicationId, pUnknownElement->wzValue, 0);
                ExitOnFailure(hr, "Failed to copy RSS feed application identity to default application id.");
            }
        }

        pUnknownElement = pUnknownElement->pNext;
    }

    for (DWORD i = 0; i < pRssChannel->cItems; ++i)
    {
        if (!pItem || (pItem->ftPublished.dwHighDateTime < pRssChannel->rgItems[i].ftPublished.dwHighDateTime) || 
            (pItem->ftPublished.dwHighDateTime == pRssChannel->rgItems[i].ftPublished.dwHighDateTime && 
            pItem->ftPublished.dwLowDateTime < pRssChannel->rgItems[i].ftPublished.dwLowDateTime))
        {
            pItem = pRssChannel->rgItems + i;
        }
        else
        {
            continue; // not the newest item
        }

        if (ppwzApplicationId && *ppwzApplicationId)
        {
            *ppwzApplicationId = L"\0";
        }

        if (pdw64Version)
        {
            *pdw64Version = 0;
        }

        if (ppwzApplicationSource)
        {
            hr = StrAllocString(ppwzApplicationSource, pItem->wzEnclosureUrl, 0);
            ExitOnFailure1(hr, "Failed to copy the update source: %ls", pItem->wzEnclosureUrl);
        }

        pUnknownElement = pItem->pUnknownElements;
        while (pUnknownElement)
        {
            if (0 == lstrcmpW(pUnknownElement->wzNamespace, L"http://appsyndication.org/schemas/appsyn"))
            {
                if (ppwzApplicationId && 0 == lstrcmpW(pUnknownElement->wzElement, L"application"))
                {
                    hr = StrAllocString(ppwzApplicationId, pUnknownElement->wzValue, 0);
                    ExitOnFailure(hr, "Failed to copy RSS feed application identity to application id.");
                }
                else if (pdw64Version && 0 == lstrcmpW(pUnknownElement->wzElement, L"version"))
                {
                    DWORD dwMajorVersion = 0;
                    DWORD dwMinorVersion = 0;

                    hr = FileVersionFromString(pUnknownElement->wzValue, &dwMajorVersion, &dwMinorVersion);
                    ExitOnFailure(hr, "Failed to get version from string.");

                    *pdw64Version = static_cast<DWORD64>(dwMajorVersion) << 32 | static_cast<DWORD64>(dwMinorVersion);
                }
            }

            pUnknownElement = pUnknownElement->pNext;
        }
    }

    if (ppwzApplicationId && !*ppwzApplicationId)
    {
        hr = StrAllocString(ppwzApplicationId, pwzDefaultApplicationId, 0);
        ExitOnFailure(hr, "Failed to copy default application identity to application id.");
    }

LExit:
    ReleaseStr(pwzDefaultApplicationId);
    ReleaseNullRssChannel(pRssChannel);

    if (bRssInitialized)
    {
        RssUninitialize();
    }

    return hr;
}


static HRESULT GetUpdateInfoFileName(
    __in LPCWSTR wzApplicationId,
    __out LPWSTR* ppwzUpdateInfoPath
    )
{
    HRESULT hr = S_OK;
    WCHAR wzTempPath[MAX_PATH];
    DWORD cchTempPath = countof(wzTempPath);

    cchTempPath = ::GetTempPathW(cchTempPath, wzTempPath);
    if (0 == cchTempPath)
    {
        ExitWithLastError(hr, "Failed to get temp path.");
    }
    else if (countof(wzTempPath) < cchTempPath)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        ExitOnRootFailure(hr, "Failed to get temp path.");
    }

    hr = StrAllocConcat(ppwzUpdateInfoPath, wzTempPath, 0);
    ExitOnFailure(hr, "Failed to allocate path to update info.");

    hr = StrAllocConcat(ppwzUpdateInfoPath, wzApplicationId, 0);
    ExitOnFailure(hr, "Failed to allocate path to update info.");

LExit:
    return hr;
}
