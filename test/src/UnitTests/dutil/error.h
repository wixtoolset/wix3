// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

const int ERROR_STRING_BUFFER = 1024;

static char szMsg[ERROR_STRING_BUFFER];
static WCHAR wzMsg[ERROR_STRING_BUFFER];

#define ExitTrace(x, f, ...) { HRESULT hrTemp = x; hr = ::StringCchPrintfA(szMsg, countof(szMsg), f, __VA_ARGS__); MultiByteToWideChar(CP_ACP, 0, szMsg, -1, wzMsg, countof(wzMsg)); throw gcnew System::Exception(System::String::Format("hr = 0x{0:X8}, message = {1}", hrTemp, gcnew System::String(wzMsg))); }
#define ExitTrace1 ExitTrace
#define ExitTrace2 ExitTrace
#define ExitTrace3 ExitTrace
