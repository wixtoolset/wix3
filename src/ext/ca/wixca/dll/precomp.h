#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if _WIN32_MSI < 150
#define _WIN32_MSI 150
#endif

#include <windows.h>
#include <msiquery.h>
#include <msidefs.h>
#include <shlobj.h>
#include <richedit.h>
#include <msxml2.h>
#include <shobjidl.h>
#include <intshcut.h>
#include <sddl.h>

#include "wixstrsafe.h"
#include "wcautil.h"
#include "wcawow64.h"
#include "aclutil.h"
#include "dirutil.h"
#include "fileutil.h"
#include "memutil.h"
#include "pathutil.h"
#include "procutil.h"
#include "stierr.h"
#include "strutil.h"
#include "rmutil.h"
#include "xmlutil.h"
#include "wiutil.h"
#include "osutil.h"

#include "CustomMsiErrors.h"
#include "cost.h"

#include "caSuffix.h"
