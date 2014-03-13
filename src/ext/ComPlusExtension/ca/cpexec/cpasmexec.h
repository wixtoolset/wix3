#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpasmexec.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ assembly functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// function prototypes

HRESULT CpiConfigureAssemblies(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    );
HRESULT CpiRollbackConfigureAssemblies(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    );
HRESULT CpiConfigureRoleAssignments(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    );
HRESULT CpiRollbackConfigureRoleAssignments(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    );
