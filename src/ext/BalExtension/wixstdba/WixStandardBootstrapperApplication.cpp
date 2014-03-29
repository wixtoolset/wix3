//-------------------------------------------------------------------------------------------------
// <copyright file="WixStandardBootstrapperApplication.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static const HRESULT E_WIXSTDBA_CONDITION_FAILED = MAKE_HRESULT(SEVERITY_ERROR, 500, 1);

static const LPCWSTR WIXBUNDLE_VARIABLE_ELEVATED = L"WixBundleElevated";

static const LPCWSTR WIXSTDBA_WINDOW_CLASS = L"WixStdBA";
static const LPCWSTR WIXSTDBA_VARIABLE_INSTALL_FOLDER = L"InstallFolder";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH = L"LaunchTarget";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS = L"LaunchArguments";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_HIDDEN = L"LaunchHidden";
static const LPCWSTR WIXSTDBA_VARIABLE_RUN_EXECUTABLE = L"RunExecutableVariable";
static const LPCWSTR WIXSTDBA_VARIABLE_RUN_EXECUTABLE_ERROR_MSG = L"VerifyRunExecutableErrorMsg";
static const LPCWSTR WIXSTDBA_VARIABLE_VERIFY_OFFICE_OPEN_ERROR_MSG = L"VerifyOfficeOpenMsg";
static const LPCWSTR WIXSTDBA_VARIABLE_VERIFY_CHECKBOX_ERROR_MSG = L"VerifyCheckBoxesErrorMsg";
static const LPCWSTR WIXSTDBA_VARIABLE_VERIFY_CLIENT_NOT_INSTALLING_IN_XP_ERROR_MSG = L"VerifyClientNotInstallingInXPErrorMsg";
static const DWORD WIXSTDBA_ACQUIRE_PERCENTAGE = 30;

#define MAX_PROCESSES 1024

enum WIXSTDBA_STATE
{
    WIXSTDBA_STATE_OPTIONS,
    WIXSTDBA_STATE_INITIALIZING,
    WIXSTDBA_STATE_INITIALIZED,
    WIXSTDBA_STATE_HELP,
    WIXSTDBA_STATE_DETECTING,
    WIXSTDBA_STATE_DETECTED,
    WIXSTDBA_STATE_PLANNING,
    WIXSTDBA_STATE_PLANNED,
    WIXSTDBA_STATE_APPLYING,
    WIXSTDBA_STATE_CACHING,
    WIXSTDBA_STATE_CACHED,
    WIXSTDBA_STATE_EXECUTING,
    WIXSTDBA_STATE_EXECUTED,
    WIXSTDBA_STATE_APPLIED,
    WIXSTDBA_STATE_FAILED,
};

enum WM_WIXSTDBA
{
    WM_WIXSTDBA_SHOW_HELP = WM_APP + 100,
    WM_WIXSTDBA_DETECT_PACKAGES,
    WM_WIXSTDBA_PLAN_PACKAGES,
    WM_WIXSTDBA_APPLY_PACKAGES,
    WM_WIXSTDBA_CHANGE_STATE,
};

// This enum must be kept in the same order as the vrgwzPageNames array.
enum WIXSTDBA_PAGE
{
    WIXSTDBA_PAGE_LOADING,
    WIXSTDBA_PAGE_HELP,
    WIXSTDBA_PAGE_INSTALL,
    WIXSTDBA_PAGE_OPTIONS,
    WIXSTDBA_PAGE_MODIFY,
    WIXSTDBA_PAGE_PROGRESS,
    WIXSTDBA_PAGE_PROGRESS_PASSIVE,
    WIXSTDBA_PAGE_SUCCESS,
    WIXSTDBA_PAGE_FAILURE,
    COUNT_WIXSTDBA_PAGE,
};

// This array must be kept in the same order as the WIXSTDBA_PAGE enum.
static LPCWSTR vrgwzPageNames[] = {
    L"Loading",
    L"Help",
    L"Install",
    L"Options",
    L"Modify",
    L"Progress",
    L"ProgressPassive",
    L"Success",
    L"Failure",
};

enum WIXSTDBA_CONTROL
{
    // Non-paged controls
    WIXSTDBA_CONTROL_CLOSE_BUTTON = THEME_FIRST_ASSIGN_CONTROL_ID,
    WIXSTDBA_CONTROL_MINIMIZE_BUTTON,

    // Help page
    WIXSTDBA_CONTROL_HELP_CANCEL_BUTTON,

    // Welcome page
    WIXSTDBA_CONTROL_INSTALL_BUTTON,
    WIXSTDBA_CONTROL_OPTIONS_BUTTON,
    WIXSTDBA_CONTROL_EULA_RICHEDIT,
    WIXSTDBA_CONTROL_EULA_LINK,
    WIXSTDBA_CONTROL_CHECKBOX_CLIENT1,
    WIXSTDBA_CONTROL_CHECKBOX_CLIENT2,
    WIXSTDBA_CONTROL_CHECKBOX_CLIENT3,
    WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX,
    WIXSTDBA_CONTROL_WELCOME_CANCEL_BUTTON,
    WIXSTDBA_CONTROL_VERSION_LABEL,

    // Options page
    WIXSTDBA_CONTROL_FOLDER_EDITBOX,
    WIXSTDBA_CONTROL_BROWSE_BUTTON,
    WIXSTDBA_CONTROL_OK_BUTTON,
    WIXSTDBA_CONTROL_CANCEL_BUTTON,

    // Modify page
    WIXSTDBA_CONTROL_REPAIR_BUTTON,
    WIXSTDBA_CONTROL_UNINSTALL_BUTTON,
    WIXSTDBA_CONTROL_MODIFY_CANCEL_BUTTON,

    // Progress page
    WIXSTDBA_CONTROL_CACHE_PROGRESS_PACKAGE_TEXT,
    WIXSTDBA_CONTROL_CACHE_PROGRESS_BAR,
    WIXSTDBA_CONTROL_CACHE_PROGRESS_TEXT,

    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_PACKAGE_TEXT,
    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_BAR,
    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_TEXT,
    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_ACTIONDATA_TEXT,

    WIXSTDBA_CONTROL_OVERALL_PROGRESS_PACKAGE_TEXT,
    WIXSTDBA_CONTROL_OVERALL_PROGRESS_BAR,
    WIXSTDBA_CONTROL_OVERALL_CALCULATED_PROGRESS_BAR,
    WIXSTDBA_CONTROL_OVERALL_PROGRESS_TEXT,

    WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON,

    // Success page
    WIXSTDBA_CONTROL_SUCCESS_HEADER,
    WIXSTDBA_CONTROL_SUCCESS_SETUP_HEADER,
    WIXSTDBA_CONTROL_SUCCESS_UNINSTALL_HEADER,
    WIXSTDBA_CONTROL_LAUNCH_BUTTON,
    WIXSTDBA_CONTROL_SUCCESS_RESTART_TEXT,
    WIXSTDBA_CONTROL_SUCCESS_RESTART_BUTTON,
    WIXSTDBA_CONTROL_SUCCESS_CANCEL_BUTTON,

    // Failure page
    WIXSTDBA_CONTROL_FAILURE_HEADER,
    WIXSTDBA_CONTROL_FAILURE_SETUP_HEADER,
    WIXSTDBA_CONTROL_FAILURE_UNINSTALL_HEADER,
    WIXSTDBA_CONTROL_FAILURE_LOGFILE_LINK,
    WIXSTDBA_CONTROL_FAILURE_MESSAGE_TEXT,
    WIXSTDBA_CONTROL_FAILURE_RESTART_TEXT,
    WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON,
    WIXSTDBA_CONTROL_FAILURE_CANCEL_BUTTON,
};

static THEME_ASSIGN_CONTROL_ID vrgInitControls[] = {
    { WIXSTDBA_CONTROL_CLOSE_BUTTON, L"CloseButton" },
    { WIXSTDBA_CONTROL_MINIMIZE_BUTTON, L"MinimizeButton" },

    { WIXSTDBA_CONTROL_HELP_CANCEL_BUTTON, L"HelpCancelButton" },

    { WIXSTDBA_CONTROL_INSTALL_BUTTON, L"InstallButton" },
    { WIXSTDBA_CONTROL_OPTIONS_BUTTON, L"OptionsButton" },
    { WIXSTDBA_CONTROL_EULA_RICHEDIT, L"EulaRichedit" },
    { WIXSTDBA_CONTROL_EULA_LINK, L"EulaHyperlink" },
    { WIXSTDBA_CONTROL_CHECKBOX_CLIENT1, L"InstallClient1Checkbox" },
    { WIXSTDBA_CONTROL_CHECKBOX_CLIENT2, L"InstallClient2Checkbox" },
    { WIXSTDBA_CONTROL_CHECKBOX_CLIENT3, L"InstallClient3Checkbox" },
    { WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX, L"EulaAcceptCheckbox" },
    { WIXSTDBA_CONTROL_WELCOME_CANCEL_BUTTON, L"WelcomeCancelButton" },
    { WIXSTDBA_CONTROL_VERSION_LABEL, L"InstallVersion" },

    { WIXSTDBA_CONTROL_FOLDER_EDITBOX, L"FolderEditbox" },
    { WIXSTDBA_CONTROL_BROWSE_BUTTON, L"BrowseButton" },
    { WIXSTDBA_CONTROL_OK_BUTTON, L"OptionsOkButton" },
    { WIXSTDBA_CONTROL_CANCEL_BUTTON, L"OptionsCancelButton" },

    { WIXSTDBA_CONTROL_REPAIR_BUTTON, L"RepairButton" },
    { WIXSTDBA_CONTROL_UNINSTALL_BUTTON, L"UninstallButton" },
    { WIXSTDBA_CONTROL_MODIFY_CANCEL_BUTTON, L"ModifyCancelButton" },

    { WIXSTDBA_CONTROL_CACHE_PROGRESS_PACKAGE_TEXT, L"CacheProgressPackageText" },
    { WIXSTDBA_CONTROL_CACHE_PROGRESS_BAR, L"CacheProgressbar" },
    { WIXSTDBA_CONTROL_CACHE_PROGRESS_TEXT, L"CacheProgressText" },
    { WIXSTDBA_CONTROL_EXECUTE_PROGRESS_PACKAGE_TEXT, L"ExecuteProgressPackageText" },
    { WIXSTDBA_CONTROL_EXECUTE_PROGRESS_BAR, L"ExecuteProgressbar" },
    { WIXSTDBA_CONTROL_EXECUTE_PROGRESS_TEXT, L"ExecuteProgressText" },
    { WIXSTDBA_CONTROL_EXECUTE_PROGRESS_ACTIONDATA_TEXT, L"ExecuteProgressActionDataText"},
    { WIXSTDBA_CONTROL_OVERALL_PROGRESS_PACKAGE_TEXT, L"OverallProgressPackageText" },
    { WIXSTDBA_CONTROL_OVERALL_PROGRESS_BAR, L"OverallProgressbar" },
    { WIXSTDBA_CONTROL_OVERALL_CALCULATED_PROGRESS_BAR, L"OverallCalculatedProgressbar" },
    { WIXSTDBA_CONTROL_OVERALL_PROGRESS_TEXT, L"OverallProgressText" },
    { WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON, L"ProgressCancelButton" },

    { WIXSTDBA_CONTROL_SUCCESS_HEADER, L"SuccessHeader" },
    { WIXSTDBA_CONTROL_SUCCESS_SETUP_HEADER, L"SuccessSetupHeader" },
    { WIXSTDBA_CONTROL_SUCCESS_UNINSTALL_HEADER, L"SuccessUninstallHeader" },
    { WIXSTDBA_CONTROL_LAUNCH_BUTTON, L"LaunchButton" },
    { WIXSTDBA_CONTROL_SUCCESS_RESTART_TEXT, L"SuccessRestartText" },
    { WIXSTDBA_CONTROL_SUCCESS_RESTART_BUTTON, L"SuccessRestartButton" },
    { WIXSTDBA_CONTROL_SUCCESS_CANCEL_BUTTON, L"SuccessCancelButton" },

    { WIXSTDBA_CONTROL_FAILURE_HEADER, L"FailureHeader" },
    { WIXSTDBA_CONTROL_FAILURE_SETUP_HEADER, L"FailureSetupHeader" },
    { WIXSTDBA_CONTROL_FAILURE_UNINSTALL_HEADER, L"FailureUninstallHeader" },
    { WIXSTDBA_CONTROL_FAILURE_LOGFILE_LINK, L"FailureLogFileLink" },
    { WIXSTDBA_CONTROL_FAILURE_MESSAGE_TEXT, L"FailureMessageText" },
    { WIXSTDBA_CONTROL_FAILURE_RESTART_TEXT, L"FailureRestartText" },
    { WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON, L"FailureRestartButton" },
    { WIXSTDBA_CONTROL_FAILURE_CANCEL_BUTTON, L"FailureCloseButton" },
};

class CWixStandardBootstrapperApplication : public CBalBaseBootstrapperApplication
{
public: // IBootstrapperApplication
    virtual STDMETHODIMP OnStartup()
    {
        HRESULT hr = S_OK;
        DWORD dwUIThreadId = 0;

        // create UI thread
        m_hUiThread = ::CreateThread(NULL, 0, UiThreadProc, this, 0, &dwUIThreadId);
        if (!m_hUiThread)
        {
            ExitWithLastError(hr, "Failed to create UI thread.");
        }

    LExit:
        return hr;
    }


    virtual STDMETHODIMP_(int) OnShutdown()
    {
        int nResult = IDNOACTION;

        // wait for UI thread to terminate
        if (m_hUiThread)
        {
            ::WaitForSingleObject(m_hUiThread, INFINITE);
            ReleaseHandle(m_hUiThread);
        }

        // If a restart was required.
        if (m_fRestartRequired)
        {
            if (m_fAllowRestart)
            {
                nResult = IDRESTART;
            }

            if (m_sczPrereqPackage)
            {
                BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, m_fAllowRestart ? "The prerequisites scheduled a restart. The bootstrapper application will be reloaded after the computer is restarted."
                                                                        : "A restart is required by the prerequisites but the user delayed it. The bootstrapper application will be reloaded after the computer is restarted.");
            }
        }
        else if (m_fPrereqInstalled)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "The prerequisites were successfully installed. The bootstrapper application will be reloaded.");
            nResult = IDRELOAD_BOOTSTRAPPER;
        }
        else if (m_fPrereqAlreadyInstalled)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "The prerequisites were already installed. The bootstrapper application will not be reloaded to prevent an infinite loop.");
        }
        else if (m_fPrereq)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "The prerequisites were not successfully installed, error: 0x%x. The bootstrapper application will be not reloaded.", m_hrFinal);
        }

        return nResult;
    }


    virtual STDMETHODIMP_(int) OnDetectRelatedBundle(
        __in LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in LPCWSTR /*wzBundleTag*/,
        __in BOOL fPerMachine,
        __in DWORD64 /*dw64Version*/,
        __in BOOTSTRAPPER_RELATED_OPERATION operation
        )
    {
        BalInfoAddRelatedBundleAsPackage(&m_Bundle.packages, wzBundleId, relationType, fPerMachine);

        // If we're not doing a prerequisite install, remember when our bundle would cause a downgrade.
        if (!m_sczPrereqPackage && BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE == operation)
        {
            m_fDowngrading = TRUE;
        }

        return CheckCanceled() ? IDCANCEL : IDOK;
    }


    virtual STDMETHODIMP_(void) OnDetectPackageComplete(
        __in LPCWSTR wzPackageId,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_PACKAGE_STATE state
        )
    {
        // If the prerequisite package is already installed, remember that.
        if (m_sczPrereqPackage && BOOTSTRAPPER_PACKAGE_STATE_PRESENT == state &&
            CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPackageId, -1, m_sczPrereqPackage, -1))
        {
            m_fPrereqAlreadyInstalled = TRUE;
        }
    }


    virtual STDMETHODIMP_(void) OnDetectComplete(
        __in HRESULT hrStatus
        )
    {
        if (SUCCEEDED(hrStatus) && m_pBAFunction)
        {
            m_pBAFunction->OnDetectComplete();
        }

        if (SUCCEEDED(hrStatus))
        {
            hrStatus = EvaluateConditions();
        }

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running detect complete BA function");

        SetState(WIXSTDBA_STATE_DETECTED, hrStatus);

        // If we're not interacting with the user or we're doing a layout or we're just after a force restart
        // then automatically start planning.
        if (BOOTSTRAPPER_DISPLAY_FULL > m_command.display || BOOTSTRAPPER_ACTION_LAYOUT == m_command.action || BOOTSTRAPPER_RESUME_TYPE_REBOOT == m_command.resumeType)
        {
            if (SUCCEEDED(hrStatus))
            {
                ::PostMessageW(m_hWnd, WM_WIXSTDBA_PLAN_PACKAGES, 0, m_command.action);
            }
        }
    }


    virtual STDMETHODIMP_(int) OnPlanRelatedBundle(
        __in_z LPCWSTR /*wzBundleId*/,
        __inout_z BOOTSTRAPPER_REQUEST_STATE* pRequestedState
        )
    {
        // If we're only installing prereq, do not touch related bundles.
        if (m_sczPrereqPackage)
        {
            *pRequestedState = BOOTSTRAPPER_REQUEST_STATE_NONE;
        }

        return CheckCanceled() ? IDCANCEL : IDOK;
    }


    virtual STDMETHODIMP_(int) OnPlanPackageBegin(
        __in_z LPCWSTR wzPackageId,
        __inout BOOTSTRAPPER_REQUEST_STATE *pRequestState
        )
    {
        // If we're planning to install a pre-req, install it. The pre-req needs to be installed
        // in all cases (even uninstall!) so the BA can load next.
        if (m_sczPrereqPackage)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPackageId, -1, m_sczPrereqPackage, -1))
            {
                *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            }
            else // skip everything else.
            {
                *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
            }
        }
        else if (m_sczAfterForcedRestartPackage) // after force restart skip packages until after the package that caused the restart.
        {
            // After restart we need to finish the dependency registration for our package so allow the package
            // to go present.
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPackageId, -1, m_sczAfterForcedRestartPackage, -1))
            {
                // Do not allow a repair because that could put us in a perpetual restart loop.
                if (BOOTSTRAPPER_REQUEST_STATE_REPAIR == *pRequestState)
                {
                    *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
                }

                ReleaseNullStr(m_sczAfterForcedRestartPackage); // no more skipping now.
            }
            else // not the matching package, so skip it.
            {
                BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Skipping package: %ls, after restart because it was applied before the restart.", wzPackageId);

                *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
            }
        }

        return CheckCanceled() ? IDCANCEL : IDOK;
    }


    virtual STDMETHODIMP_(void) OnPlanComplete(
        __in HRESULT hrStatus
        )
    {
        if (SUCCEEDED(hrStatus) && m_pBAFunction)
        {
            m_pBAFunction->OnPlanComplete();
        }

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running plan complete BA function");

        SetState(WIXSTDBA_STATE_PLANNED, hrStatus);

        if (SUCCEEDED(hrStatus))
        {
            ::PostMessageW(m_hWnd, WM_WIXSTDBA_APPLY_PACKAGES, 0, 0);
        }

        m_fStartedExecution = FALSE;
        m_dwCalculatedCacheProgress = 0;
        m_dwCalculatedExecuteProgress = 0;
    }


    virtual STDMETHODIMP_(int) OnCachePackageBegin(
        __in_z LPCWSTR wzPackageId,
        __in DWORD cCachePayloads,
        __in DWORD64 dw64PackageCacheSize
        )
    {
        if (wzPackageId && *wzPackageId)
        {
            BAL_INFO_PACKAGE* pPackage = NULL;
            HRESULT hr = BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);
            LPCWSTR wz = (SUCCEEDED(hr) && pPackage->sczDisplayName) ? pPackage->sczDisplayName : wzPackageId;

            ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_CACHE_PROGRESS_PACKAGE_TEXT, wz);

            // If something started executing, leave it in the overall progress text.
            if (!m_fStartedExecution)
            {
                ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_OVERALL_PROGRESS_PACKAGE_TEXT, wz);
            }
        }

        return __super::OnCachePackageBegin(wzPackageId, cCachePayloads, dw64PackageCacheSize);
    }


    virtual STDMETHODIMP_(int) OnCacheAcquireProgress(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage
        )
    {
        WCHAR wzProgress[5] = { };

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnCacheAcquireProgress() - container/package: %ls, payload: %ls, progress: %I64u, total: %I64u, overall progress: %u%%", wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage);
#endif

        ::StringCchPrintfW(wzProgress, countof(wzProgress), L"%u%%", dwOverallPercentage);
        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_CACHE_PROGRESS_TEXT, wzProgress);

        ThemeSetProgressControl(m_pTheme, WIXSTDBA_CONTROL_CACHE_PROGRESS_BAR, dwOverallPercentage);

        m_dwCalculatedCacheProgress = dwOverallPercentage * WIXSTDBA_ACQUIRE_PERCENTAGE / 100;
        ThemeSetProgressControl(m_pTheme, WIXSTDBA_CONTROL_OVERALL_CALCULATED_PROGRESS_BAR, m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);

        SetTaskbarButtonProgress(m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);

        return __super::OnCacheAcquireProgress(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage);
    }


    virtual STDMETHODIMP_(int) OnCacheAcquireComplete(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in int nRecommendation
        )
    {
        SetProgressState(hrStatus);
        return __super::OnCacheAcquireComplete(wzPackageOrContainerId, wzPayloadId, hrStatus, nRecommendation);
    }


    virtual STDMETHODIMP_(int) OnCacheVerifyComplete(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in int nRecommendation
        )
    {
        SetProgressState(hrStatus);
        return __super::OnCacheVerifyComplete(wzPackageId, wzPayloadId, hrStatus, nRecommendation);
    }


    virtual STDMETHODIMP_(void) OnCacheComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_CACHE_PROGRESS_PACKAGE_TEXT, L"");
        SetState(WIXSTDBA_STATE_CACHED, S_OK); // we always return success here and let OnApplyComplete() deal with the error.
    }


    virtual STDMETHODIMP_(int) OnError(
        __in BOOTSTRAPPER_ERROR_TYPE errorType,
        __in LPCWSTR wzPackageId,
        __in DWORD dwCode,
        __in_z LPCWSTR wzError,
        __in DWORD dwUIHint,
        __in DWORD /*cData*/,
        __in_ecount_z_opt(cData) LPCWSTR* /*rgwzData*/,
        __in int nRecommendation
        )
    {
        int nResult = nRecommendation;
        LPWSTR sczError = NULL;

        if (BOOTSTRAPPER_DISPLAY_EMBEDDED == m_command.display)
        {
             HRESULT hr = m_pEngine->SendEmbeddedError(dwCode, wzError, dwUIHint, &nResult);
             if (FAILED(hr))
             {
                 nResult = IDERROR;
             }
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL == m_command.display)
        {
            // If this is an authentication failure, let the engine try to handle it for us.
            if (BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_SERVER == errorType || BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_PROXY == errorType)
            {
                nResult = IDTRYAGAIN;
            }
            else // show a generic error message box.
            {
                BalRetryErrorOccurred(wzPackageId, dwCode);

                if (!m_fShowingInternalUiThisPackage)
                {
                    // If no error message was provided, use the error code to try and get an error message.
                    if (!wzError || !*wzError || BOOTSTRAPPER_ERROR_TYPE_WINDOWS_INSTALLER != errorType)
                    {
                        HRESULT hr = StrAllocFromError(&sczError, dwCode, NULL);
                        if (FAILED(hr) || !sczError || !*sczError)
                        {
                            StrAllocFormatted(&sczError, L"0x%x", dwCode);
                        }
                    }

                    nResult = ::MessageBoxW(m_hWnd, sczError ? sczError : wzError, m_pTheme->sczCaption, dwUIHint);
                }
            }

            SetProgressState(HRESULT_FROM_WIN32(dwCode));
        }
        else // just take note of the error code and let things continue.
        {
            BalRetryErrorOccurred(wzPackageId, dwCode);
        }

        ReleaseStr(sczError);
        return nResult;
    }


    virtual STDMETHODIMP_(int) OnExecuteMsiMessage(
        __in_z LPCWSTR wzPackageId,
        __in INSTALLMESSAGE mt,
        __in UINT uiFlags,
        __in_z LPCWSTR wzMessage,
        __in DWORD cData,
        __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
        __in int nRecommendation
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnExecuteMsiMessage() - package: %ls, message: %ls", wzPackageId, wzMessage);
#endif
        if (BOOTSTRAPPER_DISPLAY_FULL == m_command.display && (INSTALLMESSAGE_WARNING == mt || INSTALLMESSAGE_USER == mt))
        {
            int nResult = ::MessageBoxW(m_hWnd, wzMessage, m_pTheme->sczCaption, uiFlags);
            return nResult;
        }

        if (INSTALLMESSAGE_ACTIONSTART == mt)
        {
            // Need to trim the action data text
            CString tempString = wzMessage;
            size_t strLength = wcslen(tempString);
            // Find the first '.' and removes text to the left of it i.e.: 'Action <time>: CustomAction name. progress text' to just 'progress text'
            size_t strPosition = tempString.Find('.', 0);
            // Check to see if there is another '.'
            // Find the second '.' and removes text to the left of it i.e.: 'Action <time>: MergeModueGUID. progress text' to just 'progress text'
            if (strPosition != -1)
            {
                size_t nextPosition = tempString.Find('.', strPosition + 1);
                // Make sure it found something before using its position
                if (nextPosition != -1)
                {
                    strPosition = nextPosition;
                }
            }

            // Need to add + 2 for position since it will still show the '. '
            CString trimString = tempString.Right(strLength - (strPosition + 2));

            // Use the CString trimString instead of wzMessage, leave wzMessage alone
            ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_EXECUTE_PROGRESS_ACTIONDATA_TEXT, trimString);
        }

        return __super::OnExecuteMsiMessage(wzPackageId, mt, uiFlags, wzMessage, cData, rgwzData, nRecommendation);
    }


    virtual STDMETHODIMP_(int) OnProgress(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage
        )
    {
        WCHAR wzProgress[5] = { };

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnProgress() - progress: %u%%, overall progress: %u%%", dwProgressPercentage, dwOverallProgressPercentage);
#endif

        ::StringCchPrintfW(wzProgress, countof(wzProgress), L"%u%%", dwOverallProgressPercentage);
        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_OVERALL_PROGRESS_TEXT, wzProgress);

        ThemeSetProgressControl(m_pTheme, WIXSTDBA_CONTROL_OVERALL_PROGRESS_BAR, dwOverallProgressPercentage);
        SetTaskbarButtonProgress(dwOverallProgressPercentage);

        return __super::OnProgress(dwProgressPercentage, dwOverallProgressPercentage);
    }


    virtual STDMETHODIMP_(int) OnExecutePackageBegin(
        __in_z LPCWSTR wzPackageId,
        __in BOOL fExecute
        )
    {
        LPWSTR sczFormattedString = NULL;

        m_fStartedExecution = TRUE;

        if (wzPackageId && *wzPackageId)
        {
            BAL_INFO_PACKAGE* pPackage = NULL;
            BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);

            LPCWSTR wz = wzPackageId;
            if (pPackage)
            {
                LOC_STRING* pLocString = NULL;

                switch (pPackage->type)
                {
                case BAL_INFO_PACKAGE_TYPE_BUNDLE_ADDON:
                    LocGetString(m_pWixLoc, L"#(loc.ExecuteAddonRelatedBundleMessage)", &pLocString);
                    break;

                case BAL_INFO_PACKAGE_TYPE_BUNDLE_PATCH:
                    LocGetString(m_pWixLoc, L"#(loc.ExecutePatchRelatedBundleMessage)", &pLocString);
                    break;

                case BAL_INFO_PACKAGE_TYPE_BUNDLE_UPGRADE:
                    LocGetString(m_pWixLoc, L"#(loc.ExecuteUpgradeRelatedBundleMessage)", &pLocString);
                    break;
                }

                if (pLocString)
                {
                    BalFormatString(pLocString->wzText, &sczFormattedString);
                }

                wz = sczFormattedString ? sczFormattedString : pPackage->sczDisplayName ? pPackage->sczDisplayName : wzPackageId;
            }

            m_fShowingInternalUiThisPackage = pPackage && pPackage->fDisplayInternalUI;

            ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_EXECUTE_PROGRESS_PACKAGE_TEXT, wz);
            ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_OVERALL_PROGRESS_PACKAGE_TEXT, wz);
        }
        else
        {
            m_fShowingInternalUiThisPackage = FALSE;
        }

        ReleaseStr(sczFormattedString);
        return __super::OnExecutePackageBegin(wzPackageId, fExecute);
    }


    virtual int __stdcall OnExecuteProgress(
        __in_z LPCWSTR wzPackageId,
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage
        )
    {
        WCHAR wzProgress[5] = { };

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnExecuteProgress() - package: %ls, progress: %u%%, overall progress: %u%%", wzPackageId, dwProgressPercentage, dwOverallProgressPercentage);
#endif

        ::StringCchPrintfW(wzProgress, countof(wzProgress), L"%u%%", dwOverallProgressPercentage);
        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_EXECUTE_PROGRESS_TEXT, wzProgress);

        ThemeSetProgressControl(m_pTheme, WIXSTDBA_CONTROL_EXECUTE_PROGRESS_BAR, dwOverallProgressPercentage);

        m_dwCalculatedExecuteProgress = dwOverallProgressPercentage * (100 - WIXSTDBA_ACQUIRE_PERCENTAGE) / 100;
        ThemeSetProgressControl(m_pTheme, WIXSTDBA_CONTROL_OVERALL_CALCULATED_PROGRESS_BAR, m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);

        SetTaskbarButtonProgress(m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);

        return __super::OnExecuteProgress(wzPackageId, dwProgressPercentage, dwOverallProgressPercentage);
    }


    virtual STDMETHODIMP_(int) OnExecutePackageComplete(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrExitCode,
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in int nRecommendation
        )
    {
        SetProgressState(hrExitCode);

        int nResult = __super::OnExecutePackageComplete(wzPackageId, hrExitCode, restart, nRecommendation);

        if (m_sczPrereqPackage && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPackageId, -1, m_sczPrereqPackage, -1))
        {
            m_fPrereqInstalled = SUCCEEDED(hrExitCode);

            // If the pre-req required a restart (any restart) then do an immediate
            // restart to ensure that the bundle will get launched again post reboot.
            if (BOOTSTRAPPER_APPLY_RESTART_NONE != restart)
            {
                nResult = IDRESTART;
            }
        }

        return nResult;
    }


    virtual STDMETHODIMP_(void) OnExecuteComplete(
        __in HRESULT hrStatus
        )
    {
        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_EXECUTE_PROGRESS_PACKAGE_TEXT, L"");
        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_EXECUTE_PROGRESS_ACTIONDATA_TEXT, L"");
        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_OVERALL_PROGRESS_PACKAGE_TEXT, L"");
        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON, FALSE); // no more cancel.

        SetState(WIXSTDBA_STATE_EXECUTED, S_OK); // we always return success here and let OnApplyComplete() deal with the error.
        SetProgressState(hrStatus);
    }


    virtual STDMETHODIMP_(int) OnResolveSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzLocalSource,
        __in_z_opt LPCWSTR wzDownloadSource
        )
    {
        int nResult = IDERROR; // assume we won't resolve source and that is unexpected.

        if (BOOTSTRAPPER_DISPLAY_FULL == m_command.display)
        {
            if (wzDownloadSource)
            {
                nResult = IDDOWNLOAD;
            }
            else // prompt to change the source location.
            {
                OPENFILENAMEW ofn = { };
                WCHAR wzFile[MAX_PATH] = { };

                ::StringCchCopyW(wzFile, countof(wzFile), wzLocalSource);

                ofn.lStructSize = sizeof(ofn);
                ofn.hwndOwner = m_hWnd;
                ofn.lpstrFile = wzFile;
                ofn.nMaxFile = countof(wzFile);
                ofn.lpstrFilter = L"All Files\0*.*\0";
                ofn.nFilterIndex = 1;
                ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
                ofn.lpstrTitle = m_pTheme->sczCaption;

                if (::GetOpenFileNameW(&ofn))
                {
                    HRESULT hr = m_pEngine->SetLocalSource(wzPackageOrContainerId, wzPayloadId, ofn.lpstrFile);
                    nResult = SUCCEEDED(hr) ? IDRETRY : IDERROR;
                }
                else
                {
                    nResult = IDCANCEL;
                }
            }
        }
        else if (wzDownloadSource)
        {
            // If doing a non-interactive install and download source is available, let's try downloading the package silently
            nResult = IDDOWNLOAD;
        }
        // else there's nothing more we can do in non-interactive mode

        return CheckCanceled() ? IDCANCEL : nResult;
    }


    virtual STDMETHODIMP_(int) OnApplyComplete(
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart
        )
    {
        m_restartResult = restart; // remember the restart result so we return the correct error code no matter what the user chooses to do in the UI.

        // If a restart was encountered and we are not suppressing restarts, then restart is required.
        m_fRestartRequired = (BOOTSTRAPPER_APPLY_RESTART_NONE != restart && BOOTSTRAPPER_RESTART_NEVER < m_command.restart);
        // If a restart is required and we're not displaying a UI or we are not supposed to prompt for restart then allow the restart.
        m_fAllowRestart = m_fRestartRequired && (BOOTSTRAPPER_DISPLAY_FULL > m_command.display || BOOTSTRAPPER_RESTART_PROMPT < m_command.restart);

        // If we are showing UI, wait a beat before moving to the final screen.
        if (BOOTSTRAPPER_DISPLAY_NONE < m_command.display)
        {
            ::Sleep(250);
        }

        SetState(WIXSTDBA_STATE_APPLIED, hrStatus);
        SetTaskbarButtonProgress(100); // show full progress bar, green, yellow, or red

        return IDNOACTION;
    }


private: // privates
    //
    // UiThreadProc - entrypoint for UI thread.
    //
    static DWORD WINAPI UiThreadProc(
        __in LPVOID pvContext
        )
    {
        HRESULT hr = S_OK;
        CWixStandardBootstrapperApplication* pThis = (CWixStandardBootstrapperApplication*)pvContext;
        BOOL fComInitialized = FALSE;
        BOOL fRet = FALSE;
        MSG msg = { };

        // Initialize COM and theme.
        hr = ::CoInitialize(NULL);
        BalExitOnFailure(hr, "Failed to initialize COM.");
        fComInitialized = TRUE;

        hr = ThemeInitialize(pThis->m_hModule);
        BalExitOnFailure(hr, "Failed to initialize theme manager.");

        hr = pThis->InitializeData();
        BalExitOnFailure(hr, "Failed to initialize data in bootstrapper application.");

        // Create main window.
        pThis->InitializeTaskbarButton();
        hr = pThis->CreateMainWindow();
        BalExitOnFailure(hr, "Failed to create main window.");

        // Okay, we're ready for packages now.
        pThis->SetState(WIXSTDBA_STATE_INITIALIZED, hr);
        ::PostMessageW(pThis->m_hWnd, BOOTSTRAPPER_ACTION_HELP == pThis->m_command.action ? WM_WIXSTDBA_SHOW_HELP : WM_WIXSTDBA_DETECT_PACKAGES, 0, 0);

        // message pump
        while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
        {
            if (-1 == fRet)
            {
                hr = E_UNEXPECTED;
                BalExitOnFailure(hr, "Unexpected return value from message pump.");
            }
            else if (!ThemeHandleKeyboardMessage(pThis->m_pTheme, msg.hwnd, &msg))
            {
                ::TranslateMessage(&msg);
                ::DispatchMessageW(&msg);
            }
        }

        // Succeeded thus far, check to see if anything went wrong while actually
        // executing changes.
        if (FAILED(pThis->m_hrFinal))
        {
            hr = pThis->m_hrFinal;
        }
        else if (pThis->CheckCanceled())
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
        }

    LExit:
        // destroy main window
        pThis->DestroyMainWindow();

        // initiate engine shutdown
        DWORD dwQuit = HRESULT_CODE(hr);
        if (BOOTSTRAPPER_APPLY_RESTART_INITIATED == pThis->m_restartResult)
        {
            dwQuit = ERROR_SUCCESS_REBOOT_INITIATED;
        }
        else if (BOOTSTRAPPER_APPLY_RESTART_REQUIRED == pThis->m_restartResult)
        {
            dwQuit = ERROR_SUCCESS_REBOOT_REQUIRED;
        }
        pThis->m_pEngine->Quit(dwQuit);

        ReleaseTheme(pThis->m_pTheme);
        ThemeUninitialize();

        // uninitialize COM
        if (fComInitialized)
        {
            ::CoUninitialize();
        }

        return hr;
    }


    //
    // InitializeData - initializes all the package and prereq information.
    //
    HRESULT InitializeData()
    {
        HRESULT hr = S_OK;
        LPWSTR sczModulePath = NULL;
        IXMLDOMDocument *pixdManifest = NULL;

        hr = BalManifestLoad(m_hModule, &pixdManifest);
        BalExitOnFailure(hr, "Failed to load bootstrapper application manifest.");

        hr = ParseOverridableVariablesFromXml(pixdManifest);
        BalExitOnFailure(hr, "Failed to read overridable variables.");

        hr = ProcessCommandLine(&m_sczLanguage);
        ExitOnFailure(hr, "Unknown commandline parameters.");

        hr = PathRelativeToModule(&sczModulePath, NULL, m_hModule);
        BalExitOnFailure(hr, "Failed to get module path.");

        hr = LoadLocalization(sczModulePath, m_sczLanguage);
        ExitOnFailure(hr, "Failed to load localization.");

        hr = LoadTheme(sczModulePath, m_sczLanguage);
        ExitOnFailure(hr, "Failed to load theme.");

        hr = BalInfoParseFromXml(&m_Bundle, pixdManifest);
        BalExitOnFailure(hr, "Failed to load bundle information.");

        hr = BalConditionsParseFromXml(&m_Conditions, pixdManifest, m_pWixLoc);
        BalExitOnFailure(hr, "Failed to load conditions from XML.");

        LoadBootstrapperBAFunctions();

        if (m_fPrereq)
        {
            hr = ParsePrerequisiteInformationFromXml(pixdManifest);
            BalExitOnFailure(hr, "Failed to read prerequisite information.");
        }
        else
        {
            hr = ParseBootrapperApplicationDataFromXml(pixdManifest);
            BalExitOnFailure(hr, "Failed to read bootstrapper application data.");
        }

    LExit:
        ReleaseObject(pixdManifest);
        ReleaseStr(sczModulePath);

        return hr;
    }


    //
    // ProcessCommandLine - process the provided command line arguments.
    //
    HRESULT ProcessCommandLine(
        __inout LPWSTR* psczLanguage
        )
    {
        HRESULT hr = S_OK;
        int argc = 0;
        LPWSTR* argv = NULL;
        LPWSTR sczVariableName = NULL;
        LPWSTR sczVariableValue = NULL;

        if (m_command.wzCommandLine && *m_command.wzCommandLine)
        {
            argv = ::CommandLineToArgvW(m_command.wzCommandLine, &argc);
            ExitOnNullWithLastError(argv, hr, "Failed to get command line.");

            for (int i = 0; i < argc; ++i)
            {
                if (argv[i][0] == L'-' || argv[i][0] == L'/')
                {
                    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"lang", -1))
                    {
                        if (i + 1 >= argc)
                        {
                            hr = E_INVALIDARG;
                            BalExitOnFailure(hr, "Must specify a language.");
                        }

                        ++i;

                        hr = StrAllocString(psczLanguage, &argv[i][0], 0);
                        BalExitOnFailure(hr, "Failed to copy language.");
                    }
                }
                else if (m_sdOverridableVariables)
                {
                    const wchar_t* pwc = wcschr(argv[i], L'=');
                    if (pwc)
                    {
                        hr = StrAllocString(&sczVariableName, argv[i], pwc - argv[i]);
                        BalExitOnFailure(hr, "Failed to copy variable name.");

                        hr = DictKeyExists(m_sdOverridableVariables, sczVariableName);
                        if (E_NOTFOUND == hr)
                        {
                            BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "Ignoring attempt to set non-overridable variable: '%ls'.", sczVariableName);
                            hr = S_OK;
                            continue;
                        }
                        ExitOnFailure(hr, "Failed to check the dictionary of overridable variables.");

                        hr = StrAllocString(&sczVariableValue, ++pwc, 0);
                        BalExitOnFailure(hr, "Failed to copy variable value.");

                        hr = m_pEngine->SetVariableString(sczVariableName, sczVariableValue);
                        BalExitOnFailure(hr, "Failed to set variable.");
                    }
                    else
                    {
                        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Ignoring unknown argument: %ls", argv[i]);
                    }
                }
            }
        }

    LExit:
        if (argv)
        {
            ::LocalFree(argv);
        }

        ReleaseStr(sczVariableName);
        ReleaseStr(sczVariableValue);

        return hr;
    }

    HRESULT LoadLocalization(
        __in_z LPCWSTR wzModulePath,
        __in_z_opt LPCWSTR wzLanguage
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczLocPath = NULL;
        LPCWSTR wzLocFileName = m_fPrereq ? L"mbapreq.wxl" : L"thm.wxl";

        hr = LocProbeForFile(wzModulePath, wzLocFileName, wzLanguage, &sczLocPath);
        BalExitOnFailure2(hr, "Failed to probe for loc file: %ls in path: %ls", wzLocFileName, wzModulePath);

        hr = LocLoadFromFile(sczLocPath, &m_pWixLoc);
        BalExitOnFailure1(hr, "Failed to load loc file from path: %ls", sczLocPath);

        if (WIX_LOCALIZATION_LANGUAGE_NOT_SET != m_pWixLoc->dwLangId)
        {
            ::SetThreadLocale(m_pWixLoc->dwLangId);
        }

        hr = StrAllocString(&m_sczConfirmCloseMessage, L"#(loc.ConfirmCancelMessage)", 0);
        ExitOnFailure(hr, "Failed to initialize confirm message loc identifier.");

        hr = LocLocalizeString(m_pWixLoc, &m_sczConfirmCloseMessage);
        BalExitOnFailure1(hr, "Failed to localize confirm close message: %ls", m_sczConfirmCloseMessage);

    LExit:
        ReleaseStr(sczLocPath);

        return hr;
    }


    HRESULT LoadTheme(
        __in_z LPCWSTR wzModulePath,
        __in_z_opt LPCWSTR wzLanguage
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczThemePath = NULL;
        LPCWSTR wzThemeFileName = m_fPrereq ? L"mbapreq.thm" : L"thm.xml";
        LPWSTR sczCaption = NULL;

        hr = LocProbeForFile(wzModulePath, wzThemeFileName, wzLanguage, &sczThemePath);
        BalExitOnFailure2(hr, "Failed to probe for theme file: %ls in path: %ls", wzThemeFileName, wzModulePath);

        hr = ThemeLoadFromFile(sczThemePath, &m_pTheme);
        BalExitOnFailure1(hr, "Failed to load theme from path: %ls", sczThemePath);

        hr = ThemeLocalize(m_pTheme, m_pWixLoc);
        BalExitOnFailure1(hr, "Failed to localize theme: %ls", sczThemePath);

        // Update the caption if there are any formatted strings in it.
        hr = BalFormatString(m_pTheme->sczCaption, &sczCaption);
        if (SUCCEEDED(hr))
        {
            ThemeUpdateCaption(m_pTheme, sczCaption);
        }

    LExit:
        ReleaseStr(sczCaption);
        ReleaseStr(sczThemePath);

        return hr;
    }


    HRESULT ParseOverridableVariablesFromXml(
        __in IXMLDOMDocument* pixdManifest
        )
    {
        HRESULT hr = S_OK;
        IXMLDOMNode* pNode = NULL;
        IXMLDOMNodeList* pNodes = NULL;
        DWORD cNodes = 0;
        LPWSTR scz = NULL;

        // get the list of variables users can override on the command line
        hr = XmlSelectNodes(pixdManifest, L"/BootstrapperApplicationData/WixStdbaOverridableVariable", &pNodes);
        if (S_FALSE == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        ExitOnFailure(hr, "Failed to select overridable variable nodes.");

        hr = pNodes->get_length((long*)&cNodes);
        ExitOnFailure(hr, "Failed to get overridable variable node count.");

        if (cNodes)
        {
            hr = DictCreateStringList(&m_sdOverridableVariables, 32, DICT_FLAG_NONE);
            ExitOnFailure(hr, "Failed to create the string dictionary.");

            for (DWORD i = 0; i < cNodes; ++i)
            {
                hr = XmlNextElement(pNodes, &pNode, NULL);
                ExitOnFailure(hr, "Failed to get next node.");

                // @Name
                hr = XmlGetAttributeEx(pNode, L"Name", &scz);
                ExitOnFailure(hr, "Failed to get @Name.");

                hr = DictAddKey(m_sdOverridableVariables, scz);
                ExitOnFailure1(hr, "Failed to add \"%ls\" to the string dictionary.", scz);

                // prepare next iteration
                ReleaseNullObject(pNode);
            }
        }

    LExit:
        ReleaseObject(pNode);
        ReleaseObject(pNodes);
        ReleaseStr(scz);
        return hr;
    }


    HRESULT ParsePrerequisiteInformationFromXml(
        __in IXMLDOMDocument* pixdManifest
        )
    {
        HRESULT hr = S_OK;
        IXMLDOMNode* pNode = NULL;

        hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixMbaPrereqInformation", &pNode);
        if (S_FALSE == hr)
        {
            hr = E_INVALIDARG;
        }
        BalExitOnFailure(hr, "BootstrapperApplication.xml manifest is missing prerequisite information.");

        hr = XmlGetAttributeEx(pNode, L"PackageId", &m_sczPrereqPackage);
        BalExitOnFailure(hr, "Failed to get prerequisite package identifier.");

        hr = XmlGetAttributeEx(pNode, L"LicenseUrl", &m_sczLicenseUrl);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        BalExitOnFailure(hr, "Failed to get prerequisite license URL.");

        hr = XmlGetAttributeEx(pNode, L"LicenseFile", &m_sczLicenseFile);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        BalExitOnFailure(hr, "Failed to get prerequisite license file.");

    LExit:
        ReleaseObject(pNode);
        return hr;
    }


    HRESULT ParseBootrapperApplicationDataFromXml(
        __in IXMLDOMDocument* pixdManifest
        )
    {
        HRESULT hr = S_OK;
        IXMLDOMNode* pNode = NULL;
        DWORD dwBool = 0;

        hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixStdbaInformation", &pNode);
        if (S_FALSE == hr)
        {
            hr = E_INVALIDARG;
        }
        BalExitOnFailure(hr, "BootstrapperApplication.xml manifest is missing wixstdba information.");

        hr = XmlGetAttributeEx(pNode, L"LicenseFile", &m_sczLicenseFile);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        BalExitOnFailure(hr, "Failed to get license file.");

        hr = XmlGetAttributeEx(pNode, L"LicenseUrl", &m_sczLicenseUrl);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        BalExitOnFailure(hr, "Failed to get license URL.");

        ReleaseObject(pNode);

        hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixStdbaOptions", &pNode);
        if (S_FALSE == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        BalExitOnFailure(hr, "Failed to read wixstdba options from BootstrapperApplication.xml manifest.");

        hr = XmlGetAttributeNumber(pNode, L"SuppressOptionsUI", &dwBool);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (SUCCEEDED(hr))
        {
            m_fSuppressOptionsUI = 0 < dwBool;
        }
        BalExitOnFailure(hr, "Failed to get SuppressOptionsUI value.");

        dwBool = 0;
        hr = XmlGetAttributeNumber(pNode, L"SuppressDowngradeFailure", &dwBool);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (SUCCEEDED(hr))
        {
            m_fSuppressDowngradeFailure = 0 < dwBool;
        }
        BalExitOnFailure(hr, "Failed to get SuppressDowngradeFailure value.");

        dwBool = 0;
        hr = XmlGetAttributeNumber(pNode, L"SuppressRepair", &dwBool);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (SUCCEEDED(hr))
        {
            m_fSuppressRepair = 0 < dwBool;
        }
        BalExitOnFailure(hr, "Failed to get SuppressRepair value.");

        hr = XmlGetAttributeNumber(pNode, L"ShowVersion", &dwBool);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (SUCCEEDED(hr))
        {
            m_fShowVersion = 0 < dwBool;
        }
        BalExitOnFailure(hr, "Failed to get ShowVersion value.");

    LExit:
        ReleaseObject(pNode);
        return hr;
    }


    //
    // CreateMainWindow - creates the main install window.
    //
    HRESULT CreateMainWindow()
    {
        HRESULT hr = S_OK;
        HICON hIcon = reinterpret_cast<HICON>(m_pTheme->hIcon);
        WNDCLASSW wc = { };
        DWORD dwWindowStyle = 0;
        int x = CW_USEDEFAULT;
        int y = CW_USEDEFAULT;
        POINT ptCursor = { };
        HMONITOR hMonitor = NULL;
        MONITORINFO mi = { };

        // If the theme did not provide an icon, try using the icon from the bundle engine.
        if (!hIcon)
        {
            HMODULE hBootstrapperEngine = ::GetModuleHandleW(NULL);
            if (hBootstrapperEngine)
            {
                hIcon = ::LoadIconW(hBootstrapperEngine, MAKEINTRESOURCEW(1));
            }
        }

        // Register the window class and create the window.
        wc.lpfnWndProc = CWixStandardBootstrapperApplication::WndProc;
        wc.hInstance = m_hModule;
        wc.hIcon = hIcon;
        wc.hCursor = ::LoadCursorW(NULL, (LPCWSTR)IDC_ARROW);
        wc.hbrBackground = m_pTheme->rgFonts[m_pTheme->dwFontId].hBackground;
        wc.lpszMenuName = NULL;
        wc.lpszClassName = WIXSTDBA_WINDOW_CLASS;
        if (!::RegisterClassW(&wc))
        {
            ExitWithLastError(hr, "Failed to register window.");
        }

        m_fRegistered = TRUE;

        // Calculate the window style based on the theme style and command display value.
        dwWindowStyle = m_pTheme->dwStyle;
        if (BOOTSTRAPPER_DISPLAY_NONE >= m_command.display)
        {
            dwWindowStyle &= ~WS_VISIBLE;
        }

        // Don't show the window if there is a splash screen (it will be made visible when the splash screen is hidden)
        if (::IsWindow(m_command.hwndSplashScreen))
        {
            dwWindowStyle &= ~WS_VISIBLE;
        }

        // Center the window on the monitor with the mouse.
        if (::GetCursorPos(&ptCursor))
        {
            hMonitor = ::MonitorFromPoint(ptCursor, MONITOR_DEFAULTTONEAREST);
            if (hMonitor)
            {
                mi.cbSize = sizeof(mi);
                if (::GetMonitorInfoW(hMonitor, &mi))
                {
                    x = mi.rcWork.left + (mi.rcWork.right  - mi.rcWork.left - m_pTheme->nWidth) / 2;
                    y = mi.rcWork.top  + (mi.rcWork.bottom - mi.rcWork.top  - m_pTheme->nHeight) / 2;
                }
            }
        }

        m_hWnd = ::CreateWindowExW(0, wc.lpszClassName, m_pTheme->sczCaption, dwWindowStyle, x, y, m_pTheme->nWidth, m_pTheme->nHeight, HWND_DESKTOP, NULL, m_hModule, this);
        ExitOnNullWithLastError(m_hWnd, hr, "Failed to create window.");

        hr = S_OK;

    LExit:
        return hr;
    }


    //
    // InitializeTaskbarButton - initializes taskbar button for progress.
    //
    void InitializeTaskbarButton()
    {
        HRESULT hr = S_OK;

        hr = ::CoCreateInstance(CLSID_TaskbarList, NULL, CLSCTX_ALL, __uuidof(ITaskbarList3), reinterpret_cast<LPVOID*>(&m_pTaskbarList));
        if (REGDB_E_CLASSNOTREG == hr) // not supported before Windows 7
        {
            ExitFunction1(hr = S_OK);
        }
        BalExitOnFailure(hr, "Failed to create ITaskbarList3. Continuing.");

        m_uTaskbarButtonCreatedMessage = ::RegisterWindowMessageW(L"TaskbarButtonCreated");
        BalExitOnNullWithLastError(m_uTaskbarButtonCreatedMessage, hr, "Failed to get TaskbarButtonCreated message. Continuing.");

    LExit:
        return;
    }

    //
    // DestroyMainWindow - clean up all the window registration.
    //
    void DestroyMainWindow()
    {
        if (::IsWindow(m_hWnd))
        {
            ::DestroyWindow(m_hWnd);
            m_hWnd = NULL;
            m_fTaskbarButtonOK = FALSE;
        }

        if (m_fRegistered)
        {
            ::UnregisterClassW(WIXSTDBA_WINDOW_CLASS, m_hModule);
            m_fRegistered = FALSE;
        }
    }


    //
    // WndProc - standard windows message handler.
    //
    static LRESULT CALLBACK WndProc(
        __in HWND hWnd,
        __in UINT uMsg,
        __in WPARAM wParam,
        __in LPARAM lParam
        )
    {
#pragma warning(suppress:4312)
        CWixStandardBootstrapperApplication* pBA = reinterpret_cast<CWixStandardBootstrapperApplication*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

        switch (uMsg)
        {
        case WM_NCCREATE:
            {
            LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
            pBA = reinterpret_cast<CWixStandardBootstrapperApplication*>(lpcs->lpCreateParams);
#pragma warning(suppress:4244)
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pBA));
            }
            break;

        case WM_NCDESTROY:
            {
            LRESULT lres = ThemeDefWindowProc(pBA ? pBA->m_pTheme : NULL, hWnd, uMsg, wParam, lParam);
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
            return lres;
            }

        case WM_CREATE:
            if (!pBA->OnCreate(hWnd))
            {
                return -1;
            }
            break;

        case WM_QUERYENDSESSION:
            return IDCANCEL != pBA->OnSystemShutdown(static_cast<DWORD>(lParam), IDCANCEL);

        case WM_CLOSE:
            // If the user chose not to close, do *not* let the default window proc handle the message.
            if (!pBA->OnClose())
            {
                return 0;
            }
            break;

        case WM_DESTROY:
            ::PostQuitMessage(0);
            break;

        case WM_WIXSTDBA_SHOW_HELP:
            pBA->OnShowHelp();
            return 0;

        case WM_WIXSTDBA_DETECT_PACKAGES:
            pBA->OnDetect();
            return 0;

        case WM_WIXSTDBA_PLAN_PACKAGES:
            pBA->OnPlan(static_cast<BOOTSTRAPPER_ACTION>(lParam));
            return 0;

        case WM_WIXSTDBA_APPLY_PACKAGES:
            pBA->OnApply();
            return 0;

        case WM_WIXSTDBA_CHANGE_STATE:
            pBA->OnChangeState(static_cast<WIXSTDBA_STATE>(lParam));
            return 0;

        case WM_COMMAND:
            switch (LOWORD(wParam))
            {
            case WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX:
                pBA->OnClickAcceptCheckbox();
                return 0;

            case WIXSTDBA_CONTROL_OPTIONS_BUTTON:
                pBA->OnClickOptionsButton();
                return 0;

            case WIXSTDBA_CONTROL_BROWSE_BUTTON:
                pBA->OnClickOptionsBrowseButton();
                return 0;

            case WIXSTDBA_CONTROL_OK_BUTTON:
                pBA->OnClickOptionsOkButton();
                return 0;

            case WIXSTDBA_CONTROL_CANCEL_BUTTON:
                pBA->OnClickOptionsCancelButton();
                return 0;

            case WIXSTDBA_CONTROL_INSTALL_BUTTON:
                pBA->OnClickInstallButton();
                return 0;

            case WIXSTDBA_CONTROL_REPAIR_BUTTON:
                pBA->OnClickRepairButton();
                return 0;

            case WIXSTDBA_CONTROL_UNINSTALL_BUTTON:
                pBA->OnClickUninstallButton();
                return 0;

            case WIXSTDBA_CONTROL_LAUNCH_BUTTON:
                pBA->OnClickLaunchButton();
                return 0;

            case WIXSTDBA_CONTROL_SUCCESS_RESTART_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON:
                pBA->OnClickRestartButton();
                return 0;

            case WIXSTDBA_CONTROL_HELP_CANCEL_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_WELCOME_CANCEL_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_MODIFY_CANCEL_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_SUCCESS_CANCEL_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_FAILURE_CANCEL_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_CLOSE_BUTTON:
                pBA->OnClickCloseButton();
                return 0;
            }
            break;

        case WM_NOTIFY:
            if (lParam)
            {
                LPNMHDR pnmhdr = reinterpret_cast<LPNMHDR>(lParam);
                switch (pnmhdr->code)
                {
                case NM_CLICK: __fallthrough;
                case NM_RETURN:
                    switch (static_cast<DWORD>(pnmhdr->idFrom))
                    {
                    case WIXSTDBA_CONTROL_EULA_LINK:
                        pBA->OnClickEulaLink();
                        return 1;
                    case WIXSTDBA_CONTROL_FAILURE_LOGFILE_LINK:
                        pBA->OnClickLogFileLink();
                        return 1;
                    }
                }
            }
            break;
        }

        if (pBA && pBA->m_pTaskbarList && uMsg == pBA->m_uTaskbarButtonCreatedMessage)
        {
            pBA->m_fTaskbarButtonOK = TRUE;
            return 0;
        }

        return ThemeDefWindowProc(pBA ? pBA->m_pTheme : NULL, hWnd, uMsg, wParam, lParam);
    }


    //
    // OnCreate - finishes loading the theme.
    //
    BOOL OnCreate(
        __in HWND hWnd
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczText = NULL;
        LPWSTR sczLicenseFormatted = NULL;
        LPWSTR sczLicensePath = NULL;
        LPWSTR sczLicenseDirectory = NULL;
        LPWSTR sczLicenseFilename = NULL;

        hr = ThemeLoadControls(m_pTheme, hWnd, vrgInitControls, countof(vrgInitControls));
        BalExitOnFailure(hr, "Failed to load theme controls.");

        C_ASSERT(COUNT_WIXSTDBA_PAGE == countof(vrgwzPageNames));
        C_ASSERT(countof(m_rgdwPageIds) == countof(vrgwzPageNames));

        ThemeGetPageIds(m_pTheme, vrgwzPageNames, m_rgdwPageIds, countof(m_rgdwPageIds));

        // Initialize the text on all "application" (non-page) controls.
        for (DWORD i = 0; i < m_pTheme->cControls; ++i)
        {
            THEME_CONTROL* pControl = m_pTheme->rgControls + i;
            if (!pControl->wPageId && pControl->sczText && *pControl->sczText)
            {
                HRESULT hrFormat = BalFormatString(pControl->sczText, &sczText);
                if (SUCCEEDED(hrFormat))
                {
                    ThemeSetTextControl(m_pTheme, pControl->wId, sczText);
                }
            }
        }

        // Load the RTF EULA control with text if the control exists.
        if (ThemeControlExists(m_pTheme, WIXSTDBA_CONTROL_EULA_RICHEDIT))
        {
            hr = (m_sczLicenseFile && *m_sczLicenseFile) ? S_OK : E_INVALIDDATA;
            if (SUCCEEDED(hr))
            {
                hr = StrAllocString(&sczLicenseFormatted, m_sczLicenseFile, 0);
                if (SUCCEEDED(hr))
                {
                    hr = LocLocalizeString(m_pWixLoc, &sczLicenseFormatted);
                    if (SUCCEEDED(hr))
                    {
                        hr = BalFormatString(sczLicenseFormatted, &sczLicenseFormatted);
                        if (SUCCEEDED(hr))
                        {
                            hr = PathRelativeToModule(&sczLicensePath, sczLicenseFormatted, m_hModule);
                            if (SUCCEEDED(hr))
                            {
                                hr = PathGetDirectory(sczLicensePath, &sczLicenseDirectory);
                                if (SUCCEEDED(hr))
                                {
                                    hr = StrAllocString(&sczLicenseFilename, PathFile(sczLicenseFormatted), 0);
                                    if (SUCCEEDED(hr))
                                    {
                                        hr = LocProbeForFile(sczLicenseDirectory, sczLicenseFilename, m_sczLanguage, &sczLicensePath);
                                        if (SUCCEEDED(hr))
                                        {
                                            hr = ThemeLoadRichEditFromFile(m_pTheme, WIXSTDBA_CONTROL_EULA_RICHEDIT, sczLicensePath, m_hModule);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (FAILED(hr))
            {
                BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "Failed to load file into license richedit control from path '%ls' manifest value: %ls", sczLicensePath, m_sczLicenseFile);
                hr = S_OK;
            }
        }

    LExit:
        ReleaseStr(sczLicenseFilename);
        ReleaseStr(sczLicenseDirectory);
        ReleaseStr(sczLicensePath);
        ReleaseStr(sczLicenseFormatted);
        ReleaseStr(sczText);

        return SUCCEEDED(hr);
    }


    //
    // OnShowHelp - display the help page.
    //
    void OnShowHelp()
    {
        SetState(WIXSTDBA_STATE_HELP, S_OK);

        // If the UI should be visible, display it now and hide the splash screen
        if (BOOTSTRAPPER_DISPLAY_NONE < m_command.display)
        {
            ::ShowWindow(m_pTheme->hwndParent, SW_SHOW);
        }

        m_pEngine->CloseSplashScreen();

        return;
    }


    //
    // OnDetect - start the processing of packages.
    //
    void OnDetect()
    {
        HRESULT hr = S_OK;

        if (m_pBAFunction)
        {
            hr = m_pBAFunction->OnDetect();
            BalExitOnFailure(hr, "Failed calling detect BA function.");
        }

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running detect BA function");

        SetState(WIXSTDBA_STATE_DETECTING, hr);

        hr = GetFileVersion();
        BalExitOnFailure(hr, "Failed to get version.");

        // Delay start to show splash screen handling
        Delay();

        // If the UI should be visible, display it now and hide the splash screen
        if (BOOTSTRAPPER_DISPLAY_NONE < m_command.display)
        {
            ::ShowWindow(m_pTheme->hwndParent, SW_SHOW);
        }

        m_pEngine->CloseSplashScreen();

        // Tell the core we're ready for the packages to be processed now.
        hr = m_pEngine->Detect();
        BalExitOnFailure(hr, "Failed to start detecting chain.");

    LExit:
        if (FAILED(hr))
        {
            SetState(WIXSTDBA_STATE_DETECTING, hr);
        }

        return;
    }

    //
    // Get the file version of the bootstrapper and record in bootstrapper log file
    //
    HRESULT GetFileVersion()
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;
        ULARGE_INTEGER uliVersion = { };

        BalFormatString(L"[WixBundleOriginalSource]", &sczValue);
        BalExitOnFailure(hr, "Failed to format variable.");

        FileVersion(sczValue, &uliVersion.HighPart, &uliVersion.LowPart);
        BalExitOnFailure(hr, "Failed to get file version.");

        hr = m_pEngine->SetVariableVersion(L"FileVersion", uliVersion.QuadPart);
        BalExitOnFailure(hr, "Failed to set variable.");

    LExit:
        ReleaseStr(sczValue);

        return hr;
    }

    //
    // Delay the closing of the splash screen in milliseconds (in Bundle.wxs set variable DelayStart with value of milliseconds)
    //
    HRESULT Delay() 
    { 
        HRESULT hr = S_OK; 
        LONGLONG llDelay = 0; 
        
        BalGetNumericVariable(L"DelayStart", &llDelay); 
        if (llDelay) 
        { 
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Delay for %dms", (DWORD)llDelay); 
            ::Sleep((DWORD)llDelay); 
        } 
        
        return hr; 
    }


    //
    // OnPlan - plan the detected changes.
    //
    void OnPlan(
        __in BOOTSTRAPPER_ACTION action
        )
    {
        HRESULT hr = S_OK;

        m_plannedAction = action;

        // If we are going to apply a downgrade, bail.
        if (m_fDowngrading && BOOTSTRAPPER_ACTION_UNINSTALL < action)
        {
            if (m_fSuppressDowngradeFailure)
            {
                BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "A newer version of this product is installed but downgrade failure has been suppressed; continuing...");
            }
            else
            {
                hr = HRESULT_FROM_WIN32(ERROR_PRODUCT_VERSION);
                BalExitOnFailure(hr, "Cannot install a product when a newer version is installed.");
            }
        }

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running plan BA function");

        SetState(WIXSTDBA_STATE_PLANNING, hr);

        if (m_pBAFunction)
        {
            m_pBAFunction->OnPlan();
        }

        hr = m_pEngine->Plan(action);
        BalExitOnFailure(hr, "Failed to start planning packages.");

    LExit:
        if (FAILED(hr))
        {
            SetState(WIXSTDBA_STATE_PLANNING, hr);
        }

        return;
    }


    //
    // OnApply - apply the packages.
    //
    void OnApply()
    {
        HRESULT hr = S_OK;

        SetState(WIXSTDBA_STATE_APPLYING, hr);
        SetProgressState(hr);
        SetTaskbarButtonProgress(0);

        hr = m_pEngine->Apply(m_hWnd);
        BalExitOnFailure(hr, "Failed to start applying packages.");

        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON, TRUE); // ensure the cancel button is enabled before starting.

    LExit:
        if (FAILED(hr))
        {
            SetState(WIXSTDBA_STATE_APPLYING, hr);
        }

        return;
    }


    //
    // OnChangeState - change state.
    //
    void OnChangeState(
        __in WIXSTDBA_STATE state
        )
    {
        WIXSTDBA_STATE stateOld = m_state;
        DWORD dwOldPageId = 0;
        DWORD dwNewPageId = 0;
        LPWSTR sczText = NULL;
        LPWSTR sczUnformattedText = NULL;
        LPWSTR sczControlState = NULL;
        LPWSTR sczControlName = NULL;

        m_state = state;

        // If our install is at the end (success or failure) and we're not showing full UI or
        // we successfully installed the prerequisite then exit (prompt for restart if required).
        if ((WIXSTDBA_STATE_APPLIED <= m_state && BOOTSTRAPPER_DISPLAY_FULL > m_command.display) ||
            (WIXSTDBA_STATE_APPLIED == m_state && m_fPrereq))
        {
            // If a restart was required but we were not automatically allowed to
            // accept the reboot then do the prompt.
            if (m_fRestartRequired && !m_fAllowRestart)
            {
                StrAllocFromError(&sczUnformattedText, HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED), NULL);

                int nResult = ::MessageBoxW(m_hWnd, sczUnformattedText ? sczUnformattedText : L"The requested operation is successful. Changes will not be effective until the system is rebooted.", m_pTheme->sczCaption, MB_ICONEXCLAMATION | MB_OKCANCEL);
                m_fAllowRestart = (IDOK == nResult);
            }

            // Quietly exit.
            ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
        }
        else // try to change the pages.
        {
            DeterminePageId(stateOld, &dwOldPageId);
            DeterminePageId(m_state, &dwNewPageId);

            if (dwOldPageId != dwNewPageId)
            {
                // Enable disable controls per-page.
                if (m_rgdwPageIds[WIXSTDBA_PAGE_INSTALL] == dwNewPageId) // on the "Install" page, ensure the install button is enabled/disabled correctly.
                {
                    LONGLONG llElevated = 0;
                    if (m_Bundle.fPerMachine)
                    {
                        BalGetNumericVariable(WIXBUNDLE_VARIABLE_ELEVATED, &llElevated);
                    }
                    ThemeControlElevates(m_pTheme, WIXSTDBA_CONTROL_INSTALL_BUTTON, (m_Bundle.fPerMachine && !llElevated));

                    // If the EULA control exists, show it only if a license URL is provided as well.
                    if (ThemeControlExists(m_pTheme, WIXSTDBA_CONTROL_EULA_LINK))
                    {
                        BOOL fEulaLink = (m_sczLicenseUrl && *m_sczLicenseUrl);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_EULA_LINK, fEulaLink);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX, fEulaLink);
                    }

                    BOOL fAcceptedLicense = !ThemeControlExists(m_pTheme, WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX) || !ThemeControlEnabled(m_pTheme, WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX) || ThemeIsControlChecked(m_pTheme, WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_INSTALL_BUTTON, fAcceptedLicense);

                    // If there is an "Options" page, the "Options" button exists, and it hasn't been suppressed, then enable the button.
                    BOOL fOptionsEnabled = m_rgdwPageIds[WIXSTDBA_PAGE_OPTIONS] && ThemeControlExists(m_pTheme, WIXSTDBA_CONTROL_OPTIONS_BUTTON) && !m_fSuppressOptionsUI;
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_OPTIONS_BUTTON, fOptionsEnabled);

                    // Show/Hide the version label if it exists.
                    if (m_rgdwPageIds[WIXSTDBA_PAGE_OPTIONS] && ThemeControlExists(m_pTheme, WIXSTDBA_CONTROL_VERSION_LABEL) && !m_fShowVersion)
                    {
                        ThemeShowControl(m_pTheme, WIXSTDBA_CONTROL_VERSION_LABEL, SW_HIDE);
                    }
                }
                else if (m_rgdwPageIds[WIXSTDBA_PAGE_MODIFY] == dwNewPageId)
                {
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_REPAIR_BUTTON, !m_fSuppressRepair);
                }
                else if (m_rgdwPageIds[WIXSTDBA_PAGE_OPTIONS] == dwNewPageId)
                {
                    HRESULT hr = BalGetStringVariable(WIXSTDBA_VARIABLE_INSTALL_FOLDER, &sczUnformattedText);
                    if (SUCCEEDED(hr))
                    {
                        BalFormatString(sczUnformattedText, &sczText);
                        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_FOLDER_EDITBOX, sczText);
                    }
                }
                else if (m_rgdwPageIds[WIXSTDBA_PAGE_SUCCESS] == dwNewPageId) // on the "Success" page, check if the restart or launch button should be enabled.
                {
                    BOOL fShowRestartButton = FALSE;
                    BOOL fLaunchTargetExists = FALSE;
                    if (m_fRestartRequired)
                    {
                        if (BOOTSTRAPPER_RESTART_PROMPT == m_command.restart)
                        {
                            fShowRestartButton = TRUE;
                        }
                    }
                    else if (ThemeControlExists(m_pTheme, WIXSTDBA_CONTROL_LAUNCH_BUTTON))
                    {
                        fLaunchTargetExists = BalStringVariableExists(WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH);
                    }

                    // Set the header of the Success page to either Setup or Uninstall
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_HEADER, TRUE);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_SETUP_HEADER, FALSE);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_UNINSTALL_HEADER, FALSE);
                    if (BOOTSTRAPPER_ACTION_INSTALL == m_plannedAction)
                    {
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_SETUP_HEADER, TRUE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_UNINSTALL_HEADER, FALSE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_HEADER, FALSE);
                    }
                    else if(BOOTSTRAPPER_ACTION_UNINSTALL == m_plannedAction)
                    {
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_UNINSTALL_HEADER, TRUE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_SETUP_HEADER, FALSE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_HEADER, FALSE);
                    }

                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_LAUNCH_BUTTON, fLaunchTargetExists && BOOTSTRAPPER_ACTION_UNINSTALL < m_plannedAction);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_RESTART_TEXT, fShowRestartButton);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_SUCCESS_RESTART_BUTTON, fShowRestartButton);
                }
                else if (m_rgdwPageIds[WIXSTDBA_PAGE_FAILURE] == dwNewPageId) // on the "Failure" page, show error message and check if the restart button should be enabled.
                {
                    BOOL fShowLogLink = (m_Bundle.sczLogVariable && *m_Bundle.sczLogVariable); // if there is a log file variable then we'll assume the log file exists.
                    BOOL fShowErrorMessage = FALSE;
                    BOOL fShowRestartButton = FALSE;

                    if (FAILED(m_hrFinal))
                    {
                        // If we know the failure message, use that.
                        if (m_sczFailedMessage && *m_sczFailedMessage)
                        {
                            StrAllocString(&sczUnformattedText, m_sczFailedMessage, 0);
                        }
                        else // try to get the error message from the error code.
                        {
                            StrAllocFromError(&sczUnformattedText, m_hrFinal, NULL);
                            if (!sczUnformattedText || !*sczUnformattedText)
                            {
                                StrAllocFromError(&sczUnformattedText, E_FAIL, NULL);
                            }
                        }

                        StrAllocFormatted(&sczText, L"0x%08x - %ls", m_hrFinal, sczUnformattedText);
                        ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_FAILURE_MESSAGE_TEXT, sczText);
                        fShowErrorMessage = TRUE;
                    }

                    if (m_fRestartRequired)
                    {
                        if (BOOTSTRAPPER_RESTART_PROMPT == m_command.restart)
                        {
                            fShowRestartButton = TRUE;
                        }
                    }

                    // Set the header of the failure page to either Setup or Uninstall
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_HEADER, TRUE);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_SETUP_HEADER, FALSE);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_UNINSTALL_HEADER, FALSE);
                    if (BOOTSTRAPPER_ACTION_INSTALL == m_plannedAction)
                    {
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_SETUP_HEADER, TRUE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_UNINSTALL_HEADER, FALSE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_HEADER, FALSE);
                    }
                    else if(BOOTSTRAPPER_ACTION_UNINSTALL == m_plannedAction)
                    {
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_UNINSTALL_HEADER, TRUE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_SETUP_HEADER, FALSE);
                        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_HEADER, FALSE);
                    }

                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_LOGFILE_LINK, fShowLogLink);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_MESSAGE_TEXT, fShowErrorMessage);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_RESTART_TEXT, fShowRestartButton);
                    ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON, fShowRestartButton);
                }

                // Process each control for special handling in the new page.
                THEME_PAGE* pPage = ThemeGetPage(m_pTheme, dwNewPageId);
                if (pPage)
                {
                    for (DWORD i = 0; i < pPage->cControlIndices; ++i)
                    {
                        THEME_CONTROL* pControl = m_pTheme->rgControls + pPage->rgdwControlIndices[i];

                        // If we are on the install, options or modify pages and this is a named control, try to set its default state.
                        if ((m_rgdwPageIds[WIXSTDBA_PAGE_INSTALL] == dwNewPageId ||
                             m_rgdwPageIds[WIXSTDBA_PAGE_OPTIONS] == dwNewPageId ||
                             m_rgdwPageIds[WIXSTDBA_PAGE_MODIFY] == dwNewPageId) &&
                             pControl->sczName && *pControl->sczName)
                        {
                            // If this is a checkbox control, try to set its default state to the state of a matching named Burn variable.
                            if (THEME_CONTROL_TYPE_CHECKBOX == pControl->type && WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX != pControl->wId)
                            {
                                LONGLONG llValue = 0;
                                HRESULT hr = BalGetNumericVariable(pControl->sczName, &llValue);

                                ThemeSendControlMessage(m_pTheme, pControl->wId, BM_SETCHECK, SUCCEEDED(hr) && llValue ? BST_CHECKED : BST_UNCHECKED, 0);
                            }

                            // If this is a button control with the BS_AUTORADIOBUTTON style, try to set its default
                            // state to the state of a matching named Burn variable.
                            if (THEME_CONTROL_TYPE_BUTTON == pControl->type && (BS_AUTORADIOBUTTON == (BS_AUTORADIOBUTTON & pControl->dwStyle)))
                            {
                                LONGLONG llValue = 0;
                                HRESULT hr = BalGetNumericVariable(pControl->sczName, &llValue);

                                // If the control value isn't set then disable it.
                                if (!SUCCEEDED(hr))
                                {
                                    ThemeControlEnable(m_pTheme, pControl->wId, FALSE);
                                }
                                else
                                {
                                    ThemeSendControlMessage(m_pTheme, pControl->wId, BM_SETCHECK, SUCCEEDED(hr) && llValue ? BST_CHECKED : BST_UNCHECKED, 0);
                                }
                            }

                            // Hide or disable controls based on the control name with 'State' appended
                            HRESULT hr = StrAllocFormatted(&sczControlName, L"%lsState", pControl->sczName);
                            if (SUCCEEDED(hr))
                            {
                                hr = BalGetStringVariable(sczControlName, &sczControlState);
                                if (SUCCEEDED(hr) && sczControlState && *sczControlState)
                                {
                                    if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, sczControlState, -1, L"disable", -1))
                                    {
                                        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Disable control %ls", pControl->sczName);
                                        ThemeControlEnable(m_pTheme, pControl->wId, FALSE);
                                    }
                                    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, sczControlState, -1, L"hide", -1))
                                    {
                                        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Hide control %ls", pControl->sczName);
                                        // TODO: This doesn't work
                                        ThemeShowControl(m_pTheme, pControl->wId, SW_HIDE);
                                    }
                                }
                            }
                        }

                        // Format the text in each of the new page's controls (if they have any text).
                        if (pControl->sczText && *pControl->sczText)
                        {
                            HRESULT hr = BalFormatString(pControl->sczText, &sczText);
                            if (SUCCEEDED(hr))
                            {
                                ThemeSetTextControl(m_pTheme, pControl->wId, sczText);
                            }
                        }
                    }
                }

                ThemeShowPage(m_pTheme, dwOldPageId, SW_HIDE);
                ThemeShowPage(m_pTheme, dwNewPageId, SW_SHOW);

                // On the install page set the focus to the install button or the next enabled control if install is disabled
                if (m_rgdwPageIds[WIXSTDBA_PAGE_INSTALL] == dwNewPageId)
                {
                    ThemeSetFocus(m_pTheme, WIXSTDBA_CONTROL_INSTALL_BUTTON);
                }
            }
        }

        ReleaseStr(sczText);
        ReleaseStr(sczUnformattedText);
        ReleaseStr(sczControlState);
        ReleaseStr(sczControlName);
    }


    //
    // OnClose - called when the window is trying to be closed.
    //
    BOOL OnClose()
    {
        BOOL fClose = FALSE;

        // If we've already succeeded or failed or showing the help page, just close (prompts are annoying if the bootstrapper is done).
        if (WIXSTDBA_STATE_APPLIED <= m_state || WIXSTDBA_STATE_HELP == m_state)
        {
            fClose = TRUE;
        }
        else // prompt the user or force the cancel if there is no UI.
        {
            fClose = PromptCancel(m_hWnd, BOOTSTRAPPER_DISPLAY_FULL != m_command.display, m_sczConfirmCloseMessage ? m_sczConfirmCloseMessage : L"Are you sure you want to cancel?", m_pTheme->sczCaption);
        }

        // If we're doing progress then we never close, we just cancel to let rollback occur.
        if (WIXSTDBA_STATE_APPLYING <= m_state && WIXSTDBA_STATE_APPLIED > m_state)
        {
            // If we canceled disable cancel button since clicking it again is silly.
            if (fClose)
            {
                ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON, FALSE);
            }

            fClose = FALSE;
        }

        return fClose;
    }


    //
    // OnClickAcceptCheckbox - allow the install to continue.
    //
    void OnClickAcceptCheckbox()
    {
        BOOL fAcceptedLicense = ThemeIsControlChecked(m_pTheme, WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX);
        ThemeControlEnable(m_pTheme, WIXSTDBA_CONTROL_INSTALL_BUTTON, fAcceptedLicense);
    }


    //
    // OnClickOptionsButton - show the options page.
    //    In Bundle.wxs set the variable: VerifyCheckBoxes to 1 to run this function
    //    In Bundle.wxs set the variable: VerifyOfficeOpen to 1 to run this function
    //    In Bundle.wxs set the variable: VerifyRunExecutable to 1 to run this function
    //    In Bundle.wex set the variable: VerifyClientNotInstallingInXP to 1 to run this function
    //
    void OnClickOptionsButton()
    {
        BOOL fAdvanceToOptionsPageCheckboxOkay = TRUE;
        BOOL fAdvanceToOptionsPageOfficeClosed = TRUE;
        BOOL fAdvanceToOptionsPageNotXP = TRUE;
        // Verify that one of the check boxes for the client has been selected, if not produce message
        LONGLONG llVerifyCheckBoxes = 0;
        BalGetNumericVariable(L"VerifyCheckBoxes", &llVerifyCheckBoxes);
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Validate check boxes: %d", (DWORD)llVerifyCheckBoxes);
        // Verify that MS Office applications are closed, if not produce a warning message
        LONGLONG llVerifyOfficeOpen = 0;
        BalGetNumericVariable(L"VerifyOfficeOpen", &llVerifyOfficeOpen);
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Verify if Office application is open: %d", (DWORD)llVerifyOfficeOpen);
        // Verify that the user wants to run an executable by a warning message
        LONGLONG llVerifyRunExecutable = 0;
        BalGetNumericVariable(L"VerifyRunExecutable", &llVerifyRunExecutable);
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Verify to run executable: %d", (DWORD)llVerifyRunExecutable);
        // Verify that the client install cannot run in XP and warn the user
        LONGLONG llVerifyClientNotInstallingInXP = 0;
        BalGetNumericVariable(L"VerifyClientNotInstallingInXP", &llVerifyClientNotInstallingInXP);
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Verify not to run in XP: %d", (DWORD)llVerifyClientNotInstallingInXP);

        if (llVerifyCheckBoxes == 1)
        {
            fAdvanceToOptionsPageCheckboxOkay = IsThereAtLeastOneClientSelected();
        }
        if (llVerifyOfficeOpen == 1)
        {
            fAdvanceToOptionsPageOfficeClosed = IsOfficeClosed();
        }
        if (llVerifyClientNotInstallingInXP == 1 && fAdvanceToOptionsPageOfficeClosed)
        {
            fAdvanceToOptionsPageNotXP = DisplayVerifyClientNotInstallingInXPMessage();
        }
        if (llVerifyRunExecutable == 1 && fAdvanceToOptionsPageOfficeClosed && fAdvanceToOptionsPageNotXP)
        {
            DisplayVerifyRunExecutableMessage();
        }
        if (fAdvanceToOptionsPageCheckboxOkay && fAdvanceToOptionsPageOfficeClosed && fAdvanceToOptionsPageNotXP)
        {
            SavePageSettings(WIXSTDBA_PAGE_INSTALL);
            m_stateBeforeOptions = m_state;
            SetState(WIXSTDBA_STATE_OPTIONS, S_OK);
        }
    }


    //
    // OnClickOptionsBrowseButton - browse for install folder on the options page.
    //
    void OnClickOptionsBrowseButton()
    {
        WCHAR wzPath[MAX_PATH] = { };
        BROWSEINFOW browseInfo = { };
        PIDLIST_ABSOLUTE pidl = NULL;

        browseInfo.hwndOwner = m_hWnd;
        browseInfo.pszDisplayName = wzPath;
        browseInfo.lpszTitle = m_pTheme->sczCaption;
        browseInfo.ulFlags = BIF_RETURNONLYFSDIRS | BIF_USENEWUI;
        pidl = ::SHBrowseForFolderW(&browseInfo);
        if (pidl && ::SHGetPathFromIDListW(pidl, wzPath))
        {
            ThemeSetTextControl(m_pTheme, WIXSTDBA_CONTROL_FOLDER_EDITBOX, wzPath);
        }

        if (pidl)
        {
            ::CoTaskMemFree(pidl);
        }

        return;
    }

    //
    // OnClickOptionsOkButton - accept the changes made by the options page.
    //
    void OnClickOptionsOkButton()
    {
        HRESULT hr = S_OK;
        LPWSTR sczPath = NULL;

        if (ThemeControlExists(m_pTheme, WIXSTDBA_CONTROL_FOLDER_EDITBOX))
        {
            hr = ThemeGetTextControl(m_pTheme, WIXSTDBA_CONTROL_FOLDER_EDITBOX, &sczPath);
            ExitOnFailure(hr, "Failed to get text from folder edit box.");

            // TODO: verify the path is valid.

            hr = m_pEngine->SetVariableString(WIXSTDBA_VARIABLE_INSTALL_FOLDER, sczPath);
            ExitOnFailure(hr, "Failed to set the install folder.");
        }

        SavePageSettings(WIXSTDBA_PAGE_OPTIONS);

    LExit:
        SetState(m_stateBeforeOptions, S_OK);
        return;
    }


    //
    // OnClickOptionsCancelButton - discard the changes made by the options page.
    //
    void OnClickOptionsCancelButton()
    {
        SetState(m_stateBeforeOptions, S_OK);
    }


    //
    // OnClickInstallButton - start the install by planning the packages.
    //
    void OnClickInstallButton()
    {
        SavePageSettings(WIXSTDBA_PAGE_INSTALL);

        this->OnPlan(BOOTSTRAPPER_ACTION_INSTALL);
    }


    //
    // OnClickRepairButton - start the repair.
    //
    void OnClickRepairButton()
    {
        this->OnPlan(BOOTSTRAPPER_ACTION_REPAIR);
    }


    //
    // OnClickUninstallButton - start the uninstall.
    //    In Bundle.wxs set the variable: VerifyOfficeOpen to 1 to run this function
    //
    void OnClickUninstallButton()
    {
        BOOL fValidateOfficeClosed = TRUE;
        // Verify that MS Office applications are closed, if not produce a warning message
        LONGLONG llVerifyOfficeOpen = 0;
        BalGetNumericVariable(L"VerifyOfficeOpen", &llVerifyOfficeOpen);
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Verify if Office application is open: %d", (DWORD)llVerifyOfficeOpen);

        if (llVerifyOfficeOpen == 1)
        {
            fValidateOfficeClosed = IsOfficeClosed();
        }
        if (fValidateOfficeClosed)
        {
            this->OnPlan(BOOTSTRAPPER_ACTION_UNINSTALL);
        }
    }


    //
    // OnClickCloseButton - close the application.
    //
    void OnClickCloseButton()
    {
        ::SendMessageW(m_hWnd, WM_CLOSE, 0, 0);
    }


    //
    // IsThereAtLeastOneClientSelected - Ensure at least one product has been selected to be installed
    //    In Bundle.wxs set the variable: VerifyCheckBoxes to 1 to run this function
    //                  set the variable: VerifyCheckBoxesErrorMsg (WIXSTDBA_VARIABLE_VERIFY_CHECKBOX_ERROR_MSG) to some error message string
    //
    bool IsThereAtLeastOneClientSelected()
    {
        HRESULT hr = S_OK;
        LPWSTR sczUnformattedVerifyCheckBoxesErrorMsg = NULL;
        LPWSTR sczVerifyCheckBoxesErrorMsg = NULL;

        if ( (!ThemeIsControlChecked(m_pTheme, WIXSTDBA_CONTROL_CHECKBOX_CLIENT1)) && (!ThemeIsControlChecked(m_pTheme, WIXSTDBA_CONTROL_CHECKBOX_CLIENT2)) && (!ThemeIsControlChecked(m_pTheme, WIXSTDBA_CONTROL_CHECKBOX_CLIENT3)) )
        {
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_VERIFY_CHECKBOX_ERROR_MSG, &sczUnformattedVerifyCheckBoxesErrorMsg);
            BalExitOnFailure1(hr, "Failed to get verify check box variable: %ls", WIXSTDBA_VARIABLE_VERIFY_CHECKBOX_ERROR_MSG);
            hr = BalFormatString(sczUnformattedVerifyCheckBoxesErrorMsg, &sczVerifyCheckBoxesErrorMsg);
            BalExitOnFailure1(hr, "Failed to format verify check box variable: %ls", sczUnformattedVerifyCheckBoxesErrorMsg);
            ::MessageBoxW(m_hWnd, sczVerifyCheckBoxesErrorMsg, m_pTheme->sczCaption, MB_OK | MB_ICONEXCLAMATION);
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "None of the check boxes were selected to be installed.");

            return FALSE;
        }

    LExit:
        ReleaseStr(sczVerifyCheckBoxesErrorMsg);
        ReleaseStr(sczUnformattedVerifyCheckBoxesErrorMsg);

        return TRUE;

    }


    //
    // IsOfficeClosed - Ensure that the Office applications are closed
    //    In Bundle.wxs set the variable: VerifyOfficeOpen to 1 to run this function
    //                  set the variable: VerifyOfficeOpenMsg (WIXSTDBA_VARIABLE_VERIFY_OFFICE_OPEN_ERROR_MSG) to some error message string
    //
    bool IsOfficeClosed()
    {
        LPCTSTR ExcelProcessName (L"EXCEL.EXE");
        DWORD   ExcelProcessId;
        LPCTSTR OutlookProcessName (L"OUTLOOK.EXE");
        DWORD   OutlookProcessId;
        LPCTSTR PowerPointProcessName (L"POWERPNT.EXE");
        DWORD   PowerPointProcessId;
        LPCTSTR WordProcessName (L"WINWORD.EXE");
        DWORD   WordProcessId;
        HRESULT hr = S_OK;
        LPWSTR sczVerifyOfficeOpenMsg = NULL;
        LPWSTR sczUnformattedVerifyOfficeOpenMsg = NULL;

        ExcelProcessId = FindProcessId(ExcelProcessName);
        OutlookProcessId = FindProcessId(OutlookProcessName);
        PowerPointProcessId = FindProcessId(PowerPointProcessName);
        WordProcessId = FindProcessId(WordProcessName);

        if ( ExcelProcessId || OutlookProcessId || PowerPointProcessId || WordProcessId )
        {
            
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_VERIFY_OFFICE_OPEN_ERROR_MSG, &sczUnformattedVerifyOfficeOpenMsg);
            BalExitOnFailure1(hr, "Failed to get verify Office open variable: %ls", WIXSTDBA_VARIABLE_VERIFY_OFFICE_OPEN_ERROR_MSG);
            hr = BalFormatString(sczUnformattedVerifyOfficeOpenMsg, &sczVerifyOfficeOpenMsg);
            BalExitOnFailure1(hr, "Failed to format verify Office open variable: %ls", sczUnformattedVerifyOfficeOpenMsg);
            ::MessageBoxW(m_hWnd, sczVerifyOfficeOpenMsg, m_pTheme->sczCaption, MB_OK | MB_ICONEXCLAMATION);
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "One or more Microsoft Office applications are opened and must be closed before setup continues.");

            return FALSE;
        }

    LExit:
        ReleaseStr(sczVerifyOfficeOpenMsg);
        ReleaseStr(sczUnformattedVerifyOfficeOpenMsg);

        return TRUE;
    }

    //
    // FindProcessId - helper function to detect if a process is running, returns true or false
    //
    DWORD FindProcessId(__in_z LPCTSTR lpcszFileName)
    {
        LPDWORD lpdwProcessIds;
        LPTSTR  lpszBaseName;
        HANDLE  hProcess;
        DWORD   i, cdwProcesses, dwProcessId = 0;

        lpdwProcessIds = (LPDWORD)HeapAlloc(GetProcessHeap(), 0, MAX_PROCESSES*sizeof(DWORD));
        if (lpdwProcessIds != NULL)
        {
            if (EnumProcesses(lpdwProcessIds, MAX_PROCESSES*sizeof(DWORD), &cdwProcesses))
            {
                lpszBaseName = (LPTSTR)HeapAlloc(GetProcessHeap(), 0, MAX_PATH*sizeof(TCHAR));
                if (lpszBaseName != NULL)
                {
                    cdwProcesses /= sizeof(DWORD);
                    for (i = 0; i < cdwProcesses; i++)
                    {
                        hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, lpdwProcessIds[i]);
                        if (hProcess != NULL)
                        {
                            if (GetModuleBaseName(hProcess, NULL, lpszBaseName, MAX_PATH) > 0)
                            {
                                if (!lstrcmpi(lpszBaseName, lpcszFileName))
                                {
                                    dwProcessId = lpdwProcessIds[i];
                                    CloseHandle(hProcess);
                                    break;
                                }
                            }
                            CloseHandle(hProcess);
                        }
                    }
                    HeapFree(GetProcessHeap(), 0, (LPVOID)lpszBaseName);
                }
            }
            HeapFree(GetProcessHeap(), 0, (LPVOID)lpdwProcessIds);
        }
        return dwProcessId;
    }


    //
    // DisplayVerifyRunExecutableMessage - if installing a specific feature/product ask the user if they want to run an executable
    //    In Bundle.wxs set the variable: VerifyRunExecutable to 1 to run this function
    //                  set the variable: VerifyRunExecutableErrorMsg (WIXSTDBA_VARIABLE_RUN_EXECUTABLE_ERROR_MSG) to some error message string
    //                  set the variable: RunExecutableVariable (WIXSTDBA_VARIABLE_RUN_EXECUTABLE) to the MSI property you will be using to run the EXE
    //
    void DisplayVerifyRunExecutableMessage()
    {
        HRESULT hr = S_OK;
        LPCWSTR sczRunExecutable = NULL;
        LPWSTR sczRunExecutableVariable = NULL;
        LPWSTR sczUnformattedRunExecutableVariable = NULL;
        LPWSTR sczVerifyRunExecutableErrorMsg = NULL;
        LPWSTR sczUnformattedVerifyRunExecutableErrorMsg = NULL;
        if (ThemeIsControlChecked(m_pTheme, WIXSTDBA_CONTROL_CHECKBOX_CLIENT3))
        {
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_RUN_EXECUTABLE, &sczUnformattedRunExecutableVariable);
            BalExitOnFailure1(hr, "Failed to get run executable variable: %ls", WIXSTDBA_VARIABLE_RUN_EXECUTABLE);
            hr = BalFormatString(sczUnformattedRunExecutableVariable, &sczRunExecutableVariable);
            BalExitOnFailure1(hr, "Failed to format run executable variable: %ls", sczUnformattedRunExecutableVariable);
            
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_RUN_EXECUTABLE_ERROR_MSG, &sczUnformattedVerifyRunExecutableErrorMsg);
            BalExitOnFailure1(hr, "Failed to get run executable error message variable: %ls", WIXSTDBA_VARIABLE_RUN_EXECUTABLE_ERROR_MSG);
            hr = BalFormatString(sczUnformattedVerifyRunExecutableErrorMsg, &sczVerifyRunExecutableErrorMsg);
            BalExitOnFailure1(hr, "Failed to format run executable error message variable: %ls", sczUnformattedVerifyRunExecutableErrorMsg);
            int nResult = ::MessageBoxW(m_hWnd, sczVerifyRunExecutableErrorMsg, m_pTheme->sczCaption, MB_YESNO | MB_ICONQUESTION);
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Asking if they want to run an executable after a client install.");

            if (nResult == IDYES)
            {
                sczRunExecutable = L"1";
            }
            else
            {
                sczRunExecutable = L"0";
            }

            hr = m_pEngine->SetVariableString(LPCWSTR(sczRunExecutableVariable), sczRunExecutable);

        }

    LExit:
        ReleaseStr(sczRunExecutableVariable);
        ReleaseStr(sczUnformattedRunExecutableVariable);
        ReleaseStr(sczVerifyRunExecutableErrorMsg);
        ReleaseStr(sczUnformattedVerifyRunExecutableErrorMsg);

        return;

    }

        
    //
    // DisplayVerifyClientNotInstallingInXPMessage - if installing a specific MSI and it can't execute on XP, warn user and then deselect checkbox
    //    In Bundle.wxs set the variable: VerifyClientNotInstallingInXP to 1 to run this function
    //                  set the variable: VerifyClientNotInstallingInXPErrorMsg (WIXSTDBA_VARIABLE_VERIFY_CLIENT_NOT_INSTALLING_IN_XP_ERROR_MSG) to some error message string
    //
    bool DisplayVerifyClientNotInstallingInXPMessage()
    {
        HRESULT hr = S_OK;
        LPWSTR sczVerifyClientNotInstallingInXPErrorMsg = NULL;
        LPWSTR sczUnformattedVerifyClientNotInstallingInXPErrorMsg = NULL;

        OSVERSIONINFO osvi;
        BOOL bIsWindowsXP;

        ZeroMemory(&osvi, sizeof(OSVERSIONINFO));
        osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

        GetVersionEx(&osvi);

        bIsWindowsXP = ( (osvi.dwMajorVersion == 5) && ((osvi.dwMinorVersion == 1) || (osvi.dwMinorVersion == 2)) );

        if (ThemeIsControlChecked(m_pTheme, WIXSTDBA_CONTROL_CHECKBOX_CLIENT3) && bIsWindowsXP)
        {
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_VERIFY_CLIENT_NOT_INSTALLING_IN_XP_ERROR_MSG, &sczUnformattedVerifyClientNotInstallingInXPErrorMsg);
            BalExitOnFailure1(hr, "Failed to get client cannot install in XP error message variable: %ls", WIXSTDBA_VARIABLE_VERIFY_CLIENT_NOT_INSTALLING_IN_XP_ERROR_MSG);
            hr = BalFormatString(sczUnformattedVerifyClientNotInstallingInXPErrorMsg, &sczVerifyClientNotInstallingInXPErrorMsg);
            BalExitOnFailure1(hr, "Failed to format run executable error message variable: %ls", sczUnformattedVerifyClientNotInstallingInXPErrorMsg);
            ::MessageBoxW(m_hWnd, sczVerifyClientNotInstallingInXPErrorMsg, m_pTheme->sczCaption, MB_OK | MB_ICONWARNING);
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Warning user client install cannot be installed in Windows XP.");

            return FALSE;

        }

    LExit:
        ReleaseStr(sczVerifyClientNotInstallingInXPErrorMsg);
        ReleaseStr(sczUnformattedVerifyClientNotInstallingInXPErrorMsg);

        return TRUE;

    }


    //
    // OnClickEulaLink - show the end user license agreement.
    //
    void OnClickEulaLink()
    {
        HRESULT hr = S_OK;
        LPWSTR sczLicenseUrl = NULL;
        LPWSTR sczLicensePath = NULL;
        LPWSTR sczLicenseDirectory = NULL;
        LPWSTR sczLicenseFilename = NULL;
        URI_PROTOCOL protocol = URI_PROTOCOL_UNKNOWN;

        hr = StrAllocString(&sczLicenseUrl, m_sczLicenseUrl, 0);
        BalExitOnFailure1(hr, "Failed to copy license URL: %ls", m_sczLicenseUrl);

        hr = LocLocalizeString(m_pWixLoc, &sczLicenseUrl);
        BalExitOnFailure1(hr, "Failed to localize license URL: %ls", m_sczLicenseUrl);

        hr = BalFormatString(sczLicenseUrl, &sczLicenseUrl);
        BalExitOnFailure1(hr, "Failed to get formatted license URL: %ls", m_sczLicenseUrl);

        hr = UriProtocol(sczLicenseUrl, &protocol);
        if (FAILED(hr) || URI_PROTOCOL_UNKNOWN == protocol)
        {
            // Probe for localized license file
            hr = PathRelativeToModule(&sczLicensePath, sczLicenseUrl, m_hModule);
            if (SUCCEEDED(hr))
            {
                hr = PathGetDirectory(sczLicensePath, &sczLicenseDirectory);
                if (SUCCEEDED(hr))
                {
                    hr = LocProbeForFile(sczLicenseDirectory, PathFile(sczLicenseUrl), m_sczLanguage, &sczLicensePath);
                }
            }
        }

        hr = ShelExec(sczLicensePath ? sczLicensePath : sczLicenseUrl, NULL, L"open", NULL, SW_SHOWDEFAULT, m_hWnd, NULL);
        BalExitOnFailure(hr, "Failed to launch URL to EULA.");

    LExit:
        ReleaseStr(sczLicensePath);
        ReleaseStr(sczLicenseUrl);
        ReleaseStr(sczLicenseDirectory);
        ReleaseStr(sczLicenseFilename);

        return;
    }


    //
    // OnClickLaunchButton - launch the app from the success page.
    //
    void OnClickLaunchButton()
    {
        HRESULT hr = S_OK;
        LPWSTR sczUnformattedLaunchTarget = NULL;
        LPWSTR sczLaunchTarget = NULL;
        LPWSTR sczUnformattedArguments = NULL;
        LPWSTR sczArguments = NULL;
        int nCmdShow = SW_SHOWNORMAL;

        hr = BalGetStringVariable(WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH, &sczUnformattedLaunchTarget);
        BalExitOnFailure1(hr, "Failed to get launch target variable '%ls'.", WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH);

        hr = BalFormatString(sczUnformattedLaunchTarget, &sczLaunchTarget);
        BalExitOnFailure1(hr, "Failed to format launch target variable: %ls", sczUnformattedLaunchTarget);

        if (BalStringVariableExists(WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS))
        {
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS, &sczUnformattedArguments);
            BalExitOnFailure1(hr, "Failed to get launch arguments '%ls'.", WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS);

            hr = BalFormatString(sczUnformattedArguments, &sczArguments);
            BalExitOnFailure1(hr, "Failed to format launch arguments variable: %ls", sczUnformattedArguments);
        }

        if (BalStringVariableExists(WIXSTDBA_VARIABLE_LAUNCH_HIDDEN))
        {
            nCmdShow = SW_HIDE;
        }

        hr = ShelExec(sczLaunchTarget, sczArguments, L"open", NULL, nCmdShow, m_hWnd, NULL);
        BalExitOnFailure1(hr, "Failed to launch target: %ls", sczLaunchTarget);

        ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);

    LExit:
        ReleaseStr(sczArguments);
        ReleaseStr(sczUnformattedArguments);
        ReleaseStr(sczLaunchTarget);
        ReleaseStr(sczUnformattedLaunchTarget);

        return;
    }


    //
    // OnClickRestartButton - allows the restart and closes the app.
    //
    void OnClickRestartButton()
    {
        AssertSz(m_fRestartRequired, "Restart must be requested to be able to click on the restart button.");

        m_fAllowRestart = TRUE;
        ::SendMessageW(m_hWnd, WM_CLOSE, 0, 0);

        return;
    }


    //
    // OnClickLogFileLink - show the log file.
    //
    void OnClickLogFileLink()
    {
        HRESULT hr = S_OK;
        LPWSTR sczLogFile = NULL;

        hr = BalGetStringVariable(m_Bundle.sczLogVariable, &sczLogFile);
        BalExitOnFailure1(hr, "Failed to get log file variable '%ls'.", m_Bundle.sczLogVariable);

        hr = ShelExec(L"notepad.exe", sczLogFile, L"open", NULL, SW_SHOWDEFAULT, m_hWnd, NULL);
        BalExitOnFailure1(hr, "Failed to open log file target: %ls", sczLogFile);

    LExit:
        ReleaseStr(sczLogFile);

        return;
    }


    //
    // SetState
    //
    void SetState(
        __in WIXSTDBA_STATE state,
        __in HRESULT hrStatus
        )
    {
        if (FAILED(hrStatus))
        {
            m_hrFinal = hrStatus;
        }

        if (FAILED(m_hrFinal))
        {
            state = WIXSTDBA_STATE_FAILED;
        }

        if (WIXSTDBA_STATE_OPTIONS == state || m_state < state)
        {
            ::PostMessageW(m_hWnd, WM_WIXSTDBA_CHANGE_STATE, 0, state);
        }
    }


    void DeterminePageId(
        __in WIXSTDBA_STATE state,
        __out DWORD* pdwPageId
        )
    {
        if (BOOTSTRAPPER_DISPLAY_PASSIVE == m_command.display)
        {
            switch (state)
            {
            case WIXSTDBA_STATE_INITIALIZED:
                *pdwPageId = BOOTSTRAPPER_ACTION_HELP == m_command.action ? m_rgdwPageIds[WIXSTDBA_PAGE_HELP] : m_rgdwPageIds[WIXSTDBA_PAGE_LOADING];
                break;

            case WIXSTDBA_STATE_HELP:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_HELP];
                break;

            case WIXSTDBA_STATE_DETECTING:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_LOADING] ? m_rgdwPageIds[WIXSTDBA_PAGE_LOADING] : m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] ? m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] : m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS];
                break;

            case WIXSTDBA_STATE_DETECTED: __fallthrough;
            case WIXSTDBA_STATE_PLANNING: __fallthrough;
            case WIXSTDBA_STATE_PLANNED: __fallthrough;
            case WIXSTDBA_STATE_APPLYING: __fallthrough;
            case WIXSTDBA_STATE_CACHING: __fallthrough;
            case WIXSTDBA_STATE_CACHED: __fallthrough;
            case WIXSTDBA_STATE_EXECUTING: __fallthrough;
            case WIXSTDBA_STATE_EXECUTED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] ? m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] : m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS];
                break;

            default:
                *pdwPageId = 0;
                break;
            }
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL == m_command.display)
        {
            switch (state)
            {
            case WIXSTDBA_STATE_INITIALIZING:
                *pdwPageId = 0;
                break;

            case WIXSTDBA_STATE_INITIALIZED:
                *pdwPageId = BOOTSTRAPPER_ACTION_HELP == m_command.action ? m_rgdwPageIds[WIXSTDBA_PAGE_HELP] : m_rgdwPageIds[WIXSTDBA_PAGE_LOADING];
                break;

            case WIXSTDBA_STATE_HELP:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_HELP];
                break;

            case WIXSTDBA_STATE_DETECTING:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_LOADING];
                break;

            case WIXSTDBA_STATE_DETECTED:
                switch (m_command.action)
                {
                case BOOTSTRAPPER_ACTION_INSTALL:
                    *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_INSTALL];
                    break;

                case BOOTSTRAPPER_ACTION_MODIFY: __fallthrough;
                case BOOTSTRAPPER_ACTION_REPAIR: __fallthrough;
                case BOOTSTRAPPER_ACTION_UNINSTALL:
                    *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_MODIFY];
                    break;
                }
                break;

            case WIXSTDBA_STATE_OPTIONS:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_OPTIONS];
                break;

            case WIXSTDBA_STATE_PLANNING: __fallthrough;
            case WIXSTDBA_STATE_PLANNED: __fallthrough;
            case WIXSTDBA_STATE_APPLYING: __fallthrough;
            case WIXSTDBA_STATE_CACHING: __fallthrough;
            case WIXSTDBA_STATE_CACHED: __fallthrough;
            case WIXSTDBA_STATE_EXECUTING: __fallthrough;
            case WIXSTDBA_STATE_EXECUTED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS];
                break;

            case WIXSTDBA_STATE_APPLIED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_SUCCESS];
                break;

            case WIXSTDBA_STATE_FAILED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_FAILURE];
                break;
            }
        }
    }


    HRESULT EvaluateConditions()
    {
        HRESULT hr = S_OK;
        BOOL fResult = FALSE;

        for (DWORD i = 0; i < m_Conditions.cConditions; ++i)
        {
            BAL_CONDITION* pCondition = m_Conditions.rgConditions + i;

            hr = BalConditionEvaluate(pCondition, m_pEngine, &fResult, &m_sczFailedMessage);
            BalExitOnFailure(hr, "Failed to evaluate condition.");

            if (!fResult)
            {
                hr = E_WIXSTDBA_CONDITION_FAILED;
                BalExitOnFailure1(hr, "Bundle condition evaluated to false: %ls", pCondition->sczCondition);
            }
        }

        ReleaseNullStr(m_sczFailedMessage);

    LExit:
        return hr;
    }


    void SetTaskbarButtonProgress(
        __in DWORD dwOverallPercentage
        )
    {
        HRESULT hr = S_OK;

        if (m_fTaskbarButtonOK)
        {
            hr = m_pTaskbarList->SetProgressValue(m_hWnd, dwOverallPercentage, 100UL);
            BalExitOnFailure1(hr, "Failed to set taskbar button progress to: %d%%.", dwOverallPercentage);
        }

    LExit:
        return;
    }


    void SetTaskbarButtonState(
        __in TBPFLAG tbpFlags
        )
    {
        HRESULT hr = S_OK;

        if (m_fTaskbarButtonOK)
        {
            hr = m_pTaskbarList->SetProgressState(m_hWnd, tbpFlags);
            BalExitOnFailure1(hr, "Failed to set taskbar button state.", tbpFlags);
        }

    LExit:
        return;
    }


    void SetProgressState(
        __in HRESULT hrStatus
        )
    {
        TBPFLAG flag = TBPF_NORMAL;

        if (IsCanceled() || HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) == hrStatus)
        {
            flag = TBPF_PAUSED;
        }
        else if (IsRollingBack() || FAILED(hrStatus))
        {
            flag = TBPF_ERROR;
        }

        SetTaskbarButtonState(flag);
    }


    HRESULT LoadBootstrapperBAFunctions()
    {
        HRESULT hr = S_OK;
        LPWSTR sczBafPath = NULL;

        hr = PathRelativeToModule(&sczBafPath, L"bafunctions.dll", m_hModule);
        BalExitOnFailure(hr, "Failed to get path to BA function DLL.");

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: LoadBootstrapperBAFunctions() - BA function DLL %ls", sczBafPath);
#endif

        m_hBAFModule = ::LoadLibraryW(sczBafPath);
        if (m_hBAFModule)
        {
            PFN_BOOTSTRAPPER_BA_FUNCTION_CREATE pfnBAFunctionCreate = reinterpret_cast<PFN_BOOTSTRAPPER_BA_FUNCTION_CREATE>(::GetProcAddress(m_hBAFModule, "CreateBootstrapperBAFunction"));
            BalExitOnNullWithLastError1(pfnBAFunctionCreate, hr, "Failed to get CreateBootstrapperBAFunction entry-point from: %ls", sczBafPath);

            hr = pfnBAFunctionCreate(m_pEngine, m_hBAFModule, &m_pBAFunction);
            BalExitOnFailure(hr, "Failed to create BA function.");
        }
#ifdef DEBUG
        else
        {
            BalLogError(HRESULT_FROM_WIN32(::GetLastError()), "WIXSTDBA: LoadBootstrapperBAFunctions() - Failed to load DLL %ls", sczBafPath);
        }
#endif

    LExit:
        if (m_hBAFModule && !m_pBAFunction)
        {
            ::FreeLibrary(m_hBAFModule);
            m_hBAFModule = NULL;
        }
        ReleaseStr(sczBafPath);

        return hr;
    }


    void SavePageSettings(
        __in WIXSTDBA_PAGE page
        )
    {
        THEME_PAGE* pPage = NULL;

        pPage = ThemeGetPage(m_pTheme, m_rgdwPageIds[page]);
        if (pPage)
        {
            for (DWORD i = 0; i < pPage->cControlIndices; ++i)
            {
                // Loop through all the checkbox controls (or buttons with BS_AUTORADIOBUTTON) with names and set a Burn variable with that name to true or false.
                THEME_CONTROL* pControl = m_pTheme->rgControls + pPage->rgdwControlIndices[i];
                if ((THEME_CONTROL_TYPE_CHECKBOX == pControl->type) ||
                    (THEME_CONTROL_TYPE_BUTTON == pControl->type && (BS_AUTORADIOBUTTON == (BS_AUTORADIOBUTTON & pControl->dwStyle)) &&
                    pControl->sczName && *pControl->sczName))
                {
                    BOOL bChecked = ThemeIsControlChecked(m_pTheme, pControl->wId);
                    m_pEngine->SetVariableNumeric(pControl->sczName, bChecked ? 1 : 0);
                }

                // Loop through all the editbox controls with names and set a Burn variable with that name to the contents.
                if (THEME_CONTROL_TYPE_EDITBOX == pControl->type && pControl->sczName && *pControl->sczName && WIXSTDBA_CONTROL_FOLDER_EDITBOX != pControl->wId)
                {
                    LPWSTR sczValue = NULL;
                    ThemeGetTextControl(m_pTheme, pControl->wId, &sczValue);
                    m_pEngine->SetVariableString(pControl->sczName, sczValue);
                }
            }
        }
    }


public:
    //
    // Constructor - initialize member variables.
    //
    CWixStandardBootstrapperApplication(
        __in HMODULE hModule,
        __in BOOL fPrereq,
        __in IBootstrapperEngine* pEngine,
        __in const BOOTSTRAPPER_COMMAND* pCommand
        ) : CBalBaseBootstrapperApplication(pEngine, pCommand, 3, 3000)
    {
        m_hModule = hModule;
        memcpy_s(&m_command, sizeof(m_command), pCommand, sizeof(BOOTSTRAPPER_COMMAND));

        // Pre-req BA should only show help or do an install (to launch the Managed BA which can then do the right action).
        if (fPrereq && BOOTSTRAPPER_ACTION_HELP != m_command.action && BOOTSTRAPPER_ACTION_INSTALL != m_command.action)
        {
            m_command.action = BOOTSTRAPPER_ACTION_INSTALL;
        }
        else // maybe modify the action state if the bundle is or is not already installed.
        {
            LONGLONG llInstalled = 0;
            HRESULT hr = BalGetNumericVariable(L"WixBundleInstalled", &llInstalled);
            if (SUCCEEDED(hr) && BOOTSTRAPPER_RESUME_TYPE_REBOOT != m_command.resumeType && 0 < llInstalled && BOOTSTRAPPER_ACTION_INSTALL == m_command.action)
            {
                m_command.action = BOOTSTRAPPER_ACTION_MODIFY;
            }
            else if (0 == llInstalled && (BOOTSTRAPPER_ACTION_MODIFY == m_command.action || BOOTSTRAPPER_ACTION_REPAIR == m_command.action))
            {
                m_command.action = BOOTSTRAPPER_ACTION_INSTALL;
            }
        }

        m_plannedAction = BOOTSTRAPPER_ACTION_UNKNOWN;

        // When resuming from restart doing some install-like operation, try to find the package that forced the
        // restart. We'll use this information during planning.
        m_sczAfterForcedRestartPackage = NULL;

        if (BOOTSTRAPPER_RESUME_TYPE_REBOOT == m_command.resumeType && BOOTSTRAPPER_ACTION_UNINSTALL < m_command.action)
        {
            // Ensure the forced restart package variable is null when it is an empty string.
            HRESULT hr = BalGetStringVariable(L"WixBundleForcedRestartPackage", &m_sczAfterForcedRestartPackage);
            if (FAILED(hr) || !m_sczAfterForcedRestartPackage || !*m_sczAfterForcedRestartPackage)
            {
                ReleaseNullStr(m_sczAfterForcedRestartPackage);
            }
        }

        m_pWixLoc = NULL;
        memset(&m_Bundle, 0, sizeof(m_Bundle));
        memset(&m_Conditions, 0, sizeof(m_Conditions));
        m_sczConfirmCloseMessage = NULL;
        m_sczFailedMessage = NULL;

        m_sczLanguage = NULL;
        m_pTheme = NULL;
        memset(m_rgdwPageIds, 0, sizeof(m_rgdwPageIds));
        m_hUiThread = NULL;
        m_fRegistered = FALSE;
        m_hWnd = NULL;

        m_state = WIXSTDBA_STATE_INITIALIZING;
        m_hrFinal = S_OK;

        m_fDowngrading = FALSE;
        m_restartResult = BOOTSTRAPPER_APPLY_RESTART_NONE;
        m_fRestartRequired = FALSE;
        m_fAllowRestart = FALSE;

        m_sczLicenseFile = NULL;
        m_sczLicenseUrl = NULL;
        m_fSuppressOptionsUI = FALSE;
        m_fSuppressDowngradeFailure = FALSE;
        m_fSuppressRepair = FALSE;
        m_fShowVersion = FALSE;

        m_sdOverridableVariables = NULL;
        m_pTaskbarList = NULL;
        m_uTaskbarButtonCreatedMessage = UINT_MAX;
        m_fTaskbarButtonOK = FALSE;
        m_fShowingInternalUiThisPackage = FALSE;

        m_fPrereq = fPrereq;
        m_sczPrereqPackage = NULL;
        m_fPrereqInstalled = FALSE;
        m_fPrereqAlreadyInstalled = FALSE;

        pEngine->AddRef();
        m_pEngine = pEngine;

        m_hBAFModule = NULL;
        m_pBAFunction = NULL;
    }


    //
    // Destructor - release member variables.
    //
    ~CWixStandardBootstrapperApplication()
    {
        AssertSz(!::IsWindow(m_hWnd), "Window should have been destroyed before destructor.");
        AssertSz(!m_pTheme, "Theme should have been released before destructor.");

        ReleaseObject(m_pTaskbarList);
        ReleaseDict(m_sdOverridableVariables);
        ReleaseStr(m_sczFailedMessage);
        ReleaseStr(m_sczConfirmCloseMessage);
        BalConditionsUninitialize(&m_Conditions);
        BalInfoUninitialize(&m_Bundle);
        LocFree(m_pWixLoc);

        ReleaseStr(m_sczLanguage);
        ReleaseStr(m_sczLicenseFile);
        ReleaseStr(m_sczLicenseUrl);
        ReleaseStr(m_sczPrereqPackage);
        ReleaseStr(m_sczAfterForcedRestartPackage);
        ReleaseNullObject(m_pEngine);

        if (m_hBAFModule)
        {
            ::FreeLibrary(m_hBAFModule);
            m_hBAFModule = NULL;
        }
    }

private:
    HMODULE m_hModule;
    BOOTSTRAPPER_COMMAND m_command;
    IBootstrapperEngine* m_pEngine;
    BOOTSTRAPPER_ACTION m_plannedAction;

    LPWSTR m_sczAfterForcedRestartPackage;

    WIX_LOCALIZATION* m_pWixLoc;
    BAL_INFO_BUNDLE m_Bundle;
    BAL_CONDITIONS m_Conditions;
    LPWSTR m_sczFailedMessage;
    LPWSTR m_sczConfirmCloseMessage;

    LPWSTR m_sczLanguage;
    THEME* m_pTheme;
    DWORD m_rgdwPageIds[countof(vrgwzPageNames)];
    HANDLE m_hUiThread;
    BOOL m_fRegistered;
    HWND m_hWnd;

    WIXSTDBA_STATE m_state;
    WIXSTDBA_STATE m_stateBeforeOptions;
    HRESULT m_hrFinal;

    BOOL m_fStartedExecution;
    DWORD m_dwCalculatedCacheProgress;
    DWORD m_dwCalculatedExecuteProgress;

    BOOL m_fDowngrading;
    BOOTSTRAPPER_APPLY_RESTART m_restartResult;
    BOOL m_fRestartRequired;
    BOOL m_fAllowRestart;

    LPWSTR m_sczLicenseFile;
    LPWSTR m_sczLicenseUrl;
    BOOL m_fSuppressOptionsUI;
    BOOL m_fSuppressDowngradeFailure;
    BOOL m_fSuppressRepair;
    BOOL m_fShowVersion;

    STRINGDICT_HANDLE m_sdOverridableVariables;

    BOOL m_fPrereq;
    LPWSTR m_sczPrereqPackage;
    BOOL m_fPrereqInstalled;
    BOOL m_fPrereqAlreadyInstalled;

    ITaskbarList3* m_pTaskbarList;
    UINT m_uTaskbarButtonCreatedMessage;
    BOOL m_fTaskbarButtonOK;
    BOOL m_fShowingInternalUiThisPackage;

    HMODULE m_hBAFModule;
    IBootstrapperBAFunction* m_pBAFunction;
};


//
// CreateUserExperience - creates a new IBurnUserExperience object.
//
HRESULT CreateBootstrapperApplication(
    __in HMODULE hModule,
    __in BOOL fPrereq,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_COMMAND* pCommand,
    __out IBootstrapperApplication** ppApplication
    )
{
    HRESULT hr = S_OK;
    CWixStandardBootstrapperApplication* pApplication = NULL;

    pApplication = new CWixStandardBootstrapperApplication(hModule, fPrereq, pEngine, pCommand);
    ExitOnNull(pApplication, hr, E_OUTOFMEMORY, "Failed to create new standard bootstrapper application object.");

    *ppApplication = pApplication;
    pApplication = NULL;

LExit:
    ReleaseObject(pApplication);
    return hr;
}
