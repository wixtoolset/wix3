// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


/******************************************************************
WixExitEarlyWithSuccess - entry point for WixExitEarlyWithSuccess
    custom action which does nothing except return exit code
    ERROR_NO_MORE_ITEMS. The Windows Installer documentation at
    http://msdn.microsoft.com/library/aa368072.aspx indicates that
    this exit code is not treated as an error. This will cause a
    calling application to receive a successful return code if
    this custom action executes. This can be useful for backwards
    compatibility when an application redistributes an MSI and
    a future major upgrade is released for that MSI. It should be
    conditioned on a property set by an entry in the Upgrade table 
    of the MSI that detects newer major upgrades of the same MSI
    already installed on the system. It should be scheduled after
    the FindRelatedProducts action so that the property will be
    set if appropriate.
********************************************************************/
extern "C" UINT __stdcall WixExitEarlyWithSuccess(
    __in MSIHANDLE /*hInstall*/
    )
{
    return ERROR_NO_MORE_ITEMS;
}
