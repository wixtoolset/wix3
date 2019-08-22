#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>
#include <gdiplus.h>
#include <msiquery.h>
#include <objbase.h>
#include <shlobj.h>
#include <shlwapi.h>
#include <stdlib.h>
#include <strsafe.h>
#include <stddef.h>

#include "dutil.h"
#include "apputil.h"
#include "memutil.h"
#include "dictutil.h"
#include "dirutil.h"
#include "fileutil.h"
#include "locutil.h"
#include "logutil.h"
#include "pathutil.h"
#include "resrutil.h"
#include "shelutil.h"
#include "strutil.h"
#include "thmutil.h"
#include "uriutil.h"
#include "xmlutil.h"

#include "IBootstrapperEngine.h"
#include "IBootstrapperApplication.h"

#include "BalBaseBootstrapperApplication.h"
#include "balinfo.h"
#include "balcondition.h"

HRESULT CreateBootstrapperApplication(
    __in HMODULE hModule,
    __in BOOL fPrereq,
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppApplication
    );

#include "IBootstrapperBAFunction.h"

