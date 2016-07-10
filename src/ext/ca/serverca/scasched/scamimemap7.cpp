// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
HRESULT ScaWriteMimeMap7(
    __in_z LPCWSTR wzWebName,
    __in_z LPCWSTR wzRootOfWeb,
    SCA_MIMEMAP* psmmList
    )
{
    HRESULT hr = S_OK;
    SCA_MIMEMAP* psmm;

    //create the mimemap list for this vdir application
    //all go to same web/root location tag
    hr = ScaWriteConfigID(IIS_MIMEMAP_BEGIN);
    ExitOnFailure(hr, "Failed to write mimemap begin id");
    hr = ScaWriteConfigString(wzWebName);                //site name key
    ExitOnFailure(hr, "Failed to write mimemap web key");
    hr = ScaWriteConfigString(wzRootOfWeb);               //app path key
    ExitOnFailure(hr, "Failed to write mimemap app key");

    psmm = psmmList;

    while (psmm)
    {
        //create the Extension for this vdir application
        hr = ScaWriteConfigID(IIS_MIMEMAP);
        ExitOnFailure(hr, "Failed to write mimemap id");

        if (*psmm->wzExtension)
        {
            hr = ScaWriteConfigString(psmm->wzExtension);
        }
        else   // blank means "*" (all)
        {
            hr = ScaWriteConfigString(L"*");
        }
        ExitOnFailure(hr, "Failed to write mimemap extension");

        hr = ScaWriteConfigString(psmm->wzMimeType);
        ExitOnFailure(hr, "Failed to write mimemap type");

        psmm = psmm->psmmNext;
    }

    hr = ScaWriteConfigID(IIS_MIMEMAP_END);
    ExitOnFailure(hr, "Failed to write mimemap end id");

LExit:
    return hr;
}


//static HRESULT AddMimeMapToList(SCA_MIMEMAP** ppsmmList)
//{
//    HRESULT hr = S_OK;
//
//    SCA_MIMEMAP* psmm = static_cast<SCA_MIMEMAP*>(MemAlloc(sizeof(SCA_MIMEMAP), TRUE));
//    ExitOnNull(psmm, hr, E_OUTOFMEMORY, "failed to allocate memory for new mime map list element");
//
//    psmm->psmmNext = *ppsmmList;
//    *ppsmmList = psmm;
//
//LExit:
//    return hr;
//}
