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
//    Precompiled header for Server execution CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#include <windows.h>
#include <msiquery.h>
#include <oleauto.h>

#include <Iads.h>
#include <activeds.h> 
#include <lm.h>        // NetApi32.lib
#include <LMaccess.h>
#include <LMErr.h>
#include <Ntsecapi.h>
#include <Dsgetdc.h>
#include <wincrypt.h>
#include <ComAdmin.h>
#include <ahadmin.h>    // IIS 7 config 

#include "wixstrsafe.h"
#include "wcautil.h"
#include "wcawow64.h"
#include "aclutil.h"
#include "certutil.h"
#include "dirutil.h"
#include "iis7util.h"
#include "fileutil.h"
#include "memutil.h"
#include "metautil.h"
#include "pathutil.h"
#include "perfutil.h"
#include "strutil.h"
#include "sqlutil.h"
#include "userutil.h"
#include "cryputil.h"

#include "CustomMsiErrors.h"
#include "scasmbexec.h"
#include "..\inc\sca.h"
#include "..\inc\scacost.h"

#include "scaexecIIS7.h"
