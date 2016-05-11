#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#define CB_CERTIFICATE_HASH 20

// Certificate.Attribute
enum SCA_CERT_ATTRIBUTES
{
    SCA_CERT_ATTRIBUTE_DEFAULT = 0,
    SCA_CERT_ATTRIBUTE_REQUEST = 1,
    SCA_CERT_ATTRIBUTE_BINARYDATA = 2,
    SCA_CERT_ATTRIBUTE_OVERWRITE = 4,
};


// Certificate.StoreLocation
enum SCA_CERTSYSTEMSTORE
{
    SCA_CERTSYSTEMSTORE_CURRENTUSER = 1,
    SCA_CERTSYSTEMSTORE_LOCALMACHINE = 2,
};
