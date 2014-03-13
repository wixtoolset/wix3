//---------------------------------------------------------------------
// <copyright file="Extract.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

#include "precomp.h"

//---------------------------------------------------------------------
// CABINET EXTRACTION
//---------------------------------------------------------------------

// Globals make this code unsuited for multhreaded use,
// but FDI doesn't provide any other way to pass context.

// Handle to the FDI (cab extraction) engine. Need access to this in a callback.
static HFDI g_hfdi;

// FDI is not unicode-aware, so avoid passing these paths through the callbacks.
static const wchar_t* g_szExtractDir;
static const wchar_t* g_szCabFile;

// Offset into the source file where the cabinet really starts.
// Used to trick FDI into extracting from a concatenated cabinet.
static int g_lCabOffset;

// Use the secure CRT version of _wsopen if available.
#ifdef __GOT_SECURE_LIB__
#define _wsopen__s(hf,file,oflag,shflag,pmode) _wsopen_s(&hf,file,oflag,shflag,pmode)
#else
#define _wsopen__s(hf,file,oflag,shflag,pmode) hf = _wsopen(file,oflag,shflag,pmode)
#endif

/// <summary>
/// FDI callback to open a cabinet file.
/// </summary>
/// <param name="pszFile">Name of the file to be opened. This parameter
/// is ignored since with our limited use this method is only ever called
/// to open the main cabinet file.</param>
/// <param name="oflag">Type of operations allowed.</param>
/// <param name="pmode">Permission setting.</param>
/// <returns>Integer file handle, or -1 if the file could not be opened.</returns>
/// <remarks>
/// To support reading from a cabinet that is concatenated onto
/// another file, this function first searches for the offset of the cabinet,
/// then saves that offset for use in recalculating later seeks.
/// </remarks>
static FNOPEN(CabOpen)
{
	UNREFERENCED_PARAMETER(pszFile);
	int hf;
	_wsopen__s(hf, g_szCabFile, oflag, _SH_DENYWR, pmode);
	if (hf != -1)
	{
		FDICABINETINFO cabInfo;
		int length = _lseek(hf, 0, SEEK_END);
		for(int offset = 0; offset < length; offset += 256)
		{
			if (_lseek(hf, offset, SEEK_SET) != offset) break;
			if (FDIIsCabinet(g_hfdi, hf, &cabInfo))
			{
				g_lCabOffset = offset;
				_lseek(hf, offset, SEEK_SET);
				return hf;
			}
		}
		_close(hf);
	}
	return -1;
}

/// <summary>
/// FDI callback to seek within a file.
/// </summary>
/// <param name="hf">File handle.</param>
/// <param name="dist">Seek distance</param>
/// <param name="seektype">Whether to seek relative to the
/// beginning, current position, or end of the file.</param>
/// <returns>Resultant position within the cabinet.</returns>
/// <remarks>
/// To support reading from a cabinet that is concatenated onto
/// another file, this function recalculates seeks based on the
/// offset that was determined when the cabinet was opened.
/// </remarks>
static FNSEEK(CabSeek)
{
	if (seektype == SEEK_SET) dist += g_lCabOffset;
	int pos = _lseek((int) hf, dist, seektype);
	pos -= g_lCabOffset;
	return pos;
}

/// <summary>
/// Ensures a directory and its parent directory path exists.
/// </summary>
/// <param name="szDirPath">Directory path, not including file name.</param>
/// <returns>0 if the directory exists or was successfully created, else nonzero.</returns>
/// <remarks>
/// This function modifies characters in szDirPath, but always restores them
/// regardless of error condition.
/// </remarks>
static int EnsureDirectoryExists(__inout_z wchar_t* szDirPath)
{
	int ret = 0;
	if (!::CreateDirectoryW(szDirPath, NULL))
	{
		UINT err = ::GetLastError();
		if (err != ERROR_ALREADY_EXISTS)
		{
			// Directory creation failed for some reason other than already existing.
			// Try to create the parent directory first.
			wchar_t* szLastSlash = NULL;
			for (wchar_t* sz = szDirPath; *sz; sz++)
			{
				if (*sz == L'\\')
				{
					szLastSlash = sz;
				}
			}
			if (szLastSlash)
			{
				// Temporarily take one directory off the path and recurse.
				*szLastSlash = L'\0';
				ret = EnsureDirectoryExists(szDirPath);
				*szLastSlash = L'\\';

				// Try to create the directory if all parents are created.
				if (ret == 0 && !::CreateDirectoryW(szDirPath, NULL))
				{
					err = ::GetLastError();
					if (err != ERROR_ALREADY_EXISTS)
					{
						ret = -1;
					}
				}
			}
			else
			{
				ret = -1;
			}
		}
	}
	return ret;
}

/// <summary>
/// Ensures a file's directory and its parent directory path exists.
/// </summary>
/// <param name="szDirPath">Path including file name.</param>
/// <returns>0 if the file's directory exists or was successfully created, else nonzero.</returns>
/// <remarks>
/// This function modifies characters in szFilePath, but always restores them
/// regardless of error condition.
/// </remarks>
static int EnsureFileDirectoryExists(__inout_z wchar_t* szFilePath)
{
	int ret = 0;
	wchar_t* szLastSlash = NULL;
	for (wchar_t* sz = szFilePath; *sz; sz++)
	{
		if (*sz == L'\\')
		{
			szLastSlash = sz;
		}
	}
	if (szLastSlash)
	{
		*szLastSlash = L'\0';
		ret = EnsureDirectoryExists(szFilePath);
		*szLastSlash = L'\\';
	}
	return ret;
}

/// <summary>
/// FDI callback for handling files in the cabinet.
/// </summary>
/// <param name="fdint">Type of notification.</param>
/// <param name="pfdin">Structure containing data about the notification.</param>
/// <remarks>
/// Refer to fdi.h for more comments on this notification callback.
/// </remarks>
static FNFDINOTIFY(CabNotification)
{
	// fdintCOPY_FILE:
	//     Called for each file that *starts* in the current cabinet, giving
	//     the client the opportunity to request that the file be copied or
	//     skipped.
	//   Entry:
	//     pfdin->psz1    = file name in cabinet
	//     pfdin->cb      = uncompressed size of file
	//     pfdin->date    = file date
	//     pfdin->time    = file time
	//     pfdin->attribs = file attributes
	//     pfdin->iFolder = file's folder index
	//   Exit-Success:
	//     Return non-zero file handle for destination file; FDI writes
	//     data to this file use the PFNWRITE function supplied to FDICreate,
	//     and then calls fdintCLOSE_FILE_INFO to close the file and set
	//     the date, time, and attributes.
	//   Exit-Failure:
	//     Returns 0  => Skip file, do not copy
	//     Returns -1 => Abort FDICopy() call
	if (fdint == fdintCOPY_FILE)
	{
		size_t cchFile = MultiByteToWideChar(CP_UTF8, 0, pfdin->psz1, -1, NULL, 0);
		size_t cchFilePath = wcslen(g_szExtractDir) + 1 + cchFile;
		wchar_t* szFilePath = (wchar_t*) _alloca((cchFilePath + 1) * sizeof(wchar_t));
		if (szFilePath == NULL) return -1;
		StringCchCopyW(szFilePath, cchFilePath + 1, g_szExtractDir);
		StringCchCatW(szFilePath, cchFilePath + 1, L"\\");
		MultiByteToWideChar(CP_UTF8, 0, pfdin->psz1, -1,
			szFilePath + cchFilePath - cchFile, (int) cchFile + 1);
		int hf = -1;
		if (EnsureFileDirectoryExists(szFilePath) == 0)
		{
			_wsopen__s(hf, szFilePath,
				_O_BINARY | _O_CREAT | _O_WRONLY | _O_SEQUENTIAL,
				_SH_DENYWR, _S_IREAD | _S_IWRITE);
		}
		return hf;
	}

	// fdintCLOSE_FILE_INFO:
	//     Called after all of the data has been written to a target file.
	//     This function must close the file and set the file date, time,
	//     and attributes.
	//   Entry:
	//     pfdin->psz1    = file name in cabinet
	//     pfdin->hf      = file handle
	//     pfdin->date    = file date
	//     pfdin->time    = file time
	//     pfdin->attribs = file attributes
	//     pfdin->iFolder = file's folder index
	//     pfdin->cb      = Run After Extract (0 - don't run, 1 Run)
	//   Exit-Success:
	//     Returns TRUE
	//   Exit-Failure:
	//     Returns FALSE, or -1 to abort
	else if (fdint == fdintCLOSE_FILE_INFO)
	{
		_close((int) pfdin->hf);
		return TRUE;
	}
	return 0;
}

/// <summary>
/// Extracts all contents of a cabinet file to a directory.
/// </summary>
/// <param name="szCabFile">Path to the cabinet file to be extracted.
/// The cabinet may actually start at some offset within the file,
/// as long as that offset is a multiple of 256.</param>
/// <param name="szExtractDir">Directory where files are to be extracted.
/// This directory must already exist, but should be empty.</param>
/// <returns>0 if the cabinet was extracted successfully,
/// or an error code if any error occurred.</returns>
/// <remarks>
/// The extraction will not overwrite any files in the destination
/// directory; extraction will be interrupted and fail if any files
/// with the same name already exist.
/// </remarks>
int ExtractCabinet(const wchar_t* szCabFile, const wchar_t* szExtractDir)
{
	ERF erf;
	// Most of the FDI callbacks can be handled by existing CRT I/O functions.
	// For our functionality we only need to handle the open and seek callbacks.
	HFDI hfdi = FDICreate((PFNALLOC) malloc, (PFNFREE) free, CabOpen,
		(PFNREAD) _read, (PFNWRITE) _write, (PFNCLOSE) _close,
		CabSeek, cpu80386, &erf);
	if (hfdi != NULL)
	{
		g_hfdi = hfdi;
		g_szCabFile = szCabFile;
		g_szExtractDir = szExtractDir;
		char szEmpty[1] = {0};
		if (FDICopy(hfdi, szEmpty, szEmpty, 0, CabNotification, NULL, NULL))
		{
			FDIDestroy(hfdi);
			return 0;
		}
		FDIDestroy(hfdi);
	}

	return erf.erfOper;
}
