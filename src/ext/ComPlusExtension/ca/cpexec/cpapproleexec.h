#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpapproleexec.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ application role functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// function prototypes

HRESULT CpiConfigureApplicationRoles(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    );
HRESULT CpiRollbackConfigureApplicationRoles(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    );
HRESULT CpiConfigureUsersInApplicationRoles(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    );
HRESULT CpiRollbackConfigureUsersInApplicationRoles(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    );
