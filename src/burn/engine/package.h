#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

struct _BURN_RELATED_BUNDLES;
typedef _BURN_RELATED_BUNDLES BURN_RELATED_BUNDLES;

struct _BURN_PACKAGE;
typedef _BURN_PACKAGE BURN_PACKAGE;

// constants

enum BURN_EXE_EXIT_CODE_TYPE
{
    BURN_EXE_EXIT_CODE_TYPE_NONE,
    BURN_EXE_EXIT_CODE_TYPE_SUCCESS,
    BURN_EXE_EXIT_CODE_TYPE_ERROR,
    BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT,
    BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT,
};

enum BURN_EXE_PROTOCOL_TYPE
{
    BURN_EXE_PROTOCOL_TYPE_NONE,
    BURN_EXE_PROTOCOL_TYPE_BURN,
    BURN_EXE_PROTOCOL_TYPE_NETFX4,
};

enum BURN_PACKAGE_TYPE
{
    BURN_PACKAGE_TYPE_NONE,
    BURN_PACKAGE_TYPE_EXE,
    BURN_PACKAGE_TYPE_MSI,
    BURN_PACKAGE_TYPE_MSP,
    BURN_PACKAGE_TYPE_MSU,
};

enum BURN_CACHE_STATE
{
    BURN_CACHE_STATE_NONE,
    BURN_CACHE_STATE_PARTIAL,
    BURN_CACHE_STATE_COMPLETE,
};

enum BURN_CACHE_TYPE
{
    BURN_CACHE_TYPE_NO,
    BURN_CACHE_TYPE_YES,
    BURN_CACHE_TYPE_ALWAYS,
};

enum BURN_DEPENDENCY_ACTION
{
    BURN_DEPENDENCY_ACTION_NONE,
    BURN_DEPENDENCY_ACTION_REGISTER,
    BURN_DEPENDENCY_ACTION_UNREGISTER,
};

enum BURN_PATCH_TARGETCODE_TYPE
{
    BURN_PATCH_TARGETCODE_TYPE_UNKNOWN,
    BURN_PATCH_TARGETCODE_TYPE_PRODUCT,
    BURN_PATCH_TARGETCODE_TYPE_UPGRADE,
};

// structs

typedef struct _BURN_EXE_EXIT_CODE
{
    BURN_EXE_EXIT_CODE_TYPE type;
    DWORD dwCode;
    BOOL fWildcard;
} BURN_EXE_EXIT_CODE;

typedef struct _BURN_EXE_COMMAND_LINE_ARGUMENT
{
    LPWSTR sczInstallArgument;
    LPWSTR sczUninstallArgument;
    LPWSTR sczRepairArgument;
    LPWSTR sczCondition;
} BURN_EXE_COMMAND_LINE_ARGUMENT;

typedef struct _BURN_MSPTARGETPRODUCT
{
    MSIINSTALLCONTEXT context;
    DWORD dwOrder;
    WCHAR wzTargetProductCode[39];
    BURN_PACKAGE* pChainedTargetPackage;
    BOOL fSlipstream;

    BOOTSTRAPPER_PACKAGE_STATE patchPackageState; // only valid after Detect.
    BOOTSTRAPPER_ACTION_STATE execute;            // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE rollback;           // only valid during Plan.
} BURN_MSPTARGETPRODUCT;

typedef struct _BURN_MSIPROPERTY
{
    LPWSTR sczId;
    LPWSTR sczValue; // used during forward execution
    LPWSTR sczRollbackValue;  // used during rollback
} BURN_MSIPROPERTY;

typedef struct _BURN_MSIFEATURE
{
    LPWSTR sczId;
    LPWSTR sczAddLocalCondition;
    LPWSTR sczAddSourceCondition;
    LPWSTR sczAdvertiseCondition;
    LPWSTR sczRollbackAddLocalCondition;
    LPWSTR sczRollbackAddSourceCondition;
    LPWSTR sczRollbackAdvertiseCondition;

    BOOTSTRAPPER_FEATURE_STATE currentState;   // only valid after Detect.
    BOOTSTRAPPER_FEATURE_ACTION execute;       // only valid during Plan.
    BOOTSTRAPPER_FEATURE_ACTION rollback;      // only valid during Plan.
} BURN_MSIFEATURE;

typedef struct _BURN_RELATED_MSI
{
    LPWSTR sczUpgradeCode;
    DWORD64 qwMinVersion;
    DWORD64 qwMaxVersion;
    BOOL fMinProvided;
    BOOL fMaxProvided;
    BOOL fMinInclusive;
    BOOL fMaxInclusive;
    BOOL fOnlyDetect;
    BOOL fLangInclusive;

    DWORD* rgdwLanguages;
    DWORD cLanguages;
} BURN_RELATED_MSI;

typedef struct _BURN_PACKAGE_PAYLOAD
{
    BURN_PAYLOAD* pPayload;
    BOOL fCached;
} BURN_PACKAGE_PAYLOAD;

typedef struct _BURN_DEPENDENCY_PROVIDER
{
    LPWSTR sczKey;
    LPWSTR sczVersion;
    LPWSTR sczDisplayName;
    BOOL fImported;
} BURN_DEPENDENCY_PROVIDER;

typedef struct _BURN_ROLLBACK_BOUNDARY
{
    LPWSTR sczId;
    BOOL fVital;
} BURN_ROLLBACK_BOUNDARY;

typedef struct _BURN_PATCH_TARGETCODE
{
    LPWSTR sczTargetCode;
    BURN_PATCH_TARGETCODE_TYPE type;
} BURN_PATCH_TARGETCODE;

typedef struct _BURN_PACKAGE
{
    LPWSTR sczId;

    LPWSTR sczLogPathVariable;          // name of the variable that will be set to the log path.
    LPWSTR sczRollbackLogPathVariable;  // name of the variable that will be set to the rollback path.

    LPWSTR sczInstallCondition;
    LPWSTR sczRollbackInstallCondition;
    BOOL fPerMachine;
    BOOL fUninstallable;
    BOOL fVital;

    BURN_CACHE_TYPE cacheType;
    LPWSTR sczCacheId;

    DWORD64 qwInstallSize;
    DWORD64 qwSize;

    BURN_ROLLBACK_BOUNDARY* pRollbackBoundaryForward;  // used during install and repair.
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundaryBackward; // used during uninstall.

    BOOTSTRAPPER_PACKAGE_STATE currentState;    // only valid after Detect.
    BURN_CACHE_STATE cache;                     // only valid after Detect.
    BOOTSTRAPPER_PACKAGE_STATE expected;        // only valid during Plan.
    BOOTSTRAPPER_REQUEST_STATE defaultRequested;// only valid during Plan.
    BOOTSTRAPPER_REQUEST_STATE requested;       // only valid during Plan.
    BOOL fAcquire;                              // only valid during Plan.
    BOOL fUncache;                              // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE execute;          // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE rollback;         // only valid during Plan.
    BURN_DEPENDENCY_ACTION providerExecute;     // only valid during Plan.
    BURN_DEPENDENCY_ACTION providerRollback;    // only valid during Plan.
    BURN_DEPENDENCY_ACTION dependencyExecute;   // only valid during Plan.
    BURN_DEPENDENCY_ACTION dependencyRollback;  // only valid during Plan.
    BOOL fDependencyManagerWasHere;             // only valid during Plan.
    HRESULT hrCacheResult;                      // only valid during Apply.

    BURN_PACKAGE_PAYLOAD* rgPayloads;
    DWORD cPayloads;

    BURN_DEPENDENCY_PROVIDER* rgDependencyProviders;
    DWORD cDependencyProviders;

    BURN_PACKAGE_TYPE type;
    union
    {
        struct
        {
            LPWSTR sczDetectCondition;
            LPWSTR sczInstallArguments;
            LPWSTR sczRepairArguments;
            LPWSTR sczUninstallArguments;
            LPWSTR sczIgnoreDependencies;
            LPWSTR sczAncestors;

            BOOL fPseudoBundle;

            BOOL fRepairable;
            BURN_EXE_PROTOCOL_TYPE protocol;

            BOOL fSupportsAncestors;

            BURN_EXE_EXIT_CODE* rgExitCodes;
            DWORD cExitCodes;

            BURN_EXE_COMMAND_LINE_ARGUMENT* rgCommandLineArguments;
            DWORD cCommandLineArguments;
        } Exe;
        struct
        {
            LPWSTR sczProductCode;
            DWORD dwLanguage;
            DWORD64 qwVersion;
            LPWSTR sczInstalledProductCode;
            DWORD64 qwInstalledVersion;
            BOOL fDisplayInternalUI;

            BURN_MSIPROPERTY* rgProperties;
            DWORD cProperties;

            BURN_MSIFEATURE* rgFeatures;
            DWORD cFeatures;

            BURN_RELATED_MSI* rgRelatedMsis;
            DWORD cRelatedMsis;

            _BURN_PACKAGE** rgpSlipstreamMspPackages;
            LPWSTR* rgsczSlipstreamMspPackageIds;
            DWORD cSlipstreamMspPackages;

            BOOL fCompatibleInstalled;
        } Msi;
        struct
        {
            LPWSTR sczPatchCode;
            LPWSTR sczApplicabilityXml;
            BOOL fDisplayInternalUI;

            BURN_MSIPROPERTY* rgProperties;
            DWORD cProperties;

            BURN_MSPTARGETPRODUCT* rgTargetProducts;
            DWORD cTargetProductCodes;
        } Msp;
        struct
        {
            LPWSTR sczDetectCondition;
            LPWSTR sczKB;
        } Msu;
    };
} BURN_PACKAGE;

typedef struct _BURN_PACKAGES
{
    BURN_ROLLBACK_BOUNDARY* rgRollbackBoundaries;
    DWORD cRollbackBoundaries;

    BURN_PACKAGE* rgPackages;
    DWORD cPackages;

    BURN_PACKAGE* rgCompatiblePackages;
    DWORD cCompatiblePackages;

    BURN_PATCH_TARGETCODE* rgPatchTargetCodes;
    DWORD cPatchTargetCodes;

    MSIPATCHSEQUENCEINFOW* rgPatchInfo;
    BURN_PACKAGE** rgPatchInfoToPackage; // direct lookup from patch information to the (MSP) package it describes.
                                         // Thus this array is the exact same size as rgPatchInfo.
    DWORD cPatchInfo;
} BURN_PACKAGES;


// function declarations

HRESULT PackagesParseFromXml(
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in IXMLDOMNode* pixnBundle
    );
void PackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
void PackagesUninitialize(
    __in BURN_PACKAGES* pPackages
    );
HRESULT PackageFindById(
    __in BURN_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __out BURN_PACKAGE** ppPackage
    );
HRESULT PackageFindRelatedById(
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in_z LPCWSTR wzId,
    __out BURN_PACKAGE** ppPackage
    );
HRESULT PackageGetProperty(
    __in const BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzProperty,
    __out_z_opt LPWSTR* psczValue
    );
HRESULT PackageEnsureCompatiblePackagesArray(
    __in BURN_PACKAGES* pPackages
    );


#if defined(__cplusplus)
}
#endif
