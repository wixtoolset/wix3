#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


enum BURN_MODE;
enum BURN_DEPENDENCY_REGISTRATION_ACTION;
struct _BURN_LOGGING;
typedef _BURN_LOGGING BURN_LOGGING;

// constants

const LPCWSTR BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH = L"BundleCachePath";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE = L"BundleAddonCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE = L"BundleDetectCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE = L"BundlePatchCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = L"BundleUpgradeCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME = L"DisplayName";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION = L"BundleVersion";
const LPCWSTR BURN_REGISTRATION_REGISTRY_ENGINE_VERSION = L"EngineVersion";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = L"BundleProviderKey";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_TAG = L"BundleTag";

enum BURN_RESUME_MODE
{
    BURN_RESUME_MODE_NONE,
    BURN_RESUME_MODE_ACTIVE,
    BURN_RESUME_MODE_SUSPEND,
    BURN_RESUME_MODE_ARP,
    BURN_RESUME_MODE_REBOOT_PENDING,
};

enum BURN_REGISTRATION_MODIFY_TYPE
{
    BURN_REGISTRATION_MODIFY_ENABLED,
    BURN_REGISTRATION_MODIFY_DISABLE,
    BURN_REGISTRATION_MODIFY_DISABLE_BUTTON,
};


// structs

typedef struct _BURN_UPDATE_REGISTRATION
{
    BOOL fRegisterUpdate;
    LPWSTR sczManufacturer;
    LPWSTR sczDepartment;
    LPWSTR sczProductFamily;
    LPWSTR sczName;
    LPWSTR sczClassification;
} BURN_UPDATE_REGISTRATION;

typedef struct _BURN_RELATED_BUNDLE
{
    BOOTSTRAPPER_RELATION_TYPE relationType;

    DWORD64 qwVersion;
    LPWSTR sczTag;

    BURN_PACKAGE package;
} BURN_RELATED_BUNDLE;

typedef struct _BURN_RELATED_BUNDLES
{
    BURN_RELATED_BUNDLE* rgRelatedBundles;
    DWORD cRelatedBundles;
} BURN_RELATED_BUNDLES;

typedef struct _BURN_SOFTWARE_TAG
{
    LPWSTR sczFilename;
    LPWSTR sczRegid;
    LPWSTR sczPath;
    LPSTR sczTag;
} BURN_SOFTWARE_TAG;

typedef struct _BURN_SOFTWARE_TAGS
{
    BURN_SOFTWARE_TAG* rgSoftwareTags;
    DWORD cSoftwareTags;
} BURN_SOFTWARE_TAGS;

typedef struct _BURN_REGISTRATION
{
    BOOL fPerMachine;
    BOOL fRegisterArp;
    BOOL fDisableResume;
    BOOL fInstalled;
    LPWSTR sczId;
    LPWSTR sczTag;

    LPWSTR *rgsczDetectCodes;
    DWORD cDetectCodes;

    LPWSTR *rgsczUpgradeCodes;
    DWORD cUpgradeCodes;

    LPWSTR *rgsczAddonCodes;
    DWORD cAddonCodes;

    LPWSTR *rgsczPatchCodes;
    DWORD cPatchCodes;

    DWORD64 qwVersion;
    LPWSTR sczActiveParent;
    LPWSTR sczProviderKey;
    LPWSTR sczExecutableName;

    // paths
    HKEY hkRoot;
    LPWSTR sczRegistrationKey;
    LPWSTR sczCacheExecutablePath;
    LPWSTR sczResumeCommandLine;
    LPWSTR sczStateFile;

    // ARP registration
    LPWSTR sczDisplayName;
    LPWSTR sczDisplayVersion;
    LPWSTR sczPublisher;
    LPWSTR sczHelpLink;
    LPWSTR sczHelpTelephone;
    LPWSTR sczAboutUrl;
    LPWSTR sczUpdateUrl;
    LPWSTR sczParentDisplayName;
    LPWSTR sczComments;
    //LPWSTR sczReadme; // TODO: this would be a file path
    LPWSTR sczContact;
    //DWORD64 qwEstimatedSize; // TODO: size should come from disk cost calculation
    BURN_REGISTRATION_MODIFY_TYPE modify;
    BOOL fNoRemoveDefined;
    BOOL fNoRemove;

    BURN_SOFTWARE_TAGS softwareTags;

    // Update registration
    BURN_UPDATE_REGISTRATION update;

    // Only valid after detect.
    BURN_RELATED_BUNDLES relatedBundles;

    LPWSTR sczDetectedProviderKeyBundleId;
    LPWSTR sczAncestors;

    BOOL fEnabledForwardCompatibleBundle;
    BURN_PACKAGE forwardCompatibleBundle;
} BURN_REGISTRATION;


// functions

HRESULT RegistrationParseFromXml(
    __in BURN_REGISTRATION* pRegistration,
    __in IXMLDOMNode* pixnBundle
    );
void RegistrationUninitialize(
    __in BURN_REGISTRATION* pRegistration
    );
HRESULT RegistrationSetVariables(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    );
HRESULT RegistrationDetectInstalled(
    __in BURN_REGISTRATION* pRegistration,
    __out BOOL* pfInstalled
    );
HRESULT RegistrationDetectResumeType(
    __in BURN_REGISTRATION* pRegistration,
    __out BOOTSTRAPPER_RESUME_TYPE* pResumeType
    );
HRESULT RegistrationDetectRelatedBundles(
    __in BURN_REGISTRATION* pRegistration
    );
HRESULT RegistrationSessionBegin(
    __in_z LPCWSTR wzEngineWorkingPath,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwRegistrationOptions,
    __in BURN_DEPENDENCY_REGISTRATION_ACTION dependencyRegistrationAction,
    __in DWORD64 qwEstimatedSize
    );
HRESULT RegistrationSessionResume(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    );
HRESULT RegistrationSessionEnd(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BURN_RESUME_MODE resumeMode,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __in BURN_DEPENDENCY_REGISTRATION_ACTION dependencyRegistrationAction
    );
HRESULT RegistrationSaveState(
    __in BURN_REGISTRATION* pRegistration,
    __in_bcount_opt(cbBuffer) BYTE* pbBuffer,
    __in_opt DWORD cbBuffer
    );
HRESULT RegistrationLoadState(
    __in BURN_REGISTRATION* pRegistration,
    __out_bcount(*pcbBuffer) BYTE** ppbBuffer,
    __out DWORD* pcbBuffer
    );
HRESULT RegistrationGetResumeCommandLine(
    __in const BURN_REGISTRATION* pRegistration,
    __deref_out_z LPWSTR* psczResumeCommandLine
    );


#if defined(__cplusplus)
}
#endif
