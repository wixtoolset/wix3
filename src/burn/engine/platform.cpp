// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// variables

PFN_INITIATESYSTEMSHUTDOWNEXW vpfnInitiateSystemShutdownExW;


// function definitions

extern "C" void PlatformInitialize()
{
    vpfnInitiateSystemShutdownExW = ::InitiateSystemShutdownExW;
}
