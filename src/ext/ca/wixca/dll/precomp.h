#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Precompiled header for WiX CA
// </summary>
//-------------------------------------------------------------------------------------------------

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