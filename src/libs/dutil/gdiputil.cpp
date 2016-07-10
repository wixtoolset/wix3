// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace Gdiplus;

/********************************************************************
 GdipBitmapFromResource - read a GDI+ image out of a resource stream

********************************************************************/
extern "C" HRESULT DAPI GdipBitmapFromResource(
    __in_opt HINSTANCE hinst,
    __in_z LPCSTR szId,
    __out Bitmap **ppBitmap
    )
{
    HRESULT hr = S_OK;
    LPVOID pvData = NULL;
    DWORD cbData = 0;
    HGLOBAL hGlobal = NULL;;
    LPVOID pv = NULL;
    IStream *pStream = NULL;
    Bitmap *pBitmap = NULL;
    Status gs = Ok;

    hr = ResReadData(hinst, szId, &pvData, &cbData);
    ExitOnFailure(hr, "Failed to load GDI+ bitmap from resource.");

    // Have to copy the fixed resource data into moveable (heap) memory
    // since that's what GDI+ expects.
    hGlobal = ::GlobalAlloc(GMEM_MOVEABLE, cbData);
    ExitOnNullWithLastError(hGlobal, hr, "Failed to allocate global memory.");

    pv = ::GlobalLock(hGlobal);
    ExitOnNullWithLastError(pv, hr, "Failed to lock global memory.");

    memcpy(pv, pvData, cbData);

    ::GlobalUnlock(pv); // no point taking any more memory than we have already
    pv = NULL;

    hr = ::CreateStreamOnHGlobal(hGlobal, TRUE, &pStream);
    ExitOnFailure(hr, "Failed to allocate stream from global memory.");

    hGlobal = NULL; // we gave the global memory to the stream object so it will close it

    pBitmap = Bitmap::FromStream(pStream);
    ExitOnNull(pBitmap, hr, E_OUTOFMEMORY, "Failed to allocate bitmap from stream.");

    gs = pBitmap->GetLastStatus();
    ExitOnGdipFailure(gs, hr, "Failed to load bitmap from stream.");

    *ppBitmap = pBitmap;
    pBitmap = NULL;

LExit:
    if (pBitmap)
    {
        delete pBitmap;
    }

    ReleaseObject(pStream);

    if (pv)
    {
        ::GlobalUnlock(pv);
    }

    if (hGlobal)
    {
        ::GlobalFree(hGlobal);
    }

    return hr;
}


/********************************************************************
 GdipBitmapFromFile - read a GDI+ image from a file.

********************************************************************/
extern "C" HRESULT DAPI GdipBitmapFromFile(
    __in_z LPCWSTR wzFileName,
    __out Bitmap **ppBitmap
    )
{
    HRESULT hr = S_OK;
    Bitmap *pBitmap = NULL;
    Status gs = Ok;

    ExitOnNull(ppBitmap, hr, E_INVALIDARG, "Invalid null wzFileName");

    pBitmap = Bitmap::FromFile(wzFileName);
    ExitOnNull(pBitmap, hr, E_OUTOFMEMORY, "Failed to allocate bitmap from file.");

    gs = pBitmap->GetLastStatus();
    ExitOnGdipFailure1(gs, hr, "Failed to load bitmap from file: %ls", wzFileName);

    *ppBitmap = pBitmap;
    pBitmap = NULL;

LExit:
    if (pBitmap)
    {
        delete pBitmap;
    }

    return hr;
}


HRESULT DAPI GdipHresultFromStatus(
    __in Gdiplus::Status gs
    )
{
    switch (gs)
    {
    case Ok:
        return S_OK;

    case GenericError:
        return E_FAIL;

    case InvalidParameter:
        return E_INVALIDARG;

    case OutOfMemory:
        return E_OUTOFMEMORY;

    case ObjectBusy:
        return HRESULT_FROM_WIN32(ERROR_BUSY);

    case InsufficientBuffer:
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);

    case NotImplemented:
        return E_NOTIMPL;

    case Win32Error:
        return E_FAIL;

    case WrongState:
        return E_FAIL;

    case Aborted:
        return E_ABORT;

    case FileNotFound:
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);

    case ValueOverflow:
        return HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);

    case AccessDenied:
        return E_ACCESSDENIED;

    case UnknownImageFormat:
        return HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);

    case FontFamilyNotFound: __fallthrough;
    case FontStyleNotFound: __fallthrough;
    case NotTrueTypeFont:
        return E_UNEXPECTED;

    case UnsupportedGdiplusVersion:
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    case GdiplusNotInitialized:
        return E_UNEXPECTED;

    case PropertyNotFound: __fallthrough;
    case PropertyNotSupported:
        return E_FAIL;
    }

    return E_UNEXPECTED;
}
