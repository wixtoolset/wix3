//-------------------------------------------------------------------------------------------------
// <copyright file="bitsengine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Setup chainer/bootstrapper BITS based download engine for WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

// structs


// functions

HRESULT BitsDownloadUrl(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in DOWNLOAD_SOURCE* pDownloadSource,
    __in LPCWSTR wzDestinationPath,
    __in BOOL fBackgroundDownload
    );


#ifdef __cplusplus
}
#endif
