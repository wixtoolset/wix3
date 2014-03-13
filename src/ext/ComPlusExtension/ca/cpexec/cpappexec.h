#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpappexec.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ application functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------


// function prototypes

HRESULT CpiConfigureApplications(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    );
HRESULT CpiRollbackConfigureApplications(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    );
