#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

const DWORD MB_RETRYTRYAGAIN = 0xF;


// structs

typedef struct _BURN_USER_EXPERIENCE
{
    BOOL fSplashScreen;
    BURN_PAYLOADS payloads;

    HMODULE hUXModule;
    IBootstrapperApplication* pUserExperience;
    LPWSTR sczTempDirectory;

    CRITICAL_SECTION csEngineActive;    // Changing the engine active state in the user experience must be
                                        // syncronized through this critical section.
                                        // Note: The engine must never do a UX callback while in this critical section.

    BOOL fEngineActive;                 // Indicates that the engine is currently active with one of the execution
                                        // steps (detect, plan, apply), and cannot accept requests from the UX.
                                        // This flag should be cleared by the engine prior to UX callbacks that
                                        // allows altering of the engine state.

    HRESULT hrApplyError;               // Tracks is an error occurs during apply that requires the cache or
                                        // execute threads to bail.

    HWND hwndApply;                     // The window handle provided at the beginning of Apply(). Only valid
                                        // during apply.

    HWND hwndDetect;                    // The window handle provided at the beginning of Detect(). Only valid
                                        // during Detect.

    DWORD dwExitCode;                   // Exit code returned by the user experience for the engine overall.
} BURN_USER_EXPERIENCE;


// functions

HRESULT UserExperienceParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
    );
void UserExperienceUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceLoad(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IBootstrapperEngine* pEngine,
    __in BOOTSTRAPPER_COMMAND* pCommand
    );
HRESULT UserExperienceUnload(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceEnsureWorkingFolder(
    __in LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
    );
HRESULT UserExperienceRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
int UserExperienceSendError(
    __in IBootstrapperApplication* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
    );
HRESULT UserExperienceActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out_opt BOOL* pfActivated
    );
void UserExperienceDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
void UserExperienceExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
void UserExperienceExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    );
HRESULT UserExperienceInterpretResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
int UserExperienceCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
HRESULT UserExperienceInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
#if defined(__cplusplus)
}
#endif
