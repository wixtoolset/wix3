#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// structs

typedef struct _BURN_UPDATE
{
    BOOL fUpdateAvailable;
    LPWSTR sczUpdateSource;

    BURN_PACKAGE package;
} BURN_UPDATE;


// function declarations

HRESULT UpdateParseFromXml(
    __in BURN_UPDATE* pUpdate,
    __in IXMLDOMNode* pixnBundle
    );
void UpdateUninitialize(
    __in BURN_UPDATE* pUpdate
    );

#if defined(__cplusplus)
}
#endif
