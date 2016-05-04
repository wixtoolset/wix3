// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR BUNDLE_CLEAN_ROOM_WORKING_FOLDER_NAME = L".cr";
static const LPCWSTR BUNDLE_WORKING_FOLDER_NAME = L".be";
static const LPCWSTR UNVERIFIED_CACHE_FOLDER_NAME = L".unverified";
static const LPCWSTR PACKAGE_CACHE_FOLDER_NAME = L"Package Cache";
static const DWORD FILE_OPERATION_RETRY_COUNT = 3;
static const DWORD FILE_OPERATION_RETRY_WAIT = 2000;

static BOOL vfInitializedCache = FALSE;
static BOOL vfRunningFromCache = FALSE;
static LPWSTR vsczSourceProcessPath = NULL;
static LPWSTR vsczWorkingFolder = NULL;
static LPWSTR vsczDefaultUserPackageCache = NULL;
static LPWSTR vsczDefaultMachinePackageCache = NULL;
static LPWSTR vsczCurrentMachinePackageCache = NULL;

static HRESULT CalculateWorkingFolder(
    __in_z LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczWorkingFolder
    );
static HRESULT GetLastUsedSourceFolder(
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczLastSource
    );
static HRESULT CreateCompletedPath(
    __in BOOL fPerMachine,
    __in LPCWSTR wzCacheId,
    __out LPWSTR* psczCacheDirectory
    );
static HRESULT CreateUnverifiedPath(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPayloadId,
    __out_z LPWSTR* psczUnverifiedPayloadPath
    );
static HRESULT GetRootPath(
    __in BOOL fPerMachine,
    __in BOOL fAllowRedirect,
    __deref_out_z LPWSTR* psczRootPath
    );
static HRESULT VerifyThenTransferContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove
    );
static HRESULT VerifyThenTransferPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove
    );
static HRESULT TransferWorkingPathToUnverifiedPath(
    __in_z LPCWSTR wzWorkingPath,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove
    );
static HRESULT VerifyFileAgainstPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzVerifyPath
    );
static HRESULT ResetPathPermissions(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPath
    );
static HRESULT SecurePath(
    __in LPCWSTR wzPath
    );
static HRESULT CopyEngineToWorkingFolder(
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzWorkingFolderName,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_PAYLOADS* pUxPayloads,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    );
static HRESULT CopyEngineWithSignatureFixup(
    __in HANDLE hEngineFile,
    __in_z LPCWSTR wzEnginePath,
    __in_z LPCWSTR wzTargetPath,
    __in BURN_SECTION* pSection
    );
static HRESULT RemoveBundleOrPackage(
    __in BOOL fBundle,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleOrPackageId,
    __in_z LPCWSTR wzCacheId
    );
static HRESULT VerifyHash(
    __in BYTE* pbHash,
    __in DWORD cbHash,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile
    );
static HRESULT VerifyPayloadWithCatalog(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile
    );
static HRESULT VerifyPayloadAgainstChain(
    __in BURN_PAYLOAD* pPayload,
    __in PCCERT_CHAIN_CONTEXT pChainContext
    );


extern "C" HRESULT CacheInitialize(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in_z_opt LPCWSTR wzSourceProcessPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCurrentPath = NULL;
    LPWSTR sczCompletedFolder = NULL;
    LPWSTR sczCompletedPath = NULL;
    LPWSTR sczOriginalSource = NULL;
    LPWSTR sczOriginalSourceFolder = NULL;
    int nCompare = 0;

    if (!vfInitializedCache)
    {
        hr = PathForCurrentProcess(&sczCurrentPath, NULL);
        ExitOnFailure(hr, "Failed to get current process path.");

        // Determine if we are running from the package cache or not.
        hr = CacheGetCompletedPath(pRegistration->fPerMachine, pRegistration->sczId, &sczCompletedFolder);
        ExitOnFailure(hr, "Failed to get completed path for bundle.");

        hr = PathConcat(sczCompletedFolder, pRegistration->sczExecutableName, &sczCompletedPath);
        ExitOnFailure(hr, "Failed to combine working path with engine file name.");

        hr = PathCompare(sczCurrentPath, sczCompletedPath, &nCompare);
        ExitOnFailure1(hr, "Failed to compare current path for bundle: %ls", sczCurrentPath);

        vfRunningFromCache = (CSTR_EQUAL == nCompare);

        // If a source process path was not provided (e.g. we are not being
        // run in a clean room) then use the current process path as the
        // source process path.
        if (!wzSourceProcessPath)
        {
            wzSourceProcessPath = sczCurrentPath;
        }

        hr = StrAllocString(&vsczSourceProcessPath, wzSourceProcessPath, 0);
        ExitOnFailure(hr, "Failed to initialize cache source path.");

        // If we're not running from the cache, ensure the original source is set.
        if (!vfRunningFromCache)
        {
            // If the original source has not been set already then set it where the bundle is
            // running from right now. This value will be persisted and we'll use it when launched
            // from the clean room or package cache since none of our packages will be relative to
            // those locations.
            hr = VariableGetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE, &sczOriginalSource);
            if (E_NOTFOUND == hr)
            {
                hr = VariableSetLiteralString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE, wzSourceProcessPath, FALSE);
                ExitOnFailure(hr, "Failed to set original source variable.");

                hr = StrAllocString(&sczOriginalSource, wzSourceProcessPath, 0);
                ExitOnFailure(hr, "Failed to copy current path to original source.");
            }

            hr = VariableGetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER, &sczOriginalSourceFolder);
            if (E_NOTFOUND == hr)
            {
                hr = PathGetDirectory(sczOriginalSource, &sczOriginalSourceFolder);
                ExitOnFailure(hr, "Failed to get directory from original source path.");

                hr = VariableSetLiteralString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER, sczOriginalSourceFolder, FALSE);
                ExitOnFailure(hr, "Failed to set original source directory variable.");
            }
        }

        vfInitializedCache = TRUE;
    }

LExit:
    ReleaseStr(sczCurrentPath);
    ReleaseStr(sczCompletedFolder);
    ReleaseStr(sczCompletedPath);
    ReleaseStr(sczOriginalSource);
    ReleaseStr(sczOriginalSourceFolder);

    return hr;
}

extern "C" HRESULT CacheEnsureWorkingFolder(
    __in_z LPCWSTR wzBundleId,
    __deref_out_z_opt LPWSTR* psczWorkingFolder
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    hr = CalculateWorkingFolder(wzBundleId, &sczWorkingFolder);
    ExitOnFailure(hr, "Failed to calculate working folder to ensure it exists.");

    hr = DirEnsureExists(sczWorkingFolder, NULL);
    ExitOnFailure(hr, "Failed create working folder.");

    // Best effort to ensure our working folder is not encrypted.
    ::DecryptFileW(sczWorkingFolder, 0);

    if (psczWorkingFolder)
    {
        hr = StrAllocString(psczWorkingFolder, sczWorkingFolder, 0);
        ExitOnFailure(hr, "Failed to copy working folder.");
    }

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}

extern "C" HRESULT CacheCalculateBundleWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in LPCWSTR wzExecutableName,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    Assert(vfInitializedCache);

    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    // If the bundle is running out of the package cache then we use that as the
    // working folder since we feel safe in the package cache.
    if (vfRunningFromCache)
    {
        hr = PathForCurrentProcess(psczWorkingPath, NULL);
        ExitOnFailure(hr, "Failed to get current process path.");
    }
    else // Otherwise, use the real working folder.
    {
        hr = CalculateWorkingFolder(wzBundleId, &sczWorkingFolder);
        ExitOnFailure(hr, "Failed to get working folder for bundle.");

        hr = StrAllocFormatted(psczWorkingPath, L"%ls%ls\\%ls", sczWorkingFolder, BUNDLE_WORKING_FOLDER_NAME, wzExecutableName);
        ExitOnFailure(hr, "Failed to calculate the bundle working path.");
    }

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}

extern "C" HRESULT CacheCalculateBundleLayoutWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    hr = CalculateWorkingFolder(wzBundleId, psczWorkingPath);
    ExitOnFailure(hr, "Failed to get working folder for bundle layout.");

    hr = StrAllocConcat(psczWorkingPath, wzBundleId, 0);
    ExitOnFailure(hr, "Failed to append bundle id for bundle layout working path.");

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}

extern "C" HRESULT CacheCalculatePayloadWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in BURN_PAYLOAD* pPayload,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    HRESULT hr = S_OK;

    hr = CalculateWorkingFolder(wzBundleId, psczWorkingPath);
    ExitOnFailure(hr, "Failed to get working folder for payload.");

    hr = StrAllocConcat(psczWorkingPath, pPayload->sczKey, 0);
    ExitOnFailure(hr, "Failed to append SHA1 hash as payload unverified path.");

LExit:
    return hr;
}

extern "C" HRESULT CacheCalculateContainerWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in BURN_CONTAINER* pContainer,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    HRESULT hr = S_OK;

    hr = CalculateWorkingFolder(wzBundleId, psczWorkingPath);
    ExitOnFailure(hr, "Failed to get working folder for container.");

    hr = StrAllocConcat(psczWorkingPath, pContainer->sczHash, 0);
    ExitOnFailure(hr, "Failed to append SHA1 hash as container unverified path.");

LExit:
    return hr;
}

extern "C" HRESULT CacheGetRootCompletedPath(
    __in BOOL fPerMachine,
    __in BOOL fForceInitialize,
    __deref_out_z LPWSTR* psczRootCompletedPath
    )
{
    HRESULT hr = S_OK;

    if (fForceInitialize)
    {
        hr = CreateCompletedPath(fPerMachine, L"", psczRootCompletedPath);
    }
    else
    {
        hr = GetRootPath(fPerMachine, TRUE, psczRootCompletedPath);
    }

    return hr;
}

extern "C" HRESULT CacheGetCompletedPath(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzCacheId,
    __deref_out_z LPWSTR* psczCompletedPath
    )
{
    HRESULT hr = S_OK;
    BOOL fRedirected = FALSE;
    LPWSTR sczRootPath = NULL;
    LPWSTR sczCurrentCompletedPath = NULL;
    LPWSTR sczDefaultCompletedPath = NULL;

    hr = GetRootPath(fPerMachine, TRUE, &sczRootPath);
    ExitOnFailure1(hr, "Failed to get %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");

    // GetRootPath returns S_FALSE if the package cache is redirected elsewhere.
    fRedirected = S_FALSE == hr;

    hr = PathConcat(sczRootPath, wzCacheId, &sczCurrentCompletedPath);
    ExitOnFailure(hr, "Failed to construct cache path.");

    hr = PathBackslashTerminate(&sczCurrentCompletedPath);
    ExitOnFailure(hr, "Failed to ensure cache path was backslash terminated.");

    // Return the old package cache directory if the new directory does not exist but the old directory does.
    // If neither package cache directory exists return the (possibly) redirected package cache directory.
    if (fRedirected && !DirExists(sczCurrentCompletedPath, NULL))
    {
        hr = GetRootPath(fPerMachine, FALSE, &sczRootPath);
        ExitOnFailure1(hr, "Failed to get old %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");

        hr = PathConcat(sczRootPath, wzCacheId, &sczDefaultCompletedPath);
        ExitOnFailure(hr, "Failed to construct cache path.");

        hr = PathBackslashTerminate(&sczDefaultCompletedPath);
        ExitOnFailure(hr, "Failed to ensure cache path was backslash terminated.");

        if (DirExists(sczDefaultCompletedPath, NULL))
        {
            *psczCompletedPath = sczDefaultCompletedPath;
            sczDefaultCompletedPath = NULL;

            ExitFunction();
        }
    }

    *psczCompletedPath = sczCurrentCompletedPath;
    sczCurrentCompletedPath = NULL;

LExit:
    ReleaseNullStr(sczDefaultCompletedPath);
    ReleaseNullStr(sczCurrentCompletedPath);
    ReleaseNullStr(sczRootPath);

    return hr;
}

extern "C" HRESULT CacheGetResumePath(
    __in_z LPCWSTR wzPayloadWorkingPath,
    __deref_out_z LPWSTR* psczResumePath
    )
{
    HRESULT hr = S_OK;

    hr = StrAllocFormatted(psczResumePath, L"%ls.R", wzPayloadWorkingPath);
    ExitOnFailure(hr, "Failed to create resume path.");

LExit:
    return hr;
}

extern "C" HRESULT CacheFindLocalSource(
    __in_z LPCWSTR wzSourcePath,
    __in BURN_VARIABLES* pVariables,
    __out BOOL* pfFound,
    __out_z LPWSTR* psczSourceFullPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSourceProcessFolder = NULL;
    LPWSTR sczCurrentPath = NULL;
    LPWSTR sczLastSourcePath = NULL;
    LPWSTR sczLastSourceFolder = NULL;
    LPWSTR sczLayoutPath = NULL;
    LPWSTR sczLayoutFolder = NULL;
    LPCWSTR rgwzSearchPaths[3] = { };
    DWORD cSearchPaths = 0;

    // If the source path provided is a full path, obviously that is where we should be looking.
    if (PathIsAbsolute(wzSourcePath))
    {
        rgwzSearchPaths[0] = wzSourcePath;
        cSearchPaths = 1;
    }
    else
    {
        // If we're not running from cache or we couldn't get the last source, use
        // the source path location first. In the case where we are in the bundle's
        // package cache and couldn't find a last used source we unfortunately will
        // be picking the package cache path which isn't likely to have what we are
        // looking for.
        hr = GetLastUsedSourceFolder(pVariables, &sczLastSourceFolder);
        if (!vfRunningFromCache || FAILED(hr))
        {
            hr = PathGetDirectory(vsczSourceProcessPath, &sczSourceProcessFolder);
            ExitOnFailure(hr, "Failed to get current process directory.");

            hr = PathConcat(sczSourceProcessFolder, wzSourcePath, &sczCurrentPath);
            ExitOnFailure(hr, "Failed to combine last source with source.");

            rgwzSearchPaths[0] = sczCurrentPath;
            cSearchPaths = 1;
        }

        // If we have a last used source and it does not duplicate the existing search path,
        // add the last used source to the search path second.
        if (sczLastSourceFolder && *sczLastSourceFolder)
        {
            hr = PathConcat(sczLastSourceFolder, wzSourcePath, &sczLastSourcePath);
            ExitOnFailure(hr, "Failed to combine last source with source.");

            if (0 == cSearchPaths || CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, rgwzSearchPaths[0], -1, sczLastSourcePath, -1))
            {
                rgwzSearchPaths[cSearchPaths] = sczLastSourcePath;
                ++cSearchPaths;
            }
        }

        // Also consider the layout directory if set on the command line or by the BA.
        hr = VariableGetString(pVariables, BURN_BUNDLE_LAYOUT_DIRECTORY, &sczLayoutFolder);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get bundle layout directory property.");

            hr = PathConcat(sczLayoutFolder, wzSourcePath, &sczLayoutPath);
            ExitOnFailure(hr, "Failed to combine layout source with source.");

            rgwzSearchPaths[cSearchPaths] = sczLayoutPath;
            ++cSearchPaths;
        }
    }

    *pfFound = FALSE; // assume we won't find the file locally.

    for (DWORD i = 0; i < cSearchPaths; ++i)
    {
        // If the file exists locally, copy its path.
        if (FileExistsEx(rgwzSearchPaths[i], NULL))
        {
            hr = StrAllocString(psczSourceFullPath, rgwzSearchPaths[i], 0);
            ExitOnFailure(hr, "Failed to copy source path.");

            *pfFound = TRUE;
            break;
        }
    }

    // If nothing was found, return the first thing in our search path as the
    // best path where we thought we should have found the file.
    if (!*pfFound)
    {
        hr = StrAllocString(psczSourceFullPath, rgwzSearchPaths[0], 0);
        ExitOnFailure(hr, "Failed to copy source path.");
    }

LExit:
    ReleaseStr(sczCurrentPath);
    ReleaseStr(sczSourceProcessFolder);
    ReleaseStr(sczLastSourceFolder);
    ReleaseStr(sczLastSourcePath);
    ReleaseStr(sczLayoutFolder);
    ReleaseStr(sczLayoutPath);

    return hr;
}

extern "C" HRESULT CacheSetLastUsedSource(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzRelativePath
    )
{
    HRESULT hr = S_OK;
    size_t cchSourcePath = 0;
    size_t cchRelativePath = 0;
    size_t iSourceRelativePath = 0;
    LPWSTR sczSourceFolder = NULL;
    LPWSTR sczLastSourceFolder = NULL;
    int nCompare = 0;

    hr = ::StringCchLengthW(wzSourcePath, STRSAFE_MAX_CCH, &cchSourcePath);
    ExitOnFailure(hr, "Failed to determine length of source path.");

    hr = ::StringCchLengthW(wzRelativePath, STRSAFE_MAX_CCH, &cchRelativePath);
    ExitOnFailure(hr, "Failed to determine length of relative path.");

    // If the source path is smaller than the relative path (plus space for "X:\") then we know they
    // are not relative to each other.
    if (cchSourcePath < cchRelativePath + 3)
    {
        ExitFunction();
    }

    // If the source path ends with the relative path then this source could be a new path.
    iSourceRelativePath = cchSourcePath - cchRelativePath;
    if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, wzSourcePath + iSourceRelativePath, -1, wzRelativePath, -1))
    {
        hr = StrAllocString(&sczSourceFolder, wzSourcePath, iSourceRelativePath);
        ExitOnFailure(hr, "Failed to trim source folder.");

        hr = VariableGetString(pVariables, BURN_BUNDLE_LAST_USED_SOURCE, &sczLastSourceFolder);
        if (SUCCEEDED(hr))
        {
            nCompare = ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczSourceFolder, -1, sczLastSourceFolder, -1);
        }
        else if (E_NOTFOUND == hr)
        {
            nCompare = CSTR_GREATER_THAN;
            hr = S_OK;
        }

        if (CSTR_EQUAL != nCompare)
        {
            hr = VariableSetLiteralString(pVariables, BURN_BUNDLE_LAST_USED_SOURCE, sczSourceFolder, FALSE);
            ExitOnFailure(hr, "Failed to set last source.");
        }
    }

LExit:
    ReleaseStr(sczLastSourceFolder);
    ReleaseStr(sczSourceFolder);

    return hr;
}

extern "C" HRESULT CacheSendProgressCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in HANDLE hDestinationFile
    )
{
    static LARGE_INTEGER LARGE_INTEGER_ZERO = { };

    HRESULT hr = S_OK;
    DWORD dwResult = PROGRESS_CONTINUE;
    LARGE_INTEGER liTotalSize = { };
    LARGE_INTEGER liTotalTransferred = { };

    if (pCallback->pfnProgress)
    {
        liTotalSize.QuadPart = dw64Total;
        liTotalTransferred.QuadPart = dw64Progress;

        dwResult = (*pCallback->pfnProgress)(liTotalSize, liTotalTransferred, LARGE_INTEGER_ZERO, LARGE_INTEGER_ZERO, 1, CALLBACK_CHUNK_FINISHED, INVALID_HANDLE_VALUE, hDestinationFile, pCallback->pv);
        switch (dwResult)
        {
        case PROGRESS_CONTINUE:
            hr = S_OK;
            break;

        case PROGRESS_CANCEL: __fallthrough; // TODO: should cancel and stop be treated differently?
        case PROGRESS_STOP:
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
            ExitOnRootFailure(hr, "UX aborted on download progress.");

        case PROGRESS_QUIET: // Not actually an error, just an indication to the caller to stop requesting progress.
            pCallback->pfnProgress = NULL;
            hr = S_OK;
            break;

        default:
            hr = E_UNEXPECTED;
            ExitOnRootFailure(hr, "Invalid return code from progress routine.");
        }
    }

LExit:
    return hr;
}

extern "C" void CacheSendErrorCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in HRESULT hrError,
    __in_z_opt LPCWSTR wzError,
    __out_opt BOOL* pfRetry
    )
{
    if (pfRetry)
    {
        *pfRetry = FALSE;
    }

    if (pCallback->pfnCancel)
    {
        int nResult = (*pCallback->pfnCancel)(hrError, wzError, pfRetry != NULL, pCallback->pv);
        if (pfRetry && IDRETRY == nResult)
        {
            *pfRetry = TRUE;
        }
    }
}

extern "C" BOOL CacheBundleRunningFromCache()
{
    return vfRunningFromCache;
}

extern "C" HRESULT CacheBundleToCleanRoom(
    __in BURN_PAYLOADS* pUxPayloads,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczCleanRoomBundlePath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSourcePath = NULL;
    LPWSTR wzExecutableName = NULL;

    hr = PathForCurrentProcess(&sczSourcePath, NULL);
    ExitOnFailure(hr, "Failed to get current path for process to cache to clean room.");

    wzExecutableName = PathFile(sczSourcePath);

    hr = CopyEngineToWorkingFolder(sczSourcePath, BUNDLE_CLEAN_ROOM_WORKING_FOLDER_NAME, wzExecutableName, pUxPayloads, pSection, psczCleanRoomBundlePath);
    ExitOnFailure(hr, "Failed to cache bundle to clean room.");

LExit:
    ReleaseStr(sczSourcePath);

    return hr;
}

extern "C" HRESULT CacheBundleToWorkingDirectory(
    __in_z LPCWSTR /*wzBundleId*/,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_PAYLOADS* pUxPayloads,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    )
{
    Assert(vfInitializedCache);

    HRESULT hr = S_OK;
    LPWSTR sczSourcePath = NULL;

    // Initialize the source.
    hr = PathForCurrentProcess(&sczSourcePath, NULL);
    ExitOnFailure(hr, "Failed to get current process path.");

    // If the bundle is running out of the package cache then we don't need to copy it to
    // the working folder since we feel safe in the package cache and will run from there.
    if (vfRunningFromCache)
    {
        hr = StrAllocString(psczEngineWorkingPath, sczSourcePath, 0);
        ExitOnFailure(hr, "Failed to use current process path as target path.");
    }
    else // otherwise, carry on putting the bundle in the working folder.
    {
        hr = CopyEngineToWorkingFolder(sczSourcePath, BUNDLE_WORKING_FOLDER_NAME, wzExecutableName, pUxPayloads, pSection, psczEngineWorkingPath);
        ExitOnFailure(hr, "Failed to copy engine to working folder.");
    }

LExit:
    ReleaseStr(sczSourcePath);

    return hr;
}

extern "C" HRESULT CacheLayoutBundle(
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzSourceBundlePath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczTargetPath = NULL;

    hr = PathConcat(wzLayoutDirectory, wzExecutableName, &sczTargetPath);
    ExitOnFailure(hr, "Failed to combine completed path with engine file name for layout.");

    LogStringLine(REPORT_STANDARD, "Layout bundle from: '%ls' to: '%ls'", wzSourceBundlePath, sczTargetPath);

    hr = FileEnsureMoveWithRetry(wzSourceBundlePath, sczTargetPath, TRUE, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    ExitOnFailure2(hr, "Failed to layout bundle from: '%ls' to '%ls'", wzSourceBundlePath, sczTargetPath);

LExit:
    ReleaseStr(sczTargetPath);

    return hr;
}

extern "C" HRESULT CacheCompleteBundle(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzBundleId,
    __in BURN_PAYLOADS* pUxPayloads,
    __in_z LPCWSTR wzSourceBundlePath
#ifdef DEBUG
    , __in_z LPCWSTR wzExecutablePath
#endif
    )
{
    HRESULT hr = S_OK;
    int nCompare = 0;
    LPWSTR sczTargetDirectory = NULL;
    LPWSTR sczTargetPath = NULL;
    LPWSTR sczSourceDirectory = NULL;
    LPWSTR sczPayloadSourcePath = NULL;

    hr = CreateCompletedPath(fPerMachine, wzBundleId, &sczTargetDirectory);
    ExitOnFailure(hr, "Failed to create completed cache path for bundle.");

    hr = PathConcat(sczTargetDirectory, wzExecutableName, &sczTargetPath);
    ExitOnFailure(hr, "Failed to combine completed path with engine file name.");

    Assert(CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, wzExecutablePath, -1, sczTargetPath, -1));

    // If the bundle is running out of the package cache then we don't need to copy it there
    // (and don't want to since it'll be in use) so bail.
    hr = PathCompare(wzSourceBundlePath, sczTargetPath, &nCompare);
    ExitOnFailure1(hr, "Failed to compare completed cache path for bundle: %ls", wzSourceBundlePath);

    if (CSTR_EQUAL == nCompare)
    {
        ExitFunction();
    }

    // Otherwise, carry on putting the bundle in the cache.
    LogStringLine(REPORT_STANDARD, "Caching bundle from: '%ls' to: '%ls'", wzSourceBundlePath, sczTargetPath);

    FileRemoveFromPendingRename(sczTargetPath); // best effort to ensure bundle is not deleted from cache post restart.

    hr = FileEnsureCopyWithRetry(wzSourceBundlePath, sczTargetPath, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    ExitOnFailure2(hr, "Failed to cache bundle from: '%ls' to '%ls'", wzSourceBundlePath, sczTargetPath);

    // Reset the path permissions in the cache.
    hr = ResetPathPermissions(fPerMachine, sczTargetPath);
    ExitOnFailure1(hr, "Failed to reset permissions on cached bundle: '%ls'", sczTargetPath);

    hr = PathGetDirectory(wzSourceBundlePath, &sczSourceDirectory);
    ExitOnFailure1(hr, "Failed to get directory from engine working path: %ls", wzSourceBundlePath);

    // Cache external UX payloads to completed path.
    for (DWORD i = 0; i < pUxPayloads->cPayloads; ++i)
    {
        BURN_PAYLOAD* pPayload = &pUxPayloads->rgPayloads[i];

        if (BURN_PAYLOAD_PACKAGING_EXTERNAL == pPayload->packaging)
        {
            hr = PathConcat(sczSourceDirectory, pPayload->sczSourcePath, &sczPayloadSourcePath);
            ExitOnFailure(hr, "Failed to build payload source path.");

            hr = CacheCompletePayload(fPerMachine, pPayload, wzBundleId, sczPayloadSourcePath, FALSE);
            ExitOnFailure1(hr, "Failed to complete the cache of payload: %ls", pPayload->sczKey);
        }
    }

LExit:
    ReleaseStr(sczPayloadSourcePath);
    ReleaseStr(sczSourceDirectory);
    ReleaseStr(sczTargetPath);
    ReleaseStr(sczTargetDirectory);

    return hr;
}

extern "C" HRESULT CacheLayoutContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedPath = NULL;

    hr = PathConcat(wzLayoutDirectory, pContainer->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to concat complete cached path.");

    hr = VerifyThenTransferContainer(pContainer, sczCachedPath, wzUnverifiedContainerPath, fMove);
    ExitOnFailure1(hr, "Failed to layout container from cached path: %ls", sczCachedPath);

LExit:
    ReleaseStr(sczCachedPath);

    return hr;
}

extern "C" HRESULT CacheLayoutPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedPath = NULL;

    hr = PathConcat(wzLayoutDirectory, pPayload->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to concat complete cached path.");

    hr = VerifyThenTransferPayload(pPayload, sczCachedPath, wzUnverifiedPayloadPath, fMove);
    ExitOnFailure1(hr, "Failed to layout payload from cached payload: %ls", sczCachedPath);

LExit:
    ReleaseStr(sczCachedPath);

    return hr;
}

extern "C" HRESULT CacheCompletePayload(
    __in BOOL fPerMachine,
    __in BURN_PAYLOAD* pPayload,
    __in_z_opt LPCWSTR wzCacheId,
    __in_z LPCWSTR wzWorkingPayloadPath,
    __in BOOL fMove
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczCachedPath = NULL;
    LPWSTR sczUnverifiedPayloadPath = NULL;

    hr = CreateCompletedPath(fPerMachine, wzCacheId, &sczCachedDirectory);
    ExitOnFailure1(hr, "Failed to get cached path for package with cache id: %ls", wzCacheId);

    hr = PathConcat(sczCachedDirectory, pPayload->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to concat complete cached path.");

    // If the cached file matches what we expected, we're good.
    hr = VerifyFileAgainstPayload(pPayload, sczCachedPath);
    if (SUCCEEDED(hr))
    {
        ::DecryptFileW(sczCachedPath, 0);  // Let's try to make sure it's not encrypted.
        LogId(REPORT_STANDARD, MSG_VERIFIED_EXISTING_PAYLOAD, pPayload->sczKey, sczCachedPath);
        ExitFunction();
    }
    else if (E_PATHNOTFOUND != hr && E_FILENOTFOUND != hr)
    {
        LogErrorId(hr, MSG_FAILED_VERIFY_PAYLOAD, pPayload->sczKey, sczCachedPath, NULL);

        FileEnsureDelete(sczCachedPath); // if the file existed but did not verify correctly, make it go away.
    }

    hr = CreateUnverifiedPath(fPerMachine, pPayload->sczKey, &sczUnverifiedPayloadPath);
    ExitOnFailure(hr, "Failed to create unverified path.");

    // If the working path exists, let's get it into the unverified path so we can reset the ACLs and verify the file.
    if (FileExistsEx(wzWorkingPayloadPath, NULL))
    {
        hr = TransferWorkingPathToUnverifiedPath(wzWorkingPayloadPath, sczUnverifiedPayloadPath, fMove);
        ExitOnFailure1(hr, "Failed to transfer working path to unverified path for payload: %ls.", pPayload->sczKey);
    }
    else if (!FileExistsEx(sczUnverifiedPayloadPath, NULL)) // if the working path and unverified path do not exist, nothing we can do.
    {
        hr = E_FILENOTFOUND;
        ExitOnFailure3(hr, "Failed to find payload: %ls in working path: %ls and unverified path: %ls", pPayload->sczKey, wzWorkingPayloadPath, sczUnverifiedPayloadPath);
    }

    hr = ResetPathPermissions(fPerMachine, sczUnverifiedPayloadPath);
    ExitOnFailure1(hr, "Failed to reset permissions on unverified cached payload: %ls", pPayload->sczKey);

    hr = VerifyFileAgainstPayload(pPayload, sczUnverifiedPayloadPath);
    if (FAILED(hr))
    {
        LogErrorId(hr, MSG_FAILED_VERIFY_PAYLOAD, pPayload->sczKey, sczUnverifiedPayloadPath, NULL);

        FileEnsureDelete(sczUnverifiedPayloadPath); // if the file did not verify correctly, make it go away.
        ExitFunction();
    }

    LogId(REPORT_STANDARD, MSG_VERIFIED_ACQUIRED_PAYLOAD, pPayload->sczKey, sczUnverifiedPayloadPath, fMove ? "moving" : "copying", sczCachedPath);

    hr = FileEnsureMoveWithRetry(sczUnverifiedPayloadPath, sczCachedPath, TRUE, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    ExitOnFailure1(hr, "Failed to move verified file to complete payload path: %ls", sczCachedPath);

    ::DecryptFileW(sczCachedPath, 0);  // Let's try to make sure it's not encrypted.

LExit:
    ReleaseStr(sczUnverifiedPayloadPath);
    ReleaseStr(sczCachedPath);
    ReleaseStr(sczCachedDirectory);

    return hr;
}

extern "C" HRESULT CacheRemoveWorkingFolder(
    __in_z_opt LPCWSTR wzBundleId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    if (vfInitializedCache)
    {
        hr = CalculateWorkingFolder(wzBundleId, &sczWorkingFolder);
        ExitOnFailure(hr, "Failed to calculate the working folder to remove it.");

        // Try to clean out everything in the working folder.
        hr = DirEnsureDeleteEx(sczWorkingFolder, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        TraceError(hr, "Could not delete bundle engine working folder.");
    }

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}

extern "C" HRESULT CacheRemoveBundle(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleId
    )
{
    HRESULT hr = S_OK;

    hr = RemoveBundleOrPackage(TRUE, fPerMachine, wzBundleId, wzBundleId);
    ExitOnFailure1(hr, "Failed to remove bundle id: %ls.", wzBundleId);

LExit:
    return hr;
}

extern "C" HRESULT CacheRemovePackage(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCacheId
    )
{
    HRESULT hr = S_OK;

    hr = RemoveBundleOrPackage(FALSE, fPerMachine, wzPackageId, wzCacheId);
    ExitOnFailure1(hr, "Failed to remove package id: %ls.", wzPackageId);

LExit:
    return hr;
}

extern "C" HRESULT CacheVerifyPayloadSignature(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile
    )
{
    HRESULT hr = S_OK;
    LONG er = ERROR_SUCCESS;

    GUID guidAuthenticode = WINTRUST_ACTION_GENERIC_VERIFY_V2;
    WINTRUST_FILE_INFO wfi = { };
    WINTRUST_DATA wtd = { };
    CRYPT_PROVIDER_DATA* pProviderData = NULL;
    CRYPT_PROVIDER_SGNR* pSigner = NULL;

    // Verify the payload assuming online.
    wfi.cbStruct = sizeof(wfi);
    wfi.pcwszFilePath = wzUnverifiedPayloadPath;
    wfi.hFile = hFile;

    wtd.cbStruct = sizeof(wtd);
    wtd.dwUnionChoice = WTD_CHOICE_FILE;
    wtd.pFile = &wfi;
    wtd.dwStateAction = WTD_STATEACTION_VERIFY;
    wtd.dwProvFlags = WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT;
    wtd.dwUIChoice = WTD_UI_NONE;

    er = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &guidAuthenticode, &wtd);
    if (er)
    {
        // Verify the payload assuming offline.
        wtd.dwProvFlags |= WTD_CACHE_ONLY_URL_RETRIEVAL;

        er = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &guidAuthenticode, &wtd);
        ExitOnWin32Error1(er, hr, "Failed authenticode verification of payload: %ls", wzUnverifiedPayloadPath);
    }

    pProviderData = WTHelperProvDataFromStateData(wtd.hWVTStateData);
    ExitOnNullWithLastError(pProviderData, hr, "Failed to get provider state from authenticode certificate.");

    pSigner = WTHelperGetProvSignerFromChain(pProviderData, 0, FALSE, 0);
    ExitOnNullWithLastError(pSigner, hr, "Failed to get signer chain from authenticode certificate.");

    hr = VerifyPayloadAgainstChain(pPayload, pSigner->pChainContext);
    ExitOnFailure(hr, "Failed to verify expected payload against actual certificate chain.");

LExit:
    return hr;
}

extern "C" void CacheCleanup(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFolder = NULL;
    LPWSTR sczFiles = NULL;
    LPWSTR sczDelete = NULL;
    HANDLE hFind = INVALID_HANDLE_VALUE;
    WIN32_FIND_DATAW wfd = { };
    DWORD cFileName = 0;

    hr = CacheGetCompletedPath(fPerMachine, UNVERIFIED_CACHE_FOLDER_NAME, &sczFolder);
    if (SUCCEEDED(hr))
    {
        hr = DirEnsureDeleteEx(sczFolder, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
    }

    if (!fPerMachine)
    {
        hr = CalculateWorkingFolder(wzBundleId, &sczFolder);
        if (SUCCEEDED(hr))
        {
            hr = PathConcat(sczFolder, L"*.*", &sczFiles);
            if (SUCCEEDED(hr))
            {
                hFind = ::FindFirstFileW(sczFiles, &wfd);
                if (INVALID_HANDLE_VALUE != hFind)
                {
                    do
                    {
                        // Skip directories.
                        if (wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
                        {
                            continue;
                        }

                        // For extra safety and to silence OACR.
                        wfd.cFileName[MAX_PATH - 1] = L'\0';

                        // Skip resume files (they end with ".R").
                        cFileName = lstrlenW(wfd.cFileName);
                        if (2 < cFileName && L'.' == wfd.cFileName[cFileName - 2] && (L'R' == wfd.cFileName[cFileName - 1] || L'r' == wfd.cFileName[cFileName - 1]))
                        {
                            continue;
                        }

                        hr = PathConcat(sczFolder, wfd.cFileName, &sczDelete);
                        if (SUCCEEDED(hr))
                        {
                            hr = FileEnsureDelete(sczDelete);
                        }
                    } while (::FindNextFileW(hFind, &wfd));
                }
            }
        }
    }

    if (INVALID_HANDLE_VALUE != hFind)
    {
        ::FindClose(hFind);
    }

    ReleaseStr(sczDelete);
    ReleaseStr(sczFiles);
    ReleaseStr(sczFolder);
}

extern "C" void CacheUninitialize()
{
    ReleaseNullStr(vsczCurrentMachinePackageCache);
    ReleaseNullStr(vsczDefaultMachinePackageCache);
    ReleaseNullStr(vsczDefaultUserPackageCache);
    ReleaseNullStr(vsczWorkingFolder);
    ReleaseNullStr(vsczSourceProcessPath);

    vfRunningFromCache = FALSE;
    vfInitializedCache = FALSE;
}

// Internal functions.

static HRESULT CalculateWorkingFolder(
    __in_z LPCWSTR /*wzBundleId*/,
    __deref_out_z LPWSTR* psczWorkingFolder
    )
{
    HRESULT hr = S_OK;
    RPC_STATUS rs = RPC_S_OK;
    WCHAR wzTempPath[MAX_PATH] = { };
    UUID guid = {};
    WCHAR wzGuid[39];

    if (!vsczWorkingFolder)
    {
        if (0 == ::GetTempPathW(countof(wzTempPath), wzTempPath))
        {
            ExitWithLastError(hr, "Failed to get temp path for working folder.");
        }

        rs = ::UuidCreate(&guid);
        hr = HRESULT_FROM_RPC(rs);
        ExitOnFailure(hr, "Failed to create working folder guid.");

        if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
        {
            hr = E_OUTOFMEMORY;
            ExitOnRootFailure(hr, "Failed to convert working folder guid into string.");
        }

        hr = StrAllocFormatted(&vsczWorkingFolder, L"%ls%ls\\", wzTempPath, wzGuid);
        ExitOnFailure(hr, "Failed to append bundle id on to temp path for working folder.");
    }

    hr = StrAllocString(psczWorkingFolder, vsczWorkingFolder, 0);
    ExitOnFailure(hr, "Failed to copy working folder path.");

LExit:
    return hr;
}

static HRESULT GetRootPath(
    __in BOOL fPerMachine,
    __in BOOL fAllowRedirect,
    __deref_out_z LPWSTR* psczRootPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczAppData = NULL;
    int nCompare = 0;

    // Cache paths are initialized once so they cannot be changed while the engine is caching payloads.
    if (fPerMachine)
    {
        // Always construct the default machine package cache path so we can determine if we're redirected.
        if (!vsczDefaultMachinePackageCache)
        {
            hr = PathGetKnownFolder(CSIDL_COMMON_APPDATA, &sczAppData);
            ExitOnFailure1(hr, "Failed to find local %hs appdata directory.", "per-machine");

            hr = PathConcat(sczAppData, PACKAGE_CACHE_FOLDER_NAME, &vsczDefaultMachinePackageCache);
            ExitOnFailure1(hr, "Failed to construct %hs package cache directory name.", "per-machine");

            hr = PathBackslashTerminate(&vsczDefaultMachinePackageCache);
            ExitOnFailure1(hr, "Failed to backslash terminate default %hs package cache directory name.", "per-machine");
        }

        if (!vsczCurrentMachinePackageCache)
        {
            hr = PolcReadString(POLICY_BURN_REGISTRY_PATH, L"PackageCache", NULL, &vsczCurrentMachinePackageCache);
            ExitOnFailure(hr, "Failed to read PackageCache policy directory.");

            if (vsczCurrentMachinePackageCache)
            {
                hr = PathBackslashTerminate(&vsczCurrentMachinePackageCache);
                ExitOnFailure(hr, "Failed to backslash terminate redirected per-machine package cache directory name.");
            }
            else
            {
                hr = StrAllocString(&vsczCurrentMachinePackageCache, vsczDefaultMachinePackageCache, 0);
                ExitOnFailure(hr, "Failed to copy default package cache directory to current package cache directory.");
            }
        }

        hr = StrAllocString(psczRootPath, fAllowRedirect ? vsczCurrentMachinePackageCache : vsczDefaultMachinePackageCache, 0);
        ExitOnFailure1(hr, "Failed to copy %hs package cache root directory.", "per-machine");

        hr = PathCompare(vsczDefaultMachinePackageCache, *psczRootPath, &nCompare);
        ExitOnFailure(hr, "Failed to compare default and current package cache directories.");

        // Return S_FALSE if the current location is not the default location (redirected).
        hr = CSTR_EQUAL == nCompare ? S_OK : S_FALSE;
    }
    else
    {
        if (!vsczDefaultUserPackageCache)
        {
            hr = PathGetKnownFolder(CSIDL_LOCAL_APPDATA, &sczAppData);
            ExitOnFailure1(hr, "Failed to find local %hs appdata directory.", "per-user");

            hr = PathConcat(sczAppData, PACKAGE_CACHE_FOLDER_NAME, &vsczDefaultUserPackageCache);
            ExitOnFailure1(hr, "Failed to construct %hs package cache directory name.", "per-user");

            hr = PathBackslashTerminate(&vsczDefaultUserPackageCache);
            ExitOnFailure1(hr, "Failed to backslash terminate default %hs package cache directory name.", "per-user");
        }

        hr = StrAllocString(psczRootPath, vsczDefaultUserPackageCache, 0);
        ExitOnFailure1(hr, "Failed to copy %hs package cache root directory.", "per-user");
    }

LExit:
    ReleaseStr(sczAppData);

    return hr;
}

static HRESULT GetLastUsedSourceFolder(
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczLastSource
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczOriginalSource = NULL;

    hr = VariableGetString(pVariables, BURN_BUNDLE_LAST_USED_SOURCE, psczLastSource);
    if (E_NOTFOUND == hr)
    {
        // Try the original source folder.
        hr = VariableGetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE, &sczOriginalSource);
        if (SUCCEEDED(hr))
        {
            hr = PathGetDirectory(sczOriginalSource, psczLastSource);
        }
    }

    return hr;
}

static HRESULT CreateCompletedPath(
    __in BOOL fPerMachine,
    __in LPCWSTR wzId,
    __out LPWSTR* psczCacheDirectory
    )
{
    static BOOL fPerMachineCacheRootVerified = FALSE;

    HRESULT hr = S_OK;
    LPWSTR sczCacheDirectory = NULL;

    // If we are doing a permachine install but have not yet verified that the root cache folder
    // was created with the correct ACLs yet, do that now.
    if (fPerMachine && !fPerMachineCacheRootVerified)
    {
        hr = GetRootPath(fPerMachine, TRUE, &sczCacheDirectory);
        ExitOnFailure(hr, "Failed to get cache directory.");

        hr = DirEnsureExists(sczCacheDirectory, NULL);
        ExitOnFailure1(hr, "Failed to create cache directory: %ls", sczCacheDirectory);

        hr = SecurePath(sczCacheDirectory);
        ExitOnFailure1(hr, "Failed to secure cache directory: %ls", sczCacheDirectory);

        fPerMachineCacheRootVerified = TRUE;
    }

    // Get the cache completed path, ensure it exists, and reset any permissions people
    // might have tried to set on the directory so we inherit the (correct!) security
    // permissions from the parent directory.
    hr = CacheGetCompletedPath(fPerMachine, wzId, &sczCacheDirectory);
    ExitOnFailure(hr, "Failed to get cache directory.");

    hr = DirEnsureExists(sczCacheDirectory, NULL);
    ExitOnFailure1(hr, "Failed to create cache directory: %ls", sczCacheDirectory);

    ResetPathPermissions(fPerMachine, sczCacheDirectory);

    *psczCacheDirectory = sczCacheDirectory;
    sczCacheDirectory = NULL;

LExit:
    ReleaseStr(sczCacheDirectory);
    return hr;
}

static HRESULT CreateUnverifiedPath(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPayloadId,
    __out_z LPWSTR* psczUnverifiedPayloadPath
    )
{
    static BOOL fUnverifiedCacheFolderCreated = FALSE;

    HRESULT hr = S_OK;
    LPWSTR sczUnverifiedCacheFolder = NULL;

    hr = CacheGetCompletedPath(fPerMachine, UNVERIFIED_CACHE_FOLDER_NAME, &sczUnverifiedCacheFolder);
    ExitOnFailure(hr, "Failed to get cache directory.");

    if (!fUnverifiedCacheFolderCreated)
    {
        hr = DirEnsureExists(sczUnverifiedCacheFolder, NULL);
        ExitOnFailure1(hr, "Failed to create unverified cache directory: %ls", sczUnverifiedCacheFolder);

        ResetPathPermissions(fPerMachine, sczUnverifiedCacheFolder);
    }

    hr = PathConcat(sczUnverifiedCacheFolder, wzPayloadId, psczUnverifiedPayloadPath);
    ExitOnFailure(hr, "Failed to concat payload id to unverified folder path.");

LExit:
    ReleaseStr(sczUnverifiedCacheFolder);

    return hr;
}

static HRESULT VerifyThenTransferContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Get the container on disk actual hash.
    hFile = ::CreateFileW(wzUnverifiedContainerPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError1(hr, "Failed to open container in working path: %ls", wzUnverifiedContainerPath);
    }

    // Container should have a hash we can use to verify with.
    if (pContainer->pbHash)
    {
        hr = VerifyHash(pContainer->pbHash, pContainer->cbHash, wzUnverifiedContainerPath, hFile);
        ExitOnFailure1(hr, "Failed to verify container hash: %ls", wzCachedPath);
    }

    LogStringLine(REPORT_STANDARD, "%ls container from working path '%ls' to path '%ls'", fMove ? L"Moving" : L"Copying", wzUnverifiedContainerPath, wzCachedPath);

    if (fMove)
    {
        hr = FileEnsureMoveWithRetry(wzUnverifiedContainerPath, wzCachedPath, TRUE, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure2(hr, "Failed to move %ls to %ls", wzUnverifiedContainerPath, wzCachedPath);
    }
    else
    {
        hr = FileEnsureCopyWithRetry(wzUnverifiedContainerPath, wzCachedPath, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure2(hr, "Failed to copy %ls to %ls", wzUnverifiedContainerPath, wzCachedPath);
    }

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}

static HRESULT VerifyThenTransferPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Get the payload on disk actual hash.
    hFile = ::CreateFileW(wzUnverifiedPayloadPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError1(hr, "Failed to open payload in working path: %ls", wzUnverifiedPayloadPath);
    }

    // If the payload has a certificate root public key identifier provided, verify the certificate.
    if (pPayload->pbCertificateRootPublicKeyIdentifier)
    {
        hr = CacheVerifyPayloadSignature(pPayload, wzUnverifiedPayloadPath, hFile);
        ExitOnFailure1(hr, "Failed to verify payload signature: %ls", wzCachedPath);
    }
    else if (pPayload->pCatalog) // If catalog files are specified, attempt to verify the file with a catalog file
    {
        hr = VerifyPayloadWithCatalog(pPayload, wzUnverifiedPayloadPath, hFile);
        ExitOnFailure1(hr, "Failed to verify payload signature: %ls", wzCachedPath);
    }
    else if (pPayload->pbHash) // the payload should have a hash we can use to verify it.
    {
        hr = VerifyHash(pPayload->pbHash, pPayload->cbHash, wzUnverifiedPayloadPath, hFile);
        ExitOnFailure1(hr, "Failed to verify payload hash: %ls", wzCachedPath);
    }

    LogStringLine(REPORT_STANDARD, "%ls payload from working path '%ls' to path '%ls'", fMove ? L"Moving" : L"Copying", wzUnverifiedPayloadPath, wzCachedPath);

    if (fMove)
    {
        hr = FileEnsureMoveWithRetry(wzUnverifiedPayloadPath, wzCachedPath, TRUE, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure2(hr, "Failed to move %ls to %ls", wzUnverifiedPayloadPath, wzCachedPath);
    }
    else
    {
        hr = FileEnsureCopyWithRetry(wzUnverifiedPayloadPath, wzCachedPath, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure2(hr, "Failed to copy %ls to %ls", wzUnverifiedPayloadPath, wzCachedPath);
    }

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}

static HRESULT TransferWorkingPathToUnverifiedPath(
    __in_z LPCWSTR wzWorkingPath,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove
    )
{
    HRESULT hr = S_OK;

    if (fMove)
    {
        hr = FileEnsureMoveWithRetry(wzWorkingPath, wzUnverifiedPayloadPath, TRUE, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure2(hr, "Failed to move %ls to %ls", wzWorkingPath, wzUnverifiedPayloadPath);
    }
    else
    {
        hr = FileEnsureCopyWithRetry(wzWorkingPath, wzUnverifiedPayloadPath, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure2(hr, "Failed to copy %ls to %ls", wzWorkingPath, wzUnverifiedPayloadPath);
    }

LExit:
    return hr;
}

static HRESULT VerifyFileAgainstPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzVerifyPath
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Get the payload on disk actual hash.
    hFile = ::CreateFileW(wzVerifyPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
        if (E_PATHNOTFOUND == hr || E_FILENOTFOUND == hr)
        {
            ExitFunction(); // do not log error when the file was not found.
        }
        ExitOnRootFailure1(hr, "Failed to open payload at path: %ls", wzVerifyPath);
    }

    // If the payload has a certificate root public key identifier provided, verify the certificate.
    if (pPayload->pbCertificateRootPublicKeyIdentifier)
    {
        hr = CacheVerifyPayloadSignature(pPayload, wzVerifyPath, hFile);
        ExitOnFailure1(hr, "Failed to verify signature of payload: %ls", pPayload->sczKey);
    }
    else if (pPayload->pCatalog) // If catalog files are specified, attempt to verify the file with a catalog file
    {
        hr = VerifyPayloadWithCatalog(pPayload, wzVerifyPath, hFile);
        ExitOnFailure1(hr, "Failed to verify catalog signature of payload: %ls", pPayload->sczKey);
    }
    else if (pPayload->pbHash) // the payload should have a hash we can use to verify it.
    {
        hr = VerifyHash(pPayload->pbHash, pPayload->cbHash, wzVerifyPath, hFile);
        ExitOnFailure1(hr, "Failed to verify hash of payload: %ls", pPayload->sczKey);
    }

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}

static HRESULT AllocateSid(
    __in WELL_KNOWN_SID_TYPE type,
    __out PSID* ppSid
    )
{
    HRESULT hr = S_OK;
    PSID pAllocSid = NULL;
    DWORD cbSid = SECURITY_MAX_SID_SIZE;

    pAllocSid = static_cast<PSID>(MemAlloc(cbSid, TRUE));
    ExitOnNull(pAllocSid, hr, E_OUTOFMEMORY, "Failed to allocate memory for well known SID.");

    if (!::CreateWellKnownSid(type, NULL, pAllocSid, &cbSid))
    {
        ExitWithLastError(hr, "Failed to create well known SID.");
    }

    *ppSid = pAllocSid;
    pAllocSid = NULL;

LExit:
    ReleaseMem(pAllocSid);
    return hr;
}


static HRESULT ResetPathPermissions(
    __in BOOL fPerMachine,
    __in LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwSetSecurity = DACL_SECURITY_INFORMATION | UNPROTECTED_DACL_SECURITY_INFORMATION;
    ACL acl = { };
    PSID pSid = NULL;

    if (fPerMachine)
    {
        hr = AllocateSid(WinBuiltinAdministratorsSid, &pSid);
        ExitOnFailure(hr, "Failed to allocate administrator SID.");

        // Create an empty (not NULL!) ACL to reset the permissions on the file to purely inherit from parent.
        if (!::InitializeAcl(&acl, sizeof(acl), ACL_REVISION))
        {
            ExitWithLastError(hr, "Failed to initialize ACL.");
        }

        dwSetSecurity |= OWNER_SECURITY_INFORMATION;
    }

    hr = AclSetSecurityWithRetry(wzPath, SE_FILE_OBJECT, dwSetSecurity, pSid, NULL, &acl, NULL, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    ExitOnWin32Error1(er, hr, "Failed to reset the ACL on cached file: %ls", wzPath);

    ::SetFileAttributesW(wzPath, FILE_ATTRIBUTE_NORMAL); // Let's try to reset any possible read-only/system bits.

LExit:
    ReleaseMem(pSid);
    return hr;
}


static HRESULT GrantAccessAndAllocateSid(
    __in WELL_KNOWN_SID_TYPE type,
    __in DWORD dwGrantAccess,
    __in EXPLICIT_ACCESS* pAccess
    )
{
    HRESULT hr = S_OK;

    hr = AllocateSid(type, reinterpret_cast<PSID*>(&pAccess->Trustee.ptstrName));
    ExitOnFailure(hr, "Failed to allocate SID to grate access.");

    pAccess->grfAccessMode = GRANT_ACCESS;
    pAccess->grfAccessPermissions = dwGrantAccess;
    pAccess->grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
    pAccess->Trustee.TrusteeForm = TRUSTEE_IS_SID;
    pAccess->Trustee.TrusteeType = TRUSTEE_IS_GROUP;

LExit:
    return hr;
}


static HRESULT SecurePath(
    __in LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    EXPLICIT_ACCESSW access[4] = { };
    PACL pAcl = NULL;

    // Administrators must be the first one in the array so we can reuse the allocated SID below.
    hr = GrantAccessAndAllocateSid(WinBuiltinAdministratorsSid, FILE_ALL_ACCESS, &access[0]);
    ExitOnFailure1(hr, "Failed to allocate access for Administrators group to path: %ls", wzPath);

    hr = GrantAccessAndAllocateSid(WinLocalSystemSid, FILE_ALL_ACCESS, &access[1]);
    ExitOnFailure1(hr, "Failed to allocate access for SYSTEM group to path: %ls", wzPath);

    hr = GrantAccessAndAllocateSid(WinWorldSid, GENERIC_READ | GENERIC_EXECUTE, &access[2]);
    ExitOnFailure1(hr, "Failed to allocate access for Everyone group to path: %ls", wzPath);

    hr = GrantAccessAndAllocateSid(WinBuiltinUsersSid, GENERIC_READ | GENERIC_EXECUTE, &access[3]);
    ExitOnFailure1(hr, "Failed to allocate access for Users group to path: %ls", wzPath);

    er = ::SetEntriesInAclW(countof(access), access, NULL, &pAcl);
    ExitOnWin32Error1(er, hr, "Failed to create ACL to secure cache path: %ls", wzPath);

    // Set the ACL and ensure the Administrators group ends up the owner
    hr = AclSetSecurityWithRetry(wzPath, SE_FILE_OBJECT, OWNER_SECURITY_INFORMATION | DACL_SECURITY_INFORMATION | PROTECTED_DACL_SECURITY_INFORMATION,
                                 reinterpret_cast<PSID>(access[0].Trustee.ptstrName), NULL, pAcl, NULL, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    ExitOnFailure1(hr, "Failed to secure cache path: %ls", wzPath);

LExit:
    if (pAcl)
    {
        ::LocalFree(pAcl);
    }

    for (DWORD i = 0; i < countof(access); ++i)
    {
        ReleaseMem(access[i].Trustee.ptstrName);
    }

    return hr;
}


static HRESULT CopyEngineToWorkingFolder(
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzWorkingFolderName,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_PAYLOADS* pUxPayloads,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;
    LPWSTR sczTargetDirectory = NULL;
    LPWSTR sczTargetPath = NULL;
    LPWSTR sczSourceDirectory = NULL;
    LPWSTR sczPayloadSourcePath = NULL;
    LPWSTR sczPayloadTargetPath = NULL;

    hr = CacheEnsureWorkingFolder(NULL, &sczWorkingFolder);
    ExitOnFailure(hr, "Failed to create working path to copy engine.");

    hr = PathConcat(sczWorkingFolder, wzWorkingFolderName, &sczTargetDirectory);
    ExitOnFailure(hr, "Failed to calculate the bundle working folder target name.");

    hr = DirEnsureExists(sczTargetDirectory, NULL);
    ExitOnFailure(hr, "Failed create bundle working folder.");

    hr = PathConcat(sczTargetDirectory, wzExecutableName, &sczTargetPath);
    ExitOnFailure(hr, "Failed to combine working path with engine file name.");

    // Copy the engine without any attached containers to the working path.
    hr = CopyEngineWithSignatureFixup(pSection->hEngineFile, wzSourcePath, sczTargetPath, pSection);
    ExitOnFailure(hr, "Failed to copy engine: '%ls' to working path: %ls", wzSourcePath, sczTargetPath);

    // Copy external UX payloads to working path.
    for (DWORD i = 0; i < pUxPayloads->cPayloads; ++i)
    {
        BURN_PAYLOAD* pPayload = &pUxPayloads->rgPayloads[i];

        if (BURN_PAYLOAD_PACKAGING_EXTERNAL == pPayload->packaging)
        {
            if (!sczSourceDirectory)
            {
                hr = PathGetDirectory(wzSourcePath, &sczSourceDirectory);
                ExitOnFailure(hr, "Failed to get directory from engine path: %ls", wzSourcePath);
            }

            hr = PathConcat(sczSourceDirectory, pPayload->sczSourcePath, &sczPayloadSourcePath);
            ExitOnFailure(hr, "Failed to build payload source path for working copy.");

            hr = PathConcat(sczTargetDirectory, pPayload->sczFilePath, &sczPayloadTargetPath);
            ExitOnFailure(hr, "Failed to build payload target path for working copy.");

            hr = FileEnsureCopyWithRetry(sczPayloadSourcePath, sczPayloadTargetPath, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
            ExitOnFailure(hr, "Failed to copy UX payload from: '%ls' to: '%ls'", sczPayloadSourcePath, sczPayloadTargetPath);
        }
    }

    if (psczEngineWorkingPath)
    {
        hr = StrAllocString(psczEngineWorkingPath, sczTargetPath, 0);
        ExitOnFailure(hr, "Failed to copy target path for engine working path.");
    }

LExit:
    ReleaseStr(sczPayloadTargetPath);
    ReleaseStr(sczPayloadSourcePath);
    ReleaseStr(sczSourceDirectory);
    ReleaseStr(sczTargetPath);
    ReleaseStr(sczTargetDirectory);
    ReleaseStr(sczWorkingFolder);

    return hr;
}


static HRESULT CopyEngineWithSignatureFixup(
    __in HANDLE hEngineFile,
    __in_z LPCWSTR wzEnginePath,
    __in_z LPCWSTR wzTargetPath,
    __in BURN_SECTION* pSection
    )
{
    HRESULT hr = S_OK;
    HANDLE hTarget = INVALID_HANDLE_VALUE;
    LARGE_INTEGER li = { };
    DWORD dwZeroOriginals[3] = { };

    hTarget = ::CreateFileW(wzTargetPath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hTarget)
    {
        ExitWithLastError1(hr, "Failed to create engine file at path: %ls", wzTargetPath);
    }

    hr = FileSetPointer(hEngineFile, 0, NULL, FILE_BEGIN);
    ExitOnFailure1(hr, "Failed to seek to beginning of engine file: %ls", wzEnginePath);

    hr = FileCopyUsingHandles(hEngineFile, hTarget, pSection->cbEngineSize, NULL);
    ExitOnFailure2(hr, "Failed to copy engine from: %ls to: %ls", wzEnginePath, wzTargetPath);

    // If the original executable was signed, let's put back the checksum and signature.
    if (pSection->dwOriginalSignatureOffset)
    {
        // Fix up the checksum.
        li.QuadPart = pSection->dwChecksumOffset;
        if (!::SetFilePointerEx(hTarget, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to seek to checksum in exe header.");
        }

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&pSection->dwOriginalChecksum), sizeof(pSection->dwOriginalChecksum));
        ExitOnFailure(hr, "Failed to update signature offset.");

        // Fix up the signature information.
        li.QuadPart = pSection->dwCertificateTableOffset;
        if (!::SetFilePointerEx(hTarget, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to seek to signature table in exe header.");
        }

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&pSection->dwOriginalSignatureOffset), sizeof(pSection->dwOriginalSignatureOffset));
        ExitOnFailure(hr, "Failed to update signature offset.");

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&pSection->dwOriginalSignatureSize), sizeof(pSection->dwOriginalSignatureSize));
        ExitOnFailure(hr, "Failed to update signature offset.");

        // Zero out the original information since that is how it was when the file was originally signed.
        li.QuadPart = pSection->dwOriginalChecksumAndSignatureOffset;
        if (!::SetFilePointerEx(hTarget, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to seek to original data in exe burn section header.");
        }

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&dwZeroOriginals), sizeof(dwZeroOriginals));
        ExitOnFailure(hr, "Failed to zero out original data offset.");
    }

LExit:
    ReleaseFileHandle(hTarget);

    return hr;
}


static HRESULT RemoveBundleOrPackage(
    __in BOOL fBundle,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleOrPackageId,
    __in_z LPCWSTR wzCacheId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczRootCacheDirectory = NULL;
    LPWSTR sczDirectory = NULL;

    hr = CacheGetCompletedPath(fPerMachine, wzCacheId, &sczDirectory);
    ExitOnFailure(hr, "Failed to calculate cache path.");

    LogId(REPORT_STANDARD, fBundle ? MSG_UNCACHE_BUNDLE : MSG_UNCACHE_PACKAGE, wzBundleOrPackageId, sczDirectory);

    // Try really hard to remove the cache directory.
    hr = E_FAIL;
    for (DWORD iRetry = 0; FAILED(hr) && iRetry < FILE_OPERATION_RETRY_COUNT; ++iRetry)
    {
        if (0 < iRetry)
        {
            ::Sleep(FILE_OPERATION_RETRY_WAIT);
        }

        hr = DirEnsureDeleteEx(sczDirectory, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        if (E_PATHNOTFOUND == hr)
        {
            break;
        }
    }

    if (FAILED(hr))
    {
        LogId(REPORT_STANDARD, fBundle ? MSG_UNABLE_UNCACHE_BUNDLE : MSG_UNABLE_UNCACHE_PACKAGE, wzBundleOrPackageId, sczDirectory, hr);
        hr = S_OK;
    }
    else
    {
        // Try to remove root package cache in the off chance it is now empty.
        hr = GetRootPath(fPerMachine, TRUE, &sczRootCacheDirectory);
        ExitOnFailure1(hr, "Failed to get %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");
        DirEnsureDeleteEx(sczRootCacheDirectory, DIR_DELETE_SCHEDULE);

        // GetRootPath returns S_FALSE if the package cache is redirected elsewhere.
        if (S_FALSE == hr)
        {
            hr = GetRootPath(fPerMachine, FALSE, &sczRootCacheDirectory);
            ExitOnFailure1(hr, "Failed to get old %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");
            DirEnsureDeleteEx(sczRootCacheDirectory, DIR_DELETE_SCHEDULE);
        }
    }

LExit:
    ReleaseStr(sczDirectory);
    ReleaseStr(sczRootCacheDirectory);

    return hr;
}


static HRESULT VerifyHash(
    __in BYTE* pbHash,
    __in DWORD cbHash,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile
    )
{
    UNREFERENCED_PARAMETER(wzUnverifiedPayloadPath);

    HRESULT hr = S_OK;
    BYTE rgbActualHash[SHA1_HASH_LEN] = { };
    DWORD64 qwHashedBytes;
    LPWSTR pszExpected = NULL;
    LPWSTR pszActual = NULL;

    // TODO: create a cryp hash file that sends progress.
    hr = CrypHashFileHandle(hFile, PROV_RSA_FULL, CALG_SHA1, rgbActualHash, sizeof(rgbActualHash), &qwHashedBytes);
    ExitOnFailure1(hr, "Failed to calculate hash for path: %ls", wzUnverifiedPayloadPath);

    // Compare hashes.
    if (cbHash != sizeof(rgbActualHash) || 0 != memcmp(pbHash, rgbActualHash, SHA1_HASH_LEN))
    {
        hr = CRYPT_E_HASH_VALUE;

        // Best effort to log the expected and actual hash value strings.
        if (SUCCEEDED(StrAllocHexEncode(pbHash, cbHash, &pszExpected)) &&
            SUCCEEDED(StrAllocHexEncode(rgbActualHash, SHA1_HASH_LEN, &pszActual)))
        {
            ExitOnFailure3(hr, "Hash mismatch for path: %ls, expected: %ls, actual: %ls", wzUnverifiedPayloadPath, pszExpected, pszActual);
        }
        else
        {
            ExitOnFailure1(hr, "Hash mismatch for path: %ls", wzUnverifiedPayloadPath);
        }
    }

LExit:
    ReleaseStr(pszActual);
    ReleaseStr(pszExpected);

    return hr;
}

static HRESULT VerifyPayloadWithCatalog(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile
    )
{
    HRESULT hr = S_FALSE;
    DWORD er = ERROR_SUCCESS;
    WINTRUST_DATA WinTrustData = { };
    WINTRUST_CATALOG_INFO WinTrustCatalogInfo = { };
    GUID gSubSystemDriver = WINTRUST_ACTION_GENERIC_VERIFY_V2;
    LPWSTR sczLowerCaseFile = NULL;
    LPWSTR pCurrent = NULL;
    LPWSTR sczName = NULL;
    DWORD dwHashSize = 0;
    DWORD dwTagSize;
    LPBYTE pbHash = NULL;

    // Get lower case file name.  Older operating systems need a lower case file
    // to match in the catalog
    hr = StrAllocString(&sczLowerCaseFile, wzUnverifiedPayloadPath, 0);
    ExitOnFailure(hr, "Failed to allocate memory");
    
    // Go through each character doing the lower case of each letter
    pCurrent = sczLowerCaseFile;
    while ('\0' != *pCurrent)
    {
        *pCurrent = (WCHAR)_tolower(*pCurrent);
        pCurrent++;
    }

    // Get file hash
    CryptCATAdminCalcHashFromFileHandle(hFile, &dwHashSize, pbHash, 0);
    er = ::GetLastError();
    if (ERROR_INSUFFICIENT_BUFFER == er)
    {
        pbHash = (LPBYTE)MemAlloc(dwHashSize, TRUE);
        if (!CryptCATAdminCalcHashFromFileHandle(hFile, &dwHashSize, pbHash, 0))
        {
            ExitWithLastError(hr, "Failed to get file hash.");
        }
    }
    else
    {
        ExitOnWin32Error(er, hr, "Failed to get file hash.");
    }

    // Make the hash into a string.  This is the member tag for the catalog
    dwTagSize = (dwHashSize * 2) + 1;
    hr = StrAlloc(&sczName, dwTagSize);
    ExitOnFailure(hr, "Failed to allocate string.");
    hr = StrHexEncode(pbHash, dwHashSize, sczName, dwTagSize);
    ExitOnFailure(hr, "Failed to encode file hash.");

    // Set up the WinVerifyTrust structures assuming online.
    WinTrustData.cbStruct = sizeof(WINTRUST_DATA);
    WinTrustData.dwUIChoice = WTD_UI_NONE;
    WinTrustData.dwUnionChoice = WTD_CHOICE_CATALOG;
    WinTrustData.dwStateAction = WTD_STATEACTION_VERIFY;
    WinTrustData.dwProvFlags = WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT;
    WinTrustData.pCatalog = &WinTrustCatalogInfo;

    WinTrustCatalogInfo.cbStruct = sizeof(WINTRUST_CATALOG_INFO);
    WinTrustCatalogInfo.pbCalculatedFileHash = pbHash;
    WinTrustCatalogInfo.cbCalculatedFileHash = dwHashSize;
    WinTrustCatalogInfo.hMemberFile = hFile;
    WinTrustCatalogInfo.pcwszMemberTag = sczName;
    WinTrustCatalogInfo.pcwszMemberFilePath = sczLowerCaseFile;
    WinTrustCatalogInfo.pcwszCatalogFilePath = pPayload->pCatalog->sczLocalFilePath;

    hr = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &gSubSystemDriver, &WinTrustData);
    if (hr)
    {
        // Set up the WinVerifyTrust structures assuming online.
        WinTrustData.dwProvFlags |= WTD_CACHE_ONLY_URL_RETRIEVAL;

        er = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &gSubSystemDriver, &WinTrustData);

        // WinVerifyTrust returns 0 for success, a few different Win32 error codes if it can't
        // find the provider, and any other error code is provider specific, so may not
        // be an actual Win32 error code
        ExitOnWin32Error1(er, hr, "Could not verify file %ls.", wzUnverifiedPayloadPath);
    }

    // Need to close the WinVerifyTrust action
    WinTrustData.dwStateAction = WTD_STATEACTION_CLOSE;
    er = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &gSubSystemDriver, &WinTrustData);
    ExitOnWin32Error(er, hr, "Could not close verify handle.");

LExit:
    ReleaseStr(sczLowerCaseFile);
    ReleaseStr(sczName);
    ReleaseMem(pbHash);

    return hr;
}

static HRESULT VerifyPayloadAgainstChain(
    __in BURN_PAYLOAD* pPayload,
    __in PCCERT_CHAIN_CONTEXT pChainContext
    )
{
    HRESULT hr = S_OK;
    PCCERT_CONTEXT pChainElementCertContext = NULL;

    BYTE rgbPublicKeyIdentifier[SHA1_HASH_LEN] = { };
    DWORD cbPublicKeyIdentifier = sizeof(rgbPublicKeyIdentifier);
    BYTE* pbThumbprint = NULL;
    DWORD cbThumbprint = 0;

    // Walk up the chain looking for a certificate in the chain that matches our expected public key identifier
    // and thumbprint (if a thumbprint was provided).
    HRESULT hrChainVerification = E_NOTFOUND; // assume we won't find a match.
    for (DWORD i = 0; i < pChainContext->rgpChain[0]->cElement; ++i)
    {
        pChainElementCertContext = pChainContext->rgpChain[0]->rgpElement[i]->pCertContext;

        // Get the certificate's public key identifier.
        if (!::CryptHashPublicKeyInfo(NULL, CALG_SHA1, 0, X509_ASN_ENCODING, &pChainElementCertContext->pCertInfo->SubjectPublicKeyInfo, rgbPublicKeyIdentifier, &cbPublicKeyIdentifier))
        {
            ExitWithLastError(hr, "Failed to get certificate public key identifier.");
        }

        // Compare the certificate's public key identifier with the payload's public key identifier. If they
        // match, we're one step closer to the a positive result.
        if (pPayload->cbCertificateRootPublicKeyIdentifier == cbPublicKeyIdentifier &&
            0 == memcmp(pPayload->pbCertificateRootPublicKeyIdentifier, rgbPublicKeyIdentifier, cbPublicKeyIdentifier))
        {
            // If the payload specified a thumbprint for the certificate, verify it.
            if (pPayload->pbCertificateRootThumbprint)
            {
                hr = CertReadProperty(pChainElementCertContext, CERT_SHA1_HASH_PROP_ID, &pbThumbprint, &cbThumbprint);
                ExitOnFailure(hr, "Failed to read certificate thumbprint.");

                if (pPayload->cbCertificateRootThumbprint == cbThumbprint &&
                    0 == memcmp(pPayload->pbCertificateRootThumbprint, pbThumbprint, cbThumbprint))
                {
                    // If we got here, we found that our payload public key identifier and thumbprint
                    // matched an element in the certficate chain.
                    hrChainVerification = S_OK;
                    break;
                }

                ReleaseNullMem(pbThumbprint);
            }
            else // no thumbprint match necessary so we're good to go.
            {
                hrChainVerification = S_OK;
                break;
            }
        }
    }
    hr = hrChainVerification;
    ExitOnFailure(hr, "Failed to find expected public key in certificate chain.");

LExit:
    ReleaseMem(pbThumbprint);

    return hr;
}
