#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

// structs


// functions

HRESULT BitsDownloadUrl(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in DOWNLOAD_SOURCE* pDownloadSource,
    __in LPCWSTR wzDestinationPath
    );


#ifdef __cplusplus
}
#endif
