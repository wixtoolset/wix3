#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

struct NetFxDataStructure
{
    bool downloadFinished;               // download done yet?
    bool installFinished;                // install done yet?
    bool downloadAbort;                  // set downloader to abort
    bool installAbort;                   // set installer to abort
    HRESULT hrDownloadFinished;          // resultant HRESULT for download
    HRESULT hrInstallFinished;           // resultant HRESULT for install
    HRESULT hrInternalError;
    WCHAR szCurrentItemStep[MAX_PATH];
    BYTE downloadSoFar;         // download progress 0 - 255 (0 to 100% done) 
    BYTE installSoFar;          // install progress 0 - 255 (0 to 100% done)
    WCHAR szEventName[MAX_PATH];         // event that chainer 'creates' and chainee 'opens'to sync communications

    BYTE version;                        // version of the data structure, set by chainer.

    DWORD messageCode;                   // current message being sent by the chainee, 0 if no message is active
    DWORD messageResponse;               // chainer's response to current message, 0 if not yet handled
    DWORD messageDataLength;             // length of the m_messageData field in bytes
    BYTE messageData[1];                 // variable length buffer, content depends on m_messageCode
};

struct NetFxChainer
{
    HANDLE hSection;

    HANDLE hEventChaineeSend;
    HANDLE hEventChainerSend;
    HANDLE hMutex;

    NetFxDataStructure* pData;
    DWORD dwDataSize;
};

#define NETFXDATA_SIZE           65536

#define NETFXDATA_VERSION        1

#define NETFX_MESSAGE(version, defaultResponse, messageCode) \
    ((((DWORD)version & 0xFF) << 24) | (((DWORD)defaultResponse & 0xFF) << 16) | ((DWORD)messageCode & 0xFFFF))
#define NETFX_MESSAGE_CODE(messageId) \
    (messageId & 0xFFFF)
#define NETFX_MESSAGE_DEFAULT_RESPONSE(messageId) \
    ((messageId >> 16) & 0xFF)
#define NETFX_MESSAGE_VERSION(messageId) \
    ((messageId >>24) & 0xFF)

#define NETFX_NO_MESSAGE    0


//------------------------------------------------------------------------------
// NETFX_CLOSE_APPS
//
// Sent by the chainee when it detects that applications are holding files in 
// use.  Respond to this message in order to tell the chainee to close the 
// applications to prevent a reboot.
//
// pData : NetFxCloseApplications : The list of applications
// Acceptable responses:
//   IDYES   : Indicates that the chainee should attempt to shutdown the apps.
//             If all apps do not successfully close the message may be sent again.
//   IDNO    : Indicates that the chainee should not attempt to close apps.
//   IDRETRY : Indicates that the chainee should refresh the list of apps.
//             Another NETFX_CLOSE_APPS message will be sent asynchronously with
//             the new list of apps.
//------------------------------------------------------------------------------
#define NETFX_CLOSE_APPS    NETFX_MESSAGE(NETFXDATA_VERSION, IDNO, 1)

struct NetFxApplication
{
    WCHAR szName[MAX_PATH];
    DWORD dwPid;
};

struct NetFxCloseApplications
{
    DWORD dwApplicationsSize;
    NetFxApplication applications[1];
};

HRESULT NetFxRunChainer(
    __in LPCWSTR wzExecutablePath,
    __in LPCWSTR wzArguments,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out DWORD* pdwExitCode
    );
#if defined(__cplusplus)
}
#endif
