// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT ScaGetHttpHeader7(
    __in int iParentType,
    __in_z LPCWSTR wzParentValue,
    __in SCA_HTTP_HEADER** ppshhList,
    __out SCA_HTTP_HEADER** ppshhOut
    )
{
    HRESULT hr = S_OK;
    SCA_HTTP_HEADER* pshhAdd = NULL;
    SCA_HTTP_HEADER* pshhLast = NULL;

    *ppshhOut = NULL;

    if (!*ppshhList)
    {
        return hr;
    }

    SCA_HTTP_HEADER* pshh = *ppshhList;
    while (pshh)
    {
        if (iParentType == pshh->iParentType && 0 == wcscmp(wzParentValue, pshh->wzParentValue))
        {
            // Found a match, take this one out of the list and add it to the matched out list
            pshhAdd = pshh;

            if (pshhLast)
            {
                // If we're not at the beginning of the list tell the last node about it's new next (since we're taking away it's current next)
                pshhLast->pshhNext = pshhAdd->pshhNext;
            }
            else
            {
                // If we are at the beginning (no pshhLast) update the beginning (since we're taking it)
                *ppshhList = pshh->pshhNext;
            }
            pshh = pshh->pshhNext; // move on

            // Add the one we've removed to the beginning of the out list
            pshhAdd->pshhNext = *ppshhOut;
            *ppshhOut = pshhAdd;
        }
        else
        {
            pshhLast = pshh; // remember the last we that didn't match
            pshh = pshh->pshhNext; // move on
        }
    }

    return hr;
}


HRESULT ScaWriteHttpHeader7(
     __in_z LPCWSTR wzWebName,
     __in_z LPCWSTR wzRoot,
     SCA_HTTP_HEADER* pshhList
    )
{

    HRESULT hr = S_OK;

    hr = ScaWriteConfigID(IIS_HTTP_HEADER_BEGIN);
    ExitOnFailure(hr, "Fail to write httpHeader begin ID");

    hr = ScaWriteConfigString(wzWebName);
    ExitOnFailure(hr, "Fail to write httpHeader Web Key");

    hr = ScaWriteConfigString(wzRoot);
    ExitOnFailure(hr, "Fail to write httpHeader Vdir key");

    // Loop through the HTTP headers
    for (SCA_HTTP_HEADER* pshh = pshhList; pshh; pshh = pshh->pshhNext)
    {
        hr = ScaWriteConfigID(IIS_HTTP_HEADER);
        ExitOnFailure(hr, "Fail to write httpHeader ID");

        hr = ScaWriteConfigString(pshh->wzName);
        ExitOnFailure(hr, "Fail to write httpHeader name");

        hr = ScaWriteConfigString(pshh->wzValue);
        ExitOnFailure(hr, "Fail to write httpHeader value");
    }

    hr = ScaWriteConfigID(IIS_HTTP_HEADER_END);
    ExitOnFailure(hr, "Fail to write httpHeader end ID");

LExit:
    return hr;
}


HRESULT ScaHttpHeaderCheckList7(
    __in SCA_HTTP_HEADER* pshhList
    )
{
    if (!pshhList)
    {
        return S_OK;
    }

    while (pshhList)
    {
        WcaLog(LOGMSG_STANDARD, "Http Header: %ls for parent: %ls not used!", pshhList->wzName, pshhList->wzParentValue);
        pshhList = pshhList->pshhNext;
    }

    return E_FAIL;
}


//static HRESULT AddHttpHeaderToList(
//    __in SCA_HTTP_HEADER** ppshhList
//    )
//{
//    HRESULT hr = S_OK;
//
//    SCA_HTTP_HEADER* pshh = static_cast<SCA_HTTP_HEADER*>(MemAlloc(sizeof(SCA_HTTP_HEADER), TRUE));
//    ExitOnNull(pshh, hr, E_OUTOFMEMORY, "failed to allocate memory for new http header list element");
//
//    pshh->pshhNext = *ppshhList;
//    *ppshhList = pshh;
//
//LExit:
//    return hr;
//}
