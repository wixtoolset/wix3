//-------------------------------------------------------------------------------------------------
// <copyright file="cache.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Burn cache functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

// structs

// functions

HRESULT CacheInitialize(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    );
HRESULT CacheEnsureWorkingFolder(
    __in LPCWSTR wzBundleId,
    __deref_out_z_opt LPWSTR* psczWorkingFolder
    );
HRESULT CacheCalculateBundleWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in LPCWSTR wzExecutableName,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculateBundleLayoutWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculatePayloadWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in BURN_PAYLOAD* pPayload,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCaclulateContainerWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in BURN_CONTAINER* pContainer,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheGetCompletedPath(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzCacheId,
    __deref_out_z LPWSTR* psczCompletedPath
    );
HRESULT CacheGetResumePath(
    __in_z LPCWSTR wzPayloadWorkingPath,
    __deref_out_z LPWSTR* psczResumePath
    );
HRESULT CacheFindLocalSource(
    __in_z LPCWSTR wzSourcePath,
    __in BURN_VARIABLES* pVariables,
    __out BOOL* pfFound,
    __out_z LPWSTR* psczSourceFullPath
    );
HRESULT CacheSetLastUsedSource(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzRelativePath
    );
HRESULT CacheSendProgressCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in HANDLE hDestinationFile
    );
void CacheSendErrorCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in HRESULT hrError,
    __in_z_opt LPCWSTR wzError,
    __out_opt BOOL* pfRetry
    );
BOOL CacheBundleRunningFromCache();
HRESULT CacheBundleToWorkingDirectory(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_PAYLOADS* pUxPayloads,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    );
HRESULT CacheLayoutBundle(
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzSourceBundlePath
    );
HRESULT CacheCompleteBundle(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzBundleId,
    __in BURN_PAYLOADS* pUxPayloads,
    __in_z LPCWSTR wzSourceBundlePath
#ifdef DEBUG
    , __in_z LPCWSTR wzExecutablePath
#endif
    );
HRESULT CacheLayoutContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove
    );
HRESULT CacheLayoutPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove
    );
HRESULT CacheCompletePayload(
    __in BOOL fPerMachine,
    __in BURN_PAYLOAD* pPayload,
    __in_z_opt LPCWSTR wzCacheId,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove
    );
HRESULT CacheRemoveWorkingFolder(
    __in_z_opt LPCWSTR wzBundleId
    );
HRESULT CacheRemoveBundle(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId
    );
HRESULT CacheRemovePackage(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCacheId
    );
HRESULT CacheVerifyPayloadSignature(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile
    );
void CacheCleanup(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleId
    );

#ifdef __cplusplus
}
#endif
