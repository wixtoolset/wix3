// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsRemoveFolderExQuery = L"SELECT `WixRemoveFolderEx`, `Component_`, `Property`, `InstallMode` FROM `WixRemoveFolderEx`";
enum eRemoveFolderExQuery { rfqId = 1, rfqComponent, rfqProperty, feqMode };

static HRESULT RecursePath(
    __in_z LPCWSTR wzPath,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzComponent,
    __in_z LPCWSTR wzProperty,
    __in int iMode,
    __inout DWORD* pdwCounter,
    __inout MSIHANDLE* phTable,
    __inout MSIHANDLE* phColumns
    )
{
    HRESULT hr = S_OK;
    DWORD er;
    LPWSTR sczSearch = NULL;
    LPWSTR sczProperty = NULL;
    HANDLE hFind = INVALID_HANDLE_VALUE;
    WIN32_FIND_DATAW wfd;
    LPWSTR sczNext = NULL;

    // First recurse down to all the child directories.
    hr = StrAllocFormatted(&sczSearch, L"%s*", wzPath);
    ExitOnFailure1(hr, "Failed to allocate file search string in path: %S", wzPath);

    hFind = ::FindFirstFileW(sczSearch, &wfd);
    if (INVALID_HANDLE_VALUE == hFind)
    {
        er = ::GetLastError();
        if (ERROR_PATH_NOT_FOUND == er)
        {
            WcaLog(LOGMSG_STANDARD, "Search path not found: %ls", sczSearch);
            ExitFunction1(hr = S_FALSE);
        }
        else
        {
            hr = HRESULT_FROM_WIN32(er);
        }
        ExitOnFailure1(hr, "Failed to find all files in path: %S", wzPath);
    }

    do
    {
        // Skip files and the dot directories.
        if (FILE_ATTRIBUTE_DIRECTORY != (wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) || L'.' == wfd.cFileName[0] && (L'\0' == wfd.cFileName[1] || (L'.' == wfd.cFileName[1] && L'\0' == wfd.cFileName[2])))
        {
            continue;
        }

        hr = StrAllocFormatted(&sczNext, L"%s%s\\", wzPath, wfd.cFileName);
        ExitOnFailure2(hr, "Failed to concat filename '%S' to string: %S", wfd.cFileName, wzPath);

        hr = RecursePath(sczNext, wzId, wzComponent, wzProperty, iMode, pdwCounter, phTable, phColumns);
        ExitOnFailure1(hr, "Failed to recurse path: %S", sczNext);
    } while (::FindNextFileW(hFind, &wfd));

    er = ::GetLastError();
    if (ERROR_NO_MORE_FILES == er)
    {
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure1(hr, "Failed while looping through files in directory: %S", wzPath);
    }

    // Finally, set a property that points at our path.
    hr = StrAllocFormatted(&sczProperty, L"_%s_%u", wzProperty, *pdwCounter);
    ExitOnFailure1(hr, "Failed to allocate Property for RemoveFile table with property: %S.", wzProperty);

    ++(*pdwCounter);

    hr = WcaSetProperty(sczProperty, wzPath);
    ExitOnFailure2(hr, "Failed to set Property: %S with path: %S", sczProperty, wzPath);

    // Add the row to remove any files and another row to remove the folder.
    hr = WcaAddTempRecord(phTable, phColumns, L"RemoveFile", NULL, 1, 5, L"RfxFiles", wzComponent, L"*.*", sczProperty, iMode);
    ExitOnFailure2(hr, "Failed to add row to remove all files for WixRemoveFolderEx row: %S under path:", wzId, wzPath);

    hr = WcaAddTempRecord(phTable, phColumns, L"RemoveFile", NULL, 1, 5, L"RfxFolder", wzComponent, NULL, sczProperty, iMode);
    ExitOnFailure2(hr, "Failed to add row to remove folder for WixRemoveFolderEx row: %S under path: %S", wzId, wzPath);

LExit:
    if (INVALID_HANDLE_VALUE != hFind)
    {
        ::FindClose(hFind);
    }

    ReleaseStr(sczNext);
    ReleaseStr(sczProperty);
    ReleaseStr(sczSearch);
    return hr;
}

extern "C" UINT WINAPI WixRemoveFoldersEx(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug WixRemoveFoldersEx");

    HRESULT hr = S_OK;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    LPWSTR sczProperty = NULL;
    LPWSTR sczPath = NULL;
    LPWSTR sczExpandedPath = NULL;
    int iMode = 0;
    DWORD dwCounter = 0;
    DWORD_PTR cchLen = 0;
    MSIHANDLE hTable = NULL;
    MSIHANDLE hColumns = NULL;

    hr = WcaInitialize(hInstall, "WixRemoveFoldersEx");
    ExitOnFailure(hr, "Failed to initialize WixRemoveFoldersEx.");

    // anything to do?
    if (S_OK != WcaTableExists(L"WixRemoveFolderEx"))
    {
        WcaLog(LOGMSG_STANDARD, "WixRemoveFolderEx table doesn't exist, so there are no folders to remove.");
        ExitFunction();
    }

    // query and loop through all the remove folders exceptions
    hr = WcaOpenExecuteView(vcsRemoveFolderExQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on WixRemoveFolderEx table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, rfqId, &sczId);
        ExitOnFailure(hr, "Failed to get remove folder identity.");

        hr = WcaGetRecordString(hRec, rfqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get remove folder component.");

        hr = WcaGetRecordString(hRec, rfqProperty, &sczProperty);
        ExitOnFailure(hr, "Failed to get remove folder property.");

        hr = WcaGetRecordInteger(hRec, feqMode, &iMode);
        ExitOnFailure(hr, "Failed to get remove folder mode");

        hr = WcaGetProperty(sczProperty, &sczPath);
        ExitOnFailure2(hr, "Failed to resolve remove folder property: %S for row: %S", sczProperty, sczId);

        // fail early if the property isn't set as you probably don't want your installers trying to delete SystemFolder
        // StringCchLengthW succeeds only if the string is zero characters plus 1 for the terminating null
        hr = ::StringCchLengthW(sczPath, 1, reinterpret_cast<UINT_PTR*>(&cchLen));
        if (SUCCEEDED(hr))
        {
            ExitOnFailure2(hr = E_INVALIDARG, "Missing folder property: %S for row: %S", sczProperty, sczId);
        }

        hr = PathExpand(&sczExpandedPath, sczPath, PATH_EXPAND_ENVIRONMENT);
        ExitOnFailure2(hr, "Failed to expand path: %S for row: %S", sczPath, sczId);
        
        hr = PathBackslashTerminate(&sczExpandedPath);
        ExitOnFailure1(hr, "Failed to backslash-terminate path: %S", sczExpandedPath);
    
        WcaLog(LOGMSG_STANDARD, "Recursing path: %S for row: %S.", sczExpandedPath, sczId);
        hr = RecursePath(sczExpandedPath, sczId, sczComponent, sczProperty, iMode, &dwCounter, &hTable, &hColumns);
        ExitOnFailure2(hr, "Failed while navigating path: %S for row: %S", sczPath, sczId);
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while processing WixRemoveFolderEx table");

LExit:
    if (hColumns)
    {
        ::MsiCloseHandle(hColumns);
    }

    if (hTable)
    {
        ::MsiCloseHandle(hTable);
    }

    ReleaseStr(sczExpandedPath);
    ReleaseStr(sczPath);
    ReleaseStr(sczProperty);
    ReleaseStr(sczComponent);
    ReleaseStr(sczId);

    DWORD er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
