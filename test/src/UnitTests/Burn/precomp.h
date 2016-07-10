#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>
#include <Bits.h>
#include <msiquery.h>
#include <objbase.h>
#include <shlobj.h>
#include <shlwapi.h>
#include <stdlib.h>
#include <strsafe.h>
#include "wininet.h"

#include <dutil.h>
#include <cryputil.h>
#include <dlutil.h>
#include <buffutil.h>
#include <dirutil.h>
#include <fileutil.h>
#include <logutil.h>
#include <memutil.h>
#include <pathutil.h>
#include <regutil.h>
#include <resrutil.h>
#include <shelutil.h>
#include <strutil.h>
#include <wiutil.h>
#include <xmlutil.h>
#include <dictutil.h>
#include <deputil.h>

#include <wixver.h>

#include "IBootstrapperEngine.h"
#include "IBootstrapperApplication.h"

#include "platform.h"
#include "variant.h"
#include "variable.h"
#include "condition.h"
#include "search.h"
#include "section.h"
#include "approvedexe.h"
#include "container.h"
#include "catalog.h"
#include "payload.h"
#include "cabextract.h"
#include "userexperience.h"
#include "package.h"
#include "update.h"
#include "pseudobundle.h"
#include "registration.h"
#include "plan.h"
#include "pipe.h"
#include "logging.h"
#include "core.h"
#include "cache.h"
#include "apply.h"
#include "exeengine.h"
#include "msiengine.h"
#include "mspengine.h"
#include "msuengine.h"
#include "dependency.h"
#include "elevation.h"
#include "embedded.h"
#include "manifest.h"
#include "splashscreen.h"
#include "bitsengine.h"

#pragma managed
#include <vcclr.h>

#include "BurnTestException.h"
#include "BurnUnitTest.h"
#include "VariableHelpers.h"
#include "ManifestHelpers.h"
