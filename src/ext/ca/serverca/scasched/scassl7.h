#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="scassl7.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    SSL functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

// prototypes
HRESULT ScaSslCertificateWrite7(
    __in_z LPCWSTR wzWebBase,
    __in SCA_WEB_SSL_CERTIFICATE* pswscList
    );
