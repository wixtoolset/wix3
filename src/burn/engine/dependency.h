//-------------------------------------------------------------------------------------------------
// <copyright file="dependency.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Dependency functions for Burn.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#if defined(__cplusplus)
extern "C" {
#endif

// constants

const LPCWSTR DEPENDENCY_IGNOREDEPENDENCIES = L"IGNOREDEPENDENCIES";


// function declarations

/********************************************************************
 DependencyUninitialize - Frees and zeros memory allocated in the
  dependency.

*********************************************************************/
void DependencyUninitialize(
    __in BURN_DEPENDENCY_PROVIDER* pProvider
    );

/********************************************************************
 DependencyParseProvidersFromXml - Parses dependency information
  from the manifest for the specified package.

*********************************************************************/
HRESULT DependencyParseProvidersFromXml(
    __in BURN_PACKAGE* pPackage,
    __in IXMLDOMNode* pixnPackage
    );

/********************************************************************
 DependencyDetectProviderKeyBundleId - Detect if the provider key is
  registered and if so what bundle is registered.

 Note: Returns E_NOTFOUND if the provider key is not registered.
*********************************************************************/
HRESULT DependencyDetectProviderKeyBundleId(
    __in BURN_REGISTRATION* pRegistration
    );

/********************************************************************
 DependencyPlanInitialize - Initializes the plan.

*********************************************************************/
HRESULT DependencyPlanInitialize(
    __in const BURN_ENGINE_STATE* pEngineState,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyAllocIgnoreDependencies - Allocates the dependencies to
  ignore as a semicolon-delimited string.

*********************************************************************/
HRESULT DependencyAllocIgnoreDependencies(
    __in const BURN_PLAN *pPlan,
    __out_z LPWSTR* psczIgnoreDependencies
    );

/********************************************************************
 DependencyAddIgnoreDependencies - Populates the ignore dependency
  names.

*********************************************************************/
HRESULT DependencyAddIgnoreDependencies(
    __in STRINGDICT_HANDLE sdIgnoreDependencies,
    __in_z LPCWSTR wzAddIgnoreDependencies
    );

/********************************************************************
 DependencyDependentExists - Checks to see if the provider key is
  already dependent on this bundle.

*********************************************************************/
BOOL DependencyDependentExists(
    __in const BURN_REGISTRATION* pRegistration,
    __in_z LPCWSTR wzDependentProviderKey
    );

/********************************************************************
 DependencyPlanPackageBegin - Updates the dependency registration
  action depending on the calculated state for the package.

*********************************************************************/
HRESULT DependencyPlanPackageBegin(
    __in BOOL fPerMachine,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyPlanPackage - adds dependency related actions to the plan
  for this package.

*********************************************************************/
HRESULT DependencyPlanPackage(
    __in_opt DWORD *pdwInsertSequence,
    __in const BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyPlanPackageComplete - Updates the dependency registration
  action depending on the planned action for the package.

*********************************************************************/
HRESULT DependencyPlanPackageComplete(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyExecutePackageProviderAction - Registers or unregisters
  provider information for the package contained within the action.

*********************************************************************/
HRESULT DependencyExecutePackageProviderAction(
    __in const BURN_EXECUTE_ACTION* pAction
    );

/********************************************************************
 DependencyExecutePackageDependencyAction - Registers or unregisters
  dependency information for the package contained within the action.

*********************************************************************/
HRESULT DependencyExecutePackageDependencyAction(
    __in BOOL fPerMachine,
    __in const BURN_EXECUTE_ACTION* pAction
    );

/********************************************************************
 DependencyRegisterBundle - Registers the bundle dependency provider.

*********************************************************************/
HRESULT DependencyRegisterBundle(
    __in const BURN_REGISTRATION* pRegistration
    );

/********************************************************************
 DependencyProcessDependentRegistration - Registers or unregisters dependents
  on the bundle based on the action.

*********************************************************************/
HRESULT DependencyProcessDependentRegistration(
    __in const BURN_REGISTRATION* pRegistration,
    __in const BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    );

/********************************************************************
 DependencyUnregisterBundle - Removes the bundle dependency provider.

 Note: Does not check for existing dependents before removing the key.
*********************************************************************/
void DependencyUnregisterBundle(
    __in const BURN_REGISTRATION* pRegistration
    );

#if defined(__cplusplus)
}
#endif
