//-------------------------------------------------------------------------------------------------
// <copyright file="pseudobundle.h" company="Outercurve Foundation">
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

#pragma once


#if defined(__cplusplus)
extern "C" {
#endif

HRESULT PseudoBundleInitialize(
    __in DWORD64 qwEngineVersion,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in_z LPCWSTR wzFilePath,
    __in_z LPCWSTR wzLocalSource,
    __in_z_opt LPCWSTR wzDownloadSource,
    __in DWORD64 qwSize,
    __in BOOL fVital,
    __in_z_opt LPCWSTR wzInstallArguments,
    __in_z_opt LPCWSTR wzRepairArguments,
    __in_z_opt LPCWSTR wzUninstallArguments,
    __in_opt BURN_DEPENDENCY_PROVIDER* pDependencyProvider,
    __in_opt BYTE* pbHash,
    __in DWORD cbHash
    );
HRESULT PseudoBundleInitializePassthrough(
    __in BURN_PACKAGE* pPassthroughPackage,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in_z_opt LPCWSTR wzApppendLogPath,
    __in_z_opt LPWSTR wzActiveParent,
    __in_z_opt LPWSTR wzAncestors,
    __in BURN_PACKAGE* pPackage
    );

#if defined(__cplusplus)
}
#endif
