// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "RemoteMsiSession.h"

void Log(MSIHANDLE hSession, const wchar_t* szMessage, ...);

int ExtractCabinet(const wchar_t* szCabFile, const wchar_t* szExtractDir);

bool DeleteDirectory(const wchar_t* szDir);

__success(return != false)
bool ExtractToTempDirectory(__in MSIHANDLE hSession, __in HMODULE hModule,
	__out_ecount_z(cchTempDirBuf) wchar_t* szTempDir, DWORD cchTempDirBuf);

bool LoadCLR(MSIHANDLE hSession, const wchar_t* szVersion, const wchar_t* szConfigFile,
	const wchar_t* szPrimaryAssembly, ICorRuntimeHost** ppHost);

bool CreateAppDomain(MSIHANDLE hSession, ICorRuntimeHost* pHost,
	const wchar_t* szName, const wchar_t* szAppBase,
	const wchar_t* szConfigFile, _AppDomain** ppAppDomain);

bool GetMethod(MSIHANDLE hSession, _AppDomain* pAppDomain,
	const wchar_t* szAssembly, const wchar_t* szClass,
	const wchar_t* szMethod, _MethodInfo** ppCAMethod);

extern HMODULE g_hModule;
extern bool g_fRunningOutOfProc;

extern RemoteMsiSession* g_pRemote;


