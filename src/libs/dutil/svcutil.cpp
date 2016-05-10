// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
