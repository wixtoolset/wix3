#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="memutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Memory helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


#if DEBUG
static BOOL vfMemInitialized = FALSE;
#endif

extern "C" HRESULT DAPI MemInitialize()
{
#if DEBUG
    vfMemInitialized = TRUE;
#endif
    return S_OK;
}

extern "C" void DAPI MemUninitialize()
{
#if DEBUG
    vfMemInitialized = FALSE;
#endif
}

extern "C" LPVOID DAPI MemAlloc(
    __in SIZE_T cbSize,
    __in BOOL fZero
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    AssertSz(cbSize > 0, "MemAlloc() called with invalid size");
    return ::HeapAlloc(::GetProcessHeap(), fZero ? HEAP_ZERO_MEMORY : 0, cbSize);
}


extern "C" LPVOID DAPI MemReAlloc(
    __in LPVOID pv,
    __in SIZE_T cbSize,
    __in BOOL fZero
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    AssertSz(cbSize > 0, "MemReAlloc() called with invalid size");
    return ::HeapReAlloc(::GetProcessHeap(), fZero ? HEAP_ZERO_MEMORY : 0, pv, cbSize);
}


extern "C" HRESULT DAPI MemEnsureArraySize(
    __deref_out_bcount(cArray * cbArrayType) LPVOID* ppvArray,
    __in DWORD cArray,
    __in SIZE_T cbArrayType,
    __in DWORD dwGrowthCount
    )
{
    HRESULT hr = S_OK;
    DWORD cNew = 0;
    LPVOID pvNew = NULL;
    SIZE_T cbNew = 0;

    hr = ::DWordAdd(cArray, dwGrowthCount, &cNew);
    ExitOnFailure(hr, "Integer overflow when calculating new element count.");

    hr = ::SIZETMult(cNew, cbArrayType, &cbNew);
    ExitOnFailure(hr, "Integer overflow when calculating new block size.");

    if (*ppvArray)
    {
        SIZE_T cbUsed = cArray * cbArrayType;
        SIZE_T cbCurrent = MemSize(*ppvArray);
        if (cbCurrent < cbUsed)
        {
            pvNew = MemReAlloc(*ppvArray, cbNew, TRUE);
            ExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to allocate array larger.");

            *ppvArray = pvNew;
        }
    }
    else
    {
        pvNew = MemAlloc(cbNew, TRUE);
        ExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to allocate new array.");

        *ppvArray = pvNew;
    }

LExit:
    return hr;
}


HRESULT DAPI MemInsertIntoArray(
    __deref_out_bcount((cExistingArray + cNumInsertItems) * cbArrayType) LPVOID* ppvArray,
    __in DWORD dwInsertIndex,
    __in DWORD cNumInsertItems,
    __in DWORD cExistingArray,
    __in SIZE_T cbArrayType,
    __in DWORD dwGrowthCount
    )
{
    HRESULT hr = S_OK;
    DWORD i;
    BYTE *pbArray = NULL;

    if (0 == cNumInsertItems)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = MemEnsureArraySize(ppvArray, cExistingArray + cNumInsertItems, cbArrayType, dwGrowthCount);
    ExitOnFailure(hr, "Failed to resize array while inserting items");

    pbArray = reinterpret_cast<BYTE *>(*ppvArray);
    for (i = cExistingArray + cNumInsertItems - 1; i > dwInsertIndex; --i)
    {
        memcpy_s(pbArray + i * cbArrayType, cbArrayType, pbArray + (i - 1) * cbArrayType, cbArrayType);
    }

    // Zero out the newly-inserted items
    memset(pbArray + dwInsertIndex * cbArrayType, 0, cNumInsertItems * cbArrayType);

LExit:
    return hr;
}

extern "C" HRESULT DAPI MemFree(
    __in LPVOID pv
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    return ::HeapFree(::GetProcessHeap(), 0, pv) ? S_OK : HRESULT_FROM_WIN32(::GetLastError());
}


extern "C" SIZE_T DAPI MemSize(
    __in LPCVOID pv
    )
{
//    AssertSz(vfMemInitialized, "MemInitialize() not called, this would normally crash");
    return ::HeapSize(::GetProcessHeap(), 0, pv);
}
