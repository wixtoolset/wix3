//-------------------------------------------------------------------------------------------------
// <copyright file="approvedexe.cpp" company="Outercurve Foundation">
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


// function definitions

extern "C" HRESULT ApprovedExesParseFromXml(
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select approved exe nodes
    hr = XmlSelectNodes(pixnBundle, L"ApprovedExeForElevation", &pixnNodes);
    ExitOnFailure(hr, "Failed to select approved exe nodes.");

    // get approved exe node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get approved exe node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for approved exes
    pApprovedExes->rgApprovedExes = (BURN_APPROVED_EXE*)MemAlloc(sizeof(BURN_APPROVED_EXE) * cNodes, TRUE);
    ExitOnNull(pApprovedExes->rgApprovedExes, hr, E_OUTOFMEMORY, "Failed to allocate memory for approved exe structs.");

    pApprovedExes->cApprovedExes = cNodes;

    // parse approved exe elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_APPROVED_EXE* pApprovedExe = &pApprovedExes->rgApprovedExes[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pApprovedExe->sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @FileSize
        hr = XmlGetAttributeEx(pixnNode, L"FileSize", &scz);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @FileSize.");

            hr = StrStringToUInt64(scz, 0, &pApprovedExe->qwFileSize);
            ExitOnFailure(hr, "Failed to parse @FileSize.");
        }

        // @Hash
        hr = XmlGetAttributeEx(pixnNode, L"Hash", &scz);
        ExitOnFailure(hr, "Failed to get @Hash.");

        hr = StrAllocHexDecode(scz, &pApprovedExe->pbHash, &pApprovedExe->cbHash);
        ExitOnFailure(hr, "Failed to hex decode the ApprovedExeForElevation/@Hash.");

        // prepare next iteration
        ReleaseNullObject(pixnNode);
        ReleaseNullStr(scz);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);
    return hr;
}

extern "C" HRESULT ApprovedExesUninitialize(
    __in BURN_APPROVED_EXES* pApprovedExes
    )
{
    if (pApprovedExes->rgApprovedExes)
    {
        for (DWORD i = 0; i < pApprovedExes->cApprovedExes; ++i)
        {
            BURN_APPROVED_EXE* pApprovedExe = &pApprovedExes->rgApprovedExes[i];

            ReleaseStr(pApprovedExe->sczId);
            ReleaseMem(pApprovedExe->pbHash);
        }
        MemFree(pApprovedExes->rgApprovedExes);
    }
    return S_OK;
}

extern "C" HRESULT ApprovedExesUninitializeLaunch(
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    )
{
    if (pLaunchApprovedExe)
    {
        ReleaseStr(pLaunchApprovedExe->sczArguments);
        ReleaseStr(pLaunchApprovedExe->sczExecutablePath);
        ReleaseStr(pLaunchApprovedExe->sczId);
        MemFree(pLaunchApprovedExe);
    }
    return S_OK;
}

extern "C" HRESULT ApprovedExesFindById(
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in_z LPCWSTR wzId,
    __out BURN_APPROVED_EXE** ppApprovedExe
    )
{
    HRESULT hr = S_OK;
    BURN_APPROVED_EXE* pApprovedExe = NULL;

    for (DWORD i = 0; i < pApprovedExes->cApprovedExes; ++i)
    {
        pApprovedExe = &pApprovedExes->rgApprovedExes[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pApprovedExe->sczId, -1, wzId, -1))
        {
            *ppApprovedExe = pApprovedExe;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

extern "C" HRESULT ApprovedExesLaunch(
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe,
    __out DWORD* pdwProcessId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCommand = NULL;
    LPWSTR sczExecutableDirectory = NULL;
    STARTUPINFOW si = {};
    PROCESS_INFORMATION pi = {};

    // build command
    if (pLaunchApprovedExe->sczArguments && *pLaunchApprovedExe->sczArguments)
    {
        hr = StrAllocFormatted(&sczCommand, L"\"%ls\" %s", pLaunchApprovedExe->sczExecutablePath, pLaunchApprovedExe->sczArguments);
    }
    else
    {
        hr = StrAllocFormatted(&sczCommand, L"\"%ls\"", pLaunchApprovedExe->sczExecutablePath);
    }
    ExitOnFailure(hr, "Failed to create executable command.");

    // Try to get the directory of the executable so we can set the current directory of the process to help those executables
    // that expect stuff to be relative to them.  Best effort only.
    hr = PathGetDirectory(pLaunchApprovedExe->sczExecutablePath, &sczExecutableDirectory);
    if (FAILED(hr))
    {
        ReleaseNullStr(sczExecutableDirectory);
    }

    si.cb = sizeof(si);
    if (!::CreateProcessW(pLaunchApprovedExe->sczExecutablePath, sczCommand, NULL, NULL, FALSE, CREATE_NEW_PROCESS_GROUP, NULL, sczExecutableDirectory, &si, &pi))
    {
        ExitWithLastError1(hr, "Failed to CreateProcess on path: %ls", pLaunchApprovedExe->sczExecutablePath);
    }

    *pdwProcessId = pi.dwProcessId;

    if (pLaunchApprovedExe->dwWaitForInputIdleTimeout)
    {
        ::WaitForInputIdle(pi.hProcess, pLaunchApprovedExe->dwWaitForInputIdleTimeout);
    }

LExit:
    ReleaseStr(sczCommand);
    ReleaseStr(sczExecutableDirectory);

    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);

    return hr;
}

extern "C" HRESULT ApprovedExesVerifySecureLocation(
    __in BURN_VARIABLES* pVariables,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczProgramFilesFolder = NULL;
    LPWSTR sczProgramFiles64Folder = NULL;
    LPWSTR sczRootCacheFolder = NULL;

    hr = VariableGetString(pVariables, L"ProgramFiles64Folder", &sczProgramFiles64Folder);
    if (SUCCEEDED(hr))
    {
        hr = PathDirectoryContainsPath(sczProgramFiles64Folder, pLaunchApprovedExe->sczExecutablePath);
        if (S_OK == hr)
        {
            ExitFunction();
        }
    }
    else if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get the ProgramFiles64 folder.");
    }

    hr = VariableGetString(pVariables, L"ProgramFilesFolder", &sczProgramFilesFolder);
    if (SUCCEEDED(hr))
    {
        hr = PathDirectoryContainsPath(sczProgramFilesFolder, pLaunchApprovedExe->sczExecutablePath);
        if (S_OK == hr)
        {
            ExitFunction();
        }
    }
    else if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get the ProgramFiles folder.");
    }

    hr = CacheGetRootCompletedPath(TRUE, TRUE, &sczRootCacheFolder);
    if (SUCCEEDED(hr))
    {
        hr = PathDirectoryContainsPath(sczRootCacheFolder, pLaunchApprovedExe->sczExecutablePath);
        if (S_OK == hr)
        {
            ExitFunction();
        }
    }
    else if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get the PackageCache folder.");
    }

    hr = S_FALSE;

LExit:
    ReleaseStr(sczProgramFilesFolder);
    ReleaseStr(sczProgramFiles64Folder);
    ReleaseStr(sczRootCacheFolder);

    return hr;
}

extern "C" HRESULT PathCanonicalizePath(
    __in_z LPCWSTR wzPath,
    __deref_out_z LPWSTR* psczCanonicalized
    )
{
    HRESULT hr = S_OK;
    int cch = MAX_PATH + 1;

    hr = StrAlloc(psczCanonicalized, cch);
    ExitOnFailure(hr, "Failed to allocate string for the canonicalized path.");

    if (::PathCanonicalizeW(*psczCanonicalized, wzPath))
    {
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
    }

LExit:
    return hr;
}

extern "C" HRESULT PathDirectoryContainsPath(
    __in_z LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    LPWSTR sczDirectory = NULL;
    LPWSTR sczOriginalPath = NULL;
    LPWSTR sczOriginalDirectory = NULL;

    hr = PathCanonicalizePath(wzPath, &sczOriginalPath);
    ExitOnFailure(hr, "Failed to canonicalize the path.");

    hr = PathCanonicalizePath(wzDirectory, &sczOriginalDirectory);
    ExitOnFailure(hr, "Failed to canonicalize the directory.");

    if (!sczOriginalPath || !*sczOriginalPath)
    {
        ExitFunction1(hr = S_FALSE);
    }
    if (!sczOriginalDirectory || !*sczOriginalDirectory)
    {
        ExitFunction1(hr = S_FALSE);
    }

    sczPath = sczOriginalPath;
    sczDirectory = sczOriginalDirectory;

    for (; *sczDirectory;)
    {
        if (!*sczPath)
        {
            ExitFunction1(hr = S_FALSE);
        }

        if (CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczDirectory, 1, sczPath, 1))
        {
            ExitFunction1(hr = S_FALSE);
        }

        ++sczDirectory;
        ++sczPath;
    }

    --sczDirectory;
    if (('\\' == *sczDirectory && *sczPath) || '\\' == *sczPath)
    {
        hr = S_OK;
    }
    else
    {
        hr = S_FALSE;
    }

LExit:
    ReleaseStr(sczOriginalPath);
    ReleaseStr(sczOriginalDirectory);
    return hr;
}
