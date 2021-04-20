#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
