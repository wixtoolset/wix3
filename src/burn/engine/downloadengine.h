//-------------------------------------------------------------------------------------------------
// <copyright file="downloadengine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Setup chainer/bootstrapper download engine for WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

// structs


// functions

HRESULT WininetDownloadUrl(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_CACHE_CALLBACK* pCallback,
    __in_z LPCWSTR wzPackageOrContainerId,
    __in_z LPCWSTR wzPayloadId,
    __in BURN_DOWNLOAD_SOURCE* pDownloadSource,
    __in DWORD64 dw64AuthoredDownloadSize,
    __in LPCWSTR wzDestinationPath
    );


#ifdef __cplusplus
}
#endif
