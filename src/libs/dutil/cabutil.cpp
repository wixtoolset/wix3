//-------------------------------------------------------------------------------------------------
// <copyright file="cabutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Cabinet decompression helper functions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// external prototypes
typedef BOOL (FAR DIAMONDAPI *PFNFDIDESTROY)(VOID*);
typedef HFDI (FAR DIAMONDAPI *PFNFDICREATE)(PFNALLOC, PFNFREE, PFNOPEN, PFNREAD, PFNWRITE, PFNCLOSE, PFNSEEK, int, PERF);
typedef BOOL (FAR DIAMONDAPI *PFNFDIISCABINET)(HFDI, INT_PTR, PFDICABINETINFO);
typedef BOOL (FAR DIAMONDAPI *PFNFDICOPY)(HFDI, char *, char *, int, PFNFDINOTIFY, PFNFDIDECRYPT, void *);


//
// static globals
//
static HMODULE vhCabinetDll = NULL;

static HFDI vhfdi = NULL;
static PFNFDICREATE vpfnFDICreate = NULL;
static PFNFDICOPY vpfnFDICopy = NULL;
static PFNFDIISCABINET vpfnFDIIsCabinet = NULL;
static PFNFDIDESTROY vpfnFDIDestroy = NULL;
static ERF verf;

static DWORD64 vdw64EmbeddedOffset = 0;

//
// structs
//
struct CAB_CALLBACK_STRUCT
{
    BOOL fStopExtracting;   // flag set when no more files are needed
    LPCWSTR pwzExtract;         // file to extract ("*" means extract all)
    LPCWSTR pwzExtractDir;      // directory to extract files to

    // possible user data
    CAB_CALLBACK_PROGRESS pfnProgress;
    LPVOID pvContext;
};

//
// prototypes
//
static __callback LPVOID DIAMONDAPI CabExtractAlloc(__in DWORD dwSize);
static __callback void DIAMONDAPI CabExtractFree(__in LPVOID pvData);
static __callback INT_PTR FAR DIAMONDAPI CabExtractOpen(__in_z PSTR pszFile, __in int oflag, __in int pmode);
static __callback UINT FAR DIAMONDAPI CabExtractRead(__in INT_PTR hf, __out void FAR *pv, __in UINT cb);
static __callback UINT FAR DIAMONDAPI CabExtractWrite(__in INT_PTR hf, __in void FAR *pv, __in UINT cb);
static __callback int FAR DIAMONDAPI CabExtractClose(__in INT_PTR hf);
static __callback long FAR DIAMONDAPI CabExtractSeek(__in INT_PTR hf, __in long dist, __in int seektype);
static __callback INT_PTR DIAMONDAPI CabExtractCallback(__in FDINOTIFICATIONTYPE iNotification, __inout FDINOTIFICATION *pFDINotify);
static HRESULT DAPI CabOperation(__in LPCWSTR wzCabinet, __in LPCWSTR wzExtractFile, __in_opt LPCWSTR wzExtractDir, __in_opt CAB_CALLBACK_PROGRESS pfnProgress, __in_opt LPVOID pvContext, __in_opt STDCALL_PFNFDINOTIFY pfnNotify, __in DWORD64 dw64EmbeddedOffset);

static STDCALL_PFNFDINOTIFY v_pfnNetFx11Notify = NULL;


inline HRESULT LoadCabinetDll()
{
    HRESULT hr = S_OK;
    if (!vhCabinetDll)
    {
        hr = LoadSystemLibrary(L"cabinet.dll", &vhCabinetDll);
        ExitOnFailure(hr, "failed to load cabinet.dll");

        // retrieve all address functions
        vpfnFDICreate = reinterpret_cast<PFNFDICREATE>(::GetProcAddress(vhCabinetDll, "FDICreate"));
        ExitOnNullWithLastError(vpfnFDICreate, hr, "failed to import FDICreate from CABINET.DLL");
        vpfnFDICopy = reinterpret_cast<PFNFDICOPY>(::GetProcAddress(vhCabinetDll, "FDICopy"));
        ExitOnNullWithLastError(vpfnFDICopy, hr, "failed to import FDICopy from CABINET.DLL");
        vpfnFDIIsCabinet = reinterpret_cast<PFNFDIISCABINET>(::GetProcAddress(vhCabinetDll, "FDIIsCabinet"));
        ExitOnNullWithLastError(vpfnFDIIsCabinet, hr, "failed to import FDIIsCabinetfrom CABINET.DLL");
        vpfnFDIDestroy = reinterpret_cast<PFNFDIDESTROY>(::GetProcAddress(vhCabinetDll, "FDIDestroy"));
        ExitOnNullWithLastError(vpfnFDIDestroy, hr, "failed to import FDIDestroyfrom CABINET.DLL");

        vhfdi = vpfnFDICreate(CabExtractAlloc, CabExtractFree, CabExtractOpen, CabExtractRead, CabExtractWrite, CabExtractClose, CabExtractSeek, cpuUNKNOWN, &verf);
        ExitOnNull(vhfdi, hr, E_FAIL, "failed to initialize cabinet.dll");
    }

LExit:
    if (FAILED(hr) && vhCabinetDll)
    {
        ::FreeLibrary(vhCabinetDll);
        vhCabinetDll = NULL;
    }

    return hr;
}


/********************************************************************
 CabInitialize - initializes internal static variables

********************************************************************/
extern "C" HRESULT DAPI CabInitialize(
    __in BOOL fDelayLoad
    )
{
    HRESULT hr = S_OK;

    if (!fDelayLoad)
    {
        hr = LoadCabinetDll();
        ExitOnFailure(hr, "failed to load CABINET.DLL");
    }

LExit:
    return hr;
}


/********************************************************************
 CabUninitialize - initializes internal static variables

********************************************************************/
extern "C" void DAPI CabUninitialize(
    )
{
    if (vhfdi)
    {
        if (vpfnFDIDestroy)
        {
            vpfnFDIDestroy(vhfdi);
        }
        vhfdi = NULL;
    }

    vpfnFDICreate = NULL;
    vpfnFDICopy =NULL;
    vpfnFDIIsCabinet = NULL;
    vpfnFDIDestroy = NULL;

    if (vhCabinetDll)
    {
        ::FreeLibrary(vhCabinetDll);
        vhCabinetDll = NULL;
    }
}

/********************************************************************
 CabEnumerate - list files inside cabinet

 NOTE: wzCabinet must be full path to cabinet file
       pfnNotify is callback function to get notified for each file
       in the cabinet
********************************************************************/
extern "C" HRESULT DAPI CabEnumerate(
    __in LPCWSTR wzCabinet,
    __in LPCWSTR wzEnumerateFile,
    __in STDCALL_PFNFDINOTIFY pfnNotify,
    __in DWORD64 dw64EmbeddedOffset
    )
{
    return CabOperation(wzCabinet, wzEnumerateFile, NULL, NULL, NULL, pfnNotify, dw64EmbeddedOffset);
}

/********************************************************************
 CabExtract - extracts one or all files from a cabinet

 NOTE: wzCabinet must be full path to cabinet file
       wzExtractFile can be a single file id or "*" to extract all files
       wzExttractDir must be normalized (end in a "\")
       if pfnBeginFile is NULL pfnEndFile must be NULL and vice versa
********************************************************************/
extern "C" HRESULT DAPI CabExtract(
    __in LPCWSTR wzCabinet,
    __in LPCWSTR wzExtractFile,
    __in LPCWSTR wzExtractDir,
    __in_opt CAB_CALLBACK_PROGRESS pfnProgress,
    __in_opt LPVOID pvContext,
    __in DWORD64 dw64EmbeddedOffset
    )
{
    return CabOperation(wzCabinet, wzExtractFile, wzExtractDir, pfnProgress, pvContext, NULL, dw64EmbeddedOffset);
}

//
// private
//
/********************************************************************
 FDINotify -- wrapper that converts call convention from __cdecl to __stdcall.

 NOTE: Since netfx 1.1 supports only function pointers (delegates) 
       with __stdcall calling convention and cabinet api uses
       __cdecl calling convention, we need this wrapper function.
       netfx 2.0 will work with [UnmanagedFunctionPointer(CallingConvention.Cdecl)] attribute on the delegate.
       TODO: remove this when upgrading to netfx 2.0.
********************************************************************/
static __callback INT_PTR DIAMONDAPI FDINotify(
    __in FDINOTIFICATIONTYPE iNotification, 
    __inout FDINOTIFICATION *pFDINotify
    )
{
    if (NULL != v_pfnNetFx11Notify)
    {
        return v_pfnNetFx11Notify(iNotification, pFDINotify);
    }
    else
    {
        return (INT_PTR)0;
    }
}


/********************************************************************
 CabOperation - helper function that enumerates or extracts files
                   from cabinet

 NOTE: wzCabinet must be full path to cabinet file
       wzExtractFile can be a single file id or "*" to extract all files
       wzExttractDir must be normalized (end in a "\")
       if pfnBeginFile is NULL pfnEndFile must be NULL and vice versa
       pfnNotify is callback function to get notified for each file
       in the cabinet. If it's NULL, files will be extracted.
********************************************************************/
static HRESULT DAPI CabOperation(
    __in LPCWSTR wzCabinet,
    __in LPCWSTR wzExtractFile,
    __in_opt LPCWSTR wzExtractDir,
    __in_opt CAB_CALLBACK_PROGRESS pfnProgress,
    __in_opt LPVOID pvContext,
    __in_opt STDCALL_PFNFDINOTIFY pfnNotify,
    __in DWORD64 dw64EmbeddedOffset
    )
{
    HRESULT hr = S_OK;
    BOOL fResult;

    LPWSTR sczCabinet = NULL;
    LPWSTR pwz = NULL;
    CHAR szCabDirectory[MAX_PATH * 4]; // Make sure these are big enough for UTF-8 strings
    CHAR szCabFile[MAX_PATH * 4];

    CAB_CALLBACK_STRUCT ccs;
    PFNFDINOTIFY pfnFdiNotify;

    //
    // ensure the cabinet.dll is loaded
    //
    if (!vhfdi)
    {
        hr = LoadCabinetDll();
        ExitOnFailure(hr, "failed to load CABINET.DLL");
    }

    hr = StrAllocString(&sczCabinet, wzCabinet, 0);
    ExitOnFailure1(hr, "Failed to make copy of cabinet name:%ls", wzCabinet);

    //
    // split the cabinet full path into directory and filename and convert to multi-byte (ick!)
    //
    pwz = FileFromPath(sczCabinet);
    ExitOnNull1(pwz, hr, E_INVALIDARG, "failed to process cabinet path: %ls", wzCabinet);

    if (!::WideCharToMultiByte(CP_UTF8, 0, pwz, -1, szCabFile, countof(szCabFile), NULL, NULL))
    {
        ExitWithLastError1(hr, "failed to convert cabinet filename to ASCII: %ls", pwz);
    }

    *pwz = '\0';

    // If a full path was not provided, use the relative current directory.
    if (wzCabinet == pwz)
    {
        hr = ::StringCchCopyA(szCabDirectory, countof(szCabDirectory), ".\\");
        ExitOnFailure(hr, "Failed to copy relative current directory as cabinet directory.");
    }
    else
    {
        if (!::WideCharToMultiByte(CP_UTF8, 0, sczCabinet, -1, szCabDirectory, countof(szCabDirectory), NULL, NULL))
        {
            ExitWithLastError1(hr, "failed to convert cabinet directory to ASCII: %ls", sczCabinet);
        }
    }

    //
    // iterate through files in cabinet extracting them to the callback function
    //
    ccs.fStopExtracting = FALSE;
    ccs.pwzExtract = wzExtractFile;
    ccs.pwzExtractDir = wzExtractDir;
    ccs.pfnProgress = pfnProgress;
    ccs.pvContext = pvContext;

    vdw64EmbeddedOffset = dw64EmbeddedOffset;

    // if pfnNotify is given, use it, otherwise use default callback
    if (NULL == pfnNotify)
    {
        pfnFdiNotify = CabExtractCallback; 
    }
    else
    {
        v_pfnNetFx11Notify = pfnNotify;
        pfnFdiNotify = FDINotify;
    }
    fResult = vpfnFDICopy(vhfdi, szCabFile, szCabDirectory, 0, pfnFdiNotify, NULL, static_cast<void*>(&ccs));
    if (!fResult && !ccs.fStopExtracting)   // if something went wrong and it wasn't us just stopping the extraction, then return a failure
    {
        ExitWithLastError1(hr, "failed to extract cabinet file: %ls", sczCabinet);
    }

LExit:
    ReleaseStr(sczCabinet);
    v_pfnNetFx11Notify = NULL;

    return hr;
}

/****************************************************************************
 default extract routines

****************************************************************************/
static __callback LPVOID DIAMONDAPI CabExtractAlloc(__in DWORD dwSize)
{
    return MemAlloc(dwSize, FALSE);
}


static __callback void DIAMONDAPI CabExtractFree(__in LPVOID pvData)
{
    MemFree(pvData);
}


static __callback INT_PTR FAR DIAMONDAPI CabExtractOpen(__in_z PSTR pszFile, __in int oflag, __in int pmode)
{
    HRESULT hr = S_OK;
    INT_PTR pFile = -1;
    LPWSTR sczCabFile = NULL;

    // if FDI asks for some unusual mode (in low memory situation it could ask for a scratch file) fail
    if ((oflag != (/*_O_BINARY*/ 0x8000 | /*_O_RDONLY*/ 0x0000)) || (pmode != (_S_IREAD | _S_IWRITE)))
    {
        hr = E_OUTOFMEMORY;
        ExitOnFailure(hr, "FDI asked for a scratch file to be created, which is unsupported");
    }

    hr = StrAllocStringAnsi(&sczCabFile, pszFile, 0, CP_UTF8);
    ExitOnFailure(hr, "Failed to convert UTF8 cab file name to wide character string");

    pFile = reinterpret_cast<INT_PTR>(::CreateFileW(sczCabFile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL));
    if (INVALID_HANDLE_VALUE == reinterpret_cast<HANDLE>(pFile))
    {
        ExitWithLastError1(hr, "failed to open file: %ls", sczCabFile);
    }

    if (vdw64EmbeddedOffset)
    {
        hr = CabExtractSeek(pFile, 0, 0);
        ExitOnFailure1(hr, "Failed to seek to embedded offset %I64d", vdw64EmbeddedOffset);
    }

LExit:
    ReleaseStr(sczCabFile);

    return FAILED(hr) ? -1 : pFile;
}


static __callback UINT FAR DIAMONDAPI CabExtractRead(__in INT_PTR hf, __out void FAR *pv, __in UINT cb)
{
    HRESULT hr = S_OK;
    DWORD cbRead = 0;

    ExitOnNull(hf, hr, E_INVALIDARG, "Failed to read file during cabinet extraction - no file given to read");
    if (!::ReadFile(reinterpret_cast<HANDLE>(hf), pv, cb, &cbRead, NULL))
    {
        ExitWithLastError(hr, "failed to read during cabinet extraction");
    }

LExit:
    return FAILED(hr) ? -1 : cbRead;
}


static __callback UINT FAR DIAMONDAPI CabExtractWrite(__in INT_PTR hf, __in void FAR *pv, __in UINT cb)
{
    HRESULT hr = S_OK;
    DWORD cbWrite = 0;

    ExitOnNull(hf, hr, E_INVALIDARG, "Failed to write file during cabinet extraction - no file given to write");
    if (!::WriteFile(reinterpret_cast<HANDLE>(hf), pv, cb, &cbWrite, NULL))
    {
        ExitWithLastError(hr, "failed to write during cabinet extraction");
    }

LExit:
    return FAILED(hr) ? -1 : cbWrite;
}


static __callback long FAR DIAMONDAPI CabExtractSeek(__in INT_PTR hf, __in long dist, __in int seektype)
{
    HRESULT hr = S_OK;
    DWORD dwMoveMethod;
    LONG lMove = 0;

    switch (seektype)
    {
    case 0:   // SEEK_SET
        dwMoveMethod = FILE_BEGIN;
        dist += static_cast<long>(vdw64EmbeddedOffset);
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
        ExitOnFailure1(hr, "unexpected seektype in FDISeek(): %d", seektype);
    }

    // SetFilePointer returns -1 if it fails (this will cause FDI to quit with an FDIERROR_USER_ABORT error. 
    // (Unless this happens while working on a cabinet, in which case FDI returns FDIERROR_CORRUPT_CABINET)
    lMove = ::SetFilePointer(reinterpret_cast<HANDLE>(hf), dist, NULL, dwMoveMethod);
    if (0xFFFFFFFF == lMove)
    {
        ExitWithLastError1(hr, "failed to move file pointer %d bytes", dist);
    }

LExit:
    return FAILED(hr) ? -1 : lMove - static_cast<long>(vdw64EmbeddedOffset);
}


static __callback int FAR DIAMONDAPI CabExtractClose(__in INT_PTR hf)
{
    HRESULT hr = S_OK;

    if (!::CloseHandle(reinterpret_cast<HANDLE>(hf)))
    {
        ExitWithLastError(hr, "failed to close file during cabinet extraction");
    }

LExit:
    return FAILED(hr) ? -1 : 0;
}


static __callback INT_PTR DIAMONDAPI CabExtractCallback(__in FDINOTIFICATIONTYPE iNotification, __inout FDINOTIFICATION *pFDINotify)
{
    Assert(pFDINotify->pv);

    HRESULT hr = S_OK;
    INT_PTR ipResult = 0;   // result to return on success

    CAB_CALLBACK_STRUCT* pccs = static_cast<CAB_CALLBACK_STRUCT*>(pFDINotify->pv);
    LPCSTR sz;
    WCHAR wz[MAX_PATH];
    FILETIME ft;

    switch (iNotification)
    {
    case fdintCOPY_FILE:  // begin extracting a resource from cabinet
        ExitOnNull(pFDINotify->psz1, hr, E_INVALIDARG, "No cabinet file ID given to convert");
        ExitOnNull(pccs, hr, E_INVALIDARG, "Failed to call cabextract callback, because no callback struct was provided");

        if (pccs->fStopExtracting)
        {
            ExitFunction1(hr = S_FALSE);   // no more extracting
        }

        // convert params to useful variables
        sz = static_cast<LPCSTR>(pFDINotify->psz1);
        if (!::MultiByteToWideChar(CP_ACP, 0, sz, -1, wz, countof(wz)))
        {
            ExitWithLastError1(hr, "failed to convert cabinet file id to unicode: %s", sz);
        }

        if (pccs->pfnProgress)
        {
            hr = pccs->pfnProgress(TRUE, wz, pccs->pvContext);
            if (S_OK != hr)
            {
                ExitFunction();
            }
        }

        if (L'*' == *pccs->pwzExtract || 0 == lstrcmpW(pccs->pwzExtract, wz))
        {
            // get the created date for the resource in the cabinet
            FILETIME ftLocal;
            if (!::DosDateTimeToFileTime(pFDINotify->date, pFDINotify->time, &ftLocal))
            {
                ExitWithLastError1(hr, "failed to get time for resource: %ls", wz);
            }
            ::LocalFileTimeToFileTime(&ftLocal, &ft);
            

            WCHAR wzPath[MAX_PATH];
            hr = ::StringCchCopyW(wzPath, countof(wzPath), pccs->pwzExtractDir);
            ExitOnFailure2(hr, "failed to copy in extract directory: %ls for file: %ls", pccs->pwzExtractDir, wz);
            hr = ::StringCchCatW(wzPath, countof(wzPath), wz);
            ExitOnFailure2(hr, "failed to concat onto path: %ls file: %ls", wzPath, wz);

            ipResult = reinterpret_cast<INT_PTR>(::CreateFileW(wzPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
            if (INVALID_HANDLE_VALUE == reinterpret_cast<HANDLE>(ipResult))
            {
                ExitWithLastError1(hr, "failed to create file: %s", wzPath);
            }

            ::SetFileTime(reinterpret_cast<HANDLE>(ipResult), &ft, &ft, &ft);   // try to set the file time (who cares if it fails)

            if (::SetFilePointer(reinterpret_cast<HANDLE>(ipResult), pFDINotify->cb, NULL, FILE_BEGIN))   // try to set the end of the file (don't worry if this fails)
            {
                if (::SetEndOfFile(reinterpret_cast<HANDLE>(ipResult)))
                {
                    ::SetFilePointer(reinterpret_cast<HANDLE>(ipResult), 0, NULL, FILE_BEGIN);  // reset the file pointer
                }
            }
        }
        else  // resource wasn't requested, skip it
        {
            hr = S_OK;
            ipResult = 0;
        }

        break;
    case fdintCLOSE_FILE_INFO:  // resource extraction complete
        Assert(pFDINotify->hf && pFDINotify->psz1);
        ExitOnNull(pccs, hr, E_INVALIDARG, "Failed to call cabextract callback, because no callback struct was provided");

        // convert params to useful variables
        sz = static_cast<LPCSTR>(pFDINotify->psz1);
        ExitOnNull(sz, hr, E_INVALIDARG, "Failed to convert cabinet file id, because no cabinet file id was provided");

        if (!::MultiByteToWideChar(CP_ACP, 0, sz, -1, wz, countof(wz)))
        {
            ExitWithLastError1(hr, "failed to convert cabinet file id to unicode: %s", sz);
        }

        if (NULL != pFDINotify->hf)  // just close the file
        {
            ::CloseHandle(reinterpret_cast<HANDLE>(pFDINotify->hf));
        }

        if (pccs->pfnProgress)
        {
            hr = pccs->pfnProgress(FALSE, wz, pccs->pvContext);
        }

        if (S_OK == hr && L'*' == *pccs->pwzExtract)   // if everything is okay and we're extracting all files, keep going
        {
            ipResult = TRUE;
        }
        else   // something went wrong or we only needed to extract one file
        {
            hr = S_OK;
            ipResult = FALSE;
            pccs->fStopExtracting = TRUE;
        }

        break;
    case fdintPARTIAL_FILE: __fallthrough;   // no action needed for these messages, fall through
    case fdintNEXT_CABINET: __fallthrough;
    case fdintENUMERATE: __fallthrough;
    case fdintCABINET_INFO:
        break;
    default:
        AssertSz(FALSE, "CabExtractCallback() - unknown FDI notification command");
    };

LExit:
    return (S_OK == hr) ? ipResult : -1;
}
