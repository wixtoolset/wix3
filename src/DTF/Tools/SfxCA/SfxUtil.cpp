//---------------------------------------------------------------------
// <copyright file="SfxUtil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Utility code for self-extracting managed MSI custom action
// packages and embedded external UI packages.
//
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

#include "precomp.h"
#include "SfxUtil.h"

/// <summary>
/// Writes a formatted message to the MSI log.
/// Does out-of-proc MSI calls if necessary.
/// </summary>
void Log(MSIHANDLE hSession, const wchar_t* szMessage, ...)
{
        const int LOG_BUFSIZE = 4096;
        wchar_t szBuf[LOG_BUFSIZE];
        va_list args;
        va_start(args, szMessage);
        StringCchVPrintf(szBuf, LOG_BUFSIZE, szMessage, args);

        if (!g_fRunningOutOfProc || NULL == g_pRemote)
        {
                MSIHANDLE hRec = MsiCreateRecord(1);
                MsiRecordSetString(hRec, 0, L"SFXCA: [1]");
                MsiRecordSetString(hRec, 1, szBuf);
                MsiProcessMessage(hSession, INSTALLMESSAGE_INFO, hRec);
                MsiCloseHandle(hRec);
        }
        else
        {
                // Logging is the only remote-MSI operation done from unmanaged code.
                // It's not very convenient here because part of the infrastructure
                // for remote MSI APIs is on the managed side.

                RemoteMsiSession::RequestData req;
                RemoteMsiSession::RequestData* pResp = NULL;
                SecureZeroMemory(&req, sizeof(RemoteMsiSession::RequestData));

                req.fields[0].vt = VT_UI4;
                req.fields[0].uiValue = 1;
                g_pRemote->SendRequest(RemoteMsiSession::MsiCreateRecord, &req, &pResp);
                MSIHANDLE hRec = (MSIHANDLE) pResp->fields[0].iValue;

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hRec;
                req.fields[1].vt = VT_UI4;
                req.fields[1].uiValue = 0;
                req.fields[2].vt = VT_LPWSTR;
                req.fields[2].szValue = L"SFXCA: [1]";
                g_pRemote->SendRequest(RemoteMsiSession::MsiRecordSetString, &req, &pResp);

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hRec;
                req.fields[1].vt = VT_UI4;
                req.fields[1].uiValue = 1;
                req.fields[2].vt = VT_LPWSTR;
                req.fields[2].szValue = szBuf;
                g_pRemote->SendRequest(RemoteMsiSession::MsiRecordSetString, &req, &pResp);

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hSession;
                req.fields[1].vt = VT_I4;
                req.fields[1].iValue = (int) INSTALLMESSAGE_INFO;
                req.fields[2].vt = VT_I4;
                req.fields[2].iValue = (int) hRec;
                g_pRemote->SendRequest(RemoteMsiSession::MsiProcessMessage, &req, &pResp);

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hRec;
                req.fields[1].vt = VT_EMPTY;
                req.fields[2].vt = VT_EMPTY;
                g_pRemote->SendRequest(RemoteMsiSession::MsiCloseHandle, &req, &pResp);
        }
}

/// <summary>
/// Deletes a directory, including all files and subdirectories.
/// </summary>
/// <param name="szDir">Path to the directory to delete,
/// not including a trailing backslash.</param>
/// <returns>True if the directory was successfully deleted, or false
/// if the deletion failed (most likely because some files were locked).
/// </returns>
bool DeleteDirectory(const wchar_t* szDir)
{
        size_t cchDir = wcslen(szDir);
        size_t cchPathBuf = cchDir + 3 + MAX_PATH;
        wchar_t* szPath = (wchar_t*) _alloca(cchPathBuf * sizeof(wchar_t));
        if (szPath == NULL) return false;
        StringCchCopy(szPath, cchPathBuf, szDir);
        StringCchCat(szPath, cchPathBuf, L"\\*");
        WIN32_FIND_DATA fd;
        HANDLE hSearch = FindFirstFile(szPath, &fd);
        while (hSearch != INVALID_HANDLE_VALUE)
        {
                StringCchCopy(szPath + cchDir + 1, cchPathBuf - (cchDir + 1), fd.cFileName);
                if ((fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
                {
                        if (wcscmp(fd.cFileName, L".") != 0 && wcscmp(fd.cFileName, L"..") != 0)
                        {
                                DeleteDirectory(szPath);
                        }
                }
                else
                {
                        DeleteFile(szPath);
                }
                if (!FindNextFile(hSearch, &fd))
                {
                        FindClose(hSearch);
                        hSearch = INVALID_HANDLE_VALUE;
                }
        }
        return RemoveDirectory(szDir) != 0;
}

bool DirectoryExists(const wchar_t* szDir)
{
        if (szDir != NULL)
        {
                DWORD dwAttrs = GetFileAttributes(szDir);
                if (dwAttrs != -1 && (dwAttrs & FILE_ATTRIBUTE_DIRECTORY) != 0)
                {
                        return true;
                }
        }
        return false;
}

/// <summary>
/// Extracts a cabinet that is concatenated to a module
/// to a new temporary directory.
/// </summary>
/// <param name="hSession">Handle to the installer session,
/// used just for logging.</param>
/// <param name="hModule">Module that has the concatenated cabinet.</param>
/// <param name="szTempDir">Buffer for returning the path of the
/// created temp directory.</param>
/// <param name="cchTempDirBuf">Size in characters of the buffer.
/// <returns>True if the files were extracted, or false if the
/// buffer was too small or the directory could not be created
/// or the extraction failed for some other reason.</returns>
__success(return != false)
bool ExtractToTempDirectory(__in MSIHANDLE hSession, __in HMODULE hModule,
        __out_ecount_z(cchTempDirBuf) wchar_t* szTempDir, DWORD cchTempDirBuf)
{
        wchar_t szModule[MAX_PATH];
        DWORD cchCopied = GetModuleFileName(hModule, szModule, MAX_PATH - 1);
        if (cchCopied == 0)
        {
                Log(hSession, L"Failed to get module path. Error code %d.", GetLastError());
                return false;
        }
        else if (cchCopied == MAX_PATH - 1)
        {
                Log(hSession, L"Failed to get module path -- path is too long.");
                return false;
        }

        if (szTempDir == NULL || cchTempDirBuf < wcslen(szModule) + 1)
        {
                Log(hSession, L"Temp directory buffer is NULL or too small.");
                return false;
        }
        StringCchCopy(szTempDir, cchTempDirBuf, szModule);
        StringCchCat(szTempDir, cchTempDirBuf, L"-");

        DWORD cchTempDir = (DWORD) wcslen(szTempDir);
        for (int i = 0; DirectoryExists(szTempDir); i++)
        {
                swprintf_s(szTempDir + cchTempDir, cchTempDirBuf - cchTempDir, L"%d", i);
        }

        if (!CreateDirectory(szTempDir, NULL))
        {
                cchCopied = GetTempPath(cchTempDirBuf, szTempDir);
                if (cchCopied == 0 || cchCopied >= cchTempDirBuf)
                {
                        Log(hSession, L"Failed to get temp directory. Error code %d", GetLastError());
                        return false;
                }

                wchar_t* szModuleName = wcsrchr(szModule, L'\\');
                if (szModuleName == NULL) szModuleName = szModule;
                else szModuleName = szModuleName + 1;
                StringCchCat(szTempDir, cchTempDirBuf, szModuleName);
                StringCchCat(szTempDir, cchTempDirBuf, L"-");

                cchTempDir = (DWORD) wcslen(szTempDir);
                for (int i = 0; DirectoryExists(szTempDir); i++)
                {
                        swprintf_s(szTempDir + cchTempDir, cchTempDirBuf - cchTempDir, L"%d", i);
                }

                if (!CreateDirectory(szTempDir, NULL))
                {
                        Log(hSession, L"Failed to create temp directory. Error code %d", GetLastError());
                        return false;
                }
        }

        Log(hSession, L"Extracting custom action to temporary directory: %s\\", szTempDir);
        int err = ExtractCabinet(szModule, szTempDir);
        if (err != 0)
        {
                Log(hSession, L"Failed to extract to temporary directory. Cabinet error code %d.", err);
                DeleteDirectory(szTempDir);
                return false;
        }
        return true;
}

