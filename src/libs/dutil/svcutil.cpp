//-------------------------------------------------------------------------------------------------
// <copyright file="svcutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Windows service helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/********************************************************************
SvcQueryConfig - queries the configuration of a service

********************************************************************/
extern "C" HRESULT DAPI SvcQueryConfig(
    __in SC_HANDLE sch,
    __out QUERY_SERVICE_CONFIGW** ppConfig
    )
{
    HRESULT hr = S_OK;
    QUERY_SERVICE_CONFIGW* pConfig = NULL;
    DWORD cbConfig = 0;

    if (!::QueryServiceConfigW(sch, NULL, 0, &cbConfig))
    {
        DWORD er = ::GetLastError();
        if (ERROR_INSUFFICIENT_BUFFER == er)
        {
            pConfig = static_cast<QUERY_SERVICE_CONFIGW*>(MemAlloc(cbConfig, TRUE));
            ExitOnNull(pConfig, hr, E_OUTOFMEMORY, "Failed to allocate memory to get configuration.");

            if (!::QueryServiceConfigW(sch, pConfig, cbConfig, &cbConfig))
            {
                ExitWithLastError(hr, "Failed to read service configuration.");
            }
        }
        else
        {
            ExitOnWin32Error(er, hr, "Failed to query service configuration.");
        }
    }

    *ppConfig = pConfig;
    pConfig = NULL;

LExit:
    ReleaseMem(pConfig);

    return hr;
}
