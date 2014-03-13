//-------------------------------------------------------------------------------------------------
// <copyright file="error.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    These get ExitOnFailure to work with the VS test projects, in a way that feels like dutil code
// </summary>
//-------------------------------------------------------------------------------------------------

const int ERROR_STRING_BUFFER = 1024;

static char szMsg[ERROR_STRING_BUFFER];
static WCHAR wzMsg[ERROR_STRING_BUFFER];

#define ExitTrace(x, s) { HRESULT hrTemp = x; throw gcnew System::Exception(System::String::Format("hr = 0x{0:X8}, message = {1}", hrTemp, gcnew System::String(s))); }
#define ExitTrace1(x, f, s) { HRESULT hrTemp = x; hr = ::StringCchPrintfA(szMsg, countof(szMsg), f, s); MultiByteToWideChar(CP_ACP, 0, szMsg, -1, wzMsg, countof(wzMsg)); throw gcnew System::Exception(System::String::Format("hr = 0x{0:X8}, message = {1}", hrTemp, gcnew System::String(wzMsg))); }
#define ExitTrace2(x, f, s, t) { HRESULT hrTemp = x; hr = ::StringCchPrintfA(szMsg, countof(szMsg), f, s, t); MultiByteToWideChar(CP_ACP, 0, szMsg, -1, wzMsg, countof(wzMsg)); throw gcnew System::Exception(System::String::Format("hr = 0x{0:X8}, message = {1}", hrTemp, gcnew System::String(wzMsg))); }
#define ExitTrace3(x, f, s, t, u) { HRESULT hrTemp = x; hr = ::StringCchPrintfA(szMsg, countof(szMsg), f, s, t, u); MultiByteToWideChar(CP_ACP, 0, szMsg, -1, wzMsg, countof(wzMsg)); throw gcnew System::Exception(System::String::Format("hr = 0x{0:X8}, message = {1}", hrTemp, gcnew System::String(wzMsg))); }

