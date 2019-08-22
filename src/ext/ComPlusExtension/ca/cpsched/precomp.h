#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#include <windows.h>
#include <msiquery.h>
#include <strsafe.h>
#include <comadmin.h>

#include "wcautil.h"
#include "memutil.h"
#include "strutil.h"
#include "wiutil.h"

#include "CustomMsiErrors.h"

#include "..\inc\cpcost.h"
#include "cputilsched.h"
#include "cppartsched.h"
#include "cppartrolesched.h"
#include "cpappsched.h"
#include "cpapprolesched.h"
#include "cpasmsched.h"
#include "cpsubssched.h"
