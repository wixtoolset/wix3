//---------------------------------------------------------------------
// <copyright file="RemoteMsiSession.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

#define LARGE_BUFFER_THRESHOLD 65536 // bytes
#define MIN_BUFFER_STRING_SIZE 1024 // wchar_ts

///////////////////////////////////////////////////////////////////////////////////////
// RemoteMsiSession //
//////////////////////
//
// Allows accessing MSI APIs from another process using named pipes.
//
class RemoteMsiSession
{
public:

    // This enumeration MUST stay in sync with the
    // managed equivalent in RemotableNativeMethods.cs!
    enum RequestId
    {
        EndSession = 0,
        MsiCloseHandle,
        MsiCreateRecord,
        MsiDatabaseGetPrimaryKeys,
        MsiDatabaseIsTablePersistent,
        MsiDatabaseOpenView,
        MsiDoAction,
        MsiEnumComponentCosts,
        MsiEvaluateCondition,
        MsiFormatRecord,
        MsiGetActiveDatabase,
        MsiGetComponentState,
        MsiGetFeatureCost,
        MsiGetFeatureState,
        MsiGetFeatureValidStates,
        MsiGetLanguage,
        MsiGetLastErrorRecord,
        MsiGetMode,
        MsiGetProperty,
        MsiGetSourcePath,
        MsiGetSummaryInformation,
        MsiGetTargetPath,
        MsiProcessMessage,
        MsiRecordClearData,
        MsiRecordDataSize,
        MsiRecordGetFieldCount,
        MsiRecordGetInteger,
        MsiRecordGetString,
        MsiRecordIsNull,
        MsiRecordReadStream,
        MsiRecordSetInteger,
        MsiRecordSetStream,
        MsiRecordSetString,
        MsiSequence,
        MsiSetComponentState,
        MsiSetFeatureAttributes,
        MsiSetFeatureState,
        MsiSetInstallLevel,
        MsiSetMode,
        MsiSetProperty,
        MsiSetTargetPath,
        MsiSummaryInfoGetProperty,
        MsiVerifyDiskSpace,
        MsiViewExecute,
        MsiViewFetch,
        MsiViewGetError,
        MsiViewGetColumnInfo,
        MsiViewModify,
    };

    static const int MAX_REQUEST_FIELDS = 4;

    // Used to pass data back and forth for remote API calls,
    // including in & out params & return values.
    // Only strings and ints are supported.
    struct RequestData
    {
        struct
        {
            VARENUM  vt;
            union {
              int    iValue;
              UINT   uiValue;
              DWORD  cchValue;
              LPWSTR szValue;
              BYTE*  sValue;
              DWORD  cbValue;
            };
        } fields[MAX_REQUEST_FIELDS];
    };

public:

    // This value is set from the single data parameter in the EndSession request.
    // It saves the exit code of the out-of-proc custom action.
    int ExitCode;

    /////////////////////////////////////////////////////////////////////////////////////
    // RemoteMsiSession constructor
    //
    // Creates a new remote session instance, for use either by the server
    // or client process.
    //
    // szName  - Identifies the session instance being remoted. The server and
    //         the client must use the same name. The name should be unique
    //         enough to avoid conflicting with other instances on the system.
    //
    // fServer - True if the calling process is the server process, false if the
    //         calling process is the client process.
    //
    RemoteMsiSession(const wchar_t* szName, bool fServer=true)
        : m_fServer(fServer),
          m_szName(szName != NULL && szName[0] != L'\0' ? szName : L"RemoteMsiSession"),
          m_szPipeName(NULL),
          m_hPipe(NULL),
          m_fConnecting(false),
          m_fConnected(false),
          m_hReceiveThread(NULL),
          m_hReceiveStopEvent(NULL),
          m_pBufReceive(NULL),
          m_cbBufReceive(0),
          m_pBufSend(NULL),
          m_cbBufSend(0),
          ExitCode(ERROR_INSTALL_FAILURE)
    {   
        SecureZeroMemory(&m_overlapped, sizeof(OVERLAPPED));
        m_overlapped.hEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // RemoteMsiSession destructor
    //
    // Closes any open handles and frees any allocated memory.
    //
    ~RemoteMsiSession()
    {
        WaitExitCode();
        if (m_hPipe != NULL)
        {
            CloseHandle(m_hPipe);
            m_hPipe = NULL;
        }
        if (m_overlapped.hEvent != NULL)
        {
            CloseHandle(m_overlapped.hEvent);
            m_overlapped.hEvent = NULL;
        }
        if (m_szPipeName != NULL)
        {
            delete[] m_szPipeName;
            m_szPipeName = NULL;
        }
        if (m_pBufReceive != NULL)
        {
            SecureZeroMemory(m_pBufReceive, m_cbBufReceive);
            delete[] m_pBufReceive;
            m_pBufReceive = NULL;
        }
        if (m_pBufSend != NULL)
        {
            SecureZeroMemory(m_pBufSend, m_cbBufSend);
            delete[] m_pBufSend;
            m_pBufSend = NULL;
        }
        m_fConnecting = false;
        m_fConnected = false;
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // RemoteMsiSession::WaitExitCode()
    //
    // Waits for the server processing thread to complete.
    //
    void WaitExitCode()
    {
        if (m_hReceiveThread != NULL)
        {
            SetEvent(m_hReceiveStopEvent);
            WaitForSingleObject(m_hReceiveThread, INFINITE);
            CloseHandle(m_hReceiveThread);
            m_hReceiveThread = NULL;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // RemoteMsiSession::Connect()
    //
    // Connects the inter-process communication channel.
    // (Currently implemented as a named pipe.)
    //
    // This method must be called first by the server process, then by the client
    // process. The method does not block; the server will asynchronously wait
    // for the client process to make the connection.
    //
    // Returns: 0 on success, Win32 error code on failure.
    //
    virtual DWORD Connect()
    {
        const wchar_t* szPipePrefix = L"\\\\.\\pipe\\";
        size_t cchPipeNameBuf = wcslen(szPipePrefix) + wcslen(m_szName) + 1;
        m_szPipeName = new wchar_t[cchPipeNameBuf];

        if (m_szPipeName == NULL)
        {
            return ERROR_OUTOFMEMORY;
        }
        else
        {
            wcscpy_s(m_szPipeName, cchPipeNameBuf, szPipePrefix);
            wcscat_s(m_szPipeName, cchPipeNameBuf, m_szName);

            if (m_fServer)
            {
                return this->ConnectPipeServer();
            }
            else
            {
                return this->ConnectPipeClient();
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // RemoteMsiSession::IsConnected()
    //
    // Checks if the server process and client process are currently connected.
    //
    virtual bool IsConnected() const
    {
        return m_fConnected;
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // RemoteMsiSession::ProcessRequests()
    //
    // For use by the service process. Watches for requests in the input buffer and calls
    // the callback for each one.
    //
    // This method does not block; it spawns a separate thread to do the work.
    //
    // Returns: 0 on success, Win32 error code on failure.
    //
    virtual DWORD ProcessRequests()
    {
        return this->StartProcessingReqests();
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // RemoteMsiSession::SendRequest()
    //
    // For use by the client process. Sends a request to the server and
    // synchronously waits on a response, up to the timeout value.
    //
    // id         - ID code of the MSI API call being requested.
    //
    // pRequest   - Pointer to a data structure containing request parameters.
    //
    // ppResponse - [OUT] Pointer to a location that receives the response parameters.
    //
    // Returns: 0 on success, Win32 error code on failure.
    // Returns WAIT_TIMEOUT if no response was received in time.
    //
    virtual DWORD SendRequest(RequestId id, const RequestData* pRequest, RequestData** ppResponse)
    {
        if (m_fServer)
        {
            return ERROR_INVALID_OPERATION;
        }

        if (!m_fConnected)
        {
            *ppResponse = NULL;
            return 0;
        }

        DWORD dwRet = this->SendRequest(id, pRequest);
        if (dwRet != 0)
        {
            return dwRet;
        }

        if (id != EndSession)
        {
            static RequestData response;
            if (ppResponse != NULL)
            {
                *ppResponse = &response;
            }

            return this->ReceiveResponse(id, &response);
        }
        else
        {
            CloseHandle(m_hPipe);
            m_hPipe = NULL;
            m_fConnected = false;
            return 0;
        }
    }

private:

    //
    // Do not allow assignment.
    //
    RemoteMsiSession& operator=(const RemoteMsiSession&);

    //
    // Called only by the server process.
    // Create a new thread to handle receiving requests.
    //
    DWORD StartProcessingReqests()
    {
        if (!m_fServer || m_hReceiveStopEvent != NULL)
        {
            return ERROR_INVALID_OPERATION;
        }

        DWORD dwRet = 0;

        m_hReceiveStopEvent = CreateEvent(NULL, TRUE, FALSE, NULL);

        if (m_hReceiveStopEvent == NULL)
        {
            dwRet = GetLastError();
        }
        else
        {
            if (m_hReceiveThread != NULL)
            {
                CloseHandle(m_hReceiveThread);
            }

            m_hReceiveThread = CreateThread(NULL, 0,
                RemoteMsiSession::ProcessRequestsThreadStatic, this, 0, NULL);
            
            if (m_hReceiveThread == NULL)
            {
                dwRet = GetLastError();
                CloseHandle(m_hReceiveStopEvent);
                m_hReceiveStopEvent = NULL;
            }
        }

        return dwRet;
    }

    //
    // Called only by the watcher process.
    // First verify the connection is complete. Then continually read and parse messages,
    // invoke the callback, and send the replies.
    //
    static DWORD WINAPI ProcessRequestsThreadStatic(void* pv)
    {
        return reinterpret_cast<RemoteMsiSession*>(pv)->ProcessRequestsThread();
    }

    DWORD ProcessRequestsThread()
    {
        DWORD dwRet;

        dwRet = CompleteConnection();
        if (dwRet != 0)
        {
            if (dwRet == ERROR_OPERATION_ABORTED) dwRet = 0;
        }

        while (m_fConnected)
        {
            RequestId id;
            RequestData req;
            dwRet = ReceiveRequest(&id, &req);
            if (dwRet != 0)
            {
                if (dwRet == ERROR_OPERATION_ABORTED ||
                    dwRet == ERROR_BROKEN_PIPE || dwRet == ERROR_NO_DATA)
                {
                    dwRet = 0;
                }
            }
            else
            {
                RequestData resp;
                ProcessRequest(id, &req, &resp);

                if (id == EndSession)
                {
                    break;
                }

                dwRet = SendResponse(id, &resp);
                if (dwRet != 0 && dwRet != ERROR_BROKEN_PIPE && dwRet != ERROR_NO_DATA)
                {
                    dwRet = 0;
                }
            }
        }

        CloseHandle(m_hReceiveStopEvent);
        m_hReceiveStopEvent = NULL;
        return dwRet;
    }

    //
    // Called only by the server process's receive thread.
    // Read one request into a RequestData object.
    //
    DWORD ReceiveRequest(RequestId* pId, RequestData* pReq)
    {
        DWORD dwRet = this->ReadPipe((BYTE*) pId, sizeof(RequestId));

        if (dwRet == 0)
        {
            dwRet = this->ReadRequestData(pReq);
        }

        return dwRet;
    }

    //
    // Called by the server process's receive thread or the client's request call
    // to read the response. Read data from the pipe, allowing interruption by the
    // stop event if on the server.
    //
    DWORD ReadPipe(__out_bcount(cbRead) BYTE* pBuf, DWORD cbRead)
    {
        DWORD dwRet = 0;
        DWORD dwTotalBytesRead = 0;

        while (dwRet == 0 && dwTotalBytesRead < cbRead)
        {
            DWORD dwBytesReadThisTime;
            ResetEvent(m_overlapped.hEvent);
            if (!ReadFile(m_hPipe, pBuf + dwTotalBytesRead, cbRead - dwTotalBytesRead, &dwBytesReadThisTime, &m_overlapped))
            {
                dwRet = GetLastError();
                if (dwRet == ERROR_IO_PENDING)
                {
                    if (m_fServer)
                    {
                        HANDLE hWaitHandles[] = { m_overlapped.hEvent, m_hReceiveStopEvent };
                        dwRet = WaitForMultipleObjects(2, hWaitHandles, FALSE, INFINITE);
                    }
                    else
                    {
                        dwRet = WaitForSingleObject(m_overlapped.hEvent, INFINITE);
                    }

                    if (dwRet == WAIT_OBJECT_0)
                    {
                        if (!GetOverlappedResult(m_hPipe, &m_overlapped, &dwBytesReadThisTime, FALSE))
                        {
                            dwRet = GetLastError();
                        }
                    }
                    else if (dwRet == WAIT_FAILED)
                    {
                        dwRet = GetLastError();
                    }
                    else
                    {
                        dwRet = ERROR_OPERATION_ABORTED;
                    }
                }
            }

            dwTotalBytesRead += dwBytesReadThisTime;
        }

        if (dwRet != 0)
        {
            if (m_fServer)
            {
                CancelIo(m_hPipe);
                DisconnectNamedPipe(m_hPipe);
            }
            else
            {
                CloseHandle(m_hPipe);
                m_hPipe = NULL;
            }
            m_fConnected = false;
        }

        return dwRet;
    }

    //
    // Called only by the server process.
    // Given a request, invoke the MSI API and return the response.
    // This is implemented in RemoteMsi.cpp.
    //
    void ProcessRequest(RequestId id, const RequestData* pReq, RequestData* pResp);

    //
    // Called only by the client process.
    // Send request data over the pipe.
    //
    DWORD SendRequest(RequestId id, const RequestData* pRequest)
    {
        DWORD dwRet = WriteRequestData(id, pRequest);
        
        if (dwRet != 0)
        {
            m_fConnected = false;
            CloseHandle(m_hPipe);
            m_hPipe = NULL;
        }

        return dwRet;
    }

    //
    // Called only by the server process.
    // Just send a response over the pipe.
    //
    DWORD SendResponse(RequestId id, const RequestData* pResp)
    {
        DWORD dwRet = WriteRequestData(id, pResp);

        if (dwRet != 0)
        {
            DisconnectNamedPipe(m_hPipe);
            m_fConnected = false;
        }

        return dwRet;
    }

    //
    // Called either by the client or server process.
    // Writes data to the pipe for a request or response.
    //
    DWORD WriteRequestData(RequestId id, const RequestData* pReq)
    {
        DWORD dwRet = 0;

        RequestData req = *pReq; // Make a copy because the const data can't be changed.

        dwRet = this->WritePipe((const BYTE *)&id, sizeof(RequestId));
        if (dwRet != 0)
        {
            return dwRet;
        }

        BYTE* sValues[MAX_REQUEST_FIELDS] = {0};
        for (int i = 0; i < MAX_REQUEST_FIELDS; i++)
        {
            if (req.fields[i].vt == VT_LPWSTR)
            {
                sValues[i] = (BYTE*) req.fields[i].szValue;
                req.fields[i].cchValue = (DWORD) wcslen(req.fields[i].szValue);
            }
            else if (req.fields[i].vt == VT_STREAM)
            {
                sValues[i] = req.fields[i].sValue;
                req.fields[i].cbValue = (DWORD) req.fields[i + 1].uiValue;
            }
        }

        dwRet = this->WritePipe((const BYTE *)&req, sizeof(RequestData));
        if (dwRet != 0)
        {
            return dwRet;
        }

        for (int i = 0; i < MAX_REQUEST_FIELDS; i++)
        {
            if (sValues[i] != NULL)
            {
                DWORD cbValue;
                if (req.fields[i].vt == VT_LPWSTR)
                {
                    cbValue = (req.fields[i].cchValue + 1) * sizeof(WCHAR);
                }
                else
                {
                    cbValue = req.fields[i].cbValue;
                }

                dwRet = this->WritePipe(const_cast<BYTE*> (sValues[i]), cbValue);
                if (dwRet != 0)
                {
                    break;
                }
            }
        }

        return dwRet;
    }

    //
    // Called when writing a request or response. Writes data to
    // the pipe, allowing interruption by the stop event if on the server.
    //
    DWORD WritePipe(const BYTE* pBuf, DWORD cbWrite)
    {
        DWORD dwRet = 0;
        DWORD dwTotalBytesWritten = 0;

        while (dwRet == 0 && dwTotalBytesWritten < cbWrite)
        {
            DWORD dwBytesWrittenThisTime;
            ResetEvent(m_overlapped.hEvent);
            if (!WriteFile(m_hPipe, pBuf + dwTotalBytesWritten, cbWrite - dwTotalBytesWritten, &dwBytesWrittenThisTime, &m_overlapped))
            {
                dwRet = GetLastError();
                if (dwRet == ERROR_IO_PENDING)
                {
                    if (m_fServer)
                    {
                        HANDLE hWaitHandles[] = { m_overlapped.hEvent, m_hReceiveStopEvent };
                        dwRet = WaitForMultipleObjects(2, hWaitHandles, FALSE, INFINITE);
                    }
                    else
                    {
                        dwRet = WaitForSingleObject(m_overlapped.hEvent, INFINITE);
                    }

                    if (dwRet == WAIT_OBJECT_0)
                    {
                        if (!GetOverlappedResult(m_hPipe, &m_overlapped, &dwBytesWrittenThisTime, FALSE))
                        {
                            dwRet = GetLastError();
                        }
                    }
                    else if (dwRet == WAIT_FAILED)
                    {
                        dwRet = GetLastError();
                    }
                    else
                    {
                        dwRet = ERROR_OPERATION_ABORTED;
                    }
                }
            }

            dwTotalBytesWritten += dwBytesWrittenThisTime;
        }

        return dwRet;
    }

    //
    // Called either by the client or server process.
    // Reads data from the pipe for a request or response.
    //
    DWORD ReadRequestData(RequestData* pReq)
    {
        DWORD dwRet = ReadPipe((BYTE*) pReq, sizeof(RequestData));

        if (dwRet == 0)
        {
            DWORD cbData = 0;
            for (int i = 0; i < MAX_REQUEST_FIELDS; i++)
            {
                if (pReq->fields[i].vt == VT_LPWSTR)
                {
                    cbData += (pReq->fields[i].cchValue + 1) * sizeof(WCHAR);
                }
                else if (pReq->fields[i].vt == VT_STREAM)
                {
                    cbData += pReq->fields[i].cbValue;
                }
            }
            
            if (cbData > 0)
            {
                if (!CheckRequestDataBuf(cbData))
                {
                    return ERROR_OUTOFMEMORY;
                }

                dwRet = this->ReadPipe((BYTE*) m_pBufReceive, cbData);
                if (dwRet == 0)
                {
                    DWORD dwOffset = 0;
                    for (int i = 0; i < MAX_REQUEST_FIELDS; i++)
                    {
                        if (pReq->fields[i].vt == VT_LPWSTR)
                        {
                            LPWSTR szTemp = (LPWSTR) (m_pBufReceive + dwOffset);
                            dwOffset += (pReq->fields[i].cchValue + 1) * sizeof(WCHAR);
                            pReq->fields[i].szValue = szTemp;
                        }
                        else if (pReq->fields[i].vt == VT_STREAM)
                        {
                            BYTE* sTemp = m_pBufReceive + dwOffset;
                            dwOffset += pReq->fields[i].cbValue;
                            pReq->fields[i].sValue = sTemp;
                        }
                    }
                }
            }
        }

        return dwRet;
    }

    //
    // Called only by the client process.
    // Wait for a response on the pipe. If no response is received before the timeout,
    // then give up and close the connection.
    //
    DWORD ReceiveResponse(RequestId id, RequestData* pResp)
    {
        RequestId responseId;
        DWORD dwRet = ReadPipe((BYTE*) &responseId, sizeof(RequestId));
        if (dwRet == 0 && responseId != id)
        {
            dwRet = ERROR_OPERATION_ABORTED;
        }

        if (dwRet == 0)
        {
            dwRet = this->ReadRequestData(pResp);
        }

        return dwRet;
    }

    //
    // Called only by the server process's receive thread.
    // Try to complete and verify an asynchronous connection operation.
    //
    DWORD CompleteConnection()
    {
        DWORD dwRet = 0;
        if (m_fConnecting)
        {
            HANDLE hWaitHandles[] = { m_overlapped.hEvent, m_hReceiveStopEvent };
            DWORD dwWaitRes = WaitForMultipleObjects(2, hWaitHandles, FALSE, INFINITE);

            if (dwWaitRes == WAIT_OBJECT_0)
            {
                m_fConnecting = false;

                DWORD dwUnused;
                if (GetOverlappedResult(m_hPipe, &m_overlapped, &dwUnused, FALSE))
                {
                    m_fConnected = true;
                }
                else
                {
                    dwRet = GetLastError();
                }
            }
            else if (dwWaitRes == WAIT_FAILED)
            {
                CancelIo(m_hPipe);
                dwRet = GetLastError();
            }
            else
            {
                CancelIo(m_hPipe);
                dwRet = ERROR_OPERATION_ABORTED;
            }
        }
        return dwRet;
    }

    //
    // Called only by the server process.
    // Creates a named pipe instance and begins asynchronously waiting
    // for a connection from the client process.
    //
    DWORD ConnectPipeServer()
    {
        DWORD dwRet = 0;
        const int BUFSIZE = 1024; // Suggested pipe I/O buffer sizes
        m_hPipe = CreateNamedPipe(
            m_szPipeName,
            PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED | FILE_FLAG_FIRST_PIPE_INSTANCE,
            PIPE_TYPE_BYTE | PIPE_READMODE_BYTE,
            1, BUFSIZE, BUFSIZE, 0, NULL);
        if (m_hPipe == INVALID_HANDLE_VALUE)
        {
            m_hPipe = NULL;
            dwRet = GetLastError();
        }
        else if (ConnectNamedPipe(m_hPipe, &m_overlapped))
        {
            m_fConnected = true;
        }
        else
        {
            dwRet = GetLastError();

            if (dwRet == ERROR_PIPE_BUSY)
            {
                // All pipe instances are busy, so wait for a maximum of 20 seconds 
                dwRet = 0;
                if (WaitNamedPipe(m_szPipeName, 20000))
                {
                    m_fConnected = true;
                }
                else
                {
                    dwRet = GetLastError();
                }
            }
            
            if (dwRet == ERROR_IO_PENDING)
            {
                dwRet = 0;
                m_fConnecting = true;
            }
        }
        return dwRet;
    }

    //
    // Called only by the client process.
    // Attemps to open a connection to an existing named pipe instance
    // which should have already been created by the server process.
    //
    DWORD ConnectPipeClient()
    {
        DWORD dwRet = 0;
        m_hPipe = CreateFile(
            m_szPipeName, GENERIC_READ | GENERIC_WRITE, 
            0, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);
        if (m_hPipe != INVALID_HANDLE_VALUE)
        {
            m_fConnected = true;
        }
        else
        {
            m_hPipe = NULL;
            dwRet = GetLastError();
        }
        return dwRet;
    }

    //
    // Ensures that the request buffer is large enough to hold a request,
    // reallocating the buffer if necessary.
    // It will also reduce the buffer size if the previous allocation was very large.
    //
    BOOL CheckRequestDataBuf(DWORD cbBuf)
    {
        if (m_cbBufReceive < cbBuf || (LARGE_BUFFER_THRESHOLD < m_cbBufReceive && cbBuf < m_cbBufReceive))
        {
            if (m_pBufReceive != NULL)
            {
                SecureZeroMemory(m_pBufReceive, m_cbBufReceive);
                delete[] m_pBufReceive;
            }
            m_cbBufReceive = max(MIN_BUFFER_STRING_SIZE*2, cbBuf);
            m_pBufReceive = new BYTE[m_cbBufReceive];
            if (m_pBufReceive == NULL)
            {
                m_cbBufReceive = 0;
            }
        }
        return m_pBufReceive != NULL;
    }

private:

    // Name of this instance. 
    const wchar_t* m_szName;

    // "\\.\pipe\name"
    wchar_t* m_szPipeName;
    
    // Handle to the pipe instance.
    HANDLE m_hPipe;

    // Handle to the thread that receives requests.
    HANDLE m_hReceiveThread;

    // Handle to the event used to signal the receive thread to exit.
    HANDLE m_hReceiveStopEvent;

    // All pipe I/O is done in overlapped mode to avoid unintentional blocking.
    OVERLAPPED m_overlapped;
    
    // Dynamically-resized buffer for receiving requests.
    BYTE* m_pBufReceive;

    // Current size of the receive request buffer.
    DWORD m_cbBufReceive;

    // Dynamically-resized buffer for sending requests.
    wchar_t* m_pBufSend;

    // Current size of the send request buffer.
    DWORD m_cbBufSend;

    // True if this is the server process, false if this is the client process.
    const bool m_fServer;

    // True if an asynchronous connection operation is currently in progress.
    bool m_fConnecting;

    // True if the pipe is currently connected.
    bool m_fConnected;
};
