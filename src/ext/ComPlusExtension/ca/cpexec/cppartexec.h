#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cppartexec.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ partition functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// function prototypes

HRESULT CpiConfigurePartitions(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    );
HRESULT CpiRollbackConfigurePartitions(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    );
HRESULT CpiConfigurePartitionUsers(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    );
HRESULT CpiRollbackConfigurePartitionUsers(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    );
