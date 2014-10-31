//-------------------------------------------------------------------------------------------------
// <copyright file="container.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// internal function declarations

static HRESULT GetAttachedContainerInfo(
    __in HANDLE hFile,
    __in DWORD iContainerIndex,
    __out DWORD* pdwFormat,
    __out DWORD64* pqwOffset,
    __out DWORD64* pqwSize
    );


// function definitions

extern "C" HRESULT ContainersParseFromXml(
    __in BURN_SECTION* pSection,
    __in BURN_CONTAINERS* pContainers,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select container nodes
    hr = XmlSelectNodes(pixnBundle, L"Container", &pixnNodes);
    ExitOnFailure(hr, "Failed to select container nodes.");

    // get container node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get container node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for searches
    pContainers->rgContainers = (BURN_CONTAINER*)MemAlloc(sizeof(BURN_CONTAINER) * cNodes, TRUE);
    ExitOnNull(pContainers->rgContainers, hr, E_OUTOFMEMORY, "Failed to allocate memory for container structs.");

    pContainers->cContainers = cNodes;

    // parse search elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_CONTAINER* pContainer = &pContainers->rgContainers[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // TODO: Read type from manifest. Today only CABINET is supported.
        pContainer->type = BURN_CONTAINER_TYPE_CABINET;

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pContainer->sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @Primary
        hr = XmlGetYesNoAttribute(pixnNode, L"Primary", &pContainer->fPrimary);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Primary.");
        }

        // @Attached
        hr = XmlGetYesNoAttribute(pixnNode, L"Attached", &pContainer->fAttached);
        if (E_NOTFOUND != hr || pContainer->fPrimary) // if it is a primary container, it has to be attached
        {
            ExitOnFailure(hr, "Failed to get @Attached.");
        }

        // @AttachedIndex
        hr = XmlGetAttributeNumber(pixnNode, L"AttachedIndex", &pContainer->dwAttachedIndex);
        if (E_NOTFOUND != hr || pContainer->fAttached) // if it is an attached container it must have an index
        {
            ExitOnFailure(hr, "Failed to get @AttachedIndex.");
        }

        // @FilePath
        hr = XmlGetAttributeEx(pixnNode, L"FilePath", &pContainer->sczFilePath);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @FilePath.");
        }

        // The source path starts as the file path.
        hr = StrAllocString(&pContainer->sczSourcePath, pContainer->sczFilePath, 0);
        ExitOnFailure(hr, "Failed to copy @FilePath");

        // @DownloadUrl
        hr = XmlGetAttributeEx(pixnNode, L"DownloadUrl", &pContainer->downloadSource.sczUrl);
        if (E_NOTFOUND != hr || (!pContainer->fPrimary && !pContainer->sczSourcePath)) // if the package is not a primary package, it must have a source path or a download url
        {
            ExitOnFailure(hr, "Failed to get @DownloadUrl. Either @SourcePath or @DownloadUrl needs to be provided.");
        }

        // @Hash
        hr = XmlGetAttributeEx(pixnNode, L"Hash", &pContainer->sczHash);
        if (SUCCEEDED(hr))
        {
            hr = StrAllocHexDecode(pContainer->sczHash, &pContainer->pbHash, &pContainer->cbHash);
            ExitOnFailure(hr, "Failed to hex decode the Container/@Hash.");
        }
        else if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Hash.");
        }

        // If the container is attached, make sure the information in the section matches what the
        // manifest contained and get the offset to the container.
        if (pContainer->fAttached)
        {
            hr = SectionGetAttachedContainerInfo(pSection, pContainer->dwAttachedIndex, pContainer->type, &pContainer->qwAttachedOffset, &pContainer->qwFileSize, &pContainer->fActuallyAttached);
            ExitOnFailure(hr, "Failed to get attached container information.");
        }

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" void ContainersUninitialize(
    __in BURN_CONTAINERS* pContainers
    )
{
    if (pContainers->rgContainers)
    {
        for (DWORD i = 0; i < pContainers->cContainers; ++i)
        {
            BURN_CONTAINER* pContainer = &pContainers->rgContainers[i];

            ReleaseStr(pContainer->sczId);
            ReleaseStr(pContainer->sczHash);
            ReleaseStr(pContainer->sczSourcePath);
            ReleaseStr(pContainer->sczFilePath);
            ReleaseMem(pContainer->pbHash);
            ReleaseStr(pContainer->downloadSource.sczUrl);
            ReleaseStr(pContainer->downloadSource.sczUser);
            ReleaseStr(pContainer->downloadSource.sczPassword);
        }
        MemFree(pContainers->rgContainers);
    }

    // clear struct
    memset(pContainers, 0, sizeof(BURN_CONTAINERS));
}

extern "C" HRESULT ContainerOpenUX(
    __in BURN_SECTION* pSection,
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER container = { };
    LPWSTR sczExecutablePath = NULL;

    // open attached container
    container.type = BURN_CONTAINER_TYPE_CABINET;
    container.fPrimary = TRUE;
    container.fAttached = TRUE;
    container.dwAttachedIndex = 0;

    hr = SectionGetAttachedContainerInfo(pSection, container.dwAttachedIndex, container.type, &container.qwAttachedOffset, &container.qwFileSize, &container.fActuallyAttached);
    ExitOnFailure(hr, "Failed to get container information for UX container.");

    AssertSz(container.fActuallyAttached, "The BA container must always be found attached.");

    hr = PathForCurrentProcess(&sczExecutablePath, NULL);
    ExitOnFailure(hr, "Failed to get path for executing module.");

    hr = ContainerOpen(pContext, &container, pSection->hEngineFile, sczExecutablePath);
    ExitOnFailure(hr, "Failed to open attached container.");

LExit:
    ReleaseStr(sczExecutablePath);

    return hr;
}

extern "C" HRESULT ContainerOpen(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer,
    __in HANDLE hContainerFile,
    __in_z LPCWSTR wzFilePath
    )
{
    HRESULT hr = S_OK;
    LARGE_INTEGER li = { };

    // initialize context
    pContext->type = pContainer->type;
    pContext->qwSize = pContainer->qwFileSize;
    pContext->qwOffset = pContainer->qwAttachedOffset;

    // If the handle to the container is not open already, open container file
    if (INVALID_HANDLE_VALUE == hContainerFile)
    {
        pContext->hFile = ::CreateFileW(wzFilePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        ExitOnInvalidHandleWithLastError1(pContext->hFile, hr, "Failed to open file: %ls", wzFilePath);
    }
    else // use the container file handle.
    {
        if (!::DuplicateHandle(::GetCurrentProcess(), hContainerFile, ::GetCurrentProcess(), &pContext->hFile, 0, FALSE, DUPLICATE_SAME_ACCESS))
        {
            ExitWithLastError1(hr, "Failed to duplicate handle to container: %ls", wzFilePath);
        }
    }

    // If it is a container attached to an executable, seek to the container offset.
    if (pContainer->fAttached)
    {
        li.QuadPart = (LONGLONG)pContext->qwOffset;
    }

    if (!::SetFilePointerEx(pContext->hFile, li, NULL, FILE_BEGIN))
    {
        ExitWithLastError(hr, "Failed to move file pointer to container offset.");
    }

    // open the archive
    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractOpen(pContext, wzFilePath);
        break;
    }
    ExitOnFailure(hr, "Failed to open container.");

LExit:
    return hr;
}

extern "C" HRESULT ContainerNextStream(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout_z LPWSTR* psczStreamName
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractNextStream(pContext, psczStreamName);
        break;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerStreamToFile(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractStreamToFile(pContext, wzFileName);
        break;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerStreamToBuffer(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __out BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractStreamToBuffer(pContext, ppbBuffer, pcbBuffer);
        break;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerSkipStream(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractSkipStream(pContext);
        break;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerClose(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    // close container
    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractClose(pContext);
        ExitOnFailure(hr, "Failed to close cabinet.");
        break;
    }

LExit:
    ReleaseFile(pContext->hFile);

    if (SUCCEEDED(hr))
    {
        memset(pContext, 0, sizeof(BURN_CONTAINER_CONTEXT));
    }

    return hr;
}

extern "C" HRESULT ContainerFindById(
    __in BURN_CONTAINERS* pContainers,
    __in_z LPCWSTR wzId,
    __out BURN_CONTAINER** ppContainer
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER* pContainer = NULL;

    for (DWORD i = 0; i < pContainers->cContainers; ++i)
    {
        pContainer = &pContainers->rgContainers[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pContainer->sczId, -1, wzId, -1))
        {
            *ppContainer = pContainer;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
