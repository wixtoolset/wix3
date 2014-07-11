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

        // @Key
        hr = XmlGetAttributeEx(pixnNode, L"Key", &pApprovedExe->sczKey);
        ExitOnFailure(hr, "Failed to get @Key.");

        // @ValueName
        hr = XmlGetAttributeEx(pixnNode, L"ValueName", &pApprovedExe->sczValueName);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @ValueName.");
        }

        // @Win64
        hr = XmlGetYesNoAttribute(pixnNode, L"Win64", &pApprovedExe->fWin64);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Win64.");
        }

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

extern "C" void ApprovedExesUninitialize(
    __in BURN_APPROVED_EXES* pApprovedExes
    )
{
    if (pApprovedExes->rgApprovedExes)
    {
        for (DWORD i = 0; i < pApprovedExes->cApprovedExes; ++i)
        {
            BURN_APPROVED_EXE* pApprovedExe = &pApprovedExes->rgApprovedExes[i];

            ReleaseStr(pApprovedExe->sczId);
            ReleaseStr(pApprovedExe->sczKey);
            ReleaseStr(pApprovedExe->sczValueName);
        }
        MemFree(pApprovedExes->rgApprovedExes);
    }
}

extern "C" void ApprovedExesUninitializeLaunch(
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
    __in BURN_VARIABLES* pVariables,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe,
    __out DWORD* pdwProcessId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArgumentsFormatted = NULL;
    LPWSTR sczArgumentsObfuscated = NULL;
    LPWSTR sczCommand = NULL;
    LPWSTR sczCommandObfuscated = NULL;
    LPWSTR sczExecutableDirectory = NULL;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };

    // build command
    if (pLaunchApprovedExe->sczArguments && *pLaunchApprovedExe->sczArguments)
    {
        hr = VariableFormatString(pVariables, pLaunchApprovedExe->sczArguments, &sczArgumentsFormatted, NULL);
        ExitOnFailure(hr, "Failed to format argument string.");

        hr = StrAllocFormattedSecure(&sczCommand, L"\"%ls\" %s", pLaunchApprovedExe->sczExecutablePath, sczArgumentsFormatted);
        ExitOnFailure(hr, "Failed to create executable command.");

        hr = VariableFormatStringObfuscated(pVariables, pLaunchApprovedExe->sczArguments, &sczArgumentsObfuscated, NULL);
        ExitOnFailure(hr, "Failed to format obfuscated argument string.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"\"%ls\" %s", pLaunchApprovedExe->sczExecutablePath, sczArgumentsObfuscated);
    }
    else
    {
        hr = StrAllocFormatted(&sczCommand, L"\"%ls\"", pLaunchApprovedExe->sczExecutablePath);
        ExitOnFailure(hr, "Failed to create executable command.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"\"%ls\"", pLaunchApprovedExe->sczExecutablePath);
    }
    ExitOnFailure(hr, "Failed to create obfuscated executable command.");

    // Try to get the directory of the executable so we can set the current directory of the process to help those executables
    // that expect stuff to be relative to them.  Best effort only.
    hr = PathGetDirectory(pLaunchApprovedExe->sczExecutablePath, &sczExecutableDirectory);
    if (FAILED(hr))
    {
        ReleaseNullStr(sczExecutableDirectory);
    }

    LogId(REPORT_STANDARD, MSG_LAUNCHING_APPROVED_EXE, pLaunchApprovedExe->sczExecutablePath, sczCommandObfuscated);

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
    StrSecureZeroFreeString(sczArgumentsFormatted);
    ReleaseStr(sczArgumentsObfuscated);
    StrSecureZeroFreeString(sczCommand);
    ReleaseStr(sczCommandObfuscated);
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
    LPWSTR scz = NULL;

    const LPCWSTR vrgSecureFolderVariables[] = {
        L"ProgramFiles64Folder",
        L"ProgramFilesFolder",
    };

    for (DWORD i = 0; i < countof(vrgSecureFolderVariables); ++i)
    {
        LPCWSTR wzSecureFolderVariable = vrgSecureFolderVariables[i];

        hr = VariableGetString(pVariables, wzSecureFolderVariable, &scz);
        if (SUCCEEDED(hr))
        {
            hr = PathDirectoryContainsPath(scz, pLaunchApprovedExe->sczExecutablePath);
            if (S_OK == hr)
            {
                ExitFunction();
            }
        }
        else if (E_NOTFOUND != hr)
        {
            ExitOnFailure1(hr, "Failed to get the variable: %ls", wzSecureFolderVariable);
        }
    }

    // The problem with using a Variable for the root package cache folder is that it might not have been secured yet.
    // Getting it through CacheGetRootCompletedPath makes sure it has been secured.
    hr = CacheGetRootCompletedPath(TRUE, TRUE, &scz);
    ExitOnFailure(hr, "Failed to get the root package cache folder.");

    hr = PathDirectoryContainsPath(scz, pLaunchApprovedExe->sczExecutablePath);
    if (S_OK == hr)
    {
        ExitFunction();
    }

    hr = S_FALSE;

LExit:
    ReleaseStr(scz);

    return hr;
}
