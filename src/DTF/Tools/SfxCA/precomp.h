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

#include <windows.h>
#include <msiquery.h>
#include <strsafe.h>
#include <mscoree.h>
#include <io.h>
#include <fcntl.h>
#include <share.h>
#include <shlwapi.h>
#include <sys/stat.h>
#include <malloc.h>
#include <fdi.h>
#include <msiquery.h>
#import <mscorlib.tlb> raw_interfaces_only rename("ReportEvent", "CorReportEvent")
using namespace mscorlib;
