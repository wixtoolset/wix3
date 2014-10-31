//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Pre-compiled header.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#include <windows.h>
#include <msiquery.h>
#include <metahost.h>
#include <shlwapi.h>

#import <mscorlib.tlb> raw_interfaces_only rename("ReportEvent", "mscorlib_ReportEvent")

#include <dutil.h>
#include <osutil.h>
#include <pathutil.h>
#include <regutil.h>
#include <strutil.h>
#include <xmlutil.h>

#include "IBootstrapperEngine.h"
#include "IBootstrapperApplication.h"
#include "IBootstrapperApplicationFactory.h"

#include "balutil.h"
