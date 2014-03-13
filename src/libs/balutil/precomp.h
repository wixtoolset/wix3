//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Precompiled header for Bootstrapper Application Layer Utility Library.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#include <windows.h>
#include <bitsmsg.h>
#include <msi.h>
#include <wininet.h>

#include <dutil.h>
#include <pathutil.h>
#include <locutil.h>
#include <memutil.h>
#include <strutil.h>
#include <xmlutil.h>

#include "IBootstrapperEngine.h"
#include "IBootstrapperApplication.h"

#include "IBootstrapperBAFunction.h"

#include "balutil.h"
#include "balcondition.h"
#include "balinfo.h"
#include "balretry.h"
