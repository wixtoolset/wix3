//-------------------------------------------------------------------------------------------------
// <copyright file="update.h" company="Outercurve Foundation">
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
