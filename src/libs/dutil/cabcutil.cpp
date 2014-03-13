//-------------------------------------------------------------------------------------------------
// <copyright file="cabcutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Cabinet creation helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static const WCHAR CABC_MAGIC_UNICODE_STRING_MARKER = '?';
static const DWORD MAX_CABINET_HEADER_SIZE = 16 * 1024 * 1024;

// The minimum number of uncompressed bytes between FciFlushFolder() calls - if we call FciFlushFolder()
// too often (because of duplicates too close together) we theoretically ruin our compression ratio -
// left at zero to maximize install-time performance, because even a small minimum threshhold seems to
// have a high install-time performance cost for little or no size benefit. The value is left here for
// tweaking though - possible suggested values are 524288 for 512K, or 2097152 for 2MB.
static const DWORD MINFLUSHTHRESHHOLD = 0;

// structs
struct MS_CABINET_HEADER
{
    DWORD sig;
    DWORD csumHeader;
    DWORD cbCabinet;
    DWORD csumFolders;
    DWORD coffFiles;
    DWORD csumFiles;
    WORD version;
    WORD cFolders;
    WORD cFiles;
    WORD flags;
    WORD setID;
    WORD iCabinet;
};


struct MS_CABINET_ITEM
{
    DWORD cbFile;
    DWORD uoffFolderStart;
    WORD iFolder;
    WORD date;
    WORD time;
    WORD attribs;
};

struct CABC_INTERNAL_ADDFILEINFO
{
    LPCWSTR wzSourcePath;
    LPCWSTR wzEmptyPath;
};

struct CABC_DUPLICATEFILE
{
    DWORD dwFileArrayIndex;
    DWORD dwDuplicateCabFileIndex;
    LPWSTR pwzSourcePath;
    LPWSTR pwzToken;
};


struct CABC_FILE
{
    DWORD dwCabFileIndex;
    LPWSTR pwzSourcePath;
    LPWSTR pwzToken;
    PMSIFILEHASHINFO pmfHash;
    LONGLONG llFileSize;
    BOOL fHasDuplicates;
};


struct CABC_DATA
{
    LONGLONG llBytesSinceLastFlush;
    LONGLONG llFlushThreshhold;

    STRINGDICT_HANDLE shDictHandle;

    WCHAR wzCabinetPath[MAX_PATH];
    WCHAR wzEmptyFile[MAX_PATH];
    HANDLE hEmptyFile;
    DWORD dwLastFileIndex;

    DWORD cFilePaths;
    DWORD cMaxFilePaths;
    CABC_FILE *prgFiles;

    DWORD cDuplicates;
    DWORD cMaxDuplicates;
    CABC_DUPLICATEFILE *prgDuplicates;

    HRESULT hrLastError;
    BOOL fGoodCab;

    HFCI hfci;
    ERF erf;
    CCAB ccab;
    TCOMP tc;

    // Below Field are used for Cabinet Splitting
    BOOL fCabinetSplittingEnabled;
    FileSplitCabNamesCallback fileSplitCabNamesCallback;
    WCHAR wzFirstCabinetName[MAX_PATH]; // Stores Name of First Cabinet excluding ".cab" extention to help generate other names by Splitting
};

const int CABC_HANDLE_BYTES = sizeof(CABC_DATA);

//
// prototypes
//
static void FreeCabCData(
    __in CABC_DATA* pcd
    );
static HRESULT CheckForDuplicateFile(
    __in CABC_DATA *pcd,
    __out CABC_FILE **ppcf,
    __in LPCWSTR wzFileName,
    __in PMSIFILEHASHINFO *ppmfHash,
    __in LONGLONG llFileSize
    );
static HRESULT AddDuplicateFile(
    __in CABC_DATA *pcd,
    __in DWORD dwFileArrayIndex,
    __in_z LPCWSTR wzSourcePath,
    __in_opt LPCWSTR wzToken,
    __in DWORD dwDuplicateCabFileIndex
    );
static HRESULT AddNonDuplicateFile(
    __in CABC_DATA *pcd,
    __in LPCWSTR wzFile,
    __in_opt LPCWSTR wzToken,
    __in_opt const MSIFILEHASHINFO* pmfHash,
    __in LONGLONG llFileSize,
    __in DWORD dwCabFileIndex
    );
static HRESULT UpdateDuplicateFiles(
    __in const CABC_DATA *pcd
    );
static HRESULT DuplicateFile(
    __in MS_CABINET_HEADER *pHeader,
    __in const CABC_DATA *pcd,
    __in const CABC_DUPLICATEFILE *pDuplicate
    );
static HRESULT UtcFileTimeToLocalDosDateTime(
    __in const FILETIME* pFileTime,
    __out USHORT* pDate,
    __out USHORT* pTime
    );

static __callback int DIAMONDAPI CabCFilePlaced(__in PCCAB pccab, __in_z PSTR szFile, __in long cbFile, __in BOOL fContinuation, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback void * DIAMONDAPI CabCAlloc(__in ULONG cb);
static __callback void DIAMONDAPI CabCFree(__out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback INT_PTR DIAMONDAPI CabCOpen(__in_z PSTR pszFile, __in int oflag, __in int pmode, __out int *err, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback UINT FAR DIAMONDAPI CabCRead(__in INT_PTR hf, __out_bcount(cb) void FAR *memory, __in UINT cb, __out int *err, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback UINT FAR DIAMONDAPI CabCWrite(__in INT_PTR hf, __in_bcount(cb) void FAR *memory, __in UINT cb, __out int *err, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback long FAR DIAMONDAPI CabCSeek(__in INT_PTR hf, __in long dist, __in int seektype, __out int *err, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback int FAR DIAMONDAPI CabCClose(__in INT_PTR hf, __out int *err, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback int DIAMONDAPI CabCDelete(__in_z PSTR szFile, __out int *err, __out_bcount(CABC_HANDLE_BYTES) void *pv);
__success(return != FALSE) static __callback BOOL DIAMONDAPI CabCGetTempFile(__out_bcount_z(cbFile) char *szFile, __in int cbFile, __out_bcount(CABC_HANDLE_BYTES) void *pv);
__success(return != FALSE) static __callback BOOL DIAMONDAPI CabCGetNextCabinet(__in PCCAB pccab, __in ULONG ul, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback INT_PTR DIAMONDAPI CabCGetOpenInfo(__in_z PSTR pszName, __out USHORT *pdate, __out USHORT *ptime, __out USHORT *pattribs, __out int *err, __out_bcount(CABC_HANDLE_BYTES) void *pv);
static __callback long DIAMONDAPI CabCStatus(__in UINT uiTypeStatus, __in ULONG cb1, __in ULONG cb2, __out_bcount(CABC_HANDLE_BYTES) void *pv);


/********************************************************************
CabcBegin - begins creating a cabinet

NOTE: phContext must be the same handle used in AddFile and Finish.
      wzCabDir can be L"", but not NULL.
      dwMaxSize and dwMaxThresh can be 0.  A large default value will be used in that case.

********************************************************************/
extern "C" HRESULT DAPI CabCBegin(
    __in_z LPCWSTR wzCab,
    __in_z LPCWSTR wzCabDir,
    __in DWORD dwMaxFiles,
    __in DWORD dwMaxSize,
    __in DWORD dwMaxThresh,
    __in COMPRESSION_TYPE ct,
    __out HANDLE *phContext
    )
{
    Assert(wzCab && *wzCab && phContext);

    HRESULT hr = S_OK;
    CABC_DATA *pcd = NULL;
    WCHAR wzTempPath[MAX_PATH] = { };

    C_ASSERT(sizeof(MSIFILEHASHINFO) == 20);

    WCHAR wzPathBuffer [MAX_PATH] = L"";
    size_t cchPathBuffer;
    if (wzCabDir)
    {
        hr = ::StringCchLengthW(wzCabDir, MAX_PATH, &cchPathBuffer);
        ExitOnFailure(hr, "Failed to get length of cab directory");

        // Need room to terminate with L'\\' and L'\0'
        if((MAX_PATH - 1) <= cchPathBuffer || 0 == cchPathBuffer)
        {
            hr = E_INVALIDARG;
            ExitOnFailure1(hr, "Cab directory had invalid length: %u", cchPathBuffer);
        }

        hr = ::StringCchCopyW(wzPathBuffer, countof(wzPathBuffer), wzCabDir);
        ExitOnFailure(hr, "Failed to copy cab directory to buffer");

        if (L'\\' != wzPathBuffer[cchPathBuffer - 1])
        {
            hr = ::StringCchCatW(wzPathBuffer, countof(wzPathBuffer), L"\\");
            ExitOnFailure(hr, "Failed to cat \\ to end of buffer");
            ++cchPathBuffer;
        }
    }

    pcd = static_cast<CABC_DATA*>(MemAlloc(sizeof(CABC_DATA), TRUE));
    ExitOnNull(pcd, hr, E_OUTOFMEMORY, "failed to allocate cab creation data structure");

    pcd->hrLastError = S_OK;
    pcd->fGoodCab = TRUE;
    pcd->llFlushThreshhold = MINFLUSHTHRESHHOLD;

    pcd->hEmptyFile = INVALID_HANDLE_VALUE;

    pcd->fileSplitCabNamesCallback = NULL;

    if (NULL == dwMaxSize)
    {
        pcd->ccab.cb = CAB_MAX_SIZE;
        pcd->fCabinetSplittingEnabled = FALSE; // If no max cab size is supplied, cabinet splitting is not desired
    }
    else
    {
        pcd->ccab.cb = dwMaxSize * 1024 * 1024;
        pcd->fCabinetSplittingEnabled = TRUE;
    }

    if (0 == dwMaxThresh)
    {
        // Subtract 16 to magically make cabbing of uncompressed data larger than 2GB work.
        pcd->ccab.cbFolderThresh = CAB_MAX_SIZE - 16; 
    }
    else
    {
        pcd->ccab.cbFolderThresh = dwMaxThresh;
    }

    // Translate the compression type
    if (COMPRESSION_TYPE_NONE == ct)
    {
        pcd->tc = tcompTYPE_NONE;
    }
    else if (COMPRESSION_TYPE_LOW == ct)
    {
        pcd->tc = tcompTYPE_LZX | tcompLZX_WINDOW_LO;
    }
    else if (COMPRESSION_TYPE_MEDIUM == ct)
    {
        pcd->tc = TCOMPfromLZXWindow(18);
    }
    else if (COMPRESSION_TYPE_HIGH == ct)
    {
        pcd->tc = tcompTYPE_LZX | tcompLZX_WINDOW_HI;
    }
    else if (COMPRESSION_TYPE_MSZIP == ct)
    {
        pcd->tc = tcompTYPE_MSZIP;
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid compression type specified.");
    }

    if (0 == ::WideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, wzCab, -1, pcd->ccab.szCab, sizeof(pcd->ccab.szCab), NULL, NULL))
    {
        ExitWithLastError(hr, "failed to convert cab name to multi-byte");
    }

    if (0 ==  ::WideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, wzPathBuffer, -1, pcd->ccab.szCabPath, sizeof(pcd->ccab.szCab), NULL, NULL))
    {
        ExitWithLastError(hr, "failed to convert cab dir to multi-byte");
    }

    // Remember the path to the cabinet.
    hr= ::StringCchCopyW(pcd->wzCabinetPath, countof(pcd->wzCabinetPath), wzPathBuffer);
    ExitOnFailure1(hr, "Failed to copy cabinet path from path: %ls", wzPathBuffer);

    hr = ::StringCchCatW(pcd->wzCabinetPath, countof(pcd->wzCabinetPath), wzCab);
    ExitOnFailure1(hr, "Failed to concat to cabinet path cabinet name: %ls", wzCab);

    // Get the empty file to use as the blank marker for duplicates.
    if (!::GetTempPathW(countof(wzTempPath), wzTempPath))
    {
        ExitWithLastError(hr, "Failed to get temp path.");
    }

    if (!::GetTempFileNameW(wzTempPath, L"WSC", 0, pcd->wzEmptyFile))
    {
        ExitWithLastError(hr, "Failed to create a temp file name.");
    }

    // Try to open the newly created empty file (remember, GetTempFileName() is kind enough to create a file for us)
    // with a handle to automatically delete the file on close. Ignore any failure that might happen, since the worst
    // case is we'll leave a zero byte file behind in the temp folder.
    pcd->hEmptyFile = ::CreateFileW(pcd->wzEmptyFile, 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_TEMPORARY | FILE_FLAG_DELETE_ON_CLOSE, NULL);

    hr = DictCreateWithEmbeddedKey(&pcd->shDictHandle, dwMaxFiles, reinterpret_cast<void **>(&pcd->prgFiles), offsetof(CABC_FILE, pwzSourcePath), DICT_FLAG_NONE);
    ExitOnFailure(hr, "Failed to create dictionary to keep track of duplicate files");

    // Make sure to allocate at least some space, or we won't be able to realloc later if they "lied" about having zero files
    if (1 > dwMaxFiles)
    {
        dwMaxFiles = 1;
    }

    pcd->cMaxFilePaths = dwMaxFiles;
    size_t cbFileAllocSize = 0;

    hr = ::SizeTMult(pcd->cMaxFilePaths, sizeof(CABC_FILE), &(cbFileAllocSize));
    ExitOnFailure(hr, "Maximum allocation exceeded on initialization.");

    pcd->prgFiles = static_cast<CABC_FILE*>(MemAlloc(cbFileAllocSize, TRUE));
    ExitOnNull(pcd->prgFiles, hr, E_OUTOFMEMORY, "Failed to allocate memory for files.");

    // Tell cabinet API about our configuration.
    pcd->hfci = ::FCICreate(&(pcd->erf), CabCFilePlaced, CabCAlloc, CabCFree, CabCOpen, CabCRead, CabCWrite, CabCClose, CabCSeek, CabCDelete, CabCGetTempFile, &(pcd->ccab), pcd);
    if (NULL == pcd->hfci || pcd->erf.fError)
    {
        // Prefer our recorded last error, then ::GetLastError(), finally fallback to the useless "E_FAIL" error
        if (FAILED(pcd->hrLastError))
        {
            hr = pcd->hrLastError;
        }
        else
        {
            ExitWithLastError2(hr, "failed to create FCI object Oper: 0x%x Type: 0x%x", pcd->erf.erfOper, pcd->erf.erfType);
        }

        pcd->fGoodCab = FALSE;

        ExitOnFailure2(hr, "failed to create FCI object Oper: 0x%x Type: 0x%x", pcd->erf.erfOper, pcd->erf.erfType);  // TODO: can these be converted to HRESULTS?
    }

    *phContext = pcd;

LExit:
    if (FAILED(hr) && pcd && pcd->hfci)
    {
        ::FCIDestroy(pcd->hfci);
    }

    return hr;
}


/********************************************************************
CabCNextCab - This will be useful when creating multiple cabs.
Haven't needed it yet.
********************************************************************/
extern "C" HRESULT DAPI CabCNextCab(
    __in_bcount(CABC_HANDLE_BYTES) HANDLE hContext
    )
{
    UNREFERENCED_PARAMETER(hContext);
    // TODO: Make the appropriate FCIFlushCabinet and FCIFlushFolder calls
    return E_NOTIMPL;
}


/********************************************************************
CabcAddFile - adds a file to a cabinet

NOTE: hContext must be the same used in Begin and Finish
if wzToken is null, the file's original name is used within the cab
********************************************************************/
extern "C" HRESULT DAPI CabCAddFile(
    __in_z LPCWSTR wzFile,
    __in_z_opt LPCWSTR wzToken,
    __in_opt PMSIFILEHASHINFO pmfHash,
    __in_bcount(CABC_HANDLE_BYTES) HANDLE hContext
    )
{
    Assert(wzFile && *wzFile && hContext);

    HRESULT hr = S_OK;
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(hContext);
    CABC_FILE *pcfDuplicate = NULL;
    LPWSTR sczUpperCaseFile = NULL;
    LONGLONG llFileSize = 0;
    PMSIFILEHASHINFO pmfLocalHash = pmfHash;

    hr = StrAllocString(&sczUpperCaseFile, wzFile, 0);
    ExitOnFailure1(hr, "Failed to allocate new string for file %ls", wzFile);

    // Modifies the string in-place
    StrStringToUpper(sczUpperCaseFile);

    // Use Smart Cabbing if there are duplicates and if Cabinet Splitting is not desired
    // For Cabinet Spliting avoid hashing as Smart Cabbing is disabled
    if(!pcd->fCabinetSplittingEnabled)
    {
        // Store file size, primarily used to determine which files to hash for duplicates
        hr = FileSize(wzFile, &llFileSize);
        ExitOnFailure1(hr, "Failed to check size of file %ls", wzFile);

        hr = CheckForDuplicateFile(pcd, &pcfDuplicate, sczUpperCaseFile, &pmfLocalHash, llFileSize);
        ExitOnFailure1(hr, "Failed while checking for duplicate of file: %ls", wzFile);
    }

    if (pcfDuplicate) // This will be null for smart cabbing case
    {
        DWORD index;
        hr = ::PtrdiffTToDWord(pcfDuplicate - pcd->prgFiles, &index);
        ExitOnFailure1(hr, "Failed to calculate index of file name: %ls", pcfDuplicate->pwzSourcePath);

        hr = AddDuplicateFile(pcd, index, sczUpperCaseFile, wzToken, pcd->dwLastFileIndex);
        ExitOnFailure1(hr, "Failed to add duplicate of file name: %ls", pcfDuplicate->pwzSourcePath);
    }
    else
    {
        hr = AddNonDuplicateFile(pcd, sczUpperCaseFile, wzToken, pmfLocalHash, llFileSize, pcd->dwLastFileIndex);
        ExitOnFailure1(hr, "Failed to add non-duplicated file: %ls", wzFile);
    }

    ++pcd->dwLastFileIndex;

LExit:
    ReleaseStr(sczUpperCaseFile);

    // If we allocated a hash struct ourselves, free it
    if (pmfHash != pmfLocalHash)
    {
        ReleaseMem(pmfLocalHash);
    }

    return hr;
}


/********************************************************************
CabcFinish - finishes making a cabinet

NOTE: hContext must be the same used in Begin and AddFile
*********************************************************************/
extern "C" HRESULT DAPI CabCFinish(
    __in_bcount(CABC_HANDLE_BYTES) HANDLE hContext,
    __in_opt FileSplitCabNamesCallback fileSplitCabNamesCallback
    )
{
    Assert(hContext);

    HRESULT hr = S_OK;
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(hContext);
    CABC_INTERNAL_ADDFILEINFO fileInfo = { };
    DWORD dwCabFileIndex; // Total file index, counts up to pcd->dwLastFileIndex
    DWORD dwArrayFileIndex = 0; // Index into pcd->prgFiles[] array
    DWORD dwDupeArrayFileIndex = 0; // Index into pcd->prgDuplicates[] array
    LPSTR pszFileToken = NULL;
    LONGLONG llFileSize = 0;

    pcd->fileSplitCabNamesCallback = fileSplitCabNamesCallback;

    // These are used to determine whether to call FciFlushFolder() before or after the next call to FciAddFile()
    // doing so at appropriate times results in install-time performance benefits in the case of duplicate files.
    // Basically, when MSI has to extract files out of order (as it does due to our smart cabbing), it can't just jump
    // exactly to the out of order file, it must begin extracting all over again, starting from that file's CAB folder
    // (this is not the same as a regular folder, and is a concept unique to CABs).
    
    // This means MSI spends a lot of time extracting the same files twice, especially if the duplicate file has many files
    // before it in the CAB folder. To avoid this, we want to make sure whenever MSI jumps to another file in the CAB, that
    // file is at the beginning of its own folder, so no extra files need to be extracted. FciFlushFolder() causes the CAB
    // to close the current folder, and create a new folder for the next file to be added.
    
    // So to maximize our performance benefit, we must call FciFlushFolder() at every place MSI will jump "to" in the CAB sequence.
    // So, we call FciFlushFolder() before adding the original version of a duplicated file (as this will be jumped "to")
    // And we call FciFlushFolder() after adding the duplicate versions of files (as this will be jumped back "to" to get back in the regular sequence)
    BOOL fFlushBefore = FALSE;
    BOOL fFlushAfter = FALSE;

    ReleaseDict(pcd->shDictHandle);

    // We need to go through all the files, duplicates and non-duplicates, sequentially in the order they were added
    for (dwCabFileIndex = 0; dwCabFileIndex < pcd->dwLastFileIndex; ++dwCabFileIndex)
    {
        if (dwArrayFileIndex < pcd->cMaxFilePaths && pcd->prgFiles[dwArrayFileIndex].dwCabFileIndex == dwCabFileIndex) // If it's a non-duplicate file
        {
            // Just a normal, non-duplicated file.  We'll add it to the list for later checking of
            // duplicates.
            fileInfo.wzSourcePath = pcd->prgFiles[dwArrayFileIndex].pwzSourcePath;
            fileInfo.wzEmptyPath = NULL;

            // Use the provided token, otherwise default to the source file name.
            if (pcd->prgFiles[dwArrayFileIndex].pwzToken)
            {
                LPCWSTR pwzTemp = pcd->prgFiles[dwArrayFileIndex].pwzToken;
                hr = StrAnsiAllocString(&pszFileToken, pwzTemp, 0, CP_ACP);
                ExitOnFailure1(hr, "failed to convert file token to ANSI: %ls", pwzTemp);
            }
            else
            {
                LPCWSTR pwzTemp = FileFromPath(fileInfo.wzSourcePath);
                hr = StrAnsiAllocString(&pszFileToken, pwzTemp, 0, CP_ACP);
                ExitOnFailure1(hr, "failed to convert file name to ANSI: %ls", pwzTemp);
            }

            if (pcd->prgFiles[dwArrayFileIndex].fHasDuplicates)
            {
                fFlushBefore = TRUE;
            }

            llFileSize = pcd->prgFiles[dwArrayFileIndex].llFileSize;

            ++dwArrayFileIndex; // Increment into the non-duplicate array
        }
        else if (dwDupeArrayFileIndex < pcd->cMaxDuplicates && pcd->prgDuplicates[dwDupeArrayFileIndex].dwDuplicateCabFileIndex == dwCabFileIndex) // If it's a duplicate file
        {
            // For duplicate files, we point them at our empty (zero-byte) file so it takes up no space
            // in the resultant cabinet.  Later on (CabCFinish) we'll go through and change all the zero
            // byte files to point at their duplicated file index.
            //
            // Notice that duplicate files are not added to the list of file paths because all duplicate
            // files point at the same path (the empty file) so there is no point in tracking them with
            // their path.
            fileInfo.wzSourcePath = pcd->prgDuplicates[dwDupeArrayFileIndex].pwzSourcePath;
            fileInfo.wzEmptyPath = pcd->wzEmptyFile;

            // Use the provided token, otherwise default to the source file name.
            if (pcd->prgDuplicates[dwDupeArrayFileIndex].pwzToken)
            {
                LPCWSTR pwzTemp = pcd->prgDuplicates[dwDupeArrayFileIndex].pwzToken;
                hr = StrAnsiAllocString(&pszFileToken, pwzTemp, 0, CP_ACP);
                ExitOnFailure1(hr, "failed to convert duplicate file token to ANSI: %ls", pwzTemp);
            }
            else
            {
                LPCWSTR pwzTemp = FileFromPath(fileInfo.wzSourcePath);
                hr = StrAnsiAllocString(&pszFileToken, pwzTemp, 0, CP_ACP);
                ExitOnFailure1(hr, "failed to convert duplicate file name to ANSI: %ls", pwzTemp);
            }

            // Flush afterward only if this isn't a duplicate of the previous file, and at least one non-duplicate file remains to be added to the cab
            if (!(dwCabFileIndex - 1 == pcd->prgFiles[pcd->prgDuplicates[dwDupeArrayFileIndex].dwFileArrayIndex].dwCabFileIndex) &&
                !(dwDupeArrayFileIndex > 0 && dwCabFileIndex - 1 == pcd->prgDuplicates[dwDupeArrayFileIndex - 1].dwDuplicateCabFileIndex) &&
                dwArrayFileIndex < pcd->cFilePaths)
            {
                fFlushAfter = TRUE;
            }

            // We're just adding a 0-byte file, so set it appropriately
            llFileSize = 0;

            ++dwDupeArrayFileIndex; // Increment into the duplicate array
        }
        else // If it's neither duplicate nor non-duplicate, throw an error
        {
            hr = HRESULT_FROM_WIN32(ERROR_EA_LIST_INCONSISTENT);
            ExitOnRootFailure(hr, "Internal inconsistency in data structures while creating CAB file - a non-standard, non-duplicate file was encountered");
        }

        if (fFlushBefore && pcd->llBytesSinceLastFlush > pcd->llFlushThreshhold)
        {
            if (!::FCIFlushFolder(pcd->hfci, CabCGetNextCabinet, CabCStatus))
            {
                ExitWithLastError2(hr, "failed to flush FCI folder before adding file, Oper: 0x%x Type: 0x%x", pcd->erf.erfOper, pcd->erf.erfType);
            }
            pcd->llBytesSinceLastFlush = 0;
        }

        pcd->llBytesSinceLastFlush += llFileSize;

        // Add the file to the cab. Notice that we are passing our CABC_INTERNAL_ADDFILEINFO struct
        // through the pointer to an ANSI string. This is neccessary so we can smuggle through the
        // path to the empty file (should this be a duplicate file).
#pragma prefast(push)
#pragma prefast(disable:6387) // OACR is silly, pszFileToken can't be false here
        if (!::FCIAddFile(pcd->hfci, reinterpret_cast<LPSTR>(&fileInfo), pszFileToken, FALSE, CabCGetNextCabinet, CabCStatus, CabCGetOpenInfo, pcd->tc))
#pragma prefast(pop)
        {
            pcd->fGoodCab = FALSE;

            // Prefer our recorded last error, then ::GetLastError(), finally fallback to the useless "E_FAIL" error
            if (FAILED(pcd->hrLastError))
            {
                hr = pcd->hrLastError;
            }
            else
            {
                ExitWithLastError3(hr, "failed to add file to FCI object Oper: 0x%x Type: 0x%x File: %ls", pcd->erf.erfOper, pcd->erf.erfType, fileInfo.wzSourcePath);
            }

            ExitOnFailure3(hr, "failed to add file to FCI object Oper: 0x%x Type: 0x%x File: %ls", pcd->erf.erfOper, pcd->erf.erfType, fileInfo.wzSourcePath);  // TODO: can these be converted to HRESULTS?
        }

        // For Cabinet Splitting case, check for pcd->hrLastError that may be set as result of Error in CabCGetNextCabinet
        // This is required as returning False in CabCGetNextCabinet is not aborting cabinet creation and is reporting success instead
        if (pcd->fCabinetSplittingEnabled && FAILED(pcd->hrLastError))
        {
            hr = pcd->hrLastError;
            ExitOnFailure(hr, "Failed to create next cabinet name while splitting cabinet.");
        }

        if (fFlushAfter && pcd->llBytesSinceLastFlush > pcd->llFlushThreshhold)
        {
            if (!::FCIFlushFolder(pcd->hfci, CabCGetNextCabinet, CabCStatus))
            {
                ExitWithLastError2(hr, "failed to flush FCI folder after adding file, Oper: 0x%x Type: 0x%x", pcd->erf.erfOper, pcd->erf.erfType);
            }
            pcd->llBytesSinceLastFlush = 0;
        }

        fFlushAfter = FALSE;
        fFlushBefore = FALSE;
    }

    if (!pcd->fGoodCab)
    {
        // Prefer our recorded last error, then ::GetLastError(), finally fallback to the useless "E_FAIL" error
        if (FAILED(pcd->hrLastError))
        {
            hr = pcd->hrLastError;
        }
        else
        {
            ExitWithLastError2(hr, "failed while creating CAB FCI object Oper: 0x%x Type: 0x%x File: %s", pcd->erf.erfOper, pcd->erf.erfType);
        }

        ExitOnFailure2(hr, "failed while creating CAB FCI object Oper: 0x%x Type: 0x%x File: %s", pcd->erf.erfOper, pcd->erf.erfType);  // TODO: can these be converted to HRESULTS?
    }

    // Only flush the cabinet if we actually succeeded in previous calls - otherwise we just waste time (a lot on big cabs)
    if (!::FCIFlushCabinet(pcd->hfci, FALSE, CabCGetNextCabinet, CabCStatus))
    {
        // If we have a last error, use that, otherwise return the useless error
        hr = FAILED(pcd->hrLastError) ? pcd->hrLastError : E_FAIL;
        ExitOnFailure2(hr, "failed to flush FCI object Oper: 0x%x Type: 0x%x", pcd->erf.erfOper, pcd->erf.erfType);  // TODO: can these be converted to HRESULTS?
    }

    if (pcd->fGoodCab && pcd->cDuplicates)
    {
        hr = UpdateDuplicateFiles(pcd);
        ExitOnFailure1(hr, "Failed to update duplicates in cabinet: %ls", pcd->wzCabinetPath);
    }

LExit:
    ::FCIDestroy(pcd->hfci);
    FreeCabCData(pcd);
    ReleaseNullStr(pszFileToken);

    return hr;
}


/********************************************************************
CabCCancel - cancels making a cabinet

NOTE: hContext must be the same used in Begin and AddFile
*********************************************************************/
extern "C" void DAPI CabCCancel(
    __in_bcount(CABC_HANDLE_BYTES) HANDLE hContext
    )
{
    Assert(hContext);

    CABC_DATA* pcd = reinterpret_cast<CABC_DATA*>(hContext);
    ::FCIDestroy(pcd->hfci);
    FreeCabCData(pcd);
}


//
// private
//

static void FreeCabCData(
    __in CABC_DATA* pcd
    )
{
    if (pcd)
    {
        ReleaseFileHandle(pcd->hEmptyFile);

        for (DWORD i = 0; i < pcd->cFilePaths; ++i)
        {
            ReleaseStr(pcd->prgFiles[i].pwzSourcePath);
            ReleaseMem(pcd->prgFiles[i].pmfHash);
        }
        ReleaseMem(pcd->prgFiles);
        ReleaseMem(pcd->prgDuplicates);

        ReleaseMem(pcd);
    }
}

/********************************************************************
 SmartCab functions

********************************************************************/

static HRESULT CheckForDuplicateFile(
    __in CABC_DATA *pcd,
    __out CABC_FILE **ppcf,
    __in LPCWSTR wzFileName,
    __in PMSIFILEHASHINFO *ppmfHash,
    __in LONGLONG llFileSize
    )
{
    DWORD i;
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    ExitOnNull(ppcf, hr, E_INVALIDARG, "No file structure sent while checking for duplicate file");
    ExitOnNull(ppmfHash, hr, E_INVALIDARG, "No file hash structure pointer sent while checking for duplicate file");

    *ppcf = NULL; // By default, we'll set our output to NULL

    hr = DictGetValue(pcd->shDictHandle, wzFileName, reinterpret_cast<void **>(ppcf));
    // If we found it in the hash of previously added source paths, return our match immediately
    if (SUCCEEDED(hr))
    {
        ExitFunction1(hr = S_OK);
    }
    else if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed while searching for file in dictionary of previously added files");

    for (i = 0; i < pcd->cFilePaths; ++i)
    {
        // If two files have the same size, use hashing to check if they're a match
        if (llFileSize == pcd->prgFiles[i].llFileSize)
        {
            // If pcd->prgFiles[i], our potential match, hasn't been hashed yet, hash it
            if (pcd->prgFiles[i].pmfHash == NULL)
            {
                pcd->prgFiles[i].pmfHash = (PMSIFILEHASHINFO)MemAlloc(sizeof(MSIFILEHASHINFO), FALSE);
                ExitOnNull(pcd->prgFiles[i].pmfHash, hr, E_OUTOFMEMORY, "Failed to allocate memory for candidate duplicate file's MSI file hash");

                pcd->prgFiles[i].pmfHash->dwFileHashInfoSize = sizeof(MSIFILEHASHINFO);
                er = ::MsiGetFileHashW(pcd->prgFiles[i].pwzSourcePath, 0, pcd->prgFiles[i].pmfHash);
                ExitOnWin32Error1(er, hr, "Failed while getting MSI file hash of candidate duplicate file: %ls", pcd->prgFiles[i].pwzSourcePath);
            }

            // If our own file hasn't yet been hashed, hash it
            if (NULL == *ppmfHash)
            {
                *ppmfHash = (PMSIFILEHASHINFO)MemAlloc(sizeof(MSIFILEHASHINFO), FALSE);
                ExitOnNull(*ppmfHash, hr, E_OUTOFMEMORY, "Failed to allocate memory for file's MSI file hash");

                (*ppmfHash)->dwFileHashInfoSize = sizeof(MSIFILEHASHINFO);
                er = ::MsiGetFileHashW(wzFileName, 0, *ppmfHash);
                ExitOnWin32Error1(er, hr, "Failed while getting MSI file hash of file: %ls", pcd->prgFiles[i].pwzSourcePath);
            }

            // If the two file hashes are both of the expected size, and they match, we've got a match, so return it!
            if (pcd->prgFiles[i].pmfHash->dwFileHashInfoSize == (*ppmfHash)->dwFileHashInfoSize &&
                sizeof(MSIFILEHASHINFO) == (*ppmfHash)->dwFileHashInfoSize &&
                pcd->prgFiles[i].pmfHash->dwData[0] == (*ppmfHash)->dwData[0] &&
                pcd->prgFiles[i].pmfHash->dwData[1] == (*ppmfHash)->dwData[1] &&
                pcd->prgFiles[i].pmfHash->dwData[2] == (*ppmfHash)->dwData[2] && 
                pcd->prgFiles[i].pmfHash->dwData[3] == (*ppmfHash)->dwData[3])
             {
                 *ppcf = pcd->prgFiles + i;
                 ExitFunction1(hr = S_OK);
             }
        }
    }

LExit:

    return hr;
}


static HRESULT AddDuplicateFile(
    __in CABC_DATA *pcd,
    __in DWORD dwFileArrayIndex,
    __in_z LPCWSTR wzSourcePath,
    __in_opt LPCWSTR wzToken,
    __in DWORD dwDuplicateCabFileIndex
    )
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;

    // Ensure there is enough memory to store this duplicate file index.
    if (pcd->cDuplicates == pcd->cMaxDuplicates)
    {
        pcd->cMaxDuplicates += 20; // grow by a reasonable number (20 is reasonable, right?)
        size_t cbDuplicates = 0;

        hr = ::SizeTMult(pcd->cMaxDuplicates, sizeof(CABC_DUPLICATEFILE), &cbDuplicates);
        ExitOnFailure(hr, "Maximum allocation exceeded.");

        if (pcd->cDuplicates)
        {
            pv = MemReAlloc(pcd->prgDuplicates, cbDuplicates, FALSE);
            ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to reallocate memory for duplicate file.");
        }
        else
        {
            pv = MemAlloc(cbDuplicates, FALSE);
            ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate memory for duplicate file.");
        }

        ZeroMemory(reinterpret_cast<BYTE*>(pv) + (pcd->cDuplicates * sizeof(CABC_DUPLICATEFILE)), (pcd->cMaxDuplicates - pcd->cDuplicates) * sizeof(CABC_DUPLICATEFILE));

        pcd->prgDuplicates = static_cast<CABC_DUPLICATEFILE*>(pv);
        pv = NULL;
    }

    // Store the duplicate file index.
    pcd->prgDuplicates[pcd->cDuplicates].dwFileArrayIndex = dwFileArrayIndex;
    pcd->prgDuplicates[pcd->cDuplicates].dwDuplicateCabFileIndex = dwDuplicateCabFileIndex;
    pcd->prgFiles[dwFileArrayIndex].fHasDuplicates = TRUE; // Mark original file as having duplicates

    hr = StrAllocString(&pcd->prgDuplicates[pcd->cDuplicates].pwzSourcePath, wzSourcePath, 0);
    ExitOnFailure1(hr, "Failed to copy duplicate file path: %ls", wzSourcePath);

    if (wzToken && *wzToken)
    {
        hr = StrAllocString(&pcd->prgDuplicates[pcd->cDuplicates].pwzToken, wzToken, 0);
        ExitOnFailure1(hr, "Failed to copy duplicate file token: %ls", wzToken);
    }

    ++pcd->cDuplicates;

LExit:
    ReleaseMem(pv);
    return hr;
}


static HRESULT AddNonDuplicateFile(
    __in CABC_DATA *pcd,
    __in LPCWSTR wzFile,
    __in_opt LPCWSTR wzToken,
    __in_opt const MSIFILEHASHINFO* pmfHash,
    __in LONGLONG llFileSize,
    __in DWORD dwCabFileIndex
    )
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;

    // Ensure there is enough memory to store this file index.
    if (pcd->cFilePaths == pcd->cMaxFilePaths)
    {
        pcd->cMaxFilePaths += 100; // grow by a reasonable number (100 is reasonable, right?)
        size_t cbFilePaths = 0;

        hr = ::SizeTMult(pcd->cMaxFilePaths, sizeof(CABC_FILE), &cbFilePaths);
        ExitOnFailure(hr, "Maximum allocation exceeded.");

        pv = MemReAlloc(pcd->prgFiles, cbFilePaths, FALSE);
        ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to reallocate memory for file.");

        ZeroMemory(reinterpret_cast<BYTE*>(pv) + (pcd->cFilePaths * sizeof(CABC_FILE)), (pcd->cMaxFilePaths - pcd->cFilePaths) * sizeof(CABC_FILE));

        pcd->prgFiles = static_cast<CABC_FILE*>(pv);
        pv = NULL;
    }

    // Store the file index information.
    // TODO: add this to a sorted list so we can do a binary search later.
    CABC_FILE *pcf = pcd->prgFiles + pcd->cFilePaths;
    pcf->dwCabFileIndex = dwCabFileIndex;
    pcf->llFileSize = llFileSize;

    if (pmfHash && sizeof(MSIFILEHASHINFO) == pmfHash->dwFileHashInfoSize)
    {
        pcf->pmfHash = (PMSIFILEHASHINFO)MemAlloc(sizeof(MSIFILEHASHINFO), FALSE);
        ExitOnNull(pcf->pmfHash, hr, E_OUTOFMEMORY, "Failed to allocate memory for individual file's MSI file hash");

        pcf->pmfHash->dwFileHashInfoSize = sizeof(MSIFILEHASHINFO);
        pcf->pmfHash->dwData[0] = pmfHash->dwData[0];
        pcf->pmfHash->dwData[1] = pmfHash->dwData[1];
        pcf->pmfHash->dwData[2] = pmfHash->dwData[2];
        pcf->pmfHash->dwData[3] = pmfHash->dwData[3];
    }

    hr = StrAllocString(&pcf->pwzSourcePath, wzFile, 0);
    ExitOnFailure1(hr, "Failed to copy file path: %ls", wzFile);

    if (wzToken && *wzToken)
    {
        hr = StrAllocString(&pcf->pwzToken, wzToken, 0);
        ExitOnFailure1(hr, "Failed to copy file token: %ls", wzToken);
    }

    ++pcd->cFilePaths;

    hr = DictAddValue(pcd->shDictHandle, pcf);
    ExitOnFailure(hr, "Failed to add file to dictionary of added files");

LExit:
    ReleaseMem(pv);
    return hr;
}


static HRESULT UpdateDuplicateFiles(
    __in const CABC_DATA *pcd
    )
{
    HRESULT hr = S_OK;
    DWORD cbCabinet = 0;
    LARGE_INTEGER liCabinetSize = { };
    HANDLE hCabinet = INVALID_HANDLE_VALUE;
    HANDLE hCabinetMapping = NULL;
    LPVOID pv = NULL;
    MS_CABINET_HEADER *pCabinetHeader = NULL;

    hCabinet = ::CreateFileW(pcd->wzCabinetPath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (INVALID_HANDLE_VALUE == hCabinet)
    {
        ExitWithLastError1(hr, "Failed to open cabinet: %ls", pcd->wzCabinetPath);
    }

    // Shouldn't need more than 16 MB to get the whole cabinet header into memory so use that as
    // the upper bound for the memory map.
    if (!::GetFileSizeEx(hCabinet, &liCabinetSize))
    {
        ExitWithLastError1(hr, "Failed to get size of cabinet: %ls", pcd->wzCabinetPath);
    }

    if (0 == liCabinetSize.HighPart && liCabinetSize.LowPart < MAX_CABINET_HEADER_SIZE)
    {
        cbCabinet = liCabinetSize.LowPart;
    }
    else
    {
        cbCabinet = MAX_CABINET_HEADER_SIZE;
    }

    // CreateFileMapping() returns NULL on failure, not INVALID_HANDLE_VALUE
    hCabinetMapping = ::CreateFileMappingW(hCabinet, NULL, PAGE_READWRITE | SEC_COMMIT, 0, cbCabinet, NULL);
    if (NULL == hCabinetMapping || INVALID_HANDLE_VALUE == hCabinetMapping)
    {
        ExitWithLastError1(hr, "Failed to memory map cabinet file: %ls", pcd->wzCabinetPath);
    }

    pv = ::MapViewOfFile(hCabinetMapping, FILE_MAP_WRITE, 0, 0, 0);
    ExitOnNullWithLastError1(pv, hr, "Failed to map view of cabinet file: %ls", pcd->wzCabinetPath);

    pCabinetHeader = static_cast<MS_CABINET_HEADER*>(pv);

    for (DWORD i = 0; i < pcd->cDuplicates; ++i)
    {
        const CABC_DUPLICATEFILE *pDuplicateFile = pcd->prgDuplicates + i;

        hr = DuplicateFile(pCabinetHeader, pcd, pDuplicateFile);
        ExitOnFailure2(hr, "Failed to find cabinet file items at index: %d and %d", pDuplicateFile->dwFileArrayIndex, pDuplicateFile->dwDuplicateCabFileIndex);
    }

LExit:
    if (pv)
    {
        ::UnmapViewOfFile(pv);
    }
    if (hCabinetMapping)
    {
        ::CloseHandle(hCabinetMapping);
    }
    ReleaseFileHandle(hCabinet);

    return hr;
}


static HRESULT DuplicateFile(
    __in MS_CABINET_HEADER *pHeader,
    __in const CABC_DATA *pcd,
    __in const CABC_DUPLICATEFILE *pDuplicate
    )
{
    HRESULT hr = S_OK;
    BYTE *pbHeader = reinterpret_cast<BYTE*>(pHeader);
    BYTE* pbItem = pbHeader + pHeader->coffFiles;
    const MS_CABINET_ITEM *pOriginalItem = NULL;
    MS_CABINET_ITEM *pDuplicateItem = NULL;

    if (pHeader->cFiles <= pcd->prgFiles[pDuplicate->dwFileArrayIndex].dwCabFileIndex ||
        pHeader->cFiles <= pDuplicate->dwDuplicateCabFileIndex ||
        pDuplicate->dwDuplicateCabFileIndex <= pcd->prgFiles[pDuplicate->dwFileArrayIndex].dwCabFileIndex)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure3(hr, "Unexpected duplicate file indices, header cFiles: %d, file index: %d, duplicate index: %d", pHeader->cFiles, pcd->prgFiles[pDuplicate->dwFileArrayIndex].dwCabFileIndex, pDuplicate->dwDuplicateCabFileIndex);
    }

    // Step through each cabinet items until we get to the original
    // file's index.  Notice that the name of the cabinet item is
    // appended to the end of the MS_CABINET_INFO, that's why we can't
    // index straight to the data we want.
    for (DWORD i = 0; i < pcd->prgFiles[pDuplicate->dwFileArrayIndex].dwCabFileIndex; ++i)
    {
        LPCSTR szItemName = reinterpret_cast<LPCSTR>(pbItem + sizeof(MS_CABINET_ITEM));
        pbItem = pbItem + sizeof(MS_CABINET_ITEM) + lstrlenA(szItemName) + 1;
    }

    pOriginalItem = reinterpret_cast<const MS_CABINET_ITEM*>(pbItem);

    // Now pick up where we left off after the original file's index
    // was found and loop until we find the duplicate file's index.
    for (DWORD i = pcd->prgFiles[pDuplicate->dwFileArrayIndex].dwCabFileIndex; i < pDuplicate->dwDuplicateCabFileIndex; ++i)
    {
        LPCSTR szItemName = reinterpret_cast<LPCSTR>(pbItem + sizeof(MS_CABINET_ITEM));
        pbItem = pbItem + sizeof(MS_CABINET_ITEM) + lstrlenA(szItemName) + 1;
    }

    pDuplicateItem = reinterpret_cast<MS_CABINET_ITEM*>(pbItem);

    if (0 != pDuplicateItem->cbFile)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure1(hr, "Failed because duplicate file does not have a file size of zero: %d", pDuplicateItem->cbFile);
    }

    pDuplicateItem->cbFile = pOriginalItem->cbFile;
    pDuplicateItem->uoffFolderStart = pOriginalItem->uoffFolderStart;
    pDuplicateItem->iFolder = pOriginalItem->iFolder;
    // Note: we do *not* duplicate the date/time and attributes metadata from
    // the original item to the duplicate. The following lines are commented
    // so people are not tempted to put them back.
    //pDuplicateItem->date = pOriginalItem->date;
    //pDuplicateItem->time = pOriginalItem->time;
    //pDuplicateItem->attribs = pOriginalItem->attribs;

LExit:
    return hr;
}


static HRESULT UtcFileTimeToLocalDosDateTime(
    __in const FILETIME* pFileTime,
    __out USHORT* pDate,
    __out USHORT* pTime
    )
{
    HRESULT hr = S_OK;
    FILETIME ftLocal = { };

    if (!::FileTimeToLocalFileTime(pFileTime, &ftLocal))
    {
        ExitWithLastError(hr, "Filed to convert file time to local file time.");
    }

    if (!::FileTimeToDosDateTime(&ftLocal, pDate, pTime))
    {
        ExitWithLastError(hr, "Filed to convert file time to DOS date time.");
    }

LExit:
    return hr;
}


/********************************************************************
 FCI callback functions

*********************************************************************/
static __callback int DIAMONDAPI CabCFilePlaced(
    __in PCCAB pccab,
    __in_z PSTR szFile,
    __in long cbFile,
    __in BOOL fContinuation,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    UNREFERENCED_PARAMETER(pccab);
    UNREFERENCED_PARAMETER(szFile);
    UNREFERENCED_PARAMETER(cbFile);
    UNREFERENCED_PARAMETER(fContinuation);
    UNREFERENCED_PARAMETER(pv);
    return 0;
}


static __callback void * DIAMONDAPI CabCAlloc(
    __in ULONG cb
    )
{
    return MemAlloc(cb, FALSE);
}


static __callback void DIAMONDAPI CabCFree(
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    MemFree(pv);
}

static __callback INT_PTR DIAMONDAPI CabCOpen(
    __in_z PSTR pszFile,
    __in int oflag,
    __in int pmode,
    __out int *err,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(pv);
    HRESULT hr = S_OK;
    INT_PTR pFile = -1;
    DWORD dwAccess = 0;
    DWORD dwDisposition = 0;
    DWORD dwAttributes = 0;

    //
    // Translate flags for CreateFile
    //
    if (oflag & _O_CREAT)
    {
        if (pmode == _S_IREAD)
            dwAccess |= GENERIC_READ;
        else if (pmode == _S_IWRITE)
            dwAccess |= GENERIC_WRITE;
        else if (pmode == (_S_IWRITE | _S_IREAD))
            dwAccess |= GENERIC_READ | GENERIC_WRITE;

        if (oflag & _O_SHORT_LIVED)
            dwDisposition = FILE_ATTRIBUTE_TEMPORARY;
        else if (oflag & _O_TEMPORARY)
            dwAttributes |= FILE_FLAG_DELETE_ON_CLOSE;
        else if (oflag & _O_EXCL)
            dwDisposition = CREATE_NEW;
    }
    if (oflag & _O_TRUNC)
        dwDisposition = CREATE_ALWAYS;

    if (!dwAccess)
        dwAccess = GENERIC_READ;
    if (!dwDisposition)
        dwDisposition = OPEN_EXISTING;
    if (!dwAttributes)
        dwAttributes = FILE_ATTRIBUTE_NORMAL;

    // Check to see if we were passed the magic character that says 'Unicode string follows'.
    if (pszFile && CABC_MAGIC_UNICODE_STRING_MARKER == *pszFile)
    {
        pFile = reinterpret_cast<INT_PTR>(::CreateFileW(reinterpret_cast<LPCWSTR>(pszFile + 1), dwAccess, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, dwDisposition, dwAttributes, NULL));
    }
    else
    {
#pragma prefast(push)
#pragma prefast(disable:25068) // We intentionally don't use the unicode API here
        pFile = reinterpret_cast<INT_PTR>(::CreateFileA(pszFile, dwAccess, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, dwDisposition, dwAttributes, NULL));
#pragma prefast(pop)
    }

    if (INVALID_HANDLE_VALUE == reinterpret_cast<HANDLE>(pFile))
    {
        ExitOnLastError1(hr, "failed to open file: %s", pszFile);
    }

LExit:
    if (FAILED(hr))
        pcd->hrLastError = *err = hr;

    return FAILED(hr) ? -1 : pFile;
}


static __callback UINT FAR DIAMONDAPI CabCRead(
    __in INT_PTR hf,
    __out_bcount(cb) void FAR *memory,
    __in UINT cb,
    __out int *err,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(pv);
    HRESULT hr = S_OK;
    DWORD cbRead = 0;

    ExitOnNull(hf, *err, E_INVALIDARG, "Failed to read during cabinet extraction because no file handle was provided");
    if (!::ReadFile(reinterpret_cast<HANDLE>(hf), memory, cb, &cbRead, NULL))
    {
        *err = ::GetLastError();
        ExitOnLastError(hr, "failed to read during cabinet extraction");
    }

LExit:
    if (FAILED(hr))
    {
        pcd->hrLastError = *err = hr;
    }

    return FAILED(hr) ? -1 : cbRead;
}


static __callback UINT FAR DIAMONDAPI CabCWrite(
    __in INT_PTR hf,
    __in_bcount(cb) void FAR *memory,
    __in UINT cb,
    __out int *err,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(pv);
    HRESULT hr = S_OK;
    DWORD cbWrite = 0;

    ExitOnNull(hf, *err, E_INVALIDARG, "Failed to write during cabinet extraction because no file handle was provided");
    if (!::WriteFile(reinterpret_cast<HANDLE>(hf), memory, cb, &cbWrite, NULL))
    {
        *err = ::GetLastError();
        ExitOnLastError(hr, "failed to write during cabinet extraction");
    }

LExit:
    if (FAILED(hr))
        pcd->hrLastError = *err = hr;

    return FAILED(hr) ? -1 : cbWrite;
}


static __callback long FAR DIAMONDAPI CabCSeek(
    __in INT_PTR hf,
    __in long dist,
    __in int seektype,
    __out int *err,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(pv);
    HRESULT hr = S_OK;
    DWORD dwMoveMethod;
    LONG lMove = 0;

    switch (seektype)
    {
    case 0:   // SEEK_SET
        dwMoveMethod = FILE_BEGIN;
        break;
    case 1:   /// SEEK_CUR
        dwMoveMethod = FILE_CURRENT;
        break;
    case 2:   // SEEK_END
        dwMoveMethod = FILE_END;
        break;
    default :
        dwMoveMethod = 0;
        hr = E_UNEXPECTED;
        ExitOnFailure1(hr, "unexpected seektype in FCISeek(): %d", seektype);
    }

    // SetFilePointer returns -1 if it fails (this will cause FDI to quit with an FDIERROR_USER_ABORT error.
    // (Unless this happens while working on a cabinet, in which case FDI returns FDIERROR_CORRUPT_CABINET)
    // Todo: update these comments for FCI (are they accurate for FCI as well?)
    lMove = ::SetFilePointer(reinterpret_cast<HANDLE>(hf), dist, NULL, dwMoveMethod);
    if (DWORD_MAX == lMove)
    {
        *err = ::GetLastError();
        ExitOnLastError1(hr, "failed to move file pointer %d bytes", dist);
    }

LExit:
    if (FAILED(hr))
    {
        pcd->hrLastError = *err = hr;
    }

    return FAILED(hr) ? -1 : lMove;
}


static __callback int FAR DIAMONDAPI CabCClose(
    __in INT_PTR hf,
    __out int *err,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(pv);
    HRESULT hr = S_OK;

    if (!::CloseHandle(reinterpret_cast<HANDLE>(hf)))
    {
        *err = ::GetLastError();
        ExitOnLastError(hr, "failed to close file during cabinet extraction");
    }

LExit:
    if (FAILED(hr))
    {
        pcd->hrLastError = *err = hr;
    }

    return FAILED(hr) ? -1 : 0;
}

static __callback int DIAMONDAPI CabCDelete(
    __in_z PSTR szFile,
    __out int *err,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    UNREFERENCED_PARAMETER(err);
    UNREFERENCED_PARAMETER(pv);

#pragma prefast(push)
#pragma prefast(disable:25068) // We intentionally don't use the unicode API here
    ::DeleteFileA(szFile);
#pragma prefast(pop)

    return 0;
}


__success(return != FALSE)
static __callback BOOL DIAMONDAPI CabCGetTempFile(
    __out_bcount_z(cbFile) char *szFile,
    __in int cbFile,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(pv);
    static volatile DWORD dwIndex = 0;

    HRESULT hr = S_OK;
    char szTempPath[MAX_PATH] = { };
    DWORD cchTempPath = MAX_PATH;
    DWORD dwProcessId = ::GetCurrentProcessId();
    HANDLE hTempFile = INVALID_HANDLE_VALUE;

    if (MAX_PATH < ::GetTempPathA(cchTempPath, szTempPath))
    {
        ExitWithLastError(hr, "Failed to get temp path during cabinet creation.");
    }

    for (DWORD i = 0; i < DWORD_MAX; ++i)
    {
        LONG dwTempIndex = ::InterlockedIncrement(reinterpret_cast<volatile LONG*>(&dwIndex));

        hr = ::StringCbPrintfA(szFile, cbFile, "%s\\%08x.%03x", szTempPath, dwTempIndex, dwProcessId);
        ExitOnFailure(hr, "failed to format log file path.");

        hTempFile = ::CreateFileA(szFile, 0, FILE_SHARE_DELETE, NULL, CREATE_NEW, FILE_ATTRIBUTE_TEMPORARY | FILE_FLAG_DELETE_ON_CLOSE, NULL);
        if (INVALID_HANDLE_VALUE != hTempFile)
        {
            // we found one that doesn't exist
            hr = S_OK;
            break;
        }
        else
        {
            hr = E_FAIL; // this file was taken so be pessimistic and assume we're not going to find one.
        }
    }
    ExitOnFailure(hr, "failed to find temporary file.");

LExit:
    ReleaseFileHandle(hTempFile);

    if (FAILED(hr))
    {
        pcd->hrLastError = hr;
    }

    return FAILED(hr)? FALSE : TRUE;
}


__success(return != FALSE)
static __callback BOOL DIAMONDAPI CabCGetNextCabinet(
    __in PCCAB pccab,
    __in ULONG ul,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    UNREFERENCED_PARAMETER(ul);

    // Construct next cab names like cab1a.cab, cab1b.cab, cab1c.cab, ........
    CABC_DATA *pcd = reinterpret_cast<CABC_DATA*>(pv);
    HRESULT hr = S_OK;
    LPWSTR pwzFileToken = NULL;
    WCHAR wzNewCabName[MAX_PATH] = L"";

    if (pccab->iCab == 1)
    {
        pcd->wzFirstCabinetName[0] = '\0';
        LPCWSTR pwzCabinetName = FileFromPath(pcd->wzCabinetPath);
        size_t len = wcsnlen(pwzCabinetName, sizeof(pwzCabinetName));
        if (len > 4)
        {
            len -= 4; // remove Extention ".cab" of 8.3 Format
        }
        hr = ::StringCchCatNW(pcd->wzFirstCabinetName, countof(pcd->wzFirstCabinetName), pwzCabinetName, len);
        ExitOnFailure(hr, "Failed to remove extension to create next Cabinet File Name");
    }

    const int nAlphabets = 26; // Number of Alphabets from a to z
    if (pccab->iCab <= nAlphabets)
    {
        // Construct next cab names like cab1a.cab, cab1b.cab, cab1c.cab, ........
        hr = ::StringCchPrintfA(pccab->szCab, sizeof(pccab->szCab), "%ls%c.cab", pcd->wzFirstCabinetName, char(((int)('a') - 1) + pccab->iCab));
        ExitOnFailure(hr, "Failed to create next Cabinet File Name");
        hr = ::StringCchPrintfW(wzNewCabName, countof(wzNewCabName), L"%ls%c.cab", pcd->wzFirstCabinetName, WCHAR(((int)('a') - 1) + pccab->iCab));
        ExitOnFailure(hr, "Failed to create next Cabinet File Name");
    }
    else if (pccab->iCab <= nAlphabets*nAlphabets)
    {
        // Construct next cab names like cab1aa.cab, cab1ab.cab, cab1ac.cab, ......, cabaz.cab, cabaa.cab, cabab.cab, cabac.cab, ......
        int char2 = (pccab->iCab) % nAlphabets;
        int char1 = (pccab->iCab - char2)/nAlphabets;
        if (char2 == 0) 
        {
            // e.g. when iCab = 52, we want az
            char2 = nAlphabets; // Second char must be 'z' in this case
            char1--; // First Char must be decremented by 1
        }
        hr = ::StringCchPrintfA(pccab->szCab, sizeof(pccab->szCab), "%ls%c%c.cab", pcd->wzFirstCabinetName, char(((int)('a') - 1) + char1), char(((int)('a') - 1) + char2));
        ExitOnFailure(hr, "Failed to create next Cabinet File Name");
        hr = ::StringCchPrintfW(wzNewCabName, countof(wzNewCabName), L"%ls%c%c.cab", pcd->wzFirstCabinetName, WCHAR(((int)('a') - 1) + char1), WCHAR(((int)('a') - 1) + char2));
        ExitOnFailure(hr, "Failed to create next Cabinet File Name");
    }
    else
    {
        hr = DISP_E_BADINDEX; // Value 0x8002000B stands for Invalid index.
        ExitOnFailure(hr, "Cannot Split Cabinet more than 26*26 = 676 times. Failed to create next Cabinet File Name");
    }

    // Callback from PFNFCIGETNEXTCABINET CabCGetNextCabinet method
    if(pcd->fileSplitCabNamesCallback != 0)
    {
        // In following if/else block, getting the Token for the First File in the Cabinets that are getting Split
        // This code will need updation if we need to send all file tokens for the splitting Cabinets
        if (pcd->prgFiles[0].pwzToken)
        {
            pwzFileToken = pcd->prgFiles[0].pwzToken;
        }
        else
        {
            LPCWSTR wzSourcePath = pcd->prgFiles[0].pwzSourcePath;
            pwzFileToken = FileFromPath(wzSourcePath);
        }

        // The call back to Binder to Add File Transfer for new Cab and add new Cab to Media table
        pcd->fileSplitCabNamesCallback(pcd->wzFirstCabinetName, wzNewCabName, pwzFileToken);
    }

LExit:
    if (FAILED(hr))
    {
        // Returning False in case of error here as stated by Documentation, However It fails to Abort Cab Creation!!!
        // So Using separate check for pcd->hrLastError after ::FCIAddFile for Cabinet Splitting
        pcd->hrLastError = hr;
        return FALSE;
    }
    else
    {
        return TRUE;
    }
}


static __callback INT_PTR DIAMONDAPI CabCGetOpenInfo(
    __in_z PSTR pszName,
    __out USHORT *pdate,
    __out USHORT *ptime,
    __out USHORT *pattribs,
    __out int *err,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    HRESULT hr = S_OK;
    CABC_INTERNAL_ADDFILEINFO* pFileInfo = reinterpret_cast<CABC_INTERNAL_ADDFILEINFO*>(pszName);
    LPCWSTR wzFile = NULL;
    DWORD cbFile = 0;
    LPSTR pszFilePlusMagic = NULL;
    DWORD cbFilePlusMagic = 0;
    WIN32_FILE_ATTRIBUTE_DATA fad = { };
    INT_PTR iResult = -1;

    // If there is an empty file provided, use that as the source path to cab (since we
    // must be dealing with a duplicate file). Otherwise, use the source path you'd expect.
    wzFile = pFileInfo->wzEmptyPath ? pFileInfo->wzEmptyPath : pFileInfo->wzSourcePath;
    cbFile = (lstrlenW(wzFile) + 1) * sizeof(WCHAR);

    // Convert the source file path into an Ansi string that our APIs will recognize as
    // a Unicode string (due to the magic character).
    cbFilePlusMagic = cbFile + 1; // add one for the magic.
    pszFilePlusMagic = reinterpret_cast<LPSTR>(MemAlloc(cbFilePlusMagic, TRUE));

    *pszFilePlusMagic = CABC_MAGIC_UNICODE_STRING_MARKER;
    memcpy_s(pszFilePlusMagic + 1, cbFilePlusMagic - 1, wzFile, cbFile);

    if (!::GetFileAttributesExW(pFileInfo->wzSourcePath, GetFileExInfoStandard, &fad))
    {
        ExitWithLastError1(hr, "Failed to get file attributes on '%s'.", pFileInfo->wzSourcePath);
    }

    // Set the attributes but only allow the few attributes that CAB supports.
    *pattribs = static_cast<USHORT>(fad.dwFileAttributes) & (FILE_ATTRIBUTE_READONLY | FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM | FILE_ATTRIBUTE_ARCHIVE);

    hr = UtcFileTimeToLocalDosDateTime(&fad.ftLastWriteTime, pdate, ptime);
    if (FAILED(hr))
    {
        // NOTE: Changed this from ftLastWriteTime to ftCreationTime because of issues around how different OSs were
        // handling the access of the FILETIME structure and how it would fail conversion to DOS time if it wasn't
        // found. This would create further problems if the file was written to the CAB without this value. Windows
        // Installer would then fail to extract the file.
        hr = UtcFileTimeToLocalDosDateTime(&fad.ftCreationTime, pdate, ptime);
        ExitOnFailure1(hr, "Filed to read a valid file time stucture on file '%s'.", pszName);
    }

    iResult = CabCOpen(pszFilePlusMagic, _O_BINARY|_O_RDONLY, 0, err, pv);

LExit:
    ReleaseMem(pszFilePlusMagic);
    if (FAILED(hr))
    {
        *err = (int)hr;
    }

    return FAILED(hr) ? -1 : iResult;
}


static __callback long DIAMONDAPI CabCStatus(
    __in UINT ui,
    __in ULONG cb1,
    __in ULONG cb2,
    __out_bcount(CABC_HANDLE_BYTES) void *pv
    )
{
    UNREFERENCED_PARAMETER(ui);
    UNREFERENCED_PARAMETER(cb1);
    UNREFERENCED_PARAMETER(cb2);
    UNREFERENCED_PARAMETER(pv);
    return 0;
}
