// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// constants

const BYTE UTF8BOM[] = {0xEF, 0xBB, 0xBF};
const BYTE UTF16BOM[] = {0xFF, 0xFE};

const LPCWSTR REGISTRY_PENDING_FILE_RENAME_KEY = L"SYSTEM\\CurrentControlSet\\Control\\Session Manager";
const LPCWSTR REGISTRY_PENDING_FILE_RENAME_VALUE = L"PendingFileRenameOperations";

/*******************************************************************
 FileFromPath -  returns a pointer to the file part of the path

********************************************************************/
extern "C" LPWSTR DAPI FileFromPath(
    __in LPCWSTR wzPath
    )
{
    if (!wzPath)
        return NULL;

    LPWSTR wzFile = const_cast<LPWSTR>(wzPath);
    for (LPWSTR wz = wzFile; *wz; ++wz)
    {
        // valid delineators
        //     \ => Windows path
        //     / => unix and URL path
        //     : => relative path from mapped root
        if (L'\\' == *wz || L'/' == *wz || L':' == *wz)
            wzFile = wz + 1;
    }

    return wzFile;
}


/*******************************************************************
 FileResolvePath - gets the full path to a file resolving environment
                   variables along the way.

********************************************************************/
extern "C" HRESULT DAPI FileResolvePath(
    __in LPCWSTR wzRelativePath,
    __out LPWSTR *ppwzFullPath
    )
{
    Assert(wzRelativePath && *wzRelativePath);

    HRESULT hr = S_OK;
    DWORD cch = 0;
    LPWSTR pwzExpandedPath = NULL;
    DWORD cchExpandedPath = 0;

    LPWSTR pwzFullPath = NULL;
    DWORD cchFullPath = 0;

    LPWSTR wzFileName = NULL;

    //
    // First, expand any environment variables.
    //
    cchExpandedPath = MAX_PATH;
    hr = StrAlloc(&pwzExpandedPath, cchExpandedPath);
    ExitOnFailure(hr, "Failed to allocate space for expanded path.");

    cch = ::ExpandEnvironmentStringsW(wzRelativePath, pwzExpandedPath, cchExpandedPath);
    if (0 == cch)
    {
        ExitWithLastError1(hr, "Failed to expand environment variables in string: %ls", wzRelativePath);
    }
    else if (cchExpandedPath < cch)
    {
        cchExpandedPath = cch;
        hr = StrAlloc(&pwzExpandedPath, cchExpandedPath);
        ExitOnFailure(hr, "Failed to re-allocate more space for expanded path.");

        cch = ::ExpandEnvironmentStringsW(wzRelativePath, pwzExpandedPath, cchExpandedPath);
        if (0 == cch)
        {
            ExitWithLastError1(hr, "Failed to expand environment variables in string: %ls", wzRelativePath);
        }
        else if (cchExpandedPath < cch)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            ExitOnRootFailure(hr, "Failed to allocate buffer for expanded path.");
        }
    }

    //
    // Second, get the full path.
    //
    cchFullPath = MAX_PATH;
    hr = StrAlloc(&pwzFullPath, cchFullPath);
    ExitOnFailure(hr, "Failed to allocate space for full path.");

    cch = ::GetFullPathNameW(pwzExpandedPath, cchFullPath, pwzFullPath, &wzFileName);
    if (0 == cch)
    {
        ExitWithLastError1(hr, "Failed to get full path for string: %ls", pwzExpandedPath);
    }
    else if (cchFullPath < cch)
    {
        cchFullPath = cch;
        hr = StrAlloc(&pwzFullPath, cchFullPath);
        ExitOnFailure(hr, "Failed to re-allocate more space for full path.");

        cch = ::GetFullPathNameW(pwzExpandedPath, cchFullPath, pwzFullPath, &wzFileName);
        if (0 == cch)
        {
            ExitWithLastError1(hr, "Failed to get full path for string: %ls", pwzExpandedPath);
        }
        else if (cchFullPath < cch)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            ExitOnRootFailure(hr, "Failed to allocate buffer for full path.");
        }
    }

    *ppwzFullPath = pwzFullPath;
    pwzFullPath = NULL;

LExit:
    ReleaseStr(pwzFullPath);
    ReleaseStr(pwzExpandedPath);

    return hr;
}


/*******************************************************************
FileStripExtension - Strip extension from filename
********************************************************************/
extern "C" HRESULT DAPI FileStripExtension(
__in LPCWSTR wzFileName,
__out LPWSTR *ppwzFileNameNoExtension
)
{
    Assert(wzFileName && *wzFileName);
   
    HRESULT hr = S_OK;
   
    SIZE_T cchFileName = wcslen(wzFileName);
   
    LPWSTR pwzFileNameNoExtension = NULL;
    DWORD cchFileNameNoExtension = 0;
   
    // Filename without extension can not be longer than _MAX_FNAME
    // Filename without extension should also not be longer than filename itself
    if (_MAX_FNAME > cchFileName)
    {
        cchFileNameNoExtension = (DWORD) cchFileName;
    }
    else
    {
        cchFileNameNoExtension = _MAX_FNAME;
    }
   
    hr = StrAlloc(&pwzFileNameNoExtension, cchFileNameNoExtension);
    ExitOnFailure(hr, "failed to allocate space for File Name without extension");
   
    // _wsplitpath_s can handle drive/path/filename/extension
    errno_t err = _wsplitpath_s(wzFileName, NULL, NULL, NULL, NULL, pwzFileNameNoExtension, cchFileNameNoExtension, NULL, NULL);
    if (0 != err)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "failed to parse filename: %ls", wzFileName);
    }
   
    *ppwzFileNameNoExtension = pwzFileNameNoExtension;
    pwzFileNameNoExtension = NULL;
   
LExit:
    ReleaseStr(pwzFileNameNoExtension);
   
    return hr;
}


/*******************************************************************
FileChangeExtension - Changes the extension of a filename
********************************************************************/
extern "C" HRESULT DAPI FileChangeExtension(
    __in LPCWSTR wzFileName,
    __in LPCWSTR wzNewExtension,
    __out LPWSTR *ppwzFileNameNewExtension
    )
{
    Assert(wzFileName && *wzFileName);

    HRESULT hr = S_OK;
    LPWSTR sczFileName = NULL;

    hr = FileStripExtension(wzFileName, &sczFileName);
    ExitOnFailure1(hr, "Failed to strip extension from file name: %ls", wzFileName);

    hr = StrAllocConcat(&sczFileName, wzNewExtension, 0);
    ExitOnFailure(hr, "Failed to add new extension.");

    *ppwzFileNameNewExtension = sczFileName;
    sczFileName = NULL;

LExit:
    ReleaseStr(sczFileName);
   
    return hr;
}


/*******************************************************************
FileAddSuffixToBaseName - Adds a suffix the base portion of a file
name; e.g., file.ext to fileSuffix.ext.
********************************************************************/
extern "C" HRESULT DAPI FileAddSuffixToBaseName(
    __in_z LPCWSTR wzFileName,
    __in_z LPCWSTR wzSuffix,
    __out_z LPWSTR* psczNewFileName
    )
{
    Assert(wzFileName && *wzFileName);

    HRESULT hr = S_OK;
    LPWSTR sczNewFileName = NULL;

    LPCWSTR wzExtension = wzFileName + lstrlenW(wzFileName);
    while (wzFileName < wzExtension && L'.' != *wzExtension)
    {
        --wzExtension;
    }

    if (wzFileName < wzExtension)
    {
        // found an extension so add the suffix before it
        hr = StrAllocFormatted(&sczNewFileName, L"%.*ls%ls%ls", static_cast<int>(wzExtension - wzFileName), wzFileName, wzSuffix, wzExtension);
    }
    else
    {
        // no extension, so add the suffix at the end of the whole name
        hr = StrAllocString(&sczNewFileName, wzFileName, 0);
        ExitOnFailure(hr, "Failed to allocate new file name.");

        hr = StrAllocConcat(&sczNewFileName, wzSuffix, 0);
    }
    ExitOnFailure(hr, "Failed to allocate new file name with suffix.");

    *psczNewFileName = sczNewFileName;
    sczNewFileName = NULL;

LExit:
    ReleaseStr(sczNewFileName);
   
    return hr;
}


/*******************************************************************
 FileVersion

********************************************************************/
extern "C" HRESULT DAPI FileVersion(
    __in LPCWSTR wzFilename,
    __out DWORD *pdwVerMajor,
    __out DWORD* pdwVerMinor
    )
{
    HRESULT hr = S_OK;

    DWORD dwHandle = 0;
    UINT cbVerBuffer = 0;
    LPVOID pVerBuffer = NULL;
    VS_FIXEDFILEINFO* pvsFileInfo = NULL;
    UINT cbFileInfo = 0;

    if (0 == (cbVerBuffer = ::GetFileVersionInfoSizeW(wzFilename, &dwHandle)))
    {
        ExitOnLastErrorDebugTrace1(hr, "failed to get version info for file: %ls", wzFilename);
    }

    pVerBuffer = ::GlobalAlloc(GMEM_FIXED, cbVerBuffer);
    ExitOnNullDebugTrace1(pVerBuffer, hr, E_OUTOFMEMORY, "failed to allocate version info for file: %ls", wzFilename);

    if (!::GetFileVersionInfoW(wzFilename, dwHandle, cbVerBuffer, pVerBuffer))
    {
        ExitOnLastErrorDebugTrace1(hr, "failed to get version info for file: %ls", wzFilename);
    }

    if (!::VerQueryValueW(pVerBuffer, L"\\", (void**)&pvsFileInfo, &cbFileInfo))
    {
        ExitOnLastErrorDebugTrace1(hr, "failed to get version value for file: %ls", wzFilename);
    }

    *pdwVerMajor = pvsFileInfo->dwFileVersionMS;
    *pdwVerMinor = pvsFileInfo->dwFileVersionLS;

LExit:
    if (pVerBuffer)
    {
        ::GlobalFree(pVerBuffer);
    }
    return hr;
}


/*******************************************************************
 FileVersionFromString

*******************************************************************/
extern "C" HRESULT DAPI FileVersionFromString(
    __in LPCWSTR wzVersion,
    __out DWORD* pdwVerMajor,
    __out DWORD* pdwVerMinor
    )
{
    Assert(pdwVerMajor && pdwVerMinor);

    HRESULT hr = S_OK;
    LPCWSTR pwz = wzVersion;
    DWORD dw;

    *pdwVerMajor = 0;
    *pdwVerMinor = 0;

    if ((L'v' == *pwz) || (L'V' == *pwz))
    {
        ++pwz;
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && (L'.' == *pwz && dw < 0x10000) || !*pwz)
    {
        *pdwVerMajor = dw << 16;

        if (!*pwz)
        {
            ExitFunction1(hr = S_OK);
        }
        ++pwz;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && (L'.' == *pwz && dw < 0x10000) || !*pwz)
    {
        *pdwVerMajor |= dw;

        if (!*pwz)
        {
            ExitFunction1(hr = S_OK);
        }
        ++pwz;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && (L'.' == *pwz && dw < 0x10000) || !*pwz)
    {
        *pdwVerMinor = dw << 16;

        if (!*pwz)
        {
            ExitFunction1(hr = S_OK);
        }
        ++pwz;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

    dw = wcstoul(pwz, (WCHAR**)&pwz, 10);
    if (pwz && L'\0' == *pwz && dw < 0x10000)
    {
        *pdwVerMinor |= dw;
    }
    else
    {
        ExitFunction1(hr = S_FALSE);
    }

LExit:
    return hr;
}


/*******************************************************************
 FileVersionFromStringEx

*******************************************************************/
extern "C" HRESULT DAPI FileVersionFromStringEx(
    __in LPCWSTR wzVersion,
    __in DWORD cchVersion,
    __out DWORD64* pqwVersion
    )
{
    Assert(wzVersion);
    Assert(pqwVersion);

    HRESULT hr = S_OK;
    LPCWSTR wzEnd = NULL;
    LPCWSTR wzPartBegin = wzVersion;
    LPCWSTR wzPartEnd = wzVersion;
    DWORD iPart = 0;
    USHORT us = 0;
    DWORD64 qwVersion = 0;

    // get string length if not provided
    if (0 >= cchVersion)
    {
        cchVersion = lstrlenW(wzVersion);
        if (0 >= cchVersion)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
    }

    if ((L'v' == *wzVersion) || (L'V' == *wzVersion))
    {
        ++wzVersion;
        --cchVersion;
        wzPartBegin = wzVersion;
        wzPartEnd = wzVersion;
    }

    // save end pointer
    wzEnd = wzVersion + cchVersion;

    // loop through parts
    for (;;)
    {
        if (4 <= iPart)
        {
            // error, too many parts
            ExitFunction1(hr = E_INVALIDARG);
        }

        // find end of part
        while (wzPartEnd < wzEnd && L'.' != *wzPartEnd)
        {
            ++wzPartEnd;
        }
        if (wzPartBegin == wzPartEnd)
        {
            // error, empty part
            ExitFunction1(hr = E_INVALIDARG);
        }

        DWORD cchPart;
        hr = ::PtrdiffTToDWord(wzPartEnd - wzPartBegin, &cchPart);
        ExitOnFailure(hr, "Version number part was too long.");

        // parse version part
        hr = StrStringToUInt16(wzPartBegin, cchPart, &us);
        ExitOnFailure(hr, "Failed to parse version number part.");

        // add part to qword version
        qwVersion |= (DWORD64)us << ((3 - iPart) * 16);

        if (wzPartEnd >= wzEnd)
        {
            // end of string
            break;
        }

        wzPartBegin = ++wzPartEnd; // skip over separator
        ++iPart;
    }

    *pqwVersion = qwVersion;

LExit:
    return hr;
}

/*******************************************************************
 FileVersionFromStringEx - Formats the DWORD64 as a string version.

*******************************************************************/
extern "C" HRESULT DAPI FileVersionToStringEx(
    __in DWORD64 qwVersion,
    __out LPWSTR* psczVersion
    )
{
    HRESULT hr = S_OK;
    WORD wMajor = 0;
    WORD wMinor = 0;
    WORD wBuild = 0;
    WORD wRevision = 0;

    // Mask and shift each WORD for each field.
    wMajor = (WORD)(qwVersion >> 48 & 0xffff);
    wMinor = (WORD)(qwVersion >> 32 & 0xffff);
    wBuild = (WORD)(qwVersion >> 16 & 0xffff);
    wRevision = (WORD)(qwVersion & 0xffff);

    // Format and return the version string.
    hr = StrAllocFormatted(psczVersion, L"%u.%u.%u.%u", wMajor, wMinor, wBuild, wRevision);
    ExitOnFailure(hr, "Failed to allocate and format the version number.");

LExit:
    return hr;
}

/*******************************************************************
 FileSetPointer - sets the file pointer.

********************************************************************/
extern "C" HRESULT DAPI FileSetPointer(
    __in HANDLE hFile,
    __in DWORD64 dw64Move,
    __out_opt DWORD64* pdw64NewPosition,
    __in DWORD dwMoveMethod
    )
{
    Assert(INVALID_HANDLE_VALUE != hFile);

    HRESULT hr = S_OK;
    LARGE_INTEGER liMove;
    LARGE_INTEGER liNewPosition;

    liMove.QuadPart = dw64Move;
    if (!::SetFilePointerEx(hFile, liMove, &liNewPosition, dwMoveMethod))
    {
        ExitWithLastError(hr, "Failed to set file pointer.");
    }

    if (pdw64NewPosition)
    {
        *pdw64NewPosition = liNewPosition.QuadPart;
    }

LExit:
    return hr;
}


/*******************************************************************
 FileSize

********************************************************************/
extern "C" HRESULT DAPI FileSize(
    __in LPCWSTR pwzFileName,
    __out LONGLONG* pllSize
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    ExitOnNull(pwzFileName, hr, E_INVALIDARG, "Attempted to check filename, but no filename was provided");

    hFile = ::CreateFileW(pwzFileName, FILE_READ_ATTRIBUTES, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError1(hr, "Failed to open file %ls while checking file size", pwzFileName);
    }

    hr = FileSizeByHandle(hFile, pllSize);
    ExitOnFailure1(hr, "Failed to check size of file %ls by handle", pwzFileName);

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}


/*******************************************************************
 FileSizeByHandle

********************************************************************/
extern "C" HRESULT DAPI FileSizeByHandle(
    __in HANDLE hFile,
    __out LONGLONG* pllSize
    )
{
    Assert(INVALID_HANDLE_VALUE != hFile && pllSize);
    HRESULT hr = S_OK;
    LARGE_INTEGER li;

    *pllSize = 0;

    if (!::GetFileSizeEx(hFile, &li))
    {
        ExitWithLastError(hr, "Failed to get size of file.");
    }

    *pllSize = li.QuadPart;

LExit:
    return hr;
}


/*******************************************************************
 FileExistsEx

********************************************************************/
extern "C" BOOL DAPI FileExistsEx(
    __in LPCWSTR wzPath,
    __out_opt DWORD *pdwAttributes
    )
{
    Assert(wzPath && *wzPath);
    BOOL fExists = FALSE;

    WIN32_FIND_DATAW fd = { };
    HANDLE hff;

    if (INVALID_HANDLE_VALUE != (hff = ::FindFirstFileW(wzPath, &fd)))
    {
        ::FindClose(hff);
        if (!(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
        {
            if (pdwAttributes)
            {
                *pdwAttributes = fd.dwFileAttributes;
            }

            fExists = TRUE;
        }
    }

    return fExists;
}


/*******************************************************************
 FileExistsAfterRestart - checks that a file exists and will continue
                          to exist after restart.

********************************************************************/
extern "C" BOOL DAPI FileExistsAfterRestart(
    __in_z LPCWSTR wzPath,
    __out_opt DWORD *pdwAttributes
    )
{
    HRESULT hr = S_OK;
    BOOL fExists = FALSE;
    HKEY hkPendingFileRename = NULL;
    LPWSTR* rgsczRenames = NULL;
    DWORD cRenames = 0;
    int nCompare = 0;

    fExists = FileExistsEx(wzPath, pdwAttributes);
    if (fExists)
    {
        hr = RegOpen(HKEY_LOCAL_MACHINE, REGISTRY_PENDING_FILE_RENAME_KEY, KEY_QUERY_VALUE, &hkPendingFileRename);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        ExitOnFailure(hr, "Failed to open pending file rename registry key.");

        hr = RegReadStringArray(hkPendingFileRename, REGISTRY_PENDING_FILE_RENAME_VALUE, &rgsczRenames, &cRenames);
        if (E_FILENOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        ExitOnFailure(hr, "Failed to read pending file renames.");

        // The pending file renames array is pairs of source and target paths. We only care
        // about checking the source paths so skip the target paths (i += 2).
        for (DWORD i = 0; i < cRenames; i += 2)
        {
            LPWSTR wzRename = rgsczRenames[i];
            if (wzRename && *wzRename)
            {
                // Skip the long path designator if present.
                if (L'\\' == wzRename[0] && L'?' == wzRename[1] && L'?' == wzRename[2] && L'\\' == wzRename[3])
                {
                    wzRename += 4;
                }

                hr = PathCompare(wzPath, wzRename, &nCompare);
                ExitOnFailure(hr, "Failed to compare path from pending file rename to check path.");

                if (CSTR_EQUAL == nCompare)
                {
                    fExists = FALSE;
                    break;
                }
            }
        }
    }

LExit:
    ReleaseStrArray(rgsczRenames, cRenames);
    ReleaseRegKey(hkPendingFileRename);

    return fExists;
}


/*******************************************************************
 FileRemoveFromPendingRename - removes the file path from the pending
                               file rename list.

********************************************************************/
extern "C" HRESULT DAPI FileRemoveFromPendingRename(
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    HKEY hkPendingFileRename = NULL;
    LPWSTR* rgsczRenames = NULL;
    DWORD cRenames = 0;
    int nCompare = 0;
    BOOL fRemoved = FALSE;
    DWORD cNewRenames = 0;

    hr = RegOpen(HKEY_LOCAL_MACHINE, REGISTRY_PENDING_FILE_RENAME_KEY, KEY_QUERY_VALUE | KEY_SET_VALUE, &hkPendingFileRename);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to open pending file rename registry key.");

    hr = RegReadStringArray(hkPendingFileRename, REGISTRY_PENDING_FILE_RENAME_VALUE, &rgsczRenames, &cRenames);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to read pending file renames.");

    // The pending file renames array is pairs of source and target paths. We only care
    // about checking the source paths so skip the target paths (i += 2).
    for (DWORD i = 0; i < cRenames; i += 2)
    {
        LPWSTR wzRename = rgsczRenames[i];
        if (wzRename && *wzRename)
        {
            // Skip the long path designator if present.
            if (L'\\' == wzRename[0] && L'?' == wzRename[1] && L'?' == wzRename[2] && L'\\' == wzRename[3])
            {
                wzRename += 4;
            }

            hr = PathCompare(wzPath, wzRename, &nCompare);
            ExitOnFailure(hr, "Failed to compare path from pending file rename to check path.");

            // If we find our path in the list, null out the source and target slot and
            // we'll compact the array next.
            if (CSTR_EQUAL == nCompare)
            {
                ReleaseNullStr(rgsczRenames[i]);
                ReleaseNullStr(rgsczRenames[i + 1]);
                fRemoved = TRUE;
            }
        }
    }

    if (fRemoved)
    {
        // Compact the array by removing any nulls.
        for (DWORD i = 0; i < cRenames; ++i)
        {
            LPWSTR wzRename = rgsczRenames[i];
            if (wzRename)
            {
                rgsczRenames[cNewRenames] = wzRename;
                ++cNewRenames;
            }
        }

        cRenames = cNewRenames; // ignore the pointers on the end of the array since an early index points to them already.

        // Write the new array back to the pending file rename key.
        hr = RegWriteStringArray(hkPendingFileRename, REGISTRY_PENDING_FILE_RENAME_VALUE, rgsczRenames, cRenames);
        ExitOnFailure(hr, "Failed to update pending file renames.");
    }

LExit:
    ReleaseStrArray(rgsczRenames, cRenames);
    ReleaseRegKey(hkPendingFileRename);

    return hr;
}


/*******************************************************************
 FileRead - read a file into memory

********************************************************************/
extern "C" HRESULT DAPI FileRead(
    __deref_out_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out DWORD* pcbDest,
    __in LPCWSTR wzSrcPath
    )
{
    HRESULT hr = FileReadPartial(ppbDest, pcbDest, wzSrcPath, FALSE, 0, 0xFFFFFFFF, FALSE);
    return hr;
}


/*******************************************************************
 FileReadUntil - read a file into memory with a maximum size

********************************************************************/
extern "C" HRESULT DAPI FileReadUntil(
    __deref_out_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out_range(<=, cbMaxRead) DWORD* pcbDest,
    __in LPCWSTR wzSrcPath,
    __in DWORD cbMaxRead
    )
{
    HRESULT hr = FileReadPartial(ppbDest, pcbDest, wzSrcPath, FALSE, 0, cbMaxRead, FALSE);
    return hr;
}


/*******************************************************************
 FileReadPartial - read a portion of a file into memory

********************************************************************/
extern "C" HRESULT DAPI FileReadPartial(
    __deref_out_bcount_full(*pcbDest) LPBYTE* ppbDest,
    __out_range(<=, cbMaxRead) DWORD* pcbDest,
    __in LPCWSTR wzSrcPath,
    __in BOOL fSeek,
    __in DWORD cbStartPosition,
    __in DWORD cbMaxRead,
    __in BOOL fPartialOK
    )
{
    HRESULT hr = S_OK;

    UINT er = ERROR_SUCCESS;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    LARGE_INTEGER liFileSize = { };
    DWORD cbData = 0;
    BYTE* pbData = NULL;

    ExitOnNull(pcbDest, hr, E_INVALIDARG, "Invalid argument pcbDest");
    ExitOnNull(ppbDest, hr, E_INVALIDARG, "Invalid argument ppbDest");
    ExitOnNull(wzSrcPath, hr, E_INVALIDARG, "Invalid argument wzSrcPath");
    ExitOnNull(*wzSrcPath, hr, E_INVALIDARG, "*wzSrcPath is null");

    hFile = ::CreateFileW(wzSrcPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        er = ::GetLastError();
        if (E_FILENOTFOUND == HRESULT_FROM_WIN32(er))
        {
            ExitFunction1(hr = E_FILENOTFOUND);
        }
        ExitOnWin32Error1(er, hr, "Failed to open file: %ls", wzSrcPath);
    }

    if (!::GetFileSizeEx(hFile, &liFileSize))
    {
        ExitWithLastError1(hr, "Failed to get size of file: %ls", wzSrcPath);
    }

    if (fSeek)
    {
        if (cbStartPosition > liFileSize.QuadPart)
        {
            hr = E_INVALIDARG;
            ExitOnFailure3(hr, "Start position %d bigger than file '%ls' size %d", cbStartPosition, wzSrcPath, liFileSize.QuadPart);
        }

        DWORD dwErr = ::SetFilePointer(hFile, cbStartPosition, NULL, FILE_CURRENT);
        if (INVALID_SET_FILE_POINTER == dwErr)
        {
            ExitOnLastError1(hr, "Failed to seek position %d", cbStartPosition);
        }
    }
    else
    {
        cbStartPosition = 0;
    }

    if (fPartialOK)
    {
        cbData = cbMaxRead;
    }
    else
    {
        cbData = liFileSize.LowPart - cbStartPosition; // should only need the low part because we cap at DWORD
        if (cbMaxRead < liFileSize.QuadPart - cbStartPosition)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            ExitOnRootFailure1(hr, "Failed to load file: %ls, too large.", wzSrcPath);
        }
    }

    if (*ppbDest)
    {
        if (0 == cbData)
        {
            ReleaseNullMem(*ppbDest);
            *pcbDest = 0;
            ExitFunction1(hr = S_OK);
        }

        LPVOID pv = MemReAlloc(*ppbDest, cbData, TRUE);
        ExitOnNull1(pv, hr, E_OUTOFMEMORY, "Failed to re-allocate memory to read in file: %ls", wzSrcPath);

        pbData = static_cast<BYTE*>(pv);
    }
    else
    {
        if (0 == cbData)
        {
            *pcbDest = 0;
            ExitFunction1(hr = S_OK);
        }

        pbData = static_cast<BYTE*>(MemAlloc(cbData, TRUE));
        ExitOnNull1(pbData, hr, E_OUTOFMEMORY, "Failed to allocate memory to read in file: %ls", wzSrcPath);
    }

    DWORD cbTotalRead = 0;
    DWORD cbRead = 0;
    do
    {
        DWORD cbRemaining = 0;
        hr = ::ULongSub(cbData, cbTotalRead, &cbRemaining);
        ExitOnFailure(hr, "Underflow calculating remaining buffer size.");

        if (!::ReadFile(hFile, pbData + cbTotalRead, cbRemaining, &cbRead, NULL))
        {
            ExitWithLastError1(hr, "Failed to read from file: %ls", wzSrcPath);
        }

        cbTotalRead += cbRead;
    } while (cbRead);

    if (cbTotalRead != cbData)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure1(hr, "Failed to completely read file: %ls", wzSrcPath);
    }

    *ppbDest = pbData;
    pbData = NULL;
    *pcbDest = cbData;

LExit:
    ReleaseMem(pbData);
    ReleaseFile(hFile);

    return hr;
}


/*******************************************************************
 FileWrite - write a file from memory

********************************************************************/
extern "C" HRESULT DAPI FileWrite(
    __in_z LPCWSTR pwzFileName,
    __in DWORD dwFlagsAndAttributes,
    __in_bcount_opt(cbData) LPCBYTE pbData,
    __in DWORD cbData,
    __out_opt HANDLE* pHandle
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Open the file
    hFile = ::CreateFileW(pwzFileName, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, dwFlagsAndAttributes, NULL);
    ExitOnInvalidHandleWithLastError1(hFile, hr, "Failed to open file: %ls", pwzFileName);

    hr = FileWriteHandle(hFile, pbData, cbData);
    ExitOnFailure1(hr, "Failed to write to file: %ls", pwzFileName);

    if (pHandle)
    {
        *pHandle = hFile;
        hFile = INVALID_HANDLE_VALUE;
    }

LExit:
    ReleaseFile(hFile);

    return hr;
}


/*******************************************************************
 FileWriteHandle - write to a file handle from memory

********************************************************************/
extern "C" HRESULT DAPI FileWriteHandle(
    __in HANDLE hFile,
    __in_bcount_opt(cbData) LPCBYTE pbData,
    __in DWORD cbData
    )
{
    HRESULT hr = S_OK;
    DWORD cbDataWritten = 0;
    DWORD cbTotal = 0;

    // Write out all of the data.
    do
    {
        if (!::WriteFile(hFile, pbData + cbTotal, cbData - cbTotal, &cbDataWritten, NULL))
        {
            ExitOnLastError(hr, "Failed to write data to file handle.");
        }

        cbTotal += cbDataWritten;
    } while (cbTotal < cbData);

LExit:
    return hr;
}


/*******************************************************************
 FileCopyUsingHandles

*******************************************************************/
extern "C" HRESULT DAPI FileCopyUsingHandles(
    __in HANDLE hSource,
    __in HANDLE hTarget,
    __in DWORD64 cbCopy,
    __out_opt DWORD64* pcbCopied
    )
{
    HRESULT hr = S_OK;
    DWORD64 cbTotalCopied = 0;
    BYTE rgbData[4 * 1024];
    DWORD cbRead = 0;

    do
    {
        cbRead = static_cast<DWORD>((0 == cbCopy) ? countof(rgbData) : min(countof(rgbData), cbCopy - cbTotalCopied));
        if (!::ReadFile(hSource, rgbData, cbRead, &cbRead, NULL))
        {
            ExitWithLastError(hr, "Failed to read from source.");
        }

        if (cbRead)
        {
            hr = FileWriteHandle(hTarget, rgbData, cbRead);
            ExitOnFailure(hr, "Failed to write to target.");
        }

        cbTotalCopied += cbRead;
    } while (cbTotalCopied < cbCopy && 0 != cbRead);

    if (pcbCopied)
    {
        *pcbCopied = cbTotalCopied;
    }

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureCopy

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureCopy(
    __in LPCWSTR wzSource,
    __in LPCWSTR wzTarget,
    __in BOOL fOverwrite
    )
{
    HRESULT hr = S_OK;
    DWORD er;

    // try to move the file first
    if (::CopyFileW(wzSource, wzTarget, !fOverwrite))
    {
        ExitFunction();  // we're done
    }

    er = ::GetLastError();  // check the error and do the right thing below
    if (!fOverwrite && (ERROR_FILE_EXISTS == er || ERROR_ALREADY_EXISTS == er))
    {
        // if not overwriting this is an expected error
        ExitFunction1(hr = S_FALSE);
    }
    else if (ERROR_PATH_NOT_FOUND == er)  // if the path doesn't exist
    {
        // try to create the directory then do the copy
        LPWSTR pwzLastSlash = NULL;
        for (LPWSTR pwz = const_cast<LPWSTR>(wzTarget); *pwz; ++pwz)
        {
            if (*pwz == L'\\')
            {
                pwzLastSlash = pwz;
            }
        }

        if (pwzLastSlash)
        {
            *pwzLastSlash = L'\0'; // null terminate
            hr = DirEnsureExists(wzTarget, NULL);
            *pwzLastSlash = L'\\'; // now put the slash back
            ExitOnFailureDebugTrace2(hr, "failed to create directory while copying file: '%ls' to: '%ls'", wzSource, wzTarget);

            // try to copy again
            if (!::CopyFileW(wzSource, wzTarget, fOverwrite))
            {
                ExitOnLastErrorDebugTrace2(hr, "failed to copy file: '%ls' to: '%ls'", wzSource, wzTarget);
            }
        }
        else // no path was specified so just return the error
        {
            hr = HRESULT_FROM_WIN32(er);
        }
    }
    else // unexpected error
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureCopyWithRetry

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureCopyWithRetry(
    __in LPCWSTR wzSource,
    __in LPCWSTR wzTarget,
    __in BOOL fOverwrite,
    __in DWORD cRetry,
    __in DWORD dwWaitMilliseconds
    )
{
    AssertSz(cRetry != DWORD_MAX, "Cannot pass DWORD_MAX for retry.");

    HRESULT hr = E_FAIL;
    DWORD i = 0;

    for (i = 0; FAILED(hr) && i <= cRetry; ++i)
    {
        if (0 < i)
        {
            ::Sleep(dwWaitMilliseconds);
        }

        hr = FileEnsureCopy(wzSource, wzTarget, fOverwrite);
        if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr
            || HRESULT_FROM_WIN32(ERROR_FILE_EXISTS) == hr || HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS) == hr)
        {
            break; // no reason to retry these errors.
        }
    }
    ExitOnFailure3(hr, "Failed to copy file: '%ls' to: '%ls' after %u retries.", wzSource, wzTarget, i);

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureMove

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureMove(
    __in LPCWSTR wzSource,
    __in LPCWSTR wzTarget,
    __in BOOL fOverwrite,
    __in BOOL fAllowCopy
    )
{
    HRESULT hr = S_OK;
    DWORD er;

    DWORD dwFlags = 0;

    if (fOverwrite)
    {
        dwFlags |= MOVEFILE_REPLACE_EXISTING;
    }
    if (fAllowCopy)
    {
        dwFlags |= MOVEFILE_COPY_ALLOWED;
    }

    // try to move the file first
    if (::MoveFileExW(wzSource, wzTarget, dwFlags))
    {
        ExitFunction();  // we're done
    }

    er = ::GetLastError();  // check the error and do the right thing below
    if (!fOverwrite && (ERROR_FILE_EXISTS == er || ERROR_ALREADY_EXISTS == er))
    {
        // if not overwriting this is an expected error
        ExitFunction1(hr = S_FALSE);
    }
    else if (ERROR_FILE_NOT_FOUND == er)
    {
        // We are seeing some cases where ::MoveFileEx() says a file was not found
        // but the source file is actually present. In that case, return path not
        // found so we try to create the target path since that is most likely
        // what is missing. Otherwise, the source file is missing and we're obviously
        // not going to be recovering from that.
        if (FileExistsEx(wzSource, NULL))
        {
            er = ERROR_PATH_NOT_FOUND;
        }
    }

    // If the path doesn't exist, try to create the directory tree then do the move.
    if (ERROR_PATH_NOT_FOUND == er)
    {
        LPWSTR pwzLastSlash = NULL;
        for (LPWSTR pwz = const_cast<LPWSTR>(wzTarget); *pwz; ++pwz)
        {
            if (*pwz == L'\\')
            {
                pwzLastSlash = pwz;
            }
        }

        if (pwzLastSlash)
        {
            *pwzLastSlash = L'\0'; // null terminate
            hr = DirEnsureExists(wzTarget, NULL);
            *pwzLastSlash = L'\\'; // now put the slash back
            ExitOnFailureDebugTrace2(hr, "failed to create directory while moving file: '%ls' to: '%ls'", wzSource, wzTarget);

            // try to move again
            if (!::MoveFileExW(wzSource, wzTarget, dwFlags))
            {
                ExitOnLastErrorDebugTrace2(hr, "failed to move file: '%ls' to: '%ls'", wzSource, wzTarget);
            }
        }
        else // no path was specified so just return the error
        {
            hr = HRESULT_FROM_WIN32(er);
        }
    }
    else // unexpected error
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    return hr;
}


/*******************************************************************
 FileEnsureMoveWithRetry

*******************************************************************/
extern "C" HRESULT DAPI FileEnsureMoveWithRetry(
    __in LPCWSTR wzSource,
    __in LPCWSTR wzTarget,
    __in BOOL fOverwrite,
    __in BOOL fAllowCopy,
    __in DWORD cRetry,
    __in DWORD dwWaitMilliseconds
    )
{
    AssertSz(cRetry != DWORD_MAX, "Cannot pass DWORD_MAX for retry.");

    HRESULT hr = E_FAIL;
    DWORD i = 0;

    for (i = 0; FAILED(hr) && i < cRetry + 1; ++i)
    {
        if (0 < i)
        {
            ::Sleep(dwWaitMilliseconds);
        }

        hr = FileEnsureMove(wzSource, wzTarget, fOverwrite, fAllowCopy);
    }
    ExitOnFailure3(hr, "Failed to move file: '%ls' to: '%ls' after %u retries.", wzSource, wzTarget, i);

LExit:
    return hr;
}


/*******************************************************************
 FileCreateTemp - creates an empty temp file

 NOTE: uses ANSI functions internally so it is Win9x safe
********************************************************************/
extern "C" HRESULT DAPI FileCreateTemp(
    __in LPCWSTR wzPrefix,
    __in LPCWSTR wzExtension,
    __deref_opt_out_z LPWSTR* ppwzTempFile,
    __out_opt HANDLE* phTempFile
    )
{
    Assert(wzPrefix && *wzPrefix);
    HRESULT hr = S_OK;
    LPSTR pszTempPath = NULL;
    DWORD cchTempPath = MAX_PATH;

    HANDLE hTempFile = INVALID_HANDLE_VALUE;
    LPSTR pszTempFile = NULL;

    int i = 0;

    hr = StrAnsiAlloc(&pszTempPath, cchTempPath);
    ExitOnFailure(hr, "failed to allocate memory for the temp path");
    ::GetTempPathA(cchTempPath, pszTempPath);

    for (i = 0; i < 1000 && INVALID_HANDLE_VALUE == hTempFile; ++i)
    {
        hr = StrAnsiAllocFormatted(&pszTempFile, "%s%ls%05d.%ls", pszTempPath, wzPrefix, i, wzExtension);
        ExitOnFailure(hr, "failed to allocate memory for log file");

        hTempFile = ::CreateFileA(pszTempFile, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == hTempFile)
        {
            // if the file already exists, just try again
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (HRESULT_FROM_WIN32(ERROR_FILE_EXISTS) == hr)
            {
                hr = S_OK;
                continue;
            }
            ExitOnFailureDebugTrace1(hr, "failed to create file: %ls", pszTempFile);
        }
    }

    if (ppwzTempFile)
    {
        hr = StrAllocStringAnsi(ppwzTempFile, pszTempFile, 0, CP_UTF8);
    }

    if (phTempFile)
    {
        *phTempFile = hTempFile;
        hTempFile = INVALID_HANDLE_VALUE;
    }

LExit:
    ReleaseFile(hTempFile);
    ReleaseStr(pszTempFile);
    ReleaseStr(pszTempPath);

    return hr;
}


/*******************************************************************
 FileCreateTempW - creates an empty temp file

*******************************************************************/
extern "C" HRESULT DAPI FileCreateTempW(
    __in LPCWSTR wzPrefix,
    __in LPCWSTR wzExtension,
    __deref_opt_out_z LPWSTR* ppwzTempFile,
    __out_opt HANDLE* phTempFile
    )
{
    Assert(wzPrefix && *wzPrefix);
    HRESULT hr = E_FAIL;

    WCHAR wzTempPath[MAX_PATH];
    DWORD cchTempPath = countof(wzTempPath);
    LPWSTR pwzTempFile = NULL;

    HANDLE hTempFile = INVALID_HANDLE_VALUE;
    int i = 0;

    if (!::GetTempPathW(cchTempPath, wzTempPath))
        ExitOnLastError(hr, "failed to get temp path");

    for (i = 0; i < 1000 && INVALID_HANDLE_VALUE == hTempFile; ++i)
    {
        hr = StrAllocFormatted(&pwzTempFile, L"%s%s%05d.%s", wzTempPath, wzPrefix, i, wzExtension);
        ExitOnFailure(hr, "failed to allocate memory for log file");

        hTempFile = ::CreateFileW(pwzTempFile, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
        if (INVALID_HANDLE_VALUE == hTempFile)
        {
            // if the file already exists, just try again
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (HRESULT_FROM_WIN32(ERROR_FILE_EXISTS) == hr)
            {
                hr = S_OK;
                continue;
            }
            ExitOnFailureDebugTrace1(hr, "failed to create file: %ls", pwzTempFile);
        }
    }

    if (phTempFile)
    {
        *phTempFile = hTempFile;
        hTempFile = INVALID_HANDLE_VALUE;
    }

    if (ppwzTempFile)
    {
        *ppwzTempFile = pwzTempFile;
        pwzTempFile = NULL;
    }

LExit:
    ReleaseFile(hTempFile);
    ReleaseStr(pwzTempFile);

    return hr;
}


/*******************************************************************
 FileIsSame

********************************************************************/
extern "C" HRESULT DAPI FileIsSame(
    __in LPCWSTR wzFile1,
    __in LPCWSTR wzFile2,
    __out LPBOOL lpfSameFile
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile1 = NULL;
    HANDLE hFile2 = NULL;
    BY_HANDLE_FILE_INFORMATION fileInfo1 = { };
    BY_HANDLE_FILE_INFORMATION fileInfo2 = { };

    hFile1 = ::CreateFileW(wzFile1, FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
    ExitOnInvalidHandleWithLastError1(hFile1, hr, "Failed to open file 1. File = '%ls'", wzFile1);

    hFile2 = ::CreateFileW(wzFile2, FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
    ExitOnInvalidHandleWithLastError1(hFile2, hr, "Failed to open file 2. File = '%ls'", wzFile2);

    if (!::GetFileInformationByHandle(hFile1, &fileInfo1))
    {
        ExitWithLastError1(hr, "Failed to get information for file 1. File = '%ls'", wzFile1);
    }

    if (!::GetFileInformationByHandle(hFile2, &fileInfo2))
    {
        ExitWithLastError1(hr, "Failed to get information for file 2. File = '%ls'", wzFile2);
    }

    *lpfSameFile = fileInfo1.dwVolumeSerialNumber == fileInfo2.dwVolumeSerialNumber &&
        fileInfo1.nFileIndexHigh == fileInfo2.nFileIndexHigh &&
        fileInfo1.nFileIndexLow == fileInfo2.nFileIndexLow ? TRUE : FALSE;

LExit:
    ReleaseFile(hFile1);
    ReleaseFile(hFile2);

    return hr;
}

/*******************************************************************
 FileEnsureDelete - deletes a file, first removing read-only,
    hidden, or system attributes if necessary.
********************************************************************/
extern "C" HRESULT DAPI FileEnsureDelete(
    __in LPCWSTR wzFile
    )
{
    HRESULT hr = S_OK;

    DWORD dwAttrib = INVALID_FILE_ATTRIBUTES;
    if (FileExistsEx(wzFile, &dwAttrib))
    {
        if (dwAttrib & FILE_ATTRIBUTE_READONLY || dwAttrib & FILE_ATTRIBUTE_HIDDEN || dwAttrib & FILE_ATTRIBUTE_SYSTEM)
        {
            if (!::SetFileAttributesW(wzFile, FILE_ATTRIBUTE_NORMAL))
            {
                ExitOnLastError1(hr, "Failed to remove attributes from file: %ls", wzFile);
            }
        }

        if (!::DeleteFileW(wzFile))
        {
            ExitOnLastError1(hr, "Failed to delete file: %ls", wzFile);
        }
    }

LExit:
    return hr;
}

/*******************************************************************
 FileGetTime - Gets the file time of a specified file
********************************************************************/
extern "C" HRESULT DAPI FileGetTime(
    __in LPCWSTR wzFile,
    __out_opt  LPFILETIME lpCreationTime,
    __out_opt  LPFILETIME lpLastAccessTime,
    __out_opt  LPFILETIME lpLastWriteTime
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = NULL;

    hFile = ::CreateFileW(wzFile, FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE | FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    ExitOnInvalidHandleWithLastError1(hFile, hr, "Failed to open file. File = '%ls'", wzFile);

    if (!::GetFileTime(hFile, lpCreationTime, lpLastAccessTime, lpLastWriteTime))
    {
        ExitWithLastError1(hr, "Failed to get file time for file. File = '%ls'", wzFile);
    }

LExit:
    ReleaseFile(hFile);
    return hr;
}

/*******************************************************************
 FileSetTime - Sets the file time of a specified file
********************************************************************/
extern "C" HRESULT DAPI FileSetTime(
    __in LPCWSTR wzFile,
    __in_opt  const FILETIME *lpCreationTime,
    __in_opt  const FILETIME *lpLastAccessTime,
    __in_opt  const FILETIME *lpLastWriteTime
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = NULL;

    hFile = ::CreateFileW(wzFile, FILE_WRITE_ATTRIBUTES, FILE_SHARE_WRITE | FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    ExitOnInvalidHandleWithLastError1(hFile, hr, "Failed to open file. File = '%ls'", wzFile);

    if (!::SetFileTime(hFile, lpCreationTime, lpLastAccessTime, lpLastWriteTime))
    {
        ExitWithLastError1(hr, "Failed to set file time for file. File = '%ls'", wzFile);
    }

LExit:
    ReleaseFile(hFile);
    return hr;
}

/*******************************************************************
 FileReSetTime - ReSets a file's last acess and modified time to the
 creation time of the file
********************************************************************/
extern "C" HRESULT DAPI FileResetTime(
    __in LPCWSTR wzFile
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = NULL;
    FILETIME ftCreateTime;

    hFile = ::CreateFileW(wzFile, FILE_WRITE_ATTRIBUTES | FILE_READ_ATTRIBUTES, FILE_SHARE_WRITE | FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    ExitOnInvalidHandleWithLastError1(hFile, hr, "Failed to open file. File = '%ls'", wzFile);
    
    if (!::GetFileTime(hFile, &ftCreateTime, NULL, NULL))
    {
        ExitWithLastError1(hr, "Failed to get file time for file. File = '%ls'", wzFile);
    }

    if (!::SetFileTime(hFile, NULL, NULL, &ftCreateTime))
    {
        ExitWithLastError1(hr, "Failed to reset file time for file. File = '%ls'", wzFile);
    }

LExit:
    ReleaseFile(hFile);
    return hr;
}


/*******************************************************************
 FileExecutableArchitecture

*******************************************************************/
extern "C" HRESULT DAPI FileExecutableArchitecture(
    __in LPCWSTR wzFile,
    __out FILE_ARCHITECTURE *pArchitecture
    )
{
    HRESULT hr = S_OK;

    HANDLE hFile = INVALID_HANDLE_VALUE;
    DWORD cbRead = 0;
    IMAGE_DOS_HEADER DosImageHeader = { };
    IMAGE_NT_HEADERS NtImageHeader = { };

    hFile = ::CreateFileW(wzFile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        ExitWithLastError1(hr, "Failed to open file: %ls", wzFile);
    }

    if (!::ReadFile(hFile, &DosImageHeader, sizeof(DosImageHeader), &cbRead, NULL))
    {
        ExitWithLastError1(hr, "Failed to read DOS header from file: %ls", wzFile);
    }

    if (DosImageHeader.e_magic != IMAGE_DOS_SIGNATURE)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnRootFailure1(hr, "Read invalid DOS header from file: %ls", wzFile);
    }

    if (INVALID_SET_FILE_POINTER == ::SetFilePointer(hFile, DosImageHeader.e_lfanew, NULL, FILE_BEGIN))
    {
        ExitWithLastError1(hr, "Failed to seek the NT header in file: %ls", wzFile);
    }

    if (!::ReadFile(hFile, &NtImageHeader, sizeof(NtImageHeader), &cbRead, NULL))
    {
        ExitWithLastError1(hr, "Failed to read NT header from file: %ls", wzFile);
    }

    if (NtImageHeader.Signature != IMAGE_NT_SIGNATURE)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnRootFailure1(hr, "Read invalid NT header from file: %ls", wzFile);
    }

    if (IMAGE_SUBSYSTEM_NATIVE == NtImageHeader.OptionalHeader.Subsystem ||
        IMAGE_SUBSYSTEM_WINDOWS_GUI == NtImageHeader.OptionalHeader.Subsystem ||
        IMAGE_SUBSYSTEM_WINDOWS_CUI == NtImageHeader.OptionalHeader.Subsystem)
    {
        switch (NtImageHeader.FileHeader.Machine)
        {
        case IMAGE_FILE_MACHINE_I386:
            *pArchitecture = FILE_ARCHITECTURE_X86;
            break;
        case IMAGE_FILE_MACHINE_IA64:
            *pArchitecture = FILE_ARCHITECTURE_IA64;
            break;
        case IMAGE_FILE_MACHINE_AMD64:
            *pArchitecture = FILE_ARCHITECTURE_X64;
            break;
        default:
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            break;
        }
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
    }
    ExitOnFailure3(hr, "Unexpected subsystem: %d machine type: %d specified in NT header from file: %ls", NtImageHeader.OptionalHeader.Subsystem, NtImageHeader.FileHeader.Machine, wzFile);

LExit:
    if (hFile != INVALID_HANDLE_VALUE)
    {
        ::CloseHandle(hFile);
    }

    return hr;
}

/*******************************************************************
 FileToString

*******************************************************************/
extern "C" HRESULT DAPI FileToString(
    __in_z LPCWSTR wzFile,
    __out LPWSTR *psczString,
    __out_opt FILE_ENCODING *pfeEncoding
    )
{
    HRESULT hr = S_OK;
    BYTE *pbFullFileBuffer = NULL;
    DWORD cbFullFileBuffer = 0;
    BOOL fNullCharFound = FALSE;
    LPWSTR sczFileText = NULL;

    // Check if the file is ANSI
    hr = FileRead(&pbFullFileBuffer, &cbFullFileBuffer, wzFile);
    ExitOnFailure1(hr, "Failed to read file: %ls", wzFile);

    if (0 == cbFullFileBuffer)
    {
        *psczString = NULL;
        ExitFunction1(hr = S_OK);
    }

    // UTF-8 BOM
    if (cbFullFileBuffer > sizeof(UTF8BOM) && 0 == memcmp(pbFullFileBuffer, UTF8BOM, sizeof(UTF8BOM)))
    {
        if (pfeEncoding)
        {
            *pfeEncoding = FILE_ENCODING_UTF8_WITH_BOM;
        }

        hr = StrAllocStringAnsi(&sczFileText, reinterpret_cast<LPCSTR>(pbFullFileBuffer + 3), cbFullFileBuffer - 3, CP_UTF8);
        ExitOnFailure1(hr, "Failed to convert file %ls from UTF-8 as its BOM indicated", wzFile);

        *psczString = sczFileText;
        sczFileText = NULL;
    }
    // UTF-16 BOM, little endian (windows regular UTF-16)
    else if (cbFullFileBuffer > sizeof(UTF16BOM) && 0 == memcmp(pbFullFileBuffer, UTF16BOM, sizeof(UTF16BOM)))
    {
        if (pfeEncoding)
        {
            *pfeEncoding = FILE_ENCODING_UTF16_WITH_BOM;
        }

        hr = StrAllocString(psczString, reinterpret_cast<LPWSTR>(pbFullFileBuffer + 2), (cbFullFileBuffer - 2) / sizeof(WCHAR));
        ExitOnFailure(hr, "Failed to allocate copy of string");
    }
    // No BOM, let's try to detect
    else
    {
        for (DWORD i = 0; i < cbFullFileBuffer; ++i)
        {
            if (pbFullFileBuffer[i] == '\0')
            {
                fNullCharFound = TRUE;
                break;
            }
        }

        if (!fNullCharFound)
        {
            if (pfeEncoding)
            {
                *pfeEncoding = FILE_ENCODING_UTF8;
            }

            hr = StrAllocStringAnsi(&sczFileText, reinterpret_cast<LPCSTR>(pbFullFileBuffer), cbFullFileBuffer, CP_UTF8);
            if (FAILED(hr))
            {
                if (E_OUTOFMEMORY == hr)
                {
                    ExitOnFailure1(hr, "Failed to convert file %ls from UTF-8", wzFile);
                }
            }
            else
            {
                *psczString = sczFileText;
                sczFileText = NULL;
            }
        }
        else if (NULL == *psczString)
        {
            if (pfeEncoding)
            {
                *pfeEncoding = FILE_ENCODING_UTF16;
            }

            hr = StrAllocString(psczString, reinterpret_cast<LPWSTR>(pbFullFileBuffer), cbFullFileBuffer / sizeof(WCHAR));
            ExitOnFailure(hr, "Failed to allocate copy of string");
        }
    }

LExit:
    ReleaseStr(sczFileText);
    ReleaseMem(pbFullFileBuffer);

    return hr;
}

/*******************************************************************
 FileFromString

*******************************************************************/
extern "C" HRESULT DAPI FileFromString(
    __in_z LPCWSTR wzFile,
    __in DWORD dwFlagsAndAttributes,
    __in_z LPCWSTR sczString,
    __in FILE_ENCODING feEncoding
    )
{
    HRESULT hr = S_OK;
    LPSTR sczUtf8String = NULL;
    BYTE *pbFullFileBuffer = NULL;
    DWORD cbFullFileBuffer = 0;
    DWORD cbStrLen = 0;

    switch (feEncoding)
    {
    case FILE_ENCODING_UTF8:
        hr = StrAnsiAllocString(&sczUtf8String, sczString, 0, CP_UTF8);
        ExitOnFailure(hr, "Failed to convert string to UTF-8 to write UTF-8 file");
        
        cbFullFileBuffer = lstrlenA(sczUtf8String);

        pbFullFileBuffer = reinterpret_cast<BYTE *>(MemAlloc(cbFullFileBuffer, TRUE));
        ExitOnNull(pbFullFileBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for output file buffer");

        memcpy_s(pbFullFileBuffer, cbFullFileBuffer, sczUtf8String, cbFullFileBuffer);
        break;
    case FILE_ENCODING_UTF8_WITH_BOM:
        hr = StrAnsiAllocString(&sczUtf8String, sczString, 0, CP_UTF8);
        ExitOnFailure(hr, "Failed to convert string to UTF-8 to write UTF-8 file");
        
        cbStrLen = lstrlenA(sczUtf8String);
        cbFullFileBuffer = sizeof(UTF8BOM) + cbStrLen;

        pbFullFileBuffer = reinterpret_cast<BYTE *>(MemAlloc(cbFullFileBuffer, TRUE));
        ExitOnNull(pbFullFileBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for output file buffer");

        memcpy_s(pbFullFileBuffer, sizeof(UTF8BOM), UTF8BOM, sizeof(UTF8BOM));
        memcpy_s(pbFullFileBuffer + sizeof(UTF8BOM), cbStrLen, sczUtf8String, cbStrLen);
        break;
    case FILE_ENCODING_UTF16:
        cbFullFileBuffer = lstrlenW(sczString) * sizeof(WCHAR);

        pbFullFileBuffer = reinterpret_cast<BYTE *>(MemAlloc(cbFullFileBuffer, TRUE));
        ExitOnNull(pbFullFileBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for output file buffer");

        memcpy_s(pbFullFileBuffer, cbFullFileBuffer, sczString, cbFullFileBuffer);
        break;
    case FILE_ENCODING_UTF16_WITH_BOM:
        cbStrLen = lstrlenW(sczString) * sizeof(WCHAR);
        cbFullFileBuffer = sizeof(UTF16BOM) + cbStrLen;

        pbFullFileBuffer = reinterpret_cast<BYTE *>(MemAlloc(cbFullFileBuffer, TRUE));
        ExitOnNull(pbFullFileBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for output file buffer");

        memcpy_s(pbFullFileBuffer, sizeof(UTF16BOM), UTF16BOM, sizeof(UTF16BOM));
        memcpy_s(pbFullFileBuffer + sizeof(UTF16BOM), cbStrLen, sczString, cbStrLen);
        break;
    }

    hr = FileWrite(wzFile, dwFlagsAndAttributes, pbFullFileBuffer, cbFullFileBuffer, NULL);
    ExitOnFailure1(hr, "Failed to write file from string to: %ls", wzFile);

LExit:
    ReleaseStr(sczUtf8String);
    ReleaseMem(pbFullFileBuffer);

    return hr;
}
