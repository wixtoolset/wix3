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
//    Precompiled header for MSMQ Execution CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#include <windows.h>
#include <msiquery.h>
#include <strsafe.h>
#include <ntsecapi.h>
#include <aclapi.h>
#include <mq.h>

#include "wcautil.h"
#include "memutil.h"
#include "strutil.h"
#include "wiutil.h"

#include "CustomMsiErrors.h"

#include "..\inc\mqcost.h"
#include "mqutilexec.h"
#include "mqqueueexec.h"
