// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// internal function declarations

static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    );


// function definitions

/*******************************************************************
 UserExperienceParseFromXml - 

*******************************************************************/
extern "C" HRESULT UserExperienceParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnUserExperienceNode = NULL;

    // select UX node
    hr = XmlSelectSingleNode(pixnBundle, L"UX", &pixnUserExperienceNode);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    ExitOnFailure(hr, "Failed to select user experience node.");

    // parse splash screen
    hr = XmlGetYesNoAttribute(pixnUserExperienceNode, L"SplashScreen", &pUserExperience->fSplashScreen);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to to get UX/@SplashScreen");
    }

    // parse payloads
    hr = PayloadsParseFromXml(&pUserExperience->payloads, NULL, NULL, pixnUserExperienceNode);
    ExitOnFailure(hr, "Failed to parse user experience payloads.");

    // make sure we have at least one payload
    if (0 == pUserExperience->payloads.cPayloads)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Too few UX payloads.");
    }

LExit:
    ReleaseObject(pixnUserExperienceNode);

    return hr;
}

/*******************************************************************
 UserExperienceUninitialize - 

*******************************************************************/
extern "C" void UserExperienceUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    ReleaseStr(pUserExperience->sczTempDirectory);
    PayloadsUninitialize(&pUserExperience->payloads);

    // clear struct
    memset(pUserExperience, 0, sizeof(BURN_USER_EXPERIENCE));
}

/*******************************************************************
 UserExperienceLoad - 

*******************************************************************/
extern "C" HRESULT UserExperienceLoad(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IBootstrapperEngine* pEngine,
    __in BOOTSTRAPPER_COMMAND* pCommand
    )
{
    HRESULT hr = S_OK;

    // load UX DLL
    pUserExperience->hUXModule = ::LoadLibraryExW(pUserExperience->payloads.rgPayloads[0].sczLocalFilePath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    ExitOnNullWithLastError(pUserExperience->hUXModule, hr, "Failed to load UX DLL.");

    // get BoostrapperApplicationCreate entry-point
    PFN_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = (PFN_BOOTSTRAPPER_APPLICATION_CREATE)::GetProcAddress(pUserExperience->hUXModule, "BootstrapperApplicationCreate");
    ExitOnNullWithLastError(pfnCreate, hr, "Failed to get BootstrapperApplicationCreate entry-point");

    // create UX
    hr = pfnCreate(pEngine, pCommand, &pUserExperience->pUserExperience);
    ExitOnFailure(hr, "Failed to create UX.");

LExit:
    return hr;
}

/*******************************************************************
 UserExperienceUnload - 

*******************************************************************/
extern "C" HRESULT UserExperienceUnload(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    ReleaseNullObject(pUserExperience->pUserExperience);

    if (pUserExperience->hUXModule)
    {
        // Get BootstrapperApplicationDestroy entry-point and call it if it exists.
        PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = (PFN_BOOTSTRAPPER_APPLICATION_DESTROY)::GetProcAddress(pUserExperience->hUXModule, "BootstrapperApplicationDestroy");
        if (pfnDestroy)
        {
            pfnDestroy();
        }

        // free UX DLL
        if (!::FreeLibrary(pUserExperience->hUXModule))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            TraceError(hr, "Failed to unload UX DLL.");
        }
        pUserExperience->hUXModule = NULL;
    }

//LExit:
    return hr;
}

extern "C" HRESULT UserExperienceEnsureWorkingFolder(
    __in LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    hr = CacheEnsureWorkingFolder(wzBundleId, &sczWorkingFolder);
    ExitOnFailure(hr, "Failed to create working folder.");

    hr = StrAllocFormatted(psczUserExperienceWorkingFolder, L"%ls%ls\\", sczWorkingFolder, L".ba");
    ExitOnFailure(hr, "Failed to calculate the bootstrapper application working path.");

    hr = DirEnsureExists(*psczUserExperienceWorkingFolder, NULL);
    ExitOnFailure(hr, "Failed create bootstrapper application working folder.");

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}


extern "C" HRESULT UserExperienceRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    // Remove temporary UX directory
    if (pUserExperience->sczTempDirectory)
    {
        hr = DirEnsureDeleteEx(pUserExperience->sczTempDirectory, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        TraceError(hr, "Could not delete bootstrapper application folder. Some files will be left in the temp folder.");
    }

//LExit:
    return hr;
}

extern "C" int UserExperienceSendError(
    __in IBootstrapperApplication* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
    )
{
    int nResult = IDNOACTION;
    DWORD dwCode = HRESULT_CODE(hrCode);
    LPWSTR sczError = NULL;

    // If no error string was provided, try to get the error string from the HRESULT.
    if (!wzError)
    {
        if (SUCCEEDED(StrAllocFromError(&sczError, hrCode, NULL)))
        {
            wzError = sczError;
        }
    }

    nResult = pUserExperience->OnError(errorType, wzPackageId, dwCode, wzError, uiFlags, 0, NULL, nRecommendation);

//LExit:
    ReleaseStr(sczError);
    return nResult;
}

extern "C" HRESULT UserExperienceActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out_opt BOOL* pfActivated
    )
{
    HRESULT hr = S_OK;
    BOOL fActivated;

    ::EnterCriticalSection(&pUserExperience->csEngineActive);
    if (InterlockedCompareExchange(reinterpret_cast<LONG*>(&pUserExperience->fEngineActive), TRUE, FALSE))
    {
        AssertSz(FALSE, "Engine should have been deactivated before activating it.");

        fActivated = FALSE;
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
    }
    else
    {
        fActivated = TRUE;
    }
    ::LeaveCriticalSection(&pUserExperience->csEngineActive);

    if (pfActivated)
    {
        *pfActivated = fActivated;
    }
    ExitOnRootFailure(hr, "Engine active cannot be changed because it was already in that state.");

LExit:
    return hr;
}

extern "C" void UserExperienceDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    BOOL fActive = InterlockedExchange(reinterpret_cast<LONG*>(&pUserExperience->fEngineActive), FALSE);
    fActive = fActive; // prevents warning in "ship" build.
    AssertSz(fActive, "Engine should have be active before deactivating it.");
}

extern "C" HRESULT UserExperienceEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = pUserExperience->fEngineActive ? HRESULT_FROM_WIN32(ERROR_BUSY) : S_OK;
    ExitOnRootFailure(hr, "Engine is active, cannot proceed.");

LExit:
    return hr;
}

extern "C" void UserExperienceExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    pUserExperience->hrApplyError = S_OK;
    pUserExperience->hwndApply = NULL;
}

extern "C" void UserExperienceExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    )
{
    if (FAILED(hrResult))
    {
        pUserExperience->hrApplyError = hrResult;
    }
}

extern "C" int UserExperienceCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    // Do not allow canceling while rolling back.
    if (fRollback && (IDCANCEL == nResult || IDABORT == nResult))
    {
        nResult = IDNOACTION;
    }
    else if (FAILED(pUserExperience->hrApplyError) && !fRollback) // if we failed cancel except not during rollback.
    {
        nResult = IDCANCEL;
    }

    nResult = FilterResult(dwAllowedResults, nResult);
    return nResult;
}

extern "C" HRESULT UserExperienceInterpretResult(
    __in BURN_USER_EXPERIENCE* /*pUserExperience*/,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    int nFilteredResult = FilterResult(dwAllowedResults, nResult);
    return IDOK == nFilteredResult || IDNOACTION == nFilteredResult ? S_OK : IDCANCEL == nFilteredResult || IDABORT == nFilteredResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
}

extern "C" HRESULT UserExperienceInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    HRESULT hr = S_OK;

    // If we failed return that error unless this is rollback which should roll on.
    if (FAILED(pUserExperience->hrApplyError) && !fRollback)
    {
        hr = pUserExperience->hrApplyError;
    }
    else
    {
        int nCheckedResult = UserExperienceCheckExecuteResult(pUserExperience, fRollback, dwAllowedResults, nResult);
        hr = IDOK == nCheckedResult || IDNOACTION == nCheckedResult ? S_OK : IDCANCEL == nCheckedResult || IDABORT == nCheckedResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
    }

    return hr;
}


// internal functions

static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    if (IDNOACTION == nResult || IDERROR == nResult) // do nothing and errors pass through.
    {
    }
    else
    {
        switch (dwAllowedResults)
        {
        case MB_OK:
            nResult = IDOK;
            break;

        case MB_OKCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDOK;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_ABORTRETRYIGNORE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDIGNORE == nResult)
            {
                nResult = IDIGNORE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNO:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDNO;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNOCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDNO == nResult)
            {
                nResult = IDNO;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_RETRYCANCEL:
            if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_CANCELTRYCONTINUE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCONTINUE == nResult || IDIGNORE == nResult)
            {
                nResult = IDCONTINUE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case WIU_MB_OKIGNORECANCELRETRY: // custom Windows Installer utility return code.
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDOK;
            }
            else if (IDCONTINUE == nResult || IDIGNORE == nResult)
            {
                nResult = IDIGNORE;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDCANCEL;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult || IDNO == nResult)
            {
                nResult = IDRETRY;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_RETRYTRYAGAIN: // custom return code.
            if (IDRETRY != nResult && IDTRYAGAIN != nResult)
            {
                nResult = IDNOACTION;
            }
            break;

        default:
            AssertSz(FALSE, "Unknown allowed results.");
            break;
        }
    }

    return nResult;
}
