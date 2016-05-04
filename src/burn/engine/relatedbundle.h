#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

HRESULT RelatedBundlesInitializeForScope(
    __in BOOL fPerMachine,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    );
void RelatedBundlesUninitialize(
    __in BURN_RELATED_BUNDLES* pRelatedBundles
    );

#if defined(__cplusplus)
}
#endif
