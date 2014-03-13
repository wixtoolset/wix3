//-------------------------------------------------------------------------------------------------
// <copyright file="proc2utl.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Process helper functions that require PSAPI.DLL.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/********************************************************************
 ProcFindAllIdsFromExeName() - returns an array of process ids that are running specified executable.

*******************************************************************/
extern "C" HRESULT DAPI ProcFindAllIdsFromExeName(
    __in_z LPCWSTR wzExeName,
    __out DWORD** ppdwProcessIds,
    __out DWORD* pcProcessIds
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HANDLE hSnap = INVALID_HANDLE_VALUE;
    BOOL fContinue = FALSE;
    PROCESSENTRY32W peData = { sizeof(peData) };
    
    hSnap = ::CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (INVALID_HANDLE_VALUE == hSnap)
    {
        ExitWithLastError(hr, "Failed to create snapshot of processes on system");
    }

    fContinue = ::Process32FirstW(hSnap, &peData);

    while (fContinue)
    {
        if (0 == lstrcmpiW((LPCWSTR)&(peData.szExeFile), wzExeName))
        {
            if (!*ppdwProcessIds)
            {
                *ppdwProcessIds = static_cast<DWORD*>(MemAlloc(sizeof(DWORD), TRUE));
                ExitOnNull(ppdwProcessIds, hr, E_OUTOFMEMORY, "Failed to allocate array for returned process IDs.");
            }
            else
            {
                DWORD* pdwReAllocReturnedPids = NULL;
                pdwReAllocReturnedPids = static_cast<DWORD*>(MemReAlloc(*ppdwProcessIds, sizeof(DWORD) * ((*pcProcessIds) + 1), TRUE));
                ExitOnNull(pdwReAllocReturnedPids, hr, E_OUTOFMEMORY, "Failed to re-allocate array for returned process IDs.");

                *ppdwProcessIds = pdwReAllocReturnedPids;
            }
            
            (*ppdwProcessIds)[*pcProcessIds] = peData.th32ProcessID;
            ++(*pcProcessIds);
        }

        fContinue = ::Process32NextW(hSnap, &peData);
    }

    er = ::GetLastError();
    if (ERROR_NO_MORE_FILES == er)
    {
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    ReleaseFile(hSnap);

    return hr;
}
