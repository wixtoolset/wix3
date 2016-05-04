#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "precomp.h"

DECLARE_INTERFACE_IID_(IBootstrapperApplicationFactory, IUnknown, "2965A12F-AC7B-43A0-85DF-E4B2168478A4")
{
    STDMETHOD(Create)(
        __in IBootstrapperEngine* pEngine,
        __in const BOOTSTRAPPER_COMMAND *pCommand,
        __out IBootstrapperApplication **ppApplication
        );
};
