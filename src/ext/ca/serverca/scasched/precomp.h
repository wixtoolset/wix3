#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#include <sal.h>

#include <windows.h>
#include <msiquery.h>
#include <MsiDefs.h>

#include <lm.h>        // NetApi32.lib
#include <xenroll.h> // ICEnroll2
#include <certsrv.h> // ICertRequest
#include <cguid.h>
#include <oledberr.h>
#include <sqloledb.h>
#include <accctrl.h>
#include <aclapi.h>
#include <Dsgetdc.h>

#include <winperf.h>    // PerfMon counter header file.
#include <loadperf.h>   // PerfMon counter header file.

#include <ahadmin.h>    // IIS 7 config

#include <errno.h>

#include "wixstrsafe.h"
#include "wcautil.h"
#include "wcawrapquery.h"
#include "certutil.h"
#include "fileutil.h"
#include "iis7util.h"
#include "memutil.h"
#include "metautil.h"
#include "perfutil.h"
#include "strutil.h"
#include "userutil.h"
#include "wiutil.h"
#include "cryputil.h"

#include "CustomMsiErrors.h"

#include "..\inc\sca.h"
#include "..\inc\scacost.h"

#include "scaapppool.h"
#include "scacert.h"
#include "scadb.h"
#include "scafilter.h"
#include "scaiis.h"
#include "scamimemap.h"
#include "scahttpheader.h"
#include "scaproperty.h"
#include "scassl.h"
#include "scasmb.h"
#include "scasqlstr.h"
#include "scaweb.h"
#include "scawebdir.h"
#include "scaweblog.h"
#include "scawebsvcext.h"
#include "scavdir.h"
#include "scaiis7.h"
#include "scaweb7.h"
#include "scaapppool7.h"
#include "scavdir7.h"
#include "scawebapp7.h"
#include "scawebappext7.h"
#include "scamimemap7.h"
#include "scawebprop7.h"
#include "scaweblog7.h"
#include "scafilter7.h"
#include "scahttpheader7.h"
#include "scaweberr7.h"
#include "scawebsvcext7.h"
#include "scaproperty7.h"
#include "scawebdir7.h"
#include "scassl7.h"

#include "caSuffix.h"
