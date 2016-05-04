// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


/*******************************************************************
 DirExists

*******************************************************************/
extern "C" BOOL DAPI DirExists(
    __in_z LPCWSTR wzPath, 
    __out_opt DWORD *pdwAttributes
    )
{
    Assert(wzPath);

    BOOL fExists = FALSE;

    DWORD dwAttributes = ::GetFileAttributesW(wzPath);
    if (0xFFFFFFFF == dwAttributes) // TODO: figure out why "INVALID_FILE_ATTRIBUTES" can't be used here
    {
        ExitFunction();
    }

    if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY)
    {
        if (pdwAttributes)
        {
            *pdwAttributes = dwAttributes;
        }

        fExists = TRUE;
    }

LExit:
    return fExists;
}


/*******************************************************************
 DirCreateTempPath

 *******************************************************************/
extern "C" HRESULT DAPI DirCreateTempPath(
    __in_z LPCWSTR wzPrefix,
    __out_ecount_z(cchPath) LPWSTR wzPath,
    __in DWORD cchPath
    )
{
    Assert(wzPrefix);
    Assert(wzPath);

    HRESULT hr = S_OK;

    WCHAR wzDir[MAX_PATH];
    WCHAR wzFile[MAX_PATH];
    DWORD cch = 0;

    cch = ::GetTempPathW(countof(wzDir), wzDir);
    if (!cch || cch >= countof(wzDir))
    {
        ExitWithLastError(hr, "Failed to GetTempPath.");
    }

    if (!::GetTempFileNameW(wzDir, wzPrefix, 0, wzFile))
    {
        ExitWithLastError(hr, "Failed to GetTempFileName.");
    }

    hr = ::StringCchCopyW(wzPath, cchPath, wzFile);

LExit:
    return hr;
}


/*******************************************************************
 DirEnsureExists

*******************************************************************/
extern "C" HRESULT DAPI DirEnsureExists(
    __in_z LPCWSTR wzPath, 
    __in_opt LPSECURITY_ATTRIBUTES psa
    )
{
    HRESULT hr = S_OK;
    UINT er;

    // try to create this directory
    if (!::CreateDirectoryW(wzPath, psa))
    {
        // if the directory already exists, bail
        er = ::GetLastError();
        if (ERROR_ALREADY_EXISTS == er)
        {
            ExitFunction1(hr = S_OK);
        }
        else if (ERROR_PATH_NOT_FOUND != er && DirExists(wzPath, NULL)) // if the directory happens to exist (don't check if CreateDirectory said it doesn't), declare success.
        {
            ExitFunction1(hr = S_OK);
        }

        // get the parent path and try to create it
        LPWSTR pwzLastSlash = NULL;
        for (LPWSTR pwz = const_cast<LPWSTR>(wzPath); *pwz; ++pwz)
        {
            if (*pwz == L'\\')
            {
                pwzLastSlash = pwz;
            }
        }

        // if there is no parent directory fail
        ExitOnNullDebugTrace(pwzLastSlash, hr, HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND), "cannot find parent path");

        *pwzLastSlash = L'\0'; // null terminate the parent path
        hr = DirEnsureExists(wzPath, psa);   // recurse!
        *pwzLastSlash = L'\\';  // put the slash back
        ExitOnFailureDebugTrace1(hr, "failed to create path: %ls", wzPath);

        // try to create the directory now that all parents are created
        if (!::CreateDirectoryW(wzPath, psa))
        {
            // if the directory already exists for some reason no error
            er = ::GetLastError();
            if (ERROR_ALREADY_EXISTS == er)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(er);
            }
        }
        else
        {
            hr = S_OK;
        }
    }

LExit:
    return hr;
}


/*******************************************************************
 DirEnsureDelete - removes an entire directory structure

*******************************************************************/
extern "C" HRESULT DAPI DirEnsureDelete(
    __in_z LPCWSTR wzPath,
    __in BOOL fDeleteFiles,
    __in BOOL fRecurse
    )
{
    HRESULT hr = S_OK;
    DWORD dwDeleteFlags = 0;

    dwDeleteFlags |= fDeleteFiles ? DIR_DELETE_FILES : 0;
    dwDeleteFlags |= fRecurse ? DIR_DELETE_RECURSE : 0;

    hr = DirEnsureDeleteEx(wzPath, dwDeleteFlags);
    return hr;
}


/*******************************************************************
 DirEnsureDeleteEx - removes an entire directory structure

*******************************************************************/
extern "C" HRESULT DAPI DirEnsureDeleteEx(
    __in_z LPCWSTR wzPath,
    __in DWORD dwFlags
    )
{
    Assert(wzPath && *wzPath);

    HRESULT hr = S_OK;
    DWORD er;

    DWORD dwAttrib;
    HANDLE hFind = INVALID_HANDLE_VALUE;
    LPWSTR sczDelete = NULL;
    WIN32_FIND_DATAW wfd;

    BOOL fDeleteFiles = (DIR_DELETE_FILES == (dwFlags & DIR_DELETE_FILES));
    BOOL fRecurse = (DIR_DELETE_RECURSE == (dwFlags & DIR_DELETE_RECURSE));
    BOOL fScheduleDelete = (DIR_DELETE_SCHEDULE == (dwFlags & DIR_DELETE_SCHEDULE));
    WCHAR wzTempDirectory[MAX_PATH] = { };
    WCHAR wzTempPath[MAX_PATH] = { };

    if (-1 == (dwAttrib = ::GetFileAttributesW(wzPath)))
    {
        er = ::GetLastError();
        if (ERROR_FILE_NOT_FOUND == er) // change "file not found" to "path not found" since we were looking for a directory.
        {
            er = ERROR_PATH_NOT_FOUND;
        }
        hr = HRESULT_FROM_WIN32(er);
        ExitOnRootFailure1(hr, "Failed to get attributes for path: %ls", wzPath);
    }

    if (dwAttrib & FILE_ATTRIBUTE_DIRECTORY)
    {
        if (dwAttrib & FILE_ATTRIBUTE_READONLY)
        {
            if (!::SetFileAttributesW(wzPath, FILE_ATTRIBUTE_NORMAL))
            {
                ExitWithLastError1(hr, "Failed to remove read-only attribute from path: %ls", wzPath);
            }
        }

        // If we're deleting files and/or child directories loop through the contents of the directory.
        if (fDeleteFiles || fRecurse)
        {
            if (fScheduleDelete)
            {
                if (!::GetTempPathW(countof(wzTempDirectory), wzTempDirectory))
                {
                    ExitWithLastError(hr, "Failed to get temp directory.");
                }
            }

            // Delete everything in this directory.
            hr = PathConcat(wzPath, L"*.*", &sczDelete);
            ExitOnFailure1(hr, "Failed to concat wild cards to string: %ls", wzPath);

            hFind = ::FindFirstFileW(sczDelete, &wfd);
            if (INVALID_HANDLE_VALUE == hFind)
            {
                ExitWithLastError1(hr, "failed to get first file in directory: %ls", wzPath);
            }

            do
            {
                // Skip the dot directories.
                if (L'.' == wfd.cFileName[0] && (L'\0' == wfd.cFileName[1] || (L'.' == wfd.cFileName[1] && L'\0' == wfd.cFileName[2])))
                {
                    continue;
                }

                // For extra safety and to silence OACR.
                wfd.cFileName[MAX_PATH - 1] = L'\0';

                hr = PathConcat(wzPath, wfd.cFileName, &sczDelete);
                ExitOnFailure2(hr, "Failed to concat filename '%ls' to directory: %ls", wfd.cFileName, wzPath);

                if (fRecurse && wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
                {
                    hr = PathBackslashTerminate(&sczDelete);
                    ExitOnFailure1(hr, "Failed to ensure path is backslash terminated: %ls", sczDelete);

                    hr = DirEnsureDeleteEx(sczDelete, dwFlags); // recursive call
                    if (FAILED(hr))
                    {
                      // if we failed to delete a subdirectory, keep trying to finish any remaining files
                      ExitTrace1(hr, "Failed to delete subdirectory; continuing: %ls", sczDelete);
                      hr = S_OK;
                    }
                }
                else if (fDeleteFiles)  // this is a file, just delete it
                {
                    if (wfd.dwFileAttributes & FILE_ATTRIBUTE_READONLY || wfd.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN || wfd.dwFileAttributes & FILE_ATTRIBUTE_SYSTEM)
                    {
                        if (!::SetFileAttributesW(sczDelete, FILE_ATTRIBUTE_NORMAL))
                        {
                            ExitWithLastError1(hr, "Failed to remove attributes from file: %ls", sczDelete);
                        }
                    }

                    if (!::DeleteFileW(sczDelete))
                    {
                        if (fScheduleDelete)
                        {
                            if (!::GetTempFileNameW(wzTempDirectory, L"DEL", 0, wzTempPath))
                            {
                                ExitWithLastError(hr, "Failed to get temp file to move to.");
                            }

                            // Try to move the file to the temp directory then schedule for delete,
                            // otherwise just schedule for delete.
                            if (::MoveFileExW(sczDelete, wzTempPath, MOVEFILE_REPLACE_EXISTING))
                            {
                                ::MoveFileExW(wzTempPath, NULL, MOVEFILE_DELAY_UNTIL_REBOOT);
                            }
                            else
                            {
                                ::MoveFileExW(sczDelete, NULL, MOVEFILE_DELAY_UNTIL_REBOOT);
                            }
                        }
                        else
                        {
                            ExitWithLastError1(hr, "Failed to delete file: %ls", sczDelete);
                        }
                    }
                }
            } while (::FindNextFileW(hFind, &wfd));

            er = ::GetLastError();
            if (ERROR_NO_MORE_FILES == er)
            {
                hr = S_OK;
            }
            else
            {
                ExitWithLastError1(hr, "Failed while looping through files in directory: %ls", wzPath);
            }
        }

        if (!::RemoveDirectoryW(wzPath))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            if (HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) == hr && fScheduleDelete)
            {
                if (::MoveFileExW(wzPath, NULL, MOVEFILE_DELAY_UNTIL_REBOOT))
                {
                    hr = S_OK;
                }
            }

            ExitOnRootFailure1(hr, "Failed to remove directory: %ls", wzPath);
        }
    }
    else
    {
        hr = E_UNEXPECTED;
        ExitOnFailure1(hr, "Directory delete cannot delete file: %ls", wzPath);
    }

    Assert(S_OK == hr);

LExit:
    ReleaseFileFindHandle(hFind);
    ReleaseStr(sczDelete);

    return hr;
}


/*******************************************************************
DirDeleteEmptyDirectoriesToRoot - removes an empty directory and as many
                                  of its parents as possible.

 Returns: count of directories deleted.
*******************************************************************/
extern "C" DWORD DAPI DirDeleteEmptyDirectoriesToRoot(
    __in_z LPCWSTR wzPath,
    __in DWORD /*dwFlags*/
    )
{
    DWORD cDeletedDirs = 0;
    LPWSTR sczPath = NULL;

    while (wzPath && *wzPath && ::RemoveDirectoryW(wzPath))
    {
        ++cDeletedDirs;

        HRESULT hr = PathGetParentPath(wzPath, &sczPath);
        ExitOnFailure(hr, "Failed to get parent directory for path: %ls", wzPath);

        wzPath = sczPath;
    }

LExit:
    ReleaseStr(sczPath);

    return cDeletedDirs;
}


/*******************************************************************
 DirGetCurrent - gets the current directory.

*******************************************************************/
extern "C" HRESULT DAPI DirGetCurrent(
    __deref_out_z LPWSTR* psczCurrentDirectory
    )
{
    HRESULT hr = S_OK;
    DWORD_PTR cch = 0;

    if (psczCurrentDirectory && *psczCurrentDirectory)
    {
        hr = StrMaxLength(*psczCurrentDirectory, &cch);
        ExitOnFailure(hr, "Failed to determine size of current directory.");
    }

    DWORD cchRequired = ::GetCurrentDirectoryW(static_cast<DWORD>(cch), 0 == cch ? NULL : *psczCurrentDirectory);
    if (0 == cchRequired)
    {
        ExitWithLastError(hr, "Failed to get current directory.");
    }
    else if (cch < cchRequired)
    {
        hr = StrAlloc(psczCurrentDirectory, cchRequired);
        ExitOnFailure(hr, "Failed to allocate string for current directory.");

        if (!::GetCurrentDirectoryW(cchRequired, *psczCurrentDirectory))
        {
            ExitWithLastError(hr, "Failed to get current directory using allocated string.");
        }
    }

LExit:
    return hr;
}


/*******************************************************************
 DirSetCurrent - sets the current directory.

*******************************************************************/
extern "C" HRESULT DAPI DirSetCurrent(
    __in_z LPCWSTR wzDirectory
    )
{
    HRESULT hr = S_OK;

    if (!::SetCurrentDirectoryW(wzDirectory))
    {
        ExitWithLastError1(hr, "Failed to set current directory to: %ls", wzDirectory);
    }

LExit:
    return hr;
}
